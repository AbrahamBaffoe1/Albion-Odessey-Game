using UnityEngine;
using UnityEngine.Rendering;
namespace AlbionOdyssey.BuildingDesigner
{
    public static class StudioGeometry
    {
        public static readonly Vector3 Origin=new Vector3(-400,0,-400);
        public static Vector3 CellCenter(int x,int z)=>new Vector3((x-5.5f)*2,0,(z-5.5f)*2);
        static Material Mat(string name,Color color)=>TowerGeometry.Material("Studio "+name,color);
        public static GameObject Box(Transform parent,string name,Vector3 p,Vector3 size,Material material,bool solid=true)
        {
            var obj=GameObject.CreatePrimitive(PrimitiveType.Cube);obj.name=name;obj.transform.SetParent(parent,false);obj.transform.localPosition=p;obj.transform.localScale=size;obj.GetComponent<Renderer>().sharedMaterial=material;if(!solid)Object.Destroy(obj.GetComponent<Collider>());return obj;
        }
        public static GameObject Build(BuildingBlueprint blueprint,bool showRoof)
        {
            var root=new GameObject("Player-authored building");root.transform.position=Origin;
            var floor=Mat("Limestone",new Color(.62f,.66f,.62f));var wall=Mat("Brick",new Color(.53f,.25f,.18f));
            var trim=Mat("Trim",new Color(.8f,.76f,.62f));var metal=Mat("Metal",new Color(.075f,.15f,.17f));
            var wood=Mat("Wood",new Color(.38f,.22f,.10f));var purple=Mat("Upholstery",new Color(.32f,.20f,.48f));
            var grass=Mat("Garden",new Color(.20f,.38f,.23f));var glass=Mat("Window",new Color(.25f,.53f,.61f,.3f));
            glass.SetFloat("_Mode",3);glass.SetInt("_SrcBlend",(int)BlendMode.One);glass.SetInt("_DstBlend",(int)BlendMode.OneMinusSrcAlpha);glass.SetInt("_ZWrite",0);glass.EnableKeyword("_ALPHAPREMULTIPLY_ON");glass.renderQueue=3000;
            Box(root.transform,"Studio site",new Vector3(0,-.12f,0),new Vector3(30,.24f,30),grass);
            for(int i=0;i<=12;i++)
            {
                Box(root.transform,"Layout grid",new Vector3((i-6)*2,.01f,0),new Vector3(.025f,.01f,24),trim,false);
                Box(root.transform,"Layout grid",new Vector3(0,.01f,(i-6)*2),new Vector3(24,.01f,.025f),trim,false);
            }
            for(int z=0;z<12;z++)for(int x=0;x<12;x++)
            {
                int i=z*12+x;var p=CellCenter(x,z);
                if(blueprint.floors[i]==1)Box(root.transform,"Floor",p+Vector3.up*.06f,new Vector3(1.98f,.12f,1.98f),floor);
                if(blueprint.roofs[i]==1&&showRoof)Box(root.transform,"Roof",p+Vector3.up*3.35f,new Vector3(2.04f,.20f,2.04f),trim);
                int kind=blueprint.furniture[i];if(kind==0)continue;
                var group=new GameObject("Furniture");group.transform.SetParent(root.transform,false);group.transform.localPosition=p+Vector3.up*.12f;group.transform.localRotation=Quaternion.Euler(0,blueprint.rotations[i]*90,0);
                if(kind==1)
                {
                    Box(group.transform,"Table top",new Vector3(0,.78f,0),new Vector3(1.45f,.12f,.8f),wood);
                    foreach(float dx in new[]{-.56f,.56f})foreach(float dz in new[]{-.25f,.25f})Box(group.transform,"Table leg",new Vector3(dx,.36f,dz),new Vector3(.08f,.72f,.08f),metal);
                }
                if(kind==2)
                {
                    Box(group.transform,"Chair seat",new Vector3(0,.46f,0),new Vector3(.6f,.12f,.6f),purple);
                    Box(group.transform,"Chair back",new Vector3(0,.82f,-.27f),new Vector3(.6f,.08f,.08f)+new Vector3(0,.54f,0),purple);
                    foreach(float dx in new[]{-.23f,.23f})foreach(float dz in new[]{-.23f,.23f})Box(group.transform,"Chair leg",new Vector3(dx,.2f,dz),new Vector3(.07f,.4f,.07f),metal);
                }
                if(kind==3)
                {
                    Box(group.transform,"Planter",Vector3.up*.27f,new Vector3(.65f,.54f,.65f),trim);
                    Box(group.transform,"Plant trunk",Vector3.up*.76f,new Vector3(.12f,1,.12f),wood);
                    var crown=GameObject.CreatePrimitive(PrimitiveType.Sphere);crown.transform.SetParent(group.transform,false);crown.transform.localPosition=Vector3.up*1.24f;crown.transform.localScale=new Vector3(.9f,1.1f,.9f);crown.GetComponent<Renderer>().sharedMaterial=grass;
                }
            }
            foreach(bool alongX in new[]{true,false})
            {
                int[] edges=alongX?blueprint.horizontal:blueprint.vertical;
                for(int z=0;z<(alongX?13:12);z++)for(int x=0;x<(alongX?12:13);x++)
                {
                    int kind=edges[z*(alongX?12:13)+x];if(kind==0)continue;
                    var edge=new GameObject(kind==1?"Wall":kind==2?"Doorway":"Window wall");edge.transform.SetParent(root.transform,false);
                    edge.transform.localPosition=new Vector3((x-6)*2+(alongX?1:0),.12f,(z-6)*2+(alongX?0:1));edge.transform.localRotation=Quaternion.Euler(0,alongX?0:90,0);
                    if(kind==1)Box(edge.transform,"Brick wall",Vector3.up*1.56f,new Vector3(2,3.12f,.18f),wall);
                    else if(kind==2)
                    {
                        foreach(float dx in new[]{-.85f,.85f})Box(edge.transform,"Door jamb",new Vector3(dx,1.56f,0),new Vector3(.3f,3.12f,.22f),trim);
                        Box(edge.transform,"Door header",new Vector3(0,2.8f,0),new Vector3(1.4f,.64f,.22f),trim);
                    }
                    else
                    {
                        Box(edge.transform,"Window sill wall",Vector3.up*.45f,new Vector3(2,.9f,.18f),wall);
                        Box(edge.transform,"Window header",Vector3.up*2.8f,new Vector3(2,.64f,.18f),wall);
                        foreach(float dx in new[]{-.9f,.9f})Box(edge.transform,"Window pier",new Vector3(dx,1.69f,0),new Vector3(.2f,1.58f,.18f),wall);
                        Box(edge.transform,"Window glass",Vector3.up*1.69f,new Vector3(1.6f,1.58f,.045f),glass);
                        Box(edge.transform,"Window mullion",Vector3.up*1.69f,new Vector3(.07f,1.58f,.12f),metal);
                    }
                    Box(edge.transform,"Cornice",Vector3.up*3.12f,new Vector3(2.04f,.16f,.28f),trim);
                }
            }
            return root;
        }
    }
}
