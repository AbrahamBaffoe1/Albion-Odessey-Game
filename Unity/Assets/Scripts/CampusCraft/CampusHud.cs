using UnityEngine;
namespace AlbionOdyssey
{
    // One owner for gameplay HUD: location, navigation, one action and one notification.
    public sealed class CampusHud : MonoBehaviour
    {
        OdysseyGame game;GUIStyle label,small,button;string lastNotice="";float noticeUntil;
        public void Setup(OdysseyGame owner){game=owner;}
        void Update(){if(game!=null&&game.notice!=lastNotice){lastNotice=game.notice;noticeUntil=Time.unscaledTime+5;}}
        void Card(Rect r){var c=GUI.color;GUI.color=new Color(.035f,.045f,.052f,.87f);GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=c;}
        public string Context()
        {
            var b=CampusBuildings.Instance;var d=b?.NearbyDoor;
            if(d!=null)return "E  "+(d.IsOpen?"Close ":"Open ")+d.Label.ToLowerInvariant();
            if(game.player.vehicle!=null)return "E  Leave vehicle   ·   Space  Brake";
            if(game.tour.InRoom)return "H  Room story   ·   E  Exit at the doorway";
            foreach(var car in game.campus.cars)if(Vector3.Distance(car.transform.position,game.player.transform.position)<4.8f)return "E  Drive campus car";
            foreach(var p in game.campus.discoveries)if(Vector3.Distance(p,game.player.transform.position)<4.5f)return "E  Collect discovery";
            if(game.player.TryTarget(out var hit)&&(hit.collider.GetComponent<MemoryMarker>()!=null||hit.collider.GetComponent<GuideMarker>()!=null))return "E  Pick up / interact";
            if(b!=null&&b.Inside)return "H  Ferguson’s story   ·   Stairs at the east end →";
            return game.tour.Nearby()!=null?"H  Read building story   ·   G  All stories":"WASD / arrows  Move   ·   G  Stories";
        }
        void Interact(){var d=CampusBuildings.Instance?.NearbyDoor;if(d!=null){d.Toggle(game.player);return;}if(game.tour.InRoom){game.tour.ExitRoom();return;}game.Interact();}
        void OnGUI()
        {
            if(game==null||game.life.PanelOpen||game.building||game.journalOpen)return;
            if(label==null){label=new GUIStyle(GUI.skin.label){fontSize=20,fontStyle=FontStyle.Bold,richText=false,wordWrap=true};small=new GUIStyle(label){fontSize=14,fontStyle=FontStyle.Normal};button=new GUIStyle(GUI.skin.button){fontSize=15,richText=false,border=new RectOffset(),padding=new RectOffset(12,12,5,5)};foreach(var s in new[]{button.normal,button.hover,button.active,button.focused}){s.background=Texture2D.whiteTexture;s.textColor=new Color(.94f,.94f,.9f);}}
            var old=GUI.matrix;var bc=GUI.backgroundColor;float scale=Mathf.Min(Screen.width/1280f,Screen.height/800f);GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));float w=Screen.width/scale,h=Screen.height/scale;GUI.color=Color.white;GUI.backgroundColor=new Color(.16f,.12f,.23f);
            var buildings=CampusBuildings.Instance;
            string place=buildings!=null&&buildings.Inside?buildings.Location:game.tour.InRoom?"Wesley Hall":game.campus.OnCampus?game.campus.Nearest.name:game.life.Location;
            if(place.Length>33)place=place.Substring(0,30)+"…";
            Card(new Rect(24,24,350,72));GUI.Label(new Rect(40,33,322,28),place,label);GUI.Label(new Rect(40,65,322,23),game.campus.keeperName+"  ·  "+game.state.Current.acorns+" acorns",small);
            if(GUI.Button(new Rect(w-246,24,98,38),"Map · M",button))game.life.SetPanel("campus");
            if(GUI.Button(new Rect(w-138,24,114,38),"Menu · Esc",button))game.shell.ShowLaunch();
            string action=Context();Card(new Rect(w/2-270,h-74,540,46));GUI.Label(new Rect(w/2-255,h-63,510,34),action,small);
            GUI.Label(new Rect(w-260,h-29,236,22),"O  Movement buttons   ·   V  Camera",small);
            if(Cursor.lockState==CursorLockMode.Locked){GUI.color=new Color(1,1,1,.65f);GUI.DrawTexture(new Rect(w/2-1,h/2-1,3,3),Texture2D.whiteTexture);GUI.color=Color.white;}
            string toast=game.sound.AchievementCaption;
            if(toast.Length>0){Card(new Rect(w/2-220,24,440,67));GUI.Label(new Rect(w/2-202,34,405,50),"Achievement unlocked\n"+toast,small);}
            // Ordinary game messages appear briefly and never under another panel or achievement.
            else if(Time.unscaledTime<noticeUntil&&lastNotice.Length>0&&!lastNotice.StartsWith("Welcome")&&!lastNotice.StartsWith("G opens")){Card(new Rect(24,106,350,72));GUI.Label(new Rect(40,116,318,56),lastNotice,small);}
            if(game.player.pointerControls)
            {
                float x=35,y=h-227;Card(new Rect(x-11,y-11,220,140));
                game.player.buttonMove=new Vector2((GUI.RepeatButton(new Rect(x+110,y+44,48,40),"→",button)?1:0)-(GUI.RepeatButton(new Rect(x,y+44,48,40),"←",button)?1:0),(GUI.RepeatButton(new Rect(x+55,y,48,40),"↑",button)?1:0)-(GUI.RepeatButton(new Rect(x+55,y+44,48,40),"↓",button)?1:0));
                game.player.buttonTurn=(GUI.RepeatButton(new Rect(x+164,y+44,34,40),"↻",button)?1:0)-(GUI.RepeatButton(new Rect(x+164,y,34,40),"↺",button)?1:0);
                if(GUI.Button(new Rect(x,y+92,130,30),"Interact",button))Interact();if(GUI.Button(new Rect(x+138,y+92,60,30),"Jump",button))game.player.buttonJump=true;
            }
            GUI.matrix=old;GUI.backgroundColor=bc;
        }
    }
}
