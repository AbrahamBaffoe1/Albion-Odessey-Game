using UnityEngine;

namespace AlbionOdyssey
{
    // Mac-friendly accessibility and input layer. Settings survive a restart and are
    // deliberately independent from the local Keeper save.
    public sealed class OdysseyAccessibility : MonoBehaviour
    {
        public static KeyCode ForwardKey { get; private set; } = KeyCode.W;
        public static KeyCode BackKey { get; private set; } = KeyCode.S;
        public static KeyCode LeftKey { get; private set; } = KeyCode.A;
        public static KeyCode RightKey { get; private set; } = KeyCode.D;
        public static KeyCode JumpKey { get; private set; } = KeyCode.Space;
        public static KeyCode InteractKey { get; private set; } = KeyCode.E;
        public static bool CaptionsEnabled { get; private set; } = true;
        public static bool LargeText { get; private set; }
        public static bool HighContrast { get; private set; }
        public static bool ReducedMotion { get; private set; }

        OdysseyGame game; bool open; float openedAt; GUIStyle title, text, button;
        public void Setup(OdysseyGame owner)
        {
            game = owner; CaptionsEnabled = PlayerPrefs.GetInt("Odyssey.Captions", 1) == 1; LargeText = PlayerPrefs.GetInt("Odyssey.LargeText", 0) == 1; HighContrast = PlayerPrefs.GetInt("Odyssey.HighContrast", 0) == 1; ReducedMotion = PlayerPrefs.GetInt("Odyssey.ReducedMotion", 0) == 1;
            bool alternate = PlayerPrefs.GetInt("Odyssey.AlternateKeys", 0) == 1; ApplyKeys(alternate);
        }
        static void ApplyKeys(bool alternate)
        {
            ForwardKey = alternate ? KeyCode.I : KeyCode.W; BackKey = alternate ? KeyCode.K : KeyCode.S; LeftKey = alternate ? KeyCode.J : KeyCode.A; RightKey = alternate ? KeyCode.L : KeyCode.D;
        }
        void Save(string key, int value) { PlayerPrefs.SetInt(key, value); PlayerPrefs.Save(); }
        public bool HandleInput()
        {
            if (!open && Input.GetKeyDown(KeyCode.F4)) { open = true; openedAt=Time.unscaledTime; game.player.controls = false; Cursor.lockState = CursorLockMode.None; Cursor.visible = true; return true; }
            if (!open) return false;
            if (Input.GetKeyDown(KeyCode.Escape)) { open = false; game.player.controls = !game.building && !game.life.PanelOpen; Cursor.lockState = game.player.pointerControls ? CursorLockMode.None : CursorLockMode.Locked; Cursor.visible = game.player.pointerControls; return true; }
            return true;
        }
        void OnGUI()
        {
            if (!open || game == null) return;
            if (title == null) { title = new GUIStyle(GUI.skin.label) { fontSize = 27, fontStyle = FontStyle.Bold }; title.normal.textColor=new Color(.96f,.94f,.86f); text = new GUIStyle(GUI.skin.label) { fontSize = 17, wordWrap = true }; text.normal.textColor=new Color(.88f,.88f,.92f); button = new GUIStyle(GUI.skin.button) { fontSize = 16, padding = new RectOffset(12, 12, 6, 6) }; }
            float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 800f); GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1)); GUI.matrix=AlbionUITheme.Slide(GUI.matrix,openedAt,ReducedMotion); float w = Screen.width / scale, h = Screen.height / scale;
            GUI.color = HighContrast ? Color.black : new Color(.025f, .028f, .052f, .98f); GUI.DrawTexture(new Rect(0, 0, w, h), Texture2D.whiteTexture); GUI.color = Color.white;
            float x = (w - 780) * .5f; GUI.Label(new Rect(x, 70, 740, 48), "ACCESSIBILITY & CONTROLS", title); GUI.Label(new Rect(x, 125, 740, 54), "F4 opens this panel. Settings are saved for this Mac. Controller left stick and A/Cross jump are supported.", text);
            if (GUI.Button(new Rect(x, 210, 360, 46), (CaptionsEnabled ? "✓ " : "○ ") + "Captions and achievement text", button)) { CaptionsEnabled = !CaptionsEnabled; Save("Odyssey.Captions", CaptionsEnabled ? 1 : 0); }
            if (GUI.Button(new Rect(x, 270, 360, 46), (LargeText ? "✓ " : "○ ") + "Large readable interface text", button)) { LargeText = !LargeText; Save("Odyssey.LargeText", LargeText ? 1 : 0); }
            if (GUI.Button(new Rect(x, 330, 360, 46), (HighContrast ? "✓ " : "○ ") + "High contrast panels", button)) { HighContrast = !HighContrast; Save("Odyssey.HighContrast", HighContrast ? 1 : 0); }
            if (GUI.Button(new Rect(x, 390, 360, 46), (ReducedMotion ? "✓ " : "○ ") + "Reduce camera and NPC motion", button)) { ReducedMotion = !ReducedMotion; Save("Odyssey.ReducedMotion", ReducedMotion ? 1 : 0); }
            bool alternate = PlayerPrefs.GetInt("Odyssey.AlternateKeys", 0) == 1;
            if (GUI.Button(new Rect(x, 470, 360, 46), (alternate ? "✓ " : "○ ") + (alternate ? "I J K L movement" : "W A S D movement"), button)) { alternate = !alternate; ApplyKeys(alternate); Save("Odyssey.AlternateKeys", alternate ? 1 : 0); }
            GUI.Label(new Rect(x + 405, 210, 360, 170), "KEYBOARD\nMove: " + ForwardKey + " " + LeftKey + " " + BackKey + " " + RightKey + "\nJump: " + JumpKey + "   Interact: " + InteractKey + "\nCamera: V   Menu: Esc\nController: left stick, right stick, A/Cross, Menu", text);
            if (GUI.Button(new Rect(x + 405, 470, 260, 46), "Close · F4 / Esc", button)) { open = false; game.player.controls = !game.building && !game.life.PanelOpen; Cursor.lockState = game.player.pointerControls ? CursorLockMode.None : CursorLockMode.Locked; Cursor.visible = game.player.pointerControls; }
        }
    }
}
