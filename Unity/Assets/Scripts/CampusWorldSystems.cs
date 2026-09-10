using System.Collections.Generic;
using UnityEngine;

namespace AlbionOdyssey
{
    // Lightweight campus life layer: authored paths, rest points and looping student routes.
    // These agents are intentionally local and deterministic so the offline game remains playable.
    public sealed class CampusWorldSystems : MonoBehaviour
    {
        OdysseyGame game;
        public CampusActivitySystem activities { get; private set; }
        Transform root;
        readonly List<CampusNpcAgent> agents = new List<CampusNpcAgent>();
        public int StudentCount=>agents.Count;
        public int MovingStudentCount
        {
            get {int count=0;foreach(var agent in agents)if(agent!=null&&agent.IsMoving)count++;return count;}
        }
        public int TransStudentCount
        {
            get {int count=0;foreach(var agent in agents)if(agent!=null&&agent.Identity!=null&&agent.Identity.IsTrans)count++;return count;}
        }

        public void Setup(OdysseyGame owner)
        {
            game = owner;
            root = new GameObject("Campus paths, furniture and NPC routes").transform;
            BuildLandscaping();
            Physics.SyncTransforms();
            BuildNpcRoutes();
            activities = gameObject.AddComponent<CampusActivitySystem>();
            activities.Setup(game);
        }

        void BuildLandscaping()
        {
            Material wood = TowerGeometry.Material("Campus bench wood", new Color(.30f, .15f, .07f));
            Material metal = TowerGeometry.Material("Campus lamp metal", new Color(.08f, .10f, .12f), .55f, .4f);
            Material planter = TowerGeometry.Material("Campus planter", new Color(.36f, .39f, .34f));
            Material flowers = TowerGeometry.Material("Campus planting", new Color(.42f, .18f, .48f));
            var anchors = new[]
            {
                CampusExpansion.Find("26").position + new Vector3(-18, 0, -9),
                CampusExpansion.Find("16").position + new Vector3(16, 0, -7),
                CampusCatalog.Point(503, 120) + new Vector3(-18, 0, -15),
                CampusCatalog.Point(503, 120) + new Vector3(18, 0, -15),
                CampusCatalog.Point(445, 225) + new Vector3(8, 0, -9)
            };
            for (int i = 0; i < anchors.Length; i++)
            {
                Bench(anchors[i], wood, i % 2 == 0);
                Lamp(anchors[i] + new Vector3(3.4f, 0, 1.2f), metal);
                Planter(anchors[i] + new Vector3(-3f, 0, 1.1f), planter, flowers);
            }
        }

        void Bench(Vector3 position, Material wood, bool faceNorth)
        {
            var t = new GameObject("Campus rest bench").transform; t.SetParent(root); t.position = position;
            KeeperAvatar.Part(t, "Bench seat", PrimitiveType.Cube, new Vector3(0, .52f, 0), new Vector3(2.4f, .16f, .58f), wood, true);
            KeeperAvatar.Part(t, "Bench back", PrimitiveType.Cube, new Vector3(0, .95f, faceNorth ? -.23f : .23f), new Vector3(2.4f, .75f, .12f), wood, true);
            foreach (int side in new[] { -1, 1 }) KeeperAvatar.Part(t, "Bench leg", PrimitiveType.Cube, new Vector3(side * .85f, .25f, 0), new Vector3(.12f, .5f, .38f), wood, true);
        }

        void Lamp(Vector3 position, Material metal)
        {
            var t = new GameObject("Campus path lamp").transform; t.SetParent(root); t.position = position;
            KeeperAvatar.Part(t, "Lamp post", PrimitiveType.Cylinder, new Vector3(0, 1.7f, 0), new Vector3(.12f, 1.7f, .12f), metal, true);
            var head = KeeperAvatar.Part(t, "Lamp head", PrimitiveType.Sphere, new Vector3(0, 3.45f, 0), new Vector3(.36f, .2f, .36f), metal);
            var light = head.AddComponent<Light>(); light.type = LightType.Point; light.range = 7; light.intensity = .65f; light.color = new Color(1f, .83f, .62f); light.shadows = LightShadows.None;
        }

        void Planter(Vector3 position, Material stone, Material flowers)
        {
            var t = new GameObject("Campus planted bed").transform; t.SetParent(root); t.position = position;
            KeeperAvatar.Part(t, "Planter", PrimitiveType.Cube, new Vector3(0, .25f, 0), new Vector3(1.35f, .5f, 1.35f), stone, true);
            KeeperAvatar.Part(t, "Planting", PrimitiveType.Sphere, new Vector3(0, .85f, 0), new Vector3(1.1f, .8f, 1.1f), flowers);
        }

