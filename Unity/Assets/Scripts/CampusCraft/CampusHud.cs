using UnityEngine;

namespace AlbionOdyssey
{
    // Art-directed gameplay HUD. The world remains visible; information arrives in small,
    // animated cards with one clear action at a time.
    public sealed class CampusHud : MonoBehaviour
    {
        static readonly Color Ink=new Color(.035f,.04f,.065f,.94f),Panel=new Color(.055f,.065f,.10f,.88f),Gold=new Color(1f,.76f,.28f),Purple=new Color(.44f,.25f,.64f),Cyan=new Color(.35f,.84f,.92f);
        OdysseyGame game;Texture2D pixel;GUIStyle eyebrow,place,value,small,button,prompt;string lastNotice="";float noticeUntil;
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
        public string Context()
        {
            var b=CampusBuildings.Instance;var d=b?.NearbyDoor;var additional=b?.AdditionalInside();
            if(additional!=null&&additional.NearbyDoor()!=null){var door=additional.NearbyDoor();return "E  "+(door.IsOpen?"Close ":"Open ")+door.Label.ToLowerInvariant();}
            if(d!=null)return "E  "+(d.IsOpen?"Close ":"Open ")+d.Label.ToLowerInvariant();
            if(game.player.vehicle!=null)return "E  Leave vehicle   ·   Space  Brake";
            if(game.tour.InRoom)return "H  Room story   ·   E  Exit at the doorway";
            foreach(var car in game.campus.cars)if(Vector3.Distance(car.transform.position,game.player.transform.position)<4.8f)return "E  Drive campus car";
            foreach(var p in game.campus.discoveries)if(Vector3.Distance(p,game.player.transform.position)<4.5f)return "E  Collect discovery";
            if(game.player.TryTarget(out var hit)&&(hit.collider.GetComponent<MemoryMarker>()!=null||hit.collider.GetComponent<GuideMarker>()!=null))return "E  Pick up / interact";
            if(additional!=null)return "H  "+additional.Place.name+" story   ·   Stairs and rooms to explore";
            if(b!=null&&b.Inside)return "H  Ferguson’s story   ·   Stairs at the east end →";
            return game.tour.Nearby()!=null?"H  Read building story   ·   G  All stories":"WASD / arrows  Move   ·   G  Stories";
        }
        void Interact(){var d=CampusBuildings.Instance?.NearbyDoor;if(d!=null){d.Toggle(game.player);return;}if(game.tour.InRoom){game.tour.ExitRoom();return;}game.Interact();}
        void InitStyles()
        {
            if(eyebrow!=null)return;
            eyebrow=new GUIStyle(GUI.skin.label){font=AlbionUITheme.BodyFont,fontSize=11,fontStyle=FontStyle.Bold};eyebrow.normal.textColor=Cyan;
            place=new GUIStyle(GUI.skin.label){font=AlbionUITheme.DisplayFont,fontSize=22,fontStyle=FontStyle.Bold};place.normal.textColor=Color.white;
            value=new GUIStyle(GUI.skin.label){font=AlbionUITheme.BodyFont,fontSize=16,fontStyle=FontStyle.Bold};value.normal.textColor=Gold;
            small=new GUIStyle(GUI.skin.label){font=AlbionUITheme.BodyFont,fontSize=14,wordWrap=true};small.normal.textColor=new Color(.83f,.84f,.9f);
            prompt=new GUIStyle(small){fontSize=16,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleCenter};prompt.normal.textColor=Color.white;
            button=new GUIStyle(GUI.skin.button){font=AlbionUITheme.BodyFont,fontSize=14,fontStyle=FontStyle.Bold,padding=new RectOffset(13,13,7,7),border=new RectOffset()};
            foreach(var s in new[]{button.normal,button.hover,button.active,button.focused}){s.background=pixel;s.textColor=Color.white;}
        }
        void Compass(float center,float top)
        {
            float cx=center,heading=game.player.transform.eulerAngles.y;Card(new Rect(cx-170,top+24,340,40));
            string[] dirs={"N","NE","E","SE","S","SW","W","NW"};
            for(int i=0;i<dirs.Length;i++){float relative=Mathf.DeltaAngle(heading,i*45);float x=cx+relative*2.8f;if(x<cx-152||x>cx+152)continue;GUI.Label(new Rect(x-16,top+32,32,22),dirs[i],new GUIStyle(eyebrow){alignment=TextAnchor.MiddleCenter,fontSize=12});}
            Fill(new Rect(cx-2,top+58,4,4),Gold);
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
            if(game.player.vehicle==null){GUI.Label(new Rect(right-304,top+55,260,25),"ENERGY  "+Mathf.RoundToInt(game.player.Stamina*100)+"%",value);Fill(new Rect(right-304,top+88,258,7),new Color(.13f,.15f,.20f));Fill(new Rect(right-304,top+88,258*game.player.Stamina,7),Cyan);}
            else GUI.Label(new Rect(right-304,top+57,260,30),Mathf.RoundToInt(Mathf.Abs(game.player.vehicle.speed)*3.6f)+"  KM/H",value);
            Card(new Rect(left+24,top+132,330,74),true);GUI.Label(new Rect(left+44,top+143,280,16),"CURRENT OBJECTIVE",eyebrow);GUI.Label(new Rect(left+44,top+164,286,34),Objective(),small);
            string progress=OdysseyState.Count(game.state.Current.memories)+" / 12 memories   ·   Beacon "+game.state.beacon+" / 24";GUI.Label(new Rect(left+44,top+190,286,18),progress,eyebrow);
            TargetTag(scale);
            string action=Context();float promptY=bottom-78;Card(new Rect((left+right)*.5f-285,promptY,570,52),true);GUI.Label(new Rect((left+right)*.5f-270,promptY+8,540,34),action,prompt);
            string toast=OdysseyAccessibility.CaptionsEnabled?game.sound.AchievementCaption:"";
            if(toast.Length>0){float y=top+130-Mathf.Sin(Time.unscaledTime*2f)*2f;Card(new Rect((left+right)*.5f-225,y,450,72),true);GUI.Label(new Rect((left+right)*.5f-205,y+10,410,17),"ACHIEVEMENT UNLOCKED",eyebrow);GUI.Label(new Rect((left+right)*.5f-205,y+32,410,28),toast,value);}
            else if(Time.unscaledTime<noticeUntil&&lastNotice.Length>0&&!lastNotice.StartsWith("Welcome")&&!lastNotice.StartsWith("G opens")){float fade=Mathf.Clamp01(Mathf.Min(1,(noticeUntil-Time.unscaledTime)*2));GUI.color=new Color(1,1,1,fade);Card(new Rect(left+24,top+220,330,66));GUI.Label(new Rect(left+44,top+232,286,42),lastNotice,small);GUI.color=Color.white;}
            if(game.player.pointerControls){float x=left+35,y=bottom-227;Card(new Rect(x-11,y-11,220,140));game.player.buttonMove=new Vector2((GUI.RepeatButton(new Rect(x+110,y+44,48,40),"→",button)?1:0)-(GUI.RepeatButton(new Rect(x,y+44,48,40),"←",button)?1:0),(GUI.RepeatButton(new Rect(x+55,y,48,40),"↑",button)?1:0)-(GUI.RepeatButton(new Rect(x+55,y+44,48,40),"↓",button)?1:0));game.player.buttonTurn=(GUI.RepeatButton(new Rect(x+164,y+44,34,40),"↻",button)?1:0)-(GUI.RepeatButton(new Rect(x+164,y,34,40),"↺",button)?1:0);if(GUI.Button(new Rect(x,y+92,130,30),"Interact",button))Interact();if(GUI.Button(new Rect(x+138,y+92,60,30),"Jump",button))game.player.buttonJump=true;}
            if(GUI.Button(new Rect(right-248,top+130,104,32),"MAP  M",button))game.life.SetPanel("campus");if(GUI.Button(new Rect(right-136,top+130,112,32),"MENU  ESC",button))game.shell.ShowLaunch();
            GUI.Label(new Rect(right-310,bottom-28,286,20),"V camera   ·   J journal   ·   F2 build",eyebrow);GUI.matrix=old;GUI.backgroundColor=oldBg;
        }
    }
}
