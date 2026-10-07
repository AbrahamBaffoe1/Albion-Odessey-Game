using System.Collections.Generic;
using UnityEngine;

namespace AlbionOdyssey
{
    /// <summary>
    /// The City of Albion, Michigan: a walkable downtown district west of the college, joined to the campus by Erie Street.
    /// It holds the Kalamazoo River with a bridge and river trail, the Superior Street storefronts and seven sourced landmarks.
    /// Geometry is a game-scale approximation; each landmark opens a sourced story with H.
    /// </summary>
    public sealed class AlbionCity : MonoBehaviour
    {
        const float NearRange = 14f;
        OdysseyGame game; Transform root;
        Material lampHead, lampPost, brickWarm, brickRed, limestone, slate, asphalt, sidewalk, lawn, water, glass, gold, bronze, hedge, trunk, leaf, stoneDark, awning;
        CityLandmark open; string hintId = ""; float openedAt; int focus; GUIStyle title, body, small, button;
        readonly Dictionary<string, GameObject> landmarkRoots = new Dictionary<string, GameObject>();

        public static Vector3 Origin => new Vector3(CityCatalog.OriginX, 0f, CityCatalog.OriginZ);
        public static Vector3 ToWorld(float x, float y, float z) { return Origin + new Vector3(x, y, z); }
        public bool PanelOpen => open != null;
        public int LandmarkCount => landmarkRoots.Count;
        /// <summary>True when the player stands within the city district.</summary>
        public bool InCity => game != null && game.player != null
            && Mathf.Abs(game.player.transform.position.x - CityCatalog.OriginX) < CityCatalog.HalfWidth
            && Mathf.Abs(game.player.transform.position.z - CityCatalog.OriginZ) < CityCatalog.HalfDepth;

        public void Setup(OdysseyGame owner)
        {
            game = owner;
            root = new GameObject("City of Albion · Michigan").transform;
            BuildMaterials();
            BuildGroundAndStreets();
            BuildRiver();
            BuildSuperiorStreet();
            BuildBohm(); BuildGardner(); BuildRieger(); BuildVictory(); BuildRiverTrail(); BuildRiverside();
            BuildTrees();
            BuildLamps();
            Physics.SyncTransforms();
            BuildResidents();
            BuildTraffic();
        }

        // ── Input and story panel ─────────────────────────────────────────────

        public bool HandleInput()
        {
            if (game == null || game.player == null) return false;
            if (open != null) return HandlePanelInput();
            if (game.life.PanelOpen || game.building || game.journalOpen) return false;
            if (Input.GetKeyDown(KeyCode.D)&&Input.GetKey(KeyCode.LeftControl)&&Input.GetKey(KeyCode.LeftShift)) { Travel(); return true; }
            var near = NearbyLandmark();
            string id = near != null ? near.id : "";
            if (id != hintId) { hintId = id; if (near != null) game.notice = near.name + " · press H to read its story."; }
            if (near != null && Input.GetKeyDown(KeyCode.H)) { OpenPanel(near); return true; }
            return false;
        }

        public CityLandmark NearbyLandmark()
        {
            var p = game.player.transform.position; if (!InCity) return null;
            return CityCatalog.Nearest(p.x - CityCatalog.OriginX, p.z - CityCatalog.OriginZ, NearRange);
        }

        public void Travel()
        {
            if (!game.player.TryExitVehicle()) { game.notice = "Move the car into an open space before travelling."; return; }
            game.tour.StopMedia();
            if (InCity)
            {
                var home = CampusCatalog.Places[0]; foreach (var p in CampusCatalog.Places) if (p.id == "26") home = p;
                game.player.Teleport(home.Arrival); game.notice = "Back at Albion College. Press Ctrl+Shift+D to return to downtown Albion.";
            }
            else
            {
                game.player.Teleport(ToWorld(0f, .08f, CityCatalog.ErieZ + 12f)); game.player.transform.rotation = Quaternion.Euler(0, 0f, 0);
                game.notice = "Downtown Albion, Michigan. Walk to a landmark and press H, or press Ctrl+Shift+D to return to campus.";
            }
        }

