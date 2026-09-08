using System.Collections.Generic;
using UnityEngine;

namespace AlbionOdyssey
{
    public enum CampusActivityKind { Learn, Teach, Serve, Buy, Eat, Play }

    // Physical activity layer for the chapter: furniture, counters, study tools and
    // people are authored as world objects so the campus reads as occupied even offline.
    public sealed class CampusActivitySystem : MonoBehaviour
    {
        public int ActivitySpaces { get; private set; }
        public int ActivityAgents { get; private set; }
        public int InteractionStations { get; private set; }
        public int CompletedInteractions { get; private set; }
        public Transform Root { get; private set; }
        readonly List<CampusActivityAgent> agents = new List<CampusActivityAgent>();
        OdysseyGame game;
        Material wood, metal, fabric, paper, food, counter, chalk, green, gold, glass;

        public void Setup(OdysseyGame owner)
        {
            game = owner;
            Root = new GameObject("Campus activity spaces · learning dining recreation").transform;
            wood = TowerGeometry.Material("Activity oak", new Color(.34f, .17f, .08f));
            metal = TowerGeometry.Material("Activity metal", new Color(.11f, .14f, .16f), .55f, .45f);
            fabric = TowerGeometry.Material("Activity upholstery", new Color(.20f, .31f, .40f));
            paper = TowerGeometry.Material("Activity paper", new Color(.92f, .88f, .72f));
            food = TowerGeometry.Material("Cafeteria food", new Color(.85f, .34f, .10f), .1f, .35f);
            counter = TowerGeometry.Material("Cafeteria counter", new Color(.40f, .23f, .12f), .2f, .3f);
            chalk = TowerGeometry.Material("Classroom board", new Color(.06f, .18f, .16f));
            green = TowerGeometry.Material("Recreation felt", new Color(.08f, .37f, .24f));
            gold = TowerGeometry.Material("Activity brass", new Color(.92f, .64f, .18f), .5f, .6f);
            glass = TowerGeometry.Material("Activity glassware", new Color(.35f, .70f, .78f), .2f, .6f);
            BuildLearningSpaces();
            BuildDiningCommons();
            BuildRecreation();
            BuildAgents();
            Physics.SyncTransforms();
        }

        // Exposed counts make the activity layer part of the release smoke contract.
        public void RecordInteraction() { CompletedInteractions++; }

        void BuildLearningSpaces()
        {
            Vector3 ferguson = CampusBuildings.Instance != null ? CampusBuildings.Instance.Origin : CampusExpansion.Find("26").position;
            RoomLabel(ferguson + new Vector3(-7.2f, 2.45f, -5.95f), "STUDENT SERVICES · LEARNING LAB");
            Board(ferguson + new Vector3(-7.0f, 2.1f, 5.2f), "TODAY\nBIOLOGY  ·  10:00\nHISTORY  ·  13:00");
            for (int i = 0; i < 6; i++)
            {
                float x = -11.2f + (i % 3) * 4.4f, z = -2.5f + (i / 3) * 3.0f;
                Desk(ferguson + new Vector3(x, .05f, z), "Learning desk " + (i + 1));
                ActivitySpace("Study seat " + (i + 1), CampusActivityKind.Learn, ferguson + new Vector3(x, .05f, z - .9f), new Vector3(1.2f, 1.2f, 1.2f), "Sit at the desk and study the current course lesson.");
            }
            Desk(ferguson + new Vector3(7.0f, .05f, 2.4f), "Instructor desk");
            ActivitySpace("Instructor station", CampusActivityKind.Teach, ferguson + new Vector3(7.0f, .05f, 1.5f), new Vector3(1.8f, 1.2f, 1.2f), "Lead the lesson here. Your course progress is saved.");
            Vector3 robinson = CampusExpansion.Find("16").position;
            RoomLabel(robinson + new Vector3(-10f, 2.4f, -7.0f), "ROBINSON HALL · SEMINAR ROOM");
            Board(robinson + new Vector3(0, 2.2f, 7.2f), "SEMINAR\nDISCUSS · LISTEN · CREATE");
            for (int i = 0; i < 4; i++)
            {
                float a = i * Mathf.PI * .5f;
                Vector3 at = robinson + new Vector3(Mathf.Cos(a) * 4.0f, .05f, Mathf.Sin(a) * 2.4f);
                Desk(at, "Robinson seminar table " + (i + 1));
                ActivitySpace("Seminar discussion " + (i + 1), CampusActivityKind.Learn, at + Vector3.back * .7f, new Vector3(1.4f, 1.2f, 1.2f), "Join the Robinson seminar and share an idea.");
            }
            Vector3 science = CampusCatalog.Point(503, 120);
            RoomLabel(science + new Vector3(-9.0f, 2.5f, -12.4f), "SCIENCE COMPLEX · TEACHING LAB");
            for (int i = 0; i < 3; i++)
            {
                Vector3 at = science + new Vector3(-9f + i * 6f, .05f, -4.2f);
                LabBench(at, "Science lab bench " + (i + 1));
                ActivitySpace("Lab practical " + (i + 1), CampusActivityKind.Learn, at + Vector3.back * .9f, new Vector3(1.8f, 1.2f, 1.2f), "Handle the lab equipment and complete today's practical.");
            }
        }

