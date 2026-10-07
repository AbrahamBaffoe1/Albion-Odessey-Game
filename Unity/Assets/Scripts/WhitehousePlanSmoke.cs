using System;
using System.IO;
using System.Collections;
using System.Linq;
using UnityEngine;
namespace AlbionOdyssey
{
    public sealed class WhitehousePlanSmoke:MonoBehaviour
    {
        OdysseyGame game;WalkableCampusBuilding hall;string output;int flights;float maxDimensionError;int measuredRooms;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-whitehousePlanSmoke")>=0)new GameObject("Whitehouse plan verification").AddComponent<WhitehousePlanSmoke>();}
        void Require(bool okay,string what)
        {
            if(!okay){Debug.LogError("WHITEHOUSE_FAILED "+what+" player="+game.player.transform.position);Application.Quit(1);throw new Exception(what);}
            Debug.Log("WHITEHOUSE_CHECK "+what);
        }
        IEnumerator Move(Vector3 displacement)
        {
            int steps=Mathf.CeilToInt(displacement.magnitude/.04f);Vector3 step=displacement/steps;
            for(int i=0;i<steps;i++){game.player.body.Move(step+Vector3.down*.025f);if(i%8==0)yield return null;}
        }
        float WallDistance(Vector3 origin,Vector3 direction,float limit)
        {
            var hits=Physics.RaycastAll(origin,direction,limit,~0,QueryTriggerInteraction.Ignore)
                .Where(h=>h.collider.transform.IsChildOf(hall.Root.transform)&&(h.collider.name=="Whitehouse Hall brick wall"||h.collider.name=="Whitehouse Hall partition"||h.collider.name=="Whitehouse Hall interior wall finish"));
            return hits.Any()?hits.Min(h=>h.distance):float.NaN;
        }
        void CheckPublishedDimensions()
        {
            foreach(var level in hall.Plan.floors)foreach(var room in level.spaces.Where(s=>s.dimensionSourcePage==5))
            {
                float y=level.level*hall.FloorHeight+.4f;
                var across=hall.Root.transform.TransformPoint(new Vector3(room.x,y,room.z+room.depth*.25f));
                var along=hall.Root.transform.TransformPoint(new Vector3(room.x+room.width*.25f,y,room.z));
                float width=WallDistance(across,Vector3.right,room.width)+WallDistance(across,Vector3.left,room.width);
                float depth=WallDistance(along,Vector3.forward,room.depth)+WallDistance(along,Vector3.back,room.depth);
                float error=Mathf.Max(Mathf.Abs(width-room.clearWidth),Mathf.Abs(depth-room.clearDepth));
                Require(!float.IsNaN(error)&&error<.003f,"Published clear walls "+room.name+" error="+error.ToString("F5"));
                maxDimensionError=Mathf.Max(maxDimensionError,error);measuredRooms++;
            }
            Require(measuredRooms==106,"All 106 published room measurements checked against physical walls");
        }
        IEnumerator Start()
        {
            Application.runInBackground=true;QualitySettings.vSyncCount=0;Application.targetFrameRate=120;
            output=Environment.GetEnvironmentVariable("WHITEHOUSE_OUTPUT")??Application.persistentDataPath+"/Playtests/whitehouse";Directory.CreateDirectory(output);
            float deadline=Time.realtimeSinceStartup+90;
            while(game==null||!game.Ready||CampusBuildings.Instance==null){if(Time.realtimeSinceStartup>deadline){Debug.LogError("WHITEHOUSE_BOOT_TIMEOUT");Application.Quit(1);yield break;}game=FindAnyObjectByType<OdysseyGame>();yield return null;}
            game.shell.Play();game.player.controls=false;game.weather.SetSnowForVerification(false);hall=CampusBuildings.Instance.Building("51");
            Require(hall.Plan.floors.Length==4,"Four published levels");
            Require(hall.Plan.floors.All(f=>f.spaces.Count(s=>s.kind=="stairs")==3),"Three stair cores on each level");
            Require(hall.UnreachablePlanSpaces.Count==0,"Room access: "+string.Join(", ",hall.UnreachablePlanSpaces));
            Require(Vector3.Dot(hall.Root.transform.forward,Vector3.forward)>.99f,"Porter Street frontage faces south");
            CheckPublishedDimensions();
            hall.Visit();yield return Move(hall.Root.transform.forward*1.5f);
            Require(hall.Doors[0].Toggle(game.player),"Main door opens");yield return new WaitForSeconds(.6f);
            yield return Move(hall.Root.transform.forward*3.8f);Require(hall.Inside,"Porter Street entry reaches ground floor");
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
            Require(hall.Plan.floors[0].spaces.Count(s=>s.kind=="room")==20,"Twenty published ground-floor bedrooms");
            Require(hall.Plan.floors[2].spaces.Any(s=>s.name=="200-S"),"Second-floor single retained");
            Require(hall.Plan.floors[3].spaces.Any(s=>s.kind=="void"),"Third-floor blocked area is not invented as a room");
            foreach(var label in FindObjectsByType<CampusWorldLabel>()){label.enabled=false;foreach(var t in label.GetComponentsInChildren<TextMesh>())t.gameObject.SetActive(false);}
            foreach(var light in FindObjectsByType<Light>())if(light.type==LightType.Directional){light.transform.rotation=Quaternion.Euler(42,-30,0);light.color=new Color(1,.94f,.85f);light.intensity=1.3f;}
            Capture("Whitehouse-footprint.png",new Vector3(-52,43,-61),new Vector3(0,3,0));
            Capture("Whitehouse-return-wing.png",new Vector3(48,28,44),new Vector3(10,4,1));
            Capture("Whitehouse-corridor.png",new Vector3(-20,8.1f,-8.5f),new Vector3(8,8.1f,-8.5f));
            File.WriteAllText(Path.Combine(output,"result.json"),"{\"passed\":true,\"levels\":4,\"stairCores\":3,\"flightTraversals\":"+flights+",\"mainEntry\":true,\"measuredRooms\":"+measuredRooms+",\"maxClearanceErrorMetres\":"+maxDimensionError.ToString("F5",System.Globalization.CultureInfo.InvariantCulture)+",\"exactReplica\":false}");Debug.Log("WHITEHOUSE_PLAN_SMOKE_OK");Application.Quit(0);
        }
        void Capture(string name,Vector3 at,Vector3 target)
        {
            var camera=new GameObject("Whitehouse review camera").AddComponent<Camera>();camera.CopyFrom(game.player.eyes);camera.enabled=false;camera.fieldOfView=52;camera.aspect=1.6f;camera.farClipPlane=220;
            camera.transform.position=hall.Root.transform.TransformPoint(at);camera.transform.LookAt(hall.Root.transform.TransformPoint(target));
            var rt=new RenderTexture(1920,1200,24){antiAliasing=4};camera.targetTexture=rt;camera.Render();var prior=RenderTexture.active;RenderTexture.active=rt;
            var pixels=new Texture2D(1920,1200,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,1920,1200),0,0);pixels.Apply();File.WriteAllBytes(Path.Combine(output,name),pixels.EncodeToPNG());RenderTexture.active=prior;Destroy(camera.gameObject);Destroy(pixels);rt.Release();
        }
    }
}
