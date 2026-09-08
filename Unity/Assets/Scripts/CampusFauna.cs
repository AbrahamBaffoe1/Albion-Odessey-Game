using System;
using System.Collections.Generic;
using UnityEngine;

namespace AlbionOdyssey
{
    /// <summary>
    /// Bounded, animated campus wildlife. Squirrels use a small deterministic
    /// habitat graph around the authored campus trees, so they feel alive
    /// without introducing a per-frame NavMesh query or unbounded spawning.
    /// </summary>
    public sealed class CampusFauna : MonoBehaviour
    {
        public const int Population=18;
        public readonly List<SquirrelAgent> Squirrels=new List<SquirrelAgent>();
        readonly List<Vector3> habitat=new List<Vector3>();
        readonly System.Random random=new System.Random(1835);
        OdysseyGame game;
        Material[] coats;

        public int ActiveCount { get { int n=0; foreach(var squirrel in Squirrels) if(squirrel!=null&&squirrel.isActiveAndEnabled)n++; return n; } }
        public int HabitatCount=>habitat.Count;

        public void Setup(OdysseyGame owner)
        {
            game=owner;
            BuildHabitat();
            BuildSquirrels();
        }

        void BuildHabitat()
        {
            habitat.Clear();
            // The tree list comes from the same seeded layout used by the
            // campus renderer. Add four safe points around each tree, then
            // retain a compact graph so agents always stay near cover.
            foreach(var tree in CampusGeometry.HabitatTrees)
            {
                for(int i=0;i<4;i++)
                {
                    float angle=i*Mathf.PI*.5f+((tree.x+tree.z)*.017f);
                    habitat.Add(tree+new Vector3(Mathf.Cos(angle)*3.8f,0,Mathf.Sin(angle)*3.8f));
                }
            }
        }

        void BuildSquirrels()
        {
            // One shared warm ivory coat keeps the population visually
            // coherent as the same leucistic eastern-gray species shown in
            // the supplied reference. The pale chest, ruby eye and pink nose
            // preserve its living-animal cues without using a photo texture.
            var ivory=TowerGeometry.Material("Squirrel coat leucistic ivory",new Color(.88f,.85f,.77f),0,.62f);
            coats=new[]{ivory};
            var cream=TowerGeometry.Material("Squirrel warm white underside",new Color(.96f,.92f,.82f),0,.72f);
            var pink=TowerGeometry.Material("Squirrel pink ears and paws",new Color(.72f,.33f,.34f),0,.58f);
            var dark=TowerGeometry.Material("Squirrel ruby eyes",new Color(.16f,.012f,.018f),0,.90f);
            var nose=TowerGeometry.Material("Squirrel rose nose",new Color(.45f,.10f,.12f),0,.68f);
            for(int i=0;i<Population;i++)
            {
                int habitatIndex=(i*17+3)%Mathf.Max(1,habitat.Count);
                Vector3 start=habitat.Count==0?new Vector3(0,.1f,100):habitat[habitatIndex];
                var root=new GameObject("Campus squirrel "+(i+1).ToString("00"));
                root.transform.position=start+Vector3.up*.12f;
                var agent=root.AddComponent<SquirrelAgent>();
                agent.Configure(this,habitatIndex,i,coats[0],cream,dark,nose,random.Next(0,2),pink);
                Squirrels.Add(agent);
            }
        }

        public Vector3 PickHabitat(int current,int salt)
        {
            if(habitat.Count==0)return new Vector3(0,0,100);
            int stride=5+(salt%7); int index=(current+stride+salt*3)%habitat.Count;
            return habitat[index];
        }

        public Vector3 CurrentTree(int index)
        {
            if(CampusGeometry.HabitatTrees.Count==0)return new Vector3(0,0,100);
            return CampusGeometry.HabitatTrees[Mathf.Abs(index)%CampusGeometry.HabitatTrees.Count];
        }

        public bool IsSafePoint(Vector3 point,float radius=.38f)
        {
            if(!Physics.Raycast(point+Vector3.up*8,Vector3.down,out var ground,20,~0,QueryTriggerInteraction.Ignore))return false;
            if(Mathf.Abs(ground.point.y)>1.2f)return false;
            // Static campus meshes can overlap the perimeter of a tree's
            // canopy. Let the agent route around those meshes at runtime;
            // rejecting the waypoint here would strand a whole grove.
            return true;
        }
    }

    public sealed class SquirrelAgent : MonoBehaviour
    {
        enum Activity { Forage, Run, Rest, Climb }
        CampusFauna fauna; Transform body,head,tail,tailMid,tailTip,frontLeft,frontRight,backLeft,backRight;
        Material coat,cream,dark,nose,pink; int habitatIndex,variant,seed,decisionCount; float speed,phase,nextDecision;
        Vector3 target; Activity activity; bool built;
        public float DistanceTravelled {get;private set;}
        public bool IsNearTree
        {
            get
            {
                foreach(var tree in CampusGeometry.HabitatTrees)
                    if(Vector3.Distance(transform.position,tree)<9)return true;
                return false;
            }
        }
        public void ResetForVerification()
        {
            if(fauna.HabitatCount==0)return;
            transform.position=fauna.PickHabitat(0,seed)+Vector3.up*.12f;
            target=transform.position;activity=Activity.Forage;nextDecision=Time.time+.2f;DistanceTravelled=0;
        }
        public string ActivityName=>activity.ToString();

