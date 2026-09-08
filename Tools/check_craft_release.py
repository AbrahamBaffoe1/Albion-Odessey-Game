"""Run the five player integration checks from any development checkout."""
from pathlib import Path
import argparse
import json
import os
import subprocess


def main():
    root = Path(__file__).resolve().parents[1]
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--app', type=Path, default=root / 'Unity/Builds/Albion Odyssey.app')
    parser.add_argument('--output', type=Path, default=root / 'Verification')
    args = parser.parse_args()
    app = args.app.expanduser().resolve()
    binary = app / 'Contents/MacOS/Albion Odyssey' if app.suffix == '.app' else app
    if not binary.is_file():
        parser.error('Build the Mac game first, or pass --app with its location.')
    verify = args.output.expanduser().resolve()
    verify.mkdir(parents=True, exist_ok=True)
    (verify / 'craft').mkdir(exist_ok=True)
    env = os.environ.copy()
    env['CRAFT_OUTPUT'] = str(verify / 'craft')
    env['ODYSSEY_SMOKE_PATH'] = str(verify / 'campus08')
    env['COMBINED_SCREENSHOT'] = str(verify / 'craft/Combined-launch.png')
    checks = []
    for flag, tag in [('-craftSmoke', 'craft'), ('-combinedSmoke', 'combined08'),
                      ('-odysseySmoke', 'campus08'), ('-tourSmoke', 'tour08'),
                      ('-shellSmoke', 'shell08')]:
        with (verify / (tag + '-process.log')).open('w') as log:
            try:
                result = subprocess.run(
                    [str(binary), flag, '-screen-width', '1440', '-screen-height', '900',
                     '-screen-fullscreen', '0', '-logFile', str(verify / (tag + '-runtime.log'))],
                    env=env, stdout=log, stderr=subprocess.STDOUT, timeout=110)
                code = result.returncode
            except subprocess.TimeoutExpired:
                code = 124
        checks.append({'check': tag, 'exit': code})
        (verify / 'craft-checks.json').write_text(json.dumps(checks, indent=2) + '\n')
        print(tag, code, flush=True)
        if code:
            return code
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
