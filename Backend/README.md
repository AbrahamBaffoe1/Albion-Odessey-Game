# Live student accounts

The Unity player uses a deployed Supabase Auth service and PostgreSQL profiles.
There is no local success fallback or simulated sign-in. Email-code authentication
proves control of an email address; it does not prove Albion College enrollment.

## Player flow

Open **Student sign in / join** on the campus menu, or press **F7**. Choose
**New here? Create account** for a new account, enter an email address, request a
code, then enter the code received by email. Returning students request a fresh
code in **Sign in** mode. The account panel edits and reloads the online display
name and signs out. Mouse/keyboard, controller navigation and an on-screen
keyboard are provided. Physical headset testing remains pending.

The account and profile persist online. This release keeps session credentials in
memory only, so restarting the app requires a new email code. Local Keeper saves,
buildings, classes and progress remain device-local. LAN presence is a separate
prototype and is not authenticated by this service. Hosted multiplayer, cloud
game saves, student enrollment and teacher authorization are separate work.

## Deployed services

- Supabase project: `livehrsersrhldektwwq`, **Albion Odyssey Accounts**.
- Public URL: `https://livehrsersrhldektwwq.supabase.co`.
- Transactional sender: `Albion Odyssey <albion-odyssey-accounts@agentmail.to>`.
- SMTP: `smtp.agentmail.to`, TLS port `465`; login is the sender inbox and
  password is the AgentMail API key, stored in Supabase's encrypted SMTP settings.
- Confirmation and magic-link templates both display `{{ .Token }}` for in-game
  entry. No browser redirect or password is needed.
- Free service plans; initial auth delivery limit is 30 messages/hour, with a
  60-second resend interval. Monitor quotas before a campus-wide rollout. A
  branded sender domain and larger capacity require additional provider setup.

Manage infrastructure and retrieve credentials with **Stripe Projects CLI**.
The CLI-created local state and `.env` files are ignored. Never commit the database
password, SMTP/API key, service role key, email codes, or session tokens. The
checked-in `Unity/Assets/Resources/AccountConfig.json` contains only the intended
public URL and publishable client key. Database authorization relies on RLS, not
on keeping that public key secret.

## Database deployment

Apply `migrations/*.sql` in order with an administrative database connection.
The server creates a profile when Auth creates a user. Authenticated clients can
select only their own profile and update only its display name. They cannot
insert profiles, change identity/timestamps, or read/update another student.
The profile stores no email address, school status, currency, or authority roles.
Deleting a user through an authorized administrative workflow cascades its profile.

On this project's dedicated pooler, deploy using environment-provided credentials:

```sh
stripe projects env --pull
set -a
source .env
set +a
PGPASSWORD="$SUPABASE_DB_PASS" psql \
  "host=aws-0-ca-central-1.pooler.supabase.com port=5432 user=postgres.$SUPABASE_PROJECT_REF dbname=postgres sslmode=require" \
  -X -v ON_ERROR_STOP=1 -f Backend/migrations/001_student_accounts.sql
# Apply the remaining numbered migrations with the same connection.
```

## Live verification

These opt-in tests send actual verification emails only to the dedicated owned QA
inboxes. The API test checks delivered sign-up, invalid/reused codes, return
sign-in, refresh/revocation, persistent profiles, column permissions and RLS. The
player test launches the actual Mac build, verifies a delivered code through
`StudentAccountService`, saves/reloads a profile, signs out, and captures the UI.
Tests remove their QA accounts and leave normal student accounts untouched.

```sh
python3 -m venv Verification/accounts-venv
Verification/accounts-venv/bin/pip install -r Backend/requirements-test.txt
# Load provisioned credentials into the environment without printing them.
# Verification/account-inboxes.json must map qa-a / qa-b to owned test inboxes.
Verification/accounts-venv/bin/python Backend/test_accounts_live.py \
  --run-live --inboxes Verification/account-inboxes.json \
  --report Verification/accounts-live-report.json
Verification/accounts-venv/bin/python Backend/test_accounts_unity.py \
  --run-live --inboxes Verification/account-inboxes.json \
  --app 'Unity/Builds/Albion Odyssey.app' --output Verification/accounts-unity
```

Run the tests sequentially and respect the email resend interval. The player test
uses isolated playtest saves. Code handoff files are owner-readable, atomically
written and removed after use. Reports never contain access or refresh tokens.

Provider references: [Supabase email OTP](https://supabase.com/docs/guides/auth/auth-email-passwordless),
[Supabase SMTP](https://supabase.com/docs/guides/auth/auth-smtp),
[AgentMail SMTP](https://docs.agentmail.to/imap-smtp).
