using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace AlbionOdyssey
{
    /// <summary>
    /// One palette and one set of materials shared by every Whitehouse-inspired nature scene, so the Forest Treasure Run
    /// and the mapped nature trails read as the same place: sage-green moss, warm earth trails, teal river water and
    /// golden-hour light, dissolving into a soft mist at the horizon.
    /// </summary>
    public sealed class NatureKit
    {
        public static readonly Color Mist = new Color(.74f, .85f, .80f), SunTint = new Color(1f, .93f, .78f);

        public readonly Material Moss, Trail, TrailEdge, TrailLight, TrailClosed, Gravel, Litter, Mud, Water, Foam, Reed, Cattail, Bark, WoodCut,
            LeafLight, LeafDeep, Fern, Stone, FlowerWhite, FlowerLilac, FlowerGold, Mint, Gold;
        readonly List<Material> all = new List<Material>();

        public NatureKit()
        {
            Moss = Make("Nature moss", new Color(.72f, .84f, .58f), .05f);
            var grass = Resources.Load<Texture2D>("CampusCraft/grass_ground_Color"); if (grass != null) Moss.mainTexture = grass;
            Trail = Make("Nature trail earth", new Color(.62f, .48f, .30f), .08f); TrailEdge = Make("Nature trail edge", new Color(.50f, .41f, .26f), .08f);
            TrailLight = Make("Nature trail lane", new Color(.74f, .62f, .43f), .08f); TrailClosed = Make("Nature trail closed", new Color(.42f, .34f, .26f), .08f);
            Gravel = Make("Nature rail gravel", new Color(.52f, .51f, .48f), .1f); Litter = Make("Nature leaf litter", new Color(.55f, .34f, .16f), .05f);
            Mud = Make("Nature river mud", new Color(.26f, .21f, .15f), .05f); Water = Make("Nature river water", new Color(.20f, .46f, .52f), .92f);
            Foam = Make("Nature water edge", new Color(.80f, .90f, .88f), .6f); Reed = Make("Nature reed", new Color(.48f, .58f, .26f), .1f);
            Cattail = Make("Nature cattail", new Color(.33f, .20f, .12f), .1f); Bark = Make("Nature bark", new Color(.30f, .20f, .12f), .1f);
            WoodCut = Make("Nature cut wood", new Color(.70f, .55f, .34f), .15f); LeafLight = Make("Nature leaf light", new Color(.38f, .54f, .22f), .15f);
            LeafDeep = Make("Nature leaf deep", new Color(.15f, .32f, .14f), .12f); Fern = Make("Nature fern", new Color(.30f, .52f, .22f), .12f);
            Stone = Make("Nature stone", new Color(.55f, .57f, .56f), .2f); FlowerWhite = Make("Nature flower white", new Color(.95f, .95f, .88f), .2f);
            FlowerLilac = Make("Nature flower lilac", new Color(.68f, .55f, .85f), .2f); FlowerGold = Make("Nature flower gold", new Color(.96f, .78f, .20f), .2f);
            Mint = Make("Nature restoration seed", new Color(.35f, .95f, .59f), .5f); Gold = Make("Nature treasure gold", new Color(1f, .69f, .13f), .6f);
        }

        Material Make(string name, Color color, float smoothness) { var m = TowerGeometry.Material(name, color, 0f, smoothness); all.Add(m); return m; }

        public void Release() { foreach (var m in all) if (m != null) Object.Destroy(m); all.Clear(); }
    }

    /// <summary>
    /// Collects meshes with their materials and bakes them into one mesh per material, so a whole stretch of woodland costs a
    /// handful of draw calls. Props are added as primitive shapes or as the hierarchy of an existing model, with no scene objects.
    /// </summary>
    public sealed class NatureBatch
    {
        static readonly Dictionary<PrimitiveType, Mesh> primitives = new Dictionary<PrimitiveType, Mesh>();
        readonly Dictionary<Material, List<CombineInstance>> groups = new Dictionary<Material, List<CombineInstance>>();
        readonly List<Mesh> scratch = new List<Mesh>();
        public readonly List<Mesh> Meshes = new List<Mesh>();

        static Mesh PrimitiveMesh(PrimitiveType type)
        {
            if (primitives.TryGetValue(type, out var mesh) && mesh != null) return mesh;
            var o = GameObject.CreatePrimitive(type); mesh = o.GetComponent<MeshFilter>().sharedMesh; Object.Destroy(o); primitives[type] = mesh; return mesh;
        }

        public void Add(Mesh mesh, int subMesh, Matrix4x4 matrix, Material material)
        {
            if (mesh == null || material == null) return;
            if (!groups.TryGetValue(material, out var list)) groups[material] = list = new List<CombineInstance>();
            list.Add(new CombineInstance { mesh = mesh, subMeshIndex = subMesh, transform = matrix });
        }

        public void Prim(PrimitiveType type, Vector3 position, Vector3 scale, Quaternion rotation, Material material)
        {
            Add(PrimitiveMesh(type), 0, Matrix4x4.TRS(position, rotation, scale), material);
        }
        public void Prim(PrimitiveType type, Vector3 position, Vector3 scale, Material material) { Prim(type, position, scale, Quaternion.identity, material); }

        /// <summary>A flat upward-facing rectangle whose texture coordinates run in whole-chunk metres, so neighbouring chunks tile seamlessly.</summary>
        public void Quad(Vector3 center, Vector2 size, Material material, float tileMetres)
        {
            var mesh = NatureLook.QuadMesh(center, size, tileMetres); scratch.Add(mesh); Add(mesh, 0, Matrix4x4.Translate(center), material);
        }

        /// <summary>Adds every renderer of <paramref name="template"/> (inactive is fine) at <paramref name="placement"/>.</summary>
        public void Hierarchy(Transform template, Matrix4x4 placement)
        {
            foreach (var filter in template.GetComponentsInChildren<MeshFilter>(true))
            {
                var renderer = filter.GetComponent<MeshRenderer>(); if (renderer == null || filter.sharedMesh == null) continue;
                var relative = template.worldToLocalMatrix * filter.transform.localToWorldMatrix; var materials = renderer.sharedMaterials;
                for (int sub = 0; sub < filter.sharedMesh.subMeshCount && sub < materials.Length; sub++) Add(filter.sharedMesh, sub, placement * relative, materials[sub]);
            }
        }

        /// <summary>Creates one child object per material under <paramref name="parent"/> and clears the batch.</summary>
        public void Flush(Transform parent)
        {
            foreach (var pair in groups)
            {
                var o = new GameObject("Batched · " + pair.Key.name); o.transform.SetParent(parent, false);
                var mesh = new Mesh { name = "Batched " + pair.Key.name, indexFormat = IndexFormat.UInt32 };
                mesh.CombineMeshes(pair.Value.ToArray(), true, true);
                o.AddComponent<MeshFilter>().sharedMesh = mesh; o.AddComponent<MeshRenderer>().sharedMaterial = pair.Key; Meshes.Add(mesh);
            }
            foreach (var mesh in scratch) if (mesh != null) Object.Destroy(mesh);
            scratch.Clear(); groups.Clear();
        }
    }

    public sealed class NatureOwnedAssets : MonoBehaviour
    {
        readonly HashSet<Mesh> meshes = new HashSet<Mesh>();
        readonly HashSet<Material> materials = new HashSet<Material>();
        public void Track(GameObject root)
        {
            foreach (var f in root.GetComponentsInChildren<MeshFilter>(true)) if (f.sharedMesh != null) meshes.Add(f.sharedMesh);
            foreach (var r in root.GetComponentsInChildren<MeshRenderer>(true)) foreach (var m in r.sharedMaterials) if (m != null) materials.Add(m);
        }
        void OnDestroy() { foreach (var mesh in meshes) if (mesh != null) Object.Destroy(mesh); foreach (var mat in materials) if (mat != null) Object.Destroy(mat); }
    }

    public static class NatureLook
    {
        public static Mesh QuadMesh(Vector3 center, Vector2 size, float tileMetres)
        {
            float hx = size.x / 2f, hz = size.y / 2f; Vector2 UV(float x, float z) { return new Vector2((center.x + x) / tileMetres, (center.z + z) / tileMetres); }
            var mesh = new Mesh { name = "Nature quad" };
            mesh.vertices = new[] { new Vector3(-hx, 0, -hz), new Vector3(-hx, 0, hz), new Vector3(hx, 0, hz), new Vector3(hx, 0, -hz) };
            mesh.uv = new[] { UV(-hx, -hz), UV(-hx, hz), UV(hx, hz), UV(hx, -hz) };
            mesh.normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up }; mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 }; mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>A walkable, tiled ground plane (with a collider) for scenes where the player moves on foot.</summary>
        public static GameObject Ground(Transform parent, string name, Vector3 center, Vector2 size, Material material, float tileMetres)
        {
            var o = new GameObject(name); o.transform.SetParent(parent, false); o.transform.localPosition = center;
            o.AddComponent<MeshFilter>().sharedMesh = QuadMesh(center, size, tileMetres); o.AddComponent<MeshRenderer>().sharedMaterial = material;
            var collider = o.AddComponent<BoxCollider>(); collider.center = new Vector3(0, -.25f, 0); collider.size = new Vector3(size.x, .5f, size.y);
            return o;
        }
    }

    /// <summary>
    /// Gives a nature scene its own light, fog and ambient colour while it is on screen, holds the campus day cycle still, and puts
    /// everything back afterwards.
    /// </summary>
    public sealed class NatureAtmosphere
    {
        static readonly List<NatureAtmosphere> stack = new List<NatureAtmosphere>();
        const float FogDensity = .0095f;
        Light sun, campusSun; GameObject sunObject; CampusEnvironment environment; bool applied;
        bool fog; Color fogColor, ambientSky, ambientEquator, ambientGround; float density; FogMode fogMode; AmbientMode ambientMode; bool campusSunWasOn, environmentWasLocked;

        public void Apply(OdysseyGame game, Transform parent)
        {
            if (applied) return;
            applied = true; fog = RenderSettings.fog; fogColor = RenderSettings.fogColor; density = RenderSettings.fogDensity; fogMode = RenderSettings.fogMode;
            ambientMode = RenderSettings.ambientMode; ambientSky = RenderSettings.ambientSkyColor; ambientEquator = RenderSettings.ambientEquatorColor; ambientGround = RenderSettings.ambientGroundColor;
            environment = game != null ? game.environment : null; if (environment != null) { environmentWasLocked = environment.Locked; environment.Locked = true; }
            campusSun = RenderSettings.sun; campusSunWasOn = campusSun != null && campusSun.enabled; if (campusSun != null) campusSun.enabled = false;
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared; RenderSettings.fogColor = NatureKit.Mist; RenderSettings.fogDensity = FogDensity;
            RenderSettings.ambientMode = AmbientMode.Trilight; RenderSettings.ambientSkyColor = new Color(.66f, .78f, .82f);
            RenderSettings.ambientEquatorColor = new Color(.50f, .58f, .46f); RenderSettings.ambientGroundColor = new Color(.26f, .28f, .20f);
            if (sunObject == null)
            {
                sunObject = new GameObject("Nature golden-hour sun"); sunObject.transform.SetParent(parent, false); sunObject.transform.rotation = Quaternion.Euler(36f, -32f, 0f);
                sun = sunObject.AddComponent<Light>(); sun.type = LightType.Directional; sun.color = NatureKit.SunTint; sun.intensity = 1.2f; sun.shadows = LightShadows.Soft; sun.shadowStrength = .8f;
            }
            sunObject.SetActive(true); RenderSettings.sun = sun; stack.Add(this);
        }

        public void Restore()
        {
            if (!applied) return;
            applied = false;
            // Nested scenes may close out of order during teardown. Unwind only from the top.
            while (stack.Count > 0 && !stack[stack.Count - 1].applied)
            {
                var last = stack[stack.Count - 1]; stack.RemoveAt(stack.Count - 1); last.RestoreSettings();
            }
        }
        void RestoreSettings()
        {
            if (sunObject != null) sunObject.SetActive(false);
            RenderSettings.fog = fog; RenderSettings.fogMode = fogMode; RenderSettings.fogColor = fogColor; RenderSettings.fogDensity = density;
            RenderSettings.ambientMode = ambientMode; RenderSettings.ambientSkyColor = ambientSky; RenderSettings.ambientEquatorColor = ambientEquator; RenderSettings.ambientGroundColor = ambientGround;
            if (campusSun != null) campusSun.enabled = campusSunWasOn;
            RenderSettings.sun = campusSun;
            if (environment != null) environment.Locked = environmentWasLocked;
        }
    }
}
