using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
namespace AlbionOdyssey
{
    public sealed class TourSmoke : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Boot()
        {if(Array.IndexOf(Environment.GetCommandLineArgs(),"-tourSmoke")>=0)new GameObject("Tour validation").AddComponent<TourSmoke>();}
        IEnumerator Start()
        {
            Application.runInBackground=true;
            var test=Check();while(true){object current;try{if(!test.MoveNext())break;current=test.Current;}catch(Exception e){Debug.LogError("TOUR_SMOKE_FAILED: "+e);Application.Quit(1);yield break;}yield return current;}
            Debug.Log("TOUR_SMOKE_OK: catalog, 61 mappings, search, room floor/door/walls, return, photo, panorama, video and cancellation");Application.Quit(0);
        }
        void Require(bool value,string message){if(!value)throw new Exception(message);}
        IEnumerator Check()
        {
            OdysseyGame g=null;float deadline=Time.realtimeSinceStartup+45;
            while(g==null||g.tour==null){g=FindAnyObjectByType<OdysseyGame>();Require(Time.realtimeSinceStartup<deadline,"Boot timeout");yield return null;}
            Application.runInBackground=true;var t=g.tour;g.life.SetPanel("");g.player.controls=false;
            Require(t.catalog.Valid(),"Catalog invalid");Require(t.catalog.Search("wesley").Length>=2,"Wesley search");
            foreach(var p in CampusCatalog.Places)Require(t.catalog.ForCampus(p.id)!=null,"Missing map record "+p.id);
            var wesley=t.catalog.ForCampus("50");Require(wesley.media.Length==3,"Wesley media coverage");Require(t.catalog.places.SelectMany(p=>p.media).All(m=>!string.IsNullOrWhiteSpace(m.caption)),"Media captions missing");
            t.Open(wesley);Require(g.sound.PendingAchievements==0,"Reading falsely awarded a lesson achievement");yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(Application.dataPath,"../../Directory-in-game.png"));
            t.Close();var before=g.player.transform.position;Require(t.EnterRoom(),"Room entry failed");g.player.controls=false;Physics.SyncTransforms();
            var origin=CampusTour.RoomOrigin;Require(Physics.Raycast(origin+new Vector3(0,1,0),Vector3.down,2),"No room floor");
            bool body=g.player.body.enabled;g.player.body.enabled=false;
            Require(!Physics.CheckCapsule(origin+new Vector3(0,.5f,-3.2f),origin+new Vector3(0,1.5f,-3.2f),.42f),"Doorway blocked");g.player.body.enabled=body;
            for(int i=0;i<80;i++){g.player.body.Move(new Vector3(0,-.05f,.05f));yield return null;}
            Require(g.player.transform.position.z>origin.z-.5f,"Could not walk through doorway");
            g.player.Teleport(origin+new Vector3(0,.08f,-1));
            for(int i=0;i<100;i++){g.player.body.Move(new Vector3(.05f,-.05f,0));yield return null;}
            Require(g.player.transform.position.x<origin.x+2.2f,"Passed through side wall");
            g.player.Teleport(origin+new Vector3(0,.05f,-2.5f));g.player.transform.rotation=Quaternion.identity;yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(Application.dataPath,"../../Wesley-in-game.png"));
            t.ExitRoom();g.player.controls=false;Require(Vector3.Distance(g.player.transform.position,before)<.1f,"Return location changed");
            t.Open(wesley);t.ShowMedia(wesley.media[0]);deadline=Time.realtimeSinceStartup+30;
            while(t.Status.StartsWith("Loading")){Require(Time.realtimeSinceStartup<deadline,"Photo timeout");yield return null;}
            Require(t.Status.StartsWith("Photo reference"),"Photo load failed: "+t.Status);
            t.ShowMedia(wesley.media[2]);deadline=Time.realtimeSinceStartup+30;
            while(t.Status.StartsWith("Loading")){Require(Time.realtimeSinceStartup<deadline,"Panorama timeout");yield return null;}
            Require(t.Status.StartsWith("360"),"Panorama load failed: "+t.Status);yield return new WaitForEndOfFrame();
            var robinson=t.catalog.ForCampus("16");t.Open(robinson);t.ShowMedia(robinson.media[0]);deadline=Time.realtimeSinceStartup+35;
            while(t.Status.StartsWith("Loading")){Require(Time.realtimeSinceStartup<deadline,"Video timeout");yield return null;}
            Require(t.Status.StartsWith("Video ·"),"Video failed: "+t.Status);yield return new WaitForSecondsRealtime(2);
            t.Open(wesley);t.ShowMedia(wesley.media[0]);t.ShowMedia(wesley.media[2]);t.Close();yield return null;Require(!t.IsOpen,"Close failed");
        }
    }
}
