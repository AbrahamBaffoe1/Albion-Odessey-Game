using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace AlbionOdyssey
{
    [Serializable] public sealed class OdysseyCrashRecord
    {
        public string timestamp, condition, stackTrace, version, platform, scene;
    }

    // Keeps the previous session recoverable and writes a small report when Unity
    // emits an exception or assertion. It never replaces the game's save file.
    public sealed class OdysseyCrashReporter : MonoBehaviour
    {
        static readonly object Gate = new object();
        readonly Queue<string> recent = new Queue<string>();
        string root, marker, report; bool shuttingDown, writing;
        OdysseyGame game;

        public bool PreviousSessionWasInterrupted { get; private set; }
        public string ReportDirectory => root;

        public void Setup(OdysseyGame owner)
        {
            game = owner; root = Path.Combine(Application.persistentDataPath, "crash-reports"); marker = Path.Combine(root, "session.active"); report = Path.Combine(root, "latest.json");
            try
            {
                Directory.CreateDirectory(root); PreviousSessionWasInterrupted = File.Exists(marker); File.WriteAllText(marker, DateTime.UtcNow.ToString("o"));
                Application.logMessageReceivedThreaded += OnLog;
            }
            catch (Exception e) { Debug.LogWarning("Crash reporter unavailable: " + e.Message); }
        }

        void OnLog(string condition, string stackTrace, LogType type)
        {
            if (shuttingDown || (type != LogType.Exception && type != LogType.Assert && type != LogType.Error)) return;
            string[] snapshot;
            lock (Gate)
            {
                string line = DateTime.UtcNow.ToString("o") + " | " + condition + "\n" + stackTrace;
                while (recent.Count >= 20) recent.Dequeue(); recent.Enqueue(line);
                if (writing) return; writing = true;
                snapshot = recent.ToArray();
            }
            try
            {
                var record = new OdysseyCrashRecord { timestamp = DateTime.UtcNow.ToString("o"), condition = condition, stackTrace = stackTrace, version = Application.version, platform = Application.platform.ToString(), scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name };
                File.WriteAllText(report, JsonUtility.ToJson(record, true));
                File.WriteAllLines(Path.Combine(root, "recent.log"), snapshot);
            }
            catch { }
            finally { lock (Gate) writing = false; }
        }

        void OnApplicationQuit()
        {
            shuttingDown = true; Application.logMessageReceivedThreaded -= OnLog;
            try { if (File.Exists(marker)) File.Delete(marker); } catch { }
        }
    }
}
