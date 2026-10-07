using System.Linq;
using UnityEngine;
namespace AlbionOdyssey
{
    public sealed partial class WalkableCampusBuilding
    {
        // Official reference: albion.edu/wp-content/uploads/2021/03/ingham-common.jpg.
        // Features and palette are evidenced; dimensions and placement are provisional.
        void InghamLivingWindows(PlanEdge edge,float centre)
        {
            float cursor=edge.start,width=.78f,sill=.65f,head=2.65f;
            foreach(float offset in new[]{-.92f,0,.92f})
            {
                float x=centre+offset,a=x-width/2,b=x+width/2;
                PlanWall(edge,FloorHeight/2,cursor,a,FloorHeight,brick);
                PlanWall(edge,sill/2,a,b,sill,brick);
                PlanWall(edge,(head+FloorHeight)/2,a,b,FloorHeight-head,brick);
                var g=new GameObject("Ingham living room front window").transform;g.SetParent(Root.transform,false);g.localPosition=new Vector3(x,(sill+head)/2,edge.fixedAt);
                Box(g,"Living room window glazing",Vector3.zero,new Vector3(width,head-sill,.065f),glass);
                foreach(float sign in new[]{-1f,1f})
                {
                    Box(g,"Living room window casing",new Vector3(sign*(width/2+.04f),0,.03f),new Vector3(.08f,head-sill+.16f,.16f),trim,false);
                    Box(g,"Living room window casing",new Vector3(0,sign*(head-sill)/2,.03f),new Vector3(width+.16f,.08f,.16f),trim,false);
                }
                Box(g,"Living room sash divider",Vector3.forward*.05f,new Vector3(width,.065f,.08f),trim,false);cursor=b;
            }
            PlanWall(edge,FloorHeight/2,cursor,edge.end,FloorHeight,brick);
        }
        void DecorateInghamLivingRoom()
        {
            var room=Plan.floors[0].spaces.First(s=>s.name=="Living room");
            var clay=CraftModel.Surface("Ingham terracotta paint",new Color(.52f,.20f,.095f));
            var green=CraftModel.Surface("Ingham green fireplace surround",new Color(.23f,.32f,.15f));
            var cream=CraftModel.Surface("Ingham pale interior trim",new Color(.88f,.85f,.76f));
            var carpet=CraftModel.Surface("Ingham charcoal carpet",new Color(.27f,.28f,.28f));
            var upholstery=CraftModel.Surface("Ingham brown seating",new Color(.31f,.27f,.20f));
            var brass=CraftModel.Surface("Ingham brass fixtures",new Color(.57f,.43f,.16f));brass.SetFloat("_Metallic",.7f);
            float left=room.x-room.width/2,front=room.z-room.depth/2,rear=room.z+room.depth/2;
            Box(Root.transform,"Ingham living room carpet",new Vector3(room.x,.009f,room.z),new Vector3(room.width-.18f,.015f,room.depth-.18f),carpet,false);
            // Paint only existing wall spans, preserving every window opening.
            foreach(var wall in Root.GetComponentsInChildren<Transform>().Where(t=>t.name==Place.name+" brick wall").ToArray())
            {
                var at=wall.localPosition;var size=wall.localScale;
                if(at.y-size.y/2<-.01f||at.y+size.y/2>FloorHeight+.01f)continue;
                if(Mathf.Abs(at.x-left)<.002f)
                {
                    float a=Mathf.Max(front,at.z-size.z/2),b=Mathf.Min(rear,at.z+size.z/2);
                    if(b>a)Box(Root.transform,"Ingham living room paint",new Vector3(left+.095f,at.y,(a+b)/2),new Vector3(.01f,size.y,b-a),clay,false);
                }
                else if(Mathf.Abs(at.z-front)<.002f)
                {
                    float a=Mathf.Max(left,at.x-size.x/2),b=Mathf.Min(left+room.width,at.x+size.x/2);
                    if(b>a)Box(Root.transform,"Ingham living room paint",new Vector3((a+b)/2,at.y,front+.095f),new Vector3(b-a,size.y,.01f),clay,false);
                }
            }
            foreach(float y in new[]{.14f,FloorHeight-.16f})
            {
                Box(Root.transform,"Ingham living room molding",new Vector3(left+.12f,y,room.z),new Vector3(.08f,.18f,room.depth-.18f),cream,false);
                Box(Root.transform,"Ingham living room molding",new Vector3(room.x,y,front+.12f),new Vector3(room.width-.18f,.18f,.08f),cream,false);
            }
            var fire=new GameObject("Ingham photo-referenced fireplace").transform;fire.SetParent(Root.transform,false);fire.localPosition=new Vector3(left+.13f,0,room.z);fire.localRotation=Quaternion.Euler(0,90,0);
            Box(fire,"Green chimney breast",new Vector3(0,FloorHeight/2,.12f),new Vector3(1.65f,FloorHeight,.24f),green);
            Box(fire,"Green fireplace hearth",new Vector3(0,.04f,.42f),new Vector3(1.85f,.08f,.8f),green);
            Box(fire,"Dark fireplace insert",new Vector3(0,.55f,.26f),new Vector3(.7f,.9f,.05f),metal,false);
            foreach(float x in new[]{-.43f,.43f})Box(fire,"Brass fireplace jamb",new Vector3(x,.57f,.30f),new Vector3(.1f,1f,.05f),brass,false);
            Box(fire,"Brass fireplace lintel",new Vector3(0,1.04f,.30f),new Vector3(.95f,.12f,.05f),brass,false);
            foreach(float x in new[]{-.75f,.75f})Box(fire,"Pale fireplace trim",new Vector3(x,.77f,.31f),new Vector3(.09f,1.5f,.1f),cream,false);
            Box(fire,"Pale fireplace mantel",new Vector3(0,1.54f,.32f),new Vector3(1.65f,.1f,.2f),cream,false);
            var mirror=KeeperAvatar.Part(fire,"Round mirror",PrimitiveType.Cylinder,new Vector3(0,2.27f,.27f),new Vector3(.87f,.025f,.87f),glass,false);mirror.transform.localRotation=Quaternion.Euler(90,0,0);
            for(int i=0;i<40;i++)
            {
                float a=i*Mathf.PI*2/40;
                var bead=Box(fire,"Brass mirror rim",new Vector3(Mathf.Cos(a)*.48f,2.27f+Mathf.Sin(a)*.48f,.31f),new Vector3(.09f,.075f,.07f),brass,false);bead.transform.localRotation=Quaternion.Euler(0,0,a*Mathf.Rad2Deg);
            }
            InghamSeat("Living room sofa",new Vector3(room.x,0,front+.75f),0,1.85f,upholstery);
            InghamSeat("Living room armchair one",new Vector3(left+.85f,0,room.z-1.65f),55,.88f,upholstery);
            InghamSeat("Living room armchair two",new Vector3(room.x+.95f,0,room.z+1.5f),-130,.88f,upholstery);
            foreach(float z in new[]{room.z-1.6f,room.z+1.5f})
            {
                Box(Root.transform,"Ingham pendant suspension",new Vector3(room.x,FloorHeight-.28f,z),new Vector3(.025f,.5f,.025f),brass,false);
                KeeperAvatar.Part(Root.transform,"Ingham pendant bowl",PrimitiveType.Sphere,new Vector3(room.x,FloorHeight-.6f,z),new Vector3(.43f,.2f,.43f),cream,false);
                Light(new Vector3(room.x,FloorHeight-.72f,z));
            }
        }
        void InghamSeat(string name,Vector3 at,float yaw,float width,Material material)
        {
            var seat=new GameObject(name).transform;seat.SetParent(Root.transform,false);seat.localPosition=at;seat.localRotation=Quaternion.Euler(0,yaw,0);
            Box(seat,"Upholstered seat base",new Vector3(0,.29f,0),new Vector3(width,.46f,.83f),material);
            Box(seat,"Seat cushion",new Vector3(0,.54f,.06f),new Vector3(width-.25f,.13f,.68f),material);
            Box(seat,"Upholstered seat back",new Vector3(0,.76f,-.34f),new Vector3(width,.66f,.22f),material);
            foreach(float side in new[]{-1f,1f})Box(seat,"Upholstered arm",new Vector3(side*(width/2-.085f),.62f,0),new Vector3(.17f,.38f,.86f),material);
        }
    }
}
