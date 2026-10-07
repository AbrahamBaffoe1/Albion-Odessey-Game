using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace AlbionOdyssey
{
    // Explicit opt-in live integration check. The runner supplies only a disposable
    // QA address and its delivered email code; it cannot mint or bypass a session.
    public sealed class StudentAccountSmoke : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-accountSmoke") >= 0)
                new GameObject("Live account validation").AddComponent<StudentAccountSmoke>();
        }
        IEnumerator Start()
        {
            var test = Check();
            while (true)
            {
                object current;
                try { if (!test.MoveNext()) break; current = test.Current; }
                catch (Exception error) { Debug.LogError("ACCOUNT_SMOKE_FAILED: " + error.Message); Application.Quit(1); yield break; }
                yield return current;
            }
            Debug.Log("ACCOUNT_SMOKE_OK: delivered email, verified session, online profile save/reload, sign-out, account menu and movement lock");
            Application.Quit(0);
        }
        void Require(bool valid, string message) { if (!valid) throw new Exception(message); }
        IEnumerator Check()
        {
            string email = Environment.GetEnvironmentVariable("ACCOUNT_SMOKE_EMAIL");
            string codeFile = Environment.GetEnvironmentVariable("ACCOUNT_SMOKE_CODE_FILE");
            string output = Environment.GetEnvironmentVariable("ACCOUNT_SMOKE_OUTPUT");
            Require(StudentAccountService.ValidEmail(email) && !string.IsNullOrEmpty(codeFile) && !string.IsNullOrEmpty(output), "Missing opt-in QA configuration");
            OdysseyGame game = null;
            float deadline = Time.realtimeSinceStartup + 45;
            while (game == null || !game.Ready || game.accountPanel == null)
            {
                game = FindAnyObjectByType<OdysseyGame>();
                Require(Time.realtimeSinceStartup < deadline, "Boot timed out");
                yield return null;
            }
            var account = game.accounts;
            game.accountPanel.Open();
            Require(account.Configured && !account.SignedIn && game.accountPanel.IsOpen && !game.player.controls, "Account menu/input lock");
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(output, "account-sign-in.png"));
            account.RequestCode(email, true);
            while (account.Busy) yield return null;
            Require(account.CodeRequested && account.CodeMatchesEmail(email) && account.ResendSeconds > 0, "Email request failed");
            Require(!account.CodeMatchesEmail("different@example.com"), "Changed email accepted for old code");
            Debug.Log("ACCOUNT_SMOKE_CODE_REQUESTED");
            deadline = Time.realtimeSinceStartup + 100;
            while (!File.Exists(codeFile)) { Require(Time.realtimeSinceStartup < deadline, "Email delivery timed out"); yield return null; }
            string code = File.ReadAllText(codeFile).Trim(); File.Delete(codeFile);
            account.VerifyCode("0000000000");
            while (account.Busy) yield return null;
            Require(!account.SignedIn, "Invalid code accepted");
            account.VerifyCode(code); code = null;
            while (account.Busy) yield return null;
            Require(account.SignedIn && account.ProfileLoaded && account.Email == email, "Verified session/profile missing");
            account.SaveDisplayName("Unity QA Student");
            while (account.Busy) yield return null;
            account.ReloadProfile();
            while (account.Busy) yield return null;
            Require(account.ProfileLoaded && account.DisplayName == "Unity QA Student", "Online profile did not persist");
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(output, "account-verified.png"));
            account.SignOut();
            while (account.Busy) yield return null;
            Require(!account.SignedIn && !account.ProfileLoaded && account.UserId == "" && account.Email == "", "Session not cleared");
            game.shell.Play();
            Require(game.player.controls && !game.life.PanelOpen, "Guest play did not resume");
            yield return null;
        }
    }
}
