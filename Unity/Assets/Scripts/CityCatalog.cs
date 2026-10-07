using System;
using System.Collections.Generic;

namespace AlbionOdyssey
{
    public sealed class CityLandmark
    {
        public string id, name, kind, summary, source;
        // Metres from the city origin: x east, z north. Game-scale approximations, not surveyed positions.
        public float x, z, width, depth, height;
        public CityLandmark(string id, string name, string kind, float x, float z, float width, float depth, float height, string summary, string source)
        { this.id = id; this.name = name; this.kind = kind; this.x = x; this.z = z; this.width = width; this.depth = depth; this.height = height; this.summary = summary; this.source = source; }
    }

    /// <summary>
    /// The City of Albion, Michigan layer that surrounds the college. Every statement in a summary is taken from the
    /// cited public source; positions, sizes and facades are game-scale approximations. Engine-free so it can be validated in CI.
    /// </summary>
    public static class CityCatalog
    {
        // The city district spans x -330..330 and z -375..375 around this origin (west of the campus, which lies east of downtown).
        public const float OriginX = -930f, OriginZ = 430f, HalfWidth = 330f, HalfDepth = 375f;
        public const float SuperiorX = 0f, ErieZ = -84f, RiverZ = -190f, RiverWidth = 26f, BridgeHalfGap = 9f;

        public static readonly CityLandmark[] Landmarks =
        {
            new CityLandmark("bohm", "Bohm Theatre", "Theatre", 22f, -30f, 16f, 28f, 11f,
                "The Bohm Theatre stands at 201 S. Superior Street. It was built to present movies and vaudeville shows and opened on Christmas Day 1929. The Friends of the Bohm Theatre acquired the building from the county tax auction, restoration began in September 2011, and the theatre reopened on October 16, 2014.",
                "https://bohmtheatre.org/about/"),
            new CityLandmark("gardner", "Gardner House Museum", "Museum", -120f, 100f, 18f, 15f, 14f,
                "Augustus P. Gardner (1817-1905), a wealthy hardware merchant, built this Victorian house in 1875. It is a three-story, thirteen-room mansion with a mansard roof and was Gardner's home until his death in 1905. The museum opens from Mother's Day weekend through the end of September, and by appointment for groups.",
                "https://www.hmdb.org/m.asp?m=116148"),
            new CityLandmark("rieger", "Rieger Park · Mother's Day marker", "Park", 150f, -137f, 40f, 30f, 3f,
                "The first known observance of Mother's Day was held in Albion on May 13, 1877, during a dispute connected with the temperance movement. A Michigan Historical Marker in Rieger Park honors Juliet Blakeley as the founder of Mother's Day.",
                "https://albionmich.net/historical-marker-for-mothers-day/"),
            new CityLandmark("victory", "Victory Park", "Park", -150f, -250f, 60f, 44f, 8f,
                "Victory Park is Albion's largest public park, just south of downtown along the Kalamazoo River. It has a 10,000-square-foot castle-themed playground, a waterfall, a natural spring and a formal garden.",
                "https://downtownalbion.com/the-parks-of-albion-michigan/"),
            new CityLandmark("superior", "Superior Street Commercial Historic District", "District", 0f, 120f, 26f, 120f, 9f,
                "The Superior Street Commercial Historic District runs primarily along Superior Street from Elm Street to Vine Street, roughly bounded by the Kalamazoo River and Cass, Elm, Eaton and Vine Streets.",
                "https://en.wikipedia.org/wiki/Superior_Street_Commercial_Historic_District"),
            new CityLandmark("river", "Kalamazoo River · Albion River Trail", "River", 90f, RiverZ, 30f, 12f, 3f,
                "Albion sits at the confluence of the north and south branches of the Kalamazoo River. The Albion River Trail is a 1.6-mile paved pathway along the river.",
                "https://www.cityofalbionmi.gov/visitors/activities_and_attractions/albion_river_trail.php"),
            new CityLandmark("riverside", "Riverside Cemetery", "Cemetery", 232f, -262f, 56f, 44f, 6f,
                "Riverside Cemetery is a forty-six-acre complex of terraced hillside on the east bank, overlooking the mill pond and the south branch of the Kalamazoo River.",
                "https://albionmich.net/riverside-cemetery/"),
        };

        public static bool Valid()
        {
            var ids = new HashSet<string>();
            foreach (var l in Landmarks)
            {
                if (string.IsNullOrWhiteSpace(l.id) || !ids.Add(l.id)) return false;
                if (string.IsNullOrWhiteSpace(l.name) || string.IsNullOrWhiteSpace(l.summary) || l.summary.Length > 520) return false;
                if (!SafeSource(l.source)) return false;
                if (l.width <= 0 || l.depth <= 0 || l.height <= 0) return false;
                if (Math.Abs(l.x) + l.width / 2 > HalfWidth || Math.Abs(l.z) + l.depth / 2 > HalfDepth) return false;
            }
            return true;
        }

        /// <summary>Only plain https links are ever opened from the game.</summary>
        public static bool SafeSource(string url)
        {
            if (string.IsNullOrEmpty(url) || url.Length > 200 || !url.StartsWith("https://", StringComparison.Ordinal)) return false;
            foreach (char c in url) if (c <= ' ' || c == '\\' || c == '<' || c == '>' || c == '"') return false;
            return url.IndexOf('@') < 0;
        }

        /// <summary>The landmark within <paramref name="range"/> metres (city-local x/z) that is closest, or null.</summary>
        public static CityLandmark Nearest(float localX, float localZ, float range)
        {
            CityLandmark best = null; float bestDistance = range;
            foreach (var l in Landmarks)
            {
                float dx = Math.Max(0f, Math.Abs(localX - l.x) - l.width / 2), dz = Math.Max(0f, Math.Abs(localZ - l.z) - l.depth / 2);
                float d = (float)Math.Sqrt(dx * dx + dz * dz); if (d <= bestDistance) { bestDistance = d; best = l; }
            }
            return best;
        }
    }
}
