using System;
using System.Linq;
using UnityEngine;
namespace AlbionOdyssey
{
    // Display projections use the existing source-derived campus and trail coordinates.
    // They are deliberately schematic; no new geography is invented for the interface.
    public static class CampusNavigationMap
    {
        public static bool Nature;
        static bool grid=true,labels=true,player=true;
        static string search="",trail="All trails";
        static Vector2 scroll,pan;
        static float zoom=1;
        static NatureMap nature;
        static readonly Color amber=OdysseyUI.Gold,cyan=OdysseyUI.Mint;
        static Color Dim=new Color(.15f,.3f,.29f,.3f);
        public static void Draw(OdysseyGame game,float x,ref int selected,int focus)
        {
            if(nature==null){var asset=Resources.Load<TextAsset>("Nature/trails");if(asset!=null)nature=JsonUtility.FromJson<NatureMap>(asset.text);}
            if(OdysseyUI.Button(new Rect(x,90,153,36),"Campus","nav-campus",!Nature)){Nature=false;Reset();}
            if(OdysseyUI.Button(new Rect(x+160,90,153,36),"Nature trails","nav-nature",Nature)){Nature=true;Reset();}
            OdysseyUI.Text(new Rect(x,140,310,25),Nature?"WHITEHOUSE / TRAIL NETWORK":"CAMPUS / DESTINATION INDEX",16,amber);
            GUI.SetNextControlName("navigation-search");search=GUI.TextField(new Rect(x,177,313,37),search,60,ConsoleMenuStyle.Field(18));
            OdysseyUI.Text(new Rect(x,221,310,24),Nature?"SELECT A TRAIL":"SEARCH BY NAME OR MAP NUMBER",12,OdysseyUI.Muted);
            string[] trails=nature==null?new[]{"All trails"}:new[]{"All trails"}.Concat(nature.paths.Select(p=>p.name).Distinct().OrderBy(p=>p)).ToArray();
            var matches=CampusCatalog.Places.Select((p,i)=>i).Where(i=>CampusCatalog.Places[i].name.IndexOf(search,StringComparison.OrdinalIgnoreCase)>=0||CampusCatalog.Places[i].id==search).ToArray();
            if(Nature&&search.Length>0)trails=trails.Where(t=>t.IndexOf(search,StringComparison.OrdinalIgnoreCase)>=0).ToArray();
            int count=Nature?trails.Length:matches.Length;
            scroll=GUI.BeginScrollView(new Rect(x,255,313,247),scroll,new Rect(0,0,294,Mathf.Max(246,count*36)));
            for(int j=0;j<count;j++)
            {
                if(Nature){string name=trails[j];if(OdysseyUI.Button(new Rect(0,j*36,289,32),name+(name=="Marsh"?" / CLOSED":""),"trail-"+name,trail==name))trail=name;}
                else{int i=matches[j];var p=CampusCatalog.Places[i];if(OdysseyUI.Button(new Rect(0,j*36,289,32),p.id+" / "+p.name,"place-"+i,selected==i))selected=i;}
            }
            GUI.EndScrollView();
            if(count==0)OdysseyUI.Text(new Rect(x+12,270,280,70),"No matching destinations.",18,OdysseyUI.Muted);
            OdysseyUI.Text(new Rect(x,518,290,23),"DISPLAY LAYERS",14,amber);
            if(OdysseyUI.Button(new Rect(x,551,313,32),(grid?"[X]":"[ ]")+" Coordinate grid","layer-grid"))grid=!grid;
            if(OdysseyUI.Button(new Rect(x,588,313,32),(labels?"[X]":"[ ]")+" Destination labels","layer-labels"))labels=!labels;
            if(OdysseyUI.Button(new Rect(x,625,313,32),(player?"[X]":"[ ]")+" Player marker","layer-player"))player=!player;
            if(OdysseyUI.Button(new Rect(x,665,313,43),"Return to campus","nav-close",focus==4))game.life.SetPanel("");
            Rect view=new Rect(x+335,90,785,460);OdysseyUI.Card(view,new Color(.023f,.032f,.032f));
            if(view.Contains(Event.current.mousePosition))
            {
                if(Event.current.type==EventType.ScrollWheel){zoom=Mathf.Clamp(zoom-Event.current.delta.y*.08f,.6f,2.8f);Event.current.Use();}
                if(Event.current.type==EventType.MouseDrag&&Event.current.button==0){pan+=Event.current.delta;Event.current.Use();}
            }
            GUI.BeginGroup(view);OdysseyUI.BeginLineCanvas(785,460);
            if(grid){for(int i=-14;i<25;i++){float a=i*42;OdysseyUI.Line(new Vector2(a-180,0),new Vector2(a+180,460),Dim);OdysseyUI.Line(new Vector2(0,a),new Vector2(785,a-135),Dim);}}
            Vector2 Project(Vector3 p){var n=Nature?new Vector2((p.x-230)/900,(p.z+110)/1050):new Vector2(p.x/1100,(p.z-410)/700);return new Vector2(392+(n.x*610+n.y*105)*zoom,245-n.y*345*zoom)+pan;}
            if(Nature&&nature!=null)
            {
                foreach(var path in nature.paths)
                {
                    bool active=trail=="All trails"||trail==path.name;
                    Color c=path.closedForRepairs?new Color(.9f,.28f,.18f):path.name=="River’s Edge"?cyan:amber;c.a=active?.9f:.13f;
                    for(int i=1;i<path.points.Length;i++)
                    {if(path.closedForRepairs&&i%4<2)continue;var a=path.points[i-1];var b=path.points[i];OdysseyUI.Line(Project(new Vector3(a.x,0,a.z)),Project(new Vector3(b.x,0,b.z)),c,active?2:1);}
                }
                if(labels)foreach(var family in nature.paths.GroupBy(p=>p.name))
                {
                    if(trail!="All trails"&&trail!=family.Key)continue;
                    var longest=family.OrderByDescending(p=>p.points.Length).First();var point=longest.points[longest.points.Length/2];var at=Project(new Vector3(point.x,0,point.z));
                    OdysseyUI.Fill(new Rect(at.x+5,at.y-22,150,22),new Color(.015f,.02f,.018f,.9f));
                    OdysseyUI.Text(new Rect(at.x+9,at.y-23,180,25),family.Key.ToUpperInvariant(),13,longest.closedForRepairs?new Color(1,.3f,.2f):amber);
                }
                var v=Project(Vector3.zero);OdysseyUI.Frame(new Rect(v.x-6,v.y-6,12,12),cyan,2);if(labels)OdysseyUI.Text(new Rect(v.x+12,v.y-28,230,27),"VISITOR CENTER",14,cyan);
                if(player&&game.player.transform.position.x>5000){var v2=Project(game.player.transform.position-new Vector3(6000,20,6000));OdysseyUI.Fill(new Rect(v2.x-4,v2.y-4,8,8),cyan);}
            }
            else
            {
                for(int i=0;i<CampusCatalog.Places.Length;i++)
                {
                    var p=CampusCatalog.Places[i];var v=Project(p.position);bool active=i==selected;float bw=Mathf.Clamp(p.width*.5f*zoom,5,35),bh=Mathf.Clamp(p.depth*.25f*zoom,4,18);
                    OdysseyUI.Frame(new Rect(v.x-bw/2,v.y-bh/2,bw,bh),new Color(.15f,.45f,.43f,.5f));
                    OdysseyUI.Line(v,v+Vector2.up*(active?32:12),active?cyan:new Color(1,.43f,.035f,.45f));var top=v+Vector2.up*(active?32:12);
                    OdysseyUI.Fill(new Rect(top.x-3,top.y-3,6,6),active?cyan:amber);
                    if(active)OdysseyUI.Frame(new Rect(top.x-9,top.y-9,18,18),cyan,2);
                    if(labels&&(active||(i%13==0&&Vector2.Distance(v,Project(CampusCatalog.Places[selected].position))>160)))
                    {string caption=p.name.Length>30?p.name.Substring(0,28)+"…":p.name;OdysseyUI.Text(new Rect(Mathf.Clamp(top.x+10,12,535),Mathf.Clamp(top.y-23,42,390),240,32),caption.ToUpperInvariant(),active?15:12,active?OdysseyUI.White:OdysseyUI.Muted);}
                    if(GUI.Button(new Rect(v.x-10,v.y-38,20,50),GUIContent.none,GUIStyle.none))selected=i;
                }
                if(player&&game.campus.OnCampus){var p=Project(game.player.transform.position);OdysseyUI.Fill(new Rect(p.x-5,p.y-5,10,10),cyan);OdysseyUI.Text(new Rect(p.x+12,p.y+9,100,26),"YOU",13,cyan);}
            }
            OdysseyUI.EndLineCanvas();
            OdysseyUI.Text(new Rect(20,15,500,24),Nature?"WHITEHOUSE NATURE CENTER":"ALBION COLLEGE / CAMPUS SECTOR",14,amber);
            OdysseyUI.Text(new Rect(694,15,75,24),"N ↑",18,cyan);
            OdysseyUI.Text(new Rect(20,422,620,24),"DRAG  Pan     /     SCROLL  Zoom     /     CLICK  Select",13,OdysseyUI.Muted);
            GUI.EndGroup();
            if(OdysseyUI.Button(new Rect(x+1005,509,100,30),"Reset view","map-reset"))Reset();
            var chosen=CampusCatalog.Places[Mathf.Clamp(selected,0,CampusCatalog.Places.Length-1)];
            OdysseyUI.Text(new Rect(x+350,567,765,44),Nature?trail.ToUpperInvariant():chosen.name.ToUpperInvariant(),29,OdysseyUI.White);
            OdysseyUI.Text(new Rect(x+350,614,750,40),Nature?"2018 trail map geometry · estimated scale · Marsh marked closed for repairs":"MAP "+chosen.id+" / "+chosen.category.ToUpperInvariant()+" / Approximate exterior position",15,amber);
            if(OdysseyUI.Button(new Rect(x+335,665,250,43),Nature?(game.player.transform.position.x>5000?"Return from nature center":"Explore nature center"):"Travel to building","nav-travel",focus==0))
            {if(Nature)UnityEngine.Object.FindAnyObjectByType<WhitehouseTrailWorld>()?.ToggleVisit();else game.campus.Travel(chosen);}
            if(OdysseyUI.Button(new Rect(x+598,665,245,43),Nature?"Start forest pursuit":"Campus discoveries","nav-discover",focus==1))
            {if(Nature)game.shared.OpenForest();else game.life.SetPanel("treasures");}
            if(OdysseyUI.Button(new Rect(x+856,665,264,43),"View official source","nav-source",focus==2))Application.OpenURL(Nature?"https://www.albion.edu/wp-content/uploads/2021/09/whitehouse-nature-center-trail-map-1.pdf":CampusCatalog.MapSource);
            OdysseyUI.Text(new Rect(x,723,1110,26),Nature?"TRAIL GEOMETRY / SOURCE MAP     ·     Terrain is provisional. Forest pursuit uses a separate game course.":"CARTOGRAPHY / SCHEMATIC     ·     Source-derived layout; building models are not surveyed replicas.",14,OdysseyUI.Muted);
        }
        static void Reset(){zoom=1;pan=Vector2.zero;scroll=Vector2.zero;search="";}
    }
}
