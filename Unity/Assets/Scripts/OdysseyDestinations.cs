using System;
using System.Collections.Generic;
using UnityEngine;
namespace AlbionOdyssey {
// Publicly documented massing; unmeasured room dimensions are a playable reconstruction.
public sealed class OdysseyDestinations:MonoBehaviour {
 OdysseyGame game;Transform observatory,telescope,sky;Camera skyCamera,arrivalCamera;RenderTexture skyImage;
 Material brick,stone,wood,metal,glass,gold;readonly List<UnityEngine.Object> owned=new List<UnityEngine.Object>();
 float yaw,pitch=25,fov=32,arrivalAt;int target;bool arrival;Vector3 natureEntry;int journalBits {get=>game.state.Current.fieldJournal;set=>game.state.Current.fieldJournal=value;}
 public Transform Observatory=>observatory;public Vector3 TelescopePosition=>telescope.position;
 public bool TelescopeOpen=>game.life.panel=="telescope";
 static readonly string[] Targets={"The Moon","Saturn","Orion"};
 static readonly Vector2[] Bearings={new Vector2(0,25),new Vector2(42,32),new Vector2(-38,43)};
 static readonly Color Ivory=new Color(.88f,.83f,.71f);
 public void Setup(OdysseyGame owner){game=owner;brick=Mat("Observatory warm red brick",new Color(.35f,.14f,.1f));stone=Mat("Limestone",new Color(.65f,.6f,.5f));wood=Mat("Oiled walnut",new Color(.18f,.1f,.058f));metal=Mat("Patinated dome",new Color(.15f,.24f,.23f),.65f);glass=Mat("Night windows",new Color(.08f,.14f,.19f),.65f);gold=Mat("Brass refractor",new Color(.61f,.4f,.14f),.7f);brick.color=new Color(.8f,.74f,.69f);brick.mainTexture=Resources.Load<Texture2D>("Architecture/T_Architecture_Brick_BaseColor");brick.SetTexture("_BumpMap",Resources.Load<Texture2D>("Architecture/T_Architecture_Brick_Normal"));brick.EnableKeyword("_NORMALMAP");brick.mainTextureScale=new Vector2(3,3);BuildObservatory();}
 Material Mat(string n,Color c,float metallic=0){var m=new Material(Shader.Find("Standard")){name=n,color=c};m.SetFloat("_Metallic",metallic);m.SetFloat("_Glossiness",.48f);owned.Add(m);return m;}
 GameObject Shape(Transform parent,string name,PrimitiveType type,Vector3 p,Vector3 size,Material material,bool collision=true){var o=GameObject.CreatePrimitive(type);o.name=name;o.transform.SetParent(parent,false);o.transform.localPosition=p;o.transform.localScale=size;o.GetComponent<Renderer>().sharedMaterial=material;if(!collision)Destroy(o.GetComponent<Collider>());return o;}
 GameObject Box(Transform p,string n,Vector3 at,Vector3 size,Material m,bool collider=true)=>Shape(p,n,PrimitiveType.Cube,at,size,m,collider);
 void Label(Transform p,string text,Vector3 at,float size=.09f){var o=new GameObject(text);o.transform.SetParent(p,false);o.transform.localPosition=at;var t=o.AddComponent<TextMesh>();t.text=text;t.fontSize=64;t.characterSize=size;t.anchor=TextAnchor.MiddleCenter;t.color=Ivory;}
 void Lamp(Transform parent,Vector3 at){var l=new GameObject("Warm interior light").AddComponent<Light>();l.transform.SetParent(parent,false);l.transform.localPosition=at;l.type=LightType.Point;l.range=13;l.intensity=1.8f;l.color=new Color(1,.8f,.53f);}
 void BuildObservatory(){
  observatory=new GameObject("5 · Brown Honors historic observatory · reconstructed interior").transform;observatory.position=CampusExpansion.Find("5").position;
  Box(observatory,"Ground floor",new Vector3(0,.05f,0),new Vector3(12,.1f,14),wood);
  // Front split leaves a real walk-through opening. Windows are recessed between wall piers.
  foreach(int side in new[]{-1,1})Box(observatory,"Entrance wing",new Vector3(side*3.7f,3.5f,-7),new Vector3(4.6f,7,.4f),brick);
  Box(observatory,"Entry lintel",new Vector3(0,5,-7),new Vector3(2.8f,4,.4f),brick);
  Box(observatory,"Rear wall",new Vector3(0,3.5f,7),new Vector3(12,7,.4f),brick);
  foreach(int side in new[]{-1,1}){
   Box(observatory,"Wall base",new Vector3(side*6,.6f,0),new Vector3(.4f,1.2f,14),brick);
   Box(observatory,"Window spandrel",new Vector3(side*6,3.8f,0),new Vector3(.4f,1.8f,14),brick);
   Box(observatory,"Cornice",new Vector3(side*6,6.7f,0),new Vector3(.65f,.65f,14.4f),stone);
   for(int j=0;j<5;j++){float z=-5.6f+j*2.8f;Box(observatory,"Brick pier",new Vector3(side*6,3.5f,z),new Vector3(.5f,6,1.2f),brick);}
   for(int j=0;j<4;j++)for(int level=0;level<2;level++){float z=-4.2f+j*2.8f,y=2.05f+3.2f*level;
    Box(observatory,"Window recess",new Vector3(side*6,y,z),new Vector3(.12f,1.7f,1.5f),glass);Box(observatory,"Stone sill",new Vector3(side*6,y-.85f,z),new Vector3(.65f,.16f,1.8f),stone);
    Box(observatory,"Window mullion",new Vector3(side*6.1f,y,z),new Vector3(.17f,1.7f,.07f),wood,false);
   }
  }
  for(int side=-1;side<=1;side+=2){
   for(int level=0;level<2;level++){float x=side*3.7f,y=1.8f+level*3.2f;
    Box(observatory,"Front sash window",new Vector3(x,y,-7.24f),new Vector3(1.15f,1.7f,.08f),glass,false);
    foreach(int edge in new[]{-1,1})Box(observatory,"Ivory window frame",new Vector3(x+edge*.64f,y,-7.3f),new Vector3(.11f,1.95f,.15f),stone,false);
    Box(observatory,"Window lintel",new Vector3(x,y+.95f,-7.3f),new Vector3(1.4f,.19f,.24f),stone,false);
    Box(observatory,"Window sill",new Vector3(x,y-.92f,-7.35f),new Vector3(1.5f,.18f,.34f),stone,false);
    Box(observatory,"Sash crossbar",new Vector3(x,y,-7.3f),new Vector3(1.2f,.07f,.13f),stone,false);
   }
   Shape(observatory,"Portico column",PrimitiveType.Cylinder,new Vector3(side*1.8f,1.5f,-8.6f),new Vector3(.25f,1.5f,.25f),stone);
  }
  Box(observatory,"Portico canopy",new Vector3(0,3.05f,-8),new Vector3(4.4f,.22f,2.6f),stone);
  Box(observatory,"Stone approach",new Vector3(0,.02f,-11),new Vector3(3.5f,.05f,7),stone);
  Box(observatory,"Brick chimney",new Vector3(-4,7.7f,4.5f),new Vector3(.9f,3,.9f),brick);
  // Gallery floor stops at the stairwell; a gentle stair ramp is the invisible collision surface.
  Box(observatory,"Upper gallery",new Vector3(1.2f,3.45f,0),new Vector3(9.4f,.18f,13.5f),wood);
  for(int i=0;i<20;i++)Box(observatory,"Stair tread",new Vector3(-4.8f,.0875f*(i+1),-4.8f+i*.49f),new Vector3(1.65f,.175f*(i+1),.5f),wood,false);
  var ramp=Box(observatory,"Stair collision ramp",new Vector3(-4.8f,1.73f,-.1f),new Vector3(1.65f,.15f,10.3f),wood);ramp.transform.localRotation=Quaternion.Euler(-19.65f,0,0);ramp.GetComponent<Renderer>().enabled=false;
  Box(observatory,"Stair landing",new Vector3(-4.4f,3.45f,5.8f),new Vector3(3,.18f,2),wood);
  for(int i=0;i<20;i++)Box(observatory,"Stair baluster",new Vector3(-3.95f,.175f*(i+1)+.45f,-4.8f+i*.49f),new Vector3(.06f,.9f,.06f),gold);
  for(int side=-1;side<=1;side+=2){var roof=Box(observatory,"Hipped roof slope",new Vector3(side*2.8f,7.3f,0),new Vector3(6.9f,.22f,14.7f),metal);roof.transform.localRotation=Quaternion.Euler(0,0,-side*20);}
  // Round corner tower with a southern gallery opening and a slit through the dome.
  Vector3 center=new Vector3(3,0,4);var domePaint=Mat("Painted observatory dome",new Color(.78f,.8f,.79f),.28f);for(int i=0;i<32;i++){float a=i*360f/32;if(a>145&&a<215)continue;var wall=Box(observatory,"Round tower brick segment",center+new Vector3(Mathf.Sin(a*Mathf.Deg2Rad)*3,4.3f,Mathf.Cos(a*Mathf.Deg2Rad)*3),new Vector3(.7f,8.6f,.3f),brick);wall.transform.localRotation=Quaternion.Euler(0,a,0);}
  Shape(observatory,"Observing platform",PrimitiveType.Cylinder,center+new Vector3(0,3.45f,0),new Vector3(5.8f,.12f,5.8f),wood);
  for(int ring=0;ring<9;ring++){float elev=(ring+.5f)*10*Mathf.Deg2Rad;float radius=3.2f*Mathf.Cos(elev),y=8.6f+3.2f*Mathf.Sin(elev);for(int i=2;i<31;i++){float a=i*360f/32;var tile=Box(observatory,"Dome copper panel",center+new Vector3(Mathf.Sin(a*Mathf.Deg2Rad)*radius,y,Mathf.Cos(a*Mathf.Deg2Rad)*radius),new Vector3(Mathf.Max(.12f,radius*.21f),.59f,.14f),domePaint,false);tile.transform.localRotation=Quaternion.Euler(-ring*10-5,a,0);}}
  // Remove roof collision under the dome aperture only; refractor is accessible from the gallery.
  telescope=new GameObject("Alvan Clark eight-inch refractor · operating position").transform;telescope.SetParent(observatory,false);telescope.localPosition=center+new Vector3(0,3.6f,0);
  Shape(telescope,"Cast iron pier",PrimitiveType.Cylinder,new Vector3(0,.8f,0),new Vector3(.38f,.8f,.38f),metal);
  Shape(telescope,"Equatorial bearing",PrimitiveType.Sphere,new Vector3(0,1.6f,0),Vector3.one*.6f,gold,false);
  var tube=Shape(telescope,"Long brass refractor",PrimitiveType.Cylinder,new Vector3(0,2.1f,0),new Vector3(.28f,1.9f,.28f),gold,false);tube.transform.localRotation=Quaternion.Euler(58,0,0);
  for(int i=-1;i<=1;i+=2){var ring=Shape(telescope,"Optical collar",PrimitiveType.Cylinder,new Vector3(0,2.1f+i*.92f,i*1.48f),new Vector3(.34f,.09f,.34f),metal,false);ring.transform.localRotation=tube.transform.localRotation;}
  Shape(telescope,"Counterweight",PrimitiveType.Sphere,new Vector3(-.65f,1.4f,0),Vector3.one*.45f,metal,false);
  Label(observatory,"ALBION OBSERVATORY",new Vector3(0,3.65f,-7.26f),.064f);Label(observatory,"OBSERVING GALLERY  /  UPSTAIRS",new Vector3(0,2.5f,6.72f),.05f);
  for(int i=0;i<3;i++){Box(observatory,"Study desk",new Vector3(1+i*1.6f,.8f,-3),new Vector3(1.25f,.12f,.8f),wood);Box(observatory,"Desk base",new Vector3(1+i*1.6f,.38f,-3),new Vector3(.7f,.75f,.5f),metal);}
  Lamp(observatory,new Vector3(0,3,-2));Lamp(observatory,new Vector3(2,6.6f,4));
 }
 public void BuildNatureCenter(Transform root){
  var hall=new GameObject("Whitehouse discovery pavilion · interpretive reconstruction").transform;hall.SetParent(root,false);hall.localPosition=new Vector3(0,0,-20);natureEntry=hall.position+new Vector3(0,.15f,-10);
  Box(hall,"Pavilion timber floor",new Vector3(0,.06f,0),new Vector3(20,.12f,14),wood);
  foreach(int side in new[]{-1,1}){Box(hall,"Limestone sidewall",new Vector3(side*10,1.6f,0),new Vector3(.4f,3.2f,14),stone);Box(hall,"Front glass wing",new Vector3(side*6,2,-7),new Vector3(8,4,.15f),glass);}
  Box(hall,"Back wall",new Vector3(0,2,7),new Vector3(20,4,.3f),wood);
  for(int i=-4;i<=4;i++){Box(hall,"Exposed roof beam",new Vector3(i*2.3f,4.3f,0),new Vector3(.2f,.4f,15),wood);}
  Box(hall,"Roof",new Vector3(0,4.7f,0),new Vector3(21,.25f,15),metal);
  Label(hall,"WHITEHOUSE",new Vector3(0,3.85f,-7.2f),.10f);Label(hall,"NATURE CENTER",new Vector3(0,3.05f,-7.2f),.048f);
  string[] names={"WOODLAND","PRAIRIE","RIVER & MARSH"};
  for(int i=0;i<3;i++){float x=-6+i*6;Box(hall,"Habitat exhibit",new Vector3(x,1.2f,5.7f),new Vector3(4,2.4f,.25f),metal);Label(hall,names[i],new Vector3(x,2,5.5f),.046f);Label(hall,i==0?"Listen to the canopy":i==1?"Follow the pollinators":"Watch the water",new Vector3(x,1.3f,5.5f),.025f);Lamp(hall,new Vector3(x,3.5f,2));
   Box(hall,"Specimen table",new Vector3(x,.95f,2),new Vector3(2.8f,.16f,1.4f),wood);foreach(int side in new[]{-1,1})Box(hall,"Table leg",new Vector3(x+side*1,.47f,2),new Vector3(.12f,.94f,.9f),metal);
  }
  var guide=new GameObject("Nature center student guide").AddComponent<KeeperAvatar>();guide.transform.SetParent(hall,false);guide.transform.localPosition=new Vector3(5,0,-2);guide.Build(3,3,0,false);guide.transform.localRotation=Quaternion.Euler(0,180,0);
  Label(hall,"FIELD JOURNAL\nExplore the living classroom",new Vector3(-5,2,-2),.048f);
 }
 public void ArriveNature(){
  var trails=FindAnyObjectByType<WhitehouseTrailWorld>();if(trails==null)return;if(!trails.EnterVisit())return;game.life.SetPanel("arrival");arrival=true;arrivalAt=Time.unscaledTime;
  if(arrivalCamera==null){arrivalCamera=new GameObject("Nature center arrival camera").AddComponent<Camera>();arrivalCamera.depth=150;arrivalCamera.fieldOfView=48;arrivalCamera.farClipPlane=350;}
  arrivalCamera.gameObject.SetActive(true);game.player.Teleport(natureEntry);UpdateArrival();
 }
 void UpdateArrival(){if(!arrival)return;float t=OdysseyAccessibility.ReducedMotion?1:Mathf.SmoothStep(0,1,(Time.unscaledTime-arrivalAt)/4.5f);arrivalCamera.transform.position=natureEntry+new Vector3(8*(1-t),3.8f-1.7f*t,-14+9*t);arrivalCamera.transform.LookAt(natureEntry+new Vector3(0,2,10));}
 public void FinishArrival(){game.adventure?.Arrived();arrival=false;if(arrivalCamera!=null)arrivalCamera.gameObject.SetActive(false);game.life.SetPanel("");game.player.Teleport(natureEntry);game.player.transform.rotation=Quaternion.identity;}
 public void OpenTelescope(){if(sky==null)BuildSky();sky.gameObject.SetActive(true);game.life.SetPanel("telescope");Aim(0);}
 void Aim(int i){target=i;yaw=Bearings[i].x;pitch=Bearings[i].y;fov=i==0?12:i==1?7:32;}
 public void RecordObservation(){if(TelescopeOpen&&Vector3.Distance(game.player.transform.position,telescope.position)<3.4f){game.adventure?.Observe(target);game.activities?.Record(1);}}
 public bool RecordHabitat(int i){if(game.life.panel!="fieldguide"||i<0||i>2||Vector3.Distance(game.player.transform.position,natureEntry+new Vector3(5,0,8))>=4)return false;int before=journalBits;journalBits|=1<<i;if(game.Save()){game.activities?.Record(2);return true;}journalBits=before;return false;}
 public void CloseTelescope(){if(sky!=null)sky.gameObject.SetActive(false);game.life.SetPanel("");}
 public bool HandleInput(){
  if(TelescopeOpen){if(Input.GetKeyDown(KeyCode.Space))RecordObservation();if(Input.GetKeyDown(KeyCode.Escape)){CloseTelescope();return true;}if(Input.GetKeyDown(KeyCode.Alpha1))Aim(0);if(Input.GetKeyDown(KeyCode.Alpha2))Aim(1);if(Input.GetKeyDown(KeyCode.Alpha3))Aim(2);
   float dt=Time.unscaledDeltaTime;yaw+=(Input.GetAxisRaw("Horizontal")*25)*dt;pitch=Mathf.Clamp(pitch+Input.GetAxisRaw("Vertical")*20*dt,-10,85);if(Input.GetMouseButton(0)){yaw+=Input.GetAxis("Mouse X")*1.2f;pitch=Mathf.Clamp(pitch-Input.GetAxis("Mouse Y")*1.2f,-10,85);}fov=Mathf.Clamp(fov-Input.mouseScrollDelta.y*1.5f,3,60);return true;}
  if(game.life.panel=="arrival"){if(Input.GetKeyDown(KeyCode.Return)||Input.GetKeyDown(KeyCode.Escape))FinishArrival();return true;}
  if(game.life.panel=="fieldguide"){if(Input.GetKeyDown(KeyCode.Escape))game.life.SetPanel("");return true;}
  if(!game.life.PanelOpen&&OdysseyAccessibility.InteractPressed()){
   if(Vector3.Distance(game.player.transform.position,telescope.position)<3.4f){OpenTelescope();return true;}
   if(natureEntry!=Vector3.zero&&Vector3.Distance(game.player.transform.position,natureEntry+new Vector3(5,0,8))<4){game.life.SetPanel("fieldguide");return true;}
  }return false;
 }
 void Update(){UpdateArrival();if(TelescopeOpen&&skyCamera!=null){skyCamera.transform.localRotation=Quaternion.Euler(-pitch,yaw,0);skyCamera.fieldOfView=fov;bool fog=RenderSettings.fog;try{RenderSettings.fog=false;skyCamera.Render();}finally{RenderSettings.fog=fog;}}}
 Vector3 Direction(float az,float alt)=>Quaternion.Euler(-alt,az,0)*Vector3.forward;
 void BuildSky(){sky=new GameObject("Educational sky theatre · not a live ephemeris").transform;sky.position=new Vector3(10000,2000,10000);
  var stars=Mat("Starlight",new Color(.8f,.87f,1));stars.EnableKeyword("_EMISSION");stars.SetColor("_EmissionColor",new Color(2,2.5f,3));var rng=new System.Random(1884);
  for(int i=0;i<750;i++){float az=(float)rng.NextDouble()*360,alt=(float)rng.NextDouble()*140-30;Shape(sky,"Star",PrimitiveType.Sphere,Direction(az,alt)*250,Vector3.one*(.1f+(float)rng.NextDouble()*.45f),stars,false).transform.LookAt(sky.position);}
  var lunar=Mat("NASA LROC lunar surface",Color.white);lunar.mainTexture=Resources.Load<Texture2D>("Presentation/MoonLROC");lunar.SetFloat("_Glossiness",0);
  var moon=Shape(sky,"Moon · NASA Scientific Visualization Studio",PrimitiveType.Sphere,Direction(0,25)*180,Vector3.one*8,lunar,false).transform;moon.localRotation=Quaternion.Euler(0,90,0);
  var saturn=Shape(sky,"Saturn",PrimitiveType.Sphere,Direction(42,32)*180,new Vector3(3,2.7f,3),gold,false).transform;
  var ringMat=Mat("Saturn ring ice",new Color(.7f,.6f,.42f));for(int i=0;i<100;i++){float a=i*Mathf.PI*2/100;Shape(saturn,"Saturn ring",PrimitiveType.Sphere,new Vector3(Mathf.Cos(a)*.85f,0,Mathf.Sin(a)*.85f),new Vector3(.09f,.016f,.29f),ringMat,false).transform.localRotation=Quaternion.Euler(0,-i*3.6f,0);}saturn.localRotation=Quaternion.Euler(28,10,0);
  foreach(var p in new[]{new Vector2(-44,48),new Vector2(-31,47),new Vector2(-39,42),new Vector2(-37,41),new Vector2(-35,40),new Vector2(-43,34),new Vector2(-29,35)})Shape(sky,"Orion bright star",PrimitiveType.Sphere,Direction(p.x,p.y)*200,Vector3.one*.65f,stars,false);
  var light=new GameObject("Sky theatre sunlight").AddComponent<Light>();light.transform.SetParent(sky,false);light.type=LightType.Directional;light.transform.localRotation=Quaternion.Euler(15,-35,0);light.intensity=1.2f;light.cullingMask=1<<28;
  skyCamera=new GameObject("Refractor view").AddComponent<Camera>();skyCamera.transform.SetParent(sky,false);skyCamera.clearFlags=CameraClearFlags.SolidColor;skyCamera.backgroundColor=new Color(.002f,.004f,.012f);skyCamera.cullingMask=1<<28;skyCamera.nearClipPlane=.1f;skyCamera.farClipPlane=400;skyCamera.enabled=false;skyImage=new RenderTexture(1280,800,24);skyCamera.targetTexture=skyImage;
  foreach(var t in sky.GetComponentsInChildren<Transform>())t.gameObject.layer=28;
 }
 void OnGUI(){if(game==null||!game.Ready)return;var old=GUI.matrix;float s=Mathf.Min(Screen.width/1440f,Screen.height/900f);GUI.matrix=Matrix4x4.Scale(Vector3.one*s);float w=Screen.width/s,h=Screen.height/s;
  if(TelescopeOpen){GUI.DrawTexture(new Rect(0,0,w,h),skyImage,ScaleMode.ScaleAndCrop);OdysseyUI.Fill(new Rect(0,0,w,115),new Color(.01f,.015f,.025f,.9f));OdysseyCinematic.Title(new Rect(40,18,w-80,55),"The Observatory",34,Ivory);OdysseyUI.Text(new Rect(40,76,w-80,30),"ALVAN CLARK REFRACTOR  /  AN EDUCATIONAL SKY",16,OdysseyUI.Muted);
   if(OdysseyUI.Button(new Rect(w-340,130,300,42),"Record observation · Space","sky-record"))RecordObservation();
   OdysseyUI.Fill(new Rect(w/2-14,h/2,28,1),Ivory);OdysseyUI.Fill(new Rect(w/2,h/2-14,1,28),Ivory);
   for(int i=0;i<3;i++)if(OdysseyUI.Button(new Rect(40+i*205,h-125,190,42),(i+1)+"  "+Targets[i],"sky-"+i,target==i))Aim(i);
   OdysseyUI.Text(new Rect(40,h-65,w-360,42),"Drag / arrows: aim    Scroll: magnify    Field of view "+fov.ToString("0")+"°    /    Illustrative sky; positions are not live",17,Ivory);if(OdysseyUI.Button(new Rect(w-255,h-75,215,45),"Leave telescope","sky-close"))CloseTelescope();
  }else if(arrival){OdysseyUI.Fill(new Rect(0,0,w,80),Color.black);OdysseyUI.Fill(new Rect(0,h-200,w,200),new Color(.008f,.013f,.014f,.9f));OdysseyCinematic.Title(new Rect(40,h-185,w-80,65),"Whitehouse Nature Center",42,Ivory);OdysseyUI.Text(new Rect(w/2-380,h-112,760,35),"The run ends. A new discovery begins.",23,Ivory);if(OdysseyUI.Button(new Rect(w/2-140,h-62,280,40),"Enter the living classroom","arrival-enter"))FinishArrival();
  }else if(game.life.panel=="fieldguide"){
   OdysseyUI.Fill(new Rect(0,0,w,h),new Color(.015f,.02f,.019f,.97f));OdysseyCinematic.Title(new Rect(80,60,w-160,65),"The Whitehouse Field Journal",40,Ivory);
   string[] entries={"Woodland / Listen to the canopy","Prairie / Follow the pollinators","River & marsh / Watch the water"};string[] descriptions={"Look for layered habitats: the canopy, understory and leaf litter each shelter different life.","Wildflowers support bees and butterflies. Stay on the marked path and leave blooms for wildlife.","Wetlands slow water and provide shelter. Observe from the trail without disturbing reeds or banks."};
   for(int i=0;i<3;i++){float y=190+i*165;OdysseyCinematic.Title(new Rect(100,y,w-200,42),entries[i],27,Ivory);OdysseyUI.Text(new Rect(180,y+55,w-360,50),descriptions[i],22,OdysseyUI.White);if(OdysseyUI.Button(new Rect(w/2-125,y+105,250,38),(journalBits&(1<<i))!=0?"Recorded":"Add to field journal","journal-"+i)){RecordHabitat(i);}}
   if(OdysseyUI.Button(new Rect(w/2-150,h-95,300,48),"Return to the center","journal-close"))game.life.SetPanel("");
  }else if(!game.life.PanelOpen){string hint=Vector3.Distance(game.player.transform.position,telescope.position)<3.4f?"Use the Alvan Clark telescope":natureEntry!=Vector3.zero&&Vector3.Distance(game.player.transform.position,natureEntry+new Vector3(5,0,8))<4?"Speak to the nature guide":"";if(hint.Length>0){OdysseyUI.Fill(new Rect(w/2-300,h-150,600,50),new Color(.02f,.02f,.02f,.9f));OdysseyUI.Text(new Rect(w/2-280,h-142,560,40),OdysseyAccessibility.InteractLabel+"  ·  "+hint,22,Ivory);}}
  GUI.matrix=old;
 }
 void OnDestroy(){foreach(var o in owned)if(o!=null)Destroy(o);if(observatory!=null)Destroy(observatory.gameObject);if(sky!=null)Destroy(sky.gameObject);if(skyImage!=null){skyImage.Release();Destroy(skyImage);}if(arrivalCamera!=null)Destroy(arrivalCamera.gameObject);}
}}
