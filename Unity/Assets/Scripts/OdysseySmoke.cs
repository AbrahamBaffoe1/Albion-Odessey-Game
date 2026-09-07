using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AlbionOdyssey
{
    // Opt-in player verification. Uses a separate save and never changes normal play progress.
    public sealed class OdysseySmoke : MonoBehaviour
    {
        public static bool Enabled=>Array.IndexOf(Environment.GetCommandLineArgs(),"-odysseySmoke")>=0;
        [Serializable] class Result { public bool passed;public string error;public int floors;public int descents;public bool journal;public bool guide;public bool charterComplete;public int triangles;public int colliders;public float seconds; }
        OdysseyGame game;
        Result result=new Result();
        string Output=>Environment.GetEnvironmentVariable("ODYSSEY_SMOKE_PATH")??Path.Combine(Application.persistentDataPath,"Smoke");
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot(){if(Enabled)new GameObject("Automated physics walkthrough").AddComponent<OdysseySmoke>();}
        IEnumerator Start()
        {
            Directory.CreateDirectory(Output);
            yield return null;yield return null;
            game=FindAnyObjectByType<OdysseyGame>();
            if(game==null||game.player==null){Fail("Game failed to initialize");yield break;}
            game.state=new OdysseyState();game.player.controls=false;Cursor.lockState=CursorLockMode.None;Cursor.visible=false;
            foreach(var f in FindObjectsByType<MeshFilter>())if(f.name.StartsWith("SM_LegacyTower"))result.triangles+=f.sharedMesh.triangles.Length/3;
            result.colliders=FindObjectsByType<BoxCollider>().Length;
            yield return Capture("01-exterior",new Vector3(30,13,-42),new Vector3(0,14,0));
            if(!Physics.Raycast(new Vector3(5,1.65f,-17),Vector3.forward,out var guideHit,3)||guideHit.collider.GetComponent<GuideMarker>()==null){Fail("Pip guide is not targetable");yield break;}
            result.guide=true;
            yield return Capture("06-pip-guide",new Vector3(6.3f,1.5f,-17.5f),new Vector3(5,1.3f,-15));
            yield return Walk(new Vector3(0,0,1.4f));
            if(result.error!=null)yield break;
            for(int floor=0;floor<8;floor++)
            {
                float height=floor*3.6f;
                if(Mathf.Abs(game.player.transform.position.y-height)>.20f){Fail("Floor height mismatch on "+floor+": "+game.player.transform.position);yield break;}
                result.floors++;
                Vector3 eye=game.player.eyes.transform.position;
                var orb=new Vector3(0,height+1.15f,3);
                if(!Physics.Raycast(eye,(orb-eye).normalized,out var hit,3.5f)||hit.collider.GetComponent<MemoryMarker>()==null){Fail("Memory cannot be reached on floor "+floor);yield break;}
                if(!game.state.Collect(floor)){Fail("Memory reward failed");yield break;}
                Destroy(hit.collider.gameObject);
                if(floor==7)break;
                foreach(var point in new[]{new Vector3(0,height,0),new Vector3(3.1f,height,0),new Vector3(3.1f,height,-1.4f),new Vector3(9.1f,height,-1.4f),new Vector3(9.1f,height,1.0f),new Vector3(9.1f,height+1.8f,4.8f),new Vector3(11.5f,height+1.8f,4.8f),new Vector3(11.5f,height+3.6f,.6f),new Vector3(11.5f,height+3.6f,-1.4f),new Vector3(3.1f,height+3.6f,-1.4f),new Vector3(3.1f,height+3.6f,0),new Vector3(0,height+3.6f,0),new Vector3(0,height+3.6f,1.4f)})
                {yield return Walk(point);if(result.error!=null)yield break;}
            }
            yield return Capture("02-top-floor",game.player.transform.position+Vector3.up*1.65f,new Vector3(0,7*3.6f+1.6f,-8));
            for(int floor=7;floor>0;floor--)
            {
                float height=floor*3.6f;
                foreach(var point in new[]{new Vector3(0,height,0),new Vector3(3.1f,height,0),new Vector3(3.1f,height,-1.4f),new Vector3(11.5f,height,-1.4f),new Vector3(11.5f,height,.6f),new Vector3(11.5f,height-1.8f,4.8f),new Vector3(9.1f,height-1.8f,4.8f),new Vector3(9.1f,height-3.6f,1.0f),new Vector3(9.1f,height-3.6f,-1.4f),new Vector3(3.1f,height-3.6f,-1.4f),new Vector3(3.1f,height-3.6f,0),new Vector3(0,height-3.6f,0)})
                {yield return Walk(point);if(result.error!=null)yield break;}
                if(Mathf.Abs(game.player.transform.position.y-(height-3.6f))>.20f){Fail("Descending height mismatch on "+floor);yield break;}
                result.descents++;
            }
            yield return Capture("04-furnished-atrium",new Vector3(-4.5f,1.65f,-3.5f),new Vector3(-11,1.1f,2));
            if(!game.state.Build(24,2)||!game.state.Build(25,3)||!game.state.Build(26,1)||!game.state.Build(27,4)||!game.Save()||!game.state.Reclaim(25)||!game.state.Contribute()||!game.state.Contribute()||!game.state.Contribute()||!game.state.Valid()||!game.Save()){Fail("Build, refund, contribution or save failed");yield break;}
            var saved=JsonUtility.FromJson<OdysseyState>(File.ReadAllText(Path.Combine(Application.persistentDataPath,"albion-unity-smoke.json")));
            if(!saved.Valid()||saved.Current.plots[24]!=2||saved.Current.plots[25]!=0||saved.beacon!=6||saved.Current.memories!=255||saved.Current.milestones!=31){Fail("Saved state does not match play progress");yield break;}
            yield return Walk(new Vector3(0,0,-20));if(result.error!=null)yield break;
            for(int i=8;i<12;i++)
            {
                float x=(i-9.5f)*4;yield return Walk(new Vector3(x,0,-20));if(result.error!=null)yield break;
                var target=new Vector3(x,1.15f,-18)-game.player.eyes.transform.position;
                if(!Physics.Raycast(game.player.eyes.transform.position,target.normalized,out var hit,3.5f)||hit.collider.GetComponent<MemoryMarker>()?.id!=i||!game.state.Collect(i)){Fail("Outdoor memory "+i+" could not be collected");yield break;}
                Destroy(hit.collider.gameObject);
            }
            if(!game.state.Build(25,3)){Fail("Final campus type could not be rebuilt");yield break;}
            while(game.state.beacon<24)if(!game.state.Contribute()){Fail("Charter resources cannot complete Beacon");yield break;}
            if(!game.Save()||game.state.Current.milestones!=63){Fail("Full charter did not complete");yield break;}
            var completed=JsonUtility.FromJson<OdysseyState>(File.ReadAllText(Path.Combine(Application.persistentDataPath,"albion-unity-smoke.json")));
            if(!completed.Valid()||completed.Current.milestones!=63||completed.Current.memories!=4095||completed.beacon!=24){Fail("Completed chapter did not persist");yield break;}
            result.charterComplete=true;
            game.ToggleMode();game.RebuildCampus();
            if(GameObject.Find("Library")==null){Fail("Built library is missing from the scene");yield break;}
            yield return new WaitForEndOfFrame();
            yield return Capture("03-personal-campus",null,null);
            game.SetJournal(true);
            if(game.player.controls||!game.journalOpen){Fail("Journal did not suspend player controls");yield break;}
            yield return new WaitForEndOfFrame();
            var journalImage=ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.Combine(Output,"05-journal.png"),journalImage.EncodeToPNG());Destroy(journalImage);
            game.SetJournal(false);result.journal=true;
            result.passed=true;result.seconds=Time.realtimeSinceStartup;
            File.WriteAllText(Path.Combine(Output,"result.json"),JsonUtility.ToJson(result,true));
            Debug.Log("ODYSSEY_SMOKE_OK: eight floors climbed and descended with CharacterController, memory rays, charter milestones, journal, building, refund and save.");
            Application.Quit(0);
        }
        IEnumerator Walk(Vector3 target)
        {
            var p=game.player;int attempts=0;
            while(Vector2.Distance(new Vector2(p.transform.position.x,p.transform.position.z),new Vector2(target.x,target.z))>.035f)
            {
                var delta=target-p.transform.position;delta.y=0;
                p.body.Move(Vector3.ClampMagnitude(delta,.055f)+Vector3.down*.025f);
                if(++attempts>1800){Fail("Route obstructed near "+p.transform.position+" heading to "+target);yield break;}
                if(attempts%12==0)yield return null;
            }
            for(int i=0;i<30;i++)p.body.Move(Vector3.down*.025f);
            yield return null;
        }
        IEnumerator Capture(string name,Vector3? position,Vector3? target)
        {
            Camera camera=game.building?game.builderCamera:game.player.eyes;
            Vector3 oldPosition=camera.transform.position;Quaternion oldRotation=camera.transform.rotation;
            if(position.HasValue)camera.transform.position=position.Value;
            if(target.HasValue)camera.transform.LookAt(target.Value);
            yield return new WaitForEndOfFrame();
            var rt=new RenderTexture(1440,900,24);camera.targetTexture=rt;camera.Render();
            var old=RenderTexture.active;RenderTexture.active=rt;
            var texture=new Texture2D(1440,900,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1440,900),0,0);texture.Apply();
            File.WriteAllBytes(Path.Combine(Output,name+".png"),texture.EncodeToPNG());
            camera.targetTexture=null;RenderTexture.active=old;Destroy(texture);Destroy(rt);
            camera.transform.position=oldPosition;camera.transform.rotation=oldRotation;
        }
        void Fail(string message)
        {
            result.error=message;result.seconds=Time.realtimeSinceStartup;
            File.WriteAllText(Path.Combine(Output,"result.json"),JsonUtility.ToJson(result,true));
            Debug.LogError("ODYSSEY_SMOKE_FAILED: "+message);Application.Quit(1);
        }
    }
}
