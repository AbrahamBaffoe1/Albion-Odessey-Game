using System;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
namespace AlbionOdyssey
{
    [Serializable] public sealed class TourRoomMaterial { public string name; public float[] color; }
    [Serializable] public sealed class TourRoomModel { public TourRoomMaterial[] materials; public CollisionBox[] colliders; }
    public static class TourRoom
    {
        public static GameObject Build(Vector3 origin)
        {
            var model=JsonUtility.FromJson<TourRoomModel>(Resources.Load<TextAsset>("CampusTour/wesley-model").text);
            var data=Resources.Load<TextAsset>("CampusTour/wesley-mesh").bytes;
            var root=new GameObject("Wesley dorm study · evaluated Blender mesh");root.transform.position=origin;
            using(var r=new BinaryReader(new MemoryStream(data)))
            {
                if(new string(r.ReadChars(4))!="AOT1")throw new InvalidDataException("Invalid room mesh");
                int count=r.ReadInt32(),groups=r.ReadInt32();if(count<3||count>1000000||groups!=model.materials.Length)throw new InvalidDataException("Invalid room counts");
                var positions=new Vector3[count];var normals=new Vector3[count];
                for(int i=0;i<count;i++){positions[i]=new Vector3(r.ReadSingle(),r.ReadSingle(),r.ReadSingle());normals[i]=new Vector3(r.ReadSingle(),r.ReadSingle(),r.ReadSingle());}
                var mesh=new Mesh{name="Wesley · actual Blender mesh",indexFormat=IndexFormat.UInt32};mesh.vertices=positions;mesh.normals=normals;mesh.subMeshCount=groups;
                for(int g=0;g<groups;g++){int n=r.ReadInt32();if(n<0||n>count*3)throw new InvalidDataException("Invalid room triangles");var indices=new int[n];for(int i=0;i<n;i++){indices[i]=r.ReadInt32();if(indices[i]<0||indices[i]>=count)throw new InvalidDataException("Invalid vertex index");}mesh.SetTriangles(indices,g);}
                mesh.RecalculateBounds();root.AddComponent<MeshFilter>().sharedMesh=mesh;
                var materials=new Material[groups];for(int i=0;i<groups;i++){var m=model.materials[i];materials[i]=TowerGeometry.Material("Wesley "+m.name,new Color(m.color[0],m.color[1],m.color[2]),0,.2f);}
                root.AddComponent<MeshRenderer>().sharedMaterials=materials;
            }
            foreach(var b in model.colliders){var o=new GameObject(b.name+" collision");o.transform.SetParent(root.transform,false);var c=o.AddComponent<BoxCollider>();c.center=new Vector3(b.center[0],b.center[1],b.center[2]);c.size=new Vector3(b.size[0],b.size[1],b.size[2]);}
            return root;
        }
    }
}
