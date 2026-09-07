using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace AlbionOdyssey
{
    [Serializable] public class MeshCatalog { public MeshEntry[] assets; }
    [Serializable] public class MeshEntry { public string name; public string[] materials; public int vertices; }
    [Serializable] public class ArchitectureManifest { public ArchitecturePart[] assets; }
    [Serializable] public class ArchitecturePart { public string name; public CollisionBox[] collision_boxes; }
    [Serializable] public class CollisionBox { public string name; public float[] center; public float[] size; }

    public static class TowerGeometry
    {
        public static readonly Dictionary<string,Material> Materials=new Dictionary<string,Material>();
        public static Vector3 Map(float[] a) => new Vector3(a[0],a[2],a[1]);
        public static Material Material(string name,Color color,float metallic=0,float smoothness=.2f)
        {
            if(Materials.TryGetValue(name,out var cached))return cached;
            var m=new Material(Shader.Find("Standard")){name=name,color=color};
            m.SetFloat("_Metallic",metallic); m.SetFloat("_Glossiness",smoothness);
            Materials.Add(name,m); return m;
        }
        static Material Resolve(string name)
        {
            if(Materials.TryGetValue(name,out var m))return m;
            Color c; float metal=0,gloss=.2f;
            switch(name)
            {
                case "M_Architecture_Brick": c=new Color(.75f,.75f,.75f);break;
                case "M_Architecture_Stone":c=new Color(.62f,.61f,.55f);break;
                case "M_Architecture_Floor":c=new Color(.30f,.32f,.33f);break;
                case "M_Architecture_Plaster":c=new Color(.78f,.77f,.70f);break;
                case "M_Architecture_Metal":c=new Color(.035f,.046f,.055f);metal=.7f;gloss=.7f;break;
                case "M_Architecture_Glass":c=new Color(.10f,.22f,.27f,.3f);metal=.25f;gloss=.84f;break;
                case "M_Architecture_Wood":c=new Color(.27f,.12f,.045f);break;
                case "M_Architecture_Grass":c=new Color(.16f,.21f,.095f);break;
                case "M_Architecture_Asphalt":c=new Color(.045f,.05f,.055f);break;
                default:c=new Color(.30f,.14f,.44f);break;
            }
            m=Material(name,c,metal,gloss);
            if(name=="M_Architecture_Brick")
            {
                m.mainTexture=Resources.Load<Texture2D>("Architecture/T_Architecture_Brick_BaseColor");
                m.SetTexture("_BumpMap",Resources.Load<Texture2D>("Architecture/T_Architecture_Brick_Normal"));
                m.EnableKeyword("_NORMALMAP");
            }
            if(name=="M_Architecture_Glass")
            {
                m.SetFloat("_Mode",3); m.SetInt("_SrcBlend",(int)BlendMode.One);
                m.SetInt("_DstBlend",(int)BlendMode.OneMinusSrcAlpha);m.SetInt("_ZWrite",0);
                m.EnableKeyword("_ALPHAPREMULTIPLY_ON");m.renderQueue=3000;
            }
            return m;
        }
        public static GameObject Load()
        {
            Materials.Clear();
            var root=new GameObject("Legacy Hall - actual Blender geometry");
            var catalog=JsonUtility.FromJson<MeshCatalog>(Resources.Load<TextAsset>("Architecture/mesh_catalog").text);
            foreach(var entry in catalog.assets)
            {
                var data=Resources.Load<TextAsset>("Architecture/"+entry.name).bytes;
                using(var reader=new BinaryReader(new MemoryStream(data)))
                {
                    if(new string(reader.ReadChars(4))!="AOM1")throw new InvalidDataException("Invalid mesh header");
                    int count=reader.ReadInt32(),submeshes=reader.ReadInt32();
                    if(count!=entry.vertices||count<0||count>1000000||submeshes!=entry.materials.Length)throw new InvalidDataException("Invalid mesh counts");
                    var positions=new Vector3[count];var normals=new Vector3[count];var uv=new Vector2[count];
                    for(int i=0;i<count;i++)
                    {
                        positions[i]=new Vector3(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());
                        normals[i]=new Vector3(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());
                        uv[i]=new Vector2(reader.ReadSingle(),reader.ReadSingle());
                    }
                    var mesh=new Mesh{name=entry.name,indexFormat=IndexFormat.UInt32};
                    mesh.vertices=positions;mesh.normals=normals;mesh.uv=uv;mesh.subMeshCount=submeshes;
                    for(int s=0;s<submeshes;s++)
                    {
                        int n=reader.ReadInt32();if(n<0||n>count*3)throw new InvalidDataException("Invalid triangle count");
                        var indices=new int[n];for(int i=0;i<n;i++)indices[i]=reader.ReadInt32();
                        mesh.SetTriangles(indices,s);
                    }
                    mesh.RecalculateBounds();mesh.RecalculateTangents();
                    var part=new GameObject(entry.name);part.transform.SetParent(root.transform,false);
                    part.AddComponent<MeshFilter>().sharedMesh=mesh;
                    var mats=new Material[submeshes];for(int i=0;i<submeshes;i++)mats[i]=Resolve(entry.materials[i]);
                    part.AddComponent<MeshRenderer>().sharedMaterials=mats;
                }
            }
            var manifest=JsonUtility.FromJson<ArchitectureManifest>(Resources.Load<TextAsset>("Architecture/architecture_manifest").text);
            foreach(var part in manifest.assets)
            {
                var body=new GameObject(part.name+" collision");body.transform.SetParent(root.transform,false);
                foreach(var box in part.collision_boxes)
                {
                    var collider=body.AddComponent<BoxCollider>();collider.center=Map(box.center);collider.size=Map(box.size);
                }
            }
            return root;
        }
    }
}
