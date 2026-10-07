using System;
using System.IO;
using System.Linq;
using System.Collections;
using UnityEngine;
namespace AlbionOdyssey
{
    public sealed class SeatonMetricSmoke:MonoBehaviour
    {
        OdysseyGame game;WalkableCampusBuilding hall;string output;int rooms,doors;float maxError;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-seatonMetricSmoke")>=0)new GameObject("Seaton metric stage verification").AddComponent<SeatonMetricSmoke>();}
        void Require(bool ok,string message){if(!ok){Debug.LogError("SEATON_METRIC_INTERIOR_FAILED "+message);Application.Quit(1);throw new Exception(message);}Debug.Log("SEATON_METRIC_CHECK "+message);}
        IEnumerator Move(Vector3 v){int n=Mathf.CeilToInt(v.magnitude/.035f);for(int i=0;i<n;i++){game.player.body.Move(v/n+Vector3.down*.025f);if(i%8==0)yield return null;}}
        float Distance(Vector3 from,Vector3 direction,float limit)
        {
            var hits=Physics.RaycastAll(from,direction,limit,~0,QueryTriggerInteraction.Ignore).Where(h=>h.collider.transform.IsChildOf(hall.Root.transform)&&(h.collider.name==hall.Place.name+" brick wall"||h.collider.name==hall.Place.name+" partition"));
            return hits.Any()?hits.Min(h=>h.distance):float.NaN;
        }
        float Dimension(WesleySpace room,int level,bool width)
        {
            foreach(float offset in new[]{.35f,-.35f,.2f,-.2f,0f})
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
            output=Environment.GetEnvironmentVariable("SEATON_METRIC_OUTPUT");Directory.CreateDirectory(output);
            var plan=JsonUtility.FromJson<WesleyPlan>(File.ReadAllText(Environment.GetEnvironmentVariable("SEATON_METRIC_PLAN")));
            plan.floorHeight=3.2f;plan.entryLevel=0;plan.baseElevation=0;
            var place=new CampusPlace("seaton-metric-stage","Seaton metric stage","Residential","house",0,0,1,1,7);place.position=new Vector3(4000,100,4000);
            hall=new WalkableCampusBuilding(game,place,plan.width,plan.depth,plan.floors.Length,plan.floorHeight,false,"Unfinished source-based verification geometry",plan);
            game.shell.Play();game.player.controls=false;Physics.SyncTransforms();yield return null;
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-seatonMetricStairsOnly")>=0)
            {
                yield return CheckStairs();Application.Quit(0);yield break;
            }
            Require(hall.Plan.floors.Length==4,"Four supplied floors");
            foreach(var level in plan.floors)
            {
                foreach(var room in level.spaces.Where(s=>s.kind=="room"&&s.clearWidth>0))
                {
                    float w=Dimension(room,level.level,true),d=Dimension(room,level.level,false);
                    float error=Mathf.Max(Mathf.Abs(w-room.clearWidth),Mathf.Abs(d-room.clearDepth));
                    Require(!float.IsNaN(error)&&error<.003f,"Physical dimensions "+room.name+" error="+error);maxError=Mathf.Max(maxError,error);rooms++;
                    var door=hall.Doors.FirstOrDefault(v=>v.Label==level.name+" · "+room.name);
                    Require(door!=null,"Door exists "+room.name);
                    var local=hall.Root.transform.InverseTransformPoint(door.transform.position);
                    var delta=new Vector3((local.x-room.x)/room.width,0,(local.z-room.z)/room.depth);
                    var outward=Mathf.Abs(delta.x)>Mathf.Abs(delta.z)?new Vector3(Mathf.Sign(delta.x),0,0):new Vector3(0,0,Mathf.Sign(delta.z));
                    var normal=hall.Root.transform.TransformDirection(outward);
                    var start=hall.Root.transform.TransformPoint(new Vector3(local.x,level.level*hall.FloorHeight+.04f,local.z))+normal*.65f;
                    game.player.Teleport(start);yield return null;yield return Move(-normal*1.3f);
                    Require(Vector3.Dot(game.player.transform.position-door.transform.position,normal)>.1f,"Closed door blocks "+room.name);
                    game.player.Teleport(start);yield return null;Require(door.Toggle(game.player),"Open "+room.name);yield return new WaitForSeconds(.5f);
                    yield return Move(-normal*1.35f);
                    Require(Vector3.Dot(game.player.transform.position-door.transform.position,normal)<-.35f,"Cross doorway "+room.name);
                    Require(Mathf.Abs(hall.Root.transform.InverseTransformPoint(game.player.transform.position).y-level.level*hall.FloorHeight)<.2f,"Door route stays on floor "+room.name);doors++;
                }
            }
            Require(rooms==110&&doors==110,"110 rooms measured and doors traversed");
            File.WriteAllText(Path.Combine(output,"result.json"),"{\"passed\":true,\"measuredRooms\":110,\"doorTraversals\":110,\"closedDoorChecks\":110,\"maxDimensionErrorMetres\":"+maxError.ToString("F5",System.Globalization.CultureInfo.InvariantCulture)+",\"fullStairsVerified\":false,\"liveInstalled\":false,\"exactReplica\":false}");
            Debug.Log("SEATON_METRIC_OK");Application.Quit(0);
        }
        IEnumerator CheckStairs()
        {
            int up=0,down=0;
            foreach(var stair in hall.Plan.floors[0].spaces.Where(v=>v.kind=="stairs"))
            {
                bool sideways=Mathf.Abs(Mathf.Sin(stair.stairYaw*Mathf.Deg2Rad))>.5f;
                float w=sideways?stair.depth:stair.width,d=sideways?stair.width:stair.depth,lane=(d-.25f)/2;
                var rotation=hall.Root.transform.rotation*Quaternion.Euler(0,stair.stairYaw,0);
                for(int level=0;level<3;level++)
                {
                    var origin=hall.Root.transform.TransformPoint(new Vector3(stair.x,level*hall.FloorHeight,stair.z));
                    game.player.Teleport(origin+rotation*new Vector3(-w/2+.55f,.05f,-d/2+lane/2));yield return null;
                    yield return Move(rotation*Vector3.right*(w-1.1f));
                    Require(Mathf.Abs(game.player.transform.position.y-(origin.y+hall.FloorHeight/2))<.2f,stair.name+" ascent lower "+level);up++;
                    yield return Move(rotation*Vector3.forward*(d-lane));
                    yield return Move(rotation*Vector3.left*(w-1.1f));
                    Require(Mathf.Abs(game.player.transform.position.y-(origin.y+hall.FloorHeight))<.2f,stair.name+" ascent upper "+level);up++;
                    // Reverse the same route without teleporting between its flights.
                    yield return Move(rotation*Vector3.right*(w-1.1f));
                    Require(Mathf.Abs(game.player.transform.position.y-(origin.y+hall.FloorHeight/2))<.2f,stair.name+" descent upper "+level);down++;
                    yield return Move(rotation*Vector3.back*(d-lane));
                    yield return Move(rotation*Vector3.left*(w-1.1f));
                    Require(Mathf.Abs(game.player.transform.position.y-origin.y)<.2f,stair.name+" descent lower "+level);down++;
                }
            }
            Require(up==18&&down==18,"18 flights climbed and descended");
            File.WriteAllText(Path.Combine(output,"stairs-result.json"),"{\"passed\":true,\"flightsAscended\":18,\"flightsDescended\":18,\"measuredStairGeometry\":false,\"liveInstalled\":false,\"exactReplica\":false}");
            Debug.Log("SEATON_METRIC_STAIRS_OK");
        }
    }
}
