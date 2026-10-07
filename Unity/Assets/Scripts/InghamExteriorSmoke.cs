using System;
using System.IO;
using System.Collections;
using System.Linq;
using UnityEngine;
namespace AlbionOdyssey
{
    public sealed class InghamExteriorSmoke:MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-inghamExteriorSmoke")>=0)new GameObject("Ingham exterior verification").AddComponent<InghamExteriorSmoke>();}
        IEnumerator Start()
        {
            Application.runInBackground=true;OdysseyGame game=null;float deadline=Time.realtimeSinceStartup+90;
            while(game==null||!game.Ready){if(Time.realtimeSinceStartup>deadline){Application.Quit(1);yield break;}game=FindAnyObjectByType<OdysseyGame>();yield return null;}
            game.shell.Play();game.player.controls=false;game.weather.SetSnowForVerification(false);
            var hall=GameObject.Find("44 · Ingham Hall");
            if(hall==null||Vector3.Dot(-hall.transform.forward,Vector3.left)<.99f)throw new Exception("Ingham west porch orientation failed");
            var all=hall.GetComponentsInChildren<Transform>();
            if(!all.Any(t=>t.name=="Ingham central dormer")||all.Count(t=>t.name=="Ingham porch approach step")!=5)throw new Exception("Ingham exterior geometry missing");
            foreach(var label in FindObjectsByType<CampusWorldLabel>()){label.enabled=false;foreach(var text in label.GetComponentsInChildren<TextMesh>())text.gameObject.SetActive(false);}
            game.player.Teleport(hall.transform.TransformPoint(new Vector3(0,.05f,-20)));
            var camera=new GameObject("Ingham review camera").AddComponent<Camera>();camera.CopyFrom(game.player.eyes);camera.enabled=false;camera.fieldOfView=50;camera.aspect=1.6f;
            camera.transform.position=hall.transform.TransformPoint(new Vector3(-15,6,-22));camera.transform.LookAt(hall.transform.TransformPoint(new Vector3(0,4,0)));
            var rt=new RenderTexture(1600,1000,24){antiAliasing=4};camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
            var pixels=new Texture2D(1600,1000,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,1600,1000),0,0);pixels.Apply();
            string output=Environment.GetEnvironmentVariable("INGHAM_OUTPUT");Directory.CreateDirectory(output);File.WriteAllBytes(Path.Combine(output,"Ingham-exterior.png"),pixels.EncodeToPNG());
            File.WriteAllText(Path.Combine(output,"result.json"),"{\"passed\":true,\"westFacingPorch\":true,\"dormerPresent\":true,\"interiorImplemented\":false,\"exactReplica\":false}");
            Debug.Log("INGHAM_EXTERIOR_OK");Application.Quit(0);
        }
    }
}
