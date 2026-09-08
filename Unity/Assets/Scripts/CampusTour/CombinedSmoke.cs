using System;
using System.Collections;
using System.Linq;
using System.IO;
using UnityEngine;
using AlbionOdyssey.BuildingDesigner;
namespace AlbionOdyssey
{
    public sealed class CombinedSmoke : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Boot(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-combinedSmoke")>=0)new GameObject("Combined game checks").AddComponent<CombinedSmoke>();}
        IEnumerator Start()
        {
            Application.runInBackground=true;var test=Check();while(true){object current;try{if(!test.MoveNext())break;current=test.Current;}catch(Exception e){Debug.LogError("COMBINED_SMOKE_FAILED: "+e);Application.Quit(1);yield break;}yield return current;}
            Debug.Log("COMBINED_SMOKE_OK: one launch, 61 destinations, tour-to-studio cancellation, camera/audio ownership, studio save on session end, campus restore and course access");Application.Quit(0);
        }
        void Require(bool value,string message){if(!value)throw new Exception(message);}
        IEnumerator Check()
        {
            OdysseyGame g=null;BlueprintStudio studio=null;float deadline=Time.realtimeSinceStartup+45;
            while(g==null||g.shell==null||studio==null||!studio.Ready){g=FindAnyObjectByType<OdysseyGame>();studio=FindAnyObjectByType<BlueprintStudio>();Require(Time.realtimeSinceStartup<deadline,"Boot timeout");yield return null;}
            Application.runInBackground=true;g.shell.ShowLaunch();yield return new WaitForEndOfFrame();
            var path=Environment.GetEnvironmentVariable("COMBINED_SCREENSHOT");if(!string.IsNullOrWhiteSpace(path))ScreenCapture.CaptureScreenshot(path);
            foreach(var p in CampusCatalog.Places)Require(g.tour.catalog.ForCampus(p.id)!=null,"Missing building "+p.id);
            var before=g.player.transform.position;g.tour.Open(g.tour.catalog.ForCampus("50"));g.tour.ShowMedia(g.tour.selected.media[0]);
            Require(g.shell.BuildYourOwn()&&studio.Active&&!g.enabled&&Time.timeScale==0,"Studio entry did not suspend campus");
            Require(!g.tour.HasActiveMedia,"Tour download not cancelled");yield return null;
            Require(FindObjectsByType<Camera>().Count(c=>c.enabled)==1,"More than one active camera in studio");
            Require(FindObjectsByType<AudioListener>().Count(l=>l.enabled)==1,"More than one listener in studio");
            studio.StartWalk();yield return null;Require(studio.Active,"Studio walk failed");studio.StopWalk();
            g.shell.EndSession();yield return null;
            Require(!studio.Active&&g.enabled&&Time.timeScale==1&&g.shell.IsSummary&&g.shell.SaveSucceeded,"Ending in studio did not save and restore campus");
            Require(Vector2.Distance(new Vector2(before.x,before.z),new Vector2(g.player.transform.position.x,g.player.transform.position.z))<.01f,"Campus return position changed");
            Require(FindObjectsByType<AudioListener>().Count(l=>l.enabled)==1,"Listener not restored after studio");
            g.shell.PlaceCampusBuildings();Require(g.building,"Campus plot builder inaccessible");g.ToggleMode();
            g.shell.Courses();Require(g.life.panel=="courses","Course menu inaccessible");g.shell.Stories();Require(g.tour.IsOpen,"Stories inaccessible after studio");g.shell.Play();Require(g.player.controls,"Player input not restored");
        }
    }
}
