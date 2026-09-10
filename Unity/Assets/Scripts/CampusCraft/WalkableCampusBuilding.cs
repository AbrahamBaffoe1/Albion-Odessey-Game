using System.Collections.Generic;
using UnityEngine;

namespace AlbionOdyssey
{
    // Reference-driven building shell for halls whose public interior dimensions are
    // still being verified. All dimensions are explicit meters and easy to replace.
    public sealed class WalkableCampusBuilding
    {
        public readonly string Id;
        public readonly CampusPlace Place;
        public readonly Vector3 Origin;
        public readonly float Width, Depth, Floors, FloorHeight;
        public readonly string History;
        public GameObject Root { get; private set; }
        public readonly List<CampusDoor> Doors = new List<CampusDoor>();
        public bool Inside
        {
            get
            {
                if (game == null) return false;
                Vector3 p = game.player.transform.position - Origin;
                return Mathf.Abs(p.x) < Width * .5f && Mathf.Abs(p.z) < Depth * .5f && p.y >= -.2f && p.y < Floors * FloorHeight + 1f;
            }
        }
        public string Location => Inside ? Place.name + " · Level " + (Mathf.Clamp(Mathf.FloorToInt((game.player.transform.position.y + .15f) / FloorHeight), 0, (int)Floors - 1) + 1) : Place.name;

        readonly OdysseyGame game;
        readonly bool atrium;
        readonly Material brick, stone, trim, glass, roof, floor, plaster, wood, fabric, metal;

        public WalkableCampusBuilding(OdysseyGame owner, CampusPlace place, float width, float depth, int floors, float floorHeight, bool centralAtrium, string history)
        {
            game = owner; Place = place; Id = place.id; Origin = place.position; Width = width; Depth = depth; Floors = floors; FloorHeight = floorHeight; atrium = centralAtrium; History = history;
            brick = CraftModel.Surface(place.name + " brick", new Color(.58f, .25f, .17f), "red_brick_03");
            stone = CraftModel.Surface(place.name + " limestone", new Color(.78f, .74f, .66f));
            trim = CraftModel.Surface(place.name + " painted trim", new Color(.88f, .87f, .81f));
            glass = CraftModel.Surface(place.name + " glass", new Color(.13f, .25f, .30f), "", .6f); glass.SetFloat("_Metallic", .32f);
            roof = CraftModel.Surface(place.name + " slate", new Color(.13f, .16f, .18f));
            floor = CraftModel.Surface(place.name + " floor", new Color(.68f, .66f, .59f), "concrete_pavement");
            plaster = CraftModel.Surface(place.name + " interior plaster", new Color(.84f, .81f, .73f));
            wood = CraftModel.Surface(place.name + " oak", new Color(.36f, .18f, .08f));
            fabric = CraftModel.Surface(place.name + " upholstery", new Color(.23f, .24f, .28f));
            metal = CraftModel.Surface(place.name + " metal", new Color(.08f, .09f, .10f));
            Build();
        }

        static GameObject Box(Transform parent, string name, Vector3 at, Vector3 size, Material material, bool solid = true) => KeeperAvatar.Part(parent, name, PrimitiveType.Cube, at, size, material, solid);

        void Build()
        {
            Root = new GameObject(Id + " · " + Place.name + " · walkable"); Root.transform.position = Origin;
            if (Id == "16") BuildAuthoredRobinson();
            else { BuildExterior(); BuildInterior(); }
            var plaque = new GameObject(Place.name + " history plaque"); plaque.transform.SetParent(Root.transform, false); plaque.transform.localPosition = new Vector3(0, 2.15f, -Depth * .5f - .08f);
            var label = plaque.AddComponent<TextMesh>(); label.text = Place.name.ToUpperInvariant(); label.fontSize = 52; label.characterSize = .09f; label.anchor = TextAnchor.MiddleCenter; label.color = new Color(.22f, .13f, .30f);
            var info = plaque.AddComponent<CampusBuildingInfo>(); info.Title = Place.name; info.Body = History;
            Physics.SyncTransforms();
        }

