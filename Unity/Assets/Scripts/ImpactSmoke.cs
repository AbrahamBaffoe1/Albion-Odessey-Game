using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
namespace AlbionOdyssey
{
    public sealed class ImpactSmoke : MonoBehaviour
    {
        OdysseyGame game;string output;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-impactSmoke")>=0)new GameObject("Vehicle impact acceptance checks").AddComponent<ImpactSmoke>();}
        IEnumerator Start()
        {
            Application.runInBackground=true;output=Environment.GetEnvironmentVariable("IMPACT_OUTPUT")??Path.Combine(Application.persistentDataPath,"ImpactChecks");Directory.CreateDirectory(output);
            var check=Check();while(true){object current;try{if(!check.MoveNext())break;current=check.Current;}catch(Exception e){Debug.LogError("IMPACT_SMOKE_FAILED: "+e);Application.Quit(1);yield break;}yield return current;}
            File.WriteAllText(Path.Combine(output,"results.json"),"{\"passed\":true,\"ragdollBodies\":11,\"checks\":[\"vehicle sweep knocks down student\",\"ground contact\",\"automatic recovery\",\"wall prevents student hit\",\"barrier stops car\",\"individual mesh deformation on 3 LODs\",\"no repeated idle-wall damage\",\"damage reload\",\"coin and gem repairs\",\"restored meshes\",\"insufficient balance\"]}");
            Debug.Log("IMPACT_SMOKE_OK");Application.Quit(0);
        }
        void Require(bool ok,string message){if(!ok)throw new Exception(message);}
        void Capture(string name,Vector3 at,Vector3 target)
        {
            var camera=new GameObject("Impact evidence camera").AddComponent<Camera>();camera.CopyFrom(game.player.eyes);camera.enabled=false;camera.fieldOfView=45;camera.transform.position=at;camera.transform.LookAt(target);
            var rt=new RenderTexture(1440,900,24){antiAliasing=2};camera.targetTexture=rt;camera.Render();var prior=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(1440,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1440,900),0,0);image.Apply();File.WriteAllBytes(Path.Combine(output,name+".png"),image.EncodeToPNG());RenderTexture.active=prior;rt.Release();Destroy(image);Destroy(rt);Destroy(camera.gameObject);
        }
        IEnumerator Check()
        {
            float deadline=Time.realtimeSinceStartup+100;while(game==null||!game.Ready){game=FindAnyObjectByType<OdysseyGame>();Require(Time.realtimeSinceStartup<deadline,"Boot timeout");yield return null;}
            game.shell.Play();game.player.controls=false;game.state=new OdysseyState();game.sound.ResetBaseline(game.state);
            foreach(var c in game.campus.cars)c.Visual.ApplyDamage(c.Damage);
            var car=game.campus.cars[0];var start=new Vector3(510,0,720);car.transform.SetPositionAndRotation(start,Quaternion.identity);
            var ground=GameObject.CreatePrimitive(PrimitiveType.Cube);ground.name="Impact verification pavement";ground.transform.position=start+new Vector3(0,-.22f,12);ground.transform.localScale=new Vector3(50,.4f,65);
            var actor=new GameObject("Impact verification student");actor.transform.position=start+new Vector3(0,.08f,6);var agent=actor.AddComponent<CampusNpcAgent>();agent.Route=new[]{actor.transform.position+Vector3.forward*10,actor.transform.position+Vector3.forward*20};agent.Speed=.4f;agent.Build(CampusStudentProfiles.Get(3));
            game.player.Teleport(start+Vector3.back*3);yield return null;Physics.SyncTransforms();Require(car.Enter(game.player),"Enter test car");
            var impact=actor.GetComponent<CampusStudentImpact>();Require(impact!=null,"Student target absent");
            Capture("01-before-impact",start+new Vector3(-6,3,12),start+Vector3.forward*4+Vector3.up*.8f);
            car.speed=14;for(int i=0;i<20&&!impact.IsDown;i++){car.Drive(1,0,false,.05f);yield return new WaitForFixedUpdate();}
            Require(impact.IsDown&&impact.BodyCount==11,"Vehicle did not activate skeletal ragdoll");car.speed=0;
            yield return new WaitForSeconds(1.2f);
            var pelvis=actor.GetComponentsInChildren<Transform>().First(t=>t.name=="pelvis");Debug.Log("IMPACT_PELVIS "+pelvis.position+" start "+start);
            Require(pelvis.position.y>-.5f&&pelvis.position.y<.9f&&Vector3.Distance(pelvis.position,actor.transform.position)<12,"Ragdoll failed ground contact or exploded");
            Capture("02-student-knockdown",actor.transform.position+new Vector3(-5,3,7),pelvis.position+Vector3.up*.2f);
            yield return new WaitForSeconds(4);Require(!impact.IsDown&&agent.enabled,"Student never recovered");var recovered=actor.transform.position;yield return new WaitForSeconds(.8f);Require(Vector3.Distance(actor.transform.position,recovered)>.05f,"Student did not resume route");
            Capture("03-student-recovered",actor.transform.position+new Vector3(-3,2,4),actor.transform.position+Vector3.up);
            agent.enabled=false;actor.transform.position=start+new Vector3(0,.08f,14);
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.name="Impact verification building wall";wall.transform.position=start+new Vector3(0,2,11);wall.transform.localScale=new Vector3(14,4,.6f);
            car.transform.SetPositionAndRotation(start,Quaternion.identity);Physics.SyncTransforms();car.speed=19;
            for(int i=0;i<30&&car.Damage.amount==0;i++){car.Drive(1,0,false,.05f);yield return null;}
            Require(car.Damage.amount>0&&car.speed==0&&car.transform.position.z<start.z+9,"Building collision did not stop/damage car");
            Require(!impact.IsDown,"Car hit student through wall");Require(car.Visual.DeformedVertexCount>100,"Body mesh did not dent");
            int changed=car.Visual.DeformedVertexCount,damage=car.Damage.amount;Require(car.Visual.GetComponentsInChildren<MeshFilter>(true).Count(f=>f.sharedMesh.name=="Individual coupe damage")==3,"Damage missing from a distance LOD");
            Require(game.campus.cars[1].Damage.amount==0&&game.campus.cars[1].Visual.DeformedVertexCount==0,"Damage leaked to another car");
            for(int i=0;i<90;i++)car.Drive(1,0,false,.05f);Require(car.Damage.amount==damage,"Holding accelerator against wall repeats charge/damage");
            // Move the stopped vehicle away from the test wall for clear bodywork evidence.
            var crashPosition=car.transform.position;car.transform.position=start+Vector3.right*20;car.Drive(0,0,true,.01f);
            Capture("04-dented-front",car.transform.position+new Vector3(-4,2,6),car.transform.position+Vector3.up*.7f);
            car.transform.position=crashPosition;car.Drive(0,0,true,.01f);
            Require(game.Save(),"Crash save failed");var reloaded=JsonUtility.FromJson<OdysseyState>(File.ReadAllText(Path.Combine(Application.persistentDataPath,"Playtests",PlaytestMode.Name,"save.json")));reloaded.UpgradeLegacySave();Require(reloaded.Valid()&&reloaded.vehicles[0].amount==damage&&reloaded.vehicles[0].dents.Length>0,"Damage did not survive save/reload");
            for(int i=0;i<12;i++)game.state.Collect(i);game.Save();Require(game.repairs.Open(car),"Repair menu failed to open");
            yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,"05-repair-screen.png"));yield return null;
            int coins=game.state.Current.acorns,cost=car.Damage.CoinCost;Require(game.repairs.Pay(false)&&game.state.Current.acorns==coins-cost&&car.Damage.amount==0&&car.Visual.DeformedVertexCount==0,"Coin repair failed");
            Require(!game.repairs.Pay(false)&&game.state.Current.acorns==coins-cost,"Repair charged twice");
            yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,"06-repair-paid.png"));yield return null;
            car.RecordImpact(car.transform.TransformPoint(new Vector3(0,.8f,-2.1f)),car.transform.forward,12);int gems=game.state.Current.Gems,gemCost=car.Damage.GemCost;
            Require(game.repairs.Pay(true)&&game.state.Current.Gems==gems-gemCost&&car.Damage.amount==0&&game.state.Valid(),"Gem repair failed");
            game.state.active=1;car.RecordImpact(car.transform.TransformPoint(new Vector3(0,.8f,2.1f)),-car.transform.forward,19);Require(!game.repairs.Pay(true)&&car.Damage.amount>0&&game.state.Current.Gems==0,"Insufficient balance erased damage");
            Debug.Log("IMPACT_DEFORMED_VERTICES "+changed);game.life.SetPanel("");game.player.controls=false;car.speed=0;Destroy(wall);Destroy(actor);yield return null;
        }
    }
}