        void BuildDiningCommons()
        {
            CampusPlace baldwin = CampusExpansion.Find("6");
            Vector3 center = baldwin != null ? baldwin.position + new Vector3(0, .1f, -18f) : CampusCatalog.Point(497, 204) + new Vector3(0, .1f, -18f);
            // An open-sided dining annex keeps the counter, tables and people readable
            // from the campus approach while avoiding a sealed exterior placeholder.
            Box(center + Vector3.up * 2.8f, new Vector3(18f, .25f, 9f), wood, false, "Baldwin dining canopy");
            Box(center + new Vector3(-8.7f, 1.4f, 0), new Vector3(.25f, 2.8f, 9f), wood, true, "Dining west screen");
            Box(center + new Vector3(8.7f, 1.4f, 0), new Vector3(.25f, 2.8f, 9f), wood, true, "Dining east screen");
            RoomLabel(center + new Vector3(0, 3.35f, -4.7f), "BALDWIN DINING COMMONS");
            Counter(center + new Vector3(-5.8f, .05f, 1.8f));
            ActivitySpace("Food service counter", CampusActivityKind.Serve, center + new Vector3(-5.8f, .05f, .4f), new Vector3(2.2f, 1.2f, 1.2f), "Pick up a fresh meal from the dining worker.");
            ActivitySpace("Checkout counter", CampusActivityKind.Buy, center + new Vector3(-2.5f, .05f, .4f), new Vector3(1.8f, 1.2f, 1.2f), "Buy a meal with your campus dining credit.");
            for (int i = 0; i < 4; i++)
            {
                float x = 1.5f + (i % 2) * 4.0f, z = -1.7f + (i / 2) * 3.5f;
                DiningTable(center + new Vector3(x, .05f, z), i + 1);
                ActivitySpace("Dining table " + (i + 1), CampusActivityKind.Eat, center + new Vector3(x, .05f, z), new Vector3(1.8f, 1.2f, 1.8f), "Sit down, eat and talk with other students.");
            }
        }

        void BuildRecreation()
        {
            CampusPlace kellogg = CampusExpansion.Find("14");
            Vector3 center = kellogg != null ? kellogg.position + new Vector3(17f, .1f, -9f) : CampusCatalog.Point(445, 225) + new Vector3(17f, .1f, -9f);
            RoomLabel(center + new Vector3(0, 2.8f, -3.7f), "KELLOGG COMMONS · GAME LOUNGE");
            // Table tennis, chess and a small lounge make “play” a physical destination.
            Box(center + Vector3.up * .9f, new Vector3(2.6f, .16f, 1.3f), green, true, "Table tennis top");
            foreach (int side in new[] { -1, 1 }) Box(center + new Vector3(side * 1.0f, .4f, 0), new Vector3(.12f, .8f, .12f), metal, true, "Table tennis leg");
            Box(center + new Vector3(0, 1.35f, 0), new Vector3(.06f, .7f, 1.25f), paper, false, "Table tennis net");
            ActivitySpace("Table tennis", CampusActivityKind.Play, center + new Vector3(0, .05f, 1.1f), new Vector3(2.8f, 1.2f, 2f), "Start a table-tennis game with the students here.");
            ChessTable(center + new Vector3(4.0f, .05f, .2f));
            ActivitySpace("Chess table", CampusActivityKind.Play, center + new Vector3(4.0f, .05f, .2f), new Vector3(1.8f, 1.2f, 1.8f), "Play a quick chess match in the commons.");
            Bench(center + new Vector3(-4.0f, 0, .4f));
        }