        void BuildAuthoredRobinson()
        {
            // Robinson is the first additional hall with a dedicated Blender
            // export. Keep the existing building contract so map travel,
            // doors, history and activity systems remain compatible.
            var text = Resources.Load<TextAsset>("CampusCraft/robinson");
            if (text == null) { BuildExterior(); BuildInterior(); return; }
            var description = JsonUtility.FromJson<CraftDescription>(text.text);
            foreach (var section in description.sections)
                CraftModel.Load("robinson-" + section.name, section, description.materials, Root.transform);
            Door(new Vector3(0, 0, -Depth * .5f - .32f), 2.50f, 2.75f, "Robinson main entrance");
            for (int f = 0; f < 4; f++)
            {
                float y = f * FloorHeight;
                Door(new Vector3(0, y, -2.35f), 1.25f, 2.35f, "Robinson seminar door");
                Door(new Vector3(0, y, 2.35f), 1.25f, 2.35f, "Robinson seminar door");
                Sign(new Vector3(-9.2f, y + 2.35f, -5.85f), f == 0 ? "QUAD ATRIUM · SEMINARS" : "ACADEMIC OFFICES · LEVEL " + (f + 1));
                Light(new Vector3(-9.2f, y + 3.08f, 0));
            }
            Physics.SyncTransforms();
        }

        void BuildExterior()
        {
            if (Id == "18") { BuildScienceExterior(); return; }
            float roofY = Floors * FloorHeight;
            Box(Root.transform, "Foundation", new Vector3(0, .15f, 0), new Vector3(Width + .45f, .30f, Depth + .45f), stone);
            Box(Root.transform, "Back wall", new Vector3(0, roofY * .5f, Depth * .5f), new Vector3(Width, roofY, .32f), brick);
            Box(Root.transform, "Left wall", new Vector3(-Width * .5f, roofY * .5f, 0), new Vector3(.32f, roofY, Depth), brick);
            Box(Root.transform, "Right wall", new Vector3(Width * .5f, roofY * .5f, 0), new Vector3(.32f, roofY, Depth), brick);
            Box(Root.transform, "Front left wing", new Vector3(-Width * .25f, roofY * .5f, -Depth * .5f), new Vector3(Width * .5f - 2.1f, roofY, .32f), brick);
            Box(Root.transform, "Front right wing", new Vector3(Width * .25f, roofY * .5f, -Depth * .5f), new Vector3(Width * .5f - 2.1f, roofY, .32f), brick);
            Box(Root.transform, "Cornice", new Vector3(0, roofY - .25f, 0), new Vector3(Width + .5f, .45f, Depth + .45f), stone, false);
            Box(Root.transform, "Roof", new Vector3(0, roofY + .25f, 0), new Vector3(Width + .8f, .5f, Depth + .8f), roof, false);
            int windowsPerSide = Mathf.Max(2, Mathf.FloorToInt(Width / 3.2f));
            for (int f = 0; f < Floors; f++)
            {
                float y = 1.65f + f * FloorHeight;
                for (int i = 0; i < windowsPerSide; i++)
                {
                    float x = -Width * .5f + 1.7f + i * ((Width - 3.4f) / Mathf.Max(1, windowsPerSide - 1));
                    Window(new Vector3(x, y, -Depth * .5f - .18f), new Vector3(1.12f, .08f, 1.35f)); Window(new Vector3(x, y, Depth * .5f + .18f), new Vector3(1.12f, .08f, 1.35f));
                }
            }
            Box(Root.transform, "Entrance surround", new Vector3(0, 1.55f, -Depth * .5f - .20f), new Vector3(4.5f, 3.1f, .45f), trim, false);
            Box(Root.transform, "Entrance canopy", new Vector3(0, 3.35f, -Depth * .5f - 1.0f), new Vector3(5.3f, .22f, 1.8f), stone, false);
            Door(new Vector3(0, 0, -Depth * .5f - .28f), 2.45f, 2.75f, "Main entrance");
            Box(Root.transform, "Approach walk", new Vector3(0, .04f, -Depth * .5f - 3.5f), new Vector3(4.5f, .08f, 7f), floor);
            if (Id == "16")
            {
                for (int x = -1; x <= 1; x += 2) Box(Root.transform, "Robinson portico column", new Vector3(x * 2.0f, 1.65f, -Depth * .5f - .7f), new Vector3(.42f, 3.3f, .52f), stone, true);
                Box(Root.transform, "Robinson pediment", new Vector3(0, 4.0f, -Depth * .5f - .75f), new Vector3(5.2f, .35f, 1.5f), stone, false);
                Sign(new Vector3(0, 2.45f, -Depth * .5f - .48f), "ROBINSON HALL · QUADRANGLE");
                for (int side = -1; side <= 1; side += 2) Box(Root.transform, "Robinson entry lantern", new Vector3(side * 2.65f, 2.35f, -Depth * .5f - .55f), new Vector3(.28f, .55f, .18f), roof, false);
                Dormer(new Vector3(-Width * .27f, roofY + .65f, -Depth * .18f)); Dormer(new Vector3(Width * .27f, roofY + .65f, -Depth * .18f));
            }
            else if (Id == "1")
            {
                Box(Root.transform, "Bonta glazed lobby", new Vector3(0, 1.65f, -Depth * .5f - .28f), new Vector3(Width * .46f, 2.9f, .12f), glass, false);
                Box(Root.transform, "Bonta roof hip", new Vector3(0, roofY + .55f, 0), new Vector3(Width + 1.2f, .65f, Depth + 1.2f), roof, false);
                Sign(new Vector3(0, 2.55f, -Depth * .5f - .43f), "BONTA ADMISSION CENTER");
                Box(Root.transform, "Bonta visitor ramp", new Vector3(-Width * .30f, .12f, -Depth * .5f - 1.6f), new Vector3(2.2f, .14f, 3.2f), floor, true);
            }
            else
            {
                Box(Root.transform, "Entry glazing", new Vector3(0, 1.65f, -Depth * .5f - .28f), new Vector3(Width * .46f, 2.9f, .12f), glass, false);
                Sign(new Vector3(0, 2.55f, -Depth * .5f - .43f), Place.name.ToUpperInvariant());
                if (Place.shape == "chapel")
                {
                    for (int side = -1; side <= 1; side += 2) Box(Root.transform, "Chapel entry column", new Vector3(side * 2.0f, 1.65f, -Depth * .5f - .7f), new Vector3(.42f, 3.3f, .52f), stone, true);
                    Box(Root.transform, "Chapel entry pediment", new Vector3(0, 4.0f, -Depth * .5f - .75f), new Vector3(5.2f, .35f, 1.5f), stone, false);
                }
                else if (Place.shape == "gym" || Place.shape == "theatre")
                    Box(Root.transform, "Public entry canopy", new Vector3(0, 3.35f, -Depth * .5f - 1.0f), new Vector3(7.0f, .22f, 1.8f), stone, false);
                if (Place.shape == "house")
                    foreach (int side in new[] { -1, 1 }) { var slope = Box(Root.transform, "Pitched residence roof", new Vector3(side * Width * .25f, roofY + Width * .13f, 0), new Vector3(Width * .57f, .45f, Depth + 1f), roof, false); slope.transform.localRotation = Quaternion.Euler(0, 0, -side * 25f); }
                if (Place.shape == "tower") Box(Root.transform, "Tower roof cap", new Vector3(0, roofY + .55f, 0), new Vector3(Width + 1.0f, .7f, Depth + 1.0f), roof, false);
            }
        }

