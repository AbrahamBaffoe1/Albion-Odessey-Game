using System;
using UnityEngine;

namespace AlbionOdyssey
{
    // Optional XR bridge. The desktop build remains the default; when an OpenXR loader
    // is installed, F8 enables headset rendering and a comfortable third-person offset.
    public sealed class OdysseyVrSupport : MonoBehaviour
    {
        OdysseyGame game; bool active; float oldFov; int focus; string previousPanel = "";
        GUIStyle title, text, button;
        public bool Active => active;
        public bool IsOpen => game != null && game.life.panel == "vr";
        public void Setup(OdysseyGame owner)
        {
            game = owner; oldFov = game.player.eyes.fieldOfView;
            active = IsDeviceActive(); if (active) Apply(true);
        }
        public bool HandleInput()
        {
            bool toggle = Input.GetKeyDown(KeyCode.F8);
            // A closed settings panel must never consume the shared Back/Escape action.
            if (!IsOpen && !toggle) return false;
            AlbionUIInput.Poll(out var horizontal, out var vertical, out var choose, out var cancel);
            return HandleMenuInput(toggle, cancel || Input.GetKeyDown(KeyCode.Escape), horizontal, vertical, choose);
        }
        internal bool HandleMenuInput(bool toggle, bool cancel, int horizontal, int vertical, bool choose)
        {
            if (toggle) { if (IsOpen) ClosePanel(); else OpenPanel(); return true; }
            if (!IsOpen) return false;
            if (cancel) { ClosePanel(); return true; }
            if (vertical != 0 || horizontal != 0) focus = (focus + (vertical != 0 ? -vertical : horizontal) + 7) % 7;
            if (choose) Activate(focus);
            return true;
        }
        void Activate(int action)
        {
            focus = action;
            if (action == 0) Apply(!active);
            else if (action == 1) SetComfort("Odyssey.XR.SnapTurn", true);
            else if (action == 2) SetComfort("Odyssey.XR.Vignette", false);
            else if (action == 3) SetComfort("Odyssey.XR.RoomScale", true);
            else if (action == 4) SetComfort("Odyssey.XR.Hands", false);
            else if (action == 5) game.xr?.RecenterView();
            else ClosePanel();
        }
        void SetComfort(string key, bool snap)
        {
            if (game == null || game.xr == null) return;
            if (key == "Odyssey.XR.SnapTurn") { game.xr.SnapTurn = !game.xr.SnapTurn; game.xr.SmoothTurn = !game.xr.SnapTurn; PlayerPrefs.SetInt("Odyssey.XR.SmoothTurn", game.xr.SmoothTurn ? 1 : 0); }
            else if (key == "Odyssey.XR.Vignette") game.xr.ComfortVignette = !game.xr.ComfortVignette;
            else if (key == "Odyssey.XR.RoomScale") game.xr.RoomScale = !game.xr.RoomScale;
            else if (key == "Odyssey.XR.Hands") game.xr.HandTrackingEnabled = !game.xr.HandTrackingEnabled;
            PlayerPrefs.SetInt(key, key == "Odyssey.XR.SnapTurn" ? (game.xr.SnapTurn ? 1 : 0) : key == "Odyssey.XR.Vignette" ? (game.xr.ComfortVignette ? 1 : 0) : key == "Odyssey.XR.RoomScale" ? (game.xr.RoomScale ? 1 : 0) : (game.xr.HandTrackingEnabled ? 1 : 0)); PlayerPrefs.Save();
        }
        public void OpenPanel()
        {
            if (game == null || game.player == null || IsOpen) return;
            previousPanel = game.life.panel; focus = 0;
            game.life.SetPanel("vr");
        }
        public void ClosePanel()
        {
            if (!IsOpen) return;
            game.life.SetPanel(previousPanel);
        }
        void Apply(bool enable)
        {
            active = enable;
            // XR is optional in this desktop project. Reflection keeps the base player
            // compileable when the Unity XR package is not installed.
            var settings = Type.GetType("UnityEngine.XR.XRSettings, UnityEngine.XRModule");
            var property = settings == null ? null : settings.GetProperty("enabled");
            if (property != null && property.CanWrite) { try { property.SetValue(null, enable, null); } catch { } }
            if (game == null || game.player == null || game.player.eyes == null) return;
            game.player.eyes.fieldOfView = enable ? 90f : oldFov; game.player.cameraDistance = enable ? 3.1f : Mathf.Clamp(game.player.cameraDistance, 2.5f, 7f);
        }
        void Fill(Rect rect, Color color)
        {
            var previous = GUI.color; GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture); GUI.color = previous;
        }
        void Action(Rect rect, int index, string label, string state)
        {
            bool selected = index == focus;
            Color ink = new Color(.04f,.025f,.065f), cream = new Color(.96f,.95f,.98f);
            Fill(rect, selected ? AlbionUITheme.Gold : new Color(.11f,.08f,.17f));
            foreach (var style in new[] { button.normal, button.hover, button.active, button.focused })
                style.textColor = selected ? ink : cream;
            if (GUI.Button(rect, (selected ? "›  " : "") + label, button)) Activate(index);
            var color = GUI.contentColor; GUI.contentColor = selected ? ink : new Color(.77f,.73f,.85f);
            GUI.Label(new Rect(rect.xMax-146,rect.y+16,130,30),state,text); GUI.contentColor = color;
        }
        void OnGUI()
        {
            if (!IsOpen) return;
            if (title == null)
            {
                title = new GUIStyle(GUI.skin.label) { font=AlbionUITheme.DisplayFont, fontSize=AlbionUITheme.TextSize(48), fontStyle=FontStyle.Bold };
                text = new GUIStyle(GUI.skin.label) { font=AlbionUITheme.BodyFont, fontSize=AlbionUITheme.TextSize(16), wordWrap=true };
                title.normal.textColor = text.normal.textColor = Color.white;
                button = new GUIStyle(GUI.skin.label) { font=AlbionUITheme.BodyFont, fontSize=AlbionUITheme.TextSize(19), fontStyle=FontStyle.Bold, alignment=TextAnchor.MiddleLeft, padding=new RectOffset(20,150,10,10) };
            }
            var matrix=GUI.matrix; var color=GUI.color; var content=GUI.contentColor; var background=GUI.backgroundColor; int depth=GUI.depth;
            float scale=Mathf.Min(Screen.width/1280f,Screen.height/800f), w=Screen.width/scale, h=Screen.height/scale, x=(w-960)*.5f;
            GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1)); GUI.color=GUI.contentColor=Color.white; GUI.depth=-30;
            Fill(new Rect(0,0,w,h),new Color(.022f,.025f,.042f));
            Fill(new Rect(x,60,44,4),AlbionUITheme.Gold);
            GUI.Label(new Rect(x,83,960,30),"ALBION ODYSSEY  /  SETTINGS",text);
            GUI.Label(new Rect(x,125,960,72),"VR & comfort",title);
            GUI.Label(new Rect(x,208,960,50),IsDeviceActive()?"Headset connected. Adjust movement and comfort for your visit.":"Playing on desktop. Connect a supported headset to explore in VR.",text);
            Action(new Rect(x,282,960,54),0,IsDeviceActive()?"Headset view":"Preview comfort camera",active?"ON":"OFF");
            Action(new Rect(x,346,960,54),1,"Turning",game.xr!=null&&game.xr.SnapTurn?"SNAP":"SMOOTH");
            Action(new Rect(x,410,960,54),2,"Comfort vignette",game.xr!=null&&game.xr.ComfortVignette?"ON":"OFF");
            Action(new Rect(x,474,960,54),3,"Room-scale movement",game.xr!=null&&game.xr.RoomScale?"ON":"OFF");
            Action(new Rect(x,538,960,54),4,"Hand tracking",game.xr!=null&&game.xr.HandTrackingEnabled?"ON":"OFF");
            Action(new Rect(x,602,470,54),5,"Recenter view","");
            Action(new Rect(x+490,602,470,54),6,"Back","");
            GUI.Label(new Rect(x,h-78,960,50),AlbionUIInput.ControllerPresent?"STICK  Navigate   ·   SELECT  Change   ·   BACK  Return":"ESC / F8  Back   ·   ↑ ↓  Navigate   ·   ENTER  Change",text);
            GUI.matrix=matrix; GUI.color=color; GUI.contentColor=content; GUI.backgroundColor=background; GUI.depth=depth;
        }
        static bool IsDeviceActive()
        {
            var settings = Type.GetType("UnityEngine.XR.XRSettings, UnityEngine.XRModule"); var property = settings == null ? null : settings.GetProperty("isDeviceActive");
            if (property == null) return false; try { return (bool)property.GetValue(null, null); } catch { return false; }
        }
    }
}
