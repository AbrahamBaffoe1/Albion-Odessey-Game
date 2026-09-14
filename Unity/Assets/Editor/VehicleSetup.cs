using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
namespace AlbionOdyssey.Editor
{
    public sealed class VehicleImporter : AssetPostprocessor
    {
        void OnPreprocessModel()
        {
            if(!assetPath.EndsWith("Vehicles/CampusCoupe.fbx"))return;
            var model=(ModelImporter)assetImporter;model.globalScale=1;model.importAnimation=false;model.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;model.isReadable=true;model.importNormals=ModelImporterNormals.Import;model.importTangents=ModelImporterTangents.CalculateMikk;
        }
        void OnPreprocessTexture()
        {
            if(!assetPath.Contains("/Vehicles/"))return;
            var texture=(TextureImporter)assetImporter;texture.maxTextureSize=1024;
            if(assetPath.EndsWith("_Normal.png"))texture.textureType=TextureImporterType.NormalMap;
        }
    }
    public static class VehicleSetup
    {
        public static void Prepare()
        {
            AssetDatabase.Refresh();const string dir="Assets/Resources/Vehicles/";
            var model=AssetDatabase.LoadAssetAtPath<GameObject>(dir+"CampusCoupe.fbx");if(model==null)throw new Exception("Detailed campus vehicle model missing");
            var root=new GameObject("Campus Coupe");var body=UnityEngine.Object.Instantiate(model,root.transform);body.name="Vehicle geometry";
            var front=body.GetComponentsInChildren<Transform>().First(t=>t.name=="SteerFrontL_L0");var rear=body.GetComponentsInChildren<Transform>().First(t=>t.name=="SteerRearL_L0");
            if(front.position.z<rear.position.z)body.transform.localRotation=Quaternion.Euler(0,180,0);
            foreach(var renderer in body.GetComponentsInChildren<Renderer>())
            {
                var materials=renderer.sharedMaterials;
                for(int i=0;i<materials.Length;i++)
                {
                    string name=materials[i].name;string path=dir+name+".mat";
                    var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
                    if(mat==null){mat=new Material(Shader.Find("Standard")){name=name};AssetDatabase.CreateAsset(mat,path);}
                    mat.color=materials[i].color;mat.enableInstancing=true;mat.SetFloat("_Metallic",name.Contains("Paint")?.65f:name.Contains("Alloy")?.8f:name.Contains("Accent")?.45f:0);mat.SetFloat("_Glossiness",name.Contains("Paint")?.72f:name.Contains("Alloy")?.72f:.25f);
                    if(name.Contains("Glass"))
                    {
                        mat.color=new Color(.30f,.37f,.40f,.12f);mat.SetFloat("_Metallic",0);mat.SetFloat("_Glossiness",.90f);mat.SetFloat("_Mode",3);mat.SetInt("_SrcBlend",(int)BlendMode.One);mat.SetInt("_DstBlend",(int)BlendMode.OneMinusSrcAlpha);mat.SetInt("_ZWrite",0);mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");mat.renderQueue=3000;
                    }
                    if(name.Contains("Rubber")){mat.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(dir+"TireSide_Normal.png"));mat.SetFloat("_BumpScale",.5f);mat.EnableKeyword("_NORMALMAP");mat.SetFloat("_Glossiness",.18f);}
                    if(name.Contains("Headlamp")||name.Contains("Taillamp")){mat.EnableKeyword("_EMISSION");mat.SetColor("_EmissionColor",name.Contains("Headlamp")?new Color(.28f,.34f,.4f):new Color(.3f,.003f,.001f));}
                    EditorUtility.SetDirty(mat);materials[i]=mat;
                }
                renderer.sharedMaterials=materials;
            }
            var lod=root.AddComponent<LODGroup>();var levels=new LOD[3];float[] heights={.25f,.085f,.012f};
            for(int i=0;i<3;i++)
            {
                var node=body.GetComponentsInChildren<Transform>(true).First(t=>t.name=="LOD"+i);node.gameObject.SetActive(true);
                levels[i]=new LOD(heights[i],node.GetComponentsInChildren<Renderer>(true));
            }
            lod.SetLODs(levels);lod.RecalculateBounds();lod.animateCrossFading=false;
            PrefabUtility.SaveAsPrefabAsset(root,dir+"CampusCoupe.prefab");UnityEngine.Object.DestroyImmediate(root);AssetDatabase.SaveAssets();Debug.Log("VEHICLE_PREFAB_OK: detailed geometry, 3 LODs, glass, paint, wheel pivots");
        }
    }
}