        void BuildScienceExterior()
        {
            float roofY = Floors * FloorHeight;
            Box(Root.transform, "Science Complex foundation", new Vector3(0, .15f, 0), new Vector3(Width + .6f, .3f, Depth + .6f), stone);
            // A cross-shaped four-wing mass leaves a real glazed atrium in the centre.
            Wing("Kresge teaching wing", new Vector3(-Width * .34f, roofY * .5f, 0), new Vector3(Width * .32f, roofY, Depth * .78f), "KRESGE");
            Wing("Norris lecture wing", new Vector3(Width * .34f, roofY * .5f, 0), new Vector3(Width * .32f, roofY, Depth * .78f), "NORRIS");
            Wing("Palenske research wing", new Vector3(0, roofY * .5f, Depth * .34f), new Vector3(Width * .58f, roofY, Depth * .32f), "PALENSKE");
            Wing("Putnam collections wing", new Vector3(0, roofY * .5f, -Depth * .34f), new Vector3(Width * .58f, roofY, Depth * .32f), "PUTNAM");
            Box(Root.transform, "Science atrium floor", new Vector3(0, .28f, 0), new Vector3(10.4f, .16f, 10.4f), floor);
            Box(Root.transform, "Atrium east glass", new Vector3(5.25f, roofY * .5f, 0), new Vector3(.12f, roofY - .4f, 10.8f), glass, false);
            Box(Root.transform, "Atrium west glass", new Vector3(-5.25f, roofY * .5f, 0), new Vector3(.12f, roofY - .4f, 10.8f), glass, false);
            Box(Root.transform, "Atrium north glass", new Vector3(0, roofY * .5f, 5.25f), new Vector3(10.8f, roofY - .4f, .12f), glass, false);
            Box(Root.transform, "Atrium south glass", new Vector3(0, roofY * .5f, -5.25f), new Vector3(10.8f, roofY - .4f, .12f), glass, false);
            Box(Root.transform, "Science main canopy", new Vector3(0, 3.55f, -Depth * .5f - .9f), new Vector3(7.2f, .24f, 2.2f), stone, false);
            Door(new Vector3(0, 0, -Depth * .5f - .28f), 2.8f, 2.8f, "Science Complex main entrance");
            Box(Root.transform, "Science approach walk", new Vector3(0, .04f, -Depth * .5f - 3.5f), new Vector3(5.4f, .08f, 7f), floor);
            Sign(new Vector3(0, 2.2f, -Depth * .5f - .34f), "SCIENCE COMPLEX");
            Sign(new Vector3(0, roofY + .55f, -Depth * .5f - .16f), "KRESGE · NORRIS · PALENSKE · PUTNAM");
        }

