using System;
using System.Collections.Generic;
using UnityEngine;
namespace AlbionOdyssey
{
    [Serializable] public sealed class ForestRunner {public string id,display,pose;public int lane,treasures;public float distance,gap;public double finishedAt;public bool caught,finished;}
    [Serializable] public sealed class ForestEvent {public int id,lane;public float z;public string kind;}
    [Serializable] public sealed class ForestSnapshot {public int seed,seeds,treasures,rescues;public string phase,mode;public float countdown,length;public bool restored;public ForestRunner[] players;public ForestEvent[] events;}
    [Serializable] sealed class ForestIntent {public string type="forest",action;}
    public sealed class CampusForestRun : MonoBehaviour
    {
        OdysseyGame game;CampusSharedSession session;ForestSnapshot state;GameObject stage;Camera view;KeeperAvatar brit;
        readonly Dictionary<string,KeeperAvatar> runners=new Dictionary<string,KeeperAvatar>();
        readonly Dictionary<int,GameObject> events=new Dictionary<int,GameObject>();
        const float ChunkLength=210f,LaneWidth=2.4f;
        NatureKit kit;readonly NatureAtmosphere atmosphere=new NatureAtmosphere();Transform chunkA,chunkB;
        float distance;int seed=-1;bool open,preview;string previewId;
        public bool IsOpen=>open;
        public void PreviewForSmoke(ForestSnapshot snapshot,string id){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-forestSmoke")<0)throw new InvalidOperationException();preview=true;previewId=id;Show();open=true;state=snapshot;game.life.SetPanel("forestrun");}
        public void Setup(OdysseyGame owner,CampusSharedSession online){game=owner;session=online;}
        public void Accept(ForestSnapshot snapshot){state=snapshot;}
        public void Open(){if(!session.Joined)return;Show();open=true;game.life.SetPanel("forestrun");Send("join");}
        public void Close(){if(open)Send("leave");open=false;state=null;Hide();if(game?.life!=null&&game.life.panel=="forestrun")game.life.SetPanel("");}
        void Send(string action){if(preview)return;session.SendForest(JsonUtility.ToJson(new ForestIntent{action=action}));}
        public bool HandleInput(){if(!open)return false;if(Input.GetKeyDown(KeyCode.Escape)){Close();return true;}if(Input.GetKeyDown(KeyCode.LeftArrow)||Input.GetKeyDown(KeyCode.A))Send("left");if(Input.GetKeyDown(KeyCode.RightArrow)||Input.GetKeyDown(KeyCode.D))Send("right");if(Input.GetKeyDown(KeyCode.Space)||Input.GetKeyDown(KeyCode.UpArrow))Send("jump");if(Input.GetKeyDown(KeyCode.DownArrow)||Input.GetKeyDown(KeyCode.S))Send("slide");if(Input.GetKeyDown(KeyCode.R))Send("rescue");return true;}
        GameObject Part(string name,PrimitiveType shape,Vector3 position,Vector3 scale,Material mat,Transform parent=null,Quaternion? rotation=null){var o=GameObject.CreatePrimitive(shape);o.name=name;o.transform.SetParent(parent??stage.transform,false);o.transform.localPosition=position;o.transform.localRotation=rotation??Quaternion.identity;o.transform.localScale=scale;o.GetComponent<Renderer>().sharedMaterial=mat;Destroy(o.GetComponent<Collider>());return o;}
        KeeperAvatar Avatar(string name){var o=new GameObject(name);o.transform.SetParent(stage.transform,false);var a=o.AddComponent<KeeperAvatar>();a.Build(game.campus.skin,game.campus.outfit,game.campus.hair,true);return a;}
        void Show(){if(stage==null)Build();stage.SetActive(true);atmosphere.Apply(game,stage.transform);}
        void Hide(){if(stage!=null)stage.SetActive(false);atmosphere.Restore();}

        void Build()
        {
            stage=new GameObject("Whitehouse-inspired fictional race course");stage.transform.position=new Vector3(4000,100,4000);
            kit=new NatureKit();
            // The scenery is authored in Blender (Tools/build_forest_course.py): two 210 m stretches that leapfrog past the runner, so the
            // course feels endless. Stretch A has the arched footbridge and the islet; stretch B the marsh boardwalk and split-rail fence.
            var description=JsonUtility.FromJson<CraftDescription>(Resources.Load<TextAsset>("CampusCraft/forest").text);
            chunkA=CraftModel.Load("forest_stretch_a",description.sections[0],description.materials,stage.transform).transform;
            chunkB=CraftModel.Load("forest_stretch_b",description.sections[0],description.materials,stage.transform).transform;
            var cameraObject=new GameObject("Treasure run camera");cameraObject.transform.SetParent(stage.transform,false);cameraObject.transform.localPosition=new Vector3(0,4.6f,-10f);cameraObject.transform.localRotation=Quaternion.Euler(13,0,0);
            view=cameraObject.AddComponent<Camera>();view.depth=100;view.fieldOfView=62;view.nearClipPlane=.3f;view.farClipPlane=240;view.clearFlags=CameraClearFlags.Skybox;
            brit=Avatar("Brit the Briton · original game interpretation");brit.Build(2,0,2,false);var silver=TowerGeometry.Material("Briton silver",new Color(.65f,.68f,.74f),0,.35f);
            Part("Helmet",PrimitiveType.Sphere,new Vector3(0,1.75f,0),new Vector3(.43f,.48f,.45f),silver,brit.transform);
            Part("Purple crest",PrimitiveType.Cube,new Vector3(0,2.05f,0),new Vector3(.12f,.3f,.45f),TowerGeometry.Material("Albion purple",new Color(.32f,.1f,.47f),0,.35f),brit.transform);
            Part("Shield",PrimitiveType.Sphere,new Vector3(-.5f,1,.15f),new Vector3(.5f,.65f,.12f),kit.Gold,brit.transform);
            BuildButterflies();
        }

        // A few butterflies drift over the trail ahead of the runner; purely decorative.
        readonly List<Transform> butterflies=new List<Transform>();
        void BuildButterflies()
        {
            var colors=new[]{new Color(.95f,.5f,.1f),new Color(.98f,.9f,.2f),new Color(.45f,.62f,.95f)};
            for(int i=0;i<9;i++)
            {
                var root=new GameObject("Butterfly").transform;root.SetParent(stage.transform,false);var paint=TowerGeometry.Material("Butterfly "+i,colors[i%3],0,.3f);
                foreach(int side in new[]{-1,1}){var wing=Part("Wing",PrimitiveType.Cube,new Vector3(side*.09f,0,0),new Vector3(.16f,.012f,.12f),paint,root);wing.transform.localRotation=Quaternion.Euler(0,0,side*20);}
                Part("Body",PrimitiveType.Sphere,Vector3.zero,new Vector3(.025f,.025f,.1f),TowerGeometry.Material("Butterfly body",new Color(.08f,.06f,.05f),0,.2f),root);
                butterflies.Add(root);
            }
        }

        void FlutterButterflies()
        {
            float clock=Time.unscaledTime;
            for(int i=0;i<butterflies.Count;i++)
            {
                float phase=i*1.7f,forward=18+(i*11)%36,sway=Mathf.Sin(clock*.5f+phase);
                var at=new Vector3(Mathf.Sin(clock*.7f+phase)*(4+i%3)+((i%2)*2-1)*3,1.3f+Mathf.Sin(clock*1.3f+phase*2)*.45f,forward+Mathf.Sin(clock*.4f+phase)*4);
                var t=butterflies[i];t.localPosition=at;t.localRotation=Quaternion.Euler(0,Mathf.Atan2(Mathf.Cos(clock*.7f+phase),1)*Mathf.Rad2Deg*.6f,sway*10);
                float flap=Mathf.Sin(clock*22+phase)*55;t.GetChild(0).localRotation=Quaternion.Euler(0,0,-flap);t.GetChild(1).localRotation=Quaternion.Euler(0,0,flap);
            }
        }

        // Obstacles and pickups are small composite props; hit-boxes are decided by the server, these only have to read clearly.
        GameObject BuildEvent(ForestEvent e)
        {
            var root=new GameObject(e.kind);root.transform.SetParent(stage.transform,false);var at=root.transform;
            switch(e.kind)
            {
                case "log":
                    Part("Fallen log",PrimitiveType.Cylinder,new Vector3(0,.38f,0),new Vector3(.76f,1.15f,.76f),kit.Bark,at,Quaternion.Euler(0,0,90));
                    foreach(int end in new[]{-1,1})Part("Cut end",PrimitiveType.Cylinder,new Vector3(end*1.16f,.38f,0),new Vector3(.64f,.02f,.64f),kit.WoodCut,at,Quaternion.Euler(0,0,90));
                    Part("Moss",PrimitiveType.Sphere,new Vector3(.2f,.74f,0),new Vector3(.9f,.18f,.5f),kit.LeafLight,at);break;
                case "branch":
                    Part("Low branch",PrimitiveType.Cylinder,new Vector3(0,1.55f,0),new Vector3(.22f,1.35f,.22f),kit.Bark,at,Quaternion.Euler(0,0,90));
                    foreach(float x in new[]{-.95f,.1f,1f})Part("Leaves",PrimitiveType.Sphere,new Vector3(x,1.6f,0),new Vector3(.62f,.42f,.55f),kit.LeafDeep,at);
                    Part("Leaves",PrimitiveType.Sphere,new Vector3(-.4f,1.72f,0),new Vector3(.5f,.3f,.45f),kit.LeafLight,at);break;
                case "rock":
                    Part("Boulder",PrimitiveType.Sphere,new Vector3(0,.9f,0),new Vector3(1.6f,1.8f,1.5f),kit.Stone,at);
                    Part("Boulder shoulder",PrimitiveType.Sphere,new Vector3(.55f,.45f,.25f),new Vector3(.9f,.8f,.85f),kit.Stone,at);
                    Part("Moss",PrimitiveType.Sphere,new Vector3(-.1f,1.65f,0),new Vector3(.9f,.28f,.8f),kit.LeafLight,at);break;
                case "treasure":
                {var spin=new GameObject("Spin");spin.transform.SetParent(at,false);Part("Gold coin",PrimitiveType.Cylinder,Vector3.zero,new Vector3(.75f,.07f,.75f),kit.Gold,spin.transform,Quaternion.Euler(90,0,0));
                 Part("Coin rim",PrimitiveType.Cylinder,Vector3.zero,new Vector3(.82f,.05f,.82f),kit.WoodCut,spin.transform,Quaternion.Euler(90,0,0));break;}
                default:
                {var spin=new GameObject("Spin");spin.transform.SetParent(at,false);Part("Restoration seed",PrimitiveType.Sphere,Vector3.zero,new Vector3(.42f,.55f,.42f),kit.Mint,spin.transform);
                 Part("Seed leaf",PrimitiveType.Sphere,new Vector3(.12f,.3f,0),new Vector3(.3f,.07f,.16f),kit.LeafLight,spin.transform,Quaternion.Euler(0,0,25));break;}
            }
            return root;
        }

        void Update()
        {
            if(!open)return;if(!preview&&!session.Joined){Close();return;}if(state?.players==null)return;
            if(seed!=state.seed){seed=state.seed;foreach(var o in events.Values)Destroy(o);events.Clear();foreach(var a in runners.Values)Destroy(a.gameObject);runners.Clear();distance=0;}
            ForestRunner self=null;foreach(var p in state.players)if(p.id==(preview?previewId:game.accounts.UserId))self=p;if(self==null)return;
            distance=Mathf.Lerp(distance,self.distance,1-Mathf.Exp(-Time.unscaledDeltaTime*15));
            var present=new HashSet<string>();foreach(var p in state.players){present.Add(p.id);if(!runners.TryGetValue(p.id,out var a)){a=Avatar(p.display);runners[p.id]=a;var labelObject=new GameObject("Runner name");labelObject.transform.SetParent(a.transform,false);labelObject.transform.localPosition=new Vector3(0,2.6f,0);var label=labelObject.AddComponent<TextMesh>();label.text=p.display;label.fontSize=40;label.characterSize=.045f;label.anchor=TextAnchor.MiddleCenter;}
                float jump=p.pose=="jump"?1.3f:0;var target=new Vector3(p.lane*2.4f,jump,p.distance-distance);a.transform.localPosition=Vector3.Lerp(a.transform.localPosition,target,1-Mathf.Exp(-Time.unscaledDeltaTime*18));a.transform.localScale=Vector3.one*(p.pose=="slide"?.55f:1);a.gameObject.SetActive(Mathf.Abs(target.z)<150);a.Animate(p.caught||p.finished?0:10,false);var name=a.transform.Find("Runner name");if(name!=null){name.rotation=view.transform.rotation;name.gameObject.SetActive(Mathf.Abs(target.z)<22);}
            }
            var gone=new List<string>();foreach(var pair in runners)if(!present.Contains(pair.Key)){Destroy(pair.Value.gameObject);gone.Add(pair.Key);}foreach(var id in gone)runners.Remove(id);
            brit.transform.localPosition=new Vector3(self.lane*2.4f,0,-Mathf.Clamp(self.gap*.24f,1.5f,6));brit.Animate(self.caught?0:11,false);
            FlutterButterflies();
            float t=Mathf.Repeat(-distance,ChunkLength*2)-ChunkLength;chunkA.localPosition=new Vector3(0,0,t);chunkB.localPosition=new Vector3(0,0,t>=0?t-ChunkLength:t+ChunkLength);
            var visible=new HashSet<int>();if(state.events!=null)foreach(var e in state.events){float z=e.z-distance;if(z< -8||z>160)continue;visible.Add(e.id);if(!events.TryGetValue(e.id,out var o)){
                    o=BuildEvent(e);events[e.id]=o;
                }o.transform.localPosition=new Vector3(e.lane*LaneWidth,0,z);
                var spinner=o.transform.Find("Spin");if(spinner!=null){float clock=Time.unscaledTime;spinner.localPosition=new Vector3(0,1+Mathf.Sin(clock*3f+e.id)*.1f,0);spinner.localRotation=Quaternion.Euler(0,clock*140f+e.id*40f,0);}}
            var removed=new List<int>();foreach(var pair in events)if(!visible.Contains(pair.Key)){Destroy(pair.Value);removed.Add(pair.Key);}foreach(int id in removed)events.Remove(id);
        }
        void OnGUI()
        {
            if(!open)return;var old=GUI.matrix;float scale=Mathf.Min(Screen.width/1280f,Screen.height/800f);GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);float width=Screen.width/scale,height=Screen.height/scale;
            OdysseyUI.Card(new Rect(20,20,width-40,116),OdysseyUI.Surface);OdysseyUI.Text(new Rect(40,30,700,30),"BRITON TREASURE RUN · "+(state?.mode=="race"?"RACE":"CO-OP"),24,OdysseyUI.Mint,true);
            string progress=state==null?"Joining the forest…":state.phase=="countdown"?"Gather your group · starts in "+Mathf.CeilToInt(state.countdown):state.phase=="finished"?(state.mode=="race"?"Race complete · results below":"Run complete · your group found "+state.treasures+" treasures"):Mathf.FloorToInt(distance)+" / 1200 m  ·  "+(distance<400?"WOODLAND":distance<800?"MARSH EDGE":"RIVER TRAIL");
            OdysseyUI.Text(new Rect(40,66,900,28),progress,20,OdysseyUI.White,true);
            OdysseyUI.Text(new Rect(40,99,1100,24),state==null?"":state.treasures+" treasures  ·  "+state.seeds+" / 9 restoration seeds  ·  "+state.rescues+" rescues"+(state.restored?"  ·  TRAIL RESTORED":""),16,OdysseyUI.Mint);
            if(OdysseyUI.Button(new Rect(width-208,42,160,54),"EXIT · ESC","forest-exit",false))Close();
            OdysseyUI.Card(new Rect(20,height-115,width-40,95),OdysseyUI.Surface);OdysseyUI.Text(new Rect(40,height-105,width-80,27),"← →  Dodge    SPACE  Jump logs    ↓  Slide branches    R  Rescue a caught teammate",18,OdysseyUI.White,true);
            OdysseyUI.Text(new Rect(40,height-72,width-80,23),state?.mode=="race"?"Race to the finish. Each runner collects their own treasure; no teammate rescues.":"Green seeds earn a shared rescue every 3 pickups. Gold treasures belong to the whole group.",16,OdysseyUI.Muted);
            OdysseyUI.Text(new Rect(40,height-47,width-80,20),"Fictional race inspired by Whitehouse Nature Center · original mascot interpretation",12,OdysseyUI.Muted);
            bool participating=false;if(state?.players!=null)foreach(var p in state.players)if(p.id==(preview?previewId:game.accounts.UserId))participating=true;
            if(state?.mode=="race"&&state.phase=="finished"&&state.players!=null){
                var standings=new List<ForestRunner>(state.players);standings.Sort((a,b)=>{if(a.finished!=b.finished)return a.finished?-1:1;if(a.finished){int order=a.finishedAt.CompareTo(b.finishedAt);if(order!=0)return order;}int distanceOrder=b.distance.CompareTo(a.distance);return distanceOrder!=0?distanceOrder:b.treasures.CompareTo(a.treasures);});
                for(int i=0;i<Mathf.Min(standings.Count,8);i++){var racer=standings[i];OdysseyUI.Text(new Rect(40,150+i*28,700,28),(i+1)+". "+racer.display+" · "+(racer.finished?"Finished":Mathf.FloorToInt(racer.distance)+" m")+" · "+racer.treasures+" treasures",18,OdysseyUI.White,true);}
            }
            if(state?.mode=="race"&&!participating&&state.phase=="running")OdysseyUI.Text(new Rect(40,160,850,40),"Race underway. Join the next round when it finishes.",22,OdysseyUI.White,true);
            if(state!=null&&(state.phase=="finished"||!participating&&state.phase!="running")&&OdysseyUI.Button(new Rect(width/2-140,height/2-28,280,56),"RUN AGAIN","forest-again",false,true))Send("join");
            if(state?.players!=null)foreach(var p in state.players)if(p.id==(preview?previewId:game.accounts.UserId)&&p.caught&&state.phase!="finished")OdysseyUI.Text(new Rect(width/2-260,height/2-30,520,70),state.mode=="race"?"Brit caught you! Watch the remaining racers, then run again.":"Brit caught you! Your teammates can press R to rescue you.",26,OdysseyUI.White,true);
            GUI.matrix=old;
        }
        void OnDestroy(){atmosphere.Restore();if(stage!=null)Destroy(stage);kit?.Release();}
    }
}