        public void Configure(CampusFauna owner,int start,int identity,Material fur,Material muzzle,Material eyes,Material snout,int style,Material pawColor)
        {
            fauna=owner;habitatIndex=start;seed=identity;coat=fur;cream=muzzle;dark=eyes;nose=snout;variant=style;pink=pawColor;phase=identity*.83f;BuildModel();
            target=transform.position;activity=Activity.Forage;nextDecision=Time.time+1.2f+(identity*.07f);built=true;
        }

        static GameObject Part(Transform parent,string name,PrimitiveType shape,Vector3 at,Vector3 scale,Material material)
            =>KeeperAvatar.Part(parent,name,shape,at,scale,material,false);

        void BuildModel()
        {
            var root=new GameObject("Squirrel model").transform;root.SetParent(transform,false);
            root.localPosition=Vector3.up*.14f;
            body=root;
            var torso=Part(root,"Squirrel long torso",PrimitiveType.Capsule,new Vector3(0,.40f,0),new Vector3(.34f,.25f,.52f),coat).transform;
            torso.localRotation=Quaternion.Euler(90,0,0);
            Part(root,"Squirrel cream belly",PrimitiveType.Sphere,new Vector3(0,.39f,.19f),new Vector3(.25f,.20f,.24f),cream);
            Part(root,"Squirrel hind haunch",PrimitiveType.Sphere,new Vector3(0,.43f,-.18f),new Vector3(.34f,.30f,.30f),coat);
            head=Part(root,"Squirrel alert head",PrimitiveType.Sphere,new Vector3(0,.73f,.36f),new Vector3(.27f,.24f,.27f),coat).transform;
            var muzzle=Part(head,"Pointed cream muzzle",PrimitiveType.Capsule,new Vector3(0,-.01f,.22f),new Vector3(.15f,.12f,.22f),cream).transform;
            muzzle.localRotation=Quaternion.Euler(90,0,0);
            Part(head,"Nose",PrimitiveType.Sphere,new Vector3(0,-.01f,.35f),new Vector3(.055f,.045f,.045f),nose);
            for(int side=-1;side<=1;side+=2)
            {
                Part(head,"Rounded ear",PrimitiveType.Sphere,new Vector3(side*.18f,.16f,.02f),new Vector3(.13f,.17f,.09f),coat);
                Part(head,"Pink inner ear",PrimitiveType.Sphere,new Vector3(side*.18f,.16f,.085f),new Vector3(.075f,.10f,.025f),pink);
                var tuft=Part(head,"Ear tuft",PrimitiveType.Capsule,new Vector3(side*.18f,.29f,.02f),new Vector3(.045f,.16f,.045f),coat).transform;
                tuft.localRotation=Quaternion.Euler(0,0,-side*15);
                Part(head,"Bright eye",PrimitiveType.Sphere,new Vector3(side*.10f,.055f,.25f),new Vector3(.041f,.048f,.028f),dark);
                for(int whisker=0;whisker<2;whisker++)
                {
                    var line=Part(head,"Whisker",PrimitiveType.Capsule,new Vector3(side*(.12f+.018f*whisker),-.035f,.39f+.025f*whisker),new Vector3(.008f,.12f,.008f),cream).transform;
                    line.localRotation=Quaternion.Euler(0,side*(18+whisker*12),90);
                }
            }
            tail=Part(root,"Bushy tail base",PrimitiveType.Capsule,new Vector3(0,.63f,-.34f),new Vector3(.25f,.34f,.23f),coat).transform;
            tail.localRotation=Quaternion.Euler(-28,0,0);
            tailMid=Part(root,"Bushy tail middle",PrimitiveType.Capsule,new Vector3(0,.96f,-.43f),new Vector3(.29f,.42f,.26f),coat).transform;
            tailMid.localRotation=Quaternion.Euler(-42,0,0);
            tailTip=Part(root,"Bushy tail tip",PrimitiveType.Capsule,new Vector3(0,1.30f,-.29f),new Vector3(.23f,.38f,.21f),coat).transform;
            tailTip.localRotation=Quaternion.Euler(28,0,0);
            frontLeft=Leg(root,"Front left",-.16f);frontRight=Leg(root,"Front right",.16f);
            backLeft=Leg(root,"Back left",-.19f);backRight=Leg(root,"Back right",.19f);
            // A subtle pale throat catches the light without changing the
            // single natural coat color shared by every campus squirrel.
            Part(root,"Pale throat",PrimitiveType.Sphere,new Vector3(0,.61f,.27f),new Vector3(.17f,.16f,.13f),cream);
        }

