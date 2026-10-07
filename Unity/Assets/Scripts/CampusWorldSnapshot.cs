using System;
using System.Collections.Generic;
using System.IO;

namespace AlbionOdyssey
{
    public struct CampusPose
    {
        public float x, y, z, yaw, speed;
        public CampusPose(float x, float y, float z, float yaw, float speed) { this.x = x; this.y = y; this.z = z; this.yaw = yaw; this.speed = speed; }
    }

    /// <summary>
    /// Engine-free wire format for the host's moving world: student and car poses plus the set of open
    /// doors. Poses are quantized (cm positions, 0.1 degree yaw, 0.1 m/s speed) to keep one snapshot well
    /// under a single UDP datagram, and decoding rejects anything out of range so a bad packet is ignored.
    /// </summary>
    public sealed class CampusWorldSnapshot
    {
        public const byte Version = 1;
        public const int MaxActors = 64, MaxCars = 32, MaxOpenDoors = 128;
        const float Bound = 2000f;

        public readonly List<CampusPose> students = new List<CampusPose>();
        public readonly List<CampusPose> cars = new List<CampusPose>();
        public readonly List<int> openDoors = new List<int>();

        /// <summary>A stable door key from its closed position, so streamed interiors need no shared ordering.</summary>
        public static int DoorId(float x, float y, float z)
        {
            unchecked { return ((int)Math.Round(x * 10f) * 73856093) ^ ((int)Math.Round(y * 10f) * 19349663) ^ ((int)Math.Round(z * 10f) * 83492791); }
        }

        public string Encode()
        {
            using (var stream = new MemoryStream())
            using (var w = new BinaryWriter(stream))
            {
                w.Write(Version);
                WritePoses(w, students, MaxActors); WritePoses(w, cars, MaxCars);
                int doors = Math.Min(openDoors.Count, MaxOpenDoors); w.Write((byte)doors);
                for (int i = 0; i < doors; i++) w.Write(openDoors[i]);
                w.Flush(); return Convert.ToBase64String(stream.ToArray());
            }
        }

        public static bool TryDecode(string text, out CampusWorldSnapshot snapshot)
        {
            snapshot = null; if (string.IsNullOrEmpty(text) || text.Length > 4096) return false;
            try
            {
                byte[] bytes = Convert.FromBase64String(text); var result = new CampusWorldSnapshot();
                using (var r = new BinaryReader(new MemoryStream(bytes)))
                {
                    if (r.ReadByte() != Version) return false;
                    if (!ReadPoses(r, result.students, MaxActors) || !ReadPoses(r, result.cars, MaxCars)) return false;
                    int doors = r.ReadByte(); if (doors > MaxOpenDoors) return false;
                    for (int i = 0; i < doors; i++) result.openDoors.Add(r.ReadInt32());
                    if (r.BaseStream.Position != r.BaseStream.Length) return false;
                }
                snapshot = result; return true;
            }
            catch (FormatException) { return false; }
            catch (EndOfStreamException) { return false; }
        }

        static void WritePoses(BinaryWriter w, List<CampusPose> poses, int max)
        {
            int count = Math.Min(poses.Count, max); w.Write((byte)count);
            for (int i = 0; i < count; i++)
            {
                var p = poses[i];
                w.Write(Q(p.x)); w.Write(Q(p.y)); w.Write(Q(p.z));
                w.Write((ushort)Math.Round(Wrap360(p.yaw) * 10f)); w.Write((short)Math.Round(Clamp(p.speed, -300f, 300f) * 10f));
            }
        }

        static bool ReadPoses(BinaryReader r, List<CampusPose> into, int max)
        {
            int count = r.ReadByte(); if (count > max) return false;
            for (int i = 0; i < count; i++)
            {
                float x = r.ReadInt32() / 100f, y = r.ReadInt32() / 100f, z = r.ReadInt32() / 100f; int yaw = r.ReadUInt16(); float speed = r.ReadInt16() / 10f;
                if (Math.Abs(x) > Bound || Math.Abs(y) > Bound || Math.Abs(z) > Bound || yaw > 3600) return false;
                into.Add(new CampusPose(x, y, z, yaw / 10f, speed));
            }
            return true;
        }

        static int Q(float meters) { return (int)Math.Round(Clamp(meters, -Bound, Bound) * 100f); }
        static float Clamp(float v, float lo, float hi) { return float.IsNaN(v) ? 0f : v < lo ? lo : v > hi ? hi : v; }
        static float Wrap360(float degrees) { degrees %= 360f; return float.IsNaN(degrees) ? 0f : degrees < 0 ? degrees + 360f : degrees; }
    }
}
