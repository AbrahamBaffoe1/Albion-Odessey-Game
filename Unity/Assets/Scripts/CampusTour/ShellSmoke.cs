using System;
using System.Collections;
using System.IO;
using UnityEngine;
namespace AlbionOdyssey
{
    public sealed class ShellSmoke : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Boot(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-shellSmoke")>=0)new GameObject("Shell validation").AddComponent<ShellSmoke>();}
        IEnumerator Start()
        {
            Application.runInBackground=true;var test=Check();while(true){object current;try{if(!test.MoveNext())break;current=test.Current;}catch(Exception e){Debug.LogError("SHELL_SMOKE_FAILED: "+e);Application.Quit(1);yield break;}yield return current;}
            Debug.Log("SHELL_SMOKE_OK: launch, play, story/video menus, 9-video filter, summary, failed-save retention, retry, chapter-complete screen and resume");Application.Quit(0);
        }
        void Require(bool value,string message){if(!value)throw new Exception(message);}
        void Capture(string name)=>ScreenCapture.CaptureScreenshot(Path.Combine(Application.dataPath,"../../"+name+".png"));
        IEnumerator Check()
        {
            OdysseyGame g=null;float deadline=Time.realtimeSinceStartup+45;
            while(g==null||g.shell==null){g=FindAnyObjectByType<OdysseyGame>();Require(Time.realtimeSinceStartup<deadline,"Boot timeout");yield return null;}
            Require(!CampusShell.HasCompletedChapter(new Keeper{milestones=32}),"Beacon alone incorrectly completes chapter");
            Require(CampusShell.HasCompletedChapter(new Keeper{milestones=63}),"Completed charter not recognized");
            Application.runInBackground=true;var shell=g.shell;shell.ShowLaunch();
            Require(shell.OwnsPanel&&!g.player.controls,"Launch did not pause input");Require(Cursor.lockState==CursorLockMode.None,"Launch cursor locked");
            yield return new WaitForEndOfFrame();Capture("Launch-screen");
            shell.Play();Require(!g.life.PanelOpen&&g.player.controls,"Play did not resume");
            shell.Stories();Require(g.tour.IsOpen&&!g.tour.VideosOnly&&g.tour.VisibleCount==59,"Story menu route");
            shell.ShowLaunch();shell.Videos();Require(g.tour.IsOpen&&g.tour.VideosOnly&&g.tour.VisibleCount==9,"College videos route");
            yield return new WaitForEndOfFrame();Capture("College-videos-screen");
            // A rejected save must keep the session and disable the quit action.
            int savedAcorns=g.state.Current.acorns;g.state.Current.acorns=-1;shell.EndSession();
            Require(!shell.SaveSucceeded&&shell.IsSummary,"Failed save not retained");g.state.Current.acorns=savedAcorns;
            shell.EndSession();Require(shell.SaveSucceeded&&shell.IsSummary&&!g.player.controls,"Summary save/input");
            yield return new WaitForEndOfFrame();Capture("Session-end-screen");
            shell.EndSession(true);Require(shell.IsSummary,"Chapter result screen");yield return new WaitForEndOfFrame();Capture("Chapter-complete-screen");
            shell.Play();Require(g.player.controls&&!g.life.PanelOpen,"Continue exploring route");
            shell.ShowLaunch();Require(shell.OwnsPanel,"Return to launch route");
        }
    }
}
