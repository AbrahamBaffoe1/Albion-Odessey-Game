using System;
using System.Collections;
using System.Net.Mail;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace AlbionOdyssey
{
    [Serializable] public sealed class StudentAccountConfig { public string url, publishableKey; }
    [Serializable] public sealed class StudentAuthUser { public string id, email, email_confirmed_at; }
    [Serializable] public sealed class StudentAuthSession { public string access_token, refresh_token; public int expires_in; public StudentAuthUser user; }
    [Serializable] public sealed class StudentOnlineProfile { public string id, display_name; }
    [Serializable] sealed class StudentProfileRows { public StudentOnlineProfile[] rows; }
    [Serializable] sealed class EmailCodeRequest { public string email; public bool create_user; }
    [Serializable] sealed class VerifyEmailCodeRequest { public string email, token; public string type = "email"; }
    [Serializable] sealed class RefreshAccountRequest { public string refresh_token; }
    [Serializable] sealed class ProfileNameRequest { public string display_name; }

    // Only the public project key ships in the player. Sessions exist in memory;
    // email codes, access tokens and refresh tokens never enter saves or PlayerPrefs.
    public sealed class StudentAccountService : MonoBehaviour
    {
        StudentAccountConfig config;
        StudentAuthSession session;
        UnityWebRequest currentRequest;
        float refreshAt, resendAt;
        string pendingEmail;
        public bool Busy { get; private set; }
        public bool SignedIn => session != null && session.user != null;
        public bool Configured => config != null && Uri.TryCreate(config.url, UriKind.Absolute, out var uri) && uri.Scheme == "https" && !string.IsNullOrEmpty(config.publishableKey) && config.publishableKey.StartsWith("sb_publishable_");
        public string UserId => SignedIn ? session.user.id : "";
        public string Email => SignedIn ? session.user.email : "";
        public string DisplayName { get; private set; } = "Student";
        public bool ProfileLoaded { get; private set; }
        public string Status { get; private set; } = "Use your email to create an account or sign in.";
        public int ResendSeconds => Mathf.Max(0, Mathf.CeilToInt(resendAt - Time.realtimeSinceStartup));
        public bool CodeRequested => !string.IsNullOrEmpty(pendingEmail);
        public bool CodeMatchesEmail(string email) => CodeRequested && string.Equals(pendingEmail, (email ?? "").Trim(), StringComparison.OrdinalIgnoreCase);

        public void Setup()
        {
            var asset = Resources.Load<TextAsset>("AccountConfig");
            try { config = asset == null ? null : JsonUtility.FromJson<StudentAccountConfig>(asset.text); }
            catch { config = null; }
            if (!Configured) Status = "Online accounts are unavailable in this build.";
        }
        public static bool ValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email) || email.Length > 254) return false;
            try { var parsed = new MailAddress(email.Trim()); return parsed.Address == email.Trim() && parsed.Host.Contains("."); }
            catch { return false; }
        }
        public static bool ValidName(string name)
        {
            if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 24) return false;
            foreach (char c in name) if (char.IsControl(c) || c == '<' || c == '>') return false;
            return true;
        }
        public void RequestCode(string email, bool createAccount)
        {
            if (Busy || !Configured || SignedIn) return;
            if (!ValidEmail(email)) { Status = "Enter a valid email address."; return; }
            if (ResendSeconds > 0) { Status = "Wait " + ResendSeconds + " seconds before requesting another code."; return; }
            StartCoroutine(SendCode(email.Trim(), createAccount));
        }
        IEnumerator SendCode(string email, bool createAccount)
        {
            Busy = true; pendingEmail = null; Status = "Requesting your sign-in code…";
            bool ok = false;
            yield return Request("/auth/v1/otp", "POST", JsonUtility.ToJson(new EmailCodeRequest { email = email, create_user = createAccount }), false, (success, body) => ok = success);
            if (ok) { pendingEmail = email; resendAt = Time.realtimeSinceStartup + 60; Status = "Code sent to " + email + ". Check your inbox and spam folder."; }
            Busy = false;
        }
        public void VerifyCode(string code)
        {
            if (Busy || !Configured || !CodeRequested || SignedIn) return;
            code = (code ?? "").Trim();
            if (code.Length < 6 || code.Length > 10) { Status = "Enter the code from your email."; return; }
            foreach (char c in code) if (c < '0' || c > '9') { Status = "The email code contains digits only."; return; }
            StartCoroutine(Verify(code));
        }
        IEnumerator Verify(string code)
        {
            Busy = true; Status = "Verifying your email…";
            StudentAuthSession candidate = null;
            yield return Request("/auth/v1/verify", "POST", JsonUtility.ToJson(new VerifyEmailCodeRequest { email = pendingEmail, token = code }), false,
                (ok, body) => { if (ok) candidate = ParseSession(body); });
            if (candidate != null)
            {
                session = candidate; refreshAt = Time.realtimeSinceStartup + Mathf.Max(10, candidate.expires_in - 60);
                pendingEmail = null;
                yield return LoadProfile();
            }
            Busy = false;
        }
        StudentAuthSession ParseSession(string body)
        {
            try
            {
                var value = JsonUtility.FromJson<StudentAuthSession>(body);
                if (value != null && !string.IsNullOrEmpty(value.access_token) && !string.IsNullOrEmpty(value.refresh_token) && value.expires_in > 0 && value.user != null && Guid.TryParse(value.user.id, out _) && !string.IsNullOrEmpty(value.user.email_confirmed_at)) return value;
            }
            catch { }
            Status = "The service did not return a verified session. Request a new code.";
            return null;
        }
        IEnumerator LoadProfile()
        {
            ProfileLoaded = false;
            yield return Request("/rest/v1/student_profiles?select=id,display_name&id=eq." + UserId, "GET", null, true, (ok, body) =>
            {
                if (!ok) return;
                try
                {
                    var rows = JsonUtility.FromJson<StudentProfileRows>("{\"rows\":" + body + "}").rows;
                    if (rows.Length == 1 && rows[0].id == UserId) { DisplayName = rows[0].display_name; ProfileLoaded = true; Status = "Signed in. Your student profile is stored online."; return; }
                }
                catch { }
                Status = "Signed in, but your profile could not load. Use Reload profile.";
            });
        }
        public void ReloadProfile() { if (SignedIn && !Busy) StartCoroutine(Reload()); }
        IEnumerator Reload() { Busy = true; yield return LoadProfile(); Busy = false; }
        public void SaveDisplayName(string value)
        {
            if (!SignedIn || Busy || !ProfileLoaded) return;
            if (!ValidName(value)) { Status = "Choose a name of 1–24 characters without angle brackets or line breaks."; return; }
            StartCoroutine(SaveName(value.Trim()));
        }
        IEnumerator SaveName(string value)
        {
            Busy = true;
            yield return Request("/rest/v1/student_profiles?id=eq." + UserId, "PATCH", JsonUtility.ToJson(new ProfileNameRequest { display_name = value }), true,
                (ok, body) => { if (ok) { DisplayName = value; Status = "Your profile was saved online."; } });
            Busy = false;
        }
        void Update()
        {
            if (SignedIn && !Busy && Time.realtimeSinceStartup >= refreshAt) StartCoroutine(RefreshSession());
        }
        IEnumerator RefreshSession()
        {
            Busy = true; StudentAuthSession next = null; bool received = false;
            yield return Request("/auth/v1/token?grant_type=refresh_token", "POST", JsonUtility.ToJson(new RefreshAccountRequest { refresh_token = session.refresh_token }), false,
                (ok, body) => { received = ok; if (ok) next = ParseSession(body); });
            if (next != null) { session = next; refreshAt = Time.realtimeSinceStartup + Mathf.Max(10, next.expires_in - 60); }
            else { ClearSession(); Status = received ? "Please sign in again." : "Your session could not be renewed. Sign in again when connected."; }
            Busy = false;
        }
        public void SignOut() { if (!Busy && SignedIn) StartCoroutine(Logout()); }
        IEnumerator Logout()
        {
            Busy = true; bool revoked = false;
            yield return Request("/auth/v1/logout?scope=local", "POST", "{}", true, (ok, body) => revoked = ok);
            ClearSession(); Busy = false;
            Status = revoked ? "Signed out. Your profile remains safely stored online." : "Signed out on this device. The server could not confirm session revocation.";
        }
        void ClearSession() { session = null; DisplayName = "Student"; ProfileLoaded = false; pendingEmail = null; }
        IEnumerator Request(string path, string method, string body, bool authenticated, Action<bool, string> finished)
        {
            using (var request = new UnityWebRequest(config.url.TrimEnd('/') + path, method))
            {
                currentRequest = request; request.timeout = 20; request.redirectLimit = 0;
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("apikey", config.publishableKey);
                if (authenticated) request.SetRequestHeader("Authorization", "Bearer " + session.access_token);
                if (body != null) { request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body)); request.SetRequestHeader("Content-Type", "application/json"); }
                yield return request.SendWebRequest();
                bool ok = request.result == UnityWebRequest.Result.Success;
                if (!ok)
                {
                    Status = request.responseCode == 429 ? "Too many requests. Wait a little before trying again." :
                        request.responseCode == 0 ? "Cannot reach accounts. Check your connection and try again." :
                        path.Contains("verify") ? "That code is invalid or expired. Request a new code." :
                        path.Contains("otp") ? "The code could not be sent. Check your email. New here? Choose Create account." : "The account request failed. Please try again.";
                }
                finished(ok, ok ? request.downloadHandler.text : "");
                currentRequest = null;
            }
        }
        void OnDestroy() { currentRequest?.Abort(); ClearSession(); }
    }
}
