using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

namespace AlbionOdyssey
{
    [Serializable] public sealed class CampusNetPacket
    {
        public string type, session, id, display, text;
        public float x, y, z;
    }

    // A small LAN transport that keeps the game playable without a server. It provides
    // presence, movement snapshots and moderation hooks; a hosted authoritative service
    // can replace this transport later without changing campus gameplay code.
    public sealed class CampusOnlineSession : MonoBehaviour
    {
        const int Port = 40777; const float RemoteTimeout = 4.5f;
        OdysseyGame game; UdpClient socket; IPEndPoint broadcast; float nextHeartbeat,nextJoin,lastHostSeen,openedAt; int joinAttempts; bool open, active, host;
        string session = "ALBION", display = "Keeper", message = "", status = "Offline";
        readonly Dictionary<string, RemoteKeeper> remotes = new Dictionary<string, RemoteKeeper>();
        readonly HashSet<string> blocked = new HashSet<string>();
        GUIStyle title, text, button;int focus;
        public int RemoteCount => remotes.Count;
        string PlayerId { get { string id = PlayerPrefs.GetString("Odyssey.NetworkId", ""); if (id.Length == 0) { id = Guid.NewGuid().ToString("N"); PlayerPrefs.SetString("Odyssey.NetworkId", id); PlayerPrefs.Save(); } return id; } }
        public bool Active => active;
        public Vector3 RemotePosition(int index)
        {
            if (index < 0) return Vector3.zero;
            int current = 0;
            foreach (var remote in remotes.Values) if (current++ == index) return remote.root == null ? Vector3.zero : remote.root.transform.position;
            return Vector3.zero;
        }