        void BuildAgents()
        {
            CampusPlace baldwin = CampusExpansion.Find("6");
            Vector3 dining = (baldwin != null ? baldwin.position : CampusCatalog.Point(497, 204)) + new Vector3(0, .1f, -18f);
            Vector3 ferguson = CampusBuildings.Instance != null ? CampusBuildings.Instance.Origin : CampusExpansion.Find("26").position;
            Vector3 robinson = CampusExpansion.Find("16").position;
            Vector3 science = CampusCatalog.Point(503, 120);
            CampusPlace kellogg = CampusExpansion.Find("14");
            Vector3 recreation = (kellogg != null ? kellogg.position : CampusCatalog.Point(445, 225)) + new Vector3(17f, .1f, -9f);
            Agent("Professor Ayo", CampusActivityKind.Teach, new[] { ferguson + new Vector3(7, .08f, 1.5f), ferguson + new Vector3(7, .08f, 3.2f) }, 2, 3, 1);
            Agent("Study group 1", CampusActivityKind.Learn, new[] { ferguson + new Vector3(-11.2f, .08f, -3.4f), ferguson + new Vector3(-6.8f, .08f, -3.4f) }, 1, 0, 0);
            Agent("Study group 2", CampusActivityKind.Learn, new[] { robinson + new Vector3(-4, .08f, 1.7f), robinson + new Vector3(0, .08f, -1.7f) }, 3, 1, 1);
            Agent("Lab researcher", CampusActivityKind.Learn, new[] { science + new Vector3(-9, .08f, -5.0f), science + new Vector3(-3, .08f, -5.0f) }, 4, 3, 2);
            Agent("Dining worker", CampusActivityKind.Serve, new[] { dining + new Vector3(-5.8f, .08f, .35f), dining + new Vector3(-5.8f, .08f, 2.0f) }, 2, 2, 0);
            Agent("Cashier", CampusActivityKind.Buy, new[] { dining + new Vector3(-2.5f, .08f, .35f), dining + new Vector3(-5.8f, .08f, .35f) }, 3, 0, 1);
            Agent("Student lunch 1", CampusActivityKind.Eat, new[] { dining + new Vector3(1.5f, .08f, -1.7f), dining + new Vector3(5.5f, .08f, -1.7f) }, 1, 1, 0);
            Agent("Student lunch 2", CampusActivityKind.Eat, new[] { dining + new Vector3(1.5f, .08f, 1.8f), dining + new Vector3(5.5f, .08f, 1.8f) }, 4, 3, 2);
            Agent("Commons player", CampusActivityKind.Play, new[] { recreation + new Vector3(0, .08f, 1.1f), recreation + new Vector3(4, .08f, .2f) }, 2, 4, 1);
        }

        void Agent(string name, CampusActivityKind kind, Vector3[] route, int skin, int coat, int hair)
        {
            var o = new GameObject(name + " · " + kind); o.transform.SetParent(Root, false); o.transform.position = route[0];
            var agent = o.AddComponent<CampusActivityAgent>(); agent.Activity = kind; agent.Route = route; agent.Speed = kind == CampusActivityKind.Play ? .8f : 1.0f; agent.Build(skin, coat, hair); agents.Add(agent); ActivityAgents++;
        }

        void ActivitySpace(string title, CampusActivityKind kind, Vector3 at, Vector3 size, string prompt)
        {
            var o = new GameObject(title); o.transform.SetParent(Root, false); o.transform.position = at;
            var station = o.AddComponent<CampusActivityStation>(); station.Title = title; station.Kind = kind; station.Prompt = prompt; station.Owner = this;
            var hit = o.AddComponent<BoxCollider>(); hit.isTrigger = true; hit.size = size; InteractionStations++;
            ActivitySpaces++;
        }

