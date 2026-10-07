using System;
using System.Collections;
using System.IO;
using UnityEngine;
namespace AlbionOdyssey
{
    public sealed class LaunchArtBake : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-launchArtBake")>=0)new GameObject("Launch artwork capture").AddComponent<LaunchArtBake>();}
        IEnumerator Start()
        {
            Application.runInBackground=true;OdysseyGame game=null;
            while(game==null||!game.Ready){game=FindAnyObjectByType<OdysseyGame>();yield return null;}
            game.shell.Play();game.player.controls=false;game.weather.SetSnowForVerification(false);yield return new WaitForSecondsRealtime(1);
            var site=CampusExpansion.Find("26").position;
            foreach(var label in FindObjectsByType<CampusWorldLabel>()){label.enabled=false;foreach(var mesh in label.GetComponentsInChildren<TextMesh>())mesh.gameObject.SetActive(false);}
            foreach(var bubble in FindObjectsByType<CampusConversationBubble>())bubble.SetVisible(false);
            foreach(var light in FindObjectsByType<Light>())if(light.type==LightType.Directional){light.transform.rotation=Quaternion.Euler(37,-35,0);light.color=new Color(1,.90f,.75f);light.intensity=1.5f;}
            RenderSettings.ambientSkyColor=new Color(.65f,.76f,.9f);RenderSettings.ambientEquatorColor=new Color(.50f,.49f,.50f);QualitySettings.shadowDistance=160;
            var camera=new GameObject("Campus loading artwork camera").AddComponent<Camera>();camera.CopyFrom(game.player.eyes);camera.enabled=false;camera.fieldOfView=53;camera.aspect=16f/10;camera.farClipPlane=300;
            camera.transform.position=site+new Vector3(-25,5,-40);camera.transform.LookAt(site+new Vector3(5,6,0));
            var target=new RenderTexture(2160,1350,24){antiAliasing=4};camera.targetTexture=target;camera.Render();var before=RenderTexture.active;RenderTexture.active=target;
            var pixels=new Texture2D(2160,1350,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,2160,1350),0,0);pixels.Apply();string output=Environment.GetEnvironmentVariable("LAUNCH_ART_OUTPUT");Directory.CreateDirectory(Path.GetDirectoryName(output));File.WriteAllBytes(output,pixels.EncodeToPNG());RenderTexture.active=before;
            Debug.Log("LAUNCH_ART_BAKED_FROM_GAME");Application.Quit();
        }
    }
}
