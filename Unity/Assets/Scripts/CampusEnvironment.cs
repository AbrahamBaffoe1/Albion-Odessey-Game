using UnityEngine;
using UnityEngine.Rendering;

namespace AlbionOdyssey
{
    /// <summary>
    /// Real-time campus environment: a moving sun with soft shadows, twilight sky and fog, and moonlit nights. On a LAN session the host owns the clock and every joined player
    /// follows it, so the whole campus looks the same on every machine.
    /// </summary>
    public sealed class CampusEnvironment : MonoBehaviour
    {
        static readonly Color DaySky = new Color(.55f, .68f, .86f), DuskSky = new Color(.95f, .55f, .34f), NightSky = new Color(.035f, .05f, .12f);
        static readonly Color DayFog = new Color(.62f, .69f, .73f), NightFog = new Color(.03f, .04f, .08f);
        static readonly Color DaySun = new Color(1f, .96f, .88f), GoldenSun = new Color(1f, .62f, .32f), Moon = new Color(.45f, .55f, .8f);

        OdysseyGame game; Light sun; Material skybox; Color baseTint = Color.white; float hour = CampusClock.StartHour;
        bool following; float lastHostPacket, hostTarget;

        public float Hour => hour;
        public bool Following => following;
        public string TimeLabel => CampusClock.Label(hour);

        public void Setup(OdysseyGame owner)
        {
            game = owner; sun = RenderSettings.sun;
            var source = RenderSettings.skybox; if (source != null) { skybox = RenderSettings.skybox = new Material(source); if (skybox.HasProperty("_SkyTint")) baseTint = skybox.GetColor("_SkyTint"); }
            Apply();
        }

        /// <summary>Called by the LAN session when a host clock packet arrives.</summary>
        public void ReceiveHost(float hostHour)
        {
            if (!following) hour = CampusClock.Wrap(hostHour);
            hostTarget = CampusClock.Wrap(hostHour); lastHostPacket = Time.unscaledTime; following = true;
        }
        public void SetHour(float value) { hour = CampusClock.Wrap(value); Apply(); }

        void Update()
        {
            if (game == null || PlaytestMode.Active) return; // verification runs keep the fixed afternoon light
            if (following && Time.unscaledTime - lastHostPacket > 6f) following = false;
            if (following) hour = CampusClock.Follow(hour, CampusClock.Advance(hostTarget, Time.unscaledTime - lastHostPacket), Time.deltaTime);
            else hour = CampusClock.Advance(hour, Time.deltaTime);
            Apply();
        }

        void Apply()
        {
            if (sun == null) sun = RenderSettings.sun; if (sun == null) return;
            float day = CampusClock.Daylight(hour), gold = CampusClock.Golden(hour), elevation = CampusClock.SunElevation(hour);
            // The moon takes over the key light after sunset so shadows never vanish completely.
            bool night = elevation < 0f; float lift = night ? Mathf.Clamp(-elevation, 2f, 60f) : Mathf.Max(elevation, 2f);
            sun.transform.rotation = Quaternion.Euler(lift, CampusClock.SunAzimuth(hour) + (night ? 180f : 0f), 0f);
            sun.color = night ? Moon : Color.Lerp(DaySun, GoldenSun, gold);
            sun.intensity = night ? .18f : Mathf.Max(.18f, CampusClock.SunIntensity(hour));
            sun.shadows = LightShadows.Soft;
            Color sky = Color.Lerp(Color.Lerp(NightSky, DaySky, day), DuskSky, gold * .75f);
            RenderSettings.ambientSkyColor = sky * Mathf.Lerp(.35f, 1f, day);
            RenderSettings.ambientEquatorColor = Color.Lerp(NightFog * 1.4f, new Color(.32f, .36f, .4f), day);
            RenderSettings.ambientGroundColor = Color.Lerp(NightFog, new Color(.19f, .21f, .22f), day);
            RenderSettings.fogColor = Color.Lerp(Color.Lerp(NightFog, DayFog, day), DuskSky * .8f, gold * .5f);
            if (game.city != null) game.city.UpdateLamps(day);
            bool snow = game.weather != null && game.weather.IsSnowing; RenderSettings.fogDensity = Mathf.Lerp(.0008f, .0022f, snow ? 1f : 0f);
            if (skybox != null)
            {
                if (skybox.HasProperty("_Exposure")) skybox.SetFloat("_Exposure", Mathf.Lerp(.12f, 1.1f, day));
                // Skybox/Procedural exposes _SkyTint (not _Tint); warm it toward dusk from the authored tint.
                if (skybox.HasProperty("_SkyTint")) skybox.SetColor("_SkyTint", Color.Lerp(baseTint, DuskSky, gold * .6f));
            }
        }

    }
}