        void RoomLabel(Vector3 at, string value)
        { var o = new GameObject(value); o.transform.SetParent(Root, false); o.transform.position = at; var t = o.AddComponent<TextMesh>(); t.text = value; t.fontSize = 40; t.characterSize = .07f; t.anchor = TextAnchor.MiddleCenter; t.color = new Color(.97f, .84f, .50f); }
        void Board(Vector3 at, string value) { Box(at, new Vector3(3.2f, 1.7f, .12f), chalk, true, "Learning board"); RoomLabel(at + Vector3.forward * -.08f + Vector3.up * .05f, value); }
        void Desk(Vector3 at, string name) { Box(at + Vector3.up * .72f, new Vector3(1.8f, .12f, .75f), wood, true, name); foreach (int s in new[] { -1, 1 }) Box(at + new Vector3(s * .65f, .35f, 0), new Vector3(.10f, .7f, .10f), metal, true, "Desk leg"); Box(at + Vector3.up * .82f + Vector3.forward * .05f, new Vector3(.5f, .05f, .3f), paper, false, "Notebook"); }
        void LabBench(Vector3 at, string name) { Box(at + Vector3.up * .72f, new Vector3(2.7f, .14f, .9f), metal, true, name); Box(at + Vector3.up * .84f, new Vector3(.7f, .05f, .4f), glass, false, "Lab glassware"); }
        void Counter(Vector3 at) { Box(at + Vector3.up * .72f, new Vector3(6.1f, .22f, 1.1f), counter, true, "Dining service counter"); Box(at + Vector3.up * 1.05f + Vector3.forward * .28f, new Vector3(1.0f, .35f, .5f), food, false, "Prepared food tray"); Box(at + Vector3.up * 1.05f + Vector3.back * .28f, new Vector3(.7f, .35f, .5f), gold, false, "Cash register"); }
        void DiningTable(Vector3 at, int number) { Box(at + Vector3.up * .72f, new Vector3(2.4f, .12f, 1.4f), wood, true, "Dining table " + number); foreach (int side in new[] { -1, 1 }) foreach (int end in new[] { -1, 1 }) Box(at + new Vector3(side * .85f, .35f, end * .45f), new Vector3(.10f, .7f, .10f), metal, true, "Dining table leg"); foreach (int side in new[] { -1, 1 }) Box(at + new Vector3(side * 1.45f, .45f, 0), new Vector3(.55f, .12f, .55f), fabric, true, "Dining chair"); Box(at + Vector3.up * .84f, new Vector3(.35f, .06f, .25f), food, false, "Lunch plate"); }
        void ChessTable(Vector3 at) { Box(at + Vector3.up * .72f, new Vector3(1.8f, .12f, 1.8f), wood, true, "Chess table"); for (int i = 0; i < 8; i++) for (int j = 0; j < 8; j++) Box(at + new Vector3(-.7f + i * .2f, .80f, -.7f + j * .2f), new Vector3(.19f, .025f, .19f), (i + j) % 2 == 0 ? paper : wood, false, "Chess board square"); }
        void Bench(Vector3 at) { Box(at + Vector3.up * .48f, new Vector3(2.2f, .14f, .55f), fabric, true, "Commons lounge bench"); }
        static GameObject Box(Vector3 at, Vector3 size, Material mat, bool solid, string name) { var o = KeeperAvatar.Part(null, name, PrimitiveType.Cube, at, size, mat, solid); return o; }
    }

    public sealed class CampusActivityStation : MonoBehaviour
    {
        public string Title, Prompt; public CampusActivityKind Kind; public CampusActivitySystem Owner;
        public int Uses { get; private set; }
        public void Activate(OdysseyGame game)
        {
            Uses++; if (Owner != null) Owner.RecordInteraction();
            string action = Kind == CampusActivityKind.Serve ? "A dining worker serves today's hot meal." : Kind == CampusActivityKind.Buy ? "Meal purchased. Your campus dining credit covers it." : Kind == CampusActivityKind.Eat ? "You sit down to eat and meet the students at the table." : Kind == CampusActivityKind.Play ? "Game started. Invite a student and play a round." : Kind == CampusActivityKind.Teach ? "The lesson is in session. Your course attendance is recorded." : "You study with the class and review the lesson.";
            game.notice = Title + " · " + action;
        }
    }

    public sealed class CampusActivityAgent : MonoBehaviour
    {
        public CampusActivityKind Activity; public Vector3[] Route; public float Speed = 1f;
        KeeperAvatar avatar; int target; float pause; Vector3 last; bool seated;
        public void Build(int skin, int coat, int hair) { var body = new GameObject("Student body"); body.transform.SetParent(transform, false); avatar = body.AddComponent<KeeperAvatar>(); avatar.Build(skin, coat, hair, Activity != CampusActivityKind.Play); last = transform.position; }
        void Update()
        {
            if (avatar == null || Route == null || Route.Length == 0) return;
            if (pause > 0) { pause -= Time.deltaTime; avatar.Animate(0, seated); return; }
            Vector3 goal = Route[target]; Vector3 delta = goal - transform.position; delta.y = 0;
            if (delta.magnitude < .22f) { target = (target + 1) % Route.Length; pause = Activity == CampusActivityKind.Play ? 2.5f : 1.5f; seated = Activity == CampusActivityKind.Learn || Activity == CampusActivityKind.Eat; avatar.Animate(0, seated); return; }
            seated = false; transform.position += delta.normalized * Speed * Time.deltaTime; transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(delta.normalized, Vector3.up), Time.deltaTime * 5f); avatar.Animate(Speed, false); last = transform.position;
        }
    }
}
