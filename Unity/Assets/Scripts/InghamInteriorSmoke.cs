using System;
using System.IO;
using System.Linq;
using System.Collections;
using UnityEngine;
namespace AlbionOdyssey
{
    public sealed class InghamInteriorSmoke:MonoBehaviour
    {
        OdysseyGame game;WalkableCampusBuilding hall;string output;int rooms,doors;float maxError;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-inghamInteriorSmoke")>=0)new GameObject("Ingham staged interior verification").AddComponent<InghamInteriorSmoke>();}
        void Require(bool ok,string message){if(!ok){Debug.LogError("INGHAM_INTERIOR_FAILED "+message);Application.Quit(1);throw new Exception(message);}Debug.Log("INGHAM_CHECK "+message);}
        IEnumerator Move(Vector3 v){int n=Mathf.CeilToInt(v.magnitude/.035f);for(int i=0;i<n;i++){game.player.body.Move(v/n+Vector3.down*.025f);if(i%8==0)yield return null;}}
        float Distance(Vector3 from,Vector3 direction,float limit)
        {
            var hits=Physics.RaycastAll(from,direction,limit,~0,QueryTriggerInteraction.Ignore).Where(h=>h.collider.transform.IsChildOf(hall.Root.transform)&&(h.collider.name==hall.Place.name+" brick wall"||h.collider.name==hall.Place.name+" partition"));
            return hits.Any()?hits.Min(h=>h.distance):float.NaN;
        }
        float Dimension(WesleySpace room,int level,bool width)
        {
            foreach(float offset in new[]{0f,.2f,-.2f,.35f,-.35f})
            {
                var p=new Vector3(room.x+(width?0:room.width*offset),level*hall.FloorHeight+.4f,room.z+(width?room.depth*offset:0));
                var direction=hall.Root.transform.TransformDirection(width?Vector3.right:Vector3.forward);
                var origin=hall.Root.transform.TransformPoint(p);float limit=width?room.width:room.depth;
                float result=Distance(origin,direction,limit)+Distance(origin,-direction,limit);
                if(!float.IsNaN(result))return result;
            }
            return float.NaN;
        }
        IEnumerator Start()
        {
            Application.runInBackground=true;QualitySettings.vSyncCount=0;Application.targetFrameRate=120;
            float deadline=Time.realtimeSinceStartup+90;
            while(game==null||!game.Ready){if(Time.realtimeSinceStartup>deadline){Application.Quit(1);yield break;}game=FindAnyObjectByType<OdysseyGame>();yield return null;}
            output=Environment.GetEnvironmentVariable("INGHAM_OUTPUT");Directory.CreateDirectory(output);
            var plan=JsonUtility.FromJson<WesleyPlan>(File.ReadAllText(Environment.GetEnvironmentVariable("INGHAM_PLAN")));
            plan.floorHeight=3.2f;plan.entryLevel=0;plan.baseElevation=0;
            var place=new CampusPlace("ingham-stage","Ingham staged interior","Residential","house",0,0,1,1,7);place.position=new Vector3(4000,100,4000);
            hall=new WalkableCampusBuilding(game,place,plan.width,plan.depth,plan.floors.Length,plan.floorHeight,false,"Unfinished source-based verification geometry",plan);
            game.shell.Play();game.player.controls=false;Physics.SyncTransforms();yield return null;
            Require(hall.Plan.floors.Length==2,"Two supplied floors only");Require(hall.UnreachablePlanSpaces.Count==0,"All enclosed spaces have doors");
            foreach(var level in plan.floors)
            {
                foreach(var room in level.spaces.Where(s=>s.kind=="room"))
                {
                    float w=Dimension(room,level.level,true),d=Dimension(room,level.level,false);
                    float error=Mathf.Max(Mathf.Abs(w-room.clearWidth),Mathf.Abs(d-room.clearDepth));
                    Require(!float.IsNaN(error)&&error<.003f,"Physical dimensions "+room.name+" error="+error);maxError=Mathf.Max(maxError,error);rooms++;
                }
                foreach(var opening in level.doors)
                {
                    var door=hall.Doors.First(d=>d.Label==level.name+" · "+opening.space);
                    var outward=opening.side=="north"?Vector3.back:opening.side=="south"?Vector3.forward:opening.side=="west"?Vector3.left:Vector3.right;
                    var normal=hall.Root.transform.TransformDirection(outward);
                    var start=hall.Root.transform.TransformPoint(new Vector3(opening.x,level.level*hall.FloorHeight+.04f,opening.z))+normal*.6f;
                    game.player.Teleport(start);yield return null;yield return Move(-normal*1.2f);
                    Require(Vector3.Dot(game.player.transform.position-door.transform.position,normal)>.1f,"Closed door blocks "+opening.space);
                    game.player.Teleport(start);yield return null;Require(door.Toggle(game.player),"Open "+opening.space);yield return new WaitForSeconds(.5f);
                    yield return Move(-normal*1.3f);
                    Require(Vector3.Dot(game.player.transform.position-door.transform.position,normal)<-.35f,"Cross doorway "+opening.space);doors++;
                }
            }
            Require(rooms==8&&doors==12,"Eight rooms measured and twelve doors traversed");
            var roofOrigin=hall.Root.transform.TransformPoint(new Vector3(0,8,0));
            Require(Physics.Raycast(roofOrigin,Vector3.down,out var roof,3)&&Mathf.Abs(roof.point.y-(100+6.4f))<.2f,"Roof closes second level");
            Require(hall.Root.GetComponentsInChildren<Transform>().Count(t=>t.name=="Ingham living room front window")==3,"Three photo-referenced front windows");
            Require(hall.Root.GetComponentsInChildren<Transform>().Any(t=>t.name=="Ingham photo-referenced fireplace"),"Photo-referenced living room fireplace");
            Capture();
            File.WriteAllText(Path.Combine(output,"result.json"),"{\"passed\":true,\"measuredRooms\":8,\"doorTraversals\":12,\"closedDoorChecks\":12,\"maxDimensionErrorMetres\":"+maxError.ToString("F5",System.Globalization.CultureInfo.InvariantCulture)+",\"stairsVerified\":false,\"liveInteriorInstalled\":false,\"exactReplica\":false}");
            Debug.Log("INGHAM_INTERIOR_OK");Application.Quit(0);
        }
        void Capture()
        {
            var camera=new GameObject("Ingham staged hallway camera").AddComponent<Camera>();camera.CopyFrom(game.player.eyes);camera.enabled=false;camera.fieldOfView=65;camera.aspect=1.6f;
            camera.transform.position=hall.Root.transform.TransformPoint(new Vector3(-2.4f,1.65f,1.4f));camera.transform.LookAt(hall.Root.transform.TransformPoint(new Vector3(-5.3f,1.35f,-3.0f)));
            var rt=new RenderTexture(1600,1000,24){antiAliasing=4};camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
            var pixels=new Texture2D(1600,1000,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,1600,1000),0,0);pixels.Apply();File.WriteAllBytes(Path.Combine(output,"Ingham-staged-living-room.png"),pixels.EncodeToPNG());
        }
    }
}
