using System;
using System.IO;
using UnityEngine;
using UnityEngine.Profiling;

namespace AlbionOdyssey
{
    [Serializable] public sealed class OdysseyRuntimeReport
    {
        public string version = "0.11"; public float averageFps, minimumFps, maximumFps; public long allocatedBytes; public bool xrActive; public int remotePlayers; public string timestamp;
    }
    public sealed class OdysseyRuntimeDiagnostics : MonoBehaviour
    {
        OdysseyGame game; float elapsed, total, minimum = 999, maximum; int frames; bool open; GUIStyle title, text;
        public OdysseyRuntimeReport LastReport { get; private set; }
        public void Setup(OdysseyGame owner) { game = owner; }
        void Update()
        {
            float fps = 1f / Mathf.Max(.001f, Time.unscaledDeltaTime); elapsed += Time.unscaledDeltaTime; total += fps; frames++; minimum = Mathf.Min(minimum, fps); maximum = Mathf.Max(maximum, fps);
            if (Input.GetKeyDown(KeyCode.F9)) open = !open;
            if (elapsed >= 10f) { WriteReport(); elapsed = 0; total = 0; frames = 0; minimum = 999; maximum = 0; }
        }
        void WriteReport()
        {
            LastReport = new OdysseyRuntimeReport { averageFps = frames == 0 ? 0 : total / frames, minimumFps = minimum == 999 ? 0 : minimum, maximumFps = maximum, allocatedBytes = Profiler.GetTotalAllocatedMemoryLong(), xrActive = game.vr != null && game.vr.Active, remotePlayers = game.online == null ? 0 : game.online.RemoteCount, timestamp = DateTime.UtcNow.ToString("o") };
            try { File.WriteAllText(Path.Combine(Application.persistentDataPath, "runtime-profile.json"), JsonUtility.ToJson(LastReport, true)); } catch { }
        }
        void OnApplicationQuit() { WriteReport(); }
        void OnGUI()
        {
            if (!open || game == null) return;
            if (title == null) { title = new GUIStyle(GUI.skin.label) { fontSize = 24, fontStyle = FontStyle.Bold }; text = new GUIStyle(GUI.skin.label) { fontSize = 16, wordWrap = true }; }
            float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 800f); GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1)); float w = Screen.width / scale;
            GUI.color = new Color(.02f, .04f, .05f, .94f); GUI.DrawTexture(new Rect(24, 24, 390, 155), Texture2D.whiteTexture); GUI.color = Color.white; GUI.Label(new Rect(42, 40, 350, 34), "PERFORMANCE · F9", title);
            float fps = 1f / Mathf.Max(.001f, Time.unscaledDeltaTime); GUI.Label(new Rect(42, 84, 350, 78), "Frame rate: " + fps.ToString("0") + " FPS\nAllocated: " + (Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f)).ToString("0") + " MB\nProfile saved every 10 seconds", text);
        }
    }
}
