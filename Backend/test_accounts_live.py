"""Opt-in integration test against the real account service and owned QA inboxes.

Credentials are environment-only. No tokens, codes, or email bodies are logged.
Requires agentmail and psycopg[binary]; see README.md. Test users are removed.
"""
import argparse
import json
import os
import re
import time
from datetime import datetime, timezone, timedelta
from pathlib import Path
from urllib.request import Request, urlopen
from urllib.error import HTTPError

from agentmail import AgentMail
import psycopg


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--run-live', action='store_true', required=True)
    parser.add_argument('--inboxes', required=True)
    parser.add_argument('--report', required=True)
    args = parser.parse_args()
    config = json.loads(Path('Unity/Assets/Resources/AccountConfig.json').read_text())
    inboxes = json.loads(Path(args.inboxes).read_text())
    mail = AgentMail(api_key=os.environ['AGENTMAIL_AGENTMAIL_API_KEY'])
    results = {}
    created = []
    sent_at = {}

    def check(name, passed):
        results[name] = bool(passed)
        print(name + ': ' + ('PASS' if passed else 'FAIL'), flush=True)
        if not passed:
            raise RuntimeError(name)

    def request(path, method='GET', body=None, token=None, representation=False):
        headers = {'apikey': config['publishableKey'], 'Content-Type': 'application/json'}
        if token:
            headers['Authorization'] = 'Bearer ' + token
        if representation:
            headers['Prefer'] = 'return=representation'
        req = Request(config['url'] + path, method=method, headers=headers,
                      data=json.dumps(body).encode() if body is not None else None)
        try:
            with urlopen(req, timeout=25) as response:
                raw = response.read()
                return response.status, json.loads(raw) if raw else None
        except HTTPError as error:
            return error.code, None

    def code_for(email, create):
        delay = max(0, sent_at.get(email, 0) + 61 - time.monotonic())
        if delay:
            time.sleep(delay)
        start = datetime.now(timezone.utc)
        status, _ = request('/auth/v1/otp', 'POST', {'email': email, 'create_user': create})
        if status == 429:
            print('Waiting for the real email resend limit.', flush=True)
            time.sleep(60)
            start = datetime.now(timezone.utc)
            status, _ = request('/auth/v1/otp', 'POST', {'email': email, 'create_user': create})
        if status != 200:
            print('Code request HTTP status:', status, flush=True)
        check(('signup' if create else 'signin') + '_code_requested_' + str(len(sent_at)), status == 200)
        sent_at[email] = time.monotonic()
        deadline = time.monotonic() + 90
        while time.monotonic() < deadline:
            messages = mail.inboxes.messages.list(email, after=start - timedelta(seconds=5), limit=10).messages
            for summary in messages:
                message = mail.inboxes.messages.get(email, summary.message_id)
                body = (message.text or '') + '\n' + re.sub(r'<[^>]+>', ' ', message.html or '')
                found = re.search(r'(?<!\w)\d{6,10}(?!\w)', body)
                if found:
                    return found.group()
            time.sleep(2)
        raise RuntimeError('verification_email_not_delivered')

    def verify(email, code):
        return request('/auth/v1/verify', 'POST', {'email': email, 'token': code, 'type': 'email'})

    def profile(uid):
        return '/rest/v1/student_profiles?id=eq.' + uid

    try:
        sessions = []
        for key in ('qa-a', 'qa-b'):
            email = inboxes[key]
            code = code_for(email, True)
            check('delivered_email_' + key, True)
            status, _ = verify(email, '0000000000')
            check('invalid_code_rejected_' + key, status in (400, 403))
            status, session = verify(email, code)
            if status == 200:
                created.append((session['user']['id'], email))
            check('verified_signup_' + key, status == 200 and bool(session['user'].get('email_confirmed_at')))
            sessions.append(session)
            status, _ = verify(email, code)
            check('used_code_rejected_' + key, status in (400, 403))
        a, b = sessions
        uid = a['user']['id']
        token = a['access_token']
        status, rows = request(profile(uid), token=token)
        check('profile_created_by_server', status == 200 and len(rows) == 1 and rows[0]['display_name'] == 'Student')
        status, _ = request(profile(uid), 'PATCH', {'display_name': 'Albion QA Student'}, token)
        check('profile_save', status == 204)
        status, rows = request(profile(uid), token=token)
        check('profile_reload', status == 200 and rows[0]['display_name'] == 'Albion QA Student')
        status, _ = request(profile(uid))
        check('anonymous_profile_denied', status in (401, 403))
        status, rows = request(profile(uid), token=b['access_token'])
        check('other_student_read_denied', status == 200 and rows == [])
        status, rows = request(profile(uid), 'PATCH', {'display_name': 'Intruder'}, b['access_token'], True)
        check('other_student_update_denied', status == 200 and rows == [])
        status, _ = request(profile(uid), 'PATCH', {'id': b['user']['id']}, token)
        check('identity_change_denied', status in (400, 401, 403))
        status, _ = request('/rest/v1/student_profiles', 'POST', {'id': uid}, token)
        check('client_profile_insert_denied', status in (400, 401, 403))
        status, _ = request(profile(uid), 'PATCH', {'display_name': '<invalid>'}, token)
        check('invalid_profile_name_denied', status == 400)
        status, refreshed = request('/auth/v1/token?grant_type=refresh_token', 'POST', {'refresh_token': a['refresh_token']})
        check('session_refresh', status == 200 and refreshed['user']['id'] == uid)
        status, _ = request('/auth/v1/logout?scope=local', 'POST', {}, refreshed['access_token'])
        check('sign_out', status == 204)
        status, _ = request('/auth/v1/token?grant_type=refresh_token', 'POST', {'refresh_token': refreshed['refresh_token']})
        check('signed_out_session_cannot_refresh', status in (400, 401, 403))
        code = code_for(inboxes['qa-a'], False)
        status, returning = verify(inboxes['qa-a'], code)
        check('returning_student_same_account', status == 200 and returning['user']['id'] == uid)
        status, rows = request(profile(uid), token=returning['access_token'])
        check('profile_persists_across_sessions', status == 200 and rows[0]['display_name'] == 'Albion QA Student')
    finally:
        # Restrict cleanup to the exact generated QA accounts from this run.
        if created:
            with psycopg.connect(host='aws-0-ca-central-1.pooler.supabase.com', port=5432,
                                 user='postgres.' + os.environ['SUPABASE_PROJECT_REF'], dbname='postgres',
                                 password=os.environ['SUPABASE_DB_PASS'], sslmode='require', connect_timeout=10) as db:
                for uid, email in created:
                    db.execute('delete from auth.users where id=%s and email=%s', (uid, email))
            results['test_accounts_removed'] = True
        Path(args.report).write_text(json.dumps({'tested_at': datetime.now(timezone.utc).isoformat(), 'checks': results}, indent=2) + '\n')


if __name__ == '__main__':
    main()