        void Wing(string name, Vector3 center, Vector3 size, string label)
        {
            Box(Root.transform, name + " facade", center, size, brick);
            Box(Root.transform, name + " limestone base", center + Vector3.down * (size.y * .5f - .28f), new Vector3(size.x + .2f, .45f, size.z + .2f), stone, false);
            Box(Root.transform, name + " cornice", center + Vector3.up * (size.y * .5f - .22f), new Vector3(size.x + .35f, .44f, size.z + .35f), trim, false);
            Box(Root.transform, name + " roof", center + Vector3.up * (size.y * .5f + .16f), new Vector3(size.x + .5f, .36f, size.z + .5f), roof, false);
            int floors = Mathf.Max(1, Mathf.FloorToInt(size.y / FloorHeight));
            for (int f = 0; f < floors; f++)
            {
                float y = center.y - size.y * .5f + 1.65f + f * FloorHeight;
                int frontWindows = Mathf.Max(2, Mathf.FloorToInt(size.x / 3.1f));
                for (int i = 0; i < frontWindows; i++)
                {
                    float x = center.x - size.x * .5f + 1.6f + i * ((size.x - 3.2f) / Mathf.Max(1, frontWindows - 1));
                    Window(new Vector3(x, y, center.z - size.z * .5f - .18f), new Vector3(1.05f, .08f, 1.22f));
                    Window(new Vector3(x, y, center.z + size.z * .5f + .18f), new Vector3(1.05f, .08f, 1.22f));
                }
            }
            Sign(new Vector3(center.x, 2.18f, center.z - size.z * .5f - .25f), label);
        }

        void Window(Vector3 at, Vector3 size)
        {
            Box(Root.transform, "Reference window", at, size, glass, false); Box(Root.transform, "Window trim", at, size + new Vector3(.16f, .05f, .16f), trim, false);
        }

        void Dormer(Vector3 at)
        {
            Box(Root.transform, "Robinson dormer", at, new Vector3(1.8f, 1.4f, 1.1f), trim, false); Box(Root.transform, "Robinson dormer window", at + Vector3.back * .58f, new Vector3(.9f, .7f, .08f), glass, false);
        }

