"""Launch the real Mac player and complete its email-code integration check.

Only use the owned QA inboxes created for this project. Never a person's mailbox.
"""
import argparse
import json
import os
from pathlib import Path
import re
import subprocess
import time
from datetime import datetime, timezone, timedelta

from agentmail import AgentMail
import psycopg


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--run-live', action='store_true', required=True)
    parser.add_argument('--inboxes', required=True)
    parser.add_argument('--app', required=True)
    parser.add_argument('--output', required=True)
    args = parser.parse_args()
    output = Path(args.output).resolve()
    output.mkdir(parents=True, exist_ok=True)
    code_file = output / 'delivered-code.txt'
    code_file.unlink(missing_ok=True)
    log = output / 'player.log'
    log.unlink(missing_ok=True)
    email = json.loads(Path(args.inboxes).read_text())['qa-a']
    mail = AgentMail(api_key=os.environ['AGENTMAIL_AGENTMAIL_API_KEY'])
    start = datetime.now(timezone.utc)
    # Give the player the public QA address and protected handoff path, no keys.
    env = {k: v for k, v in os.environ.items() if not k.startswith(('SUPABASE_', 'AGENTMAIL_'))}
    env.update(ACCOUNT_SMOKE_EMAIL=email, ACCOUNT_SMOKE_CODE_FILE=str(code_file), ACCOUNT_SMOKE_OUTPUT=str(output))
    executable = Path(args.app).resolve() / 'Contents/MacOS/Albion Odyssey'
    proc = subprocess.Popen([str(executable), '-accountSmoke', '-screen-fullscreen', '0', '-screen-width', '1280', '-screen-height', '800', '-logFile', str(log)], env=env,
                            stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    delivered = False
    result = {'started_at': start.isoformat()}
    try:
        deadline = time.monotonic() + 180
        while proc.poll() is None and time.monotonic() < deadline:
            text = log.read_text(errors='replace') if log.exists() else ''
            if 'ACCOUNT_SMOKE_CODE_REQUESTED' in text and not delivered:
                for summary in mail.inboxes.messages.list(email, after=start - timedelta(seconds=5), limit=10).messages:
                    message = mail.inboxes.messages.get(email, summary.message_id)
                    body = (message.text or '') + '\n' + re.sub(r'<[^>]+>', ' ', message.html or '')
                    code = re.search(r'(?<!\w)\d{6,10}(?!\w)', body)
                    if code:
                        # Atomic rename prevents the player reading an empty file.
                        pending = output / 'code.pending'
                        fd = os.open(pending, os.O_WRONLY | os.O_CREAT | os.O_TRUNC, 0o600)
                        with os.fdopen(fd, 'w') as stream:
                            stream.write(code.group())
                        pending.replace(code_file)
                        delivered = True
                        print('Real verification email delivered to the player.', flush=True)
                        break
            time.sleep(2)
        if proc.poll() is None:
            proc.terminate()
            proc.wait(timeout=15)
            raise RuntimeError('Player integration check timed out')
        text = log.read_text(errors='replace')
        for line in text.splitlines():
            if line.startswith(('ACCOUNT_SMOKE_OK:', 'ACCOUNT_SMOKE_FAILED:')):
                print(line)
        result.update(exit_code=proc.returncode, email_delivered=delivered, passed=proc.returncode == 0 and 'ACCOUNT_SMOKE_OK:' in text)
        if not result['passed']:
            raise RuntimeError('Player integration check failed')
    finally:
        code_file.unlink(missing_ok=True)
        (output / 'code.pending').unlink(missing_ok=True)
        if proc.poll() is None:
            proc.terminate()
            proc.wait(timeout=15)
        with psycopg.connect(host='aws-0-ca-central-1.pooler.supabase.com', port=5432,
                             user='postgres.' + os.environ['SUPABASE_PROJECT_REF'], dbname='postgres',
                             password=os.environ['SUPABASE_DB_PASS'], sslmode='require', connect_timeout=10) as db:
            db.execute('delete from auth.users where email=%s', (email,))
        result['qa_account_removed'] = True
        (output / 'result.json').write_text(json.dumps(result, indent=2) + '\n')


if __name__ == '__main__':
    main()
