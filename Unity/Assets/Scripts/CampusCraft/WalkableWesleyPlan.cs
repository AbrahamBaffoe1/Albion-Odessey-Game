using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AlbionOdyssey
{
    [Serializable] public class WesleyRect
    {
        public float x,z,width,depth;
        public bool Contains(float px,float pz) => Mathf.Abs(px-x)<width*.5f-.001f && Mathf.Abs(pz-z)<depth*.5f-.001f;
    }
    [Serializable] public sealed class WesleySpace : WesleyRect { public string name,kind,group; public float stairYaw,clearWidth,clearDepth; public int dimensionSourcePage; }
    [Serializable] public sealed class PlanOpening { public string space,side; public float x,z,width; }
    [Serializable] public sealed class WesleyLevel { public int level; public string name; public WesleyRect[] footprints,circulationCorners; public WesleySpace[] spaces; public PlanOpening[] doors; }
    [Serializable] public sealed class PlanTerrace : WesleyRect { public int level; }
    [Serializable] public sealed class WesleyPlan { public string source,status; public float width,depth,floorHeight,entryX,rotation,baseElevation; public int entryLevel=1; public float wallThickness,facadeLeft,facadeRight; public WesleyRect[] roofSections; public PlanTerrace[] terraces; public WesleyLevel[] floors; }

    public sealed partial class WalkableCampusBuilding
    {
        public WesleyPlan Plan {get; private set;}
        public readonly List<string> UnreachablePlanSpaces=new List<string>();
        sealed class PlanEdge
        {
            public bool alongX; public float fixedAt,start,end; public int a,b;
            public float Length=>end-start;
        }
        bool InsidePlan(Vector3 p)
        {
            if(Plan==null||p.y<-.2f||p.y>=Floors*FloorHeight)return false;
            var level=Plan.floors[Mathf.Clamp(Mathf.FloorToInt((p.y+.1f)/FloorHeight),0,Plan.floors.Length-1)];
            return level.footprints.Any(r=>r.Contains(p.x,p.z))||level.spaces.Any(r=>r.Contains(p.x,p.z));
        }
        void BuildReferencePlan(WesleyPlan reference)
        {
            Plan=reference;Root.transform.position=Origin+Vector3.up*Plan.baseElevation;
            Root.transform.rotation=Quaternion.Euler(0,Plan.rotation,0);
            PlanDoor(new Vector3(Plan.entryX,Plan.entryLevel*FloorHeight,-Depth/2),2.2f,false,Place.name+" reference entrance",glass);
            foreach(var level in Plan.floors){BuildPlanLevel(level);Light(new Vector3(0,level.level*FloorHeight+2.8f,0));}
            if(Id=="ingham-stage")DecorateInghamLivingRoom();
        }
        void BuildWesleyPlan()
        {
            brick.color=new Color(.84f,.70f,.55f);
            Plan=JsonUtility.FromJson<WesleyPlan>(Resources.Load<TextAsset>("CampusCraft/wesley-plan").text);
            // The front entrance opens onto the published first floor, above the commons.
            PlanDoor(new Vector3(-3.4f,FloorHeight,-Depth/2),2.2f,false,"Wesley main entrance",glass);
            foreach(var level in Plan.floors) BuildPlanLevel(level);
            // The commons roof forms the lower court; the first-floor lounge has its own roof.
            Box(Root.transform,"Commons roof terrace",new Vector3(0,FloorHeight-.09f,-13.6f),new Vector3(20.4f,.18f,23.8f),floor);
            Box(Root.transform,"Recreation lounge roof",new Vector3(0,2*FloorHeight-.09f,3.4f),new Vector3(20.4f,.18f,10.2f),roof);
            WesleyFrontage();
        }
        void BuildSeatonPlan()
        {
            brick.color=new Color(.84f,.70f,.55f);
            // Albion's Seaton room photo shows pale painted walls, a plain floor and oak furniture.
            plaster.color=new Color(.85f,.83f,.74f);wood.color=new Color(.50f,.34f,.17f);
            floor.mainTexture=null;floor.SetTexture("_BumpMap",null);floor.DisableKeyword("_NORMALMAP");floor.color=new Color(.64f,.61f,.54f);
            Plan=JsonUtility.FromJson<WesleyPlan>(Resources.Load<TextAsset>("CampusCraft/seaton-plan").text);
            Root.transform.position=Origin+Vector3.up*Plan.baseElevation;
            Root.transform.localRotation=Quaternion.Euler(0,Plan.rotation,0);
            PlanDoor(new Vector3(Plan.entryX,FloorHeight,-Depth/2),2.2f,false,"Seaton Cass Street entrance",glass);
            foreach(var level in Plan.floors)BuildPlanLevel(level);
            float front=-Depth/2;
            // Historic entrance photographs show four supports and a short approach.
            // Dimensions and basement grade are estimates pending an elevation survey.
            Box(Root.transform,"Seaton entry landing",new Vector3(Plan.entryX,FloorHeight-.09f,front-1.2f),new Vector3(11.2f,.18f,2.4f),stone);
            foreach(float offset in new[]{-4.8f,-1.6f,1.6f,4.8f})
            {
                Box(Root.transform,"Seaton portico column",new Vector3(Plan.entryX+offset,FloorHeight+3.2f,front-1.9f),new Vector3(.42f,6.4f,.42f),trim);
                foreach(float capY in new[]{FloorHeight+.12f,FloorHeight+6.35f})Box(Root.transform,"Seaton column capital",new Vector3(Plan.entryX+offset,capY,front-1.9f),new Vector3(.65f,.24f,.65f),trim,false);
            }
            Box(Root.transform,"Seaton portico entablature",new Vector3(Plan.entryX,FloorHeight+6.6f,front-1.2f),new Vector3(11.6f,.5f,2.8f),trim,false);
            Box(Root.transform,"Seaton front cornice",new Vector3(2.3f,Floors*FloorHeight-.12f,front-.12f),new Vector3(Width-4.6f,.32f,.42f),trim,false);
            float grade=-Plan.baseElevation,rise=FloorHeight-grade;
            for(int i=0;i<5;i++)
            {
                float h=(i+1)*rise/5;
                Box(Root.transform,"Seaton entrance step",new Vector3(Plan.entryX,grade+h/2,front-2.4f-(5-i-.5f)*.3f),new Vector3(5f,h,.31f),stone);
            }
        }
        void BuildMitchellPlan()
        {
            brick.color=new Color(.76f,.57f,.43f);plaster.color=new Color(.86f,.84f,.76f);
            floor.mainTexture=null;floor.SetTexture("_BumpMap",null);floor.DisableKeyword("_NORMALMAP");
            Plan=JsonUtility.FromJson<WesleyPlan>(Resources.Load<TextAsset>("CampusCraft/mitchell-plan").text);
            Root.transform.rotation=Quaternion.Euler(0,Plan.rotation,0);
            PlanDoor(new Vector3(0,0,-Depth/2),2.2f,false,"Mitchell west lobby entrance",glass);
            foreach(var level in Plan.floors)BuildPlanLevel(level);
            // Only the first-floor diagram has a connecting lobby between the towers.
            Box(Root.transform,"Mitchell connecting lobby roof",new Vector3(0,FloorHeight+.1f,-9.4f),new Vector3(9.6f,.18f,10.8f),roof);
            foreach(float x in new[]{-6.5f,6.5f})Box(Root.transform,"Mitchell link corner roof",new Vector3(x,FloorHeight+.1f,-12f),new Vector3(3.4f,.18f,5.6f),roof);
            Box(Root.transform,"Mitchell lobby ceiling",new Vector3(0,FloorHeight-.1f,-9.4f),new Vector3(9.6f,.03f,10.8f),plaster,false);
        }
        void BuildWhitehousePlan()
        {
            brick.color=new Color(.68f,.47f,.36f);
            plaster.color=new Color(.85f,.83f,.76f);
            floor.mainTexture=null;floor.SetTexture("_BumpMap",null);floor.DisableKeyword("_NORMALMAP");
            Plan=JsonUtility.FromJson<WesleyPlan>(Resources.Load<TextAsset>("CampusCraft/whitehouse-plan").text);
            PlanDoor(new Vector3(Plan.entryX,0,-Depth/2),2.2f,false,"Whitehouse Porter Street entrance",glass);
            foreach(var level in Plan.floors)BuildPlanLevel(level);
            foreach(var terrace in Plan.terraces)
                Box(Root.transform,"Whitehouse exposed lower roof",new Vector3(terrace.x,terrace.level*FloorHeight+.1f,terrace.z),new Vector3(terrace.width,.18f,terrace.depth),roof);
            // Roof spans follow the fitted plan. Pitch and pediment height remain unmeasured.
            float top=Floors*FloorHeight,front=-Depth/2;
            foreach(var section in Plan.roofSections)
            {
                bool alongX=section.width>section.depth;float run=(alongX?section.depth:section.width)/2;
                float rise=run*Mathf.Tan(16*Mathf.Deg2Rad),slope=Mathf.Sqrt(run*run+rise*rise)+.15f;
                foreach(int side in new[]{-1,1})
                {
                    var at=new Vector3(section.x,top+rise/2,section.z)+(alongX?Vector3.forward:Vector3.right)*side*run/2;
                    var part=Box(Root.transform,"Whitehouse pitched roof",at,alongX?new Vector3(section.width+.4f,.18f,slope):new Vector3(slope,.18f,section.depth+.4f),roof,false);
                    part.transform.localRotation=alongX?Quaternion.Euler(side*16,0,0):Quaternion.Euler(0,0,-side*16);
                }
            }
            float left=Plan.facadeLeft,right=Plan.facadeRight,centre=(left+right)/2,span=right-left;
            foreach(float x in new[]{left,right})Box(Root.transform,"Whitehouse pale frontage pilaster",new Vector3(x,top/2,front-.13f),new Vector3(.45f,top,.12f),stone,false);
            Box(Root.transform,"Whitehouse frontage cornice",new Vector3(centre,top,front-.2f),new Vector3(span+.6f,.24f,.38f),stone,false);
            var pediment=new GameObject("Whitehouse central pediment");pediment.transform.SetParent(Root.transform,false);
            var mesh=new Mesh();mesh.vertices=new[]{new Vector3(left-.3f,top,front-.2f),new Vector3(right+.3f,top,front-.2f),new Vector3(centre,top+2.7f,front-.2f)};
            mesh.triangles=new[]{0,2,1};mesh.RecalculateNormals();pediment.AddComponent<MeshFilter>().sharedMesh=mesh;pediment.AddComponent<MeshRenderer>().sharedMaterial=stone;
            float half=(span+.6f)/2,edgeLength=Mathf.Sqrt(half*half+2.7f*2.7f),angle=Mathf.Atan2(2.7f,half)*Mathf.Rad2Deg;
            foreach(int side in new[]{-1,1})
            {
                var edge=Box(Root.transform,"Whitehouse pediment rake",new Vector3(centre+side*half/2,top+1.35f,front-.24f),new Vector3(edgeLength,.2f,.4f),trim,false);
                edge.transform.localRotation=Quaternion.Euler(0,0,-side*angle);
            }
        }

        static bool IsCirculation(WesleySpace s)=>s.kind=="lounge"||s.kind=="unresolvedStair";
        void BuildPlanLevel(WesleyLevel level)
        {
            var all=level.footprints.Concat<WesleyRect>(level.spaces).Concat(level.circulationCorners).ToArray();
            var xs=all.SelectMany(r=>new[]{r.x-r.width/2,r.x+r.width/2}).Select(v=>Mathf.Round(v*1000)/1000f).Distinct().OrderBy(v=>v).ToArray();
            var zs=all.SelectMany(r=>new[]{r.z-r.depth/2,r.z+r.depth/2}).Select(v=>Mathf.Round(v*1000)/1000f).Distinct().OrderBy(v=>v).ToArray();
            int nx=xs.Length-1,nz=zs.Length-1;var cells=new int[nx,nz];
            // Multiple rectangles can describe one enclosed L-shaped apartment.
            var owners=Enumerable.Range(0,level.spaces.Length).ToArray();
            for(int i=0;i<owners.Length;i++)if(!string.IsNullOrEmpty(level.spaces[i].group))
                for(int j=0;j<i;j++)if(level.spaces[j].group==level.spaces[i].group){owners[i]=j;break;}
            for(int x=0;x<nx;x++)for(int z=0;z<nz;z++)
            {
                float px=(xs[x]+xs[x+1])/2,pz=(zs[z]+zs[z+1])/2;
                cells[x,z]=all.Any(r=>r.Contains(px,pz))?0:-1;
                for(int s=0;s<level.spaces.Length;s++)if(level.spaces[s].Contains(px,pz)&&!IsCirculation(level.spaces[s]))cells[x,z]=owners[s]+1;
                // Stair cores retain their openings even where a commons rectangle overlaps.
                for(int s=0;s<level.spaces.Length;s++)if(level.spaces[s].kind=="stairs"&&level.spaces[s].Contains(px,pz))cells[x,z]=s+1;
                if(level.circulationCorners.Any(r=>r.Contains(px,pz)))cells[x,z]=0;
            }
            float y=level.level*FloorHeight;
            // Merge occupied cells into broad slabs, preserving every stair void.
            var slab=new bool[nx,nz];
            for(int x=0;x<nx;x++)for(int z=0;z<nz;z++)slab[x,z]=cells[x,z]>=0&&(cells[x,z]==0||level.spaces[cells[x,z]-1].kind!="stairs");
            MergeSlabs(slab,xs,zs,y,Place.name+" "+level.name+" floor");
            var edges=new List<PlanEdge>();
            for(int x=0;x<=nx;x++)
            {
                int z=0;while(z<nz){int a=x==0?-1:cells[x-1,z],b=x==nx?-1:cells[x,z];int end=z+1;while(end<nz&&(x==0?-1:cells[x-1,end])==a&&(x==nx?-1:cells[x,end])==b)end++;
                    if(a!=b)edges.Add(new PlanEdge{alongX=false,fixedAt=xs[x],start=zs[z],end=zs[end],a=a,b=b});z=end;}
            }
            for(int z=0;z<=nz;z++)
            {
                int x=0;while(x<nx){int a=z==0?-1:cells[x,z-1],b=z==nz?-1:cells[x,z];int end=x+1;while(end<nx&&(z==0?-1:cells[end,z-1])==a&&(z==nz?-1:cells[end,z])==b)end++;
                    if(a!=b)edges.Add(new PlanEdge{alongX=true,fixedAt=zs[z],start=xs[x],end=xs[end],a=a,b=b});x=end;}
            }
            var entrances=new Dictionary<int,PlanEdge>();
            foreach(var e in edges)
            {
                int id=0;
                if(e.a==0&&e.b>0)id=e.b;
                else if(e.b==0&&e.a>0)id=e.a;
                else if(e.a>0&&e.b>0)
                {
                    if(level.spaces[e.a-1].kind=="stairs"&&level.spaces[e.b-1].kind!="stairs")id=e.b;
                    else if(level.spaces[e.b-1].kind=="stairs"&&level.spaces[e.a-1].kind!="stairs")id=e.a;
                }
                if(id>0&&level.spaces[id-1].kind!="void"&&e.Length>1.15f)
                {
                    bool direct=e.a==0||e.b==0;
                    bool has=entrances.TryGetValue(id,out var prior);
                    bool priorDirect=has&&(prior.a==0||prior.b==0);
                    if(!has||(direct&&!priorDirect)||(direct==priorDirect&&e.Length>prior.Length))entrances[id]=e;
                }
            }
            // Authored openings override heuristic edge selection. Never silently move a
            // source-positioned door to a longer wall when its specified edge is invalid.
            var explicitOpenings=new Dictionary<int,PlanOpening>();
            foreach(var opening in level.doors??Array.Empty<PlanOpening>())
            {
                int index=Array.FindIndex(level.spaces,s=>s.name==opening.space);
                if(index<0)throw new InvalidOperationException("Unknown doorway room "+opening.space);
                int owner=owners[index]+1;bool horizontal=opening.side=="north"||opening.side=="south";
                float fixedAt=horizontal?opening.z:opening.x,along=horizontal?opening.x:opening.z;
                var edge=edges.FirstOrDefault(e=>e.alongX==horizontal&&Mathf.Abs(e.fixedAt-fixedAt)<.002f&&(e.a==owner||e.b==owner)&&(e.a==0||e.b==0)&&along-opening.width/2>=e.start-.002f&&along+opening.width/2<=e.end+.002f);
                entrances.Remove(owner);
                if(edge==null)throw new InvalidOperationException("Invalid source doorway "+level.name+" / "+opening.space);
                entrances[owner]=edge;explicitOpenings[owner]=opening;
            }
            for(int i=0;i<level.spaces.Length;i++)if(owners[i]==i&&!IsCirculation(level.spaces[i])&&level.spaces[i].kind!="void"&&!entrances.ContainsKey(i+1))UnreachablePlanSpaces.Add(level.name+" / "+level.spaces[i].name);
            foreach(var e in edges)
            {
                bool outside=e.a<0||e.b<0;int id=Mathf.Max(e.a,e.b);
                bool entrance=false;
                foreach(var pair in entrances)if(pair.Value==e){id=pair.Key;entrance=true;break;}
                bool main=outside&&e.alongX&&Mathf.Abs(e.fixedAt+Depth/2)<.05f&&e.start<Plan.entryX&&e.end>Plan.entryX&&level.level==Plan.entryLevel;
                bool rear=Id=="50"&&outside&&e.alongX&&Mathf.Abs(e.fixedAt-Depth/2)<.05f&&(e.a==0||e.b==0)&&level.level==0;
                if(main&&Id=="ingham-stage"&&level.level==0)
                {
                    var living=level.spaces.First(s=>s.name=="Living room");
                    float split=living.x+living.width/2;
                    InghamLivingWindows(new PlanEdge{alongX=true,fixedAt=e.fixedAt,start=e.start,end=split,a=e.a,b=e.b},living.x);
                    PlanWallOpening(new PlanEdge{alongX=true,fixedAt=e.fixedAt,start=split,end=e.end,a=e.a,b=e.b},y,2.2f,Plan.entryX,brick);
                }
                else if(main)PlanWallOpening(e,y,2.2f,Plan.entryX,brick);
                else if(rear){PlanWallOpening(e,y,1.4f,(e.start+e.end)/2,brick);PlanDoor(EdgePoint(e,(e.start+e.end)/2,y),1.4f,!e.alongX,"Wesley rear entrance",wood);}
                else if(entrance)
                {
                    var s=level.spaces[id-1];float opening=s.kind=="stairs"?Mathf.Min(2.8f,e.Length-.25f):1f;
                    float at=(e.start+e.end)/2;
                    if(explicitOpenings.TryGetValue(id,out var authored)){opening=authored.width;at=e.alongX?authored.x:authored.z;}
                    int other=e.a==id?e.b:e.a;
                    if(other>0&&level.spaces[other-1].kind=="stairs")
                    {
                        var stair=level.spaces[other-1];float yaw=stair.stairYaw*Mathf.Deg2Rad;
                        // A room adjoining a stair opens on its landing, never into a flight.
                        if(e.alongX)at=stair.x-Mathf.Cos(yaw)*(stair.width/2-.65f);
                        else at=stair.z+Mathf.Sin(yaw)*(stair.depth/2-.65f);
                        at=Mathf.Clamp(at,e.start+opening/2+.05f,e.end-opening/2-.05f);
                    }
                    PlanWallOpening(e,y,opening,at,plaster);
                    if(s.kind!="stairs")PlanDoor(EdgePoint(e,at,y),opening,!e.alongX,level.name+" · "+s.name,wood);
                }
                else if(outside)PlanWindowWall(e,y);
                else PlanWall(e,y+FloorHeight/2,e.start,e.end,FloorHeight,plaster);
            }
            foreach(var s in level.spaces)
            {
                if(s.kind=="stairs")PlanStair(s,level.level);
                else if(suppliedPlan==null&&s.kind=="room"&&char.IsDigit(s.name[0]))WesleyRoomFurniture(s,y);
            }
            if(Id=="46")
            {
                foreach(float x in new[]{-16.7f,16.7f})foreach(float z in new[]{-8f,0f,7f})Light(new Vector3(x,y+2.85f,z));
                if(level.level==0)Light(new Vector3(0,y+2.85f,-12f));
            }
            else if(Id=="51")
            {
                for(float x=-21;x<26;x+=7)Light(new Vector3(x,y+2.85f,-8.5f));
                for(float z=-3;z<15;z+=6)Light(new Vector3(-24,y+2.85f,z));
            }
            else if(Id=="49")
            {
                for(float x=-25;x<30;x+=7)Light(new Vector3(x,y+2.85f,-9.3f));
                for(float z=-2;z<12;z+=6)Light(new Vector3(24.9f,y+2.85f,z));
            }
            else foreach(float x in new[]{-26.2f,-15.4f,15.4f,26.2f})
                foreach(float z in new[]{-25f,-12f,2f,16f,29f})
                    if(level.footprints.Any(r=>r.Contains(x,z)))Light(new Vector3(x,y+2.85f,z));
            // Uppermost roof follows the footprint, leaving the central court open.
            if(level.level==Plan.floors.Length-1)
            {
                for(int x=0;x<nx;x++)for(int z=0;z<nz;z++)slab[x,z]=cells[x,z]>=0;
                MergeSlabs(slab,xs,zs,y+FloorHeight,Place.name+" roof");
            }
        }
        void MergeSlabs(bool[,] mask,float[] xs,float[] zs,float y,string name)
        {
            int nx=mask.GetLength(0),nz=mask.GetLength(1);
            for(int x=0;x<nx;x++)for(int z=0;z<nz;z++)if(mask[x,z])
            {
                int ex=x+1;while(ex<nx&&mask[ex,z])ex++;
                int ez=z+1;bool good=true;while(ez<nz&&good){for(int k=x;k<ex;k++)if(!mask[k,ez]){good=false;break;}if(good)ez++;}
                for(int i=x;i<ex;i++)for(int j=z;j<ez;j++)mask[i,j]=false;
                Box(Root.transform,name,new Vector3((xs[x]+xs[ex])/2,name.EndsWith("roof")?y+.1f:y-.09f,(zs[z]+zs[ez])/2),new Vector3(xs[ex]-xs[x],.18f,zs[ez]-zs[z]),name.EndsWith("roof")?roof:floor);
                if(y>0&&!name.EndsWith("roof"))Box(Root.transform,Place.name+" plaster ceiling",new Vector3((xs[x]+xs[ex])/2,y-.195f,(zs[z]+zs[ez])/2),new Vector3(xs[ex]-xs[x],.03f,zs[ez]-zs[z]),plaster,false);
            }
        }
        Vector3 EdgePoint(PlanEdge e,float along,float y)=>e.alongX?new Vector3(along,y,e.fixedAt):new Vector3(e.fixedAt,y,along);
        void PlanWall(PlanEdge e,float centerY,float start,float end,float height,Material material)
        {
            if(end-start<.01f)return;
            bool fittedFinish=Id=="51"&&material==brick&&(e.a<0||e.b<0);
            float thickness=fittedFinish?.176f:.18f;
            Box(Root.transform,Place.name+" "+(material==brick?"brick wall":"partition"),EdgePoint(e,(start+end)/2,centerY),e.alongX?new Vector3(end-start,height,thickness):new Vector3(thickness,height,end-start),Id=="46"&&material==brick&&centerY>=3*FloorHeight?roof:material);
            if(Id=="51"&&material==brick&&e.alongX&&Mathf.Abs(e.fixedAt+Depth/2)<.05f)
            {
                float a=Mathf.Max(start,Plan.facadeLeft),b=Mathf.Min(end,Plan.facadeRight);
                if(b>a)Box(Root.transform,"Whitehouse pale centre facade",new Vector3((a+b)/2,centerY,e.fixedAt-.105f),new Vector3(b-a,height,.035f),stone,false);
            }
            if((Id=="49"||Id=="51"||Id=="46")&&material==brick&&(e.a<0||e.b<0))
            {
                float sign=e.a<0?1:-1;var normal=e.alongX?Vector3.forward:Vector3.right;
                float finishWidth=fittedFinish?.002f:.025f,offset=fittedFinish?.089f:.105f;
                Box(Root.transform,Place.name+" interior wall finish",EdgePoint(e,(start+end)/2,centerY)+normal*sign*offset,e.alongX?new Vector3(end-start,height,finishWidth):new Vector3(finishWidth,height,end-start),plaster,fittedFinish);
            }
        }
        void PlanWallOpening(PlanEdge e,float y,float opening,float at,Material material)
        {
            PlanWall(e,y+FloorHeight/2,e.start,at-opening/2,FloorHeight,material);PlanWall(e,y+FloorHeight/2,at+opening/2,e.end,FloorHeight,material);
            PlanWall(e,y+(FloorHeight+2.3f)/2,at-opening/2,at+opening/2,FloorHeight-2.3f,material);
        }
        void PlanWindowWall(PlanEdge e,float y)
        {
            int count=Mathf.FloorToInt(e.Length/2.9f);if(count==0){PlanWall(e,y+FloorHeight/2,e.start,e.end,FloorHeight,brick);return;}
            float bay=e.Length/count,window=1.15f;
            for(int i=0;i<count;i++)
            {
                float a=e.start+i*bay,b=a+bay,c=(a+b)/2;
                PlanWall(e,y+FloorHeight/2,a,c-window/2,FloorHeight,brick);PlanWall(e,y+FloorHeight/2,c+window/2,b,FloorHeight,brick);
                PlanWall(e,y+.5f,c-window/2,c+window/2,1f,Id=="46"?trim:brick);PlanWall(e,y+(2.5f+FloorHeight)/2,c-window/2,c+window/2,FloorHeight-2.5f,brick);
                var group=new GameObject(Place.name+" sash window").transform;group.SetParent(Root.transform,false);group.localPosition=EdgePoint(e,c,y+1.75f);if(!e.alongX)group.localRotation=Quaternion.Euler(0,90,0);
                Box(group,"Recessed blue glazing",Vector3.zero,new Vector3(window,1.5f,.075f),glass);
                foreach(float side in new[]{-1f,1f}){Box(group,"Painted sash",new Vector3(side*window/2,0,0),new Vector3(.07f,1.64f,.16f),trim,false);Box(group,"Painted sash",new Vector3(0,side*.75f,0),new Vector3(window+.12f,.07f,.16f),trim,false);}
                Box(group,"Sash meeting rail",Vector3.zero,new Vector3(window,.06f,.12f),trim,false);
                Box(group,"Stone sill",new Vector3(0,-.81f,0),new Vector3(window+.25f,.12f,.32f),stone,false);
            }
        }
        void PlanDoor(Vector3 at,float width,bool rotated,string label,Material material)
        {
            var o=new GameObject(label);o.transform.SetParent(Root.transform,false);o.transform.localPosition=at;if(rotated)o.transform.localRotation=Quaternion.Euler(0,90,0);
            var door=o.AddComponent<CampusDoor>();door.Label=label;door.Build(width,2.3f,material);Doors.Add(door);
        }
        void PlanStair(WesleySpace s,int level)
        {
            float y=level*FloorHeight;bool sideways=Mathf.Abs(Mathf.Sin(s.stairYaw*Mathf.Deg2Rad))>.5f;
            var g=new GameObject(s.name+" · "+level).transform;g.SetParent(Root.transform,false);g.localPosition=new Vector3(s.x,y,s.z);g.localRotation=Quaternion.Euler(0,s.stairYaw,0);
            float w=sideways?s.depth:s.width,d=sideways?s.width:s.depth,landing=1.1f,run=w-landing*2,lane=(d-.25f)/2;
            Box(g,"Stair entry landing",new Vector3(-w/2+landing/2,-.09f,0),new Vector3(landing,.18f,d),floor);
            if(level==0)Box(g,"Ground stair floor",new Vector3(0,-.09f,0),new Vector3(w,.18f,d),floor);
            if(level==Plan.floors.Length-1)return;
            int steps=10;float rise=FloorHeight/2/steps,tread=run/steps;
            for(int i=0;i<steps;i++)
            {
                float h=(i+1)*rise;
                Box(g,"Stair lower tread",new Vector3(-w/2+landing+(i+.5f)*tread,h-.09f,-d/2+lane/2),new Vector3(tread+.01f,.18f,lane),stone);
                float top=FloorHeight/2+h;
                Box(g,"Stair upper tread",new Vector3(w/2-landing-(i+.5f)*tread,top-.09f,d/2-lane/2),new Vector3(tread+.01f,.18f,lane),stone);
            }
            Box(g,"Stair half landing",new Vector3(w/2-landing/2,FloorHeight/2-.09f,0),new Vector3(landing,.18f,d),floor);
            // Solid centre guard prevents stepping sideways between the flights.
            Box(g,"Stair centre guard",new Vector3(0,FloorHeight/2,0),new Vector3(run,FloorHeight,.12f),trim);
        }
        void ResidenceRoomFurniture(WesleySpace s,float y,int residents)
        {
            bool sideways=s.width>s.depth;float w=sideways?s.depth:s.width,d=sideways?s.width:s.depth;
            Light(new Vector3(s.x,y+2.85f,s.z));
            // Official pages describe furnished rooms. Placement and special-room capacity remain provisional.
            for(int resident=0;resident<residents;resident++)
            {
                var g=new GameObject(Place.name+" resident furniture · "+s.name+" · "+resident).transform;
                g.SetParent(Root.transform,false);g.localPosition=new Vector3(s.x,y,s.z);g.localRotation=Quaternion.Euler(0,(sideways?90:0)+180*resident,0);
                // Published furniture size: 38 x 80 inch XL twin; placement remains unverified.
                float x=-w/2+.65f,z=d/2-1.18f;
                Box(g,"XL twin frame · "+s.name,new Vector3(x,.38f,z),new Vector3(1.02f,.18f,2.09f),wood);
                Box(g,"XL twin mattress · "+s.name,new Vector3(x,.56f,z),new Vector3(.9652f,.2f,2.032f),fabric);
                foreach(float dx in new[]{-.42f,.42f})foreach(float dz in new[]{-.91f,.91f})Box(g,"Bed leg · "+s.name,new Vector3(x+dx,.19f,z+dz),new Vector3(.07f,.38f,.07f),wood,false);
                Box(g,"Bed headboard · "+s.name,new Vector3(x,.68f,z+.98f),new Vector3(1.02f,.62f,.07f),wood);
                Box(g,"Bed pillow · "+s.name,new Vector3(x,.73f,z+.67f),new Vector3(.66f,.14f,.36f),trim,false);
                Vector3 desk=new Vector3(w/2-.7f,.74f,d/2-.43f);
                Box(g,"Residence desk · "+s.name,desk,new Vector3(1.15f,.1f,.65f),wood);
                foreach(float dx in new[]{-.48f,.48f})foreach(float dz in new[]{-.25f,.25f})Box(g,"Desk leg · "+s.name,new Vector3(desk.x+dx,.345f,desk.z+dz),new Vector3(.06f,.69f,.06f),wood,false);

                Box(g,"Three-drawer chest · "+s.name,new Vector3(x,.42f,d/2-2.95f),new Vector3(.9f,.84f,.58f),wood);
                for(int drawer=0;drawer<3;drawer++)Box(g,"Drawer handle",new Vector3(x,.16f+drawer*.25f,d/2-3.255f),new Vector3(.24f,.04f,.05f),metal,false);
                Box(g,"Desk shelf",new Vector3(desk.x,1.25f,desk.z+.15f),new Vector3(1.15f,.06f,.3f),wood,false);
                Box(g,"Desk shelf back",new Vector3(desk.x,1f,desk.z+.29f),new Vector3(1.15f,.5f,.05f),wood,false);
                Box(g,"Desk chair seat",new Vector3(desk.x,.45f,desk.z-.65f),new Vector3(.46f,.08f,.46f),wood);
                Box(g,"Desk chair back",new Vector3(desk.x,.72f,desk.z-.85f),new Vector3(.46f,.5f,.07f),wood,false);
                foreach(float dx in new[]{-.18f,.18f})foreach(float dz in new[]{-.18f,.18f})Box(g,"Chair leg",new Vector3(desk.x+dx,.21f,desk.z-.65f+dz),new Vector3(.045f,.42f,.045f),wood,false);
            }
        }
        void WesleyRoomFurniture(WesleySpace s,float y)
        {
            if(Id=="46"){ResidenceRoomFurniture(s,y,2);return;}
            if(Id=="49"){
                // S is explicitly a single in the source diagram. RA/H occupancy is not established.
                int residents=s.name.Contains("-S")||s.name.Contains("-RA")||s.name.Contains("-H")?1:2;
                ResidenceRoomFurniture(s,y,residents);return;
            }
            // Published furniture size: 38 x 80 inch XL twin; placement remains unverified.
            float x=s.x-s.width/2+.65f,z=s.z+s.depth/2-1.18f;
            Box(Root.transform,"XL twin frame · "+s.name,new Vector3(x,y+.38f,z),new Vector3(1.02f,.18f,2.09f),wood);
            Box(Root.transform,"XL twin mattress · "+s.name,new Vector3(x,y+.56f,z),new Vector3(.9652f,.2f,2.032f),fabric);
            foreach(float dx in new[]{-.42f,.42f})foreach(float dz in new[]{-.91f,.91f})Box(Root.transform,"Bed leg · "+s.name,new Vector3(x+dx,y+.19f,z+dz),new Vector3(.07f,.38f,.07f),wood);
            Box(Root.transform,"Bed headboard · "+s.name,new Vector3(x,y+.68f,z+.98f),new Vector3(1.02f,.62f,.07f),wood);
            Box(Root.transform,"Bed pillow · "+s.name,new Vector3(x,y+.73f,z+.67f),new Vector3(.66f,.14f,.36f),trim,false);
            Vector3 desk=new Vector3(s.x+s.width/2-.7f,y+.74f,s.z+s.depth/2-.43f);
            Box(Root.transform,"Residence desk · "+s.name,desk,new Vector3(1.15f,.1f,.65f),wood);
            foreach(float dx in new[]{-.48f,.48f})foreach(float dz in new[]{-.25f,.25f})Box(Root.transform,"Desk leg · "+s.name,new Vector3(desk.x+dx,y+.345f,desk.z+dz),new Vector3(.06f,.69f,.06f),wood);
        }
        void WesleyFrontage()
        {
            float front=-Depth/2,baseY=FloorHeight,top=Floors*FloorHeight;
            // Public photo confirms six white supports; complete facade dimensions are provisional.
            Box(Root.transform,"Wesley porch",new Vector3(-3.4f,baseY-.12f,front-1.8f),new Vector3(17,.24f,3.6f),stone);
            foreach(float x in new[]{-7.5f,-4.5f,-1.5f,1.5f,4.5f,7.5f})
            {
                Box(Root.transform,"Wesley column shaft",new Vector3(x-3.4f,baseY+3,front-2.7f),new Vector3(.4f,6,.4f),trim);
                Box(Root.transform,"Wesley column capital",new Vector3(x-3.4f,baseY+6,front-2.7f),new Vector3(.65f,.25f,.65f),trim,false);
            }
            Box(Root.transform,"Portico entablature",new Vector3(-3.4f,baseY+6.25f,front-1.8f),new Vector3(17.5f,.5f,3.9f),trim,false);
            int n=20;for(int i=0;i<n;i++){float h=(i+1)*baseY/n;Box(Root.transform,"Wesley entrance step",new Vector3(-3.4f,h/2,front-3.6f-(n-i-.5f)*.3f),new Vector3(4.6f,h,.31f),stone);}
            // Cornice and pitched front roof, independent of the flat residential-wing roofs.
            float frontWidth=42f;Box(Root.transform,"Wesley front cornice",new Vector3(0,top,front+5.1f),new Vector3(frontWidth+.5f,.35f,10.4f),trim,false);
            foreach(int sign in new[]{-1,1}){var slope=Box(Root.transform,"Wesley pitched roof",new Vector3(0,top+1.2f,front+5.1f+sign*2.65f),new Vector3(frontWidth+1, .2f,5.95f),roof,false);slope.transform.localRotation=Quaternion.Euler(sign*24,0,0);}
        }
    }
}
