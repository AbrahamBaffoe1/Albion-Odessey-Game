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
                if ((i + seed) % 3 != 1) SpawnCar(root.transform, new Vector3(x, .18f, z), (i * 37 + seed) % 5);
                StallCount++;
            }

            string label = visitor ? "VISITOR PARKING\nPERMIT REQUIRED" : "FERGUSON PARKING\nPERMIT ZONE";
            var marker = new GameObject(name + " sign"); marker.transform.SetParent(root.transform, false); marker.transform.localPosition = new Vector3(-width * .5f + 1.2f, 2.1f, -depth * .5f - .5f);
            var text = marker.AddComponent<TextMesh>(); text.text = label; text.fontSize = 42; text.characterSize = .10f; text.anchor = TextAnchor.MiddleCenter; text.alignment = TextAlignment.Center; text.color = new Color(.92f, .96f, 1f);
            Part(marker.transform, "Sign face", new Vector3(0, 0, .05f), new Vector3(2.1f, 1.0f, .08f), sign, false);
            Part(marker.transform, "Sign post", new Vector3(0, -1.0f, 0), new Vector3(.08f, 2.0f, .08f), curb, true);
        }

        void SpawnCar(Transform parent, Vector3 at, int colorIndex)
        {
            var car = new GameObject(parent.name + " · parked car " + (ParkedCarCount + 1)); car.transform.SetParent(parent, false); car.transform.localPosition = at; car.transform.localRotation = Quaternion.Euler(0, 0, 0);
            var parked = car.AddComponent<CampusParkedVehicle>(); parked.Build(Paint(colorIndex)); ParkedCarCount++;
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

    // Detailed shared coupe model. Paint varies per parked car without cloning materials.
    public sealed class CampusParkedVehicle : MonoBehaviour
    {
        public void Build(Color paintColor)
        {
            var visual=gameObject.AddComponent<CampusVehicleVisual>();visual.Build(paintColor);
            var hull=gameObject.AddComponent<BoxCollider>();hull.center=new Vector3(0,.78f,0);hull.size=new Vector3(2.2f,1.3f,4.4f);
        }
    }
}
