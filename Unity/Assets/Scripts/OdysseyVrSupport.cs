using System;
using UnityEngine;

namespace AlbionOdyssey
{
    // Optional XR bridge. The desktop build remains the default; when an OpenXR loader
    // is installed, F8 enables headset rendering and a comfortable third-person offset.
    public sealed class OdysseyVrSupport : MonoBehaviour
    {
        OdysseyGame game; bool open; bool active; float oldFov;
        GUIStyle title, text, button;
        public bool Active => active;
        public void Setup(OdysseyGame owner)
        {
            game = owner; oldFov = game.player.eyes.fieldOfView;
            active = IsDeviceActive(); if (active) Apply(true);
        }
        public bool HandleInput()
        {
            if (Input.GetKeyDown(KeyCode.F8)) { open = !open; if (open) { game.player.controls = false; Cursor.lockState = CursorLockMode.None; Cursor.visible = true; } else { game.player.controls = true; Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; } return true; }
            if (!open) return false; if (Input.GetKeyDown(KeyCode.Escape)) { open = false; game.player.controls = true; Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; return true; } return true;
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
            if (title == null) { title = new GUIStyle(GUI.skin.label) { fontSize = 27, fontStyle = FontStyle.Bold }; text = new GUIStyle(GUI.skin.label) { fontSize = 17, wordWrap = true }; button = new GUIStyle(GUI.skin.button) { fontSize = 16, padding = new RectOffset(12, 12, 6, 6) }; }
            float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 800f); GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1)); float w = Screen.width / scale, h = Screen.height / scale, x = (w - 700) * .5f;
            GUI.color = new Color(.018f, .03f, .05f, .98f); GUI.DrawTexture(new Rect(0, 0, w, h), Texture2D.whiteTexture); GUI.color = Color.white;
            GUI.Label(new Rect(x, 130, 650, 45), "VR WALKTHROUGH", title); GUI.Label(new Rect(x, 195, 650, 90), "Optional XR support is detected automatically. Keep the room scale clear, use the controller stick to move, and take regular breaks. Desktop mode stays available when no headset is connected.", text);
            GUI.Label(new Rect(x, 315, 650, 34), IsDeviceActive() ? "XR device detected" : "No XR loader detected · desktop preview", text);
            if (GUI.Button(new Rect(x, 380, 250, 48), active ? "Disable VR" : "Enable VR", button)) Apply(!active);
            if (GUI.Button(new Rect(x + 270, 380, 250, 48), "Close · F8 / Esc", button)) { open = false; game.player.controls = true; Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
            GUI.Label(new Rect(x, h - 105, 650, 32), "Comfort mode: third-person camera, reduced camera distance, 90° field of view.", text);
        }
        static bool IsDeviceActive()
        {
            var settings = Type.GetType("UnityEngine.XR.XRSettings, UnityEngine.XRModule"); var property = settings == null ? null : settings.GetProperty("isDeviceActive");
            if (property == null) return false; try { return (bool)property.GetValue(null, null); } catch { return false; }
        }
    }
}
