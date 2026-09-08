using UnityEngine;

namespace AlbionOdyssey
{
    // Game-scale parking geometry anchored to the public campus map. The named
    // lots are the visitor lots beside Bonta and Kellogg plus the Ferguson lot.
    public sealed class CampusParking : MonoBehaviour
    {
        public int StallCount { get; private set; }
        public int ParkedCarCount { get; private set; }
        Material asphalt, stripe, accessible, curb, sign;

        public void Setup(OdysseyGame owner)
        {
            asphalt = TowerGeometry.Material("Parking asphalt", new Color(.10f, .12f, .13f));
            stripe = TowerGeometry.Material("Parking white lines", new Color(.88f, .87f, .78f));
            accessible = TowerGeometry.Material("Accessible parking blue", new Color(.08f, .31f, .64f));
            curb = TowerGeometry.Material("Parking curb", new Color(.47f, .49f, .46f));
            sign = TowerGeometry.Material("Parking sign", new Color(.10f, .33f, .56f), .15f, .25f);

            // The college identifies visitor parking next to Bonta and Kellogg,
            // and directs visitors to the Ferguson lot for major campus events.
            Lot("Bonta visitor parking", CampusCatalog.Point(516, 151), 10, 5, 1835, true);
            Lot("Kellogg visitor parking", CampusCatalog.Point(432, 225), 12, 6, 1884, true);
            Lot("Ferguson parking lot", CampusCatalog.Point(412, 252), 14, 7, 2002, false);
        }

        void Lot(string name, Vector3 center, int stalls, int columns, int seed, bool visitor)
        {
            int rows = Mathf.CeilToInt(stalls / (float)columns);
            float width = columns * 2.8f + 2.2f, depth = rows * 6.2f + 2.4f;
            var root = new GameObject(name); root.transform.SetParent(transform, false); root.transform.position = center;
            Part(root.transform, "Lot surface", Vector3.zero, new Vector3(width, .12f, depth), asphalt, true);
            Part(root.transform, "North curb", new Vector3(0, .10f, depth * .5f), new Vector3(width, .22f, .28f), curb, true);
            Part(root.transform, "South curb", new Vector3(0, .10f, -depth * .5f), new Vector3(width, .22f, .28f), curb, true);
            Part(root.transform, "West curb", new Vector3(-width * .5f, .10f, 0), new Vector3(.28f, .22f, depth), curb, true);
            Part(root.transform, "East curb", new Vector3(width * .5f, .10f, 0), new Vector3(.28f, .22f, depth), curb, true);

            for (int i = 0; i < stalls; i++)
            {
                int row = i / columns, column = i % columns;
                float x = (column - (columns - 1) * .5f) * 2.8f;
                float z = (row - (rows - 1) * .5f) * 6.2f;
                var lineMat = i == 0 ? accessible : stripe;
                Part(root.transform, "Parking stall line", new Vector3(x - 1.4f, .18f, z), new Vector3(.07f, .025f, 5.0f), lineMat, false);
                Part(root.transform, "Parking stall line", new Vector3(x + 1.4f, .18f, z), new Vector3(.07f, .025f, 5.0f), lineMat, false);
                if ((i + seed) % 3 != 1) SpawnCar(root.transform, new Vector3(x, .18f, z), (i + seed) % 4, (i * 37 + seed) % 5);
                StallCount++;
            }

            string label = visitor ? "VISITOR PARKING\nPERMIT REQUIRED" : "FERGUSON PARKING\nPERMIT ZONE";
            var marker = new GameObject(name + " sign"); marker.transform.SetParent(root.transform, false); marker.transform.localPosition = new Vector3(-width * .5f + 1.2f, 2.1f, -depth * .5f - .5f);
            var text = marker.AddComponent<TextMesh>(); text.text = label; text.fontSize = 42; text.characterSize = .10f; text.anchor = TextAnchor.MiddleCenter; text.alignment = TextAlignment.Center; text.color = new Color(.92f, .96f, 1f);
            Part(marker.transform, "Sign face", new Vector3(0, 0, .05f), new Vector3(2.1f, 1.0f, .08f), sign, false);
            Part(marker.transform, "Sign post", new Vector3(0, -1.0f, 0), new Vector3(.08f, 2.0f, .08f), curb, true);
        }

        void SpawnCar(Transform parent, Vector3 at, int style, int colorIndex)
        {
            var car = new GameObject(parent.name + " · parked car " + (ParkedCarCount + 1)); car.transform.SetParent(parent, false); car.transform.localPosition = at; car.transform.localRotation = Quaternion.Euler(0, 0, 0);
            var parked = car.AddComponent<CampusParkedVehicle>(); parked.Build(Paint(colorIndex), style); ParkedCarCount++;
        }

