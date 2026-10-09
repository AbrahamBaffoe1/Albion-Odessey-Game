using System;
using UnityEngine;
namespace AlbionOdyssey
{
    [Serializable] public sealed class RoomChat {public int seq;public string id,display,text;}
    [Serializable] public sealed class RoomSeat {public string id,driver;public int car,seat;}
    [Serializable] public sealed class RoomCar {public int car;public string driver;public float x,y,z,yaw,speed;public RoomSeat[] seats;}
    [Serializable] public sealed class RoomRating {public string id;public int total,count;}
    [Serializable] public sealed class RoomSocialState {public RoomChat[] messages;public RoomCar[] cars;public RoomRating[] ratings;}
    [Serializable] sealed class RideCommand {public string type="ride",action;public int car,stars;public float x,y,z,yaw,speed;}
    [Serializable] sealed class ChatCommand {public string type="chat",text;}
    public sealed class CampusRoomSocial : MonoBehaviour
    {
        OdysseyGame game;CampusSharedSession session;RoomSocialState state;string draft="",feedback="",pending="";Vector2 scroll;int lastSeq,localCar=-1,seat;float sendAt;bool rateAvailable;int ratingChoice=5;
        readonly ConversationPresentation presentation=new ConversationPresentation();bool wasOpen;
        public bool Passenger=>localCar>=0&&seat>0;
        public void Setup(OdysseyGame g,CampusSharedSession s){game=g;session=s;}
        public void ResetRoom(){if(game.player.vehicle!=null&&localCar>=0){var old=game.player.vehicle;old.TryExitPoint(out var point);old.driver=null;game.player.vehicle=null;game.player.Teleport(point);game.player.body.enabled=true;}foreach(var c in game.campus.cars)c.Replicated=false;localCar=-1;seat=0;state=null;lastSeq=0;draft="";rateAvailable=false;pending="";}
        public void Result(bool ok,string action,bool canRate){if(!ok)feedback="Could not "+action+". Park nearby and check the available seats.";else feedback=action=="chat"?"Message sent.":action=="rate"?"Thanks for rating your ride.":"";if(ok&&action=="exit")rateAvailable=canRate;if(ok&&action=="rate")rateAvailable=false;if(!ok)game.notice=feedback;pending="";}
        public void Accept(RoomSocialState value)
        {
            if(value==null)return;state=value;int nextCar=-1,nextSeat=0;
            foreach(var c in state.cars??Array.Empty<RoomCar>())
            {
                if(c.car<0||c.car>=game.campus.cars.Count)continue;var model=game.campus.cars[c.car];
                bool mine=c.driver==game.accounts.UserId;model.Replicated=!mine;
                if(!mine)model.ApplyReplicated(new Vector3(c.x,c.y,c.z),c.yaw,c.speed);
                if(mine){nextCar=c.car;nextSeat=0;}
                foreach(var r in c.seats??Array.Empty<RoomSeat>())if(r.id==game.accounts.UserId){nextCar=c.car;nextSeat=r.seat;}
            }
            if(nextCar!=localCar||nextSeat!=seat)
            {
                if(localCar>=0&&nextCar<0){var old=game.campus.cars[localCar];old.driver=null;game.player.vehicle=null;game.player.body.enabled=true;old.TryExitPoint(out var point);game.player.Teleport(point);}
                localCar=nextCar;seat=nextSeat;
                if(localCar>=0){var c=game.campus.cars[localCar];game.player.vehicle=c;game.player.body.enabled=false;c.driver=seat==0?game.player:null;}
            }
            var messages=state.messages??Array.Empty<RoomChat>();if(messages.Length>0&&messages[messages.Length-1].seq>lastSeq){lastSeq=messages[messages.Length-1].seq;scroll.y=100000;if(game.life.panel!="conversation")game.notice="New room message · press F7 to read and reply.";}
        }
        public bool Interact()
        {
            if(!session.Joined)return false;
            
            if(localCar>=0){if(!game.campus.cars[localCar].TryExitPoint(out _)){game.notice="Park with enough space to open the doors.";return true;}Send(new RideCommand{action="exit",car=localCar});return true;}
            for(int i=0;i<game.campus.cars.Count;i++)if(Vector3.Distance(game.player.transform.position,game.campus.cars[i].transform.position)<4.8f){if(!session.SocialSupported){game.notice="This room server needs the shared-car update. Leave the room to drive solo.";return true;}var c=game.campus.cars[i];RoomCar network=Array.Find(state?.cars??Array.Empty<RoomCar>(),r=>r.car==i);Send(new RideCommand{action=network!=null&&!string.IsNullOrEmpty(network.driver)?"board":"drive",car=i,x=c.transform.position.x,z=c.transform.position.z});return true;}
            return false;
        }
        void Send(RideCommand c){session.SendForest(JsonUtility.ToJson(c));pending=c.action;}
        public bool HandleInput()
        {
            if(Input.GetKeyDown(KeyCode.F7)){game.life.SetPanel(game.life.panel=="conversation"?"":"conversation");return true;}
            if(game.life.panel!="conversation")return false;if(Input.GetKeyDown(KeyCode.Escape))game.life.SetPanel("");return true;
        }
        void Update()
        {
            if(game==null)return;bool open=game.life.panel=="conversation";if(open&&!wasOpen)presentation.Capture(game);wasOpen=open;
            if(!session.Joined||localCar<0)return;var car=game.campus.cars[localCar];
            if(seat>0){game.player.transform.SetPositionAndRotation(car.transform.TransformPoint(new Vector3(seat==1?.4f:seat==2?-.4f:.4f,-.12f,seat==1?-.05f:-1)),car.transform.rotation);}
            else if(Time.unscaledTime>=sendAt){var p=car.transform.position;session.SendForest(JsonUtility.ToJson(new RideCommand{action="pose",car=localCar,x=p.x,y=p.y,z=p.z,yaw=car.transform.eulerAngles.y,speed=car.speed}));sendAt=Time.unscaledTime+.15f;}
        }
        void SendChat(){if(!session.Joined||string.IsNullOrWhiteSpace(draft))return;session.SendForest(JsonUtility.ToJson(new ChatCommand{text=draft}));draft="";}
        void DrawRideRating()
        {
            var cyan=new Color(.18f,.82f,1);var lilac=new Color(.50f,.55f,1);var white=new Color(.94f,.95f,1);
            presentation.Panel(new Rect(220,75,845,558),new Color(.035f,.015f,.24f,.80f));
            var center=presentation.Text(19,lilac);center.alignment=TextAnchor.MiddleCenter;GUI.Label(new Rect(240,112,805,45),"THANK YOU FOR RIDING TOGETHER",center);
            GUI.Label(new Rect(280,251,430,42),"Driver Rating",presentation.Text(24,lilac,true));
            for(int i=1;i<=5;i++){var rect=new Rect(274+(i-1)*61,303,58,62);var star=presentation.Text(45,i<=ratingChoice?cyan:new Color(.17f,.24f,.44f));star.alignment=TextAnchor.MiddleCenter;GUI.Label(rect,"★",star);if(GUI.Button(rect,GUIContent.none,GUIStyle.none))ratingChoice=i;}
            GUI.Label(new Rect(607,309,135,65),ratingChoice.ToString("0.0"),presentation.Text(40,white));
            GUI.Label(new Rect(813,251,225,42),"Your Ride",presentation.Text(24,lilac,true));GUI.Label(new Rect(813,319,225,70),"Completed",presentation.Text(25,white));
            if(presentation.Button(new Rect(468,541,345,60),"SUBMIT RATING",true,true))Send(new RideCommand{action="rate",stars=ratingChoice});
            if(presentation.Button(new Rect(824,583,197,32),"MAYBE LATER"))rateAvailable=false;
            GUI.Label(new Rect(278,439,713,55),feedback,presentation.Text(17,white));
        }
        void OnDestroy(){presentation.Dispose();}
        void OnGUI()
        {
            if(game==null||!game.Ready||game.life.panel!="conversation")return;
            var old=GUI.matrix;GUI.matrix=Matrix4x4.identity;OdysseyUI.Fill(new Rect(0,0,Screen.width,Screen.height),Color.black);float scale=Mathf.Min(Screen.width/1280f,Screen.height/720f);GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-1280*scale)/2,(Screen.height-720*scale)/2,0),Quaternion.identity,Vector3.one*scale);
            presentation.Background(1280,720);
            if(rateAvailable){DrawRideRating();GUI.matrix=old;return;}
            var white=new Color(.95f,.97f,1);var ink=new Color(.23f,.21f,.38f);var cyan=new Color(.30f,1,1);
            presentation.Panel(new Rect(154,70,240,247),new Color(.50f,.56f,.85f,.42f));
            Texture photo=game.portraits.Current!=null?(Texture)game.portraits.Current:presentation.Portrait;
            if(photo!=null)GUI.DrawTexture(new Rect(154,70,240,185),photo,ScaleMode.ScaleAndCrop);
            OdysseyUI.Fill(new Rect(154,255,240,62),new Color(.94f,.94f,1));var nameStyle=presentation.Text(22,ink,true);nameStyle.alignment=TextAnchor.MiddleCenter;GUI.Label(new Rect(154,255,240,62),game.campus.keeperName,nameStyle);
            presentation.Panel(new Rect(418,46,744,616),new Color(.53f,.64f,.9f,.64f));
            for(float y=48;y<468;y+=3)OdysseyUI.Fill(new Rect(420,y,740,1),new Color(.65f,.77f,1,.055f));
            if(presentation.Button(new Rect(1083,60,62,24),"↑ TOP"))scroll.y=0;
            var style=presentation.Text(25,ink);float total=12;
            foreach(var m in state?.messages??Array.Empty<RoomChat>())total+=Mathf.Max(54,style.CalcHeight(new GUIContent(m.text),407)+24)+34;
            scroll=GUI.BeginScrollView(new Rect(435,101,715,354),scroll,new Rect(0,0,690,Mathf.Max(354,total)));float row=8;
            foreach(var m in state?.messages??Array.Empty<RoomChat>()){
                bool mine=m.id==game.accounts.UserId;float width=Mathf.Clamp(style.CalcSize(new GUIContent(m.text)).x+32,108,443),x=mine?680-width:0;
                float height=Mathf.Max(54,style.CalcHeight(new GUIContent(m.text),width-32)+24);
                GUI.Label(new Rect(x+16,row,width-20,21),m.display.ToUpperInvariant(),presentation.Text(12,ink));row+=23;
                presentation.Panel(new Rect(x,row,width,height),mine?new Color(.05f,.02f,.25f):new Color(.94f,.985f,1),30);
                GUI.Label(new Rect(x+16,row+9,width-32,height-18),m.text,presentation.Text(25,mine?white:ink));row+=height+11;
            }GUI.EndScrollView();
            OdysseyUI.Fill(new Rect(418,468,744,194),new Color(.025f,.007f,.18f,.99f));
            GUI.enabled=session.Joined&&session.SocialSupported;GUI.SetNextControlName("room-message");
            var input=presentation.Text(25,white);input.padding=new RectOffset(0,0,9,9);input.normal.background=null;
            draft=GUI.TextField(new Rect(450,483,688,54),draft,280,input);
            if(string.IsNullOrEmpty(draft)&&GUI.GetNameOfFocusedControl()!="room-message")GUI.Label(new Rect(450,495,688,38),session.Joined?"Type your message…":"Join a room to start a conversation.",presentation.Text(23,new Color(.75f,.76f,.93f)));
            if(presentation.Button(new Rect(435,548,333,87),"SEND  ▷",true))SendChat();
            OdysseyUI.Fill(new Rect(782,546,2,92),new Color(.35f,.32f,.69f,.7f));
            if(presentation.Button(new Rect(799,548,341,87),"☺  ☺  ☺",false,true))draft=(draft+" ☺").Substring(0,Mathf.Min(280,draft.Length+2));
            if(Event.current.type==EventType.KeyDown&&Event.current.keyCode==KeyCode.Return&&GUI.GetNameOfFocusedControl()=="room-message"){SendChat();Event.current.Use();}
            GUI.enabled=true;GUI.Label(new Rect(435,640,700,20),feedback,presentation.Text(12,white));
            
            if(presentation.Button(new Rect(154,655,240,34),"CLOSE  ·  ESC"))game.life.SetPanel("");GUI.matrix=old;
        }
    }
}
