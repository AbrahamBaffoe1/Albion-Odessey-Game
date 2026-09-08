using UnityEngine;
using System.Collections.Generic;
namespace AlbionOdyssey
{
    public static class CampusGeometry
    {
        static Material brick,stone,roof,glass,grass,path,road,gold; static Transform root;
        static GameObject Box(Transform p,string name,Vector3 at,Vector3 size,Material mat,bool solid=true)
        {
            var o=KeeperAvatar.Part(p,name,PrimitiveType.Cube,at,size,mat,solid);
            if(mat.mainTexture!=null){var mesh=o.GetComponent<MeshFilter>().mesh;var uv=new Vector2[mesh.vertexCount];var v=mesh.vertices;var n=mesh.normals;for(int i=0;i<v.Length;i++){var w=Vector3.Scale(v[i],size)+at;uv[i]=Mathf.Abs(n[i].y)>.7f?new Vector2(w.x,w.z)/2:Mathf.Abs(n[i].x)>.7f?new Vector2(w.z,w.y)/2:new Vector2(w.x,w.y)/2;}mesh.uv=uv;mesh.RecalculateTangents();}
            return o;
        }
        public static void Build()
        {
            root=new GameObject("Albion College · map-based exterior campus").transform;
            brick=CraftModel.Surface("Campus brick",Color.white,"red_brick_03");stone=TowerGeometry.Material("Campus limestone",new Color(.78f,.72f,.59f));
            roof=TowerGeometry.Material("Campus slate",new Color(.16f,.23f,.25f));glass=TowerGeometry.Material("Campus blue windows",new Color(.19f,.36f,.44f),.15f,.7f);
            grass=CraftModel.Surface("Campus lawn",new Color(.7f,.8f,.65f),"aerial_grass_rock");path=CraftModel.Surface("Campus paving",Color.white,"concrete_pavement");
            road=TowerGeometry.Material("Campus asphalt",new Color(.14f,.17f,.18f));gold=TowerGeometry.Material("Campus gold",new Color(.94f,.65f,.15f));
            Box(root,"Campus ground",new Vector3(0,-.3f,450),new Vector3(1200,.5f,750),grass);
            Box(root,"Legacy connector",new Vector3(0,-.1f,95),new Vector3(16,.2f,190),path);
            foreach(float y in new[]{77f,123f,161f,241f,286f})Street(CampusCatalog.Point(400,y),new Vector3(1060,.12f,10));
            foreach(float x in new[]{64f,118f,178f,236f,297f,363f,474f,590f,718f})Street(CampusCatalog.Point(x,216),new Vector3(8,.12f,520));
            foreach(var place in CampusCatalog.Places)Building(place);
            // Public Quad paths, open lawns and trees are deliberately navigable.
            Box(root,"Quad east west path",CampusCatalog.Point(394,213)+Vector3.up*.03f,new Vector3(143,.1f,2.4f),path);
            Box(root,"Quad north south path",CampusCatalog.Point(409,204)+Vector3.up*.03f,new Vector3(2.4f,.1f,66),path);
            var random=new System.Random(1835);
            for(int i=0;i<160;i++)
            {
                float x=260+(float)random.NextDouble()*520,y=85+(float)random.NextDouble()*330;
                Vector3 pos=CampusCatalog.Point(x,y); bool blocked=false;
                foreach(var p in CampusCatalog.Places)if(Mathf.Abs(pos.x-p.position.x)<p.width/2+8&&Mathf.Abs(pos.z-p.position.z)<p.depth/2+8){blocked=true;break;}
                foreach(float rx in new[]{297f,363f,474f,590f,718f})if(Mathf.Abs(x-rx)<7)blocked=true;
                foreach(float ry in new[]{77f,123f,161f,241f,286f})if(Mathf.Abs(y-ry)<7)blocked=true;
                if(!blocked)Tree(pos,i%3);
            }

            // Deliberate planting around the arrival terrace and Quad, clear of doors and roads.
            var ferguson=CampusExpansion.Find("26").position;
            foreach(var at in new[]{new Vector3(-22,0,-9),new Vector3(22,0,-9),new Vector3(-22,0,15),new Vector3(22,0,15),new Vector3(0,0,24),new Vector3(-20,0,28)})Tree(ferguson+at,1);
            Combine();
        }
        static void Combine()
        {
            var groups=new Dictionary<Material,List<CombineInstance>>();
            foreach(var filter in root.GetComponentsInChildren<MeshFilter>())
            {
                var renderer=filter.GetComponent<MeshRenderer>();if(renderer==null||filter.sharedMesh==null)continue;
                var material=renderer.sharedMaterial;if(!groups.ContainsKey(material))groups[material]=new List<CombineInstance>();
                groups[material].Add(new CombineInstance{mesh=filter.sharedMesh,transform=filter.transform.localToWorldMatrix});renderer.enabled=false;
            }
            foreach(var entry in groups)
            {
                var o=new GameObject("Batched campus "+entry.Key.name);o.transform.SetParent(root);var mesh=new Mesh{indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};mesh.CombineMeshes(entry.Value.ToArray(),true,true);
                o.AddComponent<MeshFilter>().sharedMesh=mesh;o.AddComponent<MeshRenderer>().sharedMaterial=entry.Key;
            }
        }
        static void Street(Vector3 pos,Vector3 size)
        {
            Box(root,"Campus street",pos,size,road);
            bool horizontal=size.x>size.z;
            int n=Mathf.FloorToInt((horizontal?size.x:size.z)/12);
            for(int i=0;i<n;i++)Box(root,"Road marking",pos+(horizontal?Vector3.right:Vector3.forward)*((i-n/2)*12)+Vector3.up*.07f,horizontal?new Vector3(4,.02f,.10f):new Vector3(.10f,.02f,4),stone,false);
        }
        static GameObject[] oakTemplates;
        static void Tree(Vector3 p,int variant)
        {
            if(oakTemplates==null)
            {
                oakTemplates=new GameObject[3];var definition=JsonUtility.FromJson<CraftDescription>(Resources.Load<TextAsset>("CampusCraft/oak").text);
                for(int i=0;i<3;i++){oakTemplates[i]=CraftModel.Load("oak"+i,definition.sections[0],definition.materials,null);oakTemplates[i].SetActive(false);}
            }
            var tree=Object.Instantiate(oakTemplates[variant],root);tree.name="Blender oak";tree.transform.position=p;tree.transform.localRotation=Quaternion.Euler(0,(p.x*17)%360,0);tree.transform.localScale=Vector3.one*(.8f+Mathf.Abs(p.z%4)*.12f);tree.SetActive(true);
        }
        static void Building(CampusPlace p)
        {
            if(p.id=="26")return;
            var t=new GameObject(p.id+" · "+p.name).transform;t.SetParent(root);t.position=p.position;
            float w=p.width,d=p.depth,h=p.height;bool field=p.shape=="field"||p.shape=="stadium"||p.shape=="baseball";
            if(field)
            {
                Box(t,"Playing surface",Vector3.up*.08f,new Vector3(w,.12f,d),TowerGeometry.Material("Athletic turf",new Color(.12f,.35f,.24f)));
                for(int i=-4;i<=4;i++)Box(t,"Field markings",new Vector3(i*w/10,.16f,0),new Vector3(.14f,.015f,d*.84f),stone,false);
                foreach(int side in new[]{-1,1})
                {
                    for(int row=0;row<4;row++)Box(t,"Spectator seating",new Vector3(0,.5f+row*.5f,side*(d/2+1+row)),new Vector3(w*.75f,.5f,1),stone);
                    if(p.shape!="baseball")Box(t,"Goal",new Vector3(side*(w/2-2),2,0),new Vector3(.25f,4,5),gold,false);
                }
                Sign(t,p.name,new Vector3(0,3,-d/2-6),Mathf.Min(w,25));return;
            }
            Material wall=p.shape=="arts"||p.shape=="science"||p.shape=="gym"?stone:brick;
            Box(t,"Foundation",Vector3.up*.25f,new Vector3(w+.5f,.5f,d+.5f),stone);
            Box(t,"Facade",new Vector3(0,h/2,0),new Vector3(w,h,d),wall);
            Box(t,"Cornice",new Vector3(0,h-.4f,0),new Vector3(w+.5f,.4f,d+.5f),stone);
            Box(t,"Roof",new Vector3(0,h+.15f,0),new Vector3(w+.8f,.5f,d+.8f),roof);
            // Window strips are instanced shared materials; all four exterior faces are modeled.
            int floors=Mathf.Max(1,Mathf.FloorToInt(h/3.3f));
            for(int f=0;f<floors;f++)
            {
                float y=2+f*3.2f;
                for(float x=-w/2+2;x<w/2-1;x+=3.5f)foreach(int side in new[]{-1,1})Box(t,"Window",new Vector3(x,y,side*(d/2+.025f)),new Vector3(1.2f,1.55f,.06f),glass,false);
                for(float z=-d/2+2;z<d/2-1;z+=3.5f)foreach(int side in new[]{-1,1})Box(t,"Side window",new Vector3(side*(w/2+.025f),y,z),new Vector3(.06f,1.55f,1.2f),glass,false);
            }
            Box(t,"Entrance",new Vector3(0,1.3f,-d/2-.05f),new Vector3(2.2f,2.6f,.12f),roof,false);
            Box(t,"Entrance canopy",new Vector3(0,3,-d/2-1),new Vector3(4,.25f,2),stone,false);
            Box(t,"Front approach",new Vector3(0,.04f,-d/2-3.2f),new Vector3(4,.08f,6),path);
            if(p.shape=="house"||p.shape=="nature"||p.shape=="stable"||p.shape=="chapel")
            {
                foreach(int side in new[]{-1,1}){var slope=Box(t,"Pitched roof",new Vector3(side*w/4,h+w*.13f,0),new Vector3(w*.57f,.45f,d+1),roof,false);slope.transform.localRotation=Quaternion.Euler(0,0,-side*25);}
            }
            if(p.shape=="chapel")
            {
                Box(t,"Chapel bell tower",new Vector3(0,h+5,-d*.24f),new Vector3(4,11,4),stone);
                Spire(t,new Vector3(0,h+10.5f,-d*.24f),2.4f,13);
                foreach(int side in new[]{-1,1})Box(t,"Chapel columns",new Vector3(side*3,3,-d/2-.8f),new Vector3(.6f,6,.6f),stone);
            }
            if(p.shape=="observatory")
            {
                KeeperAvatar.Part(t,"Observatory round tower",PrimitiveType.Cylinder,new Vector3(0,h,0),new Vector3(w*.8f,3,w*.8f),brick,true);
                KeeperAvatar.Part(t,"Observatory copper dome",PrimitiveType.Sphere,new Vector3(0,h+3,0),new Vector3(w*.85f,4,w*.85f),roof);
            }
            if(p.shape=="library")foreach(int side in new[]{-1,1})Box(t,"Library entrance pier",new Vector3(side*2.4f,2,-d/2-.5f),new Vector3(.5f,4,.7f),stone);
            // Building names appear contextually in the HUD rather than floating across facades.
        }
        static void Spire(Transform parent,Vector3 position,float radius,float height)
        {
            var vertices=new List<Vector3>();var triangles=new List<int>();
            for(int i=0;i<12;i++)
            {
                float a=i*Mathf.PI/6,b=(i+1)*Mathf.PI/6;int n=vertices.Count;
                vertices.Add(new Vector3(Mathf.Sin(a)*radius,0,Mathf.Cos(a)*radius));vertices.Add(Vector3.up*height);vertices.Add(new Vector3(Mathf.Sin(b)*radius,0,Mathf.Cos(b)*radius));
                triangles.Add(n);triangles.Add(n+1);triangles.Add(n+2);
            }
            var mesh=new Mesh();mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();
            var o=new GameObject("Chapel pointed steeple");o.transform.SetParent(parent,false);o.transform.localPosition=position;o.AddComponent<MeshFilter>().sharedMesh=mesh;o.AddComponent<MeshRenderer>().sharedMaterial=roof;
        }
        public static void Sign(Transform parent,string text,Vector3 at,float width)
        {
            var o=new GameObject(text+" sign");o.transform.SetParent(parent,false);o.transform.localPosition=at;o.transform.localRotation=Quaternion.identity;
            var label=o.AddComponent<TextMesh>();label.text=text;label.fontSize=48;label.characterSize=.12f;label.anchor=TextAnchor.MiddleCenter;label.alignment=TextAlignment.Center;label.color=new Color(.97f,.84f,.51f);
            float estimate=text.Length*.07f;if(estimate>width)o.transform.localScale=Vector3.one*(width/estimate);
        }
    }
}
