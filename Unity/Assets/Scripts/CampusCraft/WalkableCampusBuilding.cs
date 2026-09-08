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
            BuildExterior(); BuildInterior();
            var plaque = new GameObject(Place.name + " history plaque"); plaque.transform.SetParent(Root.transform, false); plaque.transform.localPosition = new Vector3(0, 2.15f, -Depth * .5f - .08f);
            var label = plaque.AddComponent<TextMesh>(); label.text = Place.name.ToUpperInvariant(); label.fontSize = 52; label.characterSize = .09f; label.anchor = TextAnchor.MiddleCenter; label.color = new Color(.22f, .13f, .30f);
            var info = plaque.AddComponent<CampusBuildingInfo>(); info.Title = Place.name; info.Body = History;
            Physics.SyncTransforms();
        }

        void BuildExterior()
        {
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
                Dormer(new Vector3(-Width * .27f, roofY + .65f, -Depth * .18f)); Dormer(new Vector3(Width * .27f, roofY + .65f, -Depth * .18f));
            }
            else
            {
                Box(Root.transform, "Bonta glazed lobby", new Vector3(0, 1.65f, -Depth * .5f - .28f), new Vector3(Width * .46f, 2.9f, .12f), glass, false);
                Box(Root.transform, "Bonta roof hip", new Vector3(0, roofY + .55f, 0), new Vector3(Width + 1.2f, .65f, Depth + 1.2f), roof, false);
            }
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
                else
                {
                    Box(Root.transform, "Bonta reception wall", new Vector3(0, y + 1.55f, 0), new Vector3(.15f, 3.1f, Depth - 3f), plaster, true); if (f == 0) Box(Root.transform, "Visitor lounge divider", new Vector3(Width * .25f, y + 1.55f, 1.5f), new Vector3(Width * .45f, 3.1f, .15f), plaster, true);
                }
                string sign = Id == "1" ? (f == 0 ? "ADMISSIONS LOBBY" : "VISITOR SERVICES") : Id == "18" ? (f == 0 ? "SCIENCE ON DISPLAY" : f == 1 ? "TEACHING LABORATORIES" : f == 2 ? "RESEARCH LABORATORIES" : "COLLECTIONS & OBSERVATION") : (f == 0 ? "QUAD ATRIUM" : "ACADEMIC OFFICES");
                Sign(new Vector3(-Width * .25f, y + 2.35f, -Depth * .5f + .25f), sign);
                for (int x = -1; x <= 1; x += 2)
                {
                    float roomX = x * Width * .31f; Box(Root.transform, "Room partition", new Vector3(roomX, y + 1.55f, Depth * .24f), new Vector3(.14f, 3.1f, Depth * .45f), plaster, true); Door(new Vector3(roomX, y, Depth * .02f), 1.25f, 2.35f, f == 0 ? (Id == "1" ? "Admissions office" : Id == "18" ? "Teaching lab" : "Department room") : Id == "18" ? "Laboratory door" : "Office door"); Desk(new Vector3(roomX, y, Depth * .34f));
                    if (Id == "18") LabBench(new Vector3(roomX, y, -Depth * .28f));
                }
                Light(new Vector3(0, y + 3.05f, 0)); if (f < Floors - 1) Stair(f);
            }
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
                game.tour.Open(story); return true;
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