        Transform Leg(Transform root,string name,float side)
        {
            var leg=Part(root,name,PrimitiveType.Capsule,new Vector3(side,.27f,.08f),new Vector3(.09f,.20f,.09f),coat).transform;
            Part(leg,"Pink paw",PrimitiveType.Sphere,new Vector3(0,-.19f,.06f),new Vector3(.11f,.07f,.16f),pink);return leg;
        }

        void Update()
        {
            if(!built||fauna==null)return;
            if(Time.time>=nextDecision)ChooseNext();
            float oldSpeed=speed;
            if(activity==Activity.Rest){speed=0;transform.rotation=Quaternion.Euler(0,transform.eulerAngles.y,0);}
            else
            {
                speed=activity==Activity.Run?2.35f:activity==Activity.Climb?1.05f:1.45f;
                MoveTowardTarget(speed);
            }
            Animate(oldSpeed);
        }

        void ChooseNext()
        {
            int choice=(seed+decisionCount++)%10;
            if(choice==0){activity=Activity.Rest;target=transform.position;nextDecision=Time.time+1.7f+(seed%3)*.4f;return;}
            activity=choice%4==0?Activity.Climb:choice%3==0?Activity.Run:Activity.Forage;
            int nextIndex=habitatIndex;
            if(activity==Activity.Climb)
            {
                Vector3 tree=fauna.CurrentTree(habitatIndex);
                float angle=(seed+Mathf.FloorToInt(Time.time))*.8f;
                target=tree+new Vector3(Mathf.Cos(angle)*1.15f,2.35f,Mathf.Sin(angle)*1.15f);
            }
            else target=fauna.PickHabitat(habitatIndex,seed+Mathf.FloorToInt(Time.time));
            if(activity!=Activity.Climb&&!fauna.IsSafePoint(target))target=fauna.PickHabitat(nextIndex+1,seed+2);
            nextDecision=Time.time+(activity==Activity.Climb?1.8f:activity==Activity.Run?1.3f:2.8f)+(seed%4)*.35f;
        }

        void MoveTowardTarget(float maxSpeed)
        {
            Vector3 delta=target-transform.position;if(activity!=Activity.Climb)delta.y=0;
            if(delta.sqrMagnitude<.35f){habitatIndex=(habitatIndex+1)%Mathf.Max(1,fauna.HabitatCount);ChooseNext();return;}
            Vector3 direction=delta.normalized;
            if(activity!=Activity.Climb&&Physics.SphereCast(transform.position+Vector3.up*.35f,.32f,direction,out var obstacle,.9f,~0,QueryTriggerInteraction.Ignore))
            {
                target=fauna.PickHabitat(habitatIndex+2,seed+1);return;
            }
            float step=Mathf.Min(maxSpeed*Time.deltaTime,delta.magnitude);
            Vector3 before=transform.position;transform.position+=direction*step;
            if(activity!=Activity.Climb&&Physics.Raycast(transform.position+Vector3.up*3,Vector3.down,out var ground,8,~0,QueryTriggerInteraction.Ignore))transform.position=new Vector3(transform.position.x,ground.point.y+.12f,transform.position.z);
            transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(direction,Vector3.up),Mathf.Clamp01(Time.deltaTime*10));
            DistanceTravelled+=Vector3.Distance(before,transform.position);
        }

        void Animate(float oldSpeed)
        {
            float gait=speed>0?Mathf.Sin(Time.time*(activity==Activity.Run?18:10)+phase):0;
            float bounce=speed>0?Mathf.Abs(Mathf.Sin(Time.time*(activity==Activity.Run?18:10)+phase))*(activity==Activity.Run?.065f:.025f):0;
            if(body!=null)body.localPosition=new Vector3(0,.14f+bounce,0);
            if(frontLeft!=null){frontLeft.localRotation=Quaternion.Euler(gait*28,0,0);frontRight.localRotation=Quaternion.Euler(-gait*28,0,0);backLeft.localRotation=Quaternion.Euler(-gait*24,0,0);backRight.localRotation=Quaternion.Euler(gait*24,0,0);}
            if(tail!=null){tail.localRotation=Quaternion.Euler(-28+Mathf.Sin(Time.time*3+phase)*8,Mathf.Sin(Time.time*2+phase)*12,Mathf.Sin(Time.time*4+phase)*5);tailMid.localRotation=Quaternion.Euler(-42+Mathf.Sin(Time.time*2.6f+phase)*10,Mathf.Sin(Time.time*2.2f+phase)*10,0);tailTip.localRotation=Quaternion.Euler(28+Mathf.Sin(Time.time*3.5f+phase)*12,0,0);}
            if(head!=null)head.localRotation=Quaternion.Euler(activity==Activity.Rest?8:Mathf.Sin(Time.time*2+phase)*3,Mathf.Sin(Time.time*1.4f+phase)*6,0);
        }
    }
}
