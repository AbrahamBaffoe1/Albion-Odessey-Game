using System.Collections.Generic;
using UnityEngine;

namespace AlbionOdyssey
{
    /// <summary>
    /// Captures the host's moving world (route students, cars, open doors) and applies it on joined
    /// players, so everyone sees the same people, traffic and doorways. The host stays authoritative:
    /// while following, local student AI and door toggles are paused. A car the local player is driving
    /// is never overwritten.
    /// </summary>
    public sealed class CampusWorldReplica : MonoBehaviour
    {
        OdysseyGame game; bool following; float lastApplied;
        public bool Following => following;

        public void Setup(OdysseyGame owner) { game = owner; }

        public CampusWorldSnapshot Capture()
        {
            var snapshot = new CampusWorldSnapshot();
            if (game == null) return snapshot;
            if (game.world != null) foreach (var agent in game.world.Agents) { var p = agent.transform.position; snapshot.students.Add(new CampusPose(p.x, p.y, p.z, agent.transform.eulerAngles.y, agent.CurrentSpeed)); }
            if (game.campus != null) foreach (var car in game.campus.cars) { var p = car.transform.position; snapshot.cars.Add(new CampusPose(p.x, p.y, p.z, car.transform.eulerAngles.y, car.speed)); }
            if (CampusBuildings.Instance != null) foreach (var door in CampusBuildings.Instance.AllDoors()) if (door != null && door.IsOpen) { var p = door.ClosedPosition; snapshot.openDoors.Add(CampusWorldSnapshot.DoorId(p.x, p.y, p.z)); }
            return snapshot;
        }

        public void Apply(CampusWorldSnapshot snapshot)
        {
            if (game == null || snapshot == null) return;
            SetFollowing(true); lastApplied = Time.unscaledTime;
            if (game.world != null)
            {
                var agents = game.world.Agents;
                for (int i = 0; i < agents.Count && i < snapshot.students.Count; i++) { var p = snapshot.students[i]; agents[i].ApplyReplicated(new Vector3(p.x, p.y, p.z), p.yaw, p.speed); }
            }
            if (game.campus != null)
            {
                var cars = game.campus.cars;
                for (int i = 0; i < cars.Count && i < snapshot.cars.Count; i++) { var p = snapshot.cars[i]; cars[i].ApplyReplicated(new Vector3(p.x, p.y, p.z), p.yaw, p.speed); }
            }
            if (CampusBuildings.Instance != null)
            {
                var open = new HashSet<int>(snapshot.openDoors);
                foreach (var door in CampusBuildings.Instance.AllDoors()) if (door != null) { var p = door.ClosedPosition; door.SetOpen(open.Contains(CampusWorldSnapshot.DoorId(p.x, p.y, p.z))); }
            }
        }

        /// <summary>Hands control back to local simulation, e.g. when the session ends or the host goes quiet.</summary>
        public void SetFollowing(bool value)
        {
            if (following == value || game == null) return;
            following = value;
            if (game.world != null) foreach (var agent in game.world.Agents) agent.Replicated = value;
            if (game.campus != null) foreach (var car in game.campus.cars) car.Replicated = value;
            if (CampusBuildings.Instance != null) CampusBuildings.Instance.DoorsReplicated = value;
        }

        void Update() { if (following && Time.unscaledTime - lastApplied > 6f) SetFollowing(false); }
    }
}
