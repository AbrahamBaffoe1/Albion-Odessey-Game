using System;
using System.IO;
using System.Collections;
using System.Linq;
using UnityEngine;
namespace AlbionOdyssey
{
    public sealed class WesleyPlanSmoke:MonoBehaviour
    {
        string output;OdysseyGame game;WalkableCampusBuilding hall;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-wesleyPlanSmoke")>=0)new GameObject("Wesley plan verification").AddComponent<WesleyPlanSmoke>();}
        void Require(bool passed,string message){if(!passed){Debug.LogError("WESLEY_FAILED "+message+" player="+game.player.transform.position+" local="+(game.player.transform.position-hall.Origin));foreach(var hit in Physics.OverlapSphere(game.player.transform.position+Vector3.up*1.7f,.65f))Debug.LogError("CONTACT "+hit.name+" "+hit.bounds);Capture("failure.png",game.player.transform.position-hall.Origin+new Vector3(-3,3,-4),game.player.transform.position-hall.Origin+Vector3.up);Application.Quit(1);throw new Exception(message);}Debug.Log("WESLEY_CHECK "+message);}
        IEnumerator Start()
        {
            Application.runInBackground=true;QualitySettings.vSyncCount=0;Application.targetFrameRate=120;output=Environment.GetEnvironmentVariable("WESLEY_OUTPUT")??Application.persistentDataPath+"/Playtests/wesley-plan";Directory.CreateDirectory(output);
            float deadline=Time.realtimeSinceStartup+90;
            while(game==null||!game.Ready||CampusBuildings.Instance==null){if(Time.realtimeSinceStartup>deadline){Debug.LogError("WESLEY_BOOT_TIMEOUT");Application.Quit(1);yield break;}game=FindAnyObjectByType<OdysseyGame>();yield return null;}
            game.shell.Play();game.player.controls=false;game.weather.SetSnowForVerification(false);hall=CampusBuildings.Instance.Building("50");
            Require(hall.Plan.floors.Length==4,"Four published levels");
            Require(hall.Plan.floors[0].spaces.Count(s=>s.kind=="stairs")==5&&hall.Plan.floors.Skip(1).All(f=>f.spaces.Count(s=>s.kind=="stairs")==6),"Published five ground and six upper stair locations");
            Require(hall.Plan.floors[2].spaces.Any(s=>s.name=="287-S")&&hall.Plan.floors[0].spaces.Any(s=>s.name=="Kresge Commons"),"Published room identifiers retained");
            Require(hall.Root.GetComponentsInChildren<Transform>().Count(t=>t.name=="Wesley column shaft")==6,"Six portico columns");
            Require(hall.UnreachablePlanSpaces.Count==0,"Room corridor access: "+string.Join(", ",hall.UnreachablePlanSpaces));
            hall.Visit();
            for(int i=0;i<200;i++){game.player.body.Move(new Vector3(0,-.04f,.045f));if(i%8==0)yield return null;}
            Require(Mathf.Abs(game.player.transform.position.y-hall.FloorHeight)<.3f,"Front steps reach first floor");
            var door=hall.Doors[0];Require(door.Toggle(game.player),"Main entrance opens");yield return new WaitForSeconds(.6f);
            for(int i=0;i<90;i++){game.player.body.Move(new Vector3(0,-.035f,.04f));if(i%8==0)yield return null;}
            Require(hall.Inside,"Main entrance leads inside");
            // Exercise both flights of all six cores at all three level transitions.
            foreach(var s in hall.Plan.floors[1].spaces.Where(s=>s.kind=="stairs"))for(int level=s.name.Contains("west-front")?1:0;level<3;level++)
            {
                bool reverse=s.name.Contains("west-middle")||s.name.Contains("west-front")||s.name.Contains("east-rear");
                Quaternion rotation=Quaternion.Euler(0,reverse?180:0,0);Vector3 origin=hall.Origin+new Vector3(s.x,level*hall.FloorHeight,s.z);
                float lane=(s.depth-.25f)/2;
                Vector3 start=new Vector3(-s.width/2+.35f,.05f,-s.depth/2+lane/2);
                game.player.Teleport(origin+rotation*start);yield return null;
                for(int i=0;i<82;i++){game.player.body.Move(rotation*new Vector3(.045f,-.035f,0));if(i%8==0)yield return null;}
                Require(Mathf.Abs(game.player.transform.position.y-(level+.5f)*hall.FloorHeight)<.3f,s.name+" lower flight "+level);
                for(int i=0;i<39;i++){game.player.body.Move(rotation*new Vector3(0,-.035f,.045f));if(i%8==0)yield return null;}
                for(int i=0;i<82;i++){game.player.body.Move(rotation*new Vector3(-.045f,-.035f,0));if(i%8==0)yield return null;}
                Require(Mathf.Abs(game.player.transform.position.y-(level+1)*hall.FloorHeight)<.3f,s.name+" upper flight "+level);
            }
            game.player.Teleport(hall.Origin+new Vector3(0,2*hall.FloorHeight+.05f,-13));Require(!hall.Inside,"Upper courtyard is outside occupied rooms");
            foreach(var label in FindObjectsByType<CampusWorldLabel>()){label.enabled=false;foreach(var text in label.GetComponentsInChildren<TextMesh>())text.gameObject.SetActive(false);}
            foreach(var light in FindObjectsByType<Light>())if(light.type==LightType.Directional){light.transform.rotation=Quaternion.Euler(42,-30,0);light.color=new Color(1,.94f,.85f);light.intensity=1.3f;}
            QualitySettings.shadowDistance=180;
            Capture("Wesley-full-building.png",new Vector3(-65,53,-86),new Vector3(0,3,0));
            Capture("Wesley-wings.png",new Vector3(62,48,75),new Vector3(0,4,0));
            Capture("Wesley-corridor.png",new Vector3(-26.2f,8.1f,26),new Vector3(-26.2f,8.1f,10));
            File.WriteAllText(Path.Combine(output,"result.json"),"{\"passed\":true,\"levels\":4,\"stairCores\":6,\"flightTraversals\":34,\"mainEntrance\":true,\"exactReplica\":false}");Debug.Log("WESLEY_PLAN_SMOKE_OK");Application.Quit(0);
        }
        void Capture(string file,Vector3 at,Vector3 target)
        {
            var c=new GameObject("Wesley review camera").AddComponent<Camera>();c.CopyFrom(game.player.eyes);c.enabled=false;c.fieldOfView=52;c.aspect=1.6f;c.farClipPlane=300;c.transform.position=hall.Origin+at;c.transform.LookAt(hall.Origin+target);
            var rt=new RenderTexture(1920,1200,24){antiAliasing=4};c.targetTexture=rt;c.Render();var prior=RenderTexture.active;RenderTexture.active=rt;var pixels=new Texture2D(1920,1200,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,1920,1200),0,0);pixels.Apply();File.WriteAllBytes(Path.Combine(output,file),pixels.EncodeToPNG());RenderTexture.active=prior;Destroy(c.gameObject);Destroy(pixels);rt.Release();
        }
    }
}
