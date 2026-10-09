using System;
using System.Collections.Generic;
using UnityEngine;
namespace AlbionOdyssey
{
    [Serializable] public sealed class CampusSharedConfig {public string url;}
    [Serializable] sealed class CampusRoomAuth {public string type="authenticate",token,room,intent;}
    [Serializable] sealed class CampusRoomMove {public string type="move";public float x,y,z,yaw,speed;}
    [Serializable] sealed class CampusRoomEmote {public string type="emote",emote;}
    [Serializable] public sealed class CampusRoomPlayer {public string id,display,emote;public float x,y,z,yaw,speed;public int skin,outfit,hair;public bool backpack;}
    [Serializable] public sealed class CampusTrailStop { public string id,name,by; public float x,z; public bool visited; }
    [Serializable] sealed class CampusRoomMessage {public string type,id,room,hostId,mode,visibility;public int capacity,socialVersion;public CampusRoomPlayer[] players;public CampusTrailStop[] trail;public ForestSnapshot forest;public RoomSocialState social;public bool ok,canRate;public string action;}
    public sealed class CampusSharedSession : MonoBehaviour
    {
        public CampusRoomSocial Social {get;private set;}
        OdysseyGame game;CampusSharedConfig config;CampusSocket socket;string room="QUAD",connectedId="",joinIntent="",hostId="",mode="coop",visibility="private",roundPhase="";int focus,retries;bool wanted;float sendAt,retryAt,lastPacket;Vector3 lastPosition;
        readonly Dictionary<string,CampusRemoteStudent> remotes=new Dictionary<string,CampusRemoteStudent>();
        readonly HashSet<string> hidden=new HashSet<string>();
        public bool Joined {get;private set;}
        public bool SocialSupported {get;private set;}
        public string Status {get;private set;}="Join a room to meet other players.";
        public int RemoteCount=>remotes.Count;
        public string Room=>room;
        CampusTrailStop[] trail;CampusForestRun forest;
        Vector2 rosterScroll;
        public bool Configured=>config!=null&&Uri.TryCreate(config.url,UriKind.Absolute,out var uri)&&uri.Scheme=="wss";
        public void Setup(OdysseyGame owner)
        {
            game=owner;Social=gameObject.AddComponent<CampusRoomSocial>();Social.Setup(game,this);var asset=Resources.Load<TextAsset>("SharedCampusConfig");try{config=asset==null?null:JsonUtility.FromJson<CampusSharedConfig>(asset.text);}catch{}
            game.accounts.ProfileChanged+=ProfileChanged;forest=gameObject.AddComponent<CampusForestRun>();forest.Setup(game,this);
        }
        void ProfileChanged(){if(game.accounts.SignedIn){if(Joined)socket?.Send("{\"type\":\"profile\"}");}else Leave();}
        public void OpenForest(){if(!Joined){Open();Status="Join a public or private room, then choose Forest run.";return;}forest.Open();}
        public void Open(){focus=0;game.tour?.StopMedia();game.life.SetPanel("online");}
        public void Join(string code, string intent="")
        {
            if(!game.accounts.SignedIn||!game.accounts.ProfileLoaded){Status="Choose Student account from the Esc menu and sign in first.";return;}
            if(!Configured){Status="Online rooms are not configured in this build.";return;}
            code=(code??"").Trim().ToUpperInvariant();if(code.Length<3||code.Length>18){Status="Use a room code of 3–18 letters, numbers or hyphens.";return;}
            foreach(char c in code)if(!(c>='A'&&c<='Z')&&!(c>='0'&&c<='9')&&c!='-'){Status="Room codes use letters, numbers and hyphens.";return;}
            Leave();joinIntent=intent;game.online?.StopSession("");room=code;wanted=true;Application.runInBackground=true;retries=0;Connect();
        }
        void Connect()
        {
            socket?.Dispose();socket=new CampusSocket();connectedId=game.accounts.UserId;
            Status=retries==0?"Connecting to your campus room…":"Reconnecting… The server may be waking up.";
            socket.Connect(config.url,JsonUtility.ToJson(new CampusRoomAuth{token=game.accounts.AccessToken,room=room,intent=joinIntent}));lastPacket=Time.unscaledTime;
        }
        public void Leave()
        {
            Social?.ResetRoom();forest?.Close();trail=null;wanted=false;Application.runInBackground=PlaytestMode.Active;Joined=false;socket?.Dispose();socket=null;foreach(var peer in remotes.Values)if(peer!=null)Destroy(peer.gameObject);remotes.Clear();Status="Offline · explore on your own or join a room.";
        }
        public void SendForest(string message){if(Joined)socket?.Send(message);}
        public void Emote(string value)
        {
            if(!Joined)return;socket.Send(JsonUtility.ToJson(new CampusRoomEmote{emote=value}));
            var local=game.player.avatar.GetComponent<CampusSocialEmote>();if(local==null)local=game.player.avatar.gameObject.AddComponent<CampusSocialEmote>();local.Show(value);
        }
        public void MeetAtFerguson()
        {
            if(!game.player.TryExitVehicle()){Status="Park in an open space before meeting your group.";return;}
            if(game.building)game.ToggleMode();if(game.tour.InRoom)game.tour.ExitRoom();game.player.Teleport(CampusStudentNavigation.FreeWaypoint(CampusCatalog.Point(405,238)));game.life.SetPanel("");game.notice="Ferguson meetup · share your room code with friends.";
        }
        public bool HandleInput()
        {
            if(Social!=null&&Social.HandleInput())return true;
            if(forest!=null&&forest.HandleInput())return true;
            if(Input.GetKeyDown(KeyCode.F6)||CampusMenuShortcuts.Pressed(KeyCode.T,game)){OpenForest();return true;}
            if((Input.GetKeyDown(KeyCode.F5)&&!Input.GetKey(KeyCode.LeftShift))||CampusMenuShortcuts.Pressed(KeyCode.R,game)){if(game.life.panel=="online")game.life.SetPanel("");else Open();return true;}
            if(game.life.panel!="online")return false;
            if(Input.GetKeyDown(KeyCode.Escape)){game.life.SetPanel("");return true;}
            if(GUIUtility.keyboardControl==0&&AlbionUIInput.Poll(out var x,out var y,out var choose,out var back)){if(back)game.life.SetPanel("");else{if(x!=0||y!=0)focus=(focus+(x>0||y<0?1:11))%12;if(choose)Activate(focus);}}return true;
        }
        void Activate(int item)
        {
            if(item==0){if(!game.accounts.SignedIn)game.accountPanel.Open();else Join(room,"join");}
            else if(item==1)Leave();
            else if(item==2){GUIUtility.systemCopyBuffer=room;Status="Invite copied. Share this code with your friends.";}
            else if(item==3)MeetAtFerguson();
            else if(item>=4&&item<=6)Emote(item==4?"wave":item==5?"cheer":"dance");
            else if(item==7)game.life.SetPanel("");
            else if(item==8||item==9){if(!game.accounts.SignedIn)game.accountPanel.Open();else Join(item==8?"PUBLIC":"PRIVATE",item==8?"public":"create");}
            else if(item==10)OpenForest();
            else if(item==11&&Joined&&hostId==connectedId&&(string.IsNullOrEmpty(roundPhase)||roundPhase=="finished"))socket.Send("{\"type\":\"mode\",\"mode\":\""+(mode=="race"?"coop":"race")+"\"}");
        }
        void Update()
        {
            if(!wanted)return;if(!game.accounts.SignedIn||game.accounts.UserId!=connectedId){Leave();return;}
            if(socket==null){if(Time.unscaledTime>=retryAt)Connect();return;}
            while(socket.Incoming.TryDequeue(out string raw))
            {
                try
                {
                    var message=JsonUtility.FromJson<CampusRoomMessage>(raw);if(message.type=="social-result")Social.Result(message.ok,message.action,message.canRate);lastPacket=Time.unscaledTime;
                    if(message.type=="welcome"){SocialSupported=message.socialVersion>=1;room=message.room;joinIntent="join";hostId=message.hostId;mode=message.mode;visibility=message.visibility;Joined=true;retries=0;Status="Connected · room "+room;lastPosition=game.player.transform.position;}
                    if(message.type=="snapshot"&&message.players!=null){hostId=message.hostId;mode=message.mode;visibility=message.visibility;ApplyPlayers(message.players);roundPhase=message.forest?.phase;trail=message.trail;forest.Accept(message.forest);Social.Accept(message.social);}
                }catch{DisconnectForRetry(4400);return;}
            }
            if(socket.Ended){DisconnectForRetry(socket.CloseCode);return;}
            if(Joined&&Time.unscaledTime-lastPacket>20){DisconnectForRetry(1006);return;}
            if(Joined&&Time.unscaledTime>=sendAt)
            {
                float elapsed=Mathf.Max(.01f,Time.unscaledTime-sendAt+.15f);Vector3 pos=game.player.transform.position;
                socket.Send(JsonUtility.ToJson(new CampusRoomMove{x=pos.x,y=pos.y,z=pos.z,yaw=game.player.transform.eulerAngles.y,speed=Mathf.Clamp(Vector3.Distance(pos,lastPosition)/elapsed,0,30)}));lastPosition=pos;sendAt=Time.unscaledTime+.15f;
            }
        }
        void DisconnectForRetry(int code)
        {
            bool retry=code!=4401&&code!=4400&&code!=4403&&code!=4409&&code!=4404&&retries<3;
            Social?.ResetRoom();socket?.Dispose();socket=null;Joined=false;foreach(var peer in remotes.Values)if(peer!=null)Destroy(peer.gameObject);remotes.Clear();
            if(retry){retryAt=Time.unscaledTime+Mathf.Pow(2,++retries);Status="Connection lost · reconnecting shortly.";}
            else{wanted=false;Status=code==4404?"Room not found. Ask your friend for a current code.":code==4403?"This room is full. Choose another code.":code==4409?"This account joined from another session.":"Could not join. Check your connection and sign-in, then try again.";}
        }
        void ApplyPlayers(CampusRoomPlayer[] players)
        {
            if(players.Length>16)throw new InvalidOperationException();var present=new HashSet<string>();
            foreach(var row in players)
            {
                if(row.id==connectedId||hidden.Contains(row.id))continue;if(!Guid.TryParse(row.id,out _)||!float.IsFinite(row.x)||!float.IsFinite(row.y)||!float.IsFinite(row.z)||!float.IsFinite(row.yaw)||!float.IsFinite(row.speed))continue;
                present.Add(row.id);if(!remotes.TryGetValue(row.id,out var remote)){remote=new GameObject("Online student").AddComponent<CampusRemoteStudent>();remote.Initialize(row);remotes[row.id]=remote;}
                remote.Accept(row);
            }
            var gone=new List<string>();foreach(var pair in remotes)if(!present.Contains(pair.Key)){Destroy(pair.Value.gameObject);gone.Add(pair.Key);}foreach(var id in gone)remotes.Remove(id);
        }
        void OnGUI()
        {
            if(game==null||!game.Ready||game.loading.Busy)return;
            if(game.life.panel!="online")
            {
                if(Joined && trail!=null && !game.life.PanelOpen && !game.journalOpen)
                {
                    int done=0;CampusTrailStop next=null;foreach(var stop in trail){if(stop.visited)done++;else if(next==null)next=stop;}
                    OdysseyUI.Card(new Rect(18,Screen.height-112,380,92),OdysseyUI.Surface);
                    OdysseyUI.Text(new Rect(34,Screen.height-102,348,24),"CAMPUS TRAIL · "+done+" / "+trail.Length,15,OdysseyUI.Mint,true);
                    string hint=next==null?"Your group explored every stop!":next.name+" · "+Mathf.RoundToInt(Vector3.Distance(game.player.transform.position,new Vector3(next.x,0,next.z)))+" m";
                    OdysseyUI.Text(new Rect(34,Screen.height-75,348,23),hint,16,OdysseyUI.White);
                    OdysseyUI.Text(new Rect(34,Screen.height-49,348,22),"Esc menu · Online rooms · Forest run",13,OdysseyUI.Muted);
                }
                return;
            }
            var old=GUI.matrix;float scale=Mathf.Min(Screen.width/1280f,Screen.height/800f);GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);float w=Screen.width/scale,h=Screen.height/scale,x=(w-1120)/2;
            ConsoleMenuStyle.Background(game,w,h);
            ConsoleMenuStyle.Heading(x,"MULTIPLAYER",Joined?"Your party is here.":"Better together.",Joined?(visibility=="public"?"Public campus":"Private campus")+" · "+(remotes.Count+1)+" / 16 players · "+(mode=="race"?"Competitive race":"Cooperative treasure hunt"):"Find a public campus or invite friends into your own private room.");
            bool enabled=GUI.enabled;GUI.enabled=!wanted;
            if(OdysseyUI.Button(new Rect(x,255,347,62),"Find public room","room-public",focus==8,true))Activate(8);
            if(OdysseyUI.Button(new Rect(x+369,255,347,62),"Create private room","room-private",focus==9))Activate(9);
            GUI.enabled=enabled;
            OdysseyUI.Text(new Rect(x,345,600,27),Joined?"INVITE CODE":"HAVE AN INVITE?",14,OdysseyUI.Mint,true);
            GUI.enabled=!wanted;GUI.SetNextControlName("room-code");room=GUI.TextField(new Rect(x,382,450,57),room,18,ConsoleMenuStyle.Field()).ToUpperInvariant();GUI.enabled=enabled;
            if(OdysseyUI.Button(new Rect(x+469,382,247,57),Joined?"Copy invite":"Join with code","room-join",focus==(Joined?2:0)))Activate(Joined?2:0);
            OdysseyUI.Card(new Rect(x+754,255,366,312),new Color(.035f,.030f,.023f,.95f));
            OdysseyUI.Text(new Rect(x+778,277,314,31),"YOUR PARTY",14,OdysseyUI.Mint,true);
            OdysseyUI.Text(new Rect(x+778,321,314,34),Joined?game.accounts.DisplayName+" · You":"No room joined",20,OdysseyUI.White,true);
            rosterScroll=GUI.BeginScrollView(new Rect(x+778,367,314,173),rosterScroll,new Rect(0,0,290,Mathf.Max(173,remotes.Count*34)));
            int rowY=0;foreach(var peer in remotes.Values){OdysseyUI.Text(new Rect(0,rowY,290,32),peer.DisplayName,18,OdysseyUI.White);rowY+=34;}GUI.EndScrollView();
            if(Joined)
            {
                if(OdysseyUI.Button(new Rect(x,472,347,60),"Start forest run","room-run",focus==10,true))Activate(10);
                GUI.enabled=hostId==connectedId&&(string.IsNullOrEmpty(roundPhase)||roundPhase=="finished");
                if(OdysseyUI.Button(new Rect(x+369,472,347,60),mode=="race"?"Switch to co-op":"Switch to race","room-mode",focus==11))Activate(11);GUI.enabled=enabled;
                if(OdysseyUI.Button(new Rect(x,551,220,48),"Meet at Ferguson","room-meet",focus==3))Activate(3);
                for(int i=4;i<=6;i++)if(OdysseyUI.Button(new Rect(x+236+(i-4)*160,551,150,48),i==4?"Wave":i==5?"Cheer":"Dance","room-emote-"+i,focus==i))Activate(i);
            }
            OdysseyUI.Text(new Rect(x,634,716,67),Status+(Joined?"\nRoom hosts can switch modes between runs. Your email stays private.":""),17,Joined?OdysseyUI.Mint:OdysseyUI.Muted);
            if(wanted&&!Joined)OdysseyUI.Spinner(new Rect(x+663,550,42,42));
            if(Joined&&OdysseyUI.Button(new Rect(x+754,596,175,51),"Leave room","room-leave",focus==1))Activate(1);
            if(OdysseyUI.Button(new Rect(x+945,596,175,51),"Back","room-back",focus==7))Activate(7);
            if(Event.current.type==EventType.KeyDown&&Event.current.keyCode==KeyCode.Return&&GUI.GetNameOfFocusedControl()=="room-code"){Activate(0);Event.current.Use();}
            ConsoleMenuStyle.Footer(x,h,"ESC  Back     ·     ARROWS  Browse     ·     ENTER  Select");GUI.matrix=old;
        }

