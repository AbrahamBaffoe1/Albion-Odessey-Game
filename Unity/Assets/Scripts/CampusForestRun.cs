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
        NatureKit kit;readonly NatureBatch batch=new NatureBatch();readonly NatureAtmosphere atmosphere=new NatureAtmosphere();Transform chunkA,chunkB;
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
            var definition=JsonUtility.FromJson<CraftDescription>(Resources.Load<TextAsset>("CampusCraft/oak").text);var templates=new GameObject[3];
            for(int t=0;t<3;t++){templates[t]=CraftModel.Load("oak"+t,definition.sections[0],definition.materials,stage.transform);templates[t].SetActive(false);}
            // Two identical-length stretches of woodland leapfrog past the runner, so the course feels endless with a few draw calls.
            chunkA=BuildChunk("Forest stretch A",1835,templates);chunkB=BuildChunk("Forest stretch B",1877,templates);
            foreach(var template in templates)Destroy(template);
            var cameraObject=new GameObject("Treasure run camera");cameraObject.transform.SetParent(stage.transform,false);cameraObject.transform.localPosition=new Vector3(0,4.6f,-10f);cameraObject.transform.localRotation=Quaternion.Euler(13,0,0);
            view=cameraObject.AddComponent<Camera>();view.depth=100;view.fieldOfView=62;view.nearClipPlane=.3f;view.farClipPlane=240;view.clearFlags=CameraClearFlags.SolidColor;view.backgroundColor=NatureKit.Mist;
            brit=Avatar("Brit the Briton · original game interpretation");brit.Build(2,0,2,false);var silver=TowerGeometry.Material("Briton silver",new Color(.65f,.68f,.74f),0,.35f);
            Part("Helmet",PrimitiveType.Sphere,new Vector3(0,1.75f,0),new Vector3(.43f,.48f,.45f),silver,brit.transform);
            Part("Purple crest",PrimitiveType.Cube,new Vector3(0,2.05f,0),new Vector3(.12f,.3f,.45f),TowerGeometry.Material("Albion purple",new Color(.32f,.1f,.47f),0,.35f),brit.transform);
            Part("Shield",PrimitiveType.Sphere,new Vector3(-.5f,1,.15f),new Vector3(.5f,.65f,.12f),kit.Gold,brit.transform);
        }

        // One 210 m stretch of the course, in chunk-local metres: x across the trail, z along it. Everything is baked into one mesh per material.
        Transform BuildChunk(string name,int randomSeed,GameObject[] templates)
        {
            var chunk=new GameObject(name).transform;chunk.SetParent(stage.transform,false);
            var rng=new System.Random(randomSeed);float R(float a,float b){return a+(float)rng.NextDouble()*(b-a);}Quaternion Yaw(){return Quaternion.Euler(0,R(0,360),0);}
            const float L=ChunkLength,riverNear=21f,riverFar=37f;
            // Ground, trail and river. The river sits right of the trail, in a shallow channel with a mud bed and pale water edges.
            batch.Quad(new Vector3(-90,0,L/2),new Vector2(222,L),kit.Moss,7f);batch.Quad(new Vector3(95,0,L/2),new Vector2(116,L),kit.Moss,7f);
            batch.Quad(new Vector3(29,-.55f,L/2),new Vector2(riverFar-riverNear,L),kit.Mud,6f);batch.Quad(new Vector3(29,-.2f,L/2),new Vector2(14.4f,L),kit.Water,6f);
            batch.Quad(new Vector3(22.3f,-.19f,L/2),new Vector2(.9f,L),kit.Foam,6f);batch.Quad(new Vector3(35.7f,-.19f,L/2),new Vector2(.9f,L),kit.Foam,6f);
            batch.Prim(PrimitiveType.Cube,new Vector3(riverNear,-.28f,L/2),new Vector3(.12f,.56f,L),kit.Mud);batch.Prim(PrimitiveType.Cube,new Vector3(riverFar,-.28f,L/2),new Vector3(.12f,.56f,L),kit.Mud);
            batch.Quad(new Vector3(0,.03f,L/2),new Vector2(8,L),kit.Trail,5f);
            foreach(int side in new[]{-1,1})batch.Quad(new Vector3(side*4.7f,.025f,L/2),new Vector2(1.4f,L),kit.TrailEdge,5f);
            // Lane cues: pale dashes on the two lane boundaries give the speed of the run something to read against.
            for(float z=3.5f;z<L;z+=7f)foreach(int side in new[]{-1,1})batch.Prim(PrimitiveType.Cube,new Vector3(side*LaneWidth/2,.05f,z),new Vector3(.14f,.03f,2.4f),kit.TrailLight);
            for(int i=0;i<18;i++)batch.Prim(PrimitiveType.Sphere,new Vector3(R(-3.6f,3.6f),.05f,R(0,L)),new Vector3(R(.6f,1.4f),.03f,R(.4f,.9f)),Yaw(),kit.Litter);
            for(int i=0;i<28;i++){float d=R(.14f,.28f);batch.Prim(PrimitiveType.Sphere,new Vector3((i%2==0?-1:1)*R(3.1f,4.4f),.07f,R(0,L)),new Vector3(d,d*.6f,d),kit.Stone);}
            // Reeds and water lilies along both banks.
            for(int c=0;c<10;c++){float bank=c%2==0?riverNear-.6f:riverFar+.6f,cz=R(0,L);for(int k=0;k<5;k++){float h=R(1.1f,2.2f);var at=new Vector3(bank+R(-.8f,.8f),h/2-.2f,cz+R(-.8f,.8f));
                batch.Prim(PrimitiveType.Cylinder,at,new Vector3(.05f,h/2,.05f),Quaternion.Euler(R(-8,8),0,R(-8,8)),kit.Reed);if(k%3==0)batch.Prim(PrimitiveType.Cylinder,at+Vector3.up*(h/2-.1f),new Vector3(.1f,.18f,.1f),kit.Cattail);}}
            for(int i=0;i<12;i++){float d=R(.5f,.9f);var at=new Vector3(R(24f,34f),-.17f,R(0,L));batch.Prim(PrimitiveType.Cylinder,at,new Vector3(d,.015f,d),kit.LeafLight);if(i%3==0)batch.Prim(PrimitiveType.Sphere,at+Vector3.up*.05f,Vector3.one*.16f,kit.FlowerWhite);}
            // Woodland: oaks on the left, a few between trail and river, and a taller tree line on the far bank.
            void Tree(float x,float scale){batch.Hierarchy(templates[rng.Next(templates.Length)].transform,Matrix4x4.TRS(new Vector3(x,0,R(0,L)),Yaw(),Vector3.one*scale));}
            for(int i=0;i<16;i++)Tree(-R(10f,70f),R(.8f,1.5f));for(int i=0;i<7;i++)Tree(R(9.5f,18.5f),R(.8f,1.3f));for(int i=0;i<9;i++)Tree(R(41f,80f),R(1.1f,1.8f));
            // Understory: ferns, shrubs, boulders, fallen logs and wildflowers, kept clear of the trail and the river.
            float Verge(){float x=R(6f,26f);if(x>riverNear-1.5f)x=R(6f,riverNear-1.5f);return (rng.Next(2)==0?-1:1)*x;}
            for(int i=0;i<26;i++){float x=Verge(),z=R(0,L);for(int k=0;k<2;k++)batch.Prim(PrimitiveType.Sphere,new Vector3(x+R(-.4f,.4f),.16f,z+R(-.4f,.4f)),new Vector3(R(.9f,1.4f),.28f,R(.4f,.6f)),Quaternion.Euler(0,R(0,360),R(-12,12)),kit.Fern);}
            for(int i=0;i<16;i++){float x=Verge(),z=R(0,L),s=R(1.1f,1.9f);batch.Prim(PrimitiveType.Sphere,new Vector3(x,s*.4f,z),new Vector3(s,s*.8f,s),kit.LeafDeep);batch.Prim(PrimitiveType.Sphere,new Vector3(x+.2f,s*.7f,z-.2f),new Vector3(s*.6f,s*.4f,s*.6f),kit.LeafLight);}
            for(int i=0;i<8;i++){float x=Verge(),z=R(0,L),s=R(.9f,1.8f);batch.Prim(PrimitiveType.Sphere,new Vector3(x,.25f,z),new Vector3(s,s*.6f,s*.85f),Yaw(),kit.Stone);batch.Prim(PrimitiveType.Sphere,new Vector3(x,.25f+s*.28f,z),new Vector3(s*.7f,s*.2f,s*.6f),kit.LeafLight);}
            for(int i=0;i<4;i++)batch.Prim(PrimitiveType.Cylinder,new Vector3((i%2==0?-1:1)*R(6f,9f),.3f,R(0,L)),new Vector3(.5f,R(1.4f,2.4f),.5f),Quaternion.Euler(90,R(-20,20),0),kit.Bark);
            for(int i=0;i<8;i++){float x=(rng.Next(2)==0?-1:1)*R(5.5f,12f),z=R(0,L);var color=i%3==0?kit.FlowerWhite:i%3==1?kit.FlowerLilac:kit.FlowerGold;for(int k=0;k<4;k++)batch.Prim(PrimitiveType.Sphere,new Vector3(x+R(-.5f,.5f),.25f,z+R(-.5f,.5f)),Vector3.one*.18f,color);}
            // Soft, dark hills beyond the tree line give the mist something to fade from.
            for(int i=0;i<6;i++)batch.Prim(PrimitiveType.Sphere,new Vector3((i%2==0?-1:1)*R(85f,150f),R(2f,6f),R(0,L)),new Vector3(R(30f,50f),R(14f,24f),R(24f,40f)),kit.LeafDeep);
            batch.Flush(chunk);return chunk;
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
        void OnDestroy(){atmosphere.Restore();if(stage!=null)Destroy(stage);foreach(var mesh in batch.Meshes)if(mesh!=null)Destroy(mesh);kit?.Release();}
    }
}
