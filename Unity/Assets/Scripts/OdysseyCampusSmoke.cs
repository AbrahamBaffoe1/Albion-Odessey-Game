using System.IO;
using System.Collections;
using UnityEngine;
namespace AlbionOdyssey
{
    public sealed partial class OdysseySmoke
    {
        IEnumerator CampusChecks()
        {
            var c=game.campus;var p=game.player;var a=game.world==null?null:game.world.activities;game.life.SetPanel("");p.controls=false;p.thirdPerson=false;
            if(c==null||CampusCatalog.Places.Length!=61||c.cars.Count!=3||c.parking==null||c.parking.StallCount<36||c.parking.ParkedCarCount<20||a==null||a.ActivitySpaces<33||a.ActivityAgents<31||a.InteractionStations<33||a.ActiveClubCount<11||a.ClubAgents<22){Fail("Campus destinations, parking, activities or clubs missing");yield break;}
            result.parkingStalls=c.parking.StallCount;result.parkedCars=c.parking.ParkedCarCount;
            result.activitySpaces=a.ActivitySpaces;result.activityAgents=a.ActivityAgents;result.activityStations=a.InteractionStations;result.activeClubs=a.ActiveClubCount;result.clubAgents=a.ClubAgents;
            var firstStation=FindAnyObjectByType<CampusActivityStation>();if(firstStation==null){Fail("Activity interaction stations missing");yield break;}firstStation.Activate(game);result.activityInteraction=firstStation.Uses==1&&a.CompletedInteractions==1;
            int walkable=0;foreach(var place in CampusCatalog.Places)if(CampusBuildings.Instance.Building(place.id)!=null)walkable++;
            if(walkable<11){Fail("Walkable building chapter incomplete: "+walkable);yield break;}result.walkableBuildings=walkable;
            foreach(var place in CampusCatalog.Places)
            {
                bool science=place.id=="18k"||place.id=="18n"||place.id=="18p"||place.id=="18u";
                bool hasWalkable=CampusBuildings.Instance!=null&&CampusBuildings.Instance.Building(place.id)!=null;
                if((place.id=="1"||place.id=="16") ? CampusBuildings.Instance.Building(place.id)==null : science ? CampusBuildings.Instance.Building("18")==null : !hasWalkable&&GameObject.Find(place.id+" · "+place.name)==null){Fail("Missing model "+place.name);yield break;}
                if(!c.Travel(place)){Fail("Travel failed "+place.name);yield break;}p.controls=false;
                yield return null;
                Vector3 at=p.transform.position;
                p.body.enabled=false;
                bool blocked=Physics.CheckCapsule(at+Vector3.up*.5f,at+Vector3.up*1.5f,.42f,~0,QueryTriggerInteraction.Ignore);
                bool ground=Physics.Raycast(at+Vector3.up,Vector3.down,2);
                p.body.enabled=true;
                if(blocked||!ground){Fail("Unsafe campus arrival "+place.id+" "+place.name+" "+at+" blocked="+blocked+" ground="+ground);yield break;}
                result.campusDestinations++;
            }
            c.Travel(CampusExpansion.Find("5"));p.controls=false;
            yield return Capture("13-campus-overview",new Vector3(-155,155,258),CampusCatalog.Point(423,170));
            p.Teleport(CampusCatalog.Point(405,213)+Vector3.up*.08f);p.transform.rotation=Quaternion.Euler(0,0,0);p.thirdPerson=true;p.UpdateCamera();
            yield return null;
            if(!p.avatar.gameObject.activeSelf||Vector3.Distance(p.eyes.transform.position,p.transform.position)<2){Fail("Third-person avatar or camera missing");yield break;}
            game.life.SetPanel("settings");yield return Screen("14-character-settings");
            if(p.controls){Fail("Character settings leaked movement");yield break;}
            c.skin=4;c.outfit=2;c.hair=1;c.backpack=false;c.keeperName="Campus Test";c.RefreshAvatar();c.SaveKeeper();
            c.skin=0;c.outfit=0;c.LoadKeeper();
            if(c.skin!=4||c.outfit!=2||c.hair!=1||c.backpack||c.keeperName!="Campus Test"){Fail("Character save roundtrip failed");yield break;}
            game.state.active=1;c.LoadKeeper();c.outfit=3;c.SaveKeeper();game.state.active=0;c.LoadKeeper();
            if(c.outfit!=2){Fail("Character profile isolation failed");yield break;}
            p.thirdPerson=true;game.life.SetPanel("");p.controls=false;p.UpdateCamera();yield return Screen("15-third-person");
            p.Teleport(new Vector3(5,.08f,-18));p.UpdateCamera();p.eyes.transform.LookAt(new Vector3(5,1.2f,-15));
            if(!p.TryTarget(out var guide)||guide.collider.GetComponent<GuideMarker>()==null){Fail("Third-person interaction ray hit own body or missed guide");yield break;}
            p.Teleport(CampusCatalog.Point(405,213)+Vector3.up*.08f);p.UpdateCamera();
            Vector3 pivot=p.transform.position+Vector3.up*1.35f;float clearDistance=Vector3.Distance(p.eyes.transform.position,pivot);
            var cameraWall=GameObject.CreatePrimitive(PrimitiveType.Cube);cameraWall.transform.position=Vector3.Lerp(pivot,p.eyes.transform.position,.5f);cameraWall.transform.localScale=Vector3.one;Physics.SyncTransforms();p.UpdateCamera();
            if(Vector3.Distance(p.eyes.transform.position,pivot)>clearDistance-1){Fail("Camera failed to pull in before a wall");yield break;}
            Destroy(cameraWall);yield return null;
            result.character=true;
            // Test an actual car along a clear street, then against a physical barrier.
            var car=c.cars[0];car.transform.SetPositionAndRotation(CampusCatalog.Point(409,241),Quaternion.Euler(0,90,0));
            p.Teleport(car.transform.position+Vector3.back*3);p.controls=false;Physics.SyncTransforms();
            if(!car.Enter(p)||p.body.enabled||p.vehicle!=car){Fail("Car entry failed");yield break;}
            var before=car.transform.position;
            game.life.SetPanel("welcome");car.speed=4;yield return null;yield return null;
            if(car.transform.position!=before||p.controls){Fail("Menu did not pause driving");yield break;}
            game.life.SetPanel("");p.controls=false;car.speed=0;
            for(int i=0;i<60;i++){car.Drive(1,0,false,1f/60);if(i%10==0)yield return null;}
            if(Vector3.Distance(before,car.transform.position)<2||car.speed<1){Fail("Car throttle did not move vehicle");yield break;}
            if(car.Exit()){Fail("Car allowed moving exit");yield break;}
            for(int i=0;i<60;i++)car.Drive(0,0,true,1f/60);
            if(Mathf.Abs(car.speed)>.01f){Fail("Car brakes failed");yield break;}
            before=car.transform.position;for(int i=0;i<50;i++)car.Drive(-1,0,false,1f/60);
            if(Vector3.Dot(car.transform.position-before,car.transform.forward)>=-1){Fail("Car reverse failed");yield break;}
            car.speed=0;var barrier=GameObject.CreatePrimitive(PrimitiveType.Cube);barrier.name="Smoke vehicle collision barrier";
            barrier.transform.position=car.transform.position+car.transform.forward*7+Vector3.up*1.5f;barrier.transform.localScale=new Vector3(1,3,10);Physics.SyncTransforms();before=car.transform.position;
            for(int i=0;i<180;i++)car.Drive(1,0,false,1f/60);
            if(Vector3.Distance(before,car.transform.position)>5||car.speed>.01f){Fail("Car passed through collision barrier");yield break;}
            Destroy(barrier);yield return null;car.speed=0;p.thirdPerson=true;p.UpdateCamera();yield return Screen("16-driving");
            if(!car.Exit()||!p.body.enabled||p.vehicle!=null){Fail("Car exit failed");yield break;}
            result.driving=true;
            c.found=0;
            for(int i=0;i<c.discoveries.Count;i++)
            {
                p.Teleport(c.discoveries[i]+Vector3.back*3+Vector3.up*.08f);p.controls=false;
                if(!c.Discover(i)){Fail("Campus discovery unreachable "+i);yield break;}
                game.life.SetPanel("");p.controls=false;
            }
            if(c.CountFound()!=7){Fail("Campus discovery count failed");yield break;}c.LoadKeeper();
            if(c.CountFound()!=7){Fail("Discoveries did not persist");yield break;}
            c.Discover(6);yield return Screen("17-discovery-journal");game.life.SetPanel("campus");yield return Screen("18-campus-map");
            result.campusDiscoveries=true;
            game.life.SetPanel("welcome");yield return Screen("19-welcome");
            var club=CampusExpansion.Find("61");if(club!=null)yield return Capture("20-active-club",club.position+new Vector3(0,1.7f,-3.2f),club.position+new Vector3(0,1.2f,1.0f));
        }
        IEnumerator Screen(string name)
        {
            yield return new WaitForEndOfFrame();
            var texture=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(Output,name+".png"),texture.EncodeToPNG());Destroy(texture);
        }
    }
}