        void BuildNpcRoutes()
        {
            Vector3[][] routes =
            {
                new[]{CampusCatalog.Point(400, 241), CampusCatalog.Point(445, 203), CampusCatalog.Point(503, 120)},
                new[]{CampusCatalog.Point(503, 120), CampusCatalog.Point(445, 225), CampusCatalog.Point(377, 180)},
                new[]{CampusCatalog.Point(409, 213), CampusCatalog.Point(413, 47), CampusCatalog.Point(504, 151)},
                new[]{CampusCatalog.Point(504, 151), CampusCatalog.Point(550, 344), CampusCatalog.Point(630, 349)},
                new[]{CampusCatalog.Point(503, 120), CampusCatalog.Point(428, 274), CampusCatalog.Point(739, 294)},
                new[]{CampusCatalog.Point(445, 203), CampusCatalog.Point(375, 229), CampusCatalog.Point(346, 180)},
                new[]{CampusCatalog.Point(346, 180), CampusCatalog.Point(377, 180), CampusCatalog.Point(429, 189)},
                new[]{CampusCatalog.Point(497, 204)+new Vector3(0,0,-18), CampusCatalog.Point(445, 225), CampusCatalog.Point(497, 204)},
                new[]{CampusCatalog.Point(413, 47), CampusCatalog.Point(323, 69), CampusCatalog.Point(310, 69)},
                new[]{CampusCatalog.Point(497, 274), CampusCatalog.Point(529, 274), CampusCatalog.Point(540, 188)},
                new[]{CampusCatalog.Point(550, 344), CampusCatalog.Point(544, 400), CampusCatalog.Point(684, 349)},
                new[]{CampusCatalog.Point(398, 174), CampusCatalog.Point(409, 213), CampusCatalog.Point(428, 274)},
                new[]{CampusCatalog.Point(503, 105), CampusCatalog.Point(503, 138), CampusCatalog.Point(512, 105)},
                new[]{CampusCatalog.Point(739, 294), CampusCatalog.Point(394, 383), CampusCatalog.Point(630, 349)},
                new[]{CampusCatalog.Point(445, 225)+new Vector3(-10,0,-9), CampusCatalog.Point(445, 203), CampusCatalog.Point(375, 229)},
                new[]{CampusCatalog.Point(572, 268), CampusCatalog.Point(575, 203), CampusCatalog.Point(693, 206)},
                new[]{CampusCatalog.Point(497, 204)+new Vector3(0,0,-18), CampusCatalog.Point(445, 225)+new Vector3(17,0,-9), CampusCatalog.Point(497, 204)},
                new[]{CampusCatalog.Point(400, 241)+new Vector3(18,0,0), CampusCatalog.Point(409, 213), CampusCatalog.Point(445, 203)},
                new[]{CampusCatalog.Point(310, 69), CampusCatalog.Point(323, 69), CampusCatalog.Point(375, 229)},
                new[]{CampusCatalog.Point(235, 296), CampusCatalog.Point(310, 275), CampusCatalog.Point(323, 275)},
                new[]{CampusCatalog.Point(630, 349), CampusCatalog.Point(682, 401), CampusCatalog.Point(544, 400)},
                new[]{CampusCatalog.Point(739, 294), CampusCatalog.Point(739, 310), CampusCatalog.Point(394, 383)},
                new[]{CampusCatalog.Point(489, 258), CampusCatalog.Point(509, 258), CampusCatalog.Point(532, 258)},
                new[]{CampusCatalog.Point(377, 180)+new Vector3(-16,0,0), CampusCatalog.Point(346, 180)+new Vector3(16,0,0), CampusCatalog.Point(429, 189)},
                new[]{CampusCatalog.Point(503, 120)+new Vector3(-20,0,0), CampusCatalog.Point(503, 138)+new Vector3(20,0,0), CampusCatalog.Point(492, 105)},
                new[]{CampusCatalog.Point(544, 400)+new Vector3(-18,0,0), CampusCatalog.Point(630, 349)+new Vector3(18,0,0), CampusCatalog.Point(684, 349)},
                new[]{CampusCatalog.Point(394, 383)+new Vector3(-20,0,-10), CampusCatalog.Point(394, 383)+new Vector3(20,0,10), CampusCatalog.Point(739, 294)}
            };
            for (int i = 0; i < routes.Length; i++)
            {
                var profile=CampusStudentProfiles.Get(i);
                for(int waypoint=0;waypoint<routes[i].Length;waypoint++)routes[i][waypoint]=CampusStudentNavigation.FreeWaypoint(routes[i][waypoint]);
                var o = new GameObject("Campus student · "+profile.Name+" · route "+(i+1)); o.transform.SetParent(root); o.transform.position = routes[i][0];
                var agent = o.AddComponent<CampusNpcAgent>(); agent.Route = routes[i]; agent.Speed = 1.1f + (i % 3) * .18f; agent.Build(profile); agents.Add(agent);
            }
        }
    }

    public sealed class CampusNpcAgent : MonoBehaviour
    {
        public Vector3[] Route; public float Speed = 1.2f;
        KeeperAvatar avatar; int target; Vector3 last;
        public CampusStudentIdentity Identity { get; private set; }
        public bool IsMoving { get; private set; }
        public void Build(CampusStudentProfile profile)
        {
            var body = new GameObject("Student avatar"); body.transform.SetParent(transform, false); avatar = body.AddComponent<KeeperAvatar>(); avatar.Build(profile.Skin, profile.Coat, profile.Hair, profile.Backpack); Identity=gameObject.AddComponent<CampusStudentIdentity>(); Identity.Apply(profile); var tag=gameObject.AddComponent<CampusWorldLabel>();tag.Configure(profile.Name+"  ·  STUDENT",new Color(.52f,.84f,.9f),new Vector3(0,2.35f,0),13f); last = transform.position;
        }
        void Update()
        {
            IsMoving=false;
            if (Route == null || Route.Length < 2 || avatar == null) return;
            Vector3 goal = Route[target] + Vector3.up * .08f; Vector3 delta = goal - transform.position; delta.y = 0;
            if (delta.magnitude < .7f) { target = (target + 1) % Route.Length; return; }
            Vector3 direction=delta.normalized;float distance=Mathf.Min(Speed*Time.deltaTime,delta.magnitude);
            if(CampusStudentNavigation.Blocked(transform.position,direction,distance)){target=(target+1)%Route.Length;return;}
            IsMoving=true;
            transform.position += direction * distance;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(delta.normalized, Vector3.up), Time.deltaTime * 5f);
            avatar.Animate((transform.position - last).magnitude / Mathf.Max(.001f, Time.deltaTime), false); last = transform.position;
        }
    }
}
