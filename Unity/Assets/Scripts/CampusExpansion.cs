using System;
using System.Collections.Generic;
using UnityEngine;
namespace AlbionOdyssey
{
    public sealed class CampusExpansion : MonoBehaviour
    {
        public OdysseyGame game;public readonly List<CampusCar> cars=new List<CampusCar>();public CampusParking parking;public CampusFauna fauna;
        public readonly List<Vector3> discoveries=new List<Vector3>();
        public int found;public int selectedPlace;public int skin=1,outfit,hair;public bool backpack=true;
        public string keeperName="Keeper";int active=-1,treasurePage;string search="",category="All";Vector2 scroll;
        KeeperAvatar preview;Camera previewCamera;RenderTexture previewTexture;
        GUIStyle heading,text,small,button;bool styles;int controllerFocus;string controllerPanel="";
        public static readonly string[] TreasureNames={"Brit the Briton","The painted Rock","The observatory telescope","The library's rare books","Whitehouse's living classroom","The former chapel in Kellogg","Pip's golden acorn"};
        public static readonly string[] TreasureText={
            "Brit the Briton was introduced in fall 2011. The mascot appears at college games and events. This game uses an original stylized knight as a discovery guide.",
            "The Rock stands at the northeast corner of the Quad. The class of 1899 gave it to the college, and students regularly paint it. Our purple-and-gold paint scheme is game artwork.",
            "The observatory was completed in 1884. Its historic equipment includes an eight-inch Clark refractor and a clock that measures sidereal, or star, time. This outdoor token celebrates that history.",
            "Albion's library holds rare books and manuscripts, including early printed books from the 1400s. Archives and Special Collections are in the Mudd Learning Center. The glowing book here is a game token.",
            "Whitehouse Nature Center is an outdoor learning space with a visitor center, a classroom, wildlife observation and live reptile and amphibian exhibits. Explore this game clearing to find its nature token.",
            "Gerstacker Commons, known as the Stack, is inside Kellogg Center. The wood-floored area with a balcony was once the college chapel, before Goodrich Chapel was built.",
            "ORIGINAL GAME FICTION: Pip hid a golden acorn near the equestrian district. This magical treasure is part of Albion Odyssey's story, not a real campus artifact."};
        public static readonly string[] TreasureSources={
            "https://www.albion.edu/wp-content/uploads/2021/03/Download-the-booklet-PDF.pdf",
            "https://www.albion.edu/about/at-a-glance/albion-isms/",
            "https://www.albion.edu/departments/physics/observatory-history/",
            "https://archives.albion.edu/rare-books",
            "https://www.albion.edu/about/our-campus/whitehouse-nature-center/",
            "https://www.albion.edu/wp-content/uploads/2021/03/Download-the-booklet-PDF.pdf",""};
        string Key=>"OdysseyCampus06."+(PlaytestMode.Active?"test."+PlaytestMode.Name+".":"")+game.state.active+".";
        public bool OnCampus=>game.player.transform.position.z>95;
        public CampusPlace Nearest
        {
            get {CampusPlace closest=null;float distance=float.MaxValue;foreach(var p in CampusCatalog.Places){float d=Vector3.Distance(p.position,game.player.transform.position);if(d<distance){distance=d;closest=p;}}return closest;}
        }
        public void Setup(OdysseyGame owner)
        {
            game=owner;CampusGeometry.Build();parking=gameObject.AddComponent<CampusParking>();parking.Setup(game);game.player.CreateAvatar();fauna=gameObject.AddComponent<CampusFauna>();fauna.Setup(game);game.player.thirdPerson=!OdysseySmoke.Enabled;
            game.player.eyes.farClipPlane=1400;game.player.eyes.cullingMask=~(1<<30);
            foreach(var p in new[]{CampusCatalog.Point(409,241),CampusCatalog.Point(520,161),CampusCatalog.Point(599,286)})
            {
                var o=new GameObject("Briton Cruiser "+(cars.Count+1));o.transform.position=p;var car=o.AddComponent<CampusCar>();car.Build(KeeperAvatar.Coats[cars.Count]);cars.Add(car);
            }
            BuildDiscoveries();LoadKeeper();
            if(!OdysseySmoke.Enabled){game.player.Teleport(CampusCatalog.Point(405,238)+Vector3.up*.08f);game.notice="Welcome to Albion! M opens all 61 destinations. E enters a nearby car. F3 opens your character settings.";}
        }
        public void LoadKeeper()
        {
            active=game.state.active;skin=Mathf.Clamp(PlayerPrefs.GetInt(Key+"skin",1),0,4);outfit=Mathf.Clamp(PlayerPrefs.GetInt(Key+"outfit",0),0,4);hair=Mathf.Clamp(PlayerPrefs.GetInt(Key+"hair",0),0,2);backpack=PlayerPrefs.GetInt(Key+"pack",1)==1;
            keeperName=PlayerPrefs.GetString(Key+"name","Keeper "+(active+1));if(keeperName.Length>24)keeperName=keeperName.Substring(0,24);
            found=PlayerPrefs.GetInt(Key+"found",0)&127;
            game.player.thirdPerson=OdysseySmoke.Enabled?false:PlayerPrefs.GetInt(Key+"third",1)==1;
            game.player.cameraDistance=Mathf.Clamp(PlayerPrefs.GetFloat(Key+"zoom",4.5f),2.5f,7);
            RefreshAvatar();
        }
        public void SaveKeeper()
        {
            keeperName=keeperName.Trim();if(keeperName.Length==0)keeperName="Keeper "+(active+1);
            PlayerPrefs.SetString(Key+"name",keeperName);PlayerPrefs.SetInt(Key+"skin",skin);PlayerPrefs.SetInt(Key+"outfit",outfit);PlayerPrefs.SetInt(Key+"hair",hair);PlayerPrefs.SetInt(Key+"pack",backpack?1:0);
            PlayerPrefs.SetInt(Key+"found",found);PlayerPrefs.SetInt(Key+"third",game.player.thirdPerson?1:0);PlayerPrefs.SetFloat(Key+"zoom",game.player.cameraDistance);PlayerPrefs.Save();
        }
        public void RefreshAvatar(){game.player.avatar.Build(skin,outfit,hair,backpack);if(preview!=null){preview.Build(skin,outfit,hair,backpack);LayerPreview();}}
        void Update(){if(game!=null&&active!=game.state.active)LoadKeeper();}
        public bool HandleInput()
        {
            if(game.life!=null&&!game.life.PanelOpen)controllerPanel="";
            if(Input.GetKeyDown(KeyCode.F3)){game.life.SetPanel(game.life.panel=="settings"?"":"settings");return true;}
            if(game.life!=null&&(game.life.panel=="settings"||game.life.panel=="campus"||game.life.panel=="treasures"))
            {
                if(controllerPanel!=game.life.panel){controllerPanel=game.life.panel;controllerFocus=0;}
                if(AlbionUIInput.Poll(out var horizontal,out var vertical,out var choose,out var cancel))
                {
                    string panel=game.life.panel;int count=panel=="settings"?8:panel=="campus"?5:4;
                    if(cancel){game.life.SetPanel("");return true;}
                    if(panel=="campus"&&controllerFocus==0&&horizontal!=0)
                    {
                        selectedPlace=(selectedPlace+(horizontal>0?1:-1)+CampusCatalog.Places.Length)%CampusCatalog.Places.Length;
                    }
                    else if(panel=="treasures"&&controllerFocus==0&&horizontal!=0)
                    {
                        treasurePage=(treasurePage+(horizontal>0?1:-1)+TreasureNames.Length)%TreasureNames.Length;
                    }
                    else if(vertical!=0)
                    {
                        controllerFocus=(controllerFocus+(vertical>0?-1:1)+count)%count;
                    }
                    if(choose)
                    {
                        if(panel=="settings")
                        {
                            if(controllerFocus==0){skin=0;outfit=0;hair=0;backpack=true;RefreshAvatar();SaveKeeper();}
                            else if(controllerFocus==1){skin=(skin+1)%KeeperAvatar.Skin.Length;RefreshAvatar();SaveKeeper();}
                            else if(controllerFocus==2){outfit=(outfit+1)%KeeperAvatar.Coats.Length;RefreshAvatar();SaveKeeper();}
                            else if(controllerFocus==3){hair=(hair+1)%3;RefreshAvatar();SaveKeeper();}
                            else if(controllerFocus==4){backpack=!backpack;RefreshAvatar();SaveKeeper();}
                            else if(controllerFocus==5){game.player.thirdPerson=!game.player.thirdPerson;SaveKeeper();}
                            else if(controllerFocus==6){SaveKeeper();game.life.SetPanel("");}
                            else {SaveKeeper();game.life.SetPanel("welcome");}
                        }
                        else if(panel=="campus")
                        {
                            if(controllerFocus==0)Travel(CampusCatalog.Places[Mathf.Clamp(selectedPlace,0,CampusCatalog.Places.Length-1)]);
                            else if(controllerFocus==1)game.life.SetPanel("treasures");
                            else if(controllerFocus==2)Application.OpenURL(CampusCatalog.MapSource);
                            else if(controllerFocus==3)game.life.SetPanel("map");
                            else game.life.SetPanel("");
                        }
                        else
                        {
                            if(controllerFocus==1)
                            {
                                if(game.player.TryExitVehicle()){if(game.building)game.ToggleMode();game.player.Teleport(discoveries[treasurePage]+new Vector3(0,.08f,-3));game.player.transform.rotation=Quaternion.identity;game.life.SetPanel("");}
                            }
                            else if(controllerFocus==2&&treasurePage<6)Application.OpenURL(TreasureSources[treasurePage]);
                            else if(controllerFocus==3)game.life.SetPanel("campus");
                        }
                    }
                    return true;
                }
            }
            return false;
        }
        void BuildDiscoveries()
        {
            discoveries.Add(CampusCatalog.Point(568,382));discoveries.Add(CampusCatalog.Point(429,189));
            discoveries.Add(Find("5").Arrival+Vector3.left*4);discoveries.Add(Find("21").Arrival);
            discoveries.Add(Find("25").Arrival);discoveries.Add(Find("14").Arrival);discoveries.Add(CampusCatalog.Point(414,404));
            var gold=TowerGeometry.Material("Discovery gold",new Color(1,.71f,.18f),.4f,.65f);
            var purple=TowerGeometry.Material("Discovery purple",new Color(.29f,.1f,.47f));
            for(int i=0;i<discoveries.Count;i++)
            {
                var t=new GameObject(TreasureNames[i]).transform;t.position=discoveries[i];
                if(i==0)
                {
                    var knight=new GameObject("Brit · original knight interpretation");knight.transform.SetParent(t,false);var a=knight.AddComponent<KeeperAvatar>();a.Build(2,0,2,false);
                    var silver=TowerGeometry.Material("Brit helmet",new Color(.62f,.66f,.72f),.6f,.6f);
                    KeeperAvatar.Part(t,"Knight helmet",PrimitiveType.Sphere,new Vector3(0,1.8f,0),new Vector3(.46f,.5f,.44f),silver);
                    KeeperAvatar.Part(t,"Helmet plume",PrimitiveType.Cube,new Vector3(0,2.14f,-.06f),new Vector3(.12f,.33f,.35f),purple);
                    KeeperAvatar.Part(t,"Knight visor",PrimitiveType.Cube,new Vector3(0,1.8f,.23f),new Vector3(.3f,.065f,.04f),purple);
                    KeeperAvatar.Part(t,"Briton shield",PrimitiveType.Sphere,new Vector3(-.5f,1.1f,.12f),new Vector3(.55f,.75f,.12f),gold);
                }
                else if(i==1)
                {
                    KeeperAvatar.Part(t,"The Rock",PrimitiveType.Sphere,new Vector3(0,.9f,0),new Vector3(3,1.8f,2.3f),purple,true);
                    KeeperAvatar.Part(t,"Painted gold band",PrimitiveType.Cube,new Vector3(0,1.02f,-1.05f),new Vector3(1.7f,.35f,.1f),gold);
                }
                else
                {
                    KeeperAvatar.Part(t,"Discovery plinth",PrimitiveType.Cylinder,new Vector3(0,.35f,0),new Vector3(1,.35f,1),purple,true);
                    var token=KeeperAvatar.Part(t,"Discovery token",i==3?PrimitiveType.Cube:PrimitiveType.Sphere,new Vector3(0,1.2f,0),i==3?new Vector3(.65f,.15f,.5f):Vector3.one*.55f,gold);
                    if(i==2){token.transform.localScale=new Vector3(.22f,.22f,1.3f);token.transform.localRotation=Quaternion.Euler(-30,0,0);}
                }
                CampusGeometry.Sign(t,TreasureNames[i]+"\nE · Discover",new Vector3(0,2.9f,0),9);
                var worldTag=t.gameObject.AddComponent<CampusWorldLabel>();worldTag.Configure("DISCOVERY\n"+TreasureNames[i],new Color(1f,.76f,.28f),new Vector3(0,3.45f,0),18f);
            }
        }
        public static CampusPlace Find(string id)=>Array.Find(CampusCatalog.Places,p=>p.id==id);
        public bool Discover(int id)
        {
            if(id<0||id>=discoveries.Count||Vector3.Distance(game.player.transform.position,discoveries[id])>4.5f)return false;
            bool fresh=(found&(1<<id))==0;found|=1<<id;treasurePage=id;SaveKeeper();
            if(fresh)game.sound.Play(OdysseyCue.Memory);game.life.SetPanel("treasures");return true;
        }
        public bool TryInteract()
        {
            if(game.player.vehicle!=null){game.notice=game.player.vehicle.Exit()?"Back on foot. Your car stays where you parked it.":"Brake with Space before exiting. Leave room beside the car.";return true;}
            foreach(var car in cars)if(car.Enter(game.player)){game.notice="Cruiser: W/S accelerate or reverse · A/D steer · SPACE brake · E exit";return true;}
            for(int i=0;i<discoveries.Count;i++)if(Discover(i))return true;
            return false;
        }
        public bool Travel(CampusPlace place)
        {
            if(!game.player.TryExitVehicle()){game.notice="No clear space to leave this car. Move it into the open first.";return false;}
            if(!SafeArrival(place.Arrival,out var arrival)){game.notice="This arrival is obstructed. Try another nearby destination.";return false;}
            if(game.building)game.ToggleMode();game.player.Teleport(arrival);game.player.transform.rotation=Quaternion.identity;game.life.SetPanel("");
            game.notice=place.name+" · Exterior model. Explore nearby or use M to choose another destination.";return true;
        }
        public bool SafeArrival(Vector3 center,out Vector3 arrival)
        {
            bool enabled=game.player.body.enabled;game.player.body.enabled=false;Physics.SyncTransforms();
            foreach(float radius in new[]{0f,2f,4f,6f,10f})for(int direction=0;direction<8;direction++)
            {
                Vector3 p=center+Quaternion.Euler(0,direction*45,0)*Vector3.right*radius;
                bool clear=!Physics.CheckCapsule(p+Vector3.up*.5f,p+Vector3.up*1.5f,.45f,~0,QueryTriggerInteraction.Ignore);
                if(clear&&Physics.Raycast(p+Vector3.up,Vector3.down,out var ground,2,~0,QueryTriggerInteraction.Ignore))
                {arrival=new Vector3(p.x,ground.point.y+.08f,p.z);game.player.body.enabled=enabled;return true;}
            }
            game.player.body.enabled=enabled;arrival=center;return false;
        }
        void InitStyles()
        {
            if(styles)return;styles=true;
            heading=new GUIStyle(GUI.skin.label){font=AlbionUITheme.DisplayFont,fontSize=28,fontStyle=FontStyle.Bold,richText=false};text=new GUIStyle(GUI.skin.label){font=AlbionUITheme.BodyFont,fontSize=17,wordWrap=true,richText=false};small=new GUIStyle(text){font=AlbionUITheme.BodyFont,fontSize=14};
            button=new GUIStyle(GUI.skin.button){font=AlbionUITheme.BodyFont,fontSize=15,wordWrap=true,richText=false,border=new RectOffset(),padding=new RectOffset(12,12,5,5)};
            foreach(var s in new[]{button.normal,button.hover,button.active,button.focused}){s.background=Texture2D.whiteTexture;s.textColor=Color.white;}
        }
        void Label(float x,float y,float w,float h,string s,GUIStyle style=null)=>GUI.Label(new Rect(x,y,w,h),s,style??text);
        bool Button(float x,float y,float w,string s)=>GUI.Button(new Rect(x,y,w,38),s,button);
        void Card(Rect r,Color c){GUI.color=c;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=Color.white;}
        void FocusBox(Rect r,int index)
        {
            if(controllerFocus!=index)return;
            var old=GUI.color;GUI.color=AlbionUITheme.Cyan;
            GUI.DrawTexture(new Rect(r.x-3,r.y-3,r.width+6,3),Texture2D.whiteTexture);GUI.DrawTexture(new Rect(r.x-3,r.yMax,r.width+6,3),Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.x-3,r.y,3,r.height),Texture2D.whiteTexture);GUI.DrawTexture(new Rect(r.xMax,r.y,3,r.height),Texture2D.whiteTexture);GUI.color=old;
        }
        public void DrawPanel(string panel,float x)
        {
            InitStyles();GUI.backgroundColor=new Color(.26f,.17f,.39f);
            if(panel=="settings"){DrawSettings(x);return;}
            if(panel=="treasures"){DrawTreasures(x);return;}
            Label(x,88,1100,32,"61 DESTINATIONS  /  Campus exteriors · approximate map layout",small);
            Label(x,126,370,26,"FIND A BUILDING",small);search=GUI.TextField(new Rect(x,157,370,35),search,60);
            string[] categories={"All","Academic","Resources","Residential","Greek life","Athletics","Campus life","Nature"};
            for(int i=0;i<categories.Length;i++)if(GUI.Button(new Rect(x+(i%2)*189,201+(i/2)*31,181,28),(category==categories[i]?"● ":"")+categories[i],button)){category=categories[i];scroll=Vector2.zero;}
            var matches=new List<int>();for(int i=0;i<CampusCatalog.Places.Length;i++){var p=CampusCatalog.Places[i];if((category=="All"||p.category==category)&&(p.name.IndexOf(search,StringComparison.OrdinalIgnoreCase)>=0||p.id==search))matches.Add(i);}
            scroll=GUI.BeginScrollView(new Rect(x,336,380,355),scroll,new Rect(0,0,355,Mathf.Max(350,matches.Count*47)));
            for(int j=0;j<matches.Count;j++){int i=matches[j];if(Button(0,j*47,352,(selectedPlace==i?"● ":"")+CampusCatalog.Places[i].id+"  "+CampusCatalog.Places[i].name))selectedPlace=i;}
            if(matches.Count==0)Label(12,20,310,80,"No matching buildings. Try another name or choose All.");GUI.EndScrollView();
            float mx=x+410,my=126,mw=710,mh=390;
            Card(new Rect(mx,my,mw,mh),new Color(.12f,.21f,.19f));
            Func<Vector3,Vector2> map=p=>new Vector2(mx+(p.x/1.5f+400-35)/730*mw,my+(250-(p.z-400)/1.5f-20)/420*mh);
            foreach(float streetY in new[]{77f,123f,161f,241f,286f})
            {var v=map(CampusCatalog.Point(400,streetY));Card(new Rect(mx,v.y-2,mw,4),new Color(.08f,.12f,.13f));}
            foreach(float streetX in new[]{64f,118f,178f,236f,297f,363f,474f,590f,718f})
            {var v=map(CampusCatalog.Point(streetX,220));Card(new Rect(v.x-2,my,4,mh),new Color(.08f,.12f,.13f));}
            foreach(var p in CampusCatalog.Places)
            {
                var v=map(p.position);float w=Mathf.Max(7,p.width/1.5f/730*mw),h=Mathf.Max(7,p.depth/1.5f/420*mh);
                Card(new Rect(v.x-w/2,v.y-h/2,w,h),p==CampusCatalog.Places[selectedPlace]?new Color(1,.74f,.25f):p.category=="Athletics"?new Color(.15f,.65f,.58f):p.category=="Residential"||p.category=="Greek life"?new Color(.78f,.53f,.27f):new Color(.53f,.39f,.70f));
                if(GUI.Button(new Rect(v.x-w/2,v.y-h/2,w,h),GUIContent.none,GUIStyle.none))selectedPlace=Array.IndexOf(CampusCatalog.Places,p);
            }
            var playerDot=map(game.player.transform.position);if(OnCampus)Card(new Rect(playerDot.x-4,playerDot.y-4,8,8),Color.white);
            Label(mx+16,my+15,330,30,"ALBION COLLEGE     N ↑",small);Label(mx+16,my+mh-29,640,24,"Purple · academic    Gold · homes    Teal · athletics    White · you",small);
            var chosen=CampusCatalog.Places[selectedPlace];Label(mx,535,710,60,chosen.name,heading);Label(mx,600,690,40,chosen.category+" · Map "+chosen.id+" · Approximate exterior");
            if(Button(mx,650,215,"Travel to building"))Travel(chosen);
            if(Button(mx+232,650,215,"Campus discoveries"))game.life.SetPanel("treasures");
            if(Button(mx+464,650,230,"Official campus map"))Application.OpenURL(CampusCatalog.MapSource);
            FocusBox(new Rect(mx,650,215,38),0);FocusBox(new Rect(mx+232,650,215,38),1);FocusBox(new Rect(mx+464,650,230,38),2);
            if(Button(x,716,245,"Legacy Hall & builder"))game.life.SetPanel("map");
            FocusBox(new Rect(x,716,245,38),3);
            Label(x+270,717,840,45,"Buildings are exterior approximations; the original Legacy Hall still has eight walkable floors.",small);
        }
        void PreparePreview()
        {
            if(preview!=null)return;
            var t=new GameObject("Character dressing room");t.transform.position=new Vector3(0,-1000,0);preview=t.AddComponent<KeeperAvatar>();preview.Build(skin,outfit,hair,backpack);LayerPreview();
            previewCamera=new GameObject("Character preview camera").AddComponent<Camera>();previewCamera.enabled=false;previewCamera.transform.position=t.transform.position+new Vector3(2.4f,1.8f,4.2f);previewCamera.transform.LookAt(t.transform.position+Vector3.up*1.0f);
            previewCamera.cullingMask=1<<30;previewCamera.nearClipPlane=.1f;previewCamera.farClipPlane=10;previewCamera.fieldOfView=30;
            previewCamera.clearFlags=CameraClearFlags.SolidColor;previewCamera.backgroundColor=new Color(.08f,.1f,.14f);
            previewTexture=new RenderTexture(520,600,24);previewCamera.targetTexture=previewTexture;
        }
        void LayerPreview(){foreach(var t in preview.GetComponentsInChildren<Transform>(true))t.gameObject.layer=30;}
        void DrawSettings(float x)
        {
            PreparePreview();preview.transform.rotation=Quaternion.Euler(0,Mathf.Sin(Time.unscaledTime*.4f)*35,0);preview.Animate(0,false);
            if(Event.current.type==EventType.Repaint)previewCamera.Render();GUI.DrawTexture(new Rect(x,112,480,552),previewTexture,ScaleMode.ScaleToFit);
            Label(x,678,480,30,"YOUR KEEPER  /  Live character preview",small);
            float r=x+535;Label(r,111,580,40,"CHOOSE YOUR LOOK",heading);
            string[] presets={"Campus explorer","Trail keeper","Briton spirit"};
            for(int i=0;i<3;i++)if(Button(r+i*196,167,184,presets[i])){skin=i;outfit=i==0?0:i==1?1:4;hair=i;backpack=i!=2;RefreshAvatar();SaveKeeper();}
            FocusBox(new Rect(r,167,580,38),0);
            Label(r,227,560,26,"NAME",small);string n=GUI.TextField(new Rect(r,262,570,36),keeperName,24);if(n!=keeperName){keeperName=n;}
            Label(r,316,560,25,"SKIN TONE",small);
            GUI.contentColor=new Color(.12f,.09f,.07f);
            for(int i=0;i<5;i++){GUI.backgroundColor=KeeperAvatar.Skin[i];if(Button(r+i*114,351,102,skin==i?"Selected":"Tone "+(i+1))){skin=i;RefreshAvatar();SaveKeeper();}}
            FocusBox(new Rect(r,351,558,38),1);
            GUI.contentColor=Color.white;GUI.backgroundColor=new Color(.26f,.17f,.39f);Label(r,405,570,26,"OUTFIT COLOUR",small);
            for(int i=0;i<5;i++){GUI.backgroundColor=KeeperAvatar.Coats[i];if(Button(r+i*114,440,102,outfit==i?"Selected":"Look "+(i+1))){outfit=i;RefreshAvatar();SaveKeeper();}}
            FocusBox(new Rect(r,440,558,38),2);
            GUI.backgroundColor=new Color(.26f,.17f,.39f);
            string[] hairNames={"Dark hair","Brown hair","Shaved"};for(int i=0;i<3;i++)if(Button(r+i*196,495,184,(hair==i?"● ":"")+hairNames[i])){hair=i;RefreshAvatar();SaveKeeper();}
            FocusBox(new Rect(r,495,580,38),3);
            bool b=GUI.Toggle(new Rect(r,551,240,32),backpack,"Wear a backpack");if(b!=backpack){backpack=b;RefreshAvatar();SaveKeeper();}
            bool third=GUI.Toggle(new Rect(r+270,551,300,32),game.player.thirdPerson,"Third-person camera");if(third!=game.player.thirdPerson){game.player.thirdPerson=third;SaveKeeper();}
            FocusBox(new Rect(r,551,240,32),4);FocusBox(new Rect(r+270,551,300,32),5);
            Label(r,596,570,26,"CAMERA DISTANCE",small);game.player.cameraDistance=GUI.HorizontalSlider(new Rect(r,631,570,24),game.player.cameraDistance,2.5f,7);
            if(Button(r,678,275,"Save character & play")){SaveKeeper();game.life.SetPanel("");}
            if(Button(r+295,678,275,"Sound & game controls")){SaveKeeper();game.life.SetPanel("welcome");}
            FocusBox(new Rect(r,678,275,38),6);FocusBox(new Rect(r+295,678,275,38),7);
            Label(x,739,1100,32,"Each of the four local Keepers remembers its own appearance and discoveries. V switches camera view during play.",small);
        }
        void DrawTreasures(float x)
        {
            Label(x,97,1100,30,$"CAMPUS DISCOVERIES  /  {CountFound()}/7 collected · saved for {keeperName}",small);
            for(int i=0;i<7;i++)if(Button(x,155+i*60,400,((found&(1<<i))!=0?"✓ ":"○ ")+TreasureNames[i]))treasurePage=i;
            FocusBox(new Rect(x,155,400,38),0);
            float r=x+450;bool unlocked=(found&(1<<treasurePage))!=0;
            Label(r,157,640,80,TreasureNames[treasurePage],heading);
            Label(r,251,640,175,unlocked?TreasureText[treasurePage]:"Find the discovery marker on campus, walk close to it, and press E. You can travel to the clue below to start exploring.");
            Label(r,453,640,65,treasurePage==6?"ORIGINAL GAME FICTION":"REAL CAMPUS STORY · College source linked below",small);
            if(Button(r,554,300,"Travel to this clue"))
            {
                if(game.player.TryExitVehicle()){if(game.building)game.ToggleMode();game.player.Teleport(discoveries[treasurePage]+new Vector3(0,.08f,-3));game.player.transform.rotation=Quaternion.identity;game.life.SetPanel("");}
            }
            if(treasurePage<6&&Button(r+320,554,300,"Read college source"))Application.OpenURL(TreasureSources[treasurePage]);
            if(Button(r,619,300,"All campus buildings"))game.life.SetPanel("campus");
            FocusBox(new Rect(r,554,300,38),1);FocusBox(new Rect(r+320,554,300,38),2);FocusBox(new Rect(r,619,300,38),3);
            Label(r,690,635,65,"Brit's model and the treasure tokens are original game artwork. Discovery does not imply the real object can be taken.",small);
        }
        public int CountFound(){int count=0;for(int i=0;i<7;i++)if((found&(1<<i))!=0)count++;return count;}
        // Gameplay controls and prompts are drawn once by CampusHud.
    }
}
