using System;

namespace AlbionOdyssey
{
    /// <summary>
    /// Engine-free campus day cycle maths, so lighting and the LAN replica can be unit-tested
    /// without Unity. Hours run 0-24; one in-game day lasts <see cref="DayLengthSeconds"/>.
    /// </summary>
    public static class CampusClock
    {
        public const float DayLengthSeconds = 1200f;
        public const float StartHour = 15.5f;
        // A client only snaps to the host clock when it has drifted further than this many hours.
        public const float SnapThresholdHours = .2f;

        public static float Wrap(float hour) { hour %= 24f; return hour < 0 ? hour + 24f : hour; }
        public static float Advance(float hour, float deltaSeconds) { return Wrap(hour + deltaSeconds / DayLengthSeconds * 24f); }

        /// <summary>Signed shortest distance from <paramref name="from"/> to <paramref name="to"/> across midnight.</summary>
        public static float Delta(float from, float to) { float d = Wrap(to) - Wrap(from); if (d > 12f) d -= 24f; if (d < -12f) d += 24f; return d; }

        /// <summary>Follows the host clock: smooth while close, a hard snap after a long stall or join.</summary>
        public static float Follow(float local, float host, float deltaSeconds)
        {
            float d = Delta(local, host);
            if (Math.Abs(d) > SnapThresholdHours) return Wrap(host);
            return Wrap(local + d * Math.Min(1f, deltaSeconds * 2f));
        }

        /// <summary>Sun height above the horizon in degrees (negative at night); peaks at solar noon, 12:00.</summary>
        public static float SunElevation(float hour) { return (float)Math.Sin((Wrap(hour) - 6f) / 24f * 2 * Math.PI) * 62f; }
        /// <summary>Compass sweep of the sun, east at dawn through south at noon to west at dusk.</summary>
        public static float SunAzimuth(float hour) { return 90f + (Wrap(hour) - 6f) / 12f * 180f; }

        /// <summary>0 at night, 1 in full day, with a smooth twilight around the horizon.</summary>
        public static float Daylight(float hour)
        {
            float t = (SunElevation(hour) + 6f) / 16f; t = t < 0 ? 0 : t > 1 ? 1 : t;
            return t * t * (3f - 2f * t);
        }
        /// <summary>Peaks near sunrise and sunset to warm the light and sky.</summary>
        public static float Golden(float hour)
        {
            float e = SunElevation(hour); if (e < -6f || e > 24f) return 0f;
            float t = 1f - Math.Abs(e - 5f) / 19f; return t < 0 ? 0 : t;
        }
        public static float SunIntensity(float hour) { return 1.45f * Daylight(hour); }
        public static string Label(float hour)
        {
            hour = Wrap(hour); int h = (int)hour, m = (int)((hour - h) * 60f); string suffix = h >= 12 ? "PM" : "AM"; int shown = h % 12 == 0 ? 12 : h % 12;
            return shown + ":" + m.ToString("00") + " " + suffix;
        }
    }
}