        void BuildInterior()
        {
            if (Id == "18") { BuildScienceInterior(); return; }
            for (int f = 0; f < Floors; f++)
            {
                float y = f * FloorHeight;
                if (f == 0 || !atrium || f >= Floors)
                    Box(Root.transform, "Interior floor", new Vector3(0, y - .10f, 0), new Vector3(Width - .7f, .2f, Depth - .7f), floor);
                else
                {
                    // Leave a real opening above the stair flight; a full plate here would
                    // make the upper floor look correct but physically stop the player.
                    Box(Root.transform, "Interior floor left of stair", new Vector3(-2.7f, y - .10f, 0), new Vector3(21.2f, .2f, Depth - .7f), floor);
                    Box(Root.transform, "Interior floor right of stair", new Vector3(11.9f, y - .10f, 0), new Vector3(3.0f, .2f, Depth - .7f), floor);
                }
                if (atrium)
                {
                    Box(Root.transform, "Balcony left rail", new Vector3(-Width * .23f, y + 1.05f, 0), new Vector3(.08f, 1.05f, Depth - 2f), trim, true); Box(Root.transform, "Balcony right rail", new Vector3(Width * .23f, y + 1.05f, 0), new Vector3(.08f, 1.05f, Depth - 2f), trim, true);
                }
                else if (Id == "1")
                {
                    Box(Root.transform, "Bonta reception wall", new Vector3(0, y + 1.55f, 0), new Vector3(.15f, 3.1f, Depth - 3f), plaster, true); if (f == 0) Box(Root.transform, "Visitor lounge divider", new Vector3(Width * .25f, y + 1.55f, 1.5f), new Vector3(Width * .45f, 3.1f, .15f), plaster, true);
                }
                else if (Place.shape != "chapel" && Place.shape != "gym" && Place.category != "Greek life")
                    Box(Root.transform, "Building corridor divider", new Vector3(0, y + 1.55f, 0), new Vector3(.15f, 3.1f, Depth - 3f), plaster, true);
                string sign = Id == "1" ? (f == 0 ? "ADMISSIONS LOBBY" : "VISITOR SERVICES") : Id == "18" ? (f == 0 ? "SCIENCE ON DISPLAY" : f == 1 ? "TEACHING LABORATORIES" : f == 2 ? "RESEARCH LABORATORIES" : "COLLECTIONS & OBSERVATION") : Id == "16" ? (f == 0 ? "QUAD ATRIUM" : "ACADEMIC OFFICES") : Place.name.ToUpperInvariant() + (f == 0 ? " · COMMONS" : " · ROOMS & STUDY");
                Sign(new Vector3(-Width * .25f, y + 2.35f, -Depth * .5f + .25f), sign);
                for (int x = -1; x <= 1; x += 2)
                {
                    float roomX = x * Width * .31f; Box(Root.transform, "Room partition", new Vector3(roomX, y + 1.55f, Depth * .24f), new Vector3(.14f, 3.1f, Depth * .45f), plaster, true); Door(new Vector3(roomX, y, Depth * .02f), 1.25f, 2.35f, f == 0 ? (Id == "1" ? "Admissions office" : Id == "18" ? "Teaching lab" : "Room entrance") : Id == "18" ? "Laboratory door" : "Office door"); Desk(new Vector3(roomX, y, Depth * .34f));
                    if (Id == "18") LabBench(new Vector3(roomX, y, -Depth * .28f));
                }
                if (Id == "1" && f == 0) VisitorFurniture(y);
                else if (Id == "16") AcademicFurniture(y);
                else if (Place.shape != "chapel") GenericFurniture(y);
                Light(new Vector3(0, y + 3.05f, 0)); if (f < Floors - 1) Stair(f);
            }
        }

        void BuildScienceInterior()
        {
            const float atriumHalf = 5.05f;
            for (int f = 0; f < Floors; f++)
            {
                float y = f * FloorHeight;
                if (f == 0)
                    Box(Root.transform, "Science ground floor", new Vector3(0, y - .1f, 0), new Vector3(Width - .7f, .2f, Depth - .7f), floor);
                else
                {
                    Box(Root.transform, "Science west floor plate", new Vector3(-(Width + atriumHalf) * .25f, y - .1f, 0), new Vector3((Width - atriumHalf) * .5f, .2f, Depth - .7f), floor);
                    Box(Root.transform, "Science east floor plate", new Vector3((Width + atriumHalf) * .25f, y - .1f, 0), new Vector3((Width - atriumHalf) * .5f, .2f, Depth - .7f), floor);
                    Box(Root.transform, "Science north floor plate", new Vector3(0, y - .1f, (Depth + atriumHalf) * .25f), new Vector3(atriumHalf * 2f, .2f, (Depth - atriumHalf) * .5f), floor);
                    Box(Root.transform, "Science south floor plate", new Vector3(0, y - .1f, -(Depth + atriumHalf) * .25f), new Vector3(atriumHalf * 2f, .2f, (Depth - atriumHalf) * .5f), floor);
                }
                // Guard rails make the atrium edge readable and block accidental falls.
                if (f > 0)
                {
                    Box(Root.transform, "Atrium west rail", new Vector3(-atriumHalf, y + 1.05f, 0), new Vector3(.08f, 1.05f, atriumHalf * 2f), trim, true);
                    Box(Root.transform, "Atrium east rail", new Vector3(atriumHalf, y + 1.05f, 0), new Vector3(.08f, 1.05f, atriumHalf * 2f), trim, true);
                    Box(Root.transform, "Atrium north rail", new Vector3(0, y + 1.05f, atriumHalf), new Vector3(atriumHalf * 2f, 1.05f, .08f), trim, true);
                    Box(Root.transform, "Atrium south rail", new Vector3(0, y + 1.05f, -atriumHalf), new Vector3(atriumHalf * 2f, 1.05f, .08f), trim, true);
                }
                string levelSign = f == 0 ? "SCIENCE ON DISPLAY" : f == 1 ? "TEACHING LABORATORIES" : f == 2 ? "RESEARCH LABORATORIES" : "COLLECTIONS & OBSERVATION";
                Sign(new Vector3(-Width * .33f, y + 2.35f, -Depth * .5f + .25f), levelSign);
                LabRoom(new Vector3(-Width * .31f, y, 0), "Kresge teaching lab", f < 2);
                LabRoom(new Vector3(Width * .31f, y, 0), "Norris lecture / study room", f == 0);
                LabRoom(new Vector3(0, y, Depth * .34f), "Palenske research lab", f >= 2);
                LabRoom(new Vector3(0, y, -Depth * .34f), "Putnam collections room", f != 2);
                Door(new Vector3(-Width * .31f, y, -Depth * .5f + 1.8f), 1.25f, 2.35f, "Science room door");
                Door(new Vector3(Width * .31f, y, -Depth * .5f + 1.8f), 1.25f, 2.35f, "Science room door");
                if (f == 0) { Door(new Vector3(-2.1f, y, -Depth * .5f - .28f), 1.2f, 2.35f, "Accessible side entrance"); Sign(new Vector3(0, y + 2.1f, -Depth * .5f + .35f), "ELEVATOR · RESTROOMS · INFO"); }
                Light(new Vector3(0, y + 3.05f, 0));
                ScienceStair(f);
            }
        }

