using System;
using UnityEngine;

namespace AlbionOdyssey
{
    // Optional XR bridge. The desktop build remains the default; when an OpenXR loader
    // is installed, F8 enables headset rendering and a comfortable third-person offset.
    public sealed class OdysseyVrSupport : MonoBehaviour
    {
        OdysseyGame game; bool open; bool active; float oldFov; int focus; float openedAt;
        GUIStyle title, text, button;
        public bool Active => active;
        public void Setup(OdysseyGame owner)
        {
            game = owner; oldFov = game.player.eyes.fieldOfView;
            active = IsDeviceActive(); if (active) Apply(true);
        }
        public bool HandleInput()
        {
            if (Input.GetKeyDown(KeyCode.F8)) { if (open) ClosePanel(); else OpenPanel(); return true; }
            if (!open)
            {
                AlbionUIInput.Poll(out var unusedHorizontal, out var unusedVertical, out var unusedChoose, out var menu);
                if (menu) { OpenPanel(); return true; }
                return false;
            }
            if (Input.GetKeyDown(KeyCode.Escape)) { ClosePanel(); return true; }
            int horizontal, vertical; bool choose, cancel;
            AlbionUIInput.Poll(out horizontal, out vertical, out choose, out cancel);
            if (vertical != 0 || horizontal != 0) focus = (focus + (vertical != 0 ? -vertical : horizontal) + 6) % 6;
            if (cancel) { ClosePanel(); return true; }
            if (choose) { if (focus == 0) Apply(!active); else if (focus == 1) SetComfort("Odyssey.XR.SnapTurn", true); else if (focus == 2) SetComfort("Odyssey.XR.Vignette", false); else if (focus == 3) SetComfort("Odyssey.XR.RoomScale", true); else if (focus == 4) SetComfort("Odyssey.XR.Hands", false); else ClosePanel(); return true; }
            return true;
        }
        void SetComfort(string key, bool snap)
        {
            if (game == null || game.xr == null) return;
            if (key == "Odyssey.XR.SnapTurn") game.xr.SnapTurn = !game.xr.SnapTurn;
            else if (key == "Odyssey.XR.Vignette") game.xr.ComfortVignette = !game.xr.ComfortVignette;
            else if (key == "Odyssey.XR.RoomScale") game.xr.RoomScale = !game.xr.RoomScale;
            else if (key == "Odyssey.XR.Hands") game.xr.HandTrackingEnabled = !game.xr.HandTrackingEnabled;
            PlayerPrefs.SetInt(key, key == "Odyssey.XR.SnapTurn" ? (game.xr.SnapTurn ? 1 : 0) : key == "Odyssey.XR.Vignette" ? (game.xr.ComfortVignette ? 1 : 0) : key == "Odyssey.XR.RoomScale" ? (game.xr.RoomScale ? 1 : 0) : (game.xr.HandTrackingEnabled ? 1 : 0)); PlayerPrefs.Save();
        }
        string ComfortLabel(int index, string label) { return focus == index ? "▶  " + label : label; }
        void OpenPanel()
        {
            open = true; focus = 0; openedAt = Time.unscaledTime;
            if (game == null || game.player == null) return;
            game.player.controls = false; Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        }
        void ClosePanel()
        {
            open = false;
            if (game == null || game.player == null) return;
            game.player.controls = true; Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
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
        void OnGUI()
        {
            if (!open || game == null) return;
            if (title == null) { title = new GUIStyle(GUI.skin.label) { font=AlbionUITheme.DisplayFont,fontSize = 27, fontStyle = FontStyle.Bold }; text = new GUIStyle(GUI.skin.label) { font=AlbionUITheme.BodyFont,fontSize = 17, wordWrap = true }; button = AlbionUITheme.Button(16); }
            float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 800f); GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1)); float w = Screen.width / scale, h = Screen.height / scale, x = (w - 700) * .5f;
            GUI.color = new Color(.018f, .03f, .05f, .98f); GUI.DrawTexture(new Rect(0, 0, w, h), Texture2D.whiteTexture); GUI.color = Color.white;
            AlbionUITheme.TopRule(w); GUI.Label(new Rect(x, 130, 650, 45), "VR WALKTHROUGH", title); GUI.Label(new Rect(x, 195, 650, 90), "Optional XR support is detected automatically. Keep the room scale clear, use the controller stick to move, and take regular breaks. Desktop mode stays available when no headset is connected.", text);
            GUI.Label(new Rect(x, 315, 650, 34), IsDeviceActive() ? "XR device detected" : "No XR loader detected · desktop preview", text);
            string enable = (focus == 0 ? "▶  " : "") + (active ? "Disable VR" : "Enable VR");
            string snap = (game.xr != null && game.xr.SnapTurn ? "✓  " : "○  ") + "Snap turn";
            string vignette = (game.xr != null && game.xr.ComfortVignette ? "✓  " : "○  ") + "Comfort vignette";
            string room = (game.xr != null && game.xr.RoomScale ? "✓  " : "○  ") + "Room-scale movement";
            string hands = (game.xr != null && game.xr.HandTrackingEnabled ? "✓  " : "○  ") + "Hand tracking";
            string close = "Close · F8 / Esc";
            if (GUI.Button(new Rect(x, 365, 250, 44), enable, button)) { focus = 0; Apply(!active); }
            if (GUI.Button(new Rect(x + 270, 365, 250, 44), ComfortLabel(1, snap), button)) { focus = 1; SetComfort("Odyssey.XR.SnapTurn", true); }
            if (GUI.Button(new Rect(x, 420, 250, 44), ComfortLabel(2, vignette), button)) { focus = 2; SetComfort("Odyssey.XR.Vignette", false); }
            if (GUI.Button(new Rect(x + 270, 420, 250, 44), ComfortLabel(3, room), button)) { focus = 3; SetComfort("Odyssey.XR.RoomScale", true); }
            if (GUI.Button(new Rect(x, 475, 250, 44), ComfortLabel(4, hands), button)) { focus = 4; SetComfort("Odyssey.XR.Hands", false); }
            if (GUI.Button(new Rect(x + 270, 475, 250, 44), ComfortLabel(5, close), button)) { focus = 5; ClosePanel(); }
            GUI.Label(new Rect(x, h - 145, 650, 32), "Comfort mode: third-person camera, reduced camera distance, 90° field of view.", text);
            GUI.Label(new Rect(x, h - 105, 650, 32), "STICK Navigate   ·   TRIGGER Select   ·   MENU Back", text);
        }
        static bool IsDeviceActive()
        {
            var settings = Type.GetType("UnityEngine.XR.XRSettings, UnityEngine.XRModule"); var property = settings == null ? null : settings.GetProperty("isDeviceActive");
            if (property == null) return false; try { return (bool)property.GetValue(null, null); } catch { return false; }
        }
    }
}
