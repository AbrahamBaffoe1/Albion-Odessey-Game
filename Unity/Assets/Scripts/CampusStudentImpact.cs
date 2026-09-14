using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace AlbionOdyssey
{
    // Only fictional, locally simulated campus agents receive this component.
    public sealed class CampusStudentImpact : MonoBehaviour
    {
        public bool IsDown {get;private set;}
        public int BodyCount=>bodies.Count;
        CapsuleCollider target;KeeperAvatar avatar;Animator animator;float immuneUntil;
        readonly List<Rigidbody> bodies=new List<Rigidbody>();
        readonly List<Collider> shapes=new List<Collider>();
        Transform[] bones;Vector3[] restPosition;Quaternion[] restRotation;
        Behaviour walker,activity,label,bubble;bool walkEnabled,activityEnabled;
        Vector3 start;Quaternion facing;
        void Start()
        {
            avatar=GetComponentInChildren<KeeperAvatar>();if(avatar==null)return;
            animator=avatar.GetComponentInChildren<Animator>();
            walker=GetComponent<CampusNpcAgent>();activity=GetComponent<CampusActivityAgent>();
            label=GetComponent<CampusWorldLabel>();bubble=GetComponent<CampusConversationBubble>();
            target=gameObject.AddComponent<CapsuleCollider>();target.isTrigger=true;target.radius=.32f;target.height=1.8f;target.center=Vector3.up*.95f;
        }
        void MakeBody(Transform bone,Transform end,float radius,float mass)
        {
            var body=bone.gameObject.AddComponent<Rigidbody>();body.isKinematic=true;body.mass=mass;body.linearDamping=.5f;body.angularDamping=2;body.maxAngularVelocity=8;body.solverIterations=10;body.solverVelocityIterations=4;
            body.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;
            float scale=Mathf.Max(.001f,Mathf.Abs(bone.lossyScale.x));
            Collider shape;
            if(end==null){var sphere=bone.gameObject.AddComponent<SphereCollider>();sphere.radius=radius/scale;shape=sphere;}
            else{Vector3 offset=bone.InverseTransformPoint(end.position);var capsule=bone.gameObject.AddComponent<CapsuleCollider>();capsule.center=offset*.5f;capsule.direction=Mathf.Abs(offset.x)>Mathf.Abs(offset.y)?(Mathf.Abs(offset.x)>Mathf.Abs(offset.z)?0:2):(Mathf.Abs(offset.y)>Mathf.Abs(offset.z)?1:2);capsule.radius=radius/scale;capsule.height=Mathf.Max(offset.magnitude,2*capsule.radius);shape=capsule;}
            shape.enabled=false;bodies.Add(body);shapes.Add(shape);
        }
        bool BuildRagdoll()
        {
            if(bodies.Count>0)return true;if(animator==null)return false;
            var named=animator.GetComponentsInChildren<Transform>().GroupBy(t=>t.name).ToDictionary(g=>g.Key,g=>g.First());
            string[] from={"pelvis","spine_03","neck_01","upperarm_l","lowerarm_l","upperarm_r","lowerarm_r","thigh_l","calf_l","thigh_r","calf_r"};
            string[] to={null,"neck_01",null,"lowerarm_l","hand_l","lowerarm_r","hand_r","calf_l","foot_l","calf_r","foot_r"};
            foreach(string name in from)if(!named.ContainsKey(name))return false;
            bones=animator.GetComponentsInChildren<Transform>();restPosition=bones.Select(t=>t.localPosition).ToArray();restRotation=bones.Select(t=>t.localRotation).ToArray();
            for(int i=0;i<from.Length;i++)MakeBody(named[from[i]],to[i]==null?null:named[to[i]],i<2?.18f:i==2?.14f:i<7?.085f:.105f,i==0?16:i==1?18:i==2?5:i<7?3:6);
            foreach(var body in bodies)
            {
                var parent=body.transform.parent;while(parent!=null&&parent.GetComponent<Rigidbody>()==null)parent=parent.parent;
                if(parent==null)continue;
                var joint=body.gameObject.AddComponent<CharacterJoint>();joint.connectedBody=parent.GetComponent<Rigidbody>();joint.enableProjection=true;joint.projectionDistance=.08f;joint.projectionAngle=15;
                joint.lowTwistLimit=new SoftJointLimit{limit=-35};joint.highTwistLimit=new SoftJointLimit{limit=35};joint.swing1Limit=new SoftJointLimit{limit=55};joint.swing2Limit=new SoftJointLimit{limit=35};
            }
            for(int i=0;i<shapes.Count;i++)for(int j=i+1;j<shapes.Count;j++)Physics.IgnoreCollision(shapes[i],shapes[j]);
            return true;
        }
        public bool KnockDown(Vector3 velocity,Collider car)
        {
            if(target==null||IsDown||Time.time<immuneUntil||velocity.magnitude<2.5f||!BuildRagdoll())return false;
            IsDown=true;start=transform.position;facing=transform.rotation;walkEnabled=walker!=null&&walker.enabled;activityEnabled=activity!=null&&activity.enabled;
            if(walker!=null)walker.enabled=false;if(activity!=null)activity.enabled=false;if(label!=null)label.enabled=false;if(bubble!=null)bubble.enabled=false;
            target.enabled=false;animator.enabled=false;
            for(int i=0;i<bodies.Count;i++)
            {
                shapes[i].enabled=true;if(car!=null)Physics.IgnoreCollision(shapes[i],car);
                bodies[i].isKinematic=false;bodies[i].linearVelocity=Vector3.ClampMagnitude(velocity*.42f,5.5f)+Vector3.up*1.1f;
                bodies[i].angularVelocity=Vector3.Cross(Vector3.up,velocity.normalized)*2.5f;
            }
            StartCoroutine(Recover(car));return true;
        }
        IEnumerator Recover(Collider car)
        {
            yield return new WaitForSeconds(3.5f);
            Vector3 landing=bodies[0].position;landing.y=start.y;
            foreach(var body in bodies){body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;body.isKinematic=true;}
            foreach(var shape in shapes)shape.enabled=false;
            if(Vector3.Distance(landing,start)>8)landing=start;
            Vector3 safe=CampusStudentNavigation.FreeWaypoint(landing);safe.y=start.y;
            if(Physics.CheckCapsule(safe+Vector3.up*.36f,safe+Vector3.up*1.46f,.3f,~0,QueryTriggerInteraction.Ignore))safe=start;
            var worldPos=bones.Select(t=>t.position).ToArray();var worldRot=bones.Select(t=>t.rotation).ToArray();
            transform.SetPositionAndRotation(safe,facing);avatar.transform.localPosition=Vector3.zero;avatar.transform.localRotation=Quaternion.identity;
            for(int i=0;i<bones.Length;i++){bones[i].position=worldPos[i];bones[i].rotation=worldRot[i];}
            var fallenPos=bones.Select(t=>t.localPosition).ToArray();var fallenRot=bones.Select(t=>t.localRotation).ToArray();
            for(int i=0;i<bones.Length;i++){bones[i].localPosition=restPosition[i];bones[i].localRotation=restRotation[i];}
            animator.enabled=true;animator.Rebind();animator.Play("Locomotion",0,0);animator.Update(0);animator.enabled=false;
            var standingPos=bones.Select(t=>t.localPosition).ToArray();var standingRot=bones.Select(t=>t.localRotation).ToArray();
            for(float t=0;t<1.15f;t+=Time.deltaTime)
            {
                float blend=Mathf.SmoothStep(0,1,t/1.15f);
                for(int i=0;i<bones.Length;i++){bones[i].localPosition=Vector3.Lerp(fallenPos[i],standingPos[i],blend);bones[i].localRotation=Quaternion.Slerp(fallenRot[i],standingRot[i],blend);}
                yield return null;
            }
            for(int i=0;i<bones.Length;i++){bones[i].localPosition=standingPos[i];bones[i].localRotation=standingRot[i];}
            animator.enabled=true;if(walker!=null)walker.enabled=walkEnabled;if(activity!=null)activity.enabled=activityEnabled;if(label!=null)label.enabled=true;if(bubble!=null)bubble.enabled=true;
            foreach(var shape in shapes)if(car!=null)Physics.IgnoreCollision(shape,car,false);
            immuneUntil=Time.time+2;target.enabled=true;IsDown=false;
        }
    }
}
