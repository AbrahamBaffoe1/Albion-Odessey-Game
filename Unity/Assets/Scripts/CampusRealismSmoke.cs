using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
namespace AlbionOdyssey
{
    public sealed class CampusRealismSmoke:MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-realismSmoke")>=0)new GameObject("Campus realism verification").AddComponent<CampusRealismSmoke>();}
        string output;
        void Check(bool pass,string message){if(!pass)throw new Exception(message);}
        IEnumerator Start()
        {
            output=Environment.GetEnvironmentVariable("REALISM_OUTPUT")??Application.persistentDataPath+"/Playtests/realism";Directory.CreateDirectory(output);
            var stack=new System.Collections.Generic.Stack<IEnumerator>();stack.Push(Run());
            while(stack.Count>0){object value;try{var check=stack.Peek();if(!check.MoveNext()){stack.Pop();continue;}value=check.Current;if(value is IEnumerator nested){stack.Push(nested);continue;}}catch(Exception e){Debug.LogError("REALISM_FAILED "+e);File.WriteAllText(output+"/result.txt",e.ToString());Application.Quit(1);yield break;}yield return value;}
            File.WriteAllText(output+"/result.txt","PASS: foliage retained; photographed daylight loaded; capsule overlap and long-step wall prevention; route around wall; simulated students outside solid geometry; student portrait captured.");Debug.Log("REALISM_SMOKE_OK");Application.Quit(0);
        }
        IEnumerator Run()
        {
            OdysseyGame game=null;float until=Time.realtimeSinceStartup+100;
            while(game==null||!game.Ready){Check(Time.realtimeSinceStartup<until,"Boot timeout");game=FindAnyObjectByType<OdysseyGame>();yield return null;}
            game.shell.Play();game.player.controls=false;game.weather.SetSnowForVerification(false);
            Check(CampusStudentNavigation.Ready,"Navigation mesh unavailable");
            var foliage=GameObject.Find("Batched campus Craft / Sunlit leaves");Check(foliage!=null&&foliage.GetComponent<MeshFilter>().sharedMesh.vertexCount>100,"Leaf submeshes missing");
            Check(RenderSettings.skybox.shader.name=="Skybox/Panoramic","Photographed sky missing");
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.name="Navigation test wall";wall.transform.position=new Vector3(-450,1.5f,120);wall.transform.localScale=new Vector3(.2f,3,14);Physics.SyncTransforms();
            Check(CampusStudentNavigation.Blocked(new Vector3(-457,.08f,120),Vector3.right,14),"Long step tunneled through wall");
            Check(CampusStudentNavigation.Blocked(new Vector3(-450,.08f,120),Vector3.right,.1f),"Initial wall overlap missed");
            CampusStudentNavigation.BuildPaths();var walker=new GameObject("Navigation test walker").transform;walker.position=new Vector3(-457,.08f,120);var goal=new Vector3(-443,.08f,120);var path=new CampusStudentPath();
            for(int i=0;i<600&&Vector3.Distance(walker.position,goal)>.8f;i++){path.Move(walker,goal,.12f);Check(!CampusStudentNavigation.Occupied(walker.position),"Path entered wall");yield return null;}
            Check(Vector3.Distance(walker.position,goal)<.8f,"Could not route around wall: "+walker.position);Destroy(wall);Destroy(walker.gameObject);
            foreach(var agent in FindObjectsByType<CampusNpcAgent>())Check(!CampusStudentNavigation.Occupied(agent.transform.position),"Student inside solid geometry: "+agent.name);
            int moving=0;
            for(int frame=0;frame<120;frame++)
            {
                foreach(var agent in FindObjectsByType<CampusNpcAgent>()){Check(!CampusStudentNavigation.Occupied(agent.transform.position),"Student penetrated geometry during walk: "+agent.name);if(agent.IsMoving)moving++;}
                yield return null;
            }
            Check(moving>10,"Simulated population stopped moving");
            var hall=CampusBuildings.Instance.Building("50");hall.Visit();yield return null;
            foreach(var label in FindObjectsByType<CampusWorldLabel>()){label.enabled=false;foreach(var mesh in label.GetComponentsInChildren<TextMesh>())mesh.gameObject.SetActive(false);}
            Capture(game,hall.Origin+new Vector3(-29,9,-49),hall.Origin+new Vector3(0,5,-6),"Campus-daylight.png");
            var student=FindObjectsByType<CampusNpcAgent>().First();student.enabled=false;
            var avatar=student.GetComponentInChildren<KeeperAvatar>();avatar.Animate(0,false);
            var centre=student.transform.position+Vector3.up*1.15f;
            Capture(game,centre+student.transform.forward*2.8f+student.transform.right*.6f,centre,"Student-in-game.png");
        }
        void Capture(OdysseyGame game,Vector3 from,Vector3 to,string name)
        {
            var camera=new GameObject("Review camera").AddComponent<Camera>();camera.CopyFrom(game.player.eyes);camera.enabled=false;camera.fieldOfView=48;camera.aspect=1.6f;camera.farClipPlane=300;camera.transform.position=from;camera.transform.LookAt(to);
            var target=new RenderTexture(1920,1200,24){antiAliasing=4};camera.targetTexture=target;camera.Render();var prior=RenderTexture.active;RenderTexture.active=target;
            var pixels=new Texture2D(1920,1200,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,1920,1200),0,0);pixels.Apply();File.WriteAllBytes(Path.Combine(output,name),pixels.EncodeToPNG());RenderTexture.active=prior;camera.targetTexture=null;target.Release();Destroy(target);Destroy(pixels);Destroy(camera.gameObject);
        }
    }
}