        void LabRoom(Vector3 at, string label, bool bench)
        {
            Sign(at + new Vector3(0, 2.55f, -2.3f), label.ToUpperInvariant());
            if (!bench) { Desk(at + new Vector3(0, 0, .35f)); return; }
            LabBench(at + new Vector3(0, 0, .35f)); LabBench(at + new Vector3(0, 0, -1.35f));
        }

        void ScienceStair(int floorIndex)
        {
            float y = floorIndex * FloorHeight;
            for (int step = 0; step < 20; step++) Box(Root.transform, "Science stair tread", new Vector3(Width * .27f, y + (step + 1) * .17f - .085f, -Depth * .12f + step * .28f), new Vector3(2.2f, .17f, .28f), stone, true);
            for (int side = -1; side <= 1; side += 2) Box(Root.transform, "Science stair rail", new Vector3(Width * .27f + side * 1.15f, y + 1.55f, Depth * .12f), new Vector3(.06f, .06f, Depth * .48f), wood, false);
        }

        void Desk(Vector3 at)
        {
            Box(Root.transform, "Room desk", at + Vector3.up * .72f, new Vector3(1.9f, .10f, .8f), wood, true); Box(Root.transform, "Room monitor", at + new Vector3(0, 1.12f, .28f), new Vector3(.55f, .38f, .08f), metal, false); Box(Root.transform, "Room chair", at + new Vector3(0, .45f, -.78f), new Vector3(.55f, .12f, .55f), fabric, true);
        }

        void LabBench(Vector3 at)
        {
            Box(Root.transform, "Science lab bench", at + Vector3.up * .88f, new Vector3(2.4f, .12f, .72f), wood, true);
            Box(Root.transform, "Science bench cabinet", at + Vector3.up * .42f, new Vector3(2.2f, .72f, .62f), plaster, true);
            Box(Root.transform, "Science display case", at + new Vector3(0, 1.28f, .22f), new Vector3(.72f, .48f, .12f), glass, false);
        }

        void VisitorFurniture(float y)
        {
            Box(Root.transform, "Bonta reception desk", new Vector3(-Width * .22f, y + .72f, Depth * .30f), new Vector3(3.8f, 1.35f, .75f), wood, true);
            for (int i = -1; i <= 1; i++) Box(Root.transform, "Bonta visitor chair", new Vector3(i * 1.45f, y + .48f, Depth * .08f), new Vector3(.65f, .18f, .65f), fabric, true);
            Sign(new Vector3(-Width * .22f, y + 2.25f, Depth * .30f - .42f), "VISITOR CHECK-IN");
        }

