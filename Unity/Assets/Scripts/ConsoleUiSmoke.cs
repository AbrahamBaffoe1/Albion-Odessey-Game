using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;
namespace AlbionOdyssey
{
    // Explicit visual fixtures: no emails sent, no credentials used and no saves changed.
    public sealed class ConsoleUiSmoke:MonoBehaviour
    {
        string output;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Boot(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-consoleUiSmoke")>=0)new GameObject("Console UI review").AddComponent<ConsoleUiSmoke>();}
        void Set(object target,string name,object value){target.GetType().GetField(name,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(target,value);}
        IEnumerator Capture(string name){yield return new WaitForSecondsRealtime(.4f);yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));yield return new WaitForSecondsRealtime(.3f);}
        IEnumerator Start()
        {
            Application.runInBackground=true;output=Environment.GetEnvironmentVariable("CONSOLE_UI_OUTPUT");Directory.CreateDirectory(output);OdysseyGame g=null;
            float deadline=Time.realtimeSinceStartup+90;
            while(g==null||!g.Ready||g.loading.Busy){if(Time.realtimeSinceStartup>deadline){Debug.LogError("CONSOLE_UI_BOOT_TIMEOUT");Application.Quit(1);yield break;}g=FindAnyObjectByType<OdysseyGame>();yield return null;}
            yield return new WaitForSecondsRealtime(1);
            g.shell.ShowLaunch();yield return Capture("01-Console-home");
            yield return new WaitForSecondsRealtime(2.5f);yield return Capture("01b-Console-motion");
            g.shell.OpenPause();yield return Capture("02-Control-center");
            g.accountPanel.Open();yield return Capture("03-Sign-in");
            Set(g.accountPanel,"email","player@example.test");Set(g.accounts,"pendingEmail","player@example.test");
            yield return Capture("04-Email-code");
            Set(g.accountPanel,"code","bad-code");g.accountPanel.GetType().GetMethod("Activate",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(g.accountPanel,new object[]{3});if(!g.accounts.HasError||g.accounts.Busy)throw new Exception("Invalid-code feedback regression");
            yield return Capture("05-Code-feedback");g.accounts.ChangeEmail();
            g.shared.Open();yield return Capture("06-Online-rooms");
            g.life.SetPanel("campus");yield return Capture("07-Campus-navigation");
            CampusNavigationMap.Nature=true;yield return Capture("08-Nature-trails");CampusNavigationMap.Nature=false;
            g.loading.Report("Preparing campus geometry",3,6);yield return Capture("09-Loading");g.loading.Finish();
            g.shell.OpenPause();g.shell.ActivatePause(1);if(!g.accountPanel.IsOpen||g.player.controls)throw new Exception("Account menu route regression");
            g.shell.OpenPause();g.shell.ActivatePause(7);if(g.life.panel!="online"||g.player.controls)throw new Exception("Online menu route regression");
            g.shell.Play();g.city.Travel();
            if(!g.city.InCity||g.city.LandmarkCount!=7||g.campus.cars.Count!=7)throw new Exception("Combined city content regression");
            yield return Capture("10-Combined-downtown");
            g.environment.SetHour(19);yield return Capture("11-Downtown-dusk");
            g.city.Travel();
            g.shell.Play();if(!g.player.controls)throw new Exception("Return controls regression");
            File.WriteAllText(Path.Combine(output,"checks.txt"),"PASS: invalid-code feedback, account and online menu routing, movement restored. Screenshots use a fake email fixture; live code verification not tested.");
            Debug.Log("CONSOLE_UI_SMOKE_OK");Application.Quit(0);
        }
    }
}