        Color Paint(int index)
        {
            return new[]{new Color(.12f, .16f, .19f), new Color(.72f, .08f, .06f), new Color(.82f, .79f, .70f), new Color(.08f, .28f, .46f), new Color(.16f, .43f, .28f)}[Mathf.Clamp(index, 0, 4)];
        }

        static GameObject Part(Transform parent, string name, Vector3 position, Vector3 scale, Material material, bool solid)
        {
            return KeeperAvatar.Part(parent, name, PrimitiveType.Cube, position, scale, material, solid);
        }
    }

    // Unbranded, real-world vehicle silhouettes: sedan, crossover, hatchback and pickup.
    public sealed class CampusParkedVehicle : MonoBehaviour
    {
        public void Build(Color paintColor, int style)
        {
            var body = TowerGeometry.Material("Parked car paint", paintColor, .32f, .55f);
            var glass = TowerGeometry.Material("Parked car glass", new Color(.08f, .16f, .20f), .18f, .55f);
            var tire = TowerGeometry.Material("Parked car rubber", new Color(.025f, .028f, .03f));
            var chrome = TowerGeometry.Material("Parked car trim", new Color(.68f, .71f, .70f), .7f, .6f);
            var lamp = TowerGeometry.Material("Parked car lamps", new Color(1f, .82f, .48f), .15f, .7f);
            float length = style == 2 ? 3.8f : style == 3 ? 4.9f : 4.6f, width = style == 1 ? 2.05f : 1.9f;
            KeeperAvatar.Part(transform, "Vehicle body", PrimitiveType.Cube, new Vector3(0, .58f, 0), new Vector3(width, .55f, length), body);
            if (style == 3)
            {
                KeeperAvatar.Part(transform, "Pickup cab", PrimitiveType.Cube, new Vector3(0, 1.05f, length * .20f), new Vector3(width * .92f, .72f, length * .38f), body);
                KeeperAvatar.Part(transform, "Pickup bed", PrimitiveType.Cube, new Vector3(0, .88f, -length * .25f), new Vector3(width * .92f, .20f, length * .38f), body);
            }
            else
            {
                float cabinHeight = style == 1 ? 1.05f : .92f;
                KeeperAvatar.Part(transform, "Vehicle cabin", PrimitiveType.Cube, new Vector3(0, cabinHeight, -.05f), new Vector3(width * .86f, .62f, style == 2 ? 1.72f : 2.15f), body);
                KeeperAvatar.Part(transform, "Front glass", PrimitiveType.Cube, new Vector3(0, cabinHeight + .03f, style == 2 ? .43f : .55f), new Vector3(width * .72f, .32f, .05f), glass);
                KeeperAvatar.Part(transform, "Rear glass", PrimitiveType.Cube, new Vector3(0, cabinHeight + .03f, style == 2 ? -.43f : -.62f), new Vector3(width * .72f, .32f, .05f), glass);
                foreach (int side in new[]{-1, 1}) KeeperAvatar.Part(transform, "Side window", PrimitiveType.Cube, new Vector3(side * width * .44f, cabinHeight + .03f, 0), new Vector3(.04f, .30f, style == 2 ? 1.25f : 1.65f), glass);
            }
            for (int i = 0; i < 4; i++)
            {
                float side = i % 2 == 0 ? -1 : 1, z = i < 2 ? -length * .30f : length * .30f;
                var wheel = KeeperAvatar.Part(transform, "Vehicle wheel", PrimitiveType.Cylinder, new Vector3(side * (width * .53f), .42f, z), new Vector3(.65f, .13f, .65f), tire);
                wheel.transform.localRotation = Quaternion.Euler(0, 0, 90);
            }
            KeeperAvatar.Part(transform, "Front plate", PrimitiveType.Cube, new Vector3(0, .66f, length * .51f), new Vector3(.55f, .13f, .03f), chrome);
            KeeperAvatar.Part(transform, "Headlights", PrimitiveType.Cube, new Vector3(0, .76f, length * .505f), new Vector3(width * .64f, .12f, .04f), lamp);
            KeeperAvatar.Part(transform, "Tail strip", PrimitiveType.Cube, new Vector3(0, .76f, -length * .505f), new Vector3(width * .64f, .12f, .04f), TowerGeometry.Material("Parked car tail lamps", new Color(.55f, .025f, .018f), .1f, .6f));
            var hull = gameObject.AddComponent<BoxCollider>(); hull.center = new Vector3(0, .78f, 0); hull.size = new Vector3(width + .12f, 1.25f, length + .12f);
        }
    }
}
