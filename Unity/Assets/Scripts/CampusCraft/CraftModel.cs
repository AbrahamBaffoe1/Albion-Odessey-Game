using System;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
namespace AlbionOdyssey
{
    [Serializable] public class CraftMaterial {public string name,texture;public float[] color;}
    [Serializable] public class CraftSection {public string name;public CollisionBox[] colliders;}
    [Serializable] public class CraftDescription {public CraftMaterial[] materials;public CraftSection[] sections;public string note;}
    public static class CraftModel
    {
        public static Material Surface(string name,Color tint,string texture="",float smooth=.2f)
        {
            var m=TowerGeometry.Material("Craft / "+name,tint,0,smooth);
            if(texture.Length>0&&m.mainTexture==null)
            {
                m.mainTexture=Resources.Load<Texture2D>("CampusCraft/"+texture+"_Color");
                m.SetTexture("_BumpMap",Resources.Load<Texture2D>("CampusCraft/"+texture+"_Normal"));m.EnableKeyword("_NORMALMAP");
            }
            return m;
        }
        public static GameObject Load(string key,CraftSection section,CraftMaterial[] descriptors,Transform parent)
        {
            var asset=Resources.Load<TextAsset>("CampusCraft/"+key);if(asset==null)throw new FileNotFoundException(key);
            var root=new GameObject(key);root.transform.SetParent(parent,false);
            using(var r=new BinaryReader(new MemoryStream(asset.bytes)))
            {
                if(new string(r.ReadChars(4))!="AOM1")throw new InvalidDataException(key);
                int n=r.ReadInt32(),g=r.ReadInt32();if(n<3||n>2000000||g!=descriptors.Length)throw new InvalidDataException("Mesh counts");
                var vertices=new Vector3[n];var normals=new Vector3[n];var uv=new Vector2[n];
                for(int i=0;i<n;i++){vertices[i]=new Vector3(r.ReadSingle(),r.ReadSingle(),r.ReadSingle());normals[i]=new Vector3(r.ReadSingle(),r.ReadSingle(),r.ReadSingle());uv[i]=new Vector2(r.ReadSingle(),r.ReadSingle());}
                var mesh=new Mesh{name=key,indexFormat=IndexFormat.UInt32};mesh.vertices=vertices;mesh.normals=normals;mesh.uv=uv;mesh.subMeshCount=g;
                for(int i=0;i<g;i++){int count=r.ReadInt32();if(count<0||count>n*3)throw new InvalidDataException("Indices");var indices=new int[count];for(int j=0;j<count;j++){indices[j]=r.ReadInt32();if(indices[j]<0||indices[j]>=n)throw new InvalidDataException("Index");}mesh.SetTriangles(indices,i);}
                mesh.RecalculateBounds();mesh.RecalculateTangents();root.AddComponent<MeshFilter>().sharedMesh=mesh;
                var mats=new Material[g];for(int i=0;i<g;i++){var d=descriptors[i];mats[i]=Surface(d.name,new Color(d.color[0],d.color[1],d.color[2]),d.texture??"",d.name.Contains("glass")?.65f:.2f);if(d.name.Contains("glass"))mats[i].SetFloat("_Metallic",.3f);}
                root.AddComponent<MeshRenderer>().sharedMaterials=mats;
            }
            foreach(var b in section.colliders){var c=root.AddComponent<BoxCollider>();c.center=new Vector3(b.center[0],b.center[1],b.center[2]);c.size=new Vector3(b.size[0],b.size[1],b.size[2]);}
            return root;
        }
    }
}
