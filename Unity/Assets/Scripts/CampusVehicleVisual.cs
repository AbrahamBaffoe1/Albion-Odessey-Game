using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace AlbionOdyssey
{
    public sealed class CampusVehicleVisual : MonoBehaviour
    {
        static GameObject prefab;
        readonly List<Transform> spinning=new List<Transform>(), steering=new List<Transform>();
        readonly List<Transform> cabinSteering=new List<Transform>();
        readonly Dictionary<Transform,Quaternion> bind=new Dictionary<Transform,Quaternion>();
        readonly Dictionary<Transform,Vector3> axes=new Dictionary<Transform,Vector3>();
        readonly List<Renderer> lamps=new List<Renderer>();
        MaterialPropertyBlock properties;float roll,angle;bool braking;
        public int WheelPivotCount=>spinning.Count;
        public LODGroup Detail {get;private set;}
        public bool BrakeLightsOn=>braking;
        public void Build(Color color)
        {
            if(prefab==null)prefab=Resources.Load<GameObject>("Vehicles/CampusCoupe");
            if(prefab==null)throw new InvalidOperationException("Detailed campus coupe is missing from this build.");
            var model=Instantiate(prefab,transform,false);model.name="Detailed campus coupe";Detail=model.GetComponent<LODGroup>();properties=new MaterialPropertyBlock();
            foreach(var t in model.GetComponentsInChildren<Transform>())
            {
                if(t.name.StartsWith("Wheel")&&t.name.Contains("_L")){spinning.Add(t);bind[t]=t.localRotation;axes[t]=t.InverseTransformDirection(transform.right);}
                if(t.name.StartsWith("SteerFront")){steering.Add(t);bind[t]=t.localRotation;axes[t]=t.InverseTransformDirection(transform.up);}
                if(t.name.StartsWith("CabinSteering_L")){cabinSteering.Add(t);bind[t]=t.localRotation;axes[t]=t.InverseTransformDirection(transform.TransformDirection(new Vector3(0,.4f,1).normalized));}
            }
            foreach(var r in model.GetComponentsInChildren<Renderer>())
            {
                bool hasLamp=false;for(int i=0;i<r.sharedMaterials.Length;i++)
                {
                    string name=r.sharedMaterials[i].name;
                    if(name=="Vehicle Paint"){properties.Clear();properties.SetColor("_Color",color);r.SetPropertyBlock(properties,i);}
                    if(name.Contains("Taillamp"))hasLamp=true;
                }
                if(hasLamp)lamps.Add(r);
            }
        }
        readonly Dictionary<MeshFilter,Mesh> originals=new Dictionary<MeshFilter,Mesh>();
        readonly List<Mesh> dentedMeshes=new List<Mesh>();
        public int DeformedVertexCount {get;private set;}
        public void ApplyDamage(VehicleDamage damage)
        {
            foreach(var entry in originals)if(entry.Key!=null)entry.Key.sharedMesh=entry.Value;
            foreach(var mesh in dentedMeshes)Destroy(mesh);dentedMeshes.Clear();DeformedVertexCount=0;
            if(damage.amount==0)return;
            foreach(var filter in GetComponentsInChildren<MeshFilter>(true))
            {
                if(!filter.name.StartsWith("Body_Geometry_L"))continue;
                if(!originals.ContainsKey(filter))originals[filter]=filter.sharedMesh;
                Mesh mesh=Instantiate(originals[filter]);mesh.name="Individual coupe damage";var vertices=mesh.vertices;
                for(int i=0;i<vertices.Length;i++)
                {
                    Vector3 point=transform.InverseTransformPoint(filter.transform.TransformPoint(vertices[i])),offset=Vector3.zero;
                    foreach(var dent in damage.dents)
                    {
                        float d=Vector3.Distance(point,new Vector3(dent.x,dent.y,dent.z));float influence=Mathf.Clamp01(1-d/1.05f);
                        offset+=new Vector3(dent.nx,dent.ny,dent.nz)*(dent.depth*influence*influence);
                    }
                    offset=Vector3.ClampMagnitude(offset,.52f);if(offset.sqrMagnitude>.000001f)DeformedVertexCount++;
                    vertices[i]=filter.transform.InverseTransformPoint(transform.TransformPoint(point+offset));
                }
                mesh.vertices=vertices;mesh.RecalculateNormals();mesh.RecalculateBounds();filter.sharedMesh=mesh;dentedMeshes.Add(mesh);
            }
        }
        void OnDestroy(){foreach(var mesh in dentedMeshes)if(mesh!=null)Destroy(mesh);}
        public void Animate(float distance,float turn,bool brake,float dt)
        {
            roll=(roll+distance/.465f*Mathf.Rad2Deg)%360;
            angle=Mathf.MoveTowards(angle,turn*29,dt*110);
            foreach(var t in spinning)t.localRotation=bind[t]*Quaternion.AngleAxis(roll,axes[t]);
            foreach(var t in steering)t.localRotation=bind[t]*Quaternion.AngleAxis(angle,axes[t]);
            foreach(var t in cabinSteering)t.localRotation=bind[t]*Quaternion.AngleAxis(-angle*4,axes[t]);
            if(braking==brake)return;braking=brake;
            foreach(var r in lamps)for(int i=0;i<r.sharedMaterials.Length;i++)if(r.sharedMaterials[i].name.Contains("Taillamp")){properties.Clear();properties.SetColor("_EmissionColor",new Color(brake?3f:.3f,.005f,.002f));r.SetPropertyBlock(properties,i);}
        }
    }
    public sealed class CampusVehicleReflections : MonoBehaviour
    {
        IEnumerator Start()
        {
            var game=GetComponent<OdysseyGame>();while(!game.Ready)yield return null;
            // Capture once after construction, never six extra camera renders per frame.
            var host=new GameObject("Campus vehicle reflections");host.transform.position=CampusExpansion.Find("26").position+Vector3.up*3;
            var probe=host.AddComponent<ReflectionProbe>();probe.mode=ReflectionProbeMode.Realtime;probe.refreshMode=ReflectionProbeRefreshMode.ViaScripting;probe.timeSlicingMode=ReflectionProbeTimeSlicingMode.IndividualFaces;
            probe.resolution=128;probe.size=new Vector3(1200,120,1200);probe.boxProjection=false;probe.cullingMask=~((1<<29)|(1<<30));probe.farClipPlane=190;probe.clearFlags=ReflectionProbeClearFlags.Skybox;probe.RenderProbe();
        }
    }
}
