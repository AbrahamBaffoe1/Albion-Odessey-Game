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
        readonly List<Transform> trees=new List<Transform>();readonly List<Material> materials=new List<Material>();
        Material bark,leaf,gold,mint,stone;float distance;int seed=-1;bool open,preview;string previewId;
        public bool IsOpen=>open;
        public void PreviewForSmoke(ForestSnapshot snapshot,string id){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-forestSmoke")<0)throw new InvalidOperationException();preview=true;previewId=id;if(stage==null)Build();open=true;stage.SetActive(true);state=snapshot;game.life.SetPanel("forestrun");}
        public void Setup(OdysseyGame owner,CampusSharedSession online){game=owner;session=online;}
        public void Accept(ForestSnapshot snapshot){state=snapshot;}
        public void Open(){if(!session.Joined)return;if(stage==null)Build();open=true;stage.SetActive(true);game.life.SetPanel("forestrun");Send("join");}
        public void Close(){if(open)Send("leave");open=false;state=null;if(stage!=null)stage.SetActive(false);if(game?.life!=null&&game.life.panel=="forestrun")game.life.SetPanel("");}
        void Send(string action){if(preview)return;session.SendForest(JsonUtility.ToJson(new ForestIntent{action=action}));}
        public bool HandleInput(){if(!open)return false;if(Input.GetKeyDown(KeyCode.Escape)){Close();return true;}if(Input.GetKeyDown(KeyCode.LeftArrow)||Input.GetKeyDown(KeyCode.A))Send("left");if(Input.GetKeyDown(KeyCode.RightArrow)||Input.GetKeyDown(KeyCode.D))Send("right");if(Input.GetKeyDown(KeyCode.Space)||Input.GetKeyDown(KeyCode.UpArrow))Send("jump");if(Input.GetKeyDown(KeyCode.DownArrow)||Input.GetKeyDown(KeyCode.S))Send("slide");if(Input.GetKeyDown(KeyCode.R))Send("rescue");return true;}
        Material Mat(string name,Color color){var m=TowerGeometry.Material(name,color,0,.35f);materials.Add(m);return m;}
        GameObject Part(string name,PrimitiveType shape,Vector3 position,Vector3 scale,Material mat,Transform parent=null){var o=GameObject.CreatePrimitive(shape);o.name=name;o.transform.SetParent(parent??stage.transform,false);o.transform.localPosition=position;o.transform.localScale=scale;o.GetComponent<Renderer>().sharedMaterial=mat;Destroy(o.GetComponent<Collider>());return o;}
        KeeperAvatar Avatar(string name){var o=new GameObject(name);o.transform.SetParent(stage.transform,false);var a=o.AddComponent<KeeperAvatar>();a.Build(game.campus.skin,game.campus.outfit,game.campus.hair,true);return a;}
        void Build()
        {
            stage=new GameObject("Whitehouse-inspired fictional race course");stage.transform.position=new Vector3(4000,100,4000);
            bark=Mat("Forest bark",new Color(.24f,.14f,.08f));leaf=Mat("Oak canopy",new Color(.18f,.35f,.12f));gold=Mat("Treasure gold",new Color(1,.69f,.13f));mint=Mat("Restoration seed",new Color(.35f,.95f,.59f));stone=Mat("River stone",new Color(.38f,.42f,.43f));
            var soilTexture=Resources.Load<Texture2D>("CampusCraft/grass_ground_Color");
            Part("Forest floor",PrimitiveType.Cube,new Vector3(0,-.3f,75),new Vector3(110,.5f,210),Mat("Moss ground",new Color(.23f,.30f,.15f))).GetComponent<Renderer>().sharedMaterial.mainTexture=soilTexture;
            Part("Earthen trail",PrimitiveType.Cube,new Vector3(0,-.02f,75),new Vector3(8,.06f,210),Mat("Trail earth",new Color(.49f,.37f,.23f)));
            Part("River's edge",PrimitiveType.Cube,new Vector3(24,-.09f,75),new Vector3(11,.06f,210),Mat("Kalamazoo-inspired water",new Color(.18f,.43f,.48f)));
            var definition=JsonUtility.FromJson<CraftDescription>(Resources.Load<TextAsset>("CampusCraft/oak").text);var templates=new GameObject[3];for(int t=0;t<3;t++){templates[t]=CraftModel.Load("oak"+t,definition.sections[0],definition.materials,stage.transform);templates[t].SetActive(false);}
            for(int i=0;i<70;i++){
                var tree=Instantiate(templates[i%3],stage.transform);tree.name="Woodland oak";tree.SetActive(true);tree.transform.localScale=Vector3.one*(.7f+(i%5)*.11f);tree.transform.localRotation=Quaternion.Euler(0,i*137.5f,0);float side=i%2==0?-1:1;tree.transform.localPosition=new Vector3(side*(7+i%7*2.3f+Mathf.Sin(i*3.1f)),0,i*3-24);trees.Add(tree.transform);
                
                
                Part("Understory",PrimitiveType.Sphere,new Vector3(side*1.8f,.45f,1),new Vector3(1.6f,.9f,1.5f),leaf,tree.transform);
            }
            var cameraObject=new GameObject("Treasure run camera");cameraObject.transform.SetParent(stage.transform,false);cameraObject.transform.localPosition=new Vector3(0,5,-11);cameraObject.transform.localRotation=Quaternion.Euler(15,0,0);view=cameraObject.AddComponent<Camera>();view.depth=100;view.fieldOfView=65;view.farClipPlane=190;view.clearFlags=CameraClearFlags.Skybox;
            brit=Avatar("Brit the Briton · original game interpretation");brit.Build(2,0,2,false);var silver=Mat("Briton silver",new Color(.65f,.68f,.74f));
            Part("Helmet",PrimitiveType.Sphere,new Vector3(0,1.75f,0),new Vector3(.43f,.48f,.45f),silver,brit.transform);
            Part("Purple crest",PrimitiveType.Cube,new Vector3(0,2.05f,0),new Vector3(.12f,.3f,.45f),Mat("Albion purple",new Color(.32f,.1f,.47f)),brit.transform);
            Part("Shield",PrimitiveType.Sphere,new Vector3(-.5f,1,.15f),new Vector3(.5f,.65f,.12f),gold,brit.transform);
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
            for(int i=0;i<trees.Count;i++){var p=trees[i].localPosition;p.z=Mathf.Repeat(i*3-distance,210)-25;trees[i].localPosition=p;}
            var visible=new HashSet<int>();if(state.events!=null)foreach(var e in state.events){float z=e.z-distance;if(z< -8||z>160)continue;visible.Add(e.id);if(!events.TryGetValue(e.id,out var o)){
                    bool pickup=e.kind=="seed"||e.kind=="treasure";Vector3 size=pickup?Vector3.one*.65f:e.kind=="log"?new Vector3(1.9f,.7f,.7f):e.kind=="branch"?new Vector3(2.1f,.5f,.7f):new Vector3(1.7f,1.9f,1.6f);
                    o=Part(e.kind,pickup?PrimitiveType.Sphere:PrimitiveType.Cube,Vector3.zero,size,e.kind=="seed"?mint:e.kind=="treasure"?gold:e.kind=="rock"?stone:bark);events[e.id]=o;
                }o.transform.localPosition=new Vector3(e.lane*2.4f,e.kind=="branch"?1.5f:e.kind=="log"?.35f:e.kind=="rock"?.95f:1,z);}
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
        void OnDestroy(){if(stage!=null)Destroy(stage);foreach(var m in materials)Destroy(m);}
    }
}
