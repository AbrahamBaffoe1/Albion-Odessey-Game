using System;
using System.IO;
using System.Collections;
using System.Linq;
using UnityEngine;
namespace AlbionOdyssey
{
    public sealed class CraftSmoke : MonoBehaviour
    {
        OdysseyGame g;CampusBuildings b;string output;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Boot(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-craftSmoke")>=0)new GameObject("Campus craft verification").AddComponent<CraftSmoke>();}
        void Require(bool value,string note){if(!value)throw new Exception(note);}
        IEnumerator Start()
        {
            output=Environment.GetEnvironmentVariable("CRAFT_OUTPUT")??Application.persistentDataPath+"/Playtests/craftSmoke";Directory.CreateDirectory(output);
            var stack=new System.Collections.Generic.Stack<IEnumerator>();stack.Push(Check());while(stack.Count>0){object value;try{var check=stack.Peek();if(!check.MoveNext()){stack.Pop();continue;}value=check.Current;if(value is IEnumerator nested){stack.Push(nested);continue;}}catch(Exception e){Debug.LogError("CRAFT_SMOKE_FAILED "+e);File.WriteAllText(output+"/result.json","{\"passed\":false,\"reason\":"+JsonUtility.ToJson(new Note{message=e.Message})+"}");Application.Quit(1);yield break;}yield return value;}
            File.WriteAllText(output+"/result.json","{\"passed\":true,\"floors\":3,\"doors\":19,\"rigged\":true,\"skinDeformation\":true,\"streaming\":true,\"history\":true}");Debug.Log("CRAFT_SMOKE_OK: closed/open doorway collision, three stair floors, room access, authored skin animation, interior unloading, history and clean HUD");Application.Quit(0);
        }
        [Serializable] class Note {public string message;}
        IEnumerator Frame(string name){yield return new WaitForEndOfFrame();var tex=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(output+"/"+name+".png",tex.EncodeToPNG());Destroy(tex);}
        IEnumerator Move(Vector3 target,float max=12)
        {
            var p=g.player;float end=Time.realtimeSinceStartup+max;
            while(Vector2.Distance(new Vector2(p.transform.position.x,p.transform.position.z),new Vector2(target.x,target.z))>.13f)
            {
                Require(Time.realtimeSinceStartup<end,"Walk blocked: "+p.transform.position+" -> "+target);
                Vector3 d=target-p.transform.position;d.y=0;d=Vector3.ClampMagnitude(d,1);p.body.Move((d*2.6f+Vector3.down*3)*Time.deltaTime);yield return null;
            }
            for(int i=0;i<8;i++){p.body.Move(Vector3.down*.05f);yield return null;}
        }
        IEnumerator Check()
        {
            float end=Time.realtimeSinceStartup+50;while(g==null||g.shell==null||CampusBuildings.Instance==null){g=FindAnyObjectByType<OdysseyGame>();Require(Time.realtimeSinceStartup<end,"Boot");yield return null;}
            b=CampusBuildings.Instance;g.shell.ShowLaunch();yield return Frame("01-launch");b.Visit();g.player.controls=false;g.player.thirdPerson=false;g.player.UpdateCamera();yield return null;
            Require(g.player.avatar.IsRigged,"Rigged student missing");Require(b.Doors.Count==19,"Door count");Require(b.Interior.activeSelf,"Interior not loaded");
            var door=b.Doors[0];Require(!door.IsOpen,"Entrance should start closed");g.player.Teleport(b.Origin+new Vector3(0,.08f,-8));
            for(int i=0;i<80;i++){g.player.body.Move(new Vector3(0,-.03f,.06f));yield return null;}
            Require(g.player.transform.position.z<b.Origin.z-6.3f,"Closed door allowed passage");Require(door.Toggle(g.player),"Opening entrance failed");yield return new WaitForSeconds(.5f);
            yield return Move(b.Origin+new Vector3(0,0,-3));Require(b.Inside,"Did not enter building");yield return Frame("02-entrance");
            // Open a genuine interior room doorway, walk into corridor, then climb both flights.
            var roomDoor=b.Doors.First(d=>Mathf.Abs(d.ClosedPosition.x-(b.Origin.x+4))<.1f&&Mathf.Abs(d.ClosedPosition.y)<.1f&&d.ClosedPosition.z<b.Origin.z);roomDoor.Toggle(g.player);
            yield return Move(b.Origin+new Vector3(4,0,-2.4f));yield return Move(b.Origin+new Vector3(4,0,0));
            yield return Move(b.Origin+new Vector3(9.05f,0,0));yield return Move(b.Origin+new Vector3(9.05f,0,-5));yield return Move(b.Origin+new Vector3(11.7f,0,-5));
            for(int floor=1;floor<=2;floor++)
            {
                yield return Move(b.Origin+new Vector3(11.7f,floor*3.5f,5));Require(Mathf.Abs(g.player.transform.position.y-floor*3.5f)<.25f,"Stair flight did not ascend to floor "+floor);
                g.player.transform.rotation=Quaternion.Euler(0,-90,0);g.player.UpdateCamera();yield return Frame("03-floor-"+floor);
                if(floor<2){yield return Move(b.Origin+new Vector3(9.05f,floor*3.5f,5));yield return Move(b.Origin+new Vector3(9.05f,floor*3.5f,-5));yield return Move(b.Origin+new Vector3(11.7f,floor*3.5f,-5));}
            }
            g.tour.Open(g.tour.catalog.ForCampus("26"));Require(g.tour.selected.name.Contains("Ferguson"),"Building history");yield return Frame("04-history");g.tour.Close();g.player.controls=false;
            // Verify actual skin deformation from the authored clips rather than a static pose.
            g.player.avatar.gameObject.SetActive(true);
            var animator=g.player.avatar.GetComponentInChildren<Animator>(true);Require(animator!=null&&animator.runtimeAnimatorController!=null,"Animator controller missing");
            var skin=g.player.avatar.GetComponentsInChildren<SkinnedMeshRenderer>(true).OrderByDescending(r=>r.sharedMesh.vertexCount).First();
            animator.SetFloat("Speed",0);animator.Update(.1f);var a=new Mesh();skin.BakeMesh(a);animator.SetFloat("Speed",3.8f);animator.Update(.23f);var c=new Mesh();skin.BakeMesh(c);Require(a.vertices.Where((v,i)=>(v-c.vertices[i]).sqrMagnitude>.00001f).Any(),"Animation did not deform skinned mesh");Destroy(a);Destroy(c);
            b.Visit();g.player.controls=false;g.player.thirdPerson=false;g.player.eyes.transform.position=b.Origin+new Vector3(25,12,-34);g.player.eyes.transform.LookAt(b.Origin+Vector3.up*7);
            // Freeze camera ownership for the architecture review frame only.
            g.player.enabled=false;yield return Frame("05-ferguson-exterior");g.player.enabled=true;
            g.life.SetPanel("settings");yield return null;yield return null;yield return Frame("06-character");g.life.SetPanel("");g.player.controls=false;
            g.player.Teleport(b.Origin+Vector3.back*100);yield return null;yield return null;Require(!b.Interior.activeSelf,"Distant interior remained active");b.Visit();yield return null;Require(b.Interior.activeSelf,"Interior failed to reactivate");
            g.player.pointerControls=true;g.player.thirdPerson=true;g.player.UpdateCamera();yield return Frame("07-clean-hud");g.shell.EndSession();Require(g.shell.SaveSucceeded,"Saving failed");yield return Frame("08-session-end");
        }
    }
}
