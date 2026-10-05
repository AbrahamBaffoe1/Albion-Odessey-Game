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
    [Serializable] sealed class CampusRoomMessage {public string type,id,room,hostId,mode,visibility;public int capacity;public CampusRoomPlayer[] players;public CampusTrailStop[] trail;public ForestSnapshot forest;}
    public sealed class CampusSharedSession : MonoBehaviour
    {
        OdysseyGame game;CampusSharedConfig config;CampusSocket socket;string room="QUAD",connectedId="",joinIntent="",hostId="",mode="coop",visibility="private";int focus,retries;bool wanted;float sendAt,retryAt,lastPacket;Vector3 lastPosition;
        readonly Dictionary<string,CampusRemoteStudent> remotes=new Dictionary<string,CampusRemoteStudent>();
        readonly HashSet<string> hidden=new HashSet<string>();
        public bool Joined {get;private set;}
        public string Status {get;private set;}="Join a room to meet other players.";
        public int RemoteCount=>remotes.Count;
        public string Room=>room;
        CampusTrailStop[] trail;CampusForestRun forest;
        Vector2 rosterScroll;
        public bool Configured=>config!=null&&Uri.TryCreate(config.url,UriKind.Absolute,out var uri)&&uri.Scheme=="wss";
        public void Setup(OdysseyGame owner)
        {
            game=owner;var asset=Resources.Load<TextAsset>("SharedCampusConfig");try{config=asset==null?null:JsonUtility.FromJson<CampusSharedConfig>(asset.text);}catch{}
            game.accounts.ProfileChanged+=ProfileChanged;forest=gameObject.AddComponent<CampusForestRun>();forest.Setup(game,this);
        }
        void ProfileChanged(){if(game.accounts.SignedIn){if(Joined)socket?.Send("{\"type\":\"profile\"}");}else Leave();}
        public void Open(){focus=0;game.tour?.StopMedia();game.life.SetPanel("online");}
        public void Join(string code, string intent="")
        {
            if(!game.accounts.SignedIn||!game.accounts.ProfileLoaded){Status="Sign in with F7 and load your profile first.";return;}
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
            forest?.Close();trail=null;wanted=false;Application.runInBackground=PlaytestMode.Active;Joined=false;socket?.Dispose();socket=null;foreach(var peer in remotes.Values)if(peer!=null)Destroy(peer.gameObject);remotes.Clear();Status="Offline · explore on your own or join a room.";
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
            if(forest!=null&&forest.HandleInput())return true;
            if(Input.GetKeyDown(KeyCode.F6)&&Joined){forest.Open();return true;}
            if(Input.GetKeyDown(KeyCode.F5)&&!Input.GetKey(KeyCode.LeftShift)){if(game.life.panel=="online")game.life.SetPanel("");else Open();return true;}
            if(game.life.panel!="online")return false;
            if(Input.GetKeyDown(KeyCode.Escape)){game.life.SetPanel("");return true;}
            if(GUIUtility.keyboardControl==0&&AlbionUIInput.Poll(out var x,out var y,out var choose,out var back)){if(back)game.life.SetPanel("");else{if(x!=0||y!=0)focus=(focus+(x>0||y<0?1:7))%8;if(choose)Activate(focus);}}return true;
        }
        void Activate(int item){if(item==0){if(!game.accounts.SignedIn)game.accountPanel.Open();else Join(room,"join");}else if(item==1)Leave();else if(item==2){GUIUtility.systemCopyBuffer=room;Status="Room code copied. Friends can enter it in F5.";}else if(item==3)MeetAtFerguson();else if(item<7)Emote(item==4?"wave":item==5?"cheer":"dance");else game.life.SetPanel("");}
        void Update()
        {
            if(!wanted)return;if(!game.accounts.SignedIn||game.accounts.UserId!=connectedId){Leave();return;}
            if(socket==null){if(Time.unscaledTime>=retryAt)Connect();return;}
            while(socket.Incoming.TryDequeue(out string raw))
            {
                try
                {
                    var message=JsonUtility.FromJson<CampusRoomMessage>(raw);lastPacket=Time.unscaledTime;
                    if(message.type=="welcome"){room=message.room;joinIntent="join";hostId=message.hostId;mode=message.mode;visibility=message.visibility;Joined=true;retries=0;Status="Connected · room "+room;lastPosition=game.player.transform.position;}
                    if(message.type=="snapshot"&&message.players!=null){hostId=message.hostId;mode=message.mode;visibility=message.visibility;ApplyPlayers(message.players);trail=message.trail;forest.Accept(message.forest);}
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
            socket?.Dispose();socket=null;Joined=false;foreach(var peer in remotes.Values)if(peer!=null)Destroy(peer.gameObject);remotes.Clear();
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
                    OdysseyUI.Text(new Rect(34,Screen.height-49,348,22),"F6 forest treasure run · F5 room",13,OdysseyUI.Muted);
                }
                return;
            }
            var old=GUI.matrix;float scale=Mathf.Min(Screen.width/1440f,Screen.height/900f);GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);float w=Screen.width/scale,h=Screen.height/scale,x=(w-1120)/2,y=(h-730)/2;
            OdysseyUI.Fill(new Rect(0,0,w,h),OdysseyUI.Navy);OdysseyUI.Text(new Rect(x,y,700,26),"ALBION / TOGETHER",14,OdysseyUI.Mint,true);
            OdysseyUI.Text(new Rect(x,y+38,850,72),"YOUR CAMPUS. YOUR PEOPLE.",38,OdysseyUI.White,true);
            OdysseyUI.Text(new Rect(x,y+110,1100,48),"Your display name, avatar and game location are visible to people in this room. Email stays private.",18,OdysseyUI.Muted);
            OdysseyUI.Card(new Rect(x,y+180,450,330),OdysseyUI.Surface);OdysseyUI.Text(new Rect(x+24,y+204,400,28),"ROOM CODE",14,OdysseyUI.Mint,true);
            bool enabled=GUI.enabled;GUI.enabled=!wanted;room=GUI.TextField(new Rect(x+24,y+248,402,48),room,18,OdysseyUI.Font(27,true)).ToUpperInvariant();GUI.enabled=enabled;
            if(OdysseyUI.Button(new Rect(x+24,y+324,402,56),game.accounts.SignedIn?(Joined?"REJOIN ROOM":"JOIN ONLINE") : "SIGN IN TO JOIN","room-join",focus==0,true))Activate(0);
            if(OdysseyUI.Button(new Rect(x+24,y+402,190,52),"LEAVE","room-leave",focus==1))Activate(1);if(OdysseyUI.Button(new Rect(x+232,y+402,194,52),"COPY CODE","room-copy",focus==2))Activate(2);
            OdysseyUI.Card(new Rect(x+472,y+180,648,330),OdysseyUI.Surface);OdysseyUI.Text(new Rect(x+498,y+204,590,30),(Joined?remotes.Count+1:0)+" / 16 PLAYERS",18,OdysseyUI.White,true);
            if(remotes.Count==0)OdysseyUI.Text(new Rect(x+498,y+258,590,94),Joined?"You’re first here. Invite a friend with the same room code.":"Join a room to see real online players here.",24,OdysseyUI.Muted);
            rosterScroll=GUI.BeginScrollView(new Rect(x+498,y+248,590,118),rosterScroll,new Rect(0,0,560,Mathf.Max(118,remotes.Count*29)));
            int rowY=0;foreach(var peer in remotes.Values){OdysseyUI.Text(new Rect(0,rowY,555,29),peer.DisplayName,18,OdysseyUI.White);rowY+=29;}GUI.EndScrollView();
            if(Joined&&trail!=null){int n=0;foreach(var stop in trail){OdysseyUI.Text(new Rect(x+498,y+380+n*32,590,30),(stop.visited?"✓ ":"○ ")+stop.name+(stop.visited?" · "+stop.by:" · explore together"),16,stop.visited?OdysseyUI.Mint:OdysseyUI.Muted);n++;}}
            if(OdysseyUI.Button(new Rect(x,y+532,450,54),"MEET AT FERGUSON","room-meet",focus==3))Activate(3);
            foreach(var option in new[]{4,5,6})if(OdysseyUI.Button(new Rect(x+472+(option-4)*220,y+532,208,54),option==4?"WAVE":option==5?"CHEER":"DANCE","room-emote-"+option,focus==option))Activate(option);
            if(!wanted){
                if(OdysseyUI.Button(new Rect(x,y+592,218,48),"PUBLIC ROOM","room-public",false,true))Join("PUBLIC","public");
                if(OdysseyUI.Button(new Rect(x+232,y+592,218,48),"CREATE PRIVATE","room-private",false))Join("PRIVATE","create");
            }
            if(Joined&&hostId==connectedId&&OdysseyUI.Button(new Rect(x,y+646,450,42),mode=="race"?"RACE · SWITCH TO CO-OP":"CO-OP · SWITCH TO RACE","room-mode",false)){
                socket.Send("{\"type\":\"mode\",\"mode\":\""+(mode=="race"?"coop":"race")+"\"}");Status="Mode changes between runs. Finish the current run first.";
            }
            if(Joined&&OdysseyUI.Button(new Rect(x,y+592,450,48),"FOREST TREASURE RUN · F6","forest-open",false,true))forest.Open();
            OdysseyUI.Text(new Rect(x+472,y+613,400,52),Status,18,Joined?OdysseyUI.Mint:OdysseyUI.Muted);
            if(wanted&&!Joined)OdysseyUI.Spinner(new Rect(x+850,y+612,38,38));if(OdysseyUI.Button(new Rect(x+914,y+612,206,52),"BACK  ·  ESC","room-back",focus==7))Activate(7);
            OdysseyUI.Text(new Rect(x,y+699,1120,28),"F5 Rooms · F7 Account · F8 Nature trails · Room host switches modes between runs",14,OdysseyUI.Muted);GUI.matrix=old;
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
