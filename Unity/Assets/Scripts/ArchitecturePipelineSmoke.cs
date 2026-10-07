using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace AlbionOdyssey
{
    public sealed class ArchitecturePipelineSmoke:MonoBehaviour
    {
        string output;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Boot()
        {if(Array.IndexOf(Environment.GetCommandLineArgs(),"-architectureSmoke")>=0)new GameObject("Architecture pipeline verification").AddComponent<ArchitecturePipelineSmoke>();}
        static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
        IEnumerator Start()
        {
            Application.runInBackground=true;output=Environment.GetEnvironmentVariable("ARCHITECTURE_OUTPUT")??Application.persistentDataPath+"/Playtests/architecture";Directory.CreateDirectory(output);
            var stack=new Stack<IEnumerator>();stack.Push(Run());
            while(stack.Count>0)
            {
                object value;
                try{var e=stack.Peek();if(!e.MoveNext()){stack.Pop();continue;}value=e.Current;if(value is IEnumerator child){stack.Push(child);continue;}}
                catch(Exception e){Debug.LogError("ARCHITECTURE_SMOKE_FAILED "+e);File.WriteAllText(output+"/result.txt",e.ToString());Application.Quit(1);yield break;}
                yield return value;
            }
            File.WriteAllText(output+"/result.txt","PASS: prepared Ferguson/Robinson meshes, secondary UVs, collision parity, LOD triangle reduction, packed roughness, day/dusk/night and interior captures. These are rendering checks, not a survey-accuracy claim.");
            Debug.Log("ARCHITECTURE_SMOKE_OK");Application.Quit(0);
        }
        IEnumerator Run()
        {
            OdysseyGame game=null;float deadline=Time.realtimeSinceStartup+90;
            while(game==null||!game.Ready){Require(Time.realtimeSinceStartup<deadline,"Boot timeout");game=FindAnyObjectByType<OdysseyGame>();yield return null;}
            foreach(string name in new[]{"ferguson","robinson"})
            {
                var data=JsonUtility.FromJson<CraftDescription>(Resources.Load<TextAsset>("CampusCraft/"+name).text);
                foreach(var section in data.sections)
                {
                    var prefab=Resources.Load<GameObject>("PreparedArchitecture/"+name+"-"+section.name);Require(prefab!=null,"Missing prepared section: "+name+section.name);
                    var mesh=prefab.GetComponent<MeshFilter>().sharedMesh;Require(mesh.uv2.Length==mesh.vertexCount,"Missing secondary UVs");
                    Require(prefab.GetComponents<BoxCollider>().Length==section.colliders.Length,"Collider count changed");
                    var colliders=prefab.GetComponents<BoxCollider>();
                    for(int i=0;i<colliders.Length;i++)
                    {var c=section.colliders[i];Require((colliders[i].center-new Vector3(c.center[0],c.center[1],c.center[2])).sqrMagnitude<.000001f,"Collider shifted");Require((colliders[i].size-new Vector3(c.size[0],c.size[1],c.size[2])).sqrMagnitude<.000001f,"Collider resized");}
                }
            }
            var exterior=Resources.Load<GameObject>("PreparedArchitecture/ferguson-exterior");var lods=exterior.GetComponent<LODGroup>().GetLODs();Require(lods.Length==3,"Missing LODs");
            int last=int.MaxValue;foreach(var lod in lods){int count=lod.renderers[0].GetComponent<MeshFilter>().sharedMesh.triangles.Length;Require(count<last,"LOD does not reduce triangles");last=count;}
            bool rough=false;foreach(var material in exterior.GetComponent<MeshRenderer>().sharedMaterials)if(material.GetTexture("_MetallicGlossMap")!=null){Require(material.IsKeywordEnabled("_METALLICGLOSSMAP"),"Roughness keyword missing");rough=true;}Require(rough,"Roughness map missing");
            game.shell.Play();var building=CampusBuildings.Instance;building.Visit();game.player.controls=false;game.player.Teleport(building.Origin+new Vector3(23,.1f,-30));game.weather.SetSnowForVerification(false);
            game.environment.SetHour(13);yield return new WaitForSecondsRealtime(3);
            float dayIntensity=RenderSettings.sun.intensity;Color dayAmbient=RenderSettings.ambientSkyColor;
            Capture(game,building.Origin+new Vector3(27,11,-35),building.Origin+new Vector3(0,6,0),"01-ferguson-day.png");
            Capture(game,building.Origin+new Vector3(-25,7,30),building.Origin+new Vector3(0,6,0),"02-ferguson-rear.png");
            Capture(game,building.Origin+new Vector3(8,3,-13),building.Origin+new Vector3(0,3,-6),"03-ferguson-entry.png");
            yield return Benchmark(game,building.Origin);
            game.environment.SetHour(18);yield return new WaitForSecondsRealtime(12);Capture(game,building.Origin+new Vector3(27,11,-35),building.Origin+new Vector3(0,6,0),"04-ferguson-dusk.png");
            game.environment.SetHour(23);yield return new WaitForSecondsRealtime(12);
            Require(RenderSettings.sun.intensity<dayIntensity*.3f,"Night key light was overwritten");
            Require(RenderSettings.ambientSkyColor.maxColorComponent<dayAmbient.maxColorComponent*.25f,"Night ambient was overwritten");
            Capture(game,building.Origin+new Vector3(27,11,-35),building.Origin+new Vector3(0,6,0),"05-ferguson-night.png");
            Capture(game,building.Origin+new Vector3(0,1.7f,-4.6f),building.Origin+new Vector3(0,1.7f,0),"06-ferguson-interior.png");
        }
        IEnumerator Benchmark(OdysseyGame game,Vector3 origin)
        {
            var camera=new GameObject("Architecture benchmark camera").AddComponent<Camera>();camera.CopyFrom(game.player.eyes);camera.fieldOfView=48;camera.farClipPlane=220;
            camera.transform.position=origin+new Vector3(27,11,-35);camera.transform.LookAt(origin+new Vector3(0,6,0));
            bool previous=game.player.eyes.enabled;game.player.eyes.enabled=false;
            try
            {
                yield return new WaitForSecondsRealtime(3);
                var times=new float[300];double before=Time.realtimeSinceStartupAsDouble;
                for(int i=0;i<times.Length;i++){yield return null;double now=Time.realtimeSinceStartupAsDouble;times[i]=(float)((now-before)*1000);before=now;}
                Array.Sort(times);
                File.WriteAllText(Path.Combine(output,"frame-times.txt"),$"Ferguson exterior, 300 presented frames after warmup\nResolution: {Screen.width} x {Screen.height}\nGPU: {SystemInfo.graphicsDeviceName}\nMedian: {times[149]:F2} ms; p95: {times[284]:F2} ms; max: {times[299]:F2} ms\nVsync: {QualitySettings.vSyncCount}; target FPS: {Application.targetFrameRate}\nIncludes frame pacing; this is a stationary scene sample, not a campus-wide performance certification.\n");
            }
            finally{game.player.eyes.enabled=previous;Destroy(camera.gameObject);}
        }
        void Capture(OdysseyGame game,Vector3 from,Vector3 to,string name)
        {
            var camera=new GameObject("Architecture review camera").AddComponent<Camera>();camera.CopyFrom(game.player.eyes);camera.enabled=false;camera.fieldOfView=48;camera.aspect=1.6f;camera.farClipPlane=220;camera.transform.position=from;camera.transform.LookAt(to);
            var rt=new RenderTexture(1600,1000,24){antiAliasing=4};var previous=RenderTexture.active;var image=new Texture2D(1600,1000,TextureFormat.RGB24,false);
            try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1600,1000),0,0);image.Apply();File.WriteAllBytes(Path.Combine(output,name),image.EncodeToPNG());}
            finally{RenderTexture.active=previous;camera.targetTexture=null;rt.Release();Destroy(rt);Destroy(image);Destroy(camera.gameObject);}
        }
    }
}
