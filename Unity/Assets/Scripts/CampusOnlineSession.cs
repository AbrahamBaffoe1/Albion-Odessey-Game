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
        const int Port = 40777;
        OdysseyGame game; UdpClient socket; IPEndPoint broadcast; float nextHeartbeat,openedAt; bool open, active, host;
        string session = "ALBION", display = "Keeper", message = "", status = "Offline";
        readonly Dictionary<string, RemoteKeeper> remotes = new Dictionary<string, RemoteKeeper>();
        readonly HashSet<string> blocked = new HashSet<string>();
        GUIStyle title, text, button;int focus;
        public int RemoteCount => remotes.Count;
        string PlayerId { get { string id = PlayerPrefs.GetString("Odyssey.NetworkId", ""); if (id.Length == 0) { id = Guid.NewGuid().ToString("N"); PlayerPrefs.SetString("Odyssey.NetworkId", id); PlayerPrefs.Save(); } return id; } }
        public bool Active => active;

        public void Setup(OdysseyGame owner)
        {
            game = owner; display = PlayerPrefs.GetString("Odyssey.NetworkName", "Keeper " + (game.state.active + 1));
            session = PlayerPrefs.GetString("Odyssey.NetworkSession", "ALBION");
            if (PlaytestMode.Active) return;
        }
        public bool HandleInput()
        {
            if (!open && Input.GetKeyDown(KeyCode.F5)) { open = true; openedAt=Time.unscaledTime; game.player.controls = false; Cursor.lockState = CursorLockMode.None; Cursor.visible = true; return true; }
            if (!open) return false;
            if(AlbionUIInput.Poll(out var horizontal,out var vertical,out var choose,out var cancel)){if(cancel){open=false;}else{if(horizontal!=0)focus=(focus+(horizontal>0?1:-1)+4)%4;if(choose){if(focus==0)StartSession(true);else if(focus==1)StartSession(false);else if(focus==2)StopSession("");else open=false;}}return true;}
            if (Input.GetKeyDown(KeyCode.Escape)) { open = false; game.player.controls = !game.building && !game.life.PanelOpen; Cursor.lockState = game.player.pointerControls ? CursorLockMode.None : CursorLockMode.Locked; Cursor.visible = game.player.pointerControls; return true; }
            return true;
        }
        void Update()
        {
            if (!active || socket == null) return;
            ReceivePackets();
            if (Time.unscaledTime >= nextHeartbeat) { SendPresence(); nextHeartbeat = Time.unscaledTime + 1.2f; }
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
                    if (packet.type == "join" || packet.type == "hello" || packet.type == "presence") UpdateRemote(packet);
                    if (packet.type == "chat" && !string.IsNullOrEmpty(packet.text)) message = packet.display + ": " + Sanitize(packet.text);
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
            remote.target = new Vector3(packet.x, packet.y, packet.z); remote.display = Sanitize(packet.display);
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
                StopSession(""); host = asHost; socket = new UdpClient(0); socket.EnableBroadcast = true; socket.Client.Blocking = false; broadcast = new IPEndPoint(IPAddress.Broadcast, Port); active = true; status = asHost ? "Hosting LAN world" : "Searching for host"; session = session.Trim().ToUpperInvariant(); if (session.Length == 0) session = "ALBION"; PlayerPrefs.SetString("Odyssey.NetworkSession", session); PlayerPrefs.SetString("Odyssey.NetworkName", display); PlayerPrefs.Save();
                if (!asHost) Send(new CampusNetPacket { type = "join", session = session, id = PlayerId, display = display }, broadcast);
            }
            catch (Exception e) { status = "Network unavailable: " + e.Message; active = false; }
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
            if (title == null) { title = new GUIStyle(GUI.skin.label) { fontSize = 27, fontStyle = FontStyle.Bold }; title.normal.textColor=new Color(.96f,.94f,.86f); text = new GUIStyle(GUI.skin.label) { fontSize = 17, wordWrap = true }; text.normal.textColor=new Color(.88f,.88f,.92f); button = new GUIStyle(GUI.skin.button) { fontSize = 16, padding = new RectOffset(12, 12, 6, 6) }; }
            float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 800f); GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1)); GUI.matrix=AlbionUITheme.Slide(GUI.matrix,openedAt,OdysseyAccessibility.ReducedMotion); float w = Screen.width / scale, h = Screen.height / scale, x = (w - 800) * .5f;
            GUI.color = new Color(.025f, .028f, .052f, .98f); GUI.DrawTexture(new Rect(0, 0, w, h), Texture2D.whiteTexture); GUI.color = Color.white;
            GUI.Label(new Rect(x, 70, 760, 45), "SHARED CAMPUS SESSION", title); GUI.Label(new Rect(x, 125, 760, 54), "F5 opens this panel. Host or join a small LAN world. Player movement is synchronized and the host can remove a player.", text);
            GUI.Label(new Rect(x, 205, 140, 30), "WORLD CODE", text); session = GUI.TextField(new Rect(x + 150, 202, 250, 36), session, 18).ToUpperInvariant();
            GUI.Label(new Rect(x, 260, 140, 30), "DISPLAY NAME", text); display = GUI.TextField(new Rect(x + 150, 257, 250, 36), display, 24);
            if (GUI.Button(new Rect(x, 325, 190, 44), "Host world", button)) StartSession(true);
            if (GUI.Button(new Rect(x + 205, 325, 190, 44), "Join world", button)) StartSession(false);
            if (GUI.Button(new Rect(x + 410, 325, 190, 44), "Stop session", button)) StopSession("");
            GUI.Label(new Rect(x, 395, 760, 34), status + " · " + remotes.Count + " remote player(s)", text);
            int row = 440; foreach (var entry in remotes) { GUI.Label(new Rect(x, row, 430, 28), entry.Value.display + "  " + entry.Key.Substring(0, 6), text); if (host && GUI.Button(new Rect(x + 450, row, 130, 28), "Remove", button)) Block(entry.Key); row += 34; }
            message = GUI.TextField(new Rect(x, h - 115, 530, 36), message, 80); if (GUI.Button(new Rect(x + 545, h - 115, 120, 36), "Send", button) && active) { Send(new CampusNetPacket { type = "chat", text = Sanitize(message) }, broadcast); message = ""; }
            if (GUI.Button(new Rect(x + 680, 325, 100, 44), "Close", button)) { open = false; game.player.controls = !game.building && !game.life.PanelOpen; Cursor.lockState = game.player.pointerControls ? CursorLockMode.None : CursorLockMode.Locked; Cursor.visible = game.player.pointerControls; }
        }
        void OnApplicationQuit()
        {
            string path = Path.Combine(Application.persistentDataPath, "online-session.json"); try { File.WriteAllText(path, JsonUtility.ToJson(new CampusNetPacket { type = host ? "host" : "client", session = session, id = PlayerId, display = display }, true)); } catch { }
            StopSession("");
        }
        sealed class RemoteKeeper
        {
            public readonly GameObject root; public Vector3 target; public string display;
            public RemoteKeeper(GameObject owner, string name, int variant) { root = owner; display = name; root.transform.position = Vector3.zero; var avatar = root.AddComponent<KeeperAvatar>(); avatar.Build(variant % 5, (variant + 1) % 5, variant % 3, false); target = root.transform.position; }
            public void Tick() { root.transform.position = Vector3.Lerp(root.transform.position, target, Time.deltaTime * 8f); }
        }
        void LateUpdate() { foreach (var remote in remotes.Values) remote.Tick(); }
    }
}
