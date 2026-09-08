from pathlib import Path
root=Path(__file__).resolve().parents[1]/'Unity/Assets'
p=root/'Scripts/CampusGeometry.cs';s=p.read_text();s=s.replace('static GameObject Box(Transform p,string name,Vector3 at,Vector3 size,Material mat,bool solid=true)=>KeeperAvatar.Part(p,name,PrimitiveType.Cube,at,size,mat,solid);','''static GameObject Box(Transform p,string name,Vector3 at,Vector3 size,Material mat,bool solid=true)
        {
            var o=KeeperAvatar.Part(p,name,PrimitiveType.Cube,at,size,mat,solid);
            if(mat.mainTexture!=null){var mesh=o.GetComponent<MeshFilter>().mesh;var uv=new Vector2[mesh.vertexCount];var v=mesh.vertices;var n=mesh.normals;for(int i=0;i<v.Length;i++){var w=Vector3.Scale(v[i],size)+at;uv[i]=Mathf.Abs(n[i].y)>.7f?new Vector2(w.x,w.z)/2:Mathf.Abs(n[i].x)>.7f?new Vector2(w.z,w.y)/2:new Vector2(w.x,w.y)/2;}mesh.uv=uv;mesh.RecalculateTangents();}
            return o;
        }''')
s=s.replace('brick=TowerGeometry.Material("Campus red brick",new Color(.43f,.19f,.13f))','brick=CraftModel.Surface("Campus brick",Color.white,"red_brick_03")')
s=s.replace('grass=TowerGeometry.Material("Campus lawns",new Color(.28f,.39f,.19f));path=TowerGeometry.Material("Campus paths",new Color(.70f,.66f,.53f))','grass=CraftModel.Surface("Campus lawn",new Color(.7f,.8f,.65f),"aerial_grass_rock");path=CraftModel.Surface("Campus paving",Color.white,"concrete_pavement")')
s=s.replace('static void Building(CampusPlace p)\n        {','static void Building(CampusPlace p)\n        {\n            if(p.id=="26")return;')
s=s.replace('Sign(t,p.name,new Vector3(0,4.2f,-d/2-.3f),Mathf.Max(10,Mathf.Min(24,w)));','// Building names appear contextually in the HUD rather than floating across facades.')
s=s.replace('Sign(root,"ALBION COLLEGE\\nCAMPUS QUAD",CampusCatalog.Point(391,207)+Vector3.up*2.5f,4);','')
p.write_text(s)
p=root/'Scripts/OdysseyGame.cs';s=p.read_text().replace('shell=gameObject.AddComponent<CampusShell>();shell.Setup(this);','shell=gameObject.AddComponent<CampusShell>();shell.Setup(this);\n            gameObject.AddComponent<CampusBuildings>().Setup(this);\n            gameObject.AddComponent<CampusHud>().Setup(this);');s=s.replace('if(tour!=null&&tour.HandleInput())return;','if(CampusBuildings.Instance!=null&&CampusBuildings.Instance.HandleInput())return;\n            if(tour!=null&&tour.HandleInput())return;');s=s.replace('if(journalOpen){DrawJournal(width,height);return;}','if(journalOpen){DrawJournal(width,height);return;}\n            if(!building)return;');p.write_text(s)
p=root/'Scripts/CampusTour/CampusShell.cs';s=p.read_text();s=s.replace('if(Button(new Rect(w-230,150,205,42),"Stories / videos · G"))Stories();','').replace('if(Button(new Rect(w-230,202,205,42),"Menu · Esc"))ShowLaunch();','');s=s.replace('STEP INSIDE / WESLEY HALL','STEP INSIDE / FERGUSON HALL').replace('A room with a story.','Explore beyond the doors.').replace('Compare the college’s 360° view with our walkable room study.','Three levels, openable doors and rooms to explore.').replace('"Explore Wesley Hall   →")){game.tour.OpenDirectory();game.tour.Open(game.tour.catalog.ForCampus("50"));}','"Explore Ferguson Hall   →")){CampusBuildings.Instance.Visit();}');s=s.replace('hero=Resources.Load<Texture2D>("CampusTour/Shell/WesleyHero")','hero=Resources.Load<Texture2D>("CampusCraft/FergusonHero")');p.write_text(s)
p=root/'Scripts/CampusExpansion.cs';s=p.read_text();a=s.index('        void OnGUI()');s=s[:a]+'''        // Gameplay controls and prompts are drawn once by CampusHud.
    }
}
''';p.write_text(s)
p=root/'Scripts/CampusTour/CampusTour.cs';s=p.read_text();a=s.index('            if(!IsOpen)\n');b=s.index('            Box(new Rect(0,0,width,height)',a);s=s[:a]+'''            if(!IsOpen){GUI.matrix=old;return;}
'''+s[b:];p.write_text(s)
p=root/'Scripts/CampusLife.cs';s=p.read_text();s=s.replace('if(game.sound.AchievementCaption.Length>0)','if(PanelOpen&&game.sound.AchievementCaption.Length>0)');a=s.index('            if(!PanelOpen)\n',s.index('void OnGUI'));b=s.index('            GUI.color=new Color(.018f',a);s=s[:a]+'            if(!PanelOpen)return;\n'+s[b:];p.write_text(s)
p=root/'Editor/OdysseySetup.cs';s=p.read_text().replace('if(!assetPath.Contains("/Architecture/"))return;','if(!assetPath.Contains("/Architecture/")&&!assetPath.Contains("/CampusCraft/"))return;').replace('assetPath.EndsWith("_Normal.png")','(assetPath.EndsWith("_Normal.png")||assetPath.EndsWith("_Normal.jpg"))').replace('PlayerSettings.bundleVersion="0.7.0"','PlayerSettings.bundleVersion="0.8.0"');p.write_text(s)