        public void Setup(OdysseyGame owner)
        {
            game = owner; display = PlayerPrefs.GetString("Odyssey.NetworkName", "Keeper " + (game.state.active + 1));
            session = PlayerPrefs.GetString("Odyssey.NetworkSession", "ALBION");
            if (PlaytestMode.Active) return;
        }
        public bool HandleInput()
        {
            if (!open && Input.GetKeyDown(KeyCode.F5)) { open = true; focus=0; openedAt=Time.unscaledTime; game.player.controls = false; Cursor.lockState = CursorLockMode.None; Cursor.visible = true; return true; }
            if (!open) return false;
            if(AlbionUIInput.Poll(out var horizontal,out var vertical,out var choose,out var cancel)){if(cancel){ClosePanel();}else{if(vertical!=0||horizontal!=0)focus=(focus+(vertical!=0?(vertical>0?-1:1):(horizontal>0?1:-1))+6)%6;if(choose){if(focus==0)StartSession(true);else if(focus==1)StartSession(false);else if(focus==2)StopSession("");else if(focus==3)SendChat();else if(focus==4)CopyInvite();else ClosePanel();}}return true;}
            if (Input.GetKeyDown(KeyCode.Escape)) { ClosePanel(); return true; }
            return true;
        }
        void ClosePanel()
        {
            open = false; game.player.controls = !game.building && !game.life.PanelOpen;
            Cursor.lockState = game.player.pointerControls ? CursorLockMode.None : CursorLockMode.Locked; Cursor.visible = game.player.pointerControls;
        }
        void Update()
        {
            if (!active || socket == null) return;
            ReceivePackets();
            if (Time.unscaledTime >= nextHeartbeat) { SendPresence(); nextHeartbeat = Time.unscaledTime + 1.2f; }
            if (!host && Time.unscaledTime >= nextJoin) { SendJoin(); nextJoin = Time.unscaledTime + 2.4f; joinAttempts++; if (lastHostSeen > 0 && Time.unscaledTime - lastHostSeen > 6f) status = "Host not responding · retrying (" + joinAttempts + ")"; }
            PruneRemotes();
        }
        void PruneRemotes()
        {
            if (remotes.Count == 0) return;
            var expired = new List<string>();
            foreach (var entry in remotes) if (Time.unscaledTime - entry.Value.lastSeen > RemoteTimeout) expired.Add(entry.Key);
            foreach (var id in expired) { if (remotes.TryGetValue(id, out var remote) && remote.root != null) Destroy(remote.root); remotes.Remove(id); }
        }
        void ReceivePackets()
        {
            try
            {
                while (socket.Available > 0)
                {
                    IPEndPoint from = new IPEndPoint(IPAddress.Any, 0); byte[] bytes = socket.Receive(ref from); var packet = JsonUtility.FromJson<CampusNetPacket>(Encoding.UTF8.GetString(bytes));
                    if (packet == null || packet.id == PlayerId || packet.session != session || blocked.Contains(packet.id)) continue;
                    if (packet.type == "join" && host) Send(packet, from, "hello");
                    if (packet.type == "hello" || packet.type == "presence") lastHostSeen = Time.unscaledTime;
                    if (packet.type == "join" || packet.type == "hello" || packet.type == "presence") UpdateRemote(packet);
                    if (packet.type == "chat" && !string.IsNullOrEmpty(packet.text)) { message = packet.display + ": " + Sanitize(packet.text); if (remotes.TryGetValue(packet.id, out var speaker)) speaker.Chat(packet.display, packet.text); }
                    if (packet.type == "block" && packet.text == PlayerId) StopSession("You were removed by the host.");
                }
            }
            catch (SocketException) { }
            catch (Exception e) { status = "Network warning: " + e.Message; }
        }
        void UpdateRemote(CampusNetPacket packet)
        {
            if (!remotes.TryGetValue(packet.id, out var remote))
            {
                var o = new GameObject("Remote Keeper · " + packet.display); remote = new RemoteKeeper(o, packet.display, remotes.Count % 5); remotes.Add(packet.id, remote);
            }
            remote.target = new Vector3(packet.x, packet.y, packet.z); remote.display = Sanitize(packet.display); remote.lastSeen = Time.unscaledTime;
        }
        void SendPresence() { Send(new CampusNetPacket { type = "presence", session = session, id = PlayerId, display = display, x = game.player.transform.position.x, y = game.player.transform.position.y, z = game.player.transform.position.z }, broadcast); }
        void Send(CampusNetPacket packet, IPEndPoint endpoint, string typeOverride = null)
        {
            if (socket == null) return; packet.type = typeOverride ?? packet.type; packet.session = session; if (packet.id == null) packet.id = PlayerId; if (packet.display == null) packet.display = display;
            byte[] bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(packet)); try { socket.Send(bytes, bytes.Length, endpoint); } catch (Exception e) { status = "Network send failed: " + e.Message; }
        }
        void StartSession(bool asHost)
        {
            try
            {
                StopSession(""); host = asHost; socket = new UdpClient(); socket.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true); socket.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, true); socket.Client.Bind(new IPEndPoint(IPAddress.Any, Port)); socket.EnableBroadcast = true; socket.Client.Blocking = false; broadcast = new IPEndPoint(IPAddress.Broadcast, Port); active = true; joinAttempts = 0; lastHostSeen = 0; nextJoin = Time.unscaledTime; status = asHost ? "Hosting LAN world" : "Searching for host"; session = session.Trim().ToUpperInvariant(); if (session.Length == 0) session = "ALBION"; PlayerPrefs.SetString("Odyssey.NetworkSession", session); PlayerPrefs.SetString("Odyssey.NetworkName", display); PlayerPrefs.Save();
                if (!asHost) SendJoin();
            }
            catch (Exception e) { status = "Network unavailable: " + e.Message; active = false; }
        }
        void SendJoin()
        {
            if (socket == null || host) return;
            Send(new CampusNetPacket { type = "join", session = session, id = PlayerId, display = display }, broadcast);
        }
        void SendChat()
        {
            string clean=Sanitize(message);if(!active||clean.Length==0){status=active?"Type a message before sending.":"Start or join a world before chatting.";return;}
            Send(new CampusNetPacket { type="chat", text=clean }, broadcast);message="";
        }
        void CopyInvite()
        {
            GUIUtility.systemCopyBuffer="ALBION://"+session;
            status="Invite copied · world code "+session;
        }
        public void StopSession(string reason)
        {
            active = false; if (socket != null) { socket.Close(); socket = null; } foreach (var remote in remotes.Values) if (remote.root != null) Destroy(remote.root); remotes.Clear(); if (reason.Length > 0) status = reason; else status = "Offline";
        }
        public void Block(string id)
        {
            if (!host || id.Length == 0) return; blocked.Add(id); Send(new CampusNetPacket { type = "block", text = id }, broadcast); if (remotes.TryGetValue(id, out var remote)) { Destroy(remote.root); remotes.Remove(id); }
        }
        static string Sanitize(string value)
        {
            value = (value ?? "").Trim(); if (value.Length > 80) value = value.Substring(0, 80); return value.Replace("<", "").Replace(">", "").Replace("\n", " ");
        }
        void OnGUI()
        {
            if (!open || game == null) return;
            if (title == null) { title = new GUIStyle(GUI.skin.label) { font=AlbionUITheme.DisplayFont,fontSize = 27, fontStyle = FontStyle.Bold }; title.normal.textColor=new Color(.96f,.94f,.86f); text = new GUIStyle(GUI.skin.label) { font=AlbionUITheme.BodyFont,fontSize = 17, wordWrap = true }; text.normal.textColor=new Color(.88f,.88f,.92f); button = AlbionUITheme.Button(16); }
            float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 800f); GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1)); GUI.matrix=AlbionUITheme.Slide(GUI.matrix,openedAt,OdysseyAccessibility.ReducedMotion); float w = Screen.width / scale, h = Screen.height / scale; Rect safe = AlbionUITheme.SafeArea(scale); float x = Mathf.Clamp((w - 800) * .5f, safe.xMin + 24, safe.xMax - 800 - 24);
            GUI.color = new Color(.025f, .028f, .052f, .98f); GUI.DrawTexture(new Rect(0, 0, w, h), Texture2D.whiteTexture); GUI.color = Color.white;
            GUI.Label(new Rect(x, 70, 760, 45), "SHARED CAMPUS SESSION", title); GUI.Label(new Rect(x, 125, 760, 54), "F5 opens this panel. Host or join a small LAN world. Player movement is synchronized and the host can remove a player.", text);
            GUI.Label(new Rect(x, 205, 140, 30), "WORLD CODE", text); session = GUI.TextField(new Rect(x + 150, 202, 250, 36), session, 18).ToUpperInvariant();
            GUI.Label(new Rect(x, 260, 140, 30), "DISPLAY NAME", text); display = GUI.TextField(new Rect(x + 150, 257, 250, 36), display, 24);
            if (GUI.Button(new Rect(x, 325, 190, 44), (focus==0?"▶  ":"") + "Host world", button)) { focus=0; StartSession(true); }
            if (GUI.Button(new Rect(x + 205, 325, 190, 44), (focus==1?"▶  ":"") + "Join world", button)) { focus=1; StartSession(false); }
            if (GUI.Button(new Rect(x + 410, 325, 190, 44), (focus==2?"▶  ":"") + "Stop session", button)) { focus=2; StopSession(""); }
            if (GUI.Button(new Rect(x, 380, 230, 36), (focus==4?"▶  ":"") + "Copy invite code", button)) { focus=4; CopyInvite(); }
            GUI.Label(new Rect(x, 425, 760, 34), status + " · " + remotes.Count + " remote player(s)", text);
            int row = 468; string removeId = "";
            foreach (var entry in remotes)
            {
                GUI.Label(new Rect(x, row, 430, 28), entry.Value.display + "  " + entry.Key.Substring(0, 6), text);
                if (host && GUI.Button(new Rect(x + 450, row, 130, 28), "Remove", button)) removeId = entry.Key;
                row += 34;
            }
            // Defer the dictionary mutation until after enumeration. A host can
            // remove a player from the roster without throwing a GUI exception.
            if (removeId.Length > 0) Block(removeId);
            message = GUI.TextField(new Rect(x, h - 115, 530, 36), message, 80); if (GUI.Button(new Rect(x + 545, h - 115, 120, 36), (focus==3?"▶  ":"")+"Send", button)) { focus=3; SendChat(); }
            if (GUI.Button(new Rect(x + 680, 325, 100, 44), (focus==5?"▶  ":"") + "Close", button)) { focus=5; ClosePanel(); }
            GUI.Label(new Rect(x, h - 65, 760, 28), "STICK Navigate   ·   TRIGGER Select   ·   MENU Back", text);
        }
        void OnApplicationQuit()
        {
            string path = Path.Combine(Application.persistentDataPath, "online-session.json"); try { File.WriteAllText(path, JsonUtility.ToJson(new CampusNetPacket { type = host ? "host" : "client", session = session, id = PlayerId, display = display }, true)); } catch { }
            StopSession("");
        }
        sealed class RemoteKeeper
        {
            public readonly GameObject root; public Vector3 target; public string display; public float lastSeen;
            readonly CampusOnlineChatBubble chat;
            public RemoteKeeper(GameObject owner, string name, int variant) { root = owner; display = name; lastSeen = Time.unscaledTime; root.transform.position = Vector3.zero; var avatar = root.AddComponent<KeeperAvatar>(); avatar.Build(variant % 5, (variant + 1) % 5, variant % 3, false); var tag = root.AddComponent<CampusWorldLabel>(); tag.Configure(name + "  ·  ONLINE", AlbionUITheme.Cyan, new Vector3(0, 2.35f, 0), 24f); chat = root.AddComponent<CampusOnlineChatBubble>(); chat.Configure(new Vector3(0, 2.78f, 0), 20f); target = root.transform.position; }
            public void Chat(string name, string text) { chat.Show(name, text); }
            public void Tick() { root.transform.position = Vector3.Lerp(root.transform.position, target, Time.deltaTime * 8f); }
        }
        void LateUpdate() { foreach (var remote in remotes.Values) remote.Tick(); }
    }
}
