using System;
using System.IO;
using System.Collections;
using System.Linq;
using UnityEngine;
namespace AlbionOdyssey
{
    public sealed class MitchellPlanSmoke:MonoBehaviour
    {
        OdysseyGame game;WalkableCampusBuilding hall;string output;int flights;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-mitchellPlanSmoke")>=0)new GameObject("Mitchell plan verification").AddComponent<MitchellPlanSmoke>();}
        void Require(bool okay,string what)
        {
            if(!okay){Debug.LogError("MITCHELL_FAILED "+what+" player="+game.player.transform.position);Application.Quit(1);throw new Exception(what);}
            Debug.Log("MITCHELL_CHECK "+what);
        }
        IEnumerator Move(Vector3 displacement)
        {
            int steps=Mathf.CeilToInt(displacement.magnitude/.04f);Vector3 step=displacement/steps;
            for(int i=0;i<steps;i++){game.player.body.Move(step+Vector3.down*.025f);if(i%8==0)yield return null;}
        }
        IEnumerator Start()
        {
            Application.runInBackground=true;QualitySettings.vSyncCount=0;Application.targetFrameRate=120;
            output=Environment.GetEnvironmentVariable("MITCHELL_OUTPUT")??Application.persistentDataPath+"/Playtests/mitchell";Directory.CreateDirectory(output);
            float deadline=Time.realtimeSinceStartup+90;
            while(game==null||!game.Ready||CampusBuildings.Instance==null){if(Time.realtimeSinceStartup>deadline){Debug.LogError("MITCHELL_BOOT_TIMEOUT");Application.Quit(1);yield break;}game=FindAnyObjectByType<OdysseyGame>();yield return null;}
            game.shell.Play();game.player.controls=false;game.weather.SetSnowForVerification(false);hall=CampusBuildings.Instance.Building("46");
            Require(hall.Plan.floors.Length==4,"Four published levels");
            Require(hall.Plan.floors.All(f=>f.spaces.Count(s=>s.kind=="stairs")==4),"Four stair cores on each level");
            Require(hall.UnreachablePlanSpaces.Count==0,"Room access: "+string.Join(", ",hall.UnreachablePlanSpaces));
            Require(Vector3.Dot(hall.Root.transform.forward,Vector3.right)>.99f,"Lobby faces west and Mingo faces east");
            hall.Visit();yield return Move(hall.Root.transform.forward*1.5f);
            Require(hall.Doors[0].Toggle(game.player),"Main door opens");yield return new WaitForSeconds(.6f);
            yield return Move(hall.Root.transform.forward*3.8f);Require(hall.Inside,"West lobby entry reaches first floor");
            for(int level=0;level<3;level++)foreach(var s in hall.Plan.floors[level].spaces.Where(s=>s.kind=="stairs"))
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
            Require(hall.Plan.floors[0].spaces.Count(s=>s.kind=="room")==25,"Twenty-five first-floor bedrooms in published diagram");
            Require(hall.Plan.floors.Skip(1).All(f=>f.spaces.Count(s=>s.kind=="room")==32),"Thirty-two bedrooms on each upper floor");
            game.player.Teleport(hall.Root.transform.TransformPoint(new Vector3(-16.7f,.08f,-8f)));yield return null;
            yield return Move(hall.Root.transform.TransformDirection(Vector3.right)*33.4f);
            Require(hall.Root.transform.InverseTransformPoint(game.player.transform.position).x>16,"First-floor lobby connects both towers");
            game.player.Teleport(hall.Root.transform.TransformPoint(new Vector3(0,hall.FloorHeight+.1f,0)));
            Require(!hall.Inside,"No invented upper-floor bridge");
            game.player.Teleport(hall.Root.transform.TransformPoint(new Vector3(-16.7f,hall.FloorHeight+.1f,0)));
            Require(hall.Inside&&hall.Location.EndsWith("Second floor"),"North tower floor lookup uses rotated plan");
            var room=hall.Plan.floors[1].spaces.First(s=>s.name=="209");
            var roomDoor=hall.Doors.First(d=>d.Label=="Second · 209");
            var doorAt=hall.Root.transform.InverseTransformPoint(roomDoor.transform.position);
            game.player.Teleport(hall.Root.transform.TransformPoint(doorAt+new Vector3(0,.08f,-.85f)));yield return null;
            Require(roomDoor.Toggle(game.player),"Room 209 door opens");yield return new WaitForSeconds(.6f);
            yield return Move(hall.Root.transform.TransformDirection(Vector3.forward)*1.8f);
            Require(hall.Root.transform.InverseTransformPoint(game.player.transform.position).z>doorAt.z+.6f,"Double-room furniture leaves entry clear");
            Require(hall.Root.GetComponentsInChildren<Transform>().Count(t=>t.name=="XL twin mattress · 209")==2,"Two XL twin beds in a standard double");
            game.player.Teleport(hall.Root.transform.TransformPoint(new Vector3(-16.7f,.1f,0)));yield return null;
            Capture("Mitchell-double-room.png",new Vector3(room.x,hall.FloorHeight+1.65f,room.z-room.depth/2+.4f),new Vector3(room.x,hall.FloorHeight+1.1f,room.z+room.depth/2));
            game.weather.SetSnowForVerification(true);yield return null;
            var snow=GameObject.Find("Campus snow blanket · visual only").GetComponentsInChildren<Renderer>();
            var firstRoom=hall.Plan.floors[0].spaces.First(s=>s.name=="109");
            var inside=hall.Root.transform.TransformPoint(new Vector3(firstRoom.x,.095f,firstRoom.z));
            var outside=hall.Root.transform.TransformPoint(new Vector3(0,.095f,3));
            Require(!snow.Any(r=>r.bounds.Contains(inside)),"Outdoor snow excludes first-floor bedrooms");
            Require(snow.Any(r=>r.bounds.Contains(outside)),"Open space between towers retains snow");
            game.weather.SetSnowForVerification(false);
            foreach(var label in FindObjectsByType<CampusWorldLabel>()){label.enabled=false;foreach(var t in label.GetComponentsInChildren<TextMesh>())t.gameObject.SetActive(false);}
            foreach(var light in FindObjectsByType<Light>())if(light.type==LightType.Directional){light.transform.rotation=Quaternion.Euler(42,-30,0);light.color=new Color(1,.94f,.85f);light.intensity=1.3f;}
            Capture("Mitchell-footprint.png",new Vector3(-52,43,-61),new Vector3(0,3,0));
            Capture("Mitchell-return-wing.png",new Vector3(48,28,44),new Vector3(10,4,1));
            Capture("Mitchell-lounge.png",new Vector3(-17,8.1f,-8),new Vector3(-17,8.1f,8));
            File.WriteAllText(Path.Combine(output,"result.json"),"{\"passed\":true,\"levels\":4,\"stairCores\":4,\"flightTraversals\":"+flights+",\"mainEntry\":true,\"towerConnection\":true,\"doubleRoomAccess\":true,\"interiorSnowClearance\":true,\"exactReplica\":false}");Debug.Log("MITCHELL_PLAN_SMOKE_OK");Application.Quit(0);
        }
        void Capture(string name,Vector3 at,Vector3 target)
        {
            var camera=new GameObject("Mitchell review camera").AddComponent<Camera>();camera.CopyFrom(game.player.eyes);camera.enabled=false;camera.fieldOfView=52;camera.aspect=1.6f;camera.farClipPlane=220;
            camera.transform.position=hall.Root.transform.TransformPoint(at);camera.transform.LookAt(hall.Root.transform.TransformPoint(target));
            var rt=new RenderTexture(1920,1200,24){antiAliasing=4};camera.targetTexture=rt;camera.Render();var prior=RenderTexture.active;RenderTexture.active=rt;
            var pixels=new Texture2D(1920,1200,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,1920,1200),0,0);pixels.Apply();File.WriteAllBytes(Path.Combine(output,name),pixels.EncodeToPNG());RenderTexture.active=prior;Destroy(camera.gameObject);Destroy(pixels);rt.Release();
        }
    }
}
