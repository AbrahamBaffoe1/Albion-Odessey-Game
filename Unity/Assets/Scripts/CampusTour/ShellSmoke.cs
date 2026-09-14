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
            Debug.Log("SHELL_SMOKE_OK: launch, pause, Escape/VR routing, modal return, play, story/video menus, summary, save recovery and resume");Application.Quit(0);
        }
        void Require(bool value,string message){if(!value)throw new Exception(message);}
        void Capture(string name)=>ScreenCapture.CaptureScreenshot(Path.Combine(Application.dataPath,"../../"+name+".png"));
        IEnumerator Check()
        {
            OdysseyGame g=null;float deadline=Time.realtimeSinceStartup+45;
            while(g==null||!g.Ready||g.shell==null){g=FindAnyObjectByType<OdysseyGame>();Require(Time.realtimeSinceStartup<deadline,"Boot timeout");yield return null;}
            Require(!CampusShell.HasCompletedChapter(new Keeper{milestones=32}),"Beacon alone incorrectly completes chapter");
            Require(CampusShell.HasCompletedChapter(new Keeper{milestones=63}),"Completed charter not recognized");
            yield return new WaitForSecondsRealtime(.25f);
            Application.runInBackground=true;var shell=g.shell;shell.ShowLaunch();
            Require(shell.OwnsPanel&&!g.player.controls,"Launch did not pause input");Require(Cursor.lockState==CursorLockMode.None,"Launch cursor locked");
            yield return new WaitForEndOfFrame();Capture("Launch-screen");
            shell.Play();Require(!g.life.PanelOpen&&g.player.controls,"Play did not resume");
            Require(!g.vr.HandleMenuInput(false,true,0,0,false)&&!g.vr.IsOpen,"Closed VR panel stole Escape/Back");
            shell.OpenPause();Require(shell.IsPaused&&!g.player.controls&&Cursor.visible,"Pause did not release cursor and stop movement");
            yield return new WaitForEndOfFrame();Capture("Pause-menu");
            shell.ActivatePause(4);Require(g.vr.IsOpen&&!g.player.controls&&!shell.OwnsPanel,"Explicit VR settings route");
            yield return new WaitForEndOfFrame();Capture("VR-comfort-menu");
            g.vr.HandleMenuInput(false,true,0,0,false);
            Require(shell.IsPaused&&!g.vr.IsOpen&&!g.player.controls&&Cursor.visible,"VR Back failed to restore paused menu");
            shell.ActivatePause(0);Require(!g.life.PanelOpen&&g.player.controls,"Pause Resume did not restore movement");
            g.vr.HandleMenuInput(true,false,0,0,false);Require(g.vr.IsOpen,"F8 failed to open VR settings");
            g.vr.HandleMenuInput(true,false,0,0,false);Require(!g.vr.IsOpen&&g.player.controls,"F8 failed to return to gameplay");
            shell.OpenPause();shell.ActivatePause(1);Require(g.accountPanel.IsOpen&&!g.player.controls,"Pause account route");
            shell.OpenPause();shell.ActivatePause(5);Require(shell.OwnsPanel&&!shell.IsPaused,"Pause main menu route");
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
