using System;
using System.IO;
using System.Collections;
using System.Linq;
using UnityEngine;
namespace AlbionOdyssey
{
    public sealed class SeatonPlanSmoke:MonoBehaviour
    {
        OdysseyGame game;WalkableCampusBuilding hall;string output;int flights;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-seatonPlanSmoke")>=0)new GameObject("Seaton plan verification").AddComponent<SeatonPlanSmoke>();}
        void Require(bool okay,string what)
        {
            if(!okay){Debug.LogError("SEATON_FAILED "+what+" player="+game.player.transform.position);Application.Quit(1);throw new Exception(what);}
            Debug.Log("SEATON_CHECK "+what);
        }
        IEnumerator Move(Vector3 displacement)
        {
            int steps=Mathf.CeilToInt(displacement.magnitude/.04f);Vector3 step=displacement/steps;
            for(int i=0;i<steps;i++){game.player.body.Move(step+Vector3.down*.025f);if(i%8==0)yield return null;}
        }
        IEnumerator Start()
        {
            Application.runInBackground=true;QualitySettings.vSyncCount=0;Application.targetFrameRate=120;
            output=Environment.GetEnvironmentVariable("SEATON_OUTPUT")??Application.persistentDataPath+"/Playtests/seaton";Directory.CreateDirectory(output);
            float deadline=Time.realtimeSinceStartup+90;
            while(game==null||!game.Ready||CampusBuildings.Instance==null){if(Time.realtimeSinceStartup>deadline){Debug.LogError("SEATON_BOOT_TIMEOUT");Application.Quit(1);yield break;}game=FindAnyObjectByType<OdysseyGame>();yield return null;}
            game.shell.Play();game.player.controls=false;game.weather.SetSnowForVerification(false);hall=CampusBuildings.Instance.Building("49");
            Require(hall.Plan.floors.Length==4,"Four published levels");
            Require(hall.Plan.floors.All(f=>f.spaces.Count(s=>s.kind=="stairs")==3),"Three stair cores on each level");
            Require(hall.UnreachablePlanSpaces.Count==0,"Room access: "+string.Join(", ",hall.UnreachablePlanSpaces));
            Require(Vector3.Dot(hall.Root.transform.forward,Vector3.back)>.99f,"Cass Street frontage faces north");
            Require(hall.Root.GetComponentsInChildren<Transform>().Count(t=>t.name=="Seaton portico column")==4,"Four photo-referenced portico supports");
            hall.Visit();yield return Move(hall.Root.transform.forward*3.6f);
            Require(Mathf.Abs(game.player.transform.position.y-(hall.Root.transform.position.y+hall.FloorHeight))<.3f,"Entrance steps reach first floor");
            Require(hall.Doors[0].Toggle(game.player),"Main door opens");yield return new WaitForSeconds(.6f);
            yield return Move(hall.Root.transform.forward*3.8f);Require(hall.Inside,"Cass Street entry reaches corridor");
            foreach(var s in hall.Plan.floors[0].spaces.Where(s=>s.kind=="stairs"))for(int level=0;level<3;level++)
            {
                bool sideways=Mathf.Abs(Mathf.Sin(s.stairYaw*Mathf.Deg2Rad))>.5f;float w=sideways?s.depth:s.width,d=sideways?s.width:s.depth,lane=(d-.25f)/2;
                Quaternion rotation=hall.Root.transform.rotation*Quaternion.Euler(0,s.stairYaw,0);
                Vector3 origin=hall.Root.transform.TransformPoint(new Vector3(s.x,level*hall.FloorHeight,s.z));
                game.player.Teleport(origin+rotation*new Vector3(-w/2+.55f,.05f,-d/2+lane/2));yield return null;
                yield return Move(rotation*Vector3.right*(w-1.1f));
                Require(Mathf.Abs(game.player.transform.position.y-(hall.Root.transform.position.y+(level+.5f)*hall.FloorHeight))<.3f,s.name+" lower "+level);flights++;
                yield return Move(rotation*Vector3.forward*(d-lane));
                yield return Move(rotation*Vector3.left*(w-1.1f));
                Require(Mathf.Abs(game.player.transform.position.y-(hall.Root.transform.position.y+(level+1)*hall.FloorHeight))<.3f,s.name+" upper "+level);flights++;
            }
            var doubleRoom=hall.Plan.floors[1].spaces.First(s=>s.name=="111");
            var doubleBeds=hall.Root.GetComponentsInChildren<Transform>().Where(t=>t.name=="XL twin mattress · 111").ToArray();
            Require(doubleBeds.Length==2,"Standard double 111 has two resident beds");
            Require(doubleBeds.All(t=>Mathf.Abs(t.localScale.x-.9652f)<.001f&&Mathf.Abs(t.localScale.z-2.032f)<.001f),"Published 38 by 80 inch mattresses");
            Require(hall.Root.GetComponentsInChildren<Transform>().Count(t=>t.name=="XL twin mattress · 43-S")==1,"Source-labelled single 43 has one bed");
            Require(hall.Root.GetComponentsInChildren<Transform>().Count(t=>t.name=="Three-drawer chest · 111")==2,"Double room has two dressers");
            var doubleDoor=hall.Doors.First(d=>d.Label=="First · 111");
            var centre=hall.Root.transform.TransformPoint(new Vector3(doubleRoom.x,hall.FloorHeight,doubleRoom.z));
            var approach=centre-doubleDoor.transform.position;approach.y=0;approach.Normalize();
            var start=doubleDoor.transform.position-approach*.8f;start.y=hall.Root.transform.position.y+hall.FloorHeight+.05f;
            game.player.Teleport(start);yield return null;Require(doubleDoor.Toggle(game.player),"Double-room door opens");yield return new WaitForSeconds(.6f);
            yield return Move(approach*1.6f);Require(Vector3.Dot(game.player.transform.position-doubleDoor.transform.position,approach)>.5f,"Double-room furnishing leaves doorway accessible");
            Capture("Seaton-double-room.png",new Vector3(doubleRoom.x,hall.FloorHeight+1.65f,doubleRoom.z-doubleRoom.depth/2+.5f),new Vector3(doubleRoom.x,hall.FloorHeight+1.1f,doubleRoom.z+.5f));
            // End rooms adjoin a stair: verify their doors open onto a level landing.
            var roomDoor=hall.Doors.First(d=>d.Label=="Ground · 43-S");
            var local=hall.Root.transform.InverseTransformPoint(roomDoor.transform.position);
            game.player.Teleport(hall.Root.transform.TransformPoint(local+new Vector3(.8f,.05f,0)));
            Require(roomDoor.Toggle(game.player),"Room 43 landing door opens");yield return new WaitForSeconds(.6f);
            yield return Move(hall.Root.transform.TransformDirection(Vector3.left)*1.6f);
            Require(hall.Root.transform.InverseTransformPoint(game.player.transform.position).x<local.x-.5f&&Mathf.Abs(game.player.transform.position.y-hall.Root.transform.position.y)<.3f,"End room reached from level stair landing");
            var room=hall.Plan.floors[0].spaces.First(s=>s.name=="43-S");
            var roomWorld=hall.Root.transform.TransformPoint(new Vector3(room.x,0,room.z));
            Require(Physics.Raycast(new Vector3(roomWorld.x,.5f,roomWorld.z),Vector3.down,out var floorHit,4f)&&Mathf.Abs(floorHit.point.y-hall.Root.transform.position.y)<.1f,"Ground-floor room has no terrain slab through it");
            game.weather.SetSnowForVerification(true);yield return null;
            Require(game.weather.SnowCoverVisible,"Snow blanket activates");
            var snow=GameObject.Find("Campus snow blanket · visual only");
            Require(!snow.GetComponentsInChildren<Renderer>().Any(r=>r.bounds.Contains(new Vector3(roomWorld.x,.095f,roomWorld.z))),"Snow surface excludes occupied lower floor");
            Capture("Seaton-ground-floor-snow.png",new Vector3(23,1.65f,11),new Vector3(20.5f,1.2f,12.5f));
            game.weather.SetSnowForVerification(false);
            game.player.Teleport(hall.Root.transform.TransformPoint(new Vector3(0,.05f-hall.Plan.baseElevation,8)));Require(!hall.Inside,"Open side of L is outdoors");
            foreach(var label in FindObjectsByType<CampusWorldLabel>()){label.enabled=false;foreach(var t in label.GetComponentsInChildren<TextMesh>())t.gameObject.SetActive(false);}
            foreach(var light in FindObjectsByType<Light>())if(light.type==LightType.Directional){light.transform.rotation=Quaternion.Euler(42,-30,0);light.color=new Color(1,.94f,.85f);light.intensity=1.3f;}
            Capture("Seaton-footprint.png",new Vector3(-52,43,-61),new Vector3(0,3,0));
            Capture("Seaton-return-wing.png",new Vector3(48,28,44),new Vector3(10,4,1));
            Capture("Seaton-corridor.png",new Vector3(-20,8.1f,-9.3f),new Vector3(20,8.1f,-9.3f));
            File.WriteAllText(Path.Combine(output,"result.json"),"{\"passed\":true,\"levels\":4,\"stairCores\":3,\"flightTraversals\":"+flights+",\"mainEntry\":true,\"endRoomAccess\":true,\"doubleRoomAccess\":true,\"residentFurniture\":true,\"terrainClearance\":true,\"snowClearance\":true,\"exactReplica\":false}");Debug.Log("SEATON_PLAN_SMOKE_OK");Application.Quit(0);
        }
        void Capture(string name,Vector3 at,Vector3 target)
        {
            var camera=new GameObject("Seaton review camera").AddComponent<Camera>();camera.CopyFrom(game.player.eyes);camera.enabled=false;camera.fieldOfView=52;camera.aspect=1.6f;camera.farClipPlane=220;
            camera.transform.position=hall.Root.transform.TransformPoint(at);camera.transform.LookAt(hall.Root.transform.TransformPoint(target));
            var rt=new RenderTexture(1920,1200,24){antiAliasing=4};camera.targetTexture=rt;camera.Render();var prior=RenderTexture.active;RenderTexture.active=rt;
            var pixels=new Texture2D(1920,1200,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,1920,1200),0,0);pixels.Apply();File.WriteAllBytes(Path.Combine(output,name),pixels.EncodeToPNG());RenderTexture.active=prior;Destroy(camera.gameObject);Destroy(pixels);rt.Release();
        }
    }
}
