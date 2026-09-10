using UnityEngine;

namespace AlbionOdyssey
{
    /// <summary>
    /// Shared collision-aware movement for the simulated campus population.
    /// Agents remain lightweight transforms, but every step is checked against
    /// the authored campus colliders before it is applied.
    /// </summary>
    public static class CampusStudentNavigation
    {
        public static bool Blocked(Vector3 position,Vector3 direction,float distance,float radius=.30f)
        {
            if(distance<=.001f)return false;
            Vector3 bottom=position+Vector3.up*.36f,top=position+Vector3.up*1.46f;
            if(!Physics.CapsuleCast(bottom,top,radius,direction,out var hit,distance+.05f,~0,QueryTriggerInteraction.Ignore))return false;
            if(hit.collider.GetComponentInParent<CampusStudentIdentity>()!=null)return false;
            return true;
        }

        public static Vector3 FreeWaypoint(Vector3 point)
        {
            foreach(float radius in new[]{0f,2f,4f,6f,9f})
                for(int direction=0;direction<8;direction++)
                {
                    Vector3 candidate=point+Quaternion.Euler(0,direction*45f,0)*Vector3.right*radius;
                    if(!Physics.CheckCapsule(candidate+Vector3.up*.36f,candidate+Vector3.up*1.46f,.30f,~0,QueryTriggerInteraction.Ignore))return candidate+Vector3.up*.08f;
                }
            return point+Vector3.up*.08f;
        }
    }
}
