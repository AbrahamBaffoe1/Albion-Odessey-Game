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
            foreach(var track in new[]{"WhenTheFutureWhispers","TheBeautyOfYourSoul"}){var music=Resources.Load<AudioClip>("Audio/Music/"+track);if(music==null||music.length<190||music.channels!=2)throw new Exception("Soundtrack import failed: "+track);}
            if(OdysseySoundscape.Phase("arrival")!="beauty"||OdysseySoundscape.Phase("telescope")!="future"||OdysseySoundscape.Phase("forestrun")!="forestrun")throw new Exception("Music phase routing failed");
            if(!OdysseyPortraitStudio.Blocker(true,false,true,false,"",true,true).Contains("not been activated"))throw new Exception("Portrait availability feedback regression");
            if(!OdysseyPortraitStudio.Blocker(true,false,true,true,"",true,false).Contains("consent"))throw new Exception("Portrait consent feedback regression");
            if(!OdysseyPortraitStudio.Blocker(true,false,false,false,"Connection failed",true,true).Contains("retry"))throw new Exception("Portrait retry feedback regression");
            yield return new WaitForSecondsRealtime(1);
            g.shell.ShowLaunch();yield return Capture("01-Console-home");
            yield return new WaitForSecondsRealtime(2.5f);yield return Capture("01b-Console-motion");
            yield return new WaitForSecondsRealtime(.5f);yield return Capture("01c-Coin-impact");
            Set(g.shell,"menuFocus",14);yield return Capture("01d-Menu-selection");
            Set(g.accounts,"refreshAt",float.MaxValue);Set(g.accounts,"session",new StudentAuthSession{access_token="visual-fixture",user=new StudentAuthUser{id="visual-fixture",email="player@example.test"}});
            g.life.SetPanel("portrait");Set(g.portraits,"serviceChecked",true);Set(g.portraits,"enabledGeneration",false);Set(g.portraits,"status","Photo selected. Nothing has been uploaded yet.");
            yield return Capture("01e-Portrait-unavailable");Set(g.accounts,"session",null);
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
            g.environment.SetHour(19);
            g.player.transform.position=AlbionCity.ToWorld(0,1,48);g.player.transform.rotation=Quaternion.Euler(0,0,0);
            yield return Capture("11-Downtown-dusk");
            g.player.transform.position=AlbionCity.ToWorld(5,1,40);g.player.transform.rotation=Quaternion.Euler(0,90,0);
            yield return Capture("12-Downtown-shops");
            g.city.Travel();
            g.tour.OpenStory(g.tour.catalog.places[0]);yield return Capture("13-Campus-field-notes");Set(g.tour,"readingTranscript",true);yield return Capture("13b-Campus-read-overlay");g.tour.Close();
            g.life.SetPanel("treasures");Set(g.campus,"treasureMap",true);yield return Capture("14-Treasure-map");
            g.shared.Social.Accept(new RoomSocialState{messages=new[]{new RoomChat{seq=1,id="preview",display="Campus friend",text="Meet me by the library. We can look for the next hidden treasure together."},new RoomChat{seq=2,id="preview",display="Campus friend",text="I parked nearby. There is a seat for you when you arrive."}},cars=Array.Empty<RoomCar>()});
            Set(g.shared,"<Joined>k__BackingField",true);Set(g.shared,"<SocialSupported>k__BackingField",true);Set(g.shared.Social,"draft","See you by the library!");g.life.SetPanel("conversation");yield return Capture("15-Room-conversation");Set(g.shared.Social,"rateAvailable",true);yield return Capture("16-Ride-rating");Set(g.accounts,"session",new StudentAuthSession{access_token="visual-fixture",user=new StudentAuthUser{id="visual-fixture",email="player@example.test"}});
            var car=g.campus.cars[0];var cp=car.transform.position;
            var sharedCar=new RoomCar{car=0,driver="fixture-driver",x=cp.x,y=cp.y,z=cp.z,yaw=0,speed=0,seats=new[]{new RoomSeat{id="visual-fixture",driver="fixture-driver",car=0,seat=1}}};
            g.shared.Social.Accept(new RoomSocialState{cars=new[]{sharedCar}});yield return null;
            if(!g.shared.Social.Passenger||g.player.vehicle!=car||g.player.body.enabled)throw new Exception("Passenger seat assignment regression");
            float speed=car.speed;car.Drive(1,1,false,.05f);if(car.speed!=speed)throw new Exception("Passenger steering authority regression");
            sharedCar.seats=Array.Empty<RoomSeat>();g.shared.Social.Accept(new RoomSocialState{cars=new[]{sharedCar}});
            if(g.player.vehicle!=null||!g.player.body.enabled)throw new Exception("Passenger exit regression");
            sharedCar.seats=new[]{new RoomSeat{id="visual-fixture",driver="fixture-driver",car=0,seat=2}};g.shared.Social.Accept(new RoomSocialState{cars=new[]{sharedCar}});g.shared.Social.ResetRoom();
            if(g.player.vehicle!=null||!g.player.body.enabled)throw new Exception("Room disconnect recovery regression");
            Set(g.shared,"<Joined>k__BackingField",false);Set(g.accounts,"session",null);
            g.shell.Play();if(!g.player.controls)throw new Exception("Return controls regression");
            File.WriteAllText(Path.Combine(output,"checks.txt"),"PASS: passenger seat assignment, passenger cannot steer, safe passenger exit, disconnect recovery, invalid-code feedback, account and online menu routing, movement restored. Screenshots use a fake email fixture; live code verification not tested.");
            Debug.Log("CONSOLE_UI_SMOKE_OK");Application.Quit(0);
        }
    }
}