        void OnApplicationQuit(){Leave();}
        void OnDestroy(){if(game?.accounts!=null)game.accounts.ProfileChanged-=ProfileChanged;Leave();}
    }
    public sealed class CampusRemoteStudent : MonoBehaviour
    {
        KeeperAvatar avatar;CampusWorldLabel label;CampusSocialEmote emote;Vector3 target;float yaw,speed;int look=-1;string previousEmote="";
        public string DisplayName {get;private set;}
        public void Initialize(CampusRoomPlayer p){var model=new GameObject("Online avatar");model.transform.SetParent(transform,false);avatar=model.AddComponent<KeeperAvatar>();label=gameObject.AddComponent<CampusWorldLabel>();label.Configure(p.display+" · ONLINE",OdysseyUI.Mint,new Vector3(0,2.35f,0),24);emote=gameObject.AddComponent<CampusSocialEmote>();transform.position=new Vector3(p.x,p.y,p.z);Accept(p);}
        public void Accept(CampusRoomPlayer p)
        {
            target=new Vector3(p.x,p.y,p.z);yaw=p.yaw;speed=p.speed;DisplayName=p.display;label.SetText(DisplayName+" · ONLINE");int signature=p.skin+p.outfit*10+p.hair*100+(p.backpack?1000:0);
            if(look!=signature){look=signature;avatar.Build(p.skin,p.outfit,p.hair,p.backpack);}
            if(p.emote!=previousEmote&&p.emote!="")emote.Show(p.emote);previousEmote=p.emote;
        }
        void Update(){if(Vector3.Distance(transform.position,target)>10)transform.position=target;else transform.position=Vector3.Lerp(transform.position,target,1-Mathf.Exp(-Time.deltaTime*12));transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.Euler(0,yaw,0),Time.deltaTime*12);avatar.Animate(speed,false);}
    }
    public sealed class CampusSocialEmote : MonoBehaviour
    {
        string gesture="";float until;Transform arm,other;Quaternion armBase,otherBase;bool posed;
        public void Show(string value){gesture=value;until=Time.time+3;arm=null;other=null;}
        void Update(){if(posed){if(arm!=null)arm.localRotation=armBase;if(other!=null)other.localRotation=otherBase;posed=false;}}
        void LateUpdate()
        {
            if(Time.time>=until)return;if(arm==null)foreach(var t in GetComponentsInChildren<Transform>()){if(t.name=="upperarm_r")arm=t;if(t.name=="upperarm_l")other=t;}
            if(arm==null)return;armBase=arm.localRotation;if(other!=null)otherBase=other.localRotation;
            float motion=Mathf.Sin(Time.time*9)*18;arm.localRotation*=Quaternion.Euler(gesture=="dance"?motion:0,0,gesture=="wave"?-100+motion:-75);
            if(other!=null&&gesture!="wave")other.localRotation*=Quaternion.Euler(gesture=="dance"?-motion:0,0,75);posed=true;
        }
    }
}
