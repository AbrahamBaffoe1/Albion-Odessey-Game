using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

namespace AlbionOdyssey.Editor
{
    // Deterministic build step: raw Blender exports remain the source of truth.
    // Prepared meshes/prefabs are rebuilt before every player build, not generated at runtime.
    public static class ArchitecturePreparation
    {
        const string Folder="Assets/Resources/PreparedArchitecture";
        public static void Prepare()
        {
            Directory.CreateDirectory(Folder+"/Textures");Directory.CreateDirectory(Folder+"/Materials");
            foreach(var file in Directory.GetFiles("Assets/Resources/CampusCraft","*_Rough.jpg")) PackRoughness(file);
            AssetDatabase.Refresh();
            foreach(string building in new[]{"ferguson","robinson"})
            {
                var data=JsonUtility.FromJson<CraftDescription>(File.ReadAllText("Assets/Resources/CampusCraft/"+building+".json"));
                foreach(var section in data.sections) PrepareSection(building+"-"+section.name,section,data.materials);
            }
            AssetDatabase.SaveAssets();AssetDatabase.Refresh();
            Debug.Log("ARCHITECTURE_PREPARED: persisted meshes, secondary UVs, materials, and unchanged collision volumes");
        }
        static void PackRoughness(string file)
        {
            string key=Path.GetFileNameWithoutExtension(file).Replace("_Rough","");
            var source=new Texture2D(2,2,TextureFormat.RGBA32,false,true);
            if(!source.LoadImage(File.ReadAllBytes(file)))throw new InvalidDataException(file);
            var pixels=source.GetPixels32();for(int i=0;i<pixels.Length;i++)pixels[i]=new Color32(0,0,0,(byte)(255-pixels[i].r));
            source.SetPixels32(pixels);source.Apply();string path=Folder+"/Textures/"+key+"_MetallicSmoothness.png";
            File.WriteAllBytes(path,source.EncodeToPNG());UnityEngine.Object.DestroyImmediate(source);
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.sRGBTexture=false;importer.alphaSource=TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency=false;importer.wrapMode=TextureWrapMode.Repeat;importer.maxTextureSize=2048;importer.SaveAndReimport();
        }
        static void PrepareSection(string key,CraftSection section,CraftMaterial[] descriptors)
        {
            var root=CraftModel.LoadRaw(key,section,descriptors,null);
            try
            {
                var filter=root.GetComponent<MeshFilter>();var mesh=filter.sharedMesh;
                var originalBounds=mesh.bounds;int originalTriangles=mesh.triangles.Length;
                UnwrapParam.SetDefaults(out var unwrap);unwrap.packMargin=.006f;
                if(!Unwrapping.GenerateSecondaryUVSet(mesh,unwrap)||mesh.uv2.Length!=mesh.vertexCount)throw new InvalidDataException("Lightmap UVs: "+key);
                if(mesh.triangles.Length!=originalTriangles||(mesh.bounds.size-originalBounds.size).sqrMagnitude>.000001f)throw new InvalidDataException("Geometry changed during preparation: "+key);
                string meshPath=Folder+"/"+key+".asset";var previous=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                if(previous==null)AssetDatabase.CreateAsset(mesh,meshPath);
                else {EditorUtility.CopySerialized(mesh,previous);UnityEngine.Object.DestroyImmediate(mesh);filter.sharedMesh=previous;}
                var renderer=root.GetComponent<MeshRenderer>();var materials=renderer.sharedMaterials;
                for(int i=0;i<materials.Length;i++)
                {
                    string path=Folder+"/Materials/"+key+"-"+i+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
                    if(material==null){material=new Material(materials[i]);AssetDatabase.CreateAsset(material,path);}
                    else EditorUtility.CopySerialized(materials[i],material);
                    materials[i]=material;
                }
                renderer.sharedMaterials=materials;renderer.shadowCastingMode=ShadowCastingMode.On;renderer.receiveShadows=true;
                renderer.lightProbeUsage=LightProbeUsage.BlendProbes;renderer.reflectionProbeUsage=ReflectionProbeUsage.BlendProbes;
                // Readiness for a placed, baked scene; dynamic campus lighting is retained until actual lightmaps exist.
                GameObjectUtility.SetStaticEditorFlags(root,StaticEditorFlags.BatchingStatic);
                if(key=="ferguson-exterior")
                {
                    var lods=new LOD[3];lods[0]=new LOD(.22f,new Renderer[]{renderer});
                    for(int level=1;level<=2;level++)
                    {
                        string lodKey=key+"_lod"+level;
                        var child=CraftModel.LoadRaw(lodKey,new CraftSection{name=lodKey,colliders=new CollisionBox[0]},descriptors,root.transform);
                        var lodFilter=child.GetComponent<MeshFilter>();var lodMesh=lodFilter.sharedMesh;
                        string path=Folder+"/"+lodKey+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                        if(saved==null)AssetDatabase.CreateAsset(lodMesh,path);
                        else {EditorUtility.CopySerialized(lodMesh,saved);UnityEngine.Object.DestroyImmediate(lodMesh);lodFilter.sharedMesh=saved;}
                        var lodRenderer=child.GetComponent<MeshRenderer>();lodRenderer.sharedMaterials=materials;
                        lods[level]=new LOD(level==1?.08f:.002f,new Renderer[]{lodRenderer});
                    }
                    var group=root.AddComponent<LODGroup>();group.SetLODs(lods);group.RecalculateBounds();
                }
                PrefabUtility.SaveAsPrefabAsset(root,Folder+"/"+key+".prefab");
            }
            finally {UnityEngine.Object.DestroyImmediate(root);}
        }
    }
}
