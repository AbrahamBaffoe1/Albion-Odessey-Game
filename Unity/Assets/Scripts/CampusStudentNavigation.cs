using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace AlbionOdyssey
{
    public static class CampusStudentNavigation
    {
        public const float Radius=.38f;
        static NavMeshData data;
        static NavMeshDataInstance instance;
        public static bool Ready=>instance.valid;
        static bool Solid(Collider collider)=>collider.GetComponentInParent<CampusStudentIdentity>()==null;
        public static bool Occupied(Vector3 p,float radius=Radius)
        {
            foreach(var collider in Physics.OverlapCapsule(p+Vector3.up*.45f,p+Vector3.up*1.48f,radius,~0,QueryTriggerInteraction.Ignore))
                if(Solid(collider))return true;
            return false;
        }
        public static bool Blocked(Vector3 position,Vector3 direction,float distance,float radius=Radius)
        {
            if(Occupied(position,radius))return true; // Casts alone do not detect initial penetration.
            if(distance<=.001f)return false;
            foreach(var hit in Physics.CapsuleCastAll(position+Vector3.up*.45f,position+Vector3.up*1.48f,radius,direction.normalized,distance+.05f,~0,QueryTriggerInteraction.Ignore))
                if(Solid(hit.collider))return true;
            return Occupied(position+direction.normalized*distance,radius);
        }
        public static bool TryFreeWaypoint(Vector3 point,out Vector3 result)
        {
            Physics.SyncTransforms();
            for(float radius=0;radius<=40;radius+=2)
                for(int direction=0;direction<(radius==0?1:24);direction++)
                {
                    Vector3 candidate=point+Quaternion.Euler(0,direction*15f,0)*Vector3.right*radius;
                    if(!Physics.Raycast(candidate+Vector3.up*1.5f,Vector3.down,out var ground,2.5f,~0,QueryTriggerInteraction.Ignore)||ground.normal.y<.8f)continue;
                    candidate.y=ground.point.y+.08f;
                    if(!Occupied(candidate)){result=candidate;return true;}
                }
            result=point;return false;
        }
        public static Vector3 ExteriorWaypoint(Vector3 point)
        {
            foreach(var place in CampusCatalog.Places)
            {
                var hall=CampusBuildings.Instance?.Building(place.id);
                float width=hall!=null?hall.Width:place.width,depth=hall!=null?hall.Depth:place.depth;
                if(Mathf.Abs(point.x-place.position.x)<width/2+1&&Mathf.Abs(point.z-place.position.z)<depth/2+1)
                    return FreeWaypoint(place.position+Vector3.back*(depth/2+4));
            }
            return FreeWaypoint(point);
        }
        public static Vector3 FreeWaypoint(Vector3 point)
        {
            if(TryFreeWaypoint(point,out var safe))return safe;
            // Never silently spawn at the blocked request if its neighbourhood has no clearance.
            if(TryFreeWaypoint(CampusCatalog.Point(405,238),out safe))return safe;
            throw new System.InvalidOperationException("No collision-free student spawn available");
        }
        public static void BuildPaths()
        {
            if(instance.valid)instance.Remove();if(data!=null)Object.Destroy(data);
            Physics.SyncTransforms();var sources=new List<NavMeshBuildSource>();
            var bounds=new Bounds(new Vector3(-350,35,430),new Vector3(1900,100,800));
            NavMeshBuilder.CollectSources(bounds,~0,NavMeshCollectGeometry.PhysicsColliders,0,new List<NavMeshBuildMarkup>(),sources);
            sources.RemoveAll(s=>s.component!=null&&(s.component.GetComponentInParent<CampusStudentIdentity>()!=null||s.component.GetComponentInParent<CharacterController>()!=null||s.component.GetComponentInParent<CampusCar>()!=null));
            var settings=NavMesh.GetSettingsByIndex(0);settings.agentRadius=.48f;settings.agentHeight=1.95f;settings.agentClimb=.25f;settings.agentSlope=40;settings.overrideVoxelSize=true;settings.voxelSize=.16f;
            data=NavMeshBuilder.BuildNavMeshData(settings,sources,bounds,Vector3.zero,Quaternion.identity);
            if(data!=null)instance=NavMesh.AddNavMeshData(data);
            Debug.Log("CAMPUS_NAVIGATION_READY "+Ready+" sources="+sources.Count);
        }
    }
    // A path per student, with live sweeps guarding doors, vehicles and changed geometry.
    public sealed class CampusStudentPath
    {
        readonly NavMeshPath path=new NavMeshPath();Vector3 destination;Vector3[] corners;int corner;float retryAt;
        public bool Move(Transform student,Vector3 goal,float distance)
        {
            if(!CampusStudentNavigation.Ready||distance<=0)return false;
            if(corners==null||(goal-destination).sqrMagnitude>.25f)
            {
                if(Time.time<retryAt)return false;
                destination=goal;retryAt=Time.time+1.5f;
                if(!NavMesh.SamplePosition(student.position,out var start,1f,NavMesh.AllAreas)||!NavMesh.SamplePosition(goal,out var end,2f,NavMesh.AllAreas)||Mathf.Abs(start.position.y-student.position.y)>.5f||Mathf.Abs(end.position.y-goal.y)>.5f||!NavMesh.CalculatePath(start.position,end.position,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete)return false;
                corners=path.corners;corner=1;
            }
            while(corner<corners.Length&&Vector2.Distance(new Vector2(corners[corner].x,corners[corner].z),new Vector2(student.position.x,student.position.z))<.045f)corner++;
            if(corner>=corners.Length){corners=null;return false;}
            Vector3 delta=corners[corner]-student.position;delta.y=0;
            float step=Mathf.Min(distance,delta.magnitude);
            if(CampusStudentNavigation.Blocked(student.position,delta.normalized,step)){corners=null;retryAt=Time.time+1;return false;}
            Vector3 next=student.position+delta.normalized*step;
            if(!Physics.Raycast(next+Vector3.up*.5f,Vector3.down,out var floor,1f,~0,QueryTriggerInteraction.Ignore))return false;
            next.y=floor.point.y+.08f;if(CampusStudentNavigation.Occupied(next))return false;
            student.position=next;student.rotation=Quaternion.Slerp(student.rotation,Quaternion.LookRotation(delta),Time.deltaTime*5);return true;
        }
    }
}
