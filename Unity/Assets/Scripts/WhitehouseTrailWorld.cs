using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AlbionOdyssey {
[Serializable] public class NaturePoint { public float x,z; }
[Serializable] public class NaturePath { public string name;public bool closedForRepairs;public NaturePoint[] points; }
[Serializable] public class NatureMap { public NaturePath[] paths; }
public sealed class WhitehouseTrailWorld:MonoBehaviour {
 OdysseyGame game;Transform root;bool visiting;Vector3 saved;Quaternion facing;
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Boot(){new GameObject("Whitehouse mapped trails").AddComponent<WhitehouseTrailWorld>();}
 IEnumerator Start(){while(game==null||!game.Ready){game=FindAnyObjectByType<OdysseyGame>();yield return null;}
 if(Array.IndexOf(Environment.GetCommandLineArgs(),"-natureTrailSmoke")>=0){Build();game.shell.Play();game.player.Teleport(root.position+new Vector3(0,.1f,0));yield return null;
 var camera=new GameObject("Nature network review").AddComponent<Camera>();camera.CopyFrom(game.player.eyes);camera.enabled=false;camera.orthographic=true;camera.orthographicSize=600;camera.farClipPlane=2000;camera.transform.position=root.position+new Vector3(220,1200,-100);camera.transform.rotation=Quaternion.Euler(90,0,0);
 var rt=new RenderTexture(1400,1400,24);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;var pic=new Texture2D(1400,1400,TextureFormat.RGB24,false);pic.ReadPixels(new Rect(0,0,1400,1400),0,0);pic.Apply();var output=Environment.GetEnvironmentVariable("NATURE_OUTPUT");System.IO.Directory.CreateDirectory(output);System.IO.File.WriteAllBytes(System.IO.Path.Combine(output,"Trail-network.png"),pic.EncodeToPNG());Debug.Log("NATURE_TRAIL_SMOKE_OK");Application.Quit(0);}}

 void Update(){if(CampusMenuShortcuts.Pressed(KeyCode.N,game))ToggleVisit();}
 public void ToggleVisit(){if(game==null||!game.Ready)return;if(!game.player.TryExitVehicle()){game.notice="Park in an open space before visiting the nature trails.";return;}if(visiting){game.shell.Play();game.player.Teleport(saved);game.player.transform.rotation=facing;visiting=false;return;}if(root==null)Build();saved=game.player.transform.position;facing=game.player.transform.rotation;game.shell.Play();game.player.Teleport(root.position+new Vector3(0,.1f,0));visiting=true;}

 void Build(){root=new GameObject("Whitehouse Nature Center · official map network").transform;root.position=new Vector3(6000,20,6000);
 var ground=KeeperAvatar.Part(root,"Provisional flat nature terrain",PrimitiveType.Cube,new Vector3(220,-.3f,-100),new Vector3(1100,.5f,1200),CraftModel.Surface("Nature meadow",new Color(.24f,.31f,.15f)),true);
 var map=JsonUtility.FromJson<NatureMap>(Resources.Load<TextAsset>("Nature/trails").text);
 foreach(var path in map.paths){var vertices=new List<Vector3>();var indices=new List<int>();for(int i=1;i<path.points.Length;i++){var a=new Vector3(path.points[i-1].x,.015f,path.points[i-1].z);var b=new Vector3(path.points[i].x,.015f,path.points[i].z);if((a-b).sqrMagnitude<.001f)continue;var side=Vector3.Cross((b-a).normalized,Vector3.up)*(path.name=="Rail"?2.2f:1.1f);int n=vertices.Count;vertices.AddRange(new[]{a-side,a+side,b-side,b+side});indices.AddRange(new[]{n,n+1,n+2,n+2,n+1,n+3});}
 var mesh=new Mesh();mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;mesh.SetVertices(vertices);mesh.SetTriangles(indices,0);mesh.RecalculateNormals();var o=new GameObject(path.name+" mapped trail");o.transform.SetParent(root,false);o.AddComponent<MeshFilter>().sharedMesh=mesh;o.AddComponent<MeshRenderer>().sharedMaterial=CraftModel.Surface(path.name+" earth",path.closedForRepairs?new Color(.4f,.3f,.22f):new Color(.58f,.46f,.29f));
 if(path.points.Length>15){var q=path.points[path.points.Length/2];var sign=new GameObject(path.name+" marker");sign.transform.SetParent(root,false);sign.transform.localPosition=new Vector3(q.x,2,q.z);var text=sign.AddComponent<TextMesh>();text.text=path.name+(path.closedForRepairs?"\nClosed pending repairs":"");text.fontSize=48;text.characterSize=.09f;text.anchor=TextAnchor.MiddleCenter;text.color=Color.white;}}
 var definition=JsonUtility.FromJson<CraftDescription>(Resources.Load<TextAsset>("CampusCraft/oak").text);var template=CraftModel.Load("oak0",definition.sections[0],definition.materials,root);template.SetActive(false);var random=new System.Random(20181002);
 for(int i=0;i<650;i++){float x=(float)random.NextDouble()*780-70,z=(float)random.NextDouble()*590-520;bool near=false;foreach(var path in map.paths){foreach(var p in path.points)if((new Vector2(x-p.x,z-p.z)).sqrMagnitude<64){near=true;break;}if(near)break;}if(near)continue;var tree=Instantiate(template,root);tree.name="Provisional woodland planting";tree.transform.localPosition=new Vector3(x,0,z);tree.transform.localScale=Vector3.one*(.7f+(float)random.NextDouble()*.6f);tree.SetActive(true);}
 Debug.Log("WHITEHOUSE_TRAILS_READY paths="+map.paths.Length);}
 void OnGUI(){if(!visiting)return;GUI.Box(new Rect(20,20,610,70),"WHITEHOUSE NATURE CENTER · Esc menu → Nature trails to return\nPaths extracted from Albion's 2018 map. Terrain and planting provisional.\nExplore with normal movement controls. Use Esc → Forest run for the runner game.");}
}}
