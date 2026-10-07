using UnityEngine;
namespace AlbionOdyssey
{
    public static partial class CampusGeometry
    {
        // Photo: albion.edu/wp-content/uploads/2021/03/ingham.jpg.
        // West porch, brick body, pale cornices and three-window central dormer
        // follow visible features. Dimensions and concealed elevations are estimates.
        static void BuildInghamExterior(Transform t,CampusPlace p)
        {
            t.localRotation=Quaternion.Euler(0,90,0); // local front faces Ingham Street, west
            float w=p.width,d=p.depth,front=-d/2,eaves=6.9f,grade=.85f;
            var pale=TowerGeometry.Material("Ingham painted cream trim",new Color(.88f,.84f,.72f));
            var masonry=CraftModel.Surface("Ingham red brick",new Color(.60f,.37f,.28f),"red_brick_03");
            Box(t,"Ingham foundation",new Vector3(0,grade/2,0),new Vector3(w+.18f,grade,d+.18f),stone);
            Box(t,"Ingham provisional sealed shell",new Vector3(0,(eaves+grade)/2,0),new Vector3(w,eaves-grade,d),masonry);
            Box(t,"Ingham main cornice",new Vector3(0,eaves,0),new Vector3(w+.6f,.28f,d+.6f),pale);
            // Two sloped roof surfaces, interrupted visually by the central dormer.
            float pitch=22f,rise=w*.5f*Mathf.Tan(pitch*Mathf.Deg2Rad);
            foreach(int side in new[]{-1,1})
            {
                float backStart=front+1.5f,backEnd=d/2+.35f;
                var slope=Box(t,"Ingham rear roof slope",new Vector3(side*w/4,eaves+rise/2,(backStart+backEnd)/2),new Vector3(w/2/Mathf.Cos(pitch*Mathf.Deg2Rad)+.35f,.18f,backEnd-backStart),roof,false);
                slope.transform.localRotation=Quaternion.Euler(0,0,-side*pitch);
                // Leave an actual opening around the dormer rather than overlapping its glazing.
                float inner=1.65f,outer=w/2+.17f,mid=(inner+outer)/2;
                var frontSlope=Box(t,"Ingham front roof beside dormer",new Vector3(side*mid,eaves+rise-mid*Mathf.Tan(pitch*Mathf.Deg2Rad),(front-.35f+backStart)/2),new Vector3((outer-inner)/Mathf.Cos(pitch*Mathf.Deg2Rad),.18f,backStart-front+.35f),roof,false);
                frontSlope.transform.localRotation=Quaternion.Euler(0,0,-side*pitch);
            }
            for(float x=-w/2+.22f;x<w/2;x+=.55f)
                Box(t,"Ingham cornice bracket",new Vector3(x,eaves-.25f,front-.16f),new Vector3(.13f,.32f,.4f),pale,false);
            // Front glazing follows the photograph's two outer bays and central trio.
            foreach(float x in new[]{-w*.34f,w*.34f})InghamWindow(t,new Vector3(x,5.25f,front-.035f),1.05f,1.6f,pale);
            foreach(float x in new[]{-.95f,0,.95f})InghamWindow(t,new Vector3(x,5.25f,front-.045f),.61f,1.65f,pale);
            foreach(float centre in new[]{-w*.32f,w*.32f})foreach(float dx in new[]{-.55f,0,.55f})
                InghamWindow(t,new Vector3(centre+dx,2.25f,front-.045f),.44f,1.75f,pale);
            Box(t,"Ingham entrance surround",new Vector3(0,2.2f,front-.1f),new Vector3(2.2f,2.7f,.16f),pale,false);
            foreach(int side in new[]{-1,1})InghamWindow(t,new Vector3(side*.45f,2.12f,front-.2f),.7f,2.1f,pale);
            float porchFront=front-2.45f,porchRoof=4.15f;
            Box(t,"Ingham porch deck",new Vector3(0,grade-.12f,front-1.3f),new Vector3(w+.3f,.24f,2.65f),stone);
            Box(t,"Ingham porch ceiling",new Vector3(0,porchRoof-.12f,front-1.35f),new Vector3(w+.7f,.22f,2.9f),pale);
            Box(t,"Ingham porch roof",new Vector3(0,porchRoof+.07f,front-1.35f),new Vector3(w+.85f,.14f,3.05f),roof,false);
            foreach(float x in new[]{-w*.45f,-w*.35f,-.7f,.7f,w*.35f,w*.45f})
            {
                float h=porchRoof-grade-.3f;
                KeeperAvatar.Part(t,"Ingham round porch column",PrimitiveType.Cylinder,new Vector3(x,grade+h/2,porchFront),new Vector3(.23f,h/2,.23f),pale,true);
                foreach(float y in new[]{grade+.08f,porchRoof-.28f})Box(t,"Ingham porch column capital",new Vector3(x,y,porchFront),new Vector3(.4f,.16f,.4f),pale);
            }
            for(float x=-w/2+.2f;x<w/2;x+=.5f)
                Box(t,"Ingham porch bracket",new Vector3(x,porchRoof-.3f,porchFront-.25f),new Vector3(.12f,.25f,.28f),pale,false);
            for(int i=0;i<5;i++)
            {
                float h=grade*(i+1)/5;
                Box(t,"Ingham porch approach step",new Vector3(0,h/2,porchFront-.3f-(4-i)*.31f),new Vector3(2.8f,h,.32f),stone);
            }
            Box(t,"Ingham approach walk",new Vector3(0,.04f,porchFront-3.1f),new Vector3(2.8f,.08f,3.5f),path);
            float dormerW=3.1f,dormerBottom=eaves+.32f,dormerH=1.9f,dormerZ=front+.65f;
            Box(t,"Ingham central dormer",new Vector3(0,dormerBottom+dormerH/2,dormerZ),new Vector3(dormerW,dormerH,1.5f),pale);
            foreach(float x in new[]{-.95f,0,.95f})InghamWindow(t,new Vector3(x,dormerBottom+.92f,dormerZ-.78f),.65f,1.3f,pale);
            float peak=1f,half=dormerW/2+.17f;
            // Solid triangular pediment, with a horizontal base and two pale rakes.
            var pediment=new GameObject("Ingham dormer pediment");pediment.transform.SetParent(t,false);
            var mesh=new Mesh();float y0=dormerBottom+dormerH,z0=dormerZ-.8f;
            mesh.vertices=new[]{new Vector3(-half,y0,z0),new Vector3(0,y0+peak,z0),new Vector3(half,y0,z0)};
            mesh.triangles=new[]{0,1,2};mesh.RecalculateNormals();pediment.AddComponent<MeshFilter>().sharedMesh=mesh;pediment.AddComponent<MeshRenderer>().sharedMaterial=pale;
            foreach(int side in new[]{-1,1})
            {
                var rake=Box(t,"Ingham dormer rake",new Vector3(side*half/2,y0+peak/2,z0-.05f),new Vector3(Mathf.Sqrt(half*half+peak*peak),.16f,.26f),pale,false);
                var cap=Box(t,"Ingham dormer roof",new Vector3(side*half/2,y0+peak/2,dormerZ),new Vector3(Mathf.Sqrt(half*half+peak*peak),.12f,1.85f),roof,false);
                cap.transform.localRotation=Quaternion.Euler(0,0,-side*Mathf.Atan2(peak,half)*Mathf.Rad2Deg);
                rake.transform.localRotation=Quaternion.Euler(0,0,-side*Mathf.Atan2(peak,half)*Mathf.Rad2Deg);
            }
            BuildingNameplate(t,p,3.25f,d+5);
        }
        static void InghamWindow(Transform t,Vector3 at,float width,float height,Material pale)
        {
            Box(t,"Ingham window surround",at,new Vector3(width+.18f,height+.18f,.08f),pale,false);
            Box(t,"Ingham sash glazing",at+Vector3.back*.05f,new Vector3(width,height,.04f),glass,false);
            Box(t,"Ingham sash divider",at+Vector3.back*.08f,new Vector3(width,.065f,.04f),pale,false);
            Box(t,"Ingham window sill",at+new Vector3(0,-height/2-.08f,-.08f),new Vector3(width+.24f,.1f,.22f),pale,false);
        }
    }
}
