using UnityEngine;
namespace AlbionOdyssey
{
    public sealed class CampusCar : MonoBehaviour
    {
        public float speed; public BoxCollider hull; public Explorer driver;
        public CampusVehicleVisual Visual {get;private set;}
        public void Build(Color color)
        {
            Visual=gameObject.AddComponent<CampusVehicleVisual>();Visual.Build(color);
            hull=gameObject.AddComponent<BoxCollider>();hull.center=new Vector3(0,.78f,0);hull.size=new Vector3(2.2f,1.3f,4.4f);
        }
        public bool Enter(Explorer player)
        {
            if(driver!=null||player.vehicle!=null||Vector3.Distance(player.transform.position,transform.position)>4.8f)return false;
            driver=player;player.vehicle=this;player.body.enabled=false;speed=0;SyncDriver();return true;
        }
        public bool Exit()
        {
            if(driver==null)return true;
            if(Mathf.Abs(speed)>.5f)return false;
            foreach(var offset in new[]{Vector3.left*2.2f,Vector3.right*2.2f,Vector3.back*3.5f,Vector3.forward*3.5f})
            {
                Vector3 p=transform.TransformPoint(offset);p.y=.08f;
                if(Physics.CheckCapsule(p+Vector3.up*.45f,p+Vector3.up*1.5f,.43f,~0,QueryTriggerInteraction.Ignore))continue;
                var old=driver;driver=null;old.vehicle=null;old.Teleport(p);old.transform.rotation=transform.rotation;return true;
            }
            return false;
        }
        public void Drive(float throttle,float steering,bool brake,float dt)
        {
            if(driver==null)return;
            dt=Mathf.Min(dt,.05f);
            speed=Mathf.MoveTowards(speed,brake?0:throttle*(throttle<0?7:19),dt*(brake?28:throttle==0?5:8));
            Quaternion turn=transform.rotation*Quaternion.Euler(0,steering*Mathf.Clamp(speed,-9,9)*4*dt,0);
            Vector3 movement=turn*Vector3.forward*speed*dt;
            hull.enabled=false;
            bool blocked=Physics.BoxCast(transform.position+Vector3.up*.85f,new Vector3(1.1f,.6f,2.2f),movement.normalized,out _,turn,movement.magnitude+.12f,~0,QueryTriggerInteraction.Ignore);
            Vector3 next=transform.position+movement;
            blocked|=Mathf.Abs(next.x)>570||next.z<100||next.z>805;
            blocked|=Physics.CheckBox(next+Vector3.up*.85f,new Vector3(1.1f,.6f,2.2f),turn,~0,QueryTriggerInteraction.Ignore);
            hull.enabled=true;
            if(blocked)speed=0;else {transform.SetPositionAndRotation(next,turn);}
            Visual.Animate(blocked?0:speed*dt,steering,brake,dt);
            SyncDriver();
        }
        void SyncDriver(){driver.transform.position=transform.TransformPoint(new Vector3(-.38f,-.12f,-.05f));driver.transform.rotation=transform.rotation;}
    }
}
