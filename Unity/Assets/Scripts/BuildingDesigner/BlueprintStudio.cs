using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AlbionOdyssey.BuildingDesigner
{
    [DefaultExecutionOrder(-2000)]
    public sealed class BlueprintStudio : MonoBehaviour
    {
        public static bool Smoke=>Array.IndexOf(Environment.GetCommandLineArgs(),"-blueprintSmoke")>=0;
        OdysseyGame game;
        BuildingBlueprint blueprint;
        readonly BlueprintHistory history=new BlueprintHistory();
        readonly List<Behaviour> suspended=new List<Behaviour>();
        readonly List<Camera> cameras=new List<Camera>();
        readonly List<AudioListener> listeners=new List<AudioListener>();
        GameObject building,walker,preview;
        CharacterController controller;
        Camera camera;
        AudioSource effects;
        AudioClip[] clips=new AudioClip[10];
        public bool Active {get;private set;}
        public bool Ready=>game!=null;
        bool walking,showRoof,share,dirty,editingName,blurNextGui;
        int keeper,slot,rotation;StudioPart selected;
        float previousTimeScale,azimuth=35,pitch,velocity;
        Vector3 returnCameraPosition;Quaternion returnCameraRotation;
        string message="Choose a piece, then click the grid. Right-click removes the selected layer.",card="";
        Vector2 cardScroll;
        GUIStyle heading,text,small,button;
        const string FolderName="BuildingBlueprints";
        string DirectoryPath=>Path.Combine(Application.persistentDataPath,PlaytestMode.Active?"BlueprintTest-"+PlaytestMode.Name:FolderName);
        string SavePath=>Path.Combine(DirectoryPath,$"keeper-{keeper+1}-slot-{slot+1}.json");
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot(){new GameObject("Independent building designer").AddComponent<BlueprintStudio>();}
        IEnumerator Start()
        {
            yield return null;yield return null;game=FindAnyObjectByType<OdysseyGame>();
            for(int i=0;i<10;i++)clips[i]=Resources.Load<AudioClip>("BuildingDesigner/Studio"+i);
            if(Smoke)yield return RunSmoke();
        }
        public bool Enter()
        {
            if(game==null||Active)return false;
            if(!game.player.TryExitVehicle()){game.notice="Park the car in an open space before entering the studio.";return false;}
            game.tour.StopMedia();
            if(game.life.PanelOpen)game.life.SetPanel("launch");
            keeper=Smoke?0:game.state.active;Active=true;previousTimeScale=Time.timeScale;
            foreach(var c in FindObjectsByType<Camera>())if(c.enabled){cameras.Add(c);c.enabled=false;}
            foreach(var l in FindObjectsByType<AudioListener>())if(l.enabled){listeners.Add(l);l.enabled=false;}
            foreach(var b in FindObjectsByType<MonoBehaviour>())if(b!=this&&b.enabled){suspended.Add(b);b.enabled=false;}
            Time.timeScale=0;
            var cameraObject=new GameObject("Blueprint studio camera");camera=cameraObject.AddComponent<Camera>();cameraObject.AddComponent<AudioListener>();
            camera.orthographic=true;camera.orthographicSize=22;camera.nearClipPlane=.03f;camera.farClipPlane=100;camera.clearFlags=CameraClearFlags.Skybox;
            effects=cameraObject.AddComponent<AudioSource>();effects.playOnAwake=false;effects.spatialBlend=0;effects.volume=Smoke?0:game.sound.Muted?0:game.sound.Volume;
            preview=GameObject.CreatePrimitive(PrimitiveType.Cube);preview.name="Placement outline";Destroy(preview.GetComponent<Collider>());
            LoadSlot();FrameBuilding();Cursor.lockState=CursorLockMode.None;Cursor.visible=true;return true;
        }
        public void Leave()
        {
            if(!Active)return;
            if((!invalidSlot||dirty)&&!SaveBlueprint())return;
            if(camera!=null){camera.gameObject.SetActive(false);camera.transform.SetParent(null,true);Destroy(camera.gameObject);}
            if(walker!=null)Destroy(walker);if(building!=null){building.SetActive(false);Destroy(building);}if(preview!=null)Destroy(preview);
            foreach(var b in suspended)if(b!=null)b.enabled=true;foreach(var c in cameras)if(c!=null)c.enabled=true;foreach(var l in listeners)if(l!=null)l.enabled=true;
            suspended.Clear();cameras.Clear();listeners.Clear();Time.timeScale=previousTimeScale;Active=false;walking=false;
            bool free=game.building||game.journalOpen||game.life.PanelOpen||game.player.pointerControls;Cursor.lockState=free?CursorLockMode.None:CursorLockMode.Locked;Cursor.visible=free;
        }
        void LoadSlot()
        {
            invalidSlot=false;blueprint=BuildingBlueprint.Classroom();
            if(File.Exists(SavePath))
            {
                try{var content=File.ReadAllText(SavePath);var loaded=ParseCard(content);if(loaded==null)throw new InvalidDataException("Invalid layout");blueprint=loaded;message="Loaded your saved blueprint.";}
                catch(Exception){invalidSlot=true;message="This slot could not be loaded. The original file is preserved; choose another slot to save.";dirty=false;Rebuild();history.Clear();return;}
            }
            dirty=false;history.Clear();Rebuild();
        }
        bool invalidSlot;
        public bool SaveBlueprint()
        {
            if(invalidSlot){message="Choose another slot to preserve the unreadable file.";return false;}
            if(!blueprint.Valid()){message="Give the blueprint a name of 1–40 characters before saving.";return false;}
            try
            {
                Directory.CreateDirectory(DirectoryPath);string temp=SavePath+".tmp";File.WriteAllText(temp,JsonUtility.ToJson(blueprint));
                if(File.Exists(SavePath))File.Replace(temp,SavePath,SavePath+".bak");else File.Move(temp,SavePath);
                if(dirty)Play(9);dirty=false;message="Blueprint saved for Keeper "+(keeper+1)+", slot "+(slot+1)+".";return true;
            }
            catch(Exception e){message="Save failed. Your draft is still open: "+e.Message;return false;}
        }
        public static BuildingBlueprint ParseCard(string data)
        {
            if(string.IsNullOrWhiteSpace(data)||data.Length>20000)return null;
            try{var b=JsonUtility.FromJson<BuildingBlueprint>(data);return b!=null&&b.Valid()?b:null;}catch{return null;}
        }
        void Rebuild()
        {if(building!=null){building.SetActive(false);Destroy(building);}building=StudioGeometry.Build(blueprint,showRoof||walking);Physics.SyncTransforms();}
        void FrameBuilding()
        {camera.transform.position=StudioGeometry.Origin+Quaternion.Euler(0,azimuth,0)*new Vector3(0,30,-30);camera.transform.LookAt(StudioGeometry.Origin+Vector3.up);}
        void Play(int kind){if(clips[kind]!=null)effects.PlayOneShot(clips[kind]);}
        void Update()
        {
            if(game==null||Smoke)return;
            if(!share&&!editingName&&Input.GetKeyDown(KeyCode.F4)){if(Active)Leave();else Enter();return;}
            if(!Active)return;
            if(Input.GetKeyDown(KeyCode.Escape)){if(walking)StopWalk();else if(share)share=false;else Leave();return;}
            if(walking){WalkInput();return;}
            if(share||invalidSlot)return;
            float interfaceScale=Mathf.Min(Screen.width/1280f,Screen.height/800f);
            if(editingName)
            {
                if(Input.GetMouseButtonDown(0)&&Input.mousePosition.y>140*interfaceScale&&Input.mousePosition.y<Screen.height-190*interfaceScale){editingName=false;blurNextGui=true;}
                else return;
            }
            if(Input.GetKey(KeyCode.LeftControl)||Input.GetKey(KeyCode.RightControl)||Input.GetKey(KeyCode.LeftCommand)||Input.GetKey(KeyCode.RightCommand))
            {if(Input.GetKeyDown(KeyCode.Z))Undo();if(Input.GetKeyDown(KeyCode.Y))Redo();return;}

            for(int i=0;i<8;i++)if(Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1+i)))selected=(StudioPart)i;
            if(Input.GetKeyDown(KeyCode.R))rotation=(rotation+1)%4;
            if(Input.GetKey(KeyCode.Q)){azimuth-=60*Time.unscaledDeltaTime;FrameBuilding();}
            if(Input.GetKey(KeyCode.E)){azimuth+=60*Time.unscaledDeltaTime;FrameBuilding();}
            camera.orthographicSize=Mathf.Clamp(camera.orthographicSize-Input.mouseScrollDelta.y,14,32);
            float scale=Mathf.Min(Screen.width/1280f,Screen.height/800f);
            bool inside=Input.mousePosition.y>140*scale&&Input.mousePosition.y<Screen.height-190*scale;
            if(!inside){preview.SetActive(false);return;}
            if(Target(out int x,out int z,out bool alongX,out var center))
            {
                var candidate=blueprint.Copy();bool erase=Input.GetMouseButton(1);bool valid=candidate.Place(selected,x,z,alongX,rotation,erase);
                preview.SetActive(true);preview.transform.position=center+Vector3.up*.16f;preview.transform.rotation=Quaternion.identity;
                bool edge=selected>=StudioPart.Wall&&selected<=StudioPart.Window;
                preview.transform.localScale=edge?(alongX?new Vector3(1.95f,.06f,.20f):new Vector3(.20f,.06f,1.95f)):new Vector3(1.90f,.06f,1.90f);
                preview.GetComponent<Renderer>().sharedMaterial=TowerGeometry.Material(valid?"Studio valid preview":"Studio blocked preview",valid?new Color(.30f,.88f,.52f):new Color(.9f,.30f,.24f));
                if(Input.GetMouseButtonDown(0)||Input.GetMouseButtonDown(1))
                {
                    blurNextGui=true;
                    if(valid){history.Record(blueprint);blueprint=candidate;dirty=true;Rebuild();Play(erase?8:(int)selected);message=erase?"Piece removed. Undo restores it.":selected+" placed. R rotates furniture.";}
                    else message="Place a supporting floor first, choose a different space, or use another layer.";
                }
            }
            else preview.SetActive(false);
        }
        bool Target(out int x,out int z,out bool alongX,out Vector3 center)
        {
            x=z=0;alongX=true;center=Vector3.zero;var ray=camera.ScreenPointToRay(Input.mousePosition);
            if(!new Plane(Vector3.up,StudioGeometry.Origin).Raycast(ray,out float distance))return false;
            var p=ray.GetPoint(distance)-StudioGeometry.Origin;float gx=p.x/2+6,gz=p.z/2+6;
            if(gx<0||gx>12||gz<0||gz>12)return false;
            bool edge=selected>=StudioPart.Wall&&selected<=StudioPart.Window;
            if(edge)
            {
                alongX=Mathf.Abs(gz-Mathf.Round(gz))<=Mathf.Abs(gx-Mathf.Round(gx));
                x=alongX?Mathf.Clamp(Mathf.FloorToInt(gx),0,11):Mathf.RoundToInt(gx);z=alongX?Mathf.RoundToInt(gz):Mathf.Clamp(Mathf.FloorToInt(gz),0,11);
                center=StudioGeometry.Origin+new Vector3((x-6)*2+(alongX?1:0),0,(z-6)*2+(alongX?0:1));return BuildingBlueprint.Edge(alongX,x,z);
            }
            x=Mathf.FloorToInt(gx);z=Mathf.FloorToInt(gz);center=StudioGeometry.Origin+StudioGeometry.CellCenter(x,z);return BuildingBlueprint.Cell(x,z);
        }
        void Undo(){blueprint=history.Undo(blueprint);dirty=true;Rebuild();message="Undo.";}
        void Redo(){blueprint=history.Redo(blueprint);dirty=true;Rebuild();message="Redo.";}
        public void StartWalk()
        {
            if(walking)return;walking=true;share=false;preview.SetActive(false);returnCameraPosition=camera.transform.position;returnCameraRotation=camera.transform.rotation;
            walker=new GameObject("Blueprint walkthrough Keeper");walker.transform.position=StudioGeometry.Origin+new Vector3(-1,.15f,-7);controller=walker.AddComponent<CharacterController>();controller.height=1.8f;controller.radius=.32f;controller.center=Vector3.up*.9f;controller.stepOffset=.3f;
            camera.orthographic=false;camera.fieldOfView=75;camera.transform.SetParent(walker.transform,false);camera.transform.localPosition=Vector3.up*1.65f;camera.transform.localRotation=Quaternion.identity;pitch=velocity=0;
            Rebuild();Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;message="Walk through your doorway. Esc returns to the designer.";
        }
        void WalkInput()
        {
            walker.transform.Rotate(0,Input.GetAxisRaw("Mouse X")*2,0);pitch=Mathf.Clamp(pitch-Input.GetAxisRaw("Mouse Y")*2,-80,80);camera.transform.localRotation=Quaternion.Euler(pitch,0,0);
            float x=(Input.GetKey(KeyCode.D)||Input.GetKey(KeyCode.RightArrow)?1:0)-(Input.GetKey(KeyCode.A)||Input.GetKey(KeyCode.LeftArrow)?1:0);
            float z=(Input.GetKey(KeyCode.W)||Input.GetKey(KeyCode.UpArrow)?1:0)-(Input.GetKey(KeyCode.S)||Input.GetKey(KeyCode.DownArrow)?1:0);
            if(controller.isGrounded&&velocity<0)velocity=-2;if(controller.isGrounded&&Input.GetKeyDown(KeyCode.Space))velocity=5;
            velocity-=18*Time.unscaledDeltaTime;controller.Move((Vector3.ClampMagnitude(walker.transform.right*x+walker.transform.forward*z,1)*3.5f+Vector3.up*velocity)*Time.unscaledDeltaTime);
            if(walker.transform.position.y< -5){controller.enabled=false;walker.transform.position=StudioGeometry.Origin+new Vector3(-1,.15f,-7);controller.enabled=true;velocity=0;}
        }
        public void StopWalk()
        {
            walking=false;camera.transform.SetParent(null,true);camera.orthographic=true;camera.transform.position=returnCameraPosition;camera.transform.rotation=returnCameraRotation;Destroy(walker);Rebuild();Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
        }
        bool Button(float x,float y,float w,string value)=>GUI.Button(new Rect(x,y,w,36),value,button);
        void Label(float x,float y,float w,float h,string value,GUIStyle style=null)=>GUI.Label(new Rect(x,y,w,h),value,style??text);
        void OnGUI()
        {
            if(!Active)return;
            if(blurNextGui){GUI.FocusControl(null);blurNextGui=false;}
            if(heading==null){heading=new GUIStyle(GUI.skin.label){fontSize=28,fontStyle=FontStyle.Bold};text=new GUIStyle(GUI.skin.label){fontSize=16,wordWrap=true,richText=false};small=new GUIStyle(text){fontSize=13};button=new GUIStyle(GUI.skin.button){fontSize=14,richText=false};foreach(var v in new[]{button.normal,button.active,button.hover,button.focused}){v.background=Texture2D.whiteTexture;v.textColor=Color.white;}}
            float scale=Mathf.Min(Screen.width/1280f,Screen.height/800f);GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));float w=Screen.width/scale,h=Screen.height/scale;
            GUI.backgroundColor=new Color(.22f,.10f,.34f);GUI.color=new Color(.025f,.04f,.055f,.96f);GUI.DrawTexture(new Rect(0,0,w,walking?85:190),Texture2D.whiteTexture);GUI.DrawTexture(new Rect(0,h-140,w,140),Texture2D.whiteTexture);GUI.color=Color.white;
            Label(24,18,850,40,walking?"WALK INSIDE YOUR DESIGN":"THE BUILDING STUDIO",heading);
            Label(24,60,1000,25,$"Keeper {keeper+1} · Slot {slot+1} · 24 × 24 metres · Original player blueprint",small);
            if(walking){Label(24,h-120,w-48,50,"WASD / arrows walk · Mouse look · Space jump · Esc back to design · Esc twice returns to campus",text);return;}
            if(Button(w-200,22,175,"Save & return · Esc"))Leave();
            GUI.enabled=!invalidSlot;
            string[] names={"1 Floor","2 Wall","3 Door","4 Window","5 Roof","6 Table","7 Chair","8 Planter"};
            for(int i=0;i<8;i++)if(Button(24+i*153,110,145,(selected==(StudioPart)i?"● ":"")+names[i])){selected=(StudioPart)i;GUI.FocusControl(null);}
            Label(24,157,w-48,28,"Walls, doors and windows snap to floor edges. Other pieces snap to floor cells. Right-click removes the selected layer.",small);
            GUI.SetNextControlName("BlueprintName");string name=GUI.TextField(new Rect(24,h-119,260,34),blueprint.title,40);if(name!=blueprint.title){blueprint.title=name;dirty=true;}
            if(Button(300,h-120,100,"Save"))SaveBlueprint();if(Button(412,h-120,90,"Undo"))Undo();if(Button(514,h-120,90,"Redo"))Redo();
            if(Button(616,h-120,145,"Walk inside"))StartWalk();if(Button(773,h-120,140,showRoof?"Hide roof":"Show roof")){showRoof=!showRoof;Rebuild();}
            if(Button(925,h-120,160,"Blueprint card")){card=JsonUtility.ToJson(blueprint);share=true;}
            GUI.enabled=true;
            if(Button(1097,h-120,145,"Slot "+(slot+1)+" →")){if(invalidSlot||SaveBlueprint()){slot=(slot+1)%3;invalidSlot=false;LoadSlot();}}
            Label(24,h-73,w-48,24,"R rotate furniture · Q/E orbit · Scroll zoom · Ctrl/Cmd Z undo · Ctrl/Cmd Y redo · Creative materials are free in this prototype",small);
            Label(24,h-42,w-48,36,message,small);
            editingName=GUI.GetNameOfFocusedControl()=="BlueprintName";
            if(share)
            {
                GUI.color=new Color(.02f,.035f,.05f,1);GUI.DrawTexture(new Rect(80,205,w-160,h-365),Texture2D.whiteTexture);GUI.color=Color.white;
                Label(102,219,w-300,35,"BLUEPRINT CARD · COPY OR PASTE A LAYOUT",text);
                cardScroll=GUI.BeginScrollView(new Rect(102,260,w-204,h-495),cardScroll,new Rect(0,0,w-228,1500));card=GUI.TextArea(new Rect(0,0,w-230,1490),card,20000);GUI.EndScrollView();
                if(Button(102,h-220,210,"Copy card")){GUIUtility.systemCopyBuffer=card;message="Blueprint card copied.";}
                if(Button(326,h-220,220,"Import this card")){var candidate=ParseCard(card);if(candidate==null)message="Invalid card. Your current building is unchanged.";else{history.Record(blueprint);blueprint=candidate;dirty=true;Rebuild();share=false;message="Imported locally. Review it, then save.";}}
                if(Button(562,h-220,170,"Close card"))share=false;
            }
        }
        [Serializable] class SmokeResult {public bool passed,doorway,wallCollision,save,import,undo,restored;public int pieces;public string error;}
        IEnumerator RunSmoke()
        {
            var result=new SmokeResult();string output=Environment.GetEnvironmentVariable("BLUEPRINT_SMOKE_PATH")??Path.Combine(Application.persistentDataPath,"BlueprintSmokeOutput");Directory.CreateDirectory(output);
            Enter();blueprint=BuildingBlueprint.Classroom();Rebuild();yield return null;
            result.pieces=building.GetComponentsInChildren<Renderer>().Length;
            history.Record(blueprint);blueprint.Place(StudioPart.Roof,5,5,true,0,false);Undo();result.undo=blueprint.roofs[65]==0;Redo();result.undo&=blueprint.roofs[65]==1;
            string export=JsonUtility.ToJson(blueprint);result.import=ParseCard(export)!=null&&ParseCard("{bad") ==null&&ParseCard(new string('x',20001))==null;
            result.save=SaveBlueprint()&&ParseCard(File.ReadAllText(SavePath))!=null;
            Rebuild();yield return new WaitForEndOfFrame();Capture(output,"01-building-designer");
            StartWalk();yield return null;
            for(int i=0;i<120;i++){controller.Move(Vector3.forward*.045f+Vector3.down*.025f);if(i%6==0)yield return null;}
            result.doorway=walker.transform.position.z>StudioGeometry.Origin.z-2&&Mathf.Abs(walker.transform.position.y-.12f)<.2f;
            yield return new WaitForEndOfFrame();Capture(output,"02-walkthrough");
            // Move toward a solid back wall; the controller must stop inside the room.
            for(int i=0;i<250;i++){controller.Move(Vector3.forward*.045f+Vector3.down*.025f);if(i%6==0)yield return null;}
            result.wallCollision=walker.transform.position.z<StudioGeometry.Origin.z+6&&walker.transform.position.z>StudioGeometry.Origin.z+5;
            StopWalk();share=true;card=export;yield return new WaitForEndOfFrame();Capture(output,"03-blueprint-card");
            bool wasEnabled=suspended.Contains(game);Leave();result.restored=!Active&&wasEnabled&&game.enabled&&Mathf.Approximately(Time.timeScale,previousTimeScale);
            result.passed=result.doorway&&result.wallCollision&&result.save&&result.import&&result.undo&&result.restored;
            if(!result.passed)result.error="One or more designer checks failed; inspect result fields.";
            File.WriteAllText(Path.Combine(output,"result.json"),JsonUtility.ToJson(result,true));Debug.Log("BLUEPRINT_STUDIO_SMOKE "+JsonUtility.ToJson(result));Application.Quit(result.passed?0:1);
        }
        void Capture(string directory,string name)
        {
            var texture=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(directory,name+".png"),texture.EncodeToPNG());Destroy(texture);
        }
    }
}
