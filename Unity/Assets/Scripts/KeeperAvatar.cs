using UnityEngine;
namespace AlbionOdyssey
{
    public sealed class KeeperAvatar : MonoBehaviour
    {
        public static readonly Color[] Skin={new Color(.29f,.15f,.09f),new Color(.49f,.29f,.17f),new Color(.72f,.48f,.31f),new Color(.91f,.72f,.54f),new Color(.98f,.84f,.70f)};
        public static readonly Color[] Coats={new Color(.29f,.12f,.5f),new Color(.05f,.40f,.38f),new Color(.8f,.29f,.12f),new Color(.13f,.29f,.60f),new Color(.87f,.68f,.20f)};
        Animator animator; GameObject rigged;
        public bool IsRigged=>animator!=null;
        public Transform leftArm,rightArm,leftLeg,rightLeg; public int skin,outfit,hair; public bool backpack=true;
        public static GameObject Part(Transform parent,string name,PrimitiveType shape,Vector3 position,Vector3 scale,Material mat,bool solid=false)
        {
            var o=GameObject.CreatePrimitive(shape);o.name=name;o.transform.SetParent(parent,false);o.transform.localPosition=position;o.transform.localScale=scale;
            o.GetComponent<Renderer>().sharedMaterial=mat;
            if(!solid){o.GetComponent<Collider>().enabled=false;Object.Destroy(o.GetComponent<Collider>());}
            return o;
        }
        public void Build(int skinTone,int coat,int hairstyle,bool pack)
        {
            skin=Mathf.Clamp(skinTone,0,4);outfit=Mathf.Clamp(coat,0,4);hair=Mathf.Clamp(hairstyle,0,2);backpack=pack;
            foreach(Transform child in transform){child.gameObject.SetActive(false);Destroy(child.gameObject);}
            var prefab=Resources.Load<GameObject>("CampusCraft/Student");
            if(prefab!=null)
            {
                rigged=Instantiate(prefab,transform,false);animator=rigged.GetComponent<Animator>();
                animator.Rebind();animator.Play("Locomotion",0,0);animator.Update(0);
                foreach(var renderer in rigged.GetComponentsInChildren<Renderer>())
                {
                    if(renderer.name.StartsWith("Backpack"))renderer.gameObject.SetActive(backpack);
                    if(renderer.name.StartsWith("Student hair"))renderer.gameObject.SetActive(hair!=2);
                    foreach(var m in renderer.materials)
                    {
                        if(m.name.Contains("Campus sweatshirt"))m.color=Coats[outfit];
                        if(m.name.Contains("Superhero"))m.color=Color.Lerp(Color.white,Skin[skin],.28f);
                        if(m.name.Contains("Student hair"))m.color=hair==1?new Color(.17f,.075f,.035f):new Color(.045f,.025f,.016f);
                    }
                }
                return;
            }
            var skinMat=TowerGeometry.Material("Keeper skin "+skin,Skin[skin]);var jacket=TowerGeometry.Material("Keeper jacket "+outfit,Coats[outfit]);
            var dark=TowerGeometry.Material("Keeper trousers",new Color(.06f,.08f,.12f));var white=TowerGeometry.Material("Keeper shoes",new Color(.92f,.89f,.77f));
            var gold=TowerGeometry.Material("Keeper trim",new Color(.95f,.7f,.19f));var hairMat=TowerGeometry.Material("Keeper hair",new Color(.07f,.04f,.025f));
            Part(transform,"Jacket",PrimitiveType.Capsule,new Vector3(0,1.17f,0),new Vector3(.62f,.39f,.38f),jacket);
            Part(transform,"Jacket zip",PrimitiveType.Cube,new Vector3(0,1.2f,.197f),new Vector3(.025f,.46f,.018f),gold);
            Part(transform,"Collar",PrimitiveType.Cylinder,new Vector3(0,1.51f,0),new Vector3(.25f,.06f,.25f),gold);
            Part(transform,"Head",PrimitiveType.Sphere,new Vector3(0,1.73f,.015f),new Vector3(.39f,.44f,.37f),skinMat);
            for(int i=-1;i<=1;i+=2){Part(transform,"Eye",PrimitiveType.Sphere,new Vector3(i*.078f,1.77f,.181f),new Vector3(.039f,.049f,.026f),dark);Part(transform,"Ear",PrimitiveType.Sphere,new Vector3(i*.20f,1.73f,0),Vector3.one*.08f,skinMat);}
            Part(transform,"Nose",PrimitiveType.Sphere,new Vector3(0,1.7f,.206f),new Vector3(.064f,.07f,.065f),skinMat);
            if(hair<2)Part(transform,"Hair",PrimitiveType.Sphere,new Vector3(0,1.9f,-.025f),new Vector3(hair==0?.4f:.46f,hair==0?.19f:.31f,.39f),hairMat);
            if(hair==1)Part(transform,"Hair bun",PrimitiveType.Sphere,new Vector3(0,1.98f,-.15f),Vector3.one*.24f,hairMat);
            if(hair==2){Part(transform,"Cap",PrimitiveType.Sphere,new Vector3(0,1.91f,0),new Vector3(.44f,.17f,.42f),jacket);Part(transform,"Cap brim",PrimitiveType.Cube,new Vector3(0,1.88f,.20f),new Vector3(.43f,.04f,.25f),gold);}
            leftArm=Limb("Left arm",new Vector3(-.37f,1.44f,0),.48f,.18f,jacket,skinMat,false);
            rightArm=Limb("Right arm",new Vector3(.37f,1.44f,0),.48f,.18f,jacket,skinMat,false);
            leftLeg=Limb("Left leg",new Vector3(-.17f,.87f,0),.65f,.23f,dark,white,true);
            rightLeg=Limb("Right leg",new Vector3(.17f,.87f,0),.65f,.23f,dark,white,true);
            if(pack){Part(transform,"Backpack",PrimitiveType.Cube,new Vector3(0,1.18f,-.26f),new Vector3(.43f,.53f,.20f),gold);Part(transform,"Backpack pocket",PrimitiveType.Cube,new Vector3(0,1.09f,-.38f),new Vector3(.32f,.23f,.07f),jacket);}
        }
        Transform Limb(string name,Vector3 pivot,float length,float width,Material material,Material end,bool foot)
        {
            var joint=new GameObject(name).transform;joint.SetParent(transform,false);joint.localPosition=pivot;
            Part(joint,name,PrimitiveType.Capsule,new Vector3(0,-length/2,0),new Vector3(width,length/2,width),material);
            Part(joint,foot?"Shoe":"Hand",foot?PrimitiveType.Cube:PrimitiveType.Sphere,new Vector3(0,-length,foot?.07f:0),foot?new Vector3(.25f,.16f,.38f):Vector3.one*.19f,end);
            return joint;
        }
        public void Animate(float speed,bool seated)
        {
            if(animator!=null){animator.SetFloat("Speed",speed,.16f,Time.deltaTime);animator.SetBool("Seated",seated);return;}
            if(leftArm==null)return;
            float swing=Mathf.Sin(Time.time*(speed>4?12:8))*Mathf.Clamp01(speed/3)*30;
            leftArm.localRotation=Quaternion.Euler(seated?-65:swing,0,seated?0:-6);
            rightArm.localRotation=Quaternion.Euler(seated?-65:-swing,0,seated?0:6);
            leftLeg.localRotation=Quaternion.Euler(seated?-85:-swing,0,0);rightLeg.localRotation=Quaternion.Euler(seated?-85:swing,0,0);
        }
    }
}
