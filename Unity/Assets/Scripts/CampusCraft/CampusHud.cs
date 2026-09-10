using UnityEngine;

namespace AlbionOdyssey
{
    // Art-directed gameplay HUD. The world remains visible; information arrives in small,
    // animated cards with one clear action at a time.
    public sealed class CampusHud : MonoBehaviour
    {
        static readonly Color Ink=new Color(.035f,.04f,.065f,.94f),Panel=new Color(.055f,.065f,.10f,.88f),Gold=new Color(1f,.76f,.28f),Purple=new Color(.44f,.25f,.64f),Cyan=new Color(.35f,.84f,.92f);
        OdysseyGame game;Texture2D pixel;GUIStyle eyebrow,place,value,small,button,prompt,mapPlayer,mapName;string lastNotice="";float noticeUntil;
        public void Setup(OdysseyGame owner){game=owner;pixel=new Texture2D(1,1,TextureFormat.RGBA32,false);pixel.SetPixel(0,0,Color.white);pixel.Apply();}
        void Update(){if(game!=null&&game.notice!=lastNotice){lastNotice=game.notice;noticeUntil=Time.unscaledTime+5;}}
        void Fill(Rect r,Color c){var old=GUI.color;GUI.color=c;GUI.DrawTexture(r,pixel);GUI.color=old;}
        void Card(Rect r,bool accent=false){Fill(r,Panel);if(accent)Fill(new Rect(r.x,r.y,4,r.height),Gold);}
        string CurrentPlace()
        {
            var buildings=CampusBuildings.Instance;var extra=buildings?.AdditionalInside();
            string p=extra!=null?extra.Location:buildings!=null&&buildings.Inside?buildings.Location:game.tour.InRoom?"Wesley Hall":game.campus.OnCampus?game.campus.Nearest.name:game.life.Location;
            return p.Length>31?p.Substring(0,29)+"…":p;
        }
        string Objective()
        {
            int found=OdysseyState.Count(game.state.Current.memories);
            if(found<12)return "Discover the next memory in Legacy Hall";
            if(game.state.beacon<24)return "Bring acorns to the shared Beacon";
            if(game.state.school!=null&&game.state.school.active<0)return "Create a course for your campus";
            return "Your charter is complete — keep exploring";
        }
        string InteractGlyph()
        {
            if(game.xr!=null&&game.xr.Active)return "TRIGGER";
            if(game.player.pointerControls)return "INTERACT";
            if(AlbionUIInput.ControllerPresent)return "B / CIRCLE";
            return "E / F";
        }
        public string Context()
        {
            string use=InteractGlyph();
            var b=CampusBuildings.Instance;var d=b?.NearbyDoor;var additional=b?.AdditionalInside();
            if(additional!=null&&additional.NearbyDoor()!=null){var door=additional.NearbyDoor();bool tooClose=door.IsOpen&&Vector3.Distance(game.player.transform.position,door.ClosedPosition)<1.2f;return use+"  "+(tooClose?"Move clear to close ":door.IsOpen?"Close ":"Open ")+door.Label.ToLowerInvariant();}
            if(d!=null){bool tooClose=d.IsOpen&&Vector3.Distance(game.player.transform.position,d.ClosedPosition)<1.2f;return use+"  "+(tooClose?"Move clear to close ":d.IsOpen?"Close ":"Open ")+d.Label.ToLowerInvariant();}
            if(game.player.vehicle!=null)return use+"  Leave vehicle   ·   "+(AlbionUIInput.ControllerPresent?"A / CROSS":"Space")+"  Brake";
            if(game.tour.InRoom)return "H  Room story   ·   E  Exit at the doorway";
            foreach(var car in game.campus.cars)if(Vector3.Distance(car.transform.position,game.player.transform.position)<4.8f)return use+"  Drive campus car";
            foreach(var p in game.campus.discoveries)if(Vector3.Distance(p,game.player.transform.position)<4.5f)return use+"  Collect discovery";
            if(game.player.TryTarget(out var hit)&&(hit.collider.GetComponent<MemoryMarker>()!=null||hit.collider.GetComponent<GuideMarker>()!=null))return use+"  Pick up / interact";
            if(additional!=null)return "H  "+additional.Place.name+" story   ·   Stairs and rooms to explore";
            if(b!=null&&b.Inside)return "H  Ferguson’s story   ·   Stairs at the east end →";
            return game.tour.Nearby()!=null?"H  Read building story   ·   G  All stories":"WASD / arrows  Move   ·   G  Stories";
        }
        void Interact(){var d=CampusBuildings.Instance?.NearbyDoor;if(d!=null){d.Toggle(game.player);return;}if(game.tour.InRoom){game.tour.ExitRoom();return;}game.Interact();}
        void InitStyles()
        {
            if(eyebrow!=null)return;
            eyebrow=new GUIStyle(GUI.skin.label){font=AlbionUITheme.BodyFont,fontSize=AlbionUITheme.TextSize(11),fontStyle=FontStyle.Bold};eyebrow.normal.textColor=Cyan;
            place=new GUIStyle(GUI.skin.label){font=AlbionUITheme.DisplayFont,fontSize=AlbionUITheme.TextSize(22),fontStyle=FontStyle.Bold};place.normal.textColor=Color.white;
            value=new GUIStyle(GUI.skin.label){font=AlbionUITheme.BodyFont,fontSize=AlbionUITheme.TextSize(16),fontStyle=FontStyle.Bold};value.normal.textColor=Gold;
            small=new GUIStyle(GUI.skin.label){font=AlbionUITheme.BodyFont,fontSize=AlbionUITheme.TextSize(14),wordWrap=true};small.normal.textColor=new Color(.83f,.84f,.9f);
            prompt=new GUIStyle(small){fontSize=AlbionUITheme.TextSize(16),fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleCenter};prompt.normal.textColor=Color.white;
            button=new GUIStyle(GUI.skin.button){font=AlbionUITheme.BodyFont,fontSize=AlbionUITheme.TextSize(14),fontStyle=FontStyle.Bold,padding=new RectOffset(13,13,7,7),border=new RectOffset()};
            foreach(var s in new[]{button.normal,button.hover,button.active,button.focused}){s.background=pixel;s.textColor=Color.white;}
        }
        void Compass(float center,float top)
        {
            float cx=center,heading=game.player.transform.eulerAngles.y;Card(new Rect(cx-170,top+24,340,40));
            string[] dirs={"N","NE","E","SE","S","SW","W","NW"};
            for(int i=0;i<dirs.Length;i++){float relative=Mathf.DeltaAngle(heading,i*45);float x=cx+relative*2.8f;if(x<cx-152||x>cx+152)continue;GUI.Label(new Rect(x-16,top+32,32,22),dirs[i],new GUIStyle(eyebrow){alignment=TextAnchor.MiddleCenter,fontSize=12});}
            Fill(new Rect(cx-2,top+58,4,4),Gold);
        }
        Color MapColor(string category)
        {
            if(category=="Academic")return Gold;
            if(category=="Residential")return Cyan;
            if(category=="Athletics")return new Color(.45f,.85f,.48f);
            if(category=="Nature")return new Color(.48f,.82f,.46f);
            if(category=="Greek life")return new Color(.78f,.48f,.88f);
            return new Color(.72f,.74f,.82f);
        }
        void Minimap(Rect r)
        {
            Card(r);
            GUI.Label(new Rect(r.x+14,r.y+10,r.width-28,18),"CAMPUS MAP  ·  180 M",eyebrow);
            Rect map=new Rect(r.x+12,r.y+35,r.width-24,r.width-47);Fill(map,new Color(.025f,.07f,.085f,.96f));
            var grid=new Color(.20f,.52f,.55f,.18f);Fill(new Rect(map.x+map.width*.5f,map.y,1,map.height),grid);Fill(new Rect(map.x,map.y+map.height*.5f,map.width,1),grid);
            float radius=Mathf.Min(map.width,map.height)*.46f,range=180f;Vector3 player=game.player.transform.position;
            if(mapPlayer==null){mapPlayer=new GUIStyle(eyebrow){fontSize=AlbionUITheme.TextSize(17),alignment=TextAnchor.MiddleCenter};mapPlayer.normal.textColor=Gold;mapName=new GUIStyle(eyebrow){fontSize=AlbionUITheme.TextSize(8)};}
            CampusPlace closest=null;float closestDistance=float.MaxValue;
            foreach(var placeInfo in CampusCatalog.Places)
            {
                Vector3 delta=placeInfo.position-player;float distance=new Vector2(delta.x,delta.z).magnitude;
                if(distance<closestDistance){closestDistance=distance;closest=placeInfo;}
                if(distance>range)continue;
                float px=map.x+map.width*.5f+Mathf.Clamp(delta.x/range,-1,1)*radius;
                float py=map.y+map.height*.5f-Mathf.Clamp(delta.z/range,-1,1)*radius;
                Color marker=MapColor(placeInfo.category);Fill(new Rect(px-3,py-3,6,6),marker);
                if(distance<42f){mapName.normal.textColor=marker;GUI.Label(new Rect(px+6,py-7,Mathf.Min(110,map.xMax-px-5),18),placeInfo.name,mapName);}
            }
            if(game.online!=null&&game.online.Active)
            {
                Color onlineColor=new Color(1f,.40f,.78f);
                for(int i=0;i<game.online.RemoteCount;i++)
                {
                    Vector3 delta=game.online.RemotePosition(i)-player;float distance=new Vector2(delta.x,delta.z).magnitude;
                    if(distance>range)continue;
                    float px=map.x+map.width*.5f+Mathf.Clamp(delta.x/range,-1,1)*radius;
                    float py=map.y+map.height*.5f-Mathf.Clamp(delta.z/range,-1,1)*radius;
                    Fill(new Rect(px-4,py-4,8,8),onlineColor);
                    if(distance<42f){mapName.normal.textColor=onlineColor;GUI.Label(new Rect(px+7,py-7,Mathf.Min(58,map.xMax-px-5),18),"ONLINE",mapName);}
                }
            }
            // The player stays centered while the world map remains north-up.
            GUI.Label(new Rect(map.center.x-12,map.center.y-13,24,24),"▲",mapPlayer);
            GUI.Label(new Rect(map.x+4,map.y+2,18,18),"N",eyebrow);
            string nearest=closest==null?"Explore the grounds":closest.name;
            GUI.Label(new Rect(r.x+14,r.yMax-25,r.width-28,18),nearest.Length>28?nearest.Substring(0,26)+"…":nearest,small);
        }
        void CampusPulse(Rect r)
        {
            Card(r);
            GUI.Label(new Rect(r.x+14,r.y+8,r.width-28,16),"CAMPUS PULSE  ·  LIVE",eyebrow);
            int moving=game.world==null?0:game.world.MovingStudentCount;
            int seated=game.world==null||game.world.activities==null?0:game.world.activities.SeatedAgentCount;
            int wildlife=game.campus==null||game.campus.fauna==null?0:game.campus.fauna.ActiveCount;
            string weather=game.weather!=null&&game.weather.IsSnowing?"SNOW":"CLEAR";
            GUI.Label(new Rect(r.x+14,r.y+28,96,22),moving+"  MOVING",value);
            GUI.Label(new Rect(r.x+112,r.y+28,96,22),seated+"  SEATED",value);
            GUI.Label(new Rect(r.x+210,r.y+28,106,22),wildlife+"  SQUIRRELS",value);
            GUI.Label(new Rect(r.x+14,r.y+49,r.width-28,14),weather+"  ·  "+(game.world==null?0:game.world.StudentCount)+" STUDENTS ON CAMPUS",eyebrow);
        }
        string InputFooter()
        {
            if(game.xr!=null&&game.xr.Active)return "LEFT STICK  Move   ·   RIGHT STICK  Turn   ·   TRIGGER  Interact   ·   GRIP  Grab   ·   MENU  Pause";
            if(game.player.pointerControls)return "ON-SCREEN ARROWS  Move   ·   TURN  Aim   ·   INTERACT  Pick up / talk   ·   O  Hide controls";
            if(AlbionUIInput.ControllerPresent)return "LEFT STICK  Move   ·   RIGHT STICK  Look   ·   A / CROSS  Jump   ·   B / CIRCLE  Interact   ·   MENU  Pause";
            return "V  Camera   ·   J  Journal   ·   F2  Build   ·   E / F  Interact   ·   O  Screen controls";
        }
        void TargetTag(float scale)
        {
            if(game.player.eyes==null||!game.player.TryTarget(out var hit))return;
            var marker=hit.collider.GetComponentInParent<MemoryMarker>();var guide=hit.collider.GetComponentInParent<GuideMarker>();var station=hit.collider.GetComponentInParent<CampusActivityStation>();
            if(marker==null&&guide==null&&station==null)return;
            Vector3 point=hit.collider.transform.position+Vector3.up*.9f;Vector3 screen=game.player.eyes.WorldToScreenPoint(point);if(screen.z<0)return;
            Rect safe=AlbionUITheme.SafeArea(scale);float x=Mathf.Clamp(screen.x/scale,safe.xMin+118,safe.xMax-118),y=Mathf.Clamp((Screen.height-screen.y)/scale,safe.yMin+40,safe.yMax-40);string text=marker!=null?"MEMORY  "+(marker.id+1).ToString("00"):guide!=null?"PIP  CAMPUS GUIDE":"CLASSROOM ACTIVITY";
            Card(new Rect(x-112,y-26,224,30),true);GUI.Label(new Rect(x-102,y-21,204,21),text,new GUIStyle(eyebrow){alignment=TextAnchor.MiddleCenter});
        }
        void OnGUI()
        {
            if(game==null||game.player==null||game.life.PanelOpen||game.building||game.journalOpen)return;
            InitStyles();var old=GUI.matrix;var oldBg=GUI.backgroundColor;float scale=Mathf.Min(Screen.width/1440f,Screen.height/900f);GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);float w=Screen.width/scale,h=Screen.height/scale;Rect safe=AlbionUITheme.SafeArea(scale);float left=safe.xMin,right=safe.xMax,top=safe.yMin,bottom=safe.yMax;
            GUI.backgroundColor=Purple;string p=CurrentPlace();
            Card(new Rect(left+24,top+24,330,92),true);GUI.Label(new Rect(left+44,top+34,290,18),"ALBION COLLEGE  /  LIVE",eyebrow);GUI.Label(new Rect(left+44,top+55,290,30),p,place);GUI.Label(new Rect(left+44,top+84,290,22),game.campus.keeperName+"   ·   "+game.state.Current.acorns+" ACORNS",small);
            Compass((left+right)*.5f,top);
            Card(new Rect(right-326,top+24,302,92));GUI.Label(new Rect(right-304,top+35,260,18),game.player.vehicle==null?"KEEPER STATUS":"CAMPUS CAR",eyebrow);
            string online=game.online!=null&&game.online.Active?"  ·  "+game.online.RemoteCount+" ONLINE":"";
            if(game.player.vehicle==null){GUI.Label(new Rect(right-304,top+55,260,25),"ENERGY  "+Mathf.RoundToInt(game.player.Stamina*100)+"%"+online,value);Fill(new Rect(right-304,top+88,258,7),new Color(.13f,.15f,.20f));Fill(new Rect(right-304,top+88,258*game.player.Stamina,7),Cyan);}
            else GUI.Label(new Rect(right-304,top+57,260,30),Mathf.RoundToInt(Mathf.Abs(game.player.vehicle.speed)*3.6f)+"  KM/H"+online,value);
            Card(new Rect(left+24,top+132,330,74),true);GUI.Label(new Rect(left+44,top+143,280,16),"CURRENT OBJECTIVE",eyebrow);GUI.Label(new Rect(left+44,top+164,286,34),Objective(),small);
            string progress=OdysseyState.Count(game.state.Current.memories)+" / 12 memories   ·   Beacon "+game.state.beacon+" / 24";GUI.Label(new Rect(left+44,top+190,286,18),progress,eyebrow);
            TargetTag(scale);
            string action=Context();float promptY=bottom-78;Card(new Rect((left+right)*.5f-285,promptY,570,52),true);GUI.Label(new Rect((left+right)*.5f-270,promptY+8,540,34),action,prompt);
            string toast=OdysseyAccessibility.CaptionsEnabled?game.sound.AchievementCaption:"";
            if(toast.Length>0){float y=top+130-(OdysseyAccessibility.ReducedMotion?0:Mathf.Sin(Time.unscaledTime*2f)*2f);Card(new Rect((left+right)*.5f-225,y,450,72),true);GUI.Label(new Rect((left+right)*.5f-205,y+10,410,17),"ACHIEVEMENT UNLOCKED",eyebrow);GUI.Label(new Rect((left+right)*.5f-205,y+32,410,28),toast,value);}
            else if(Time.unscaledTime<noticeUntil&&lastNotice.Length>0&&!lastNotice.StartsWith("Welcome")&&!lastNotice.StartsWith("G opens")){float fade=OdysseyAccessibility.ReducedMotion?1:Mathf.Clamp01(Mathf.Min(1,(noticeUntil-Time.unscaledTime)*2));GUI.color=new Color(1,1,1,fade);Card(new Rect(left+24,top+220,330,66));GUI.Label(new Rect(left+44,top+232,286,42),lastNotice,small);GUI.color=Color.white;}
            CampusPulse(new Rect(left+24,top+296,330,68));
            if(game.player.pointerControls){float x=left+35,y=bottom-227;Card(new Rect(x-11,y-11,220,140));game.player.buttonMove=new Vector2((GUI.RepeatButton(new Rect(x+110,y+44,48,40),"→",button)?1:0)-(GUI.RepeatButton(new Rect(x,y+44,48,40),"←",button)?1:0),(GUI.RepeatButton(new Rect(x+55,y,48,40),"↑",button)?1:0)-(GUI.RepeatButton(new Rect(x+55,y+44,48,40),"↓",button)?1:0));game.player.buttonTurn=(GUI.RepeatButton(new Rect(x+164,y+44,34,40),"↻",button)?1:0)-(GUI.RepeatButton(new Rect(x+164,y,34,40),"↺",button)?1:0);if(GUI.Button(new Rect(x,y+92,130,30),"Interact",button))Interact();if(GUI.Button(new Rect(x+138,y+92,60,30),"Jump",button))game.player.buttonJump=true;}
            float mapSize=Mathf.Clamp(224f,(right-left)*.18f,224f);Minimap(new Rect(right-mapSize,top+130,mapSize,mapSize));
            if(GUI.Button(new Rect(right-mapSize,top+130+mapSize+10,104,32),"MAP  M",button))game.life.SetPanel("campus");if(GUI.Button(new Rect(right-112,top+130+mapSize+10,112,32),"MENU  ESC",button))game.shell.ShowLaunch();
            GUI.Label(new Rect(left+24,bottom-28,right-left-48,20),InputFooter(),eyebrow);GUI.matrix=old;GUI.backgroundColor=oldBg;
        }
    }
}
