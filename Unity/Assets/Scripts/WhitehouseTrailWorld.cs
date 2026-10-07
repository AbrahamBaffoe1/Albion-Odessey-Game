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
 if(Array.IndexOf(Environment.GetCommandLineArgs(),"-natureTrailSmoke")>=0){ToggleVisit();yield return null;
 var camera=new GameObject("Nature network review").AddComponent<Camera>();camera.CopyFrom(game.player.eyes);camera.enabled=false;camera.orthographic=true;camera.orthographicSize=600;camera.farClipPlane=2000;camera.transform.position=root.position+new Vector3(220,1200,-100);camera.transform.rotation=Quaternion.Euler(90,0,0);
 var rt=new RenderTexture(1400,1400,24);camera.targetTexture=rt;bool captureFog=RenderSettings.fog;RenderSettings.fog=false;camera.Render();RenderSettings.fog=captureFog;RenderTexture.active=rt;var pic=new Texture2D(1400,1400,TextureFormat.RGB24,false);pic.ReadPixels(new Rect(0,0,1400,1400),0,0);pic.Apply();var output=Environment.GetEnvironmentVariable("NATURE_OUTPUT");System.IO.Directory.CreateDirectory(output);System.IO.File.WriteAllBytes(System.IO.Path.Combine(output,"Trail-network.png"),pic.EncodeToPNG());bool lit=game.environment.Locked;var before=RenderSettings.sun;
 var nested=new NatureAtmosphere();nested.Apply(game,root);nested.Restore();bool nestedRestored=RenderSettings.sun==before&&game.environment.Locked;
 ToggleVisit();bool restored=!game.environment.Locked;camera.targetTexture=null;RenderTexture.active=null;rt.Release();Destroy(rt);Destroy(pic);Destroy(camera.gameObject);
 bool passed=lit&&nestedRestored&&restored;Debug.Log(passed?"NATURE_TRAIL_SMOKE_OK":"NATURE_TRAIL_SMOKE_FAILED");Application.Quit(passed?0:1);}}

 void Update(){if(CampusMenuShortcuts.Pressed(KeyCode.N,game))ToggleVisit();}
 public void ToggleVisit(){if(game==null||!game.Ready)return;if(!game.player.TryExitVehicle()){game.notice="Park in an open space before visiting the nature trails.";return;}if(visiting){game.shell.Play();game.player.Teleport(saved);game.player.transform.rotation=facing;visiting=false;atmosphere.Restore();return;}if(root==null)Build();saved=game.player.transform.position;facing=game.player.transform.rotation;game.shell.Play();game.player.Teleport(root.position+new Vector3(0,.1f,0));visiting=true;atmosphere.Apply(game,root);}

 NatureKit kit;readonly NatureAtmosphere atmosphere=new NatureAtmosphere();

 void Build()
 {
  root=new GameObject("Whitehouse Nature Center · official map network").transform;root.position=new Vector3(6000,20,6000);
  kit=new NatureKit();var batch=new NatureBatch();
  // One tiled meadow with a collider, so the trails sit on real ground instead of a floating slab.
  NatureLook.Ground(root,"Provisional nature meadow",new Vector3(220,-.04f,-100),new Vector2(3000,3000),kit.Moss,9f);
  var map=JsonUtility.FromJson<NatureMap>(Resources.Load<TextAsset>("Nature/trails").text);
  foreach(var path in map.paths){BuildTrail(path);AddHabitat(batch,path);}
  batch.Flush(root);
  PlantWoodland(map);
  root.gameObject.AddComponent<NatureOwnedAssets>().Track(root.gameObject);
  Debug.Log("WHITEHOUSE_TRAILS_READY paths="+map.paths.Length);
 }

 // Each mapped path becomes a trail ribbon over a slightly wider, darker verge, with its name on a marker post.
 void BuildTrail(NaturePath path)
 {
  bool rail=path.name=="Rail";float half=rail?2.2f:1.1f;
  var trail=new List<Vector3>();var trailIndices=new List<int>();var verge=new List<Vector3>();var vergeIndices=new List<int>();
  for(int i=1;i<path.points.Length;i++)
  {
   var a=new Vector3(path.points[i-1].x,.03f,path.points[i-1].z);var b=new Vector3(path.points[i].x,.03f,path.points[i].z);if((a-b).sqrMagnitude<.001f)continue;
   var side=Vector3.Cross((b-a).normalized,Vector3.up);
   AddRibbon(trail,trailIndices,a,b,side*half);AddRibbon(verge,vergeIndices,a-Vector3.up*.012f,b-Vector3.up*.012f,side*half*1.55f);
  }
  MakeRibbon(path.name+" verge",verge,vergeIndices,kit.TrailEdge);
  MakeRibbon(path.name+" mapped trail",trail,trailIndices,rail?kit.Gravel:path.closedForRepairs?kit.TrailClosed:kit.Trail);
  if(path.points.Length>15)
  {
   var q=path.points[path.points.Length/2];var sign=new GameObject(path.name+" marker");sign.transform.SetParent(root,false);sign.transform.localPosition=new Vector3(q.x,2,q.z);
   var text=sign.AddComponent<TextMesh>();text.text=path.name+(path.closedForRepairs?"\nClosed pending repairs":"");text.fontSize=48;text.characterSize=.09f;text.anchor=TextAnchor.MiddleCenter;text.color=Color.white;
  }
 }

 static void AddRibbon(List<Vector3> vertices,List<int> indices,Vector3 a,Vector3 b,Vector3 side)
 {
  int n=vertices.Count;vertices.AddRange(new[]{a-side,a+side,b-side,b+side});indices.AddRange(new[]{n,n+1,n+2,n+2,n+1,n+3});
 }

 void MakeRibbon(string name,List<Vector3> vertices,List<int> indices,Material material)
 {
  if(vertices.Count==0)return;
  var mesh=new Mesh{indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};mesh.SetVertices(vertices);mesh.SetTriangles(indices,0);mesh.RecalculateNormals();
  var o=new GameObject(name);o.transform.SetParent(root,false);o.AddComponent<MeshFilter>().sharedMesh=mesh;o.AddComponent<MeshRenderer>().sharedMaterial=material;
 }

 // Habitat dressing follows the six trail families named on the official map: marsh reeds and pools, river-edge reeds, prairie flowers,
 // woodland ferns and shrubs, and loose ballast along the former railway bed. All of it is provisional planting, not a survey.
 void AddHabitat(NatureBatch batch,NaturePath path)
 {
  int seed=2018;foreach(char c in path.name)seed=seed*31+c;var rng=new System.Random(seed);float R(float lo,float hi){return lo+(float)rng.NextDouble()*(hi-lo);}
  for(int i=0;i<path.points.Length;i+=3)
  {
   var p=path.points[i];float side=rng.Next(2)==0?-1:1;var at=new Vector3(p.x+side*R(2.5f,6f),0,p.z+R(-3f,3f));
   switch(path.name)
   {
    case "Marsh":
     for(int k=0;k<5;k++){float h=R(1f,2.1f);var r=at+new Vector3(R(-1f,1f),h/2,R(-1f,1f));batch.Prim(PrimitiveType.Cylinder,r,new Vector3(.05f,h/2,.05f),Quaternion.Euler(R(-8,8),0,R(-8,8)),kit.Reed);if(k%2==0)batch.Prim(PrimitiveType.Cylinder,r+Vector3.up*(h/2-.1f),new Vector3(.1f,.18f,.1f),kit.Cattail);}
     if(i%6==0){float d=R(4f,9f);batch.Prim(PrimitiveType.Cylinder,new Vector3(at.x+side*R(2f,4f),.03f,at.z),new Vector3(d,.015f,d*.7f),kit.Water);}
     break;
    case "River’s Edge":
     for(int k=0;k<4;k++){float h=R(.9f,1.8f);batch.Prim(PrimitiveType.Cylinder,at+new Vector3(R(-.8f,.8f),h/2,R(-.8f,.8f)),new Vector3(.05f,h/2,.05f),Quaternion.Euler(R(-8,8),0,R(-8,8)),kit.Reed);}
     batch.Prim(PrimitiveType.Sphere,at+new Vector3(1.2f,.18f,0),new Vector3(R(.5f,1.1f),.4f,R(.5f,.9f)),kit.Stone);
     break;
    case "Prairie":
     for(int k=0;k<6;k++){var color=k%3==0?kit.FlowerGold:k%3==1?kit.FlowerLilac:kit.FlowerWhite;batch.Prim(PrimitiveType.Sphere,at+new Vector3(R(-1.2f,1.2f),R(.35f,.7f),R(-1.2f,1.2f)),Vector3.one*.2f,color);}
     batch.Prim(PrimitiveType.Sphere,at+Vector3.up*.2f,new Vector3(1.6f,.4f,1.4f),kit.LeafLight);
     break;
    case "Rail":
     for(int k=0;k<3;k++){float d=R(.15f,.35f);batch.Prim(PrimitiveType.Sphere,at+new Vector3(R(-1.5f,1.5f),d*.3f,R(-1.5f,1.5f)),new Vector3(d,d*.6f,d),kit.Stone);}
     break;
    default: // Beese Ecology and Wren: woodland floor
     for(int k=0;k<2;k++)batch.Prim(PrimitiveType.Sphere,at+new Vector3(R(-.5f,.5f),.16f,R(-.5f,.5f)),new Vector3(R(.9f,1.4f),.28f,R(.4f,.6f)),Quaternion.Euler(0,R(0,360),R(-12,12)),kit.Fern);
     if(i%2==0){float sz=R(1f,1.7f);batch.Prim(PrimitiveType.Sphere,at+new Vector3(side*1.5f,sz*.4f,0),new Vector3(sz,sz*.8f,sz),kit.LeafDeep);}
     break;
   }
  }
 }

 // Trees keep their own colliders so walkers cannot pass through trunks; they stay clear of every mapped path.
 void PlantWoodland(NatureMap map)
 {
  var definition=JsonUtility.FromJson<CraftDescription>(Resources.Load<TextAsset>("CampusCraft/oak").text);
  var template=CraftModel.Load("oak0",definition.sections[0],definition.materials,root);template.SetActive(false);var random=new System.Random(20181002);
  for(int i=0;i<650;i++)
  {
   float x=(float)random.NextDouble()*780-70,z=(float)random.NextDouble()*590-520;bool near=false;
   foreach(var path in map.paths){foreach(var p in path.points)if((new Vector2(x-p.x,z-p.z)).sqrMagnitude<64){near=true;break;}if(near)break;}
   if(near)continue;
   var tree=Instantiate(template,root);tree.name="Provisional woodland planting";tree.transform.localPosition=new Vector3(x,0,z);
   tree.transform.localRotation=Quaternion.Euler(0,(float)random.NextDouble()*360f,0);tree.transform.localScale=Vector3.one*(.7f+(float)random.NextDouble()*.6f);tree.SetActive(true);
  }
 }

 void OnDestroy(){atmosphere.Restore();if(root!=null)Destroy(root.gameObject);kit?.Release();}
 void OnGUI(){if(!visiting)return;GUI.Box(new Rect(20,20,610,70),"WHITEHOUSE NATURE CENTER · Esc menu → Nature trails to return\nPaths extracted from Albion's 2018 map. Terrain and planting provisional.\nExplore with normal movement controls. Use Esc → Forest run for the runner game.");}
}}