        void AcademicFurniture(float y)
        {
            for (int i = -1; i <= 1; i++)
            {
                Box(Root.transform, "Robinson study table", new Vector3(i * 3.1f, y + .72f, Depth * .30f), new Vector3(2.1f, .10f, .75f), wood, true);
                Box(Root.transform, "Robinson study chair", new Vector3(i * 3.1f, y + .45f, Depth * .30f - .72f), new Vector3(.55f, .12f, .48f), fabric, true);
            }
        }

        void GenericFurniture(float y)
        {
            int seats = Mathf.Clamp(Mathf.FloorToInt(Width / 5f), 2, 6);
            for (int i = 0; i < seats; i++)
            {
                float x = (i - (seats - 1) * .5f) * Mathf.Min(4.0f, Width / Mathf.Max(2, seats));
                Box(Root.transform, "Common room table", new Vector3(x, y + .72f, Depth * .30f), new Vector3(1.7f, .10f, .7f), wood, true);
                Box(Root.transform, "Common room chair", new Vector3(x, y + .45f, Depth * .30f - .7f), new Vector3(.55f, .12f, .48f), fabric, true);
            }
        }

        void Stair(int floorIndex)
        {
            float y = floorIndex * FloorHeight; for (int step = 0; step < 20; step++) Box(Root.transform, "Walkable stair tread", new Vector3(Width * .33f, y + (step + 1) * .17f - .085f, -Depth * .28f + step * .30f), new Vector3(2.4f, .17f, .30f), stone, true); Box(Root.transform, "Stair upper landing", new Vector3(Width * .33f, y + FloorHeight - .10f, Depth * .35f), new Vector3(2.8f, .20f, 2.0f), floor, true); for (int side = -1; side <= 1; side += 2) Box(Root.transform, "Stair handrail", new Vector3(Width * .33f + side * 1.25f, y + 1.55f, 0), new Vector3(.06f, .06f, Depth * .62f), wood, false);
        }

        void Light(Vector3 at)
        {
            var o = new GameObject("Building ceiling light"); o.transform.SetParent(Root.transform, false); o.transform.localPosition = at; var l = o.AddComponent<Light>(); l.type = LightType.Point; l.range = 8; l.intensity = 1.4f; l.color = new Color(1f, .91f, .76f); l.shadows = LightShadows.None;
        }

        void Sign(Vector3 at, string text)
        {
            var o = new GameObject(text + " sign"); o.transform.SetParent(Root.transform, false); o.transform.localPosition = at; var label = o.AddComponent<TextMesh>(); label.text = text; label.fontSize = 44; label.characterSize = .06f; label.anchor = TextAnchor.MiddleCenter; label.color = new Color(.22f, .13f, .30f);
        }

        void Door(Vector3 at, float width, float height, string label)
        {
            var o = new GameObject(label); o.transform.SetParent(Root.transform, false); o.transform.localPosition = at; var door = o.AddComponent<CampusDoor>(); door.Label = label; door.Build(width, height, glass); Doors.Add(door);
        }

        public void Visit()
        {
            game.tour.StopMedia(); if (game.building) game.ToggleMode(); if (!game.player.TryExitVehicle()) return; game.player.Teleport(Origin + new Vector3(0, .08f, -Depth * .5f - 3.0f)); game.player.transform.rotation = Quaternion.identity; game.life.SetPanel(""); game.notice = Place.name + " · enter the reference-based interior. Press E at doors and H for the building story.";
        }

        public bool HandleInput()
        {
            if (game.life.PanelOpen || game.building || game.journalOpen) return false;
            CampusDoor nearby = NearbyDoor(); if (Input.GetKeyDown(KeyCode.E) && nearby != null) { nearby.Toggle(game.player); return true; }
            if (Input.GetKeyDown(KeyCode.H) && Inside)
            {
                TourPlace story = game.tour.catalog.ForCampus(Id);
                if (story == null && Id == "18") story = game.tour.catalog.ForCampus("18n");
                game.tour.OpenStory(story); return true;
            }
            return false;
        }

        public CampusDoor NearbyDoor()
        {
            CampusDoor best = null; float distance = 2.7f; foreach (var d in Doors) { float current = Vector3.Distance(game.player.transform.position, d.ClosedPosition); if (current < distance) { distance = current; best = d; } } return best;
        }
    }

    public sealed class CampusBuildingInfo : MonoBehaviour
    {
        public string Title, Body;
    }
}
