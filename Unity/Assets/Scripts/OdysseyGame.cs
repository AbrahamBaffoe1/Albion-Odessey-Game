using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace AlbionOdyssey
{
    public sealed class MemoryMarker : MonoBehaviour { public int id; }
    public sealed class GuideMarker : MonoBehaviour {}
    public sealed class PlotMarker : MonoBehaviour { public int cell; }
    public sealed class OdysseyGame : MonoBehaviour
    {
        public OdysseyState state=new OdysseyState();
        public Explorer player;
        public Camera builderCamera;
        public bool building;
        public CampusLife life;
        public CampusExpansion campus;
        public CampusTour tour;
        public CampusShell shell;
        public OdysseyAudio sound;
        public CampusWorldSystems world;
        public OdysseyAccessibility accessibility;
        public CampusOnlineSession online;
        public OdysseyVrSupport vr;
        public OdysseyXRExperience xr;
        public CampusWeather weather;
        public OdysseyRuntimeDiagnostics diagnostics;
        public OdysseyCrashReporter crashReporter;
        public string notice="Meet Pip beside the entrance, or explore Legacy Hall. Aim and press E to interact.";
        readonly List<GameObject> memories=new List<GameObject>();
        GameObject island,beacon;
        int selected=1;
        GUIStyle title,body,small,journalButton;
        public bool journalOpen;
        int journalPage;
        GameObject footprint;
        readonly List<Renderer> footprintEdges=new List<Renderer>();
        string SavePath=>PlaytestMode.Active?Path.Combine(Application.persistentDataPath,"Playtests",PlaytestMode.Name,"save.json"):Path.Combine(Application.persistentDataPath,"albion-unity-v2.json");
        string LoadPath=>File.Exists(SavePath)?SavePath:File.Exists(SavePath+".bak")?SavePath+".bak":Path.Combine(Application.persistentDataPath,"albion-unity-v1.json");
        static readonly string[] Names={"","Garden","Library","Observatory","Hall"};
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if(FindAnyObjectByType<OdysseyGame>()==null)new GameObject("Albion Odyssey").AddComponent<OdysseyGame>();
        }
        void Start()
        {
            Application.runInBackground=PlaytestMode.Active;
            Application.targetFrameRate=60;QualitySettings.antiAliasing=4;QualitySettings.pixelLightCount=8;QualitySettings.shadowDistance=100;QualitySettings.shadows=ShadowQuality.All;
            if(!PlaytestMode.Active&&File.Exists(LoadPath))
            {
                try{var saved=JsonUtility.FromJson<OdysseyState>(File.ReadAllText(LoadPath));saved?.UpgradeLegacySave();if(saved!=null&&saved.Valid()){state=saved;if(LoadPath.EndsWith(".bak"))notice="Recovered the last safe save after an interrupted write.";}else notice="Invalid save ignored. A new session has started.";}
                catch(Exception e){notice="Save could not be loaded: "+e.Message;}
            }
            TowerGeometry.Load();
            crashReporter=gameObject.AddComponent<OdysseyCrashReporter>();crashReporter.Setup(this);
            var guide=new GameObject("Pip the squirrel guide");guide.transform.position=new Vector3(5,1.2f,-15);
            var guideTarget=guide.AddComponent<BoxCollider>();guideTarget.size=new Vector3(.9f,1.5f,.9f);guideTarget.isTrigger=true;guide.AddComponent<GuideMarker>();
            RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.55f,.65f,.78f);
            RenderSettings.ambientEquatorColor=new Color(.32f,.36f,.4f);
            RenderSettings.ambientGroundColor=new Color(.19f,.21f,.22f);
            QualitySettings.shadowCascades=4;QualitySettings.shadowResolution=ShadowResolution.High;
            RenderSettings.fog=true;RenderSettings.fogColor=new Color(.62f,.69f,.73f);RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogDensity=.0008f;
            var sunlight=new GameObject("Afternoon sun").AddComponent<Light>();sunlight.type=LightType.Directional;
            RenderSettings.sun=sunlight;RenderSettings.skybox=Resources.Load<Material>("ArchitectureSky");
            sunlight.intensity=1.3f;sunlight.shadows=LightShadows.Soft;sunlight.transform.rotation=Quaternion.Euler(40,-35,0);
            for(int floor=0;floor<8;floor++)foreach(float z in new[]{-5f,3f})
            {
                var light=new GameObject("Hall light").AddComponent<Light>();light.type=LightType.Spot;light.spotAngle=120;light.transform.rotation=Quaternion.Euler(90,0,0);light.shadows=LightShadows.Soft;
                light.transform.position=new Vector3(0,floor*3.6f+2.7f,z);light.range=7;light.intensity=2.2f;light.color=new Color(1,.89f,.73f);
            }
            for(int floor=0;floor<8;floor++)foreach(float x in new[]{-7f,6f})foreach(float z in new[]{-6f,6f})
            {
                var lamp=new GameObject("Room ceiling light").AddComponent<Light>();lamp.type=LightType.Spot;
                lamp.transform.position=new Vector3(x,floor*3.6f+3.1f,z);lamp.transform.rotation=Quaternion.Euler(90,0,0);
                lamp.spotAngle=140;lamp.range=8;lamp.intensity=2.8f;lamp.color=new Color(1,.94f,.82f);lamp.shadows=LightShadows.Soft;
            }
            var avatar=new GameObject("Keeper - 1.92m character");avatar.transform.position=new Vector3(0,.05f,-22);
            player=avatar.AddComponent<Explorer>();player.body=avatar.AddComponent<CharacterController>();
            player.body.height=1.92f;player.body.radius=.42f;player.body.center=new Vector3(0,.96f,0);player.body.stepOffset=.40f;player.body.skinWidth=.035f;
            var eye=new GameObject("Player eye height 1.65m");eye.transform.SetParent(avatar.transform,false);eye.transform.localPosition=new Vector3(0,1.65f,0);
            player.eyes=eye.AddComponent<Camera>();player.eyes.nearClipPlane=.05f;player.eyes.farClipPlane=300;player.eyes.fieldOfView=80;
            player.eyes.clearFlags=CameraClearFlags.Skybox;player.eyes.backgroundColor=new Color(.44f,.56f,.69f);eye.AddComponent<AudioListener>();
            builderCamera=new GameObject("Campus design camera").AddComponent<Camera>();builderCamera.transform.position=new Vector3(65,32,-22);
            builderCamera.transform.LookAt(new Vector3(48,0,0));builderCamera.orthographic=true;builderCamera.orthographicSize=20;builderCamera.enabled=false;
            builderCamera.clearFlags=CameraClearFlags.Skybox;builderCamera.backgroundColor=new Color(.14f,.20f,.27f);
            OdysseyStory.Refresh(state);RefreshMemories();RebuildCampus();CreateFootprint();Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;
            sound=gameObject.AddComponent<OdysseyAudio>();sound.Initialize(state);
            life=gameObject.AddComponent<CampusLife>();life.Setup(this);
            campus=gameObject.AddComponent<CampusExpansion>();campus.Setup(this);
            tour=gameObject.AddComponent<CampusTour>();tour.Setup(this);
            shell=gameObject.AddComponent<CampusShell>();shell.Setup(this);
            gameObject.AddComponent<CampusBuildings>().Setup(this);
            weather=gameObject.AddComponent<CampusWeather>();weather.Setup(this);
            world=gameObject.AddComponent<CampusWorldSystems>();world.Setup(this);
            accessibility=gameObject.AddComponent<OdysseyAccessibility>();accessibility.Setup(this);
            online=gameObject.AddComponent<CampusOnlineSession>();online.Setup(this);
            vr=gameObject.AddComponent<OdysseyVrSupport>();vr.Setup(this);
            xr=gameObject.AddComponent<OdysseyXRExperience>();xr.Setup(this);
            diagnostics=gameObject.AddComponent<OdysseyRuntimeDiagnostics>();diagnostics.Setup(this);
            gameObject.AddComponent<CampusHud>().Setup(this);
            gameObject.AddComponent<WorldTextDepth>();
            Debug.Log("ODYSSEY_READY: Blender tower, authored collision boxes, first-person controller and campus builder initialized.");
        }
        void Update()
        {
            if(player==null)return;
            if(accessibility!=null&&accessibility.HandleInput())return;
            if(online!=null&&online.HandleInput())return;
            if(vr!=null&&vr.HandleInput())return;
            if(shell!=null&&shell.HandleInput())return;
            if(weather!=null&&weather.HandleInput())return;
            if(CampusBuildings.Instance!=null&&CampusBuildings.Instance.HandleInput())return;
            if(tour!=null&&tour.HandleInput())return;
            if(campus!=null&&campus.HandleInput())return;
            if(life!=null&&life.HandleInput())return;
            if(Input.GetKeyDown(KeyCode.J)){SetJournal(!journalOpen);}
            if(journalOpen)
            {
                if(Input.GetKeyDown(KeyCode.Escape))SetJournal(false);
                if(Input.GetKeyDown(KeyCode.LeftArrow))journalPage=(journalPage+11)%12;
                if(Input.GetKeyDown(KeyCode.RightArrow))journalPage=(journalPage+1)%12;
                return;
            }
            if(Input.GetKeyDown(KeyCode.F2))ToggleMode();
            if(Input.GetKeyDown(KeyCode.Tab)){state.active=(state.active+1)%4;Save();RefreshMemories();RebuildCampus();life.RefreshRoster();notice="Keeper "+(state.active+1)+" — your collection and personal campus.";}
            if(Input.GetKeyDown(KeyCode.C))
            {
                notice=state.Contribute()?"Two acorns added to the shared Beacon.":state.beacon>=24?"The shared Beacon is complete!":"You need two acorns to contribute.";
                Save();RebuildCampus();
            }
            if(building)
            {
                UpdateFootprint();
                builderCamera.orthographicSize=Mathf.Clamp(builderCamera.orthographicSize-Input.mouseScrollDelta.y,12,30);
                for(int i=1;i<=4;i++)if(Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha0+i)))selected=i;
                if(Input.GetKeyDown(KeyCode.T)){state.Current.style=1-state.Current.style;Save();RebuildCampus();}
                float uiScale=Mathf.Min(Screen.width/1280f,Screen.height/800f);
                if(Input.mousePosition.y>115*uiScale&&Input.mousePosition.y<Screen.height-125*uiScale&&(Input.GetMouseButtonDown(0)||Input.GetMouseButtonDown(1)))
                {
                    if(Physics.Raycast(builderCamera.ScreenPointToRay(Input.mousePosition),out var hit,200))
                    {
                        var plot=hit.collider.GetComponent<PlotMarker>();
                        if(plot!=null)
                        {
                            bool reclaim=Input.GetMouseButtonDown(1);
                            bool ok=reclaim?state.Reclaim(plot.cell):state.Build(plot.cell,selected);
                            notice=ok?(reclaim?"Building reclaimed. Full acorn refund.":Names[selected]+" built."):"Plot occupied, course assigned, or not enough acorns. Remove a building’s course before reclaiming it.";
                            if(ok){Save();RebuildCampus();}
                        }
                    }
                }
            }
            else if((Input.GetKeyDown(KeyCode.E)||Input.GetKeyDown(KeyCode.F))&&(Cursor.lockState==CursorLockMode.Locked||player.pointerControls))Interact();
            foreach(var m in memories)if(m!=null)m.transform.Rotate(0,30*Time.deltaTime,0);
        }
        public void Interact()
        {
            if(building||journalOpen||(life!=null&&life.PanelOpen))return;
            if(campus!=null&&campus.TryInteract())return;
            if(player.TryTarget(out var hit))
            {
                var activity=hit.collider.GetComponentInParent<CampusActivityStation>();
                if(activity!=null){activity.Activate(this);return;}
                if(hit.collider.GetComponent<GuideMarker>()!=null)
                {int next=OdysseyStory.Next(state.Current);notice="Pip: "+(next<6?OdysseyStory.Hints[next]:"Your charter is complete! Visit the classroom and share what you have learned.");SetJournal(true);return;}
                var memory=hit.collider.GetComponent<MemoryMarker>();
                if(memory!=null&&state.Collect(memory.id)){notice=OdysseyStory.Titles[memory.id]+" discovered. +3 acorns. Press J to read your journal.";Save();RefreshMemories();return;}
            }
            notice="Move closer and aim at a golden memory or Pip, then press E or F.";
        }
        public void ToggleMode()
        {
            if(!player.TryExitVehicle()){notice="Move the car into an open space before building.";return;}
            if(journalOpen)SetJournal(false);
            building=!building;
            if(footprint!=null)footprint.SetActive(false);player.controls=!building;player.eyes.enabled=!building;builderCamera.enabled=building;
            Cursor.lockState=building||player.pointerControls?CursorLockMode.None:CursorLockMode.Locked;Cursor.visible=building||player.pointerControls;
            notice=building?"Choose 1–4 and click an empty tile. Your tower discoveries fund this campus.":"Back at Legacy Hall. Explore the floors and collect memories.";
        }
        public bool Save()
        {
            if(!state.Valid()){notice="Save rejected: invalid game state.";return false;}
            OdysseyStory.Refresh(state);
            if(sound!=null)sound.Observe(state);
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SavePath));
                string temp=SavePath+".tmp";File.WriteAllText(temp,JsonUtility.ToJson(state,true));
                if(File.Exists(SavePath))File.Replace(temp,SavePath,SavePath+".bak");else File.Move(temp,SavePath);
                return true;
            }
            catch(Exception e){notice="Save failed; session progress remains in memory. "+e.Message;return false;}
        }
        void RefreshMemories()
        {
            foreach(var m in memories)Destroy(m);memories.Clear();
            var gold=TowerGeometry.Material("Memory gold",new Color(1,.66f,.12f),.35f,.7f);
            gold.EnableKeyword("_EMISSION");gold.SetColor("_EmissionColor",new Color(1,.4f,.03f)*1.2f);
            for(int i=0;i<12;i++)
            {
                if((state.Current.memories&(1<<i))!=0)continue;
                Vector3 pos=i<8?new Vector3(0,i*3.6f+1.15f,3):new Vector3((i-9.5f)*4,1.15f,-18);
                var orb=GameObject.CreatePrimitive(PrimitiveType.Sphere);orb.name="Memory "+(i+1);orb.transform.position=pos;orb.transform.localScale=Vector3.one*.45f;
                orb.GetComponent<Renderer>().sharedMaterial=gold;orb.AddComponent<MemoryMarker>().id=i;orb.AddComponent<OdysseyXRGrabTarget>();memories.Add(orb);
            }
        }
        GameObject Piece(PrimitiveType type,string name,Vector3 pos,Vector3 scale,Material mat,int cell=-1)
        {
            var o=GameObject.CreatePrimitive(type);o.name=name;o.transform.SetParent(island.transform,false);o.transform.position=pos;o.transform.localScale=scale;
            o.GetComponent<Renderer>().sharedMaterial=mat;if(cell>=0)o.AddComponent<PlotMarker>().cell=cell;return o;
        }
        public void RebuildCampus()
        {
            if(island!=null){island.SetActive(false);Destroy(island);}island=new GameObject("Keeper's personal campus");
            bool fantasy=state.Current.style==1;
            var grass=TowerGeometry.Material(fantasy?"Fantasy plot":"Campus plot",fantasy?new Color(.12f,.29f,.29f):new Color(.29f,.4f,.18f));
            var wall=TowerGeometry.Material(fantasy?"Fantasy facade":"Campus facade",fantasy?new Color(.42f,.25f,.58f):new Color(.5f,.22f,.13f));
            var stone=TowerGeometry.Material("Builder stone",new Color(.75f,.7f,.57f));
            var roof=TowerGeometry.Material("Builder copper",new Color(.14f,.29f,.28f),.4f,.5f);
            for(int i=0;i<49;i++)
            {
                var p=new Vector3(48+(i%7-3)*4.2f,0,(i/7-3)*4.2f);
                Piece(PrimitiveType.Cube,"Plot "+i,p,new Vector3(4.05f,.15f,4.05f),grass,i);
                int kind=state.Current.plots[i];if(kind==0)continue;
                if(kind==1)
                {
                    Piece(PrimitiveType.Cylinder,"Garden pedestal",p+Vector3.up*.25f,new Vector3(2.7f,.2f,2.7f),stone,i);
                    Piece(PrimitiveType.Cylinder,"Garden trunk",p+Vector3.up*.8f,new Vector3(.25f,.65f,.25f),wall,i);
                    Piece(PrimitiveType.Sphere,"Garden crown",p+Vector3.up*1.8f,Vector3.one*1.7f,grass,i);
                }
                else
                {
                    float height=kind==4?4.2f:kind==2?2.6f:3f;
                    Piece(PrimitiveType.Cube,Names[kind],p+Vector3.up*(height/2),new Vector3(2.9f,height,2.8f),wall,i);
                    Piece(kind==3?PrimitiveType.Sphere:PrimitiveType.Cube,"Roof",p+Vector3.up*(height+.15f),new Vector3(3.1f,kind==3?1.7f:.3f,3f),roof,i);
                    for(float x=-.8f;x<1;x+=.8f)Piece(PrimitiveType.Cube,"Window",p+new Vector3(x,height*.6f,-1.42f),new Vector3(.4f,.65f,.05f),stone,i);
                }
            }
            float h=1+state.beacon*.15f;
            beacon=Piece(PrimitiveType.Cylinder,"Shared Beacon",new Vector3(48,h/2,18),new Vector3(1,h/2,1),stone);
            var starMaterial=state.beacon==24?TowerGeometry.Material("Awakened Beacon",new Color(1,.75f,.22f),.2f,.6f):roof;
            if(state.beacon==24){starMaterial.EnableKeyword("_EMISSION");starMaterial.SetColor("_EmissionColor",new Color(1,.55f,.08f)*2);}
            var star=Piece(PrimitiveType.Sphere,"Beacon star",new Vector3(48,h+.5f,18),Vector3.one*1.4f,starMaterial);
            if(state.beacon==24){var glow=star.AddComponent<Light>();glow.type=LightType.Point;glow.color=new Color(1,.72f,.3f);glow.range=14;glow.intensity=2;}
        }
        public void SetJournal(bool open)
        {
            journalOpen=open;player.controls=!open&&!building;
            if(footprint!=null)footprint.SetActive(false);
            Cursor.lockState=open||building||player.pointerControls?CursorLockMode.None:CursorLockMode.Locked;
            Cursor.visible=open||building||player.pointerControls;
        }
        void CreateFootprint()
        {
            footprint=new GameObject("Building footprint preview");
            for(int i=0;i<4;i++)
            {
                var edge=GameObject.CreatePrimitive(PrimitiveType.Cube);edge.transform.SetParent(footprint.transform,false);
                edge.transform.localPosition=i<2?new Vector3(i==0?-1.95f:1.95f,0,0):new Vector3(0,0,i==2?-1.95f:1.95f);
                edge.transform.localScale=i<2?new Vector3(.10f,.05f,4):new Vector3(4,.05f,.10f);
                edge.GetComponent<Collider>().enabled=false;var renderer=edge.GetComponent<Renderer>();renderer.shadowCastingMode=ShadowCastingMode.Off;footprintEdges.Add(renderer);
            }
            footprint.SetActive(false);
        }
        void UpdateFootprint()
        {
            float scale=Mathf.Min(Screen.width/1280f,Screen.height/800f);
            if(Input.mousePosition.y<115*scale||Input.mousePosition.y>Screen.height-125*scale){footprint.SetActive(false);return;}
            if(!Physics.Raycast(builderCamera.ScreenPointToRay(Input.mousePosition),out var hit,200)){footprint.SetActive(false);return;}
            var plot=hit.collider.GetComponent<PlotMarker>();if(plot==null){footprint.SetActive(false);return;}
            bool valid=state.Current.plots[plot.cell]==0&&state.Current.acorns>=OdysseyState.Cost(selected);
            footprint.transform.position=new Vector3(48+(plot.cell%7-3)*4.2f,.14f,(plot.cell/7-3)*4.2f);
            var material=TowerGeometry.Material(valid?"Valid footprint":"Blocked footprint",valid?new Color(.3f,.95f,.55f):new Color(1,.25f,.18f));
            foreach(var edge in footprintEdges)edge.sharedMaterial=material;
            footprint.SetActive(true);
        }
        void DrawJournal(float width,float height)
        {
            float w=Mathf.Min(1000,width-50),h=Mathf.Min(650,height-40),x=(width-w)/2,y=(height-h)/2;
            GUI.color=new Color(0,0,0,.75f);GUI.DrawTexture(new Rect(0,0,width,height),Texture2D.whiteTexture);
            GUI.color=new Color(.024f,.037f,.052f,1);GUI.DrawTexture(new Rect(x,y,w,h),Texture2D.whiteTexture);GUI.color=Color.white;
            if(journalButton==null)
            {
                journalButton=new GUIStyle(GUI.skin.button){fontSize=14,alignment=TextAnchor.MiddleLeft,padding=new RectOffset(12,8,0,0),border=new RectOffset()};
                foreach(var appearance in new[]{journalButton.normal,journalButton.hover,journalButton.active,journalButton.focused}){appearance.background=Texture2D.whiteTexture;appearance.textColor=Color.white;}
            }
            GUI.Label(new Rect(x+24,y+18,w-130,40),"THE KEEPER'S JOURNAL",title);
            GUI.backgroundColor=new Color(.1f,.16f,.19f);
            if(GUI.Button(new Rect(x+w-115,y+22,91,30),"Close · J",journalButton))SetJournal(false);
            GUI.backgroundColor=Color.white;
            GUI.Label(new Rect(x+24,y+65,w-48,26),"Original game fiction · Your discoveries and campus charter",small);
            float column=w*.35f;
            for(int i=0;i<12;i++)
            {
                bool found=(state.Current.memories&(1<<i))!=0;
                GUI.backgroundColor=i==journalPage?new Color(.13f,.24f,.26f):new Color(.042f,.063f,.078f);
                if(GUI.Button(new Rect(x+24,y+106+i*32,column-36,28),(found?"● ":"○ ")+(i+1).ToString("00")+"  "+(found?OdysseyStory.Titles[i]:"Undiscovered memory"),journalButton))journalPage=i;
                GUI.backgroundColor=Color.white;
            }
            float rx=x+column+14,rw=w-column-40;
            bool collected=(state.Current.memories&(1<<journalPage))!=0;
            GUI.Label(new Rect(rx,y+106,rw,50),collected?OdysseyStory.Titles[journalPage]:"A memory is waiting",new GUIStyle(title){fontSize=23,wordWrap=true});
            GUI.Label(new Rect(rx,y+163,rw,30),journalPage<8?"FLOOR "+(journalPage+1)+" · "+OdysseyStory.Floors[journalPage]:"OUTSIDE · Entrance plaza",small);
            GUI.Label(new Rect(rx,y+204,rw,160),collected?OdysseyStory.Entries[journalPage]:"Find the golden memory at this location, aim at it and press E. Every Keeper can make their own discoveries.",new GUIStyle(body){wordWrap=true});
            GUI.Label(new Rect(rx,y+370,rw,28),"YOUR CHARTER",body);
            for(int i=0;i<6;i++)GUI.Label(new Rect(rx,y+407+i*26,rw,25),((state.Current.milestones&(1<<i))!=0?"✓  ":"○  ")+OdysseyStory.Chapters[i],small);
            int next=OdysseyStory.Next(state.Current);
            if(next<6)GUI.Label(new Rect(x+24,y+h-45,w-48,40),OdysseyStory.Hints[next],small);
        }
        void OnGUI()
        {
            if(player==null)return;
            if(title==null){title=new GUIStyle(GUI.skin.label){fontSize=27,fontStyle=FontStyle.Bold};body=new GUIStyle(GUI.skin.label){fontSize=17};small=new GUIStyle(GUI.skin.label){fontSize=14,wordWrap=true};}
            float scale=Mathf.Min(Screen.width/1280f,Screen.height/800f);GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));
            float width=Screen.width/scale,height=Screen.height/scale;
            if(life!=null&&life.PanelOpen)return;
            if(journalOpen){DrawJournal(width,height);return;}
            if(!building)return;
            GUI.color=new Color(.035f,.05f,.08f,.94f);GUI.DrawTexture(new Rect(16,16,Mathf.Min(width-32,850),108),Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(0,height-115,width,115),Texture2D.whiteTexture);GUI.color=Color.white;
            GUI.Label(new Rect(32,25,900,40),"ALBION ODYSSEY  /  "+(building?"YOUR LEGACY CAMPUS":campus!=null&&campus.OnCampus?"ALBION COLLEGE":life!=null?life.Location:"LEGACY HALL"),title);
            GUI.Label(new Rect(32,68,900,30),$"{(campus!=null?campus.keeperName:"KEEPER "+(state.active+1))}    {state.Current.acorns} ACORNS    {OdysseyState.Count(state.Current.memories)}/12 MEMORIES    BEACON {state.beacon}/24",body);
            GUI.Label(new Rect(32,96,800,23),tour!=null&&tour.InRoom?"Wesley · room study (approximate dimensions)":campus!=null&&campus.OnCampus&&!building?campus.Nearest.name+" · "+campus.CountFound()+"/7 discoveries" : building?"Personal campus · "+(state.Current.style==1?"Fantasy":"Campus"):$"Eight-storey Blender building · Floor {Mathf.Clamp(Mathf.FloorToInt((player.transform.position.y+.1f)/3.6f)+1,1,8)}",small);
            GUI.Label(new Rect(24,height-105,width-48,30),notice,small);
            GUI.Label(new Rect(24,height-81,width-48,25),"CHARTER  /  "+OdysseyStory.Objective(state.Current),small);
            GUI.Label(new Rect(24,height-55,width-48,30),building?$"1 Garden (2)   2 Library (4)   3 Observatory (6)   4 Hall (3)    Selected: {Names[selected]}":"WASD / ARROWS move   MOUSE look   SHIFT run   SPACE jump / brake   E interact / enter / exit   V camera",small);
            GUI.Label(new Rect(24,height-30,width-48,28),"M map    H history    G building stories    K courses    J journal    F2 build    TAB Keeper    C Beacon    Y snow    ESC help"+(building?"    T appearance    CLICK build    RIGHT-CLICK reclaim":"    Click to capture mouse"),small);
            if(!building&&!journalOpen){GUI.Label(new Rect(width/2-5,height/2-12,20,24),"+",body);}
        }
    }
}
