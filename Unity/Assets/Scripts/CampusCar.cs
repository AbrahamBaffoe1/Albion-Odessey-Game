using UnityEngine;
namespace AlbionOdyssey
{
    public sealed class CampusCar : MonoBehaviour
    {
        public float speed; public BoxCollider hull; public Explorer driver;
        Transform[] wheels=new Transform[4];
        public void Build(Color color)
        {
            var paint=TowerGeometry.Material("Car paint "+name,color,.3f,.65f);var tire=TowerGeometry.Material("Car rubber",new Color(.035f,.04f,.045f));
            var silver=TowerGeometry.Material("Car silver",new Color(.65f,.7f,.72f),.6f,.6f);var seat=TowerGeometry.Material("Car seat",new Color(.09f,.11f,.13f));
            KeeperAvatar.Part(transform,"Chassis",PrimitiveType.Cube,new Vector3(0,.65f,0),new Vector3(2.1f,.55f,4.3f),paint);
            KeeperAvatar.Part(transform,"Bonnet",PrimitiveType.Cube,new Vector3(0,1.0f,1.3f),new Vector3(2.05f,.3f,1.6f),paint);
            KeeperAvatar.Part(transform,"Rear trunk",PrimitiveType.Cube,new Vector3(0,1.02f,-1.5f),new Vector3(2.05f,.4f,1.1f),paint);
            foreach(int side in new[]{-1,1})
            {
                KeeperAvatar.Part(transform,"Door",PrimitiveType.Cube,new Vector3(side*1f,1,0),new Vector3(.14f,.5f,1.8f),paint);
                KeeperAvatar.Part(transform,"Seat",PrimitiveType.Cube,new Vector3(side*.45f,1,-.4f),new Vector3(.68f,.8f,.35f),seat);
                KeeperAvatar.Part(transform,"Headlamp",PrimitiveType.Cube,new Vector3(side*.7f,1,2.13f),new Vector3(.45f,.22f,.06f),silver);
                KeeperAvatar.Part(transform,"Tail lamp",PrimitiveType.Cube,new Vector3(side*.7f,.95f,-2.18f),new Vector3(.4f,.16f,.06f),TowerGeometry.Material("Car tail light",new Color(.7f,.035f,.02f)));
            }
            for(int i=0;i<4;i++)
            {
                var wheel=KeeperAvatar.Part(transform,"Wheel",PrimitiveType.Cylinder,new Vector3(i%2==0?-1.08f:1.08f,.48f,i<2?-1.35f:1.35f),new Vector3(.8f,.16f,.8f),tire);
                wheel.transform.localRotation=Quaternion.Euler(0,0,90);wheels[i]=wheel.transform;
            }
            hull=gameObject.AddComponent<BoxCollider>();hull.center=new Vector3(0,.85f,0);hull.size=new Vector3(2.2f,1.3f,4.4f);
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
            if(blocked)speed=0;else {transform.SetPositionAndRotation(next,turn);foreach(var wheel in wheels)wheel.Rotate(Vector3.up,speed*dt*120,Space.Self);}
            SyncDriver();
        }
        void SyncDriver(){driver.transform.position=transform.TransformPoint(new Vector3(-.45f,.42f,-.05f));driver.transform.rotation=transform.rotation;}
    }
}
