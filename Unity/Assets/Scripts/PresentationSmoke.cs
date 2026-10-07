using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace AlbionOdyssey
{
    public sealed class PresentationSmoke : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-presentationSmoke")>=0)new GameObject("Presentation verification").AddComponent<PresentationSmoke>();}
        string output;
        IEnumerator Start()
        {
            Application.runInBackground=true;
            output=Environment.GetEnvironmentVariable("PRESENTATION_OUTPUT");Directory.CreateDirectory(output);
            var run=Check();while(true){object current;try{if(!run.MoveNext())break;current=run.Current;}catch(Exception error){Debug.LogError("PRESENTATION_SMOKE_FAILED: "+error);Application.Quit(1);yield break;}yield return current;}
            Debug.Log("PRESENTATION_SMOKE_OK: real boot stages, live rigged student, lobby, HUD, appearance, transition completion, responsive layout");Application.Quit(0);
        }
        void Require(bool ok,string message){if(!ok)throw new Exception(message);}
        void Capture(string name)=>ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));
        IEnumerator Check()
        {
            OdysseyGame game=null;int previous=0,stages=0;float deadline=Time.realtimeSinceStartup+60;
            while(game==null||!game.Ready)
            {
                game=FindAnyObjectByType<OdysseyGame>();Require(Time.realtimeSinceStartup<deadline,"Boot timeout");
                if(game!=null&&game.loading!=null){Require(!game.loading.Failed,"Initialization failed");Require(game.loading.Completed>=previous,"Loading progress regressed");if(previous!=game.loading.Completed){stages++;previous=game.loading.Completed;if(previous==2)Capture("01-real-loading");}}
                yield return null;
            }
            Require(stages>=3&&game.loading.Completed==game.loading.Total&&!game.loading.Busy,"Boot milestones missing");
            Require(game.presentation.CharacterReady&&game.presentation.CampusView.IsCreated(),"Real portrait or campus render missing");
            yield return new WaitForSecondsRealtime(.3f);
            game.shell.ShowLaunch();yield return new WaitForEndOfFrame();Capture("02-live-lobby");
            game.life.SetPanel("settings");yield return new WaitForEndOfFrame();Capture("03-student");
            game.shell.Play();yield return new WaitForEndOfFrame();Capture("04-campus-hud");
            game.shell.OpenPause();yield return new WaitForEndOfFrame();Capture("05-campus-menu");
            bool completed=false;game.loading.Transition("Preparing student test",()=>{game.life.SetPanel("settings");completed=true;});
            Require(game.loading.Busy&&!completed,"Transition did not yield a paint frame");
            deadline=Time.realtimeSinceStartup+10;
            while(game.loading.Busy){Require(Time.realtimeSinceStartup<deadline,"Transition timeout");yield return null;}
            Require(completed&&game.life.panel=="settings"&&!game.player.controls,"Transition changed movement ownership");
            game.accountPanel.Open();yield return new WaitForSecondsRealtime(.25f);yield return new WaitForEndOfFrame();Capture("06-account");
            game.shell.EndSession();yield return new WaitForEndOfFrame();Capture("07-session-summary");
            game.shell.ShowLaunch();Screen.SetResolution(1024,768,FullScreenMode.Windowed);yield return new WaitForSecondsRealtime(.4f);yield return new WaitForEndOfFrame();Capture("08-lobby-compact");
        }
    }
}