        void OpenPanel(CityLandmark landmark)
        {
            open = landmark; focus = 0; openedAt = Time.unscaledTime; game.player.controls = false;
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        }

        void ClosePanel()
        {
            open = null; game.player.controls = !game.building && !game.life.PanelOpen;
            Cursor.lockState = game.player.pointerControls ? CursorLockMode.None : CursorLockMode.Locked; Cursor.visible = game.player.pointerControls;
        }

        bool HandlePanelInput()
        {
            if (AlbionUIInput.Poll(out var horizontal, out var vertical, out var choose, out var cancel))
            {
                if (cancel) ClosePanel();
                else
                {
                    if (horizontal != 0 || vertical != 0) focus = 1 - focus;
                    if (choose) { if (focus == 0) OpenSource(); else ClosePanel(); }
                }
                return true;
            }
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.H) || Input.GetKeyDown(KeyCode.E)) ClosePanel();
            return true;
        }

        void OpenSource() { if (open != null && CityCatalog.SafeSource(open.source)) Application.OpenURL(open.source); }

        void OnGUI()
        {
            if (open == null || game == null) return;
            if (title == null)
            {
                title = new GUIStyle(GUI.skin.label) { font = AlbionUITheme.DisplayFont, fontSize = 27, fontStyle = FontStyle.Bold, wordWrap = true }; title.normal.textColor = new Color(.96f, .94f, .86f);
                body = new GUIStyle(GUI.skin.label) { font = AlbionUITheme.BodyFont, fontSize = 19, wordWrap = true }; body.normal.textColor = new Color(.9f, .9f, .94f);
                small = new GUIStyle(body) { fontSize = 14 }; small.normal.textColor = new Color(.62f, .7f, .8f); button = AlbionUITheme.Button(16);
            }
            float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 800f); GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
            GUI.matrix = AlbionUITheme.Slide(GUI.matrix, openedAt, OdysseyAccessibility.ReducedMotion);
            float w = Screen.width / scale, h = Screen.height / scale, x = (w - 780) * .5f;
            GUI.color = new Color(.025f, .028f, .052f, .98f); GUI.DrawTexture(new Rect(0, 0, w, h), Texture2D.whiteTexture); GUI.color = Color.white;
            GUI.Label(new Rect(x, 70, 780, 40), "CITY OF ALBION, MICHIGAN  ·  " + open.kind.ToUpperInvariant(), small);
            GUI.Label(new Rect(x, 100, 780, 90), open.name, title);
            GUI.Label(new Rect(x, 205, 780, 300), open.summary, body);
            GUI.Label(new Rect(x, 505, 780, 48), "Source: " + open.source + "\nShown at game scale; the building and its surroundings are approximations.", small);
            if (GUI.Button(new Rect(x, 590, 230, 46), (focus == 0 ? "▶  " : "") + "Open source", button)) { focus = 0; OpenSource(); }
            if (GUI.Button(new Rect(x + 245, 590, 160, 46), (focus == 1 ? "▶  " : "") + "Close", button)) { focus = 1; ClosePanel(); }
            GUI.Label(new Rect(x, h - 65, 780, 28), "STICK Navigate   ·   TRIGGER Select   ·   MENU Back   ·   H / Esc Close", small);
        }

        // ── City life ─────────────────────────────────────────────────────────

        bool lampsLit;
        /// <summary>Lamp heads glow warm at dusk and stay dull in daylight; driven by <see cref="CampusEnvironment"/>.</summary>
        public void UpdateLamps(float daylight)
        {
            bool lit = daylight < .35f; if (lampHead == null || lit == lampsLit) return;
            lampsLit = lit; Color color = lit ? new Color(1f, .82f, .5f) : new Color(.55f, .55f, .5f);
            if (lampHead.HasProperty("_BaseColor")) lampHead.SetColor("_BaseColor", color);
            if (lampHead.HasProperty("_Color")) lampHead.SetColor("_Color", color);
            if (lampHead.HasProperty("_EmissionColor")) { lampHead.SetColor("_EmissionColor", lit ? color * 2.2f : Color.black); if (lit) lampHead.EnableKeyword("_EMISSION"); else lampHead.DisableKeyword("_EMISSION"); }
        }

        void BuildLamps()
        {
            lampPost = TowerGeometry.Material("City lamp post", new Color(.08f, .09f, .1f), .6f, .4f);
            lampHead = TowerGeometry.Material("City lamp glass", new Color(.55f, .55f, .5f), 0f, .6f);
            for (float z = -360f; z <= 360f; z += 24f)
            {
                if (Mathf.Abs(z - CityCatalog.RiverZ) < 22f || Mathf.Abs(z - CityCatalog.ErieZ) < 12f) continue;
                foreach (int side in new[] { -1, 1 })
                {
                    var post = KeeperAvatar.Part(root, "Street lamp post", PrimitiveType.Cylinder, ToWorld(side * 10.4f, 2.2f, z), new Vector3(.14f, 2.2f, .14f), lampPost, false);
                    KeeperAvatar.Part(root, "Street lamp", PrimitiveType.Sphere, ToWorld(side * 10.4f, 4.5f, z), new Vector3(.55f, .55f, .55f), lampHead, false);
                }
            }
        }

        // Pedestrians reuse the campus student agents, so they are mirrored from the LAN host like every other student.
        void BuildResidents()
        {
            if (game.world == null) return;
            var routes = new[]
            {
                new[] { new Vector3(8f, 0, 40f), new Vector3(8f, 0, 300f) },
                new[] { new Vector3(-8f, 0, 300f), new Vector3(-8f, 0, 40f) },
                new[] { new Vector3(-200f, 0, CityCatalog.ErieZ - 8f), new Vector3(-30f, 0, CityCatalog.ErieZ - 8f) },
                new[] { new Vector3(30f, 0, CityCatalog.ErieZ + 8f), new Vector3(200f, 0, CityCatalog.ErieZ + 8f) },
                new[] { new Vector3(-250f, 0, CityCatalog.RiverZ + 18f), new Vector3(-120f, 0, CityCatalog.RiverZ + 18f), new Vector3(60f, 0, CityCatalog.RiverZ + 18f) },
                new[] { new Vector3(-130f, 0, -230f), new Vector3(-170f, 0, -270f), new Vector3(-100f, 0, -270f) },
                new[] { new Vector3(140f, 0, -128f), new Vector3(168f, 0, -146f) },
                new[] { new Vector3(-110f, 0, 78f), new Vector3(-110f, 0, 124f) },
                new[] { new Vector3(-6f, 0, -42f), new Vector3(-6f, 0, -18f) },
            };
            for (int i = 0; i < routes.Length; i++)
            {
                for (int w = 0; w < routes[i].Length; w++) routes[i][w] = ToWorld(routes[i][w].x, .08f, routes[i][w].z);
                game.world.AddStudent(27 + i, routes[i], .95f + (i % 3) * .2f, "Albion city walk " + (i + 1));
            }
        }

        // Drivable town cars parked along the curbs. They join the campus car list (after the campus cruisers),
        // so the LAN replica mirrors them and joined players can drive them like any other car.
        void BuildTraffic()
        {
            if (game.campus == null) return;
            var spots = new[]
            {
                new Vector4(3.6f, 0, 200f, 0f), new Vector4(-3.6f, 0, 262f, 180f),
                new Vector4(-120f, 0, CityCatalog.ErieZ + 2.6f, 90f), new Vector4(120f, 0, CityCatalog.ErieZ - 2.6f, 270f),
            };
            for (int i = 0; i < spots.Length; i++)
            {
                var o = new GameObject("Albion town car " + (i + 1));
                o.transform.SetPositionAndRotation(ToWorld(spots[i].x, .08f, spots[i].z), Quaternion.Euler(0, spots[i].w, 0));
                var car = o.AddComponent<CampusCar>(); car.Build(KeeperAvatar.Coats[(i + 2) % KeeperAvatar.Coats.Length]); game.campus.cars.Add(car);
            }
        }

        // ── World building ────────────────────────────────────────────────────

        void BuildMaterials()
        {
            brickRed = CraftModel.Surface("City brick red", Color.white, "red_brick_03"); brickWarm = CraftModel.Surface("City brick warm", new Color(.92f, .8f, .68f), "red_brick_03");
            limestone = TowerGeometry.Material("City limestone", new Color(.78f, .72f, .59f)); slate = TowerGeometry.Material("City slate roof", new Color(.17f, .22f, .25f));
            asphalt = TowerGeometry.Material("City asphalt", new Color(.14f, .17f, .18f)); sidewalk = CraftModel.Surface("City sidewalk", Color.white, "concrete_pavement");
            lawn = CraftModel.Surface("City lawn", new Color(.66f, .78f, .6f), "aerial_grass_rock"); water = TowerGeometry.Material("Kalamazoo River water", new Color(.16f, .33f, .4f), .1f, .92f);
            glass = TowerGeometry.Material("City shop glass", new Color(.19f, .32f, .4f), .15f, .75f); gold = TowerGeometry.Material("City marquee gold", new Color(.94f, .65f, .15f), .3f, .6f);
            bronze = TowerGeometry.Material("City bronze plaque", new Color(.42f, .3f, .14f), .5f, .5f); hedge = TowerGeometry.Material("City hedge", new Color(.12f, .34f, .16f));
            trunk = TowerGeometry.Material("City trunk", new Color(.25f, .17f, .1f)); leaf = TowerGeometry.Material("City foliage", new Color(.2f, .42f, .2f));
            stoneDark = TowerGeometry.Material("City granite", new Color(.34f, .35f, .38f), .1f, .35f); awning = TowerGeometry.Material("City awning", new Color(.5f, .1f, .14f));
        }

        GameObject Box(string name, Vector3 local, Vector3 size, Material material, bool solid = true, Transform parent = null)
        {
            var o = KeeperAvatar.Part(parent != null ? parent : root, name, PrimitiveType.Cube, parent != null ? local : Origin + local, size, material, solid);
            return o;
        }

        GameObject Landmark(CityLandmark l)
        {
            var o = new GameObject(l.name); o.transform.SetParent(root, false); o.transform.position = ToWorld(l.x, 0f, l.z); landmarkRoots[l.id] = o;
            var tag = o.AddComponent<CampusWorldLabel>(); tag.Configure("H  " + l.name.ToUpperInvariant(), new Color(1f, .76f, .28f), new Vector3(0, l.height + 2.2f, 0), 60f);
            return o;
        }

        static CityLandmark Find(string id) { foreach (var l in CityCatalog.Landmarks) if (l.id == id) return l; return null; }

        void BuildGroundAndStreets()
        {
            // Joins the campus ground at its west edge (world x = -600) and shares Erie Street's row with the campus street.
            Box("City ground", new Vector3(0, -.3f, 0), new Vector3(CityCatalog.HalfWidth * 2, .5f, CityCatalog.HalfDepth * 2), lawn);
            Box("Erie Street", new Vector3(0, 0, CityCatalog.ErieZ), new Vector3(CityCatalog.HalfWidth * 2, .12f, 10f), asphalt);
            Box("Superior Street", new Vector3(CityCatalog.SuperiorX, 0, 0), new Vector3(12f, .12f, CityCatalog.HalfDepth * 2), asphalt);
            foreach (int side in new[] { -1, 1 })
            {
                Box("Superior sidewalk", new Vector3(side * 8f, .02f, 0), new Vector3(4f, .1f, CityCatalog.HalfDepth * 2), sidewalk, false);
                Box("Erie sidewalk", new Vector3(0, .02f, CityCatalog.ErieZ + side * 7f), new Vector3(CityCatalog.HalfWidth * 2, .1f, 4f), sidewalk, false);
            }
            for (float z = -360f; z < 360f; z += 12f) Box("Superior centre line", new Vector3(0, .07f, z), new Vector3(.12f, .02f, 4f), gold, false);
        }

        void BuildRiver()
        {
            float z = CityCatalog.RiverZ, half = CityCatalog.RiverWidth / 2;
            Box("Kalamazoo River", new Vector3(0, -.23f, z), new Vector3(CityCatalog.HalfWidth * 2, .5f, CityCatalog.RiverWidth), water, false);
            foreach (int bank in new[] { -1, 1 }) Box("River bank stone", new Vector3(0, .1f, z + bank * (half + .4f)), new Vector3(CityCatalog.HalfWidth * 2, .5f, .8f), stoneDark, false);
            // The river is crossed only by the Superior Street bridge; everywhere else an unseen barrier keeps players on the banks.
            foreach (int side in new[] { -1, 1 })
            {
                float length = CityCatalog.HalfWidth - CityCatalog.BridgeHalfGap; float cx = side * (CityCatalog.BridgeHalfGap + length / 2);
                var barrier = new GameObject("River barrier"); barrier.transform.SetParent(root, false); barrier.transform.position = ToWorld(cx, 1.5f, z);
                var collider = barrier.AddComponent<BoxCollider>(); collider.size = new Vector3(length, 3f, CityCatalog.RiverWidth - 2f);
            }
            foreach (int side in new[] { -1, 1 })
                Box("Bridge rail", new Vector3(side * 6.6f, .55f, z), new Vector3(.3f, 1.1f, CityCatalog.RiverWidth + 1.6f), stoneDark);
        }

        void BuildSuperiorStreet()
        {
            var random = new System.Random(1835);
            var district = Find("superior");
            // Two rows of storefronts facing Superior Street, from the district's south end to the city's northern edge.
            foreach (int side in new[] { -1, 1 })
            {
                float z = 30f;
                while (z < CityCatalog.HalfDepth - 16f)
                {
                    float length = 11f + (float)random.NextDouble() * 7f, height = 6.5f + (float)random.NextDouble() * 4.5f, depth = 14f;
                    float x = side * (10f + depth / 2 + 2f), cz = z + length / 2;
                    var mat = random.Next(3) == 0 ? limestone : (random.Next(2) == 0 ? brickRed : brickWarm);
                    Box("Storefront", new Vector3(x, height / 2, cz), new Vector3(depth, height, length - .4f), mat);
                    float faceX = x - side * (depth / 2 + .03f);
                    Box("Shop window", new Vector3(faceX, 1.9f, cz), new Vector3(.08f, 2.1f, length - 2.4f), glass, false);
                    Box("Upper windows", new Vector3(faceX, height - 2f, cz), new Vector3(.08f, 1.4f, length - 3f), glass, false);
                    Box("Shop awning", new Vector3(faceX - side * .7f, 3.2f, cz), new Vector3(1.4f, .12f, length - 2f), awning, false);
                    Box("Cornice", new Vector3(faceX - side * .15f, height + .1f, cz), new Vector3(depth + .3f, .35f, length - .2f), limestone, false);
                    z += length;
                }
            }
            // Gateway arch at the south end of the district.
            foreach (int side in new[] { -1, 1 }) Box("Gateway pillar", new Vector3(side * 9f, 3.5f, district.z - district.depth / 2), new Vector3(1.2f, 7f, 1.2f), limestone);
            var beam = Box("Gateway beam", new Vector3(0, 7.2f, district.z - district.depth / 2), new Vector3(20f, 1.2f, 1.2f), limestone);
            var sign = new GameObject("Gateway sign"); sign.transform.SetParent(beam.transform, true); sign.transform.position = ToWorld(0, 7.2f, district.z - district.depth / 2 - .7f);
            sign.transform.rotation = Quaternion.Euler(0, 0f, 0);
            var text = sign.AddComponent<TextMesh>(); text.text = "DOWNTOWN ALBION"; text.fontSize = 64; text.characterSize = .1f; text.anchor = TextAnchor.MiddleCenter; text.color = new Color(.96f, .9f, .7f);
            Landmark(district);
        }

        void BuildBohm()
        {
            var l = Find("bohm"); var o = Landmark(l);
            Box("Bohm Theatre body", new Vector3(0, l.height / 2, 0), new Vector3(l.width, l.height, l.depth), brickRed, true, o.transform);
            Box("Bohm facade piers", new Vector3(-l.width / 2 - .05f, l.height / 2 + 1.2f, 0), new Vector3(.3f, l.height + 2.4f, l.depth * .72f), limestone, false, o.transform);
            Box("Bohm marquee", new Vector3(-l.width / 2 - 1.4f, 4.2f, 0), new Vector3(2.8f, .9f, 11f), gold, false, o.transform);
            Box("Bohm vertical sign", new Vector3(-l.width / 2 - .9f, 8.2f, -4.6f), new Vector3(.4f, 5.5f, 1.4f), gold, false, o.transform);
            Box("Bohm lobby doors", new Vector3(-l.width / 2 - .06f, 1.6f, 0), new Vector3(.12f, 3.1f, 7f), glass, false, o.transform);
        }

        void BuildGardner()
        {
            var l = Find("gardner"); var o = Landmark(l);
            Material cream = TowerGeometry.Material("Gardner House paint", new Color(.82f, .78f, .66f));
            Box("Gardner body", new Vector3(0, 5f, 0), new Vector3(l.width, 10f, l.depth), cream, true, o.transform);
            Box("Mansard lower", new Vector3(0, 11.2f, 0), new Vector3(l.width - .8f, 2.6f, l.depth - .8f), slate, false, o.transform);
            Box("Mansard upper", new Vector3(0, 13.2f, 0), new Vector3(l.width - 4f, 1.6f, l.depth - 4f), slate, false, o.transform);
            Box("Gardner porch", new Vector3(l.width / 2 + 1.6f, 1.6f, 0), new Vector3(3.2f, .4f, 9f), limestone, true, o.transform);
            for (int i = 0; i < 4; i++) Box("Porch column", new Vector3(l.width / 2 + 2.9f, 2.5f, -3.6f + i * 2.4f), new Vector3(.3f, 2.4f, .3f), limestone, false, o.transform);
            Box("Gardner windows", new Vector3(l.width / 2 + .04f, 6f, 0), new Vector3(.08f, 6.5f, l.depth - 3f), glass, false, o.transform);
        }

        void BuildRieger()
        {
            var l = Find("rieger"); var o = Landmark(l);
            Box("Rieger Park paths", new Vector3(0, .03f, 0), new Vector3(l.width - 4f, .08f, 2.2f), sidewalk, false, o.transform);
            Box("Rieger Park cross path", new Vector3(0, .03f, 0), new Vector3(2.2f, .08f, l.depth - 4f), sidewalk, false, o.transform);
            Box("Mother's Day marker", new Vector3(3.6f, 1.1f, 3.6f), new Vector3(1.4f, 2.2f, .5f), stoneDark, true, o.transform);
            Box("Marker plaque", new Vector3(3.6f, 1.4f, 3.33f), new Vector3(1f, .8f, .06f), bronze, false, o.transform);
            Box("Park bench", new Vector3(-5f, .45f, 3f), new Vector3(2.4f, .9f, .6f), trunk, true, o.transform);
        }

        void BuildVictory()
        {
            var l = Find("victory"); var o = Landmark(l);
            Box("Castle playground base", new Vector3(-14f, .3f, 6f), new Vector3(14f, .6f, 10f), limestone, true, o.transform);
            foreach (var t in new[] { new Vector2(-20f, 2f), new Vector2(-8f, 2f), new Vector2(-20f, 10f), new Vector2(-8f, 10f) })
            {
                Box("Castle tower", new Vector3(t.x, 3.4f, t.y), new Vector3(2.4f, 6f, 2.4f), limestone, true, o.transform);
                Box("Castle battlement", new Vector3(t.x, 6.5f, t.y), new Vector3(3f, .6f, 3f), stoneDark, false, o.transform);
            }
            Box("Castle wall", new Vector3(-14f, 2.2f, 2f), new Vector3(10f, 3.2f, .8f), limestone, true, o.transform);
            Box("Waterfall rise", new Vector3(14f, 1.6f, -4f), new Vector3(7f, 3.2f, 5f), stoneDark, true, o.transform);
            Box("Waterfall", new Vector3(14f, 1.7f, -1.4f), new Vector3(4f, 3f, .3f), water, false, o.transform);
            Box("Spring pool", new Vector3(14f, .05f, 3f), new Vector3(6f, .1f, 4f), water, false, o.transform);
            for (int gx = 0; gx < 3; gx++) for (int gz = 0; gz < 2; gz++)
                Box("Formal garden bed", new Vector3(-2f + gx * 4.2f, .35f, -12f + gz * 4.2f), new Vector3(3.2f, .7f, 3.2f), hedge, true, o.transform);
        }

        void BuildRiverTrail()
        {
            var l = Find("river"); var o = Landmark(l);
            Box("Albion River Trail", new Vector3(0, .06f, CityCatalog.RiverZ + CityCatalog.RiverWidth / 2 + 5f), new Vector3(CityCatalog.HalfWidth * 2, .08f, 3f), sidewalk, false);
            Box("Trail sign post", new Vector3(0, 1.1f, 0), new Vector3(.25f, 2.2f, .25f), trunk, true, o.transform);
            Box("Trail sign board", new Vector3(0, 2.1f, 0), new Vector3(2.8f, .8f, .12f), limestone, false, o.transform);
            Box("Trail bench", new Vector3(5f, .45f, 0), new Vector3(2.4f, .9f, .6f), trunk, true, o.transform);
        }

        void BuildRiverside()
        {
            var l = Find("riverside"); var o = Landmark(l);
            for (int terrace = 0; terrace < 4; terrace++)
                Box("Cemetery terrace", new Vector3(0, .3f + terrace * .6f, l.depth / 2 - 6f - terrace * 9f), new Vector3(l.width, .6f + terrace * 1.2f, 8f), lawn, true, o.transform);
            var random = new System.Random(77);
            for (int terrace = 0; terrace < 4; terrace++) for (int i = 0; i < 9; i++)
            {
                float height = .7f + (float)random.NextDouble() * .5f;
                Box("Headstone", new Vector3(-22f + i * 5.4f, 1.2f * terrace + 1.2f + height / 2, l.depth / 2 - 6f - terrace * 9f + 1f), new Vector3(.8f, height, .25f), stoneDark, false, o.transform);
            }
            foreach (int side in new[] { -1, 1 }) Box("Cemetery gate pier", new Vector3(side * 3f, 1.6f, -l.depth / 2 - 1f), new Vector3(1f, 3.2f, 1f), stoneDark, true, o.transform);
            Box("Cemetery gate beam", new Vector3(0, 3.3f, -l.depth / 2 - 1f), new Vector3(7f, .3f, .3f), stoneDark, false, o.transform);
        }

        void BuildTrees()
        {
            var random = new System.Random(1877);
            for (int i = 0; i < 90; i++)
            {
                float x = (float)(random.NextDouble() * 2 - 1) * (CityCatalog.HalfWidth - 10f), z = (float)(random.NextDouble() * 2 - 1) * (CityCatalog.HalfDepth - 10f);
                if (Mathf.Abs(x) < 26f || Mathf.Abs(z - CityCatalog.ErieZ) < 10f || Mathf.Abs(z - CityCatalog.RiverZ) < 24f) continue;
                bool blocked = false; foreach (var l in CityCatalog.Landmarks) if (Mathf.Abs(x - l.x) < l.width / 2 + 10f && Mathf.Abs(z - l.z) < l.depth / 2 + 10f) { blocked = true; break; }
                if (!blocked) Tree(x, z, 5.5f + (float)random.NextDouble() * 3f);
            }
        }

        void Tree(float x, float z, float height)
        {
            var t = KeeperAvatar.Part(root, "City tree trunk", PrimitiveType.Cylinder, ToWorld(x, height * .3f, z), new Vector3(.5f, height * .3f, .5f), trunk, true);
            KeeperAvatar.Part(root, "City tree canopy", PrimitiveType.Sphere, ToWorld(x, height * .75f, z), new Vector3(height * .6f, height * .55f, height * .6f), leaf, false);
        }
    }
}
