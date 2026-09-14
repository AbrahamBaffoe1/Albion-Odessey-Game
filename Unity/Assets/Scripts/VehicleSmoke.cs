using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
namespace AlbionOdyssey
{
    public sealed class VehicleSmoke : MonoBehaviour
    {
        string output;OdysseyGame game;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-vehicleSmoke")>=0)new GameObject("Vehicle acceptance checks").AddComponent<VehicleSmoke>();}
        IEnumerator Start()
        {
            Application.runInBackground=true;output=Environment.GetEnvironmentVariable("VEHICLE_OUTPUT")??Path.Combine(Application.persistentDataPath,"VehicleChecks");Directory.CreateDirectory(output);
            var test=Check();while(true){object current;try{if(!test.MoveNext())break;current=test.Current;}catch(Exception error){Debug.LogError("VEHICLE_SMOKE_FAILED: "+error);Application.Quit(1);yield break;}yield return current;}
            Debug.Log("VEHICLE_SMOKE_OK: detailed fleet, 3 LODs, contained geometry, forward/reverse, front steering, wheel rotation, brake lights, collision barrier, stationary exit, driver and campus views");Application.Quit(0);
        }
        void Require(bool value,string message){if(!value)throw new Exception(message);}
        void Capture(string name,Vector3 at,Vector3 target)
        {
            var obj=new GameObject("Vehicle verification camera");var camera=obj.AddComponent<Camera>();camera.CopyFrom(game.player.eyes);camera.enabled=false;camera.transform.position=at;camera.transform.LookAt(target);camera.fieldOfView=42;
            var rt=new RenderTexture(1440,900,24){antiAliasing=4};camera.targetTexture=rt;camera.Render();var old=RenderTexture.active;RenderTexture.active=rt;
            var image=new Texture2D(1440,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1440,900),0,0);image.Apply();File.WriteAllBytes(Path.Combine(output,name+".png"),image.EncodeToPNG());RenderTexture.active=old;camera.targetTexture=null;rt.Release();Destroy(rt);Destroy(image);Destroy(obj);
        }
        IEnumerator Check()
        {
            float deadline=Time.realtimeSinceStartup+90;
            while(game==null||!game.Ready){game=FindAnyObjectByType<OdysseyGame>();Require(Time.realtimeSinceStartup<deadline,"Game boot timeout");yield return null;}
            game.shell.Play();game.player.controls=false;yield return new WaitForSecondsRealtime(1.2f);
            var car=game.campus.cars[0];var visual=car.Visual;
            Require(FindObjectsByType<CampusVehicleVisual>(FindObjectsSortMode.None).Length==game.campus.cars.Count+game.campus.parking.ParkedCarCount,"Fleet replacement incomplete");
            Require(visual.WheelPivotCount==12&&visual.Detail.lodCount==3,"Wheel pivots or distance detail missing");
            var levels=visual.Detail.GetLODs();int[] counts=levels.Select(l=>l.renderers.Sum(r=>r.GetComponent<MeshFilter>().sharedMesh.triangles.Length/3)).ToArray();
            Require(counts[0]>30000&&counts[0]<120000&&counts[1]<counts[0]/2&&counts[2]<10000,"LOD geometry budgets invalid: "+string.Join(",",counts));
            var bounds=levels[0].renderers[0].bounds;foreach(var renderer in levels[0].renderers)bounds.Encapsulate(renderer.bounds);
            Debug.Log("VEHICLE_BOUNDS "+bounds+" hull "+car.hull.bounds);
            Require(bounds.size.x>1.9f&&bounds.size.y>1.25f&&bounds.size.z>4f,"Vehicle import lost full-size geometry "+bounds.size);
            Require(bounds.size.x<2.21f&&bounds.size.y<1.5f&&bounds.size.z<4.41f,"Geometry exceeds collision footprint "+bounds.size);
            var front=visual.GetComponentsInChildren<Transform>().First(t=>t.name=="SteerFrontL_L0");var rear=visual.GetComponentsInChildren<Transform>().First(t=>t.name=="SteerRearL_L0");
            Require(car.transform.InverseTransformPoint(front.position).z>car.transform.InverseTransformPoint(rear.position).z,"Front of model faces backward");
            var p=car.transform.position;
            Capture("01-campus-coupe-front",p+car.transform.forward*6+car.transform.right*4+Vector3.up*2.7f,p+Vector3.up*.7f);
            Capture("02-campus-coupe-rear",p-car.transform.forward*5-car.transform.right*4+Vector3.up*2.3f,p+Vector3.up*.7f);
            var parked=FindObjectsByType<CampusParkedVehicle>(FindObjectsSortMode.None).First();p=parked.transform.position;
            Capture("03-detailed-parking",p+new Vector3(7,4,9),p+Vector3.up*.5f);
            car.transform.SetPositionAndRotation(new Vector3(510,0,720),Quaternion.identity);game.player.Teleport(car.transform.position+Vector3.back*3);Physics.SyncTransforms();
            Require(car.Enter(game.player)&&!game.player.body.enabled,"Vehicle entry failed");
            Vector3 start=car.transform.position;var wheel=visual.GetComponentsInChildren<Transform>().First(t=>t.name=="WheelFrontL_L0");Quaternion previous=wheel.localRotation;
            for(int i=0;i<90;i++){car.Drive(1,0,false,1f/60);if(i%15==0)yield return null;}
            Require(Vector3.Distance(start,car.transform.position)>3&&car.speed>1,"Throttle did not move car");Require(Quaternion.Angle(previous,wheel.localRotation)>1,"Road wheels did not spin");Require(!car.Exit(),"Moving exit allowed");
            previous=front.localRotation;car.Drive(1,1,false,.05f);Require(Quaternion.Angle(previous,front.localRotation)>1,"Front wheels did not steer");
            for(int i=0;i<90;i++)car.Drive(0,0,true,1f/60);
            Require(Mathf.Abs(car.speed)<.01f&&visual.BrakeLightsOn,"Brake feedback failed");
            start=car.transform.position;for(int i=0;i<70;i++)car.Drive(-1,0,false,1f/60);
            Require(Vector3.Dot(car.transform.position-start,car.transform.forward)<-1&&!visual.BrakeLightsOn,"Reverse or lamp release failed");
            car.speed=0;var barrier=GameObject.CreatePrimitive(PrimitiveType.Cube);barrier.transform.position=car.transform.position+car.transform.forward*7+Vector3.up*1.5f;barrier.transform.rotation=car.transform.rotation;barrier.transform.localScale=new Vector3(12,3,.3f);Physics.SyncTransforms();start=car.transform.position;
            for(int i=0;i<180;i++)car.Drive(1,0,false,1f/60);
            Require(Vector3.Distance(start,car.transform.position)<5&&car.speed==0,"Vehicle crossed collision barrier");Destroy(barrier);yield return null;
            car.transform.SetPositionAndRotation(game.campus.cars[1].transform.position+Vector3.right*7,Quaternion.identity);car.speed=0;car.Drive(0,0,true,.05f);game.player.avatar.Animate(0,true);yield return new WaitForSecondsRealtime(.4f);
            p=car.transform.position;Capture("04-driver-in-car",p+new Vector3(-4,2,5),p+Vector3.up*.7f);
            Require(car.Exit()&&game.player.body.enabled&&game.player.vehicle==null,"Stationary exit failed");
            File.WriteAllText(Path.Combine(output,"results.json"),"{\"passed\":true,\"parkedCars\":"+game.campus.parking.ParkedCarCount+",\"drivableCars\":"+game.campus.cars.Count+",\"lodTriangles\":["+string.Join(",",counts)+"]}");
        }
    }
}
