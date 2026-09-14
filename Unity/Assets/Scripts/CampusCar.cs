using UnityEngine;
namespace AlbionOdyssey
{
    public sealed class CampusCar : MonoBehaviour
    {
        public OdysseyGame Game {get;private set;}
        public int CarId {get;private set;}=-1;
        public VehicleDamage Damage=>Game!=null?Game.state.vehicles[CarId]:temporaryDamage;
        readonly VehicleDamage temporaryDamage=new VehicleDamage();float impactReady;
        public void Configure(OdysseyGame game,int id){Game=game;CarId=id;Visual.ApplyDamage(Damage);}
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
            if(dt<=0)return;dt=Mathf.Min(dt,.05f);
            float limit=Mathf.Lerp(1,.6f,Damage.amount/100f);
            speed=Mathf.MoveTowards(speed,brake?0:throttle*(throttle<0?7:19)*limit,dt*(brake?28:throttle==0?5:8));
            Quaternion turn=transform.rotation*Quaternion.Euler(0,steering*Mathf.Clamp(speed,-9,9)*4*dt,0);
            Vector3 movement=turn*Vector3.forward*speed*dt,center=transform.position+Vector3.up*.85f;
            var half=new Vector3(1.1f,.6f,2.2f);bool blocked=false;RaycastHit wall=default;
            hull.enabled=false;
            try
            {
                // Sweep every contact and select the nearest solid surface; trigger targets cannot hide a wall.
                float nearest=float.MaxValue;
                if(movement.sqrMagnitude>.000001f)foreach(var hit in Physics.BoxCastAll(center,half,movement.normalized,turn,movement.magnitude+.12f,~0,QueryTriggerInteraction.Ignore))
                {
                    if(hit.collider.GetComponentInParent<CampusStudentImpact>()!=null)continue;
                    if(hit.distance<nearest){nearest=hit.distance;wall=hit;blocked=true;}
                }
                Vector3 next=transform.position+movement;
                foreach(var hit in Physics.OverlapBox(next+Vector3.up*.85f,half,turn,~0,QueryTriggerInteraction.Ignore))
                    if(hit.GetComponentInParent<CampusStudentImpact>()==null)blocked=true;
                blocked|=Mathf.Abs(next.x)>570||next.z<100||next.z>805;
                // Only reach students in front of the first wall, including targets already touching the hull.
                if(Mathf.Abs(speed)>=2.5f)
                {
                    float reach=Mathf.Min(movement.magnitude,nearest);
                    var hitStudents=new System.Collections.Generic.HashSet<CampusStudentImpact>();
                    foreach(var hit in Physics.BoxCastAll(center,half,movement.normalized,turn,reach,~0,QueryTriggerInteraction.Collide))
                    {var student=hit.collider.GetComponentInParent<CampusStudentImpact>();if(student!=null&&hit.distance<nearest)hitStudents.Add(student);}
                    foreach(var hit in Physics.OverlapBox(center,half,turn,~0,QueryTriggerInteraction.Collide))
                    {var student=hit.GetComponentInParent<CampusStudentImpact>();if(student!=null)hitStudents.Add(student);}
                    bool struck=false;foreach(var student in hitStudents)struck|=student.KnockDown(movement.normalized*Mathf.Abs(speed),hull);
                    if(struck){Game?.repairs?.ImpactSound(false);if(Game!=null)Game.notice="Student knocked down · recovering";speed*=.7f;}
                }
                if(blocked)
                {
                    if(wall.collider!=null&&Time.time>=impactReady&&Mathf.Abs(speed)>=3)
                    {
                        float strength=Mathf.Abs(speed);Vector3 point=wall.point;
                        if(point==Vector3.zero)point=hull.ClosestPoint(transform.position+movement.normalized*3+Vector3.up*.8f);
                        RecordImpact(point,wall.normal,strength);impactReady=Time.time+.8f;
                    }
                    speed=0;
                }
                else transform.SetPositionAndRotation(next,turn);
            }
            finally{hull.enabled=true;}
            Visual.Animate(blocked?0:speed*dt,steering,brake,dt);
            SyncDriver();
        }
        public void RecordImpact(Vector3 point,Vector3 normal,float velocity)
        {
            if(velocity<3||normal.sqrMagnitude<.1f)return;
            Vector3 p=transform.InverseTransformPoint(point),n=transform.InverseTransformDirection(normal).normalized;
            var dent=new VehicleDent{x=Mathf.Clamp(p.x,-1.1f,1.1f),y=Mathf.Clamp(p.y,.25f,1.4f),z=Mathf.Clamp(p.z,-2.2f,2.2f),nx=n.x,ny=n.y,nz=n.z,depth=Mathf.Clamp(velocity*.027f,.10f,.48f)};
            Damage.Impact(Mathf.Clamp(Mathf.RoundToInt((velocity-2)*4),1,65),dent);Visual.ApplyDamage(Damage);
            if(Game!=null){Game.Save();Game.notice="Collision · body damage "+Damage.amount+"%. Stop and press R for repairs.";Game.repairs?.ImpactSound(true);}
        }
        void SyncDriver(){driver.transform.position=transform.TransformPoint(new Vector3(-.38f,-.12f,-.05f));driver.transform.rotation=transform.rotation;}
    }
}
