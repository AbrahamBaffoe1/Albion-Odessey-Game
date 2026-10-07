using UnityEngine;
namespace AlbionOdyssey
{
    public sealed partial class WalkableCampusBuilding
    {
        void BatchExteriorDetails()
        {
            // Preserve animated doors; Wesley static walls, windows and furniture share batches.
            var groups=new System.Collections.Generic.Dictionary<(Material material,int x,int y,int z),System.Collections.Generic.List<CombineInstance>>();
            foreach(var filter in Root.GetComponentsInChildren<MeshFilter>())
            {
                if (Plan != null) { if(filter.GetComponentInParent<CampusDoor>()!=null || filter.GetComponent<TextMesh>()!=null)continue; }
                else if(filter.transform.parent!=Root.transform || filter.GetComponent<Collider>()!=null)continue;
                var renderer=filter.GetComponent<MeshRenderer>();if(renderer==null||!renderer.enabled||filter.sharedMesh==null)continue;
                var material=renderer.sharedMaterial;if(material==null)continue;
                var position=Root.transform.InverseTransformPoint(filter.transform.position);
                var key=(material,Plan!=null?Mathf.FloorToInt(position.x/8):0,Plan!=null?Mathf.FloorToInt(position.y/3.2f):0,Plan!=null?Mathf.FloorToInt(position.z/8):0);
                if(!groups.ContainsKey(key))groups[key]=new System.Collections.Generic.List<CombineInstance>();
                groups[key].Add(new CombineInstance{mesh=filter.sharedMesh,transform=Root.transform.worldToLocalMatrix*filter.transform.localToWorldMatrix});renderer.enabled=false;
            }
            foreach(var group in groups)
            {
                var part=new GameObject("Batched architectural detail");part.transform.SetParent(Root.transform,false);
                var mesh=new Mesh{indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};mesh.CombineMeshes(group.Value.ToArray(),true,true);
                part.AddComponent<MeshFilter>().sharedMesh=mesh;part.AddComponent<MeshRenderer>().sharedMaterial=group.Key.material;
            }
        }

        // Original geometry studied from Albion's public Wesley entrance photograph.
        // Game-scale dimensions, not a survey; rear wings remain provisional.
        void FramedWindow(Transform parent, Vector3 at, float width, float height)
        {
            Box(parent, "Recessed blue glazing", at, new Vector3(width, height, .06f), glass, false);
            foreach (int side in new[] {-1, 1})
            {
                Box(parent, "Window jamb", at + Vector3.right * side * (width / 2 + .045f), new Vector3(.09f, height + .18f, .15f), trim, false);
                Box(parent, "Window head and sill", at + Vector3.up * side * (height / 2 + .045f), new Vector3(width + .18f, .09f, .19f), trim, false);
            }
            Box(parent, "Window centre mullion", at, new Vector3(.035f, height, .12f), trim, false);
            foreach (float fraction in new[] {-.25f, 0f, .25f})
                Box(parent, "Sash glazing bar", at + Vector3.up * height * fraction, new Vector3(width, .035f, .12f), trim, false);
        }

