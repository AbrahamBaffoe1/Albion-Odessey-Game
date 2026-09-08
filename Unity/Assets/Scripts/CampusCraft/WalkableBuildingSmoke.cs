using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace AlbionOdyssey
{
    public sealed class WalkableBuildingSmoke : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-walkableBuildingSmoke") >= 0) new GameObject("Walkable building verification").AddComponent<WalkableBuildingSmoke>();
        }

        IEnumerator Start()
        {
            OdysseyGame game = null; CampusBuildings buildings = null; float timeout = Time.realtimeSinceStartup + 45;
            while ((game = FindAnyObjectByType<OdysseyGame>()) == null || (buildings = CampusBuildings.Instance) == null)
            {
                if (Time.realtimeSinceStartup > timeout) { Fail("Game boot timeout"); yield break; }
                yield return null;
            }
            try
            {
                var robinson = buildings.Building("16"); var bonta = buildings.Building("1"); var science = buildings.Building("18");
                Require(robinson != null && bonta != null && science != null, "Robinson, Bonta or Science Complex was not registered");
                Require(robinson.Root.activeSelf && bonta.Root.activeSelf, "Building root inactive");
                Require(robinson.Doors.Count >= 9 && bonta.Doors.Count >= 3, "Walkable doors missing");
                Require(robinson.Floors == 4 && bonta.Floors == 1, "Measured floor counts missing");
                buildings.VisitCampus("16");
                Require(Vector3.Distance(game.player.transform.position, robinson.Origin + new Vector3(0, 0, -robinson.Depth * .5f - 3f)) < .2f, "Robinson arrival is not at the entrance");
                var entrance = robinson.Doors[0]; Require(!entrance.IsOpen, "Robinson entrance should start closed"); Require(entrance.Toggle(game.player), "Robinson entrance did not open");
                game.player.Teleport(robinson.Origin + new Vector3(0, .08f, -1.0f)); Require(robinson.Inside, "Robinson interior is not walkable");
                game.tour.Open(game.tour.catalog.ForCampus("16")); Require(game.tour.selected != null && game.tour.selected.name.Contains("Robinson"), "Robinson history mapping missing"); game.tour.Close();
                buildings.VisitCampus("1"); Require(Vector3.Distance(game.player.transform.position, bonta.Origin + new Vector3(0, 0, -bonta.Depth * .5f - 3f)) < .2f, "Bonta arrival is not at the entrance");
                Require(bonta.Doors[0] != null, "Bonta main door missing"); game.tour.Open(game.tour.catalog.ForCampus("1")); Require(game.tour.selected != null && game.tour.selected.name.Contains("Bonta"), "Bonta history mapping missing"); game.tour.Close();
                buildings.VisitCampus("18"); Require(Vector3.Distance(game.player.transform.position, science.Origin + new Vector3(0, 0, -science.Depth * .5f - 3f)) < .2f, "Science Complex arrival is not at the entrance"); Require(science.Floors == 4 && science.Doors.Count >= 9, "Science Complex floors or doors missing"); game.tour.Open(game.tour.catalog.ForCampus("18n")); Require(game.tour.selected != null && game.tour.selected.name.Contains("Norris"), "Science Complex history mapping missing"); game.tour.Close();
                Write("{\"passed\":true,\"robinsonFloors\":4,\"robinsonDoors\":" + robinson.Doors.Count + ",\"bontaFloors\":1,\"bontaDoors\":" + bonta.Doors.Count + ",\"scienceFloors\":4,\"scienceDoors\":" + science.Doors.Count + ",\"history\":true}"); Debug.Log("WALKABLE_BUILDING_SMOKE_OK"); Application.Quit(0);
            }
            catch (Exception e) { Fail(e.Message); }
            yield break;
        }

        static void Require(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); }
        static void Write(string text) { string path = Environment.GetEnvironmentVariable("WALKABLE_BUILDING_OUTPUT") ?? Path.Combine(Application.persistentDataPath, "walkable-building-smoke.json"); Directory.CreateDirectory(Path.GetDirectoryName(path)); File.WriteAllText(path, text); }
        static void Fail(string message) { Write("{\"passed\":false,\"reason\":\"" + message.Replace("\"", "'") + "\"}"); Debug.LogError("WALKABLE_BUILDING_SMOKE_FAILED " + message); Application.Quit(1); }
    }
}