        void BuildWesleyExterior()
        {
            Transform t = Root.transform; float h = Floors * FloorHeight, front = -Depth / 2;
            brick.color = new Color(.84f, .70f, .55f);
            Box(t, "Wesley limestone plinth", new Vector3(0, -.06f, 0), new Vector3(Width + .45f, .12f, Depth + .45f), stone);
            Box(t, "Wesley rear wall", new Vector3(0, h/2, Depth/2), new Vector3(Width, h, .32f), brick);
            foreach (int side in new[] {-1, 1})
            {
                Box(t, "Wesley side wall", new Vector3(side*Width/2, h/2, 0), new Vector3(.32f, h, Depth), brick);
                float span = Width/2 - 1.3f;
                Box(t, "Wesley entrance wing", new Vector3(side*(1.3f+span/2), h/2, front), new Vector3(span,h,.32f),brick);
            }
            Box(t,"Wesley doorway lintel",new Vector3(0,(h+2.8f)/2,front),new Vector3(2.6f,h-2.8f,.32f),brick);
            Door(new Vector3(0,0,front-.2f),2.5f,2.8f,"Wesley historic entrance");
            foreach(int side in new[]{-1,1})Box(t,"Entrance white casing",new Vector3(side*1.36f,1.4f,front-.26f),new Vector3(.13f,2.8f,.18f),trim,false);
            for(int i=0;i<20;i++)
            {
                float angle=(i+.5f)*Mathf.PI/20;
                var voussoir=Box(t,"Arched entrance transom trim",new Vector3(Mathf.Cos(angle)*1.34f,2.8f+Mathf.Sin(angle)*1.34f,front-.26f),new Vector3(.24f,.13f,.18f),trim,false);
                voussoir.transform.localRotation=Quaternion.Euler(0,0,angle*Mathf.Rad2Deg+90);
            }
            // Landscape treatment follows the clipped hedge beds flanking the entrance.
            var leaves=CraftModel.Surface("Wesley hedge foliage",new Color(.15f,.24f,.10f));
            foreach(int side in new[]{-1,1})
            {
                Box(t,"Wesley planted border",new Vector3(side*10.4f,.12f,front-3.8f),new Vector3(13.6f,.24f,1.6f),stone,false);
                for(int i=0;i<17;i++)KeeperAvatar.Part(t,"Wesley clipped shrub",PrimitiveType.Sphere,new Vector3(side*(4+i*.8f),.63f,front-3.8f),new Vector3(1.2f,1.25f,1.2f),leaves,false);
            }
            for(int f=0;f<Floors;f++)
            {
                for(int i=0;i<12;i++)
                {
                    float x=-Width/2+1.5f+i*(Width-3)/11;
                    if(f!=0||Mathf.Abs(x)>2)FramedWindow(t,new Vector3(x,1.8f+f*FloorHeight,front-.22f),1.1f,1.65f);
                    FramedWindow(t,new Vector3(x,1.8f+f*FloorHeight,Depth/2+.22f),1.1f,1.65f);
                }
                foreach(int side in new[]{-1,1})for(int i=0;i<6;i++)
                {
                    var bay=new GameObject("Wesley side window bay").transform;bay.SetParent(t,false);
                    bay.localPosition=new Vector3(side*(Width/2+.22f),1.8f+f*FloorHeight,-Depth/2+2+i*(Depth-4)/5);
                    bay.localRotation=Quaternion.Euler(0,90,0);FramedWindow(bay,Vector3.zero,1.1f,1.65f);
                }
            }
            Box(t,"White eaves",new Vector3(0,h,0),new Vector3(Width+.6f,.3f,Depth+.6f),trim,false);
            float rise=3.1f, half=Depth/2+.5f;
            foreach(int side in new[]{-1,1})
            {
                var slope=Box(t,"Wesley sloped slate roof",new Vector3(0,h+rise/2,side*half/2),new Vector3(Width+.9f,.16f,Mathf.Sqrt(half*half+rise*rise)),roof,false);
                slope.transform.localRotation=Quaternion.Euler(side*Mathf.Atan2(rise,half)*Mathf.Rad2Deg,0,0);
            }
            // Close both triangular gable ends beneath the roof slopes.
            foreach(int side in new[]{-1,1})
            {
                var end=new GameObject("Wesley brick gable");end.transform.SetParent(t,false);
                float x=side*Width/2;
                var mesh=new Mesh();mesh.vertices=new[]{new Vector3(x,h,-Depth/2),new Vector3(x,h,Depth/2),new Vector3(x,h+rise,0)};
                mesh.triangles=side>0?new[]{0,2,1}:new[]{0,1,2};mesh.uv=new[]{new Vector2(0,0),new Vector2(Depth/2,0),new Vector2(Depth/4,rise/2)};mesh.RecalculateNormals();mesh.RecalculateTangents();
                end.AddComponent<MeshFilter>().sharedMesh=mesh;end.AddComponent<MeshRenderer>().sharedMaterial=brick;
            }
            // Six white columns and deep entablature are the visible historic facade signature.
            float porticoWidth=Width*.59f, columnZ=front-2.6f;
            for(int i=0;i<6;i++)
            {
                float x=-porticoWidth/2+i*porticoWidth/5;
                Box(t,"Wesley column shaft",new Vector3(x,h/2,columnZ),new Vector3(.46f,h-.5f,.46f),trim);
                foreach(float y in new[]{.16f,h-.3f})Box(t,"Wesley column capital and base",new Vector3(x,y,columnZ),new Vector3(.68f,.32f,.68f),trim,false);
            }
            Box(t,"Portico entablature",new Vector3(0,h-.05f,front-1.4f),new Vector3(porticoWidth+1,.52f,3.5f),trim,false);
            Box(t,"Portico lead roof",new Vector3(0,h+.24f,front-1.4f),new Vector3(porticoWidth+1.1f,.1f,3.6f),roof,false);
            for(float x=-porticoWidth/2;x<=porticoWidth/2;x+=.48f)Box(t,"Portico dentil",new Vector3(x,h-.38f,front-3.1f),new Vector3(.17f,.16f,.23f),trim,false);
            foreach(float x in new[]{-Width*.40f,-Width*.25f,Width*.25f,Width*.40f})
            {
                var dormer=new GameObject("Wesley roof dormer").transform;dormer.SetParent(t,false);dormer.localPosition=new Vector3(x,h+1.0f,front+2.1f);
                Box(dormer,"Dormer enclosure",new Vector3(0,.5f,.55f),new Vector3(1.75f,1.9f,1.35f),trim,false);
                FramedWindow(dormer,new Vector3(0,.5f,-.17f),1.25f,1.55f);
                foreach(int side in new[]{-1,1}){var cap=Box(dormer,"Dormer pitched cap",new Vector3(side*.47f,1.62f,.45f),new Vector3(1.1f,.12f,1.75f),trim,false);cap.transform.localRotation=Quaternion.Euler(0,0,-side*27);}
            }
            foreach(int side in new[]{-1,1})Box(t,"Wesley brick chimney",new Vector3(side*(Width/2-2),h+2.4f,0),new Vector3(1.1f,3.4f,1.25f),brick,false);
            Box(t,"Wesley entrance walk",new Vector3(0,-.02f,front-4),new Vector3(5,.04f,8),floor);
        }
    }
}
