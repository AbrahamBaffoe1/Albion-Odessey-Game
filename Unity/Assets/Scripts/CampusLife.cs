using System.Collections.Generic;
using UnityEngine;

namespace AlbionOdyssey
{
    public sealed class CampusLife : MonoBehaviour
    {
        OdysseyGame game;
        public string panel="";
        public bool PanelOpen=>panel.Length>0;
        int history,course,subject;
        string courseName="My first class",feedback="";
        GameObject roster;
        public int ClassroomStudentCount{get;private set;}
        public int ClassroomTransStudentCount{get;private set;}
        readonly Queue<GameObject> papers=new Queue<GameObject>();
        float nextPaper;
        GUIStyle heading,text,muted,button;float panelOpenedAt;int menuFocus;
        public void Setup(OdysseyGame owner)
        {
            game=owner;
            foreach(float x in new[]{-44f,-36f,-67f,-61f})
            {
                var lamp=new GameObject("Learning space light").AddComponent<Light>();lamp.type=LightType.Point;
                lamp.transform.position=new Vector3(x,3.5f,1);lamp.range=14;lamp.intensity=3;lamp.color=new Color(1,.94f,.82f);
            }
            RefreshRoster();
            if(!OdysseySmoke.Enabled)SetPanel("welcome");
        }
        public bool InClass=>Mathf.Abs(game.player.transform.position.x+40)<8.5f&&Mathf.Abs(game.player.transform.position.z)<8.5f&&game.player.transform.position.y<3;
        public int NearbyHistory
        {
            get
            {
                var p=game.player.transform.position;
                if(p.y>3)return -1;
                if(Mathf.Abs(p.x+64)<6&&Mathf.Abs(p.z)<6)return p.x< -65.75f?0:p.x> -62.25f?2:1;
                if(InClass)return 0;
                if(Mathf.Abs(p.x)<13&&Mathf.Abs(p.z)<9)return 0;
                return -1;
            }
        }
        public string Location=>game.tour!=null&&game.tour.InRoom?"WESLEY / ROOM STUDY":game.campus!=null&&game.campus.OnCampus?game.campus.Nearest.name.ToUpperInvariant():InClass?"THE COMMON CLASSROOM":Mathf.Abs(game.player.transform.position.x+64)<7&&Mathf.Abs(game.player.transform.position.z)<7?"HISTORY PAVILION":game.player.transform.position.x>30?"YOUR CAMPUS":"LEGACY HALL";
        public void SetPanel(string value)
        {
            if(value!=""&&game.journalOpen)game.SetJournal(false);
            if(panel=="settings"&&value!="settings"&&game.campus!=null)game.campus.SaveKeeper();
            panel=value;panelOpenedAt=Time.unscaledTime;menuFocus=0;game.player.controls=!PanelOpen&&!game.building;
            game.player.buttonMove=Vector2.zero;game.player.buttonTurn=0;
            bool free=PanelOpen||game.building||game.player.pointerControls;
            Cursor.lockState=free?CursorLockMode.None:CursorLockMode.Locked;Cursor.visible=free;
        }
        public void Travel(int destination)
        {
            if(!game.player.TryExitVehicle()){game.notice="Move the car into an open space before traveling.";return;}
            if(game.building)game.ToggleMode();
            game.player.Teleport(destination==0?new Vector3(0,.05f,-22):destination==1?new Vector3(-40,.05f,-12):destination==2?new Vector3(-64,.05f,-9):new Vector3(48,.05f,-20));
            game.player.transform.rotation=Quaternion.identity;SetPanel("");
            game.notice=destination==1?"Enter the classroom. K opens courses. H opens the history lesson.":destination==2?"Enter the pavilion. Stand by a display and press H.":destination==3?"Your personal campus. F2 opens the building tools.":"Legacy Hall: aim at golden memories and press E or F.";
        }
        public bool HandleInput()
        {
            if(PanelOpen)
            {
                if(AlbionUIInput.Poll(out var horizontal,out var vertical,out var choose,out var cancel)){if(cancel){SetPanel("");return true;}int count=panel=="map"?4:panel=="history"?4:panel=="courses"?7:8;if(vertical!=0){menuFocus=(menuFocus+(vertical>0?-1:1)+count)%count;}if(choose){if(panel=="map")Travel(menuFocus);else if(panel=="history"){if(menuFocus<3)history=menuFocus;else SetPanel("courses");}else if(panel=="courses")ActivateCourseFocus();else if(panel=="welcome"){if(menuFocus==0)SetPanel("");else if(menuFocus==1)game.shell.Stories();else if(menuFocus==2)SetPanel("courses");else if(menuFocus==3)game.shell.Videos();else if(menuFocus==4)game.shell.EndSession();else if(menuFocus==5)SetPanel("settings");else if(menuFocus==6)SetPanel("treasures");else if(menuFocus==7)SetPanel("map");}return true;} }
                if(Input.GetKeyDown(KeyCode.Escape))SetPanel("");
                return true;
            }
            if(game.journalOpen)return false;
            if(Input.GetKeyDown(KeyCode.Escape)||Input.GetKeyDown(KeyCode.F1)){SetPanel("welcome");return true;}
            if(Input.GetKeyDown(KeyCode.M)){SetPanel("campus");return true;}
            if(Input.GetKeyDown(KeyCode.K)){SetPanel("courses");return true;}
            if(Input.GetKeyDown(KeyCode.H))
            {
                if(!game.building&&NearbyHistory>=0){history=NearbyHistory;SetPanel("history");}
                else game.notice="History is inside Legacy Hall, the classroom and the pavilion. Press M for the map.";
                return true;
            }
            if(!game.building&&Input.GetKeyDown(KeyCode.O))
            {
                game.player.pointerControls=!game.player.pointerControls;
                game.player.buttonMove=Vector2.zero;game.player.buttonTurn=0;SetPanel("");
            }
            if(!game.building&&Input.GetKeyDown(KeyCode.P))ThrowPaper();
            return false;
        }
        void ActivateCourseFocus()
        {
            var school=game.state.school;
            if(menuFocus==0)
            {
                bool ok=school.Create(game.state,courseName,subject);
                if(ok){course=school.active;Commit("Course created. Enroll yourself or assign students.");}
                else feedback="Place an unused Hall or Library first. Use a title of 1–40 characters. Limit: six courses.";
            }
            else if(menuFocus==1)subject=(subject+1)%3;
            else if(menuFocus==2)game.shell.PlaceCampusBuildings();
            else if(!school.Exists(course))feedback="Create or select a course first.";
            else if(menuFocus==3)
            {
                if(school.Enroll(course,game.state.active))Commit("You are enrolled. Read the lesson and answer below.");else feedback="You are already enrolled or this class is full.";
            }
            else if(menuFocus==4)
            {
                if(school.AssignStudents(course,game.state.active,1))Commit("Simulated student assigned.");else feedback="The classroom is full or you do not own this course.";
            }
            else if(menuFocus==5)
            {
                if(school.Teach(course,game.state.active)){Commit("Class is in session.");Travel(1);}else feedback="Enroll someone or add a student first.";
            }
            else
            {
                bool enrolled=(school.courses[course].enrolled&(1<<game.state.active))!=0;
                if(!enrolled){feedback="Enroll to answer this lesson.";return;}
                if(school.Answer(course,game.state.active,0))Commit("Lesson answer submitted. Try the other answers if needed.");else feedback="That answer was not correct. Read the lesson above and try again.";
            }
        }
        public bool ThrowPaper()
        {
            if(!InClass||Time.time<nextPaper){game.notice="Paper play is available inside the classroom. M opens the map.";return false;}
            nextPaper=Time.time+.4f;
            game.sound.Play(OdysseyCue.PaperThrow);
            while(papers.Count>=24){var old=papers.Dequeue();if(old!=null)Destroy(old);}
            var paper=GameObject.CreatePrimitive(PrimitiveType.Cube);paper.name="Paper play";
            paper.transform.position=game.player.eyes.transform.position+game.player.eyes.transform.forward*.7f;
            paper.transform.localScale=new Vector3(.13f,.07f,.13f);paper.GetComponent<Renderer>().sharedMaterial=TowerGeometry.Material("Paper",new Color(.95f,.88f,.68f));
            var body=paper.AddComponent<Rigidbody>();body.mass=.03f;body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
            body.linearVelocity=game.player.eyes.transform.forward*9+Vector3.up*2;body.angularVelocity=new Vector3(5,3,2);
            Physics.IgnoreCollision(paper.GetComponent<Collider>(),game.player.body);
            papers.Enqueue(paper);Destroy(paper,6);game.notice="Paper break! P throws a paper ball. Class progress is saved.";return true;
        }
        public void RefreshRoster()
        {
            if(roster!=null){roster.SetActive(false);Destroy(roster);}roster=new GameObject("Local classroom students");ClassroomStudentCount=0;ClassroomTransStudentCount=0;
            var school=game.state.school;if(!school.Exists(school.active))return;
            var c=school.courses[school.active];
            for(int i=0;i<c.Seats;i++)
            {
                float x=-40+new[]{-5.5f,-2.8f,2.8f,5.5f}[i%4],z=-3+(i/4)*2.5f-.85f;
                var profile=CampusStudentProfiles.Get(32+i);
                var mat=TowerGeometry.Material("Student coat "+i,KeeperAvatar.Coats[profile.Coat]);
                var torso=GameObject.CreatePrimitive(PrimitiveType.Capsule);torso.name="Simulated student "+(i+1);torso.transform.SetParent(roster.transform);
                torso.transform.position=new Vector3(x,.94f,z);torso.transform.localScale=new Vector3(.4f,.35f,.3f);torso.GetComponent<Renderer>().sharedMaterial=mat;Destroy(torso.GetComponent<Collider>());
                var identity=torso.AddComponent<CampusStudentIdentity>();identity.Apply(profile);ClassroomStudentCount++;if(profile.IsTrans)ClassroomTransStudentCount++;
                var head=GameObject.CreatePrimitive(PrimitiveType.Sphere);head.transform.SetParent(torso.transform,false);head.transform.localPosition=new Vector3(0,1.3f,0);head.transform.localScale=new Vector3(.7f,.8f,.85f);
                head.GetComponent<Renderer>().sharedMaterial=TowerGeometry.Material("Student skin "+i,KeeperAvatar.Skin[profile.Skin]);Destroy(head.GetComponent<Collider>());
                if(profile.Hair!=2)KeeperAvatar.Part(head.transform,"Classroom student hair",PrimitiveType.Sphere,new Vector3(0,.34f,-.02f),new Vector3(.78f,.36f,.86f),TowerGeometry.Material("Student hair "+i,profile.Hair==1?new Color(.17f,.075f,.035f):new Color(.045f,.025f,.016f)));
            }
        }
        void Commit(string message)
        {
            feedback=game.Save()?message:game.notice;RefreshRoster();
        }
        bool Button(float x,float y,float w,string label)=>GUI.Button(new Rect(x,y,w,38),label,button);
        void Label(float x,float y,float w,float h,string value,GUIStyle style=null)=>GUI.Label(new Rect(x,y,w,h),value,style??text);
        void Card(Rect r,Color accent)
        {
            var old=GUI.color;
            GUI.color=new Color(.045f,.060f,.095f,.92f);GUI.DrawTexture(r,Texture2D.whiteTexture);
            GUI.color=accent;GUI.DrawTexture(new Rect(r.x,r.y,4,r.height),Texture2D.whiteTexture);
            GUI.color=new Color(accent.r,accent.g,accent.b,.45f);GUI.DrawTexture(new Rect(r.x+4,r.y,r.width-4,2),Texture2D.whiteTexture);
            GUI.color=old;
        }
        string FocusLabel(int index,string value)=>menuFocus==index?"▶ "+value:value;
        void FocusBox(Rect r,int index)
        {
            if(menuFocus!=index)return;
            var old=GUI.color;GUI.color=AlbionUITheme.Cyan;
            GUI.DrawTexture(new Rect(r.x-3,r.y-3,r.width+6,3),Texture2D.whiteTexture);GUI.DrawTexture(new Rect(r.x-3,r.yMax,r.width+6,3),Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.x-3,r.y,3,r.height),Texture2D.whiteTexture);GUI.DrawTexture(new Rect(r.xMax,r.y,3,r.height),Texture2D.whiteTexture);GUI.color=old;
        }
        void OnGUI()
        {
            if(game==null||game.player==null||panel=="tour"||panel=="launch"||panel=="sessionend")return;
            if(heading==null)
            {
                heading=new GUIStyle(GUI.skin.label){font=AlbionUITheme.DisplayFont,fontSize=AlbionUITheme.TextSize(30),fontStyle=FontStyle.Bold};heading.normal.textColor=new Color(.96f,.94f,.86f);
                text=new GUIStyle(GUI.skin.label){font=AlbionUITheme.BodyFont,fontSize=AlbionUITheme.TextSize(18),wordWrap=true,richText=false};
                muted=new GUIStyle(text){fontSize=AlbionUITheme.TextSize(14)};
                button=AlbionUITheme.Button(16);
                foreach(var appearance in new[]{button.normal,button.hover,button.active,button.focused})
                {appearance.background=Texture2D.whiteTexture;appearance.textColor=new Color(.98f,.96f,.9f);}
            }
            float scale=Mathf.Min(Screen.width/1280f,Screen.height/800f);GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));
            float width=Screen.width/scale,height=Screen.height/scale;Rect safe=AlbionUITheme.SafeArea(scale);GUI.matrix=AlbionUITheme.Slide(GUI.matrix,panelOpenedAt,OdysseyAccessibility.ReducedMotion);
            GUI.backgroundColor=new Color(.40f,.22f,.57f);
            if(PanelOpen&&game.sound.AchievementCaption.Length>0)
            {
                GUI.color=new Color(.025f,.09f,.10f,.96f);GUI.DrawTexture(new Rect(width-440,138,420,78),Texture2D.whiteTexture);GUI.color=Color.white;
                Label(width-423,147,390,22,"ACHIEVEMENT UNLOCKED",muted);
                Label(width-423,174,390,40,game.sound.AchievementCaption,text);
            }
            if(!PanelOpen)return;
            GUI.color=new Color(.025f,.028f,.052f,1f);GUI.DrawTexture(new Rect(0,0,width,height),Texture2D.whiteTexture);GUI.color=Color.white;
            GUI.color=new Color(1f,.76f,.28f,1f);GUI.DrawTexture(new Rect(0,0,width,5),Texture2D.whiteTexture);GUI.color=Color.white;
            float left=(width-1120)/2;
            Label(left,28,920,50,panel=="campus"?"EXPLORE ALBION COLLEGE":panel=="settings"?"GAME SETTINGS / YOUR CHARACTER":panel=="treasures"?"CAMPUS TREASURES":panel=="map"?"LEGACY CAMPUS":panel=="history"?"ALBION / LEARN THE STORY":panel=="courses"?"YOUR CAMPUS / COURSES":"ALBION ODYSSEY",heading);
            if(Button(left+965,32,155,"Return · Esc"))SetPanel("");
            if(panel=="campus"||panel=="settings"||panel=="treasures") {game.campus.DrawPanel(panel,left);}
            else if(panel=="welcome")
            {
                Card(new Rect(left-16,134,545,330),AlbionUITheme.Cyan);
                Card(new Rect(left+570,134,550,330),AlbionUITheme.Gold);
                Card(new Rect(left-16,510,1120,225),AlbionUITheme.Purple);
                Label(left+12,140,500,20,"PLAYER CONTROLS",muted);
                Label(left+602,140,500,20,"CAMPUS LOOP",muted);
                Label(left,95,1080,50,"Build your campus. Discover its stories. Start a class.",text);
                Label(left,155,540,340,"MOVE  W A S D or ↑ ↓ ← →\nLOOK  Mouse   ·   RUN  Shift   ·   JUMP  Space\nPICK UP / TALK  E or F, aimed at the object\nHISTORY  H near a building   ·   ALL STORIES  G\nCOURSES  K   ·   MAP / TRAVEL  M\nBUILD MODE  F2   ·   JOURNAL  J\nKEEPER  Tab   ·   BEACON  C\nON-SCREEN ARROWS  O   ·   TURN  Z / X\nCHARACTER SETTINGS  F3   ·   CAMERA  V\nHELP / PAUSE  Esc or F1",text);
                Label(left+590,155,510,260,"NEW: EXPLORE ALBION\n61 campus destinations · 7 discoveries\nE enters / exits a car. Space brakes.\nF3 lets you choose or create your character.\n\nLEGACY CAMPUS\nCollect the golden memories at Legacy Hall.\nPress F2, then 4, and click a tile to build a Hall.\nPress K to name a course for that building.\nEnroll, assign students and travel to class.\nRead the lesson and answer its question.",text);
                Label(left+590,447,510,75,"Version 0.15 · Four local Keepers on this Mac.\nHost or join a LAN campus with F5; movement, names and chat stay in sync.",muted);
                if(Button(left,530,260,FocusLabel(0,"Play / resume")))SetPanel("");
                if(Button(left+280,530,260,FocusLabel(1,"Building stories · G")))game.shell.Stories();
                if(Button(left+840,530,230,FocusLabel(3,"College videos")))game.shell.Videos();
                if(Button(left+560,530,260,FocusLabel(2,"Create a course")))SetPanel("courses");
                if(Button(left,680,260,FocusLabel(5,"Character settings · F3")))SetPanel("settings");
                if(Button(left+280,680,260,FocusLabel(6,"Campus treasures")))SetPanel("treasures");
                if(Button(left+560,680,260,FocusLabel(7,"Legacy Hall & builder")))SetPanel("map");
                if(Button(left,600,260,FocusLabel(4,"Finish session")))game.shell.EndSession();
                FocusBox(new Rect(left,530,260,38),0);FocusBox(new Rect(left+280,530,260,38),1);FocusBox(new Rect(left+560,530,260,38),2);FocusBox(new Rect(left+840,530,230,38),3);FocusBox(new Rect(left,600,260,38),4);FocusBox(new Rect(left,680,260,38),5);FocusBox(new Rect(left+280,680,260,38),6);FocusBox(new Rect(left+560,680,260,38),7);
                Label(left+320,594,260,30,"SOUND EFFECTS  "+Mathf.RoundToInt(game.sound.Volume*100)+"%",muted);
                float volume=GUI.HorizontalSlider(new Rect(left+320,637,330,28),game.sound.Volume,0,1);
                bool effectsMuted=GUI.Toggle(new Rect(left+690,602,180,32),game.sound.Muted,"Mute effects");
                game.sound.SetPreferences(volume,effectsMuted);
                if(Button(left+870,600,230,"Preview pickup sound"))game.sound.Play(OdysseyCue.Memory);
            }
            else if(panel=="map")
            {
                string[] names={"01   LEGACY HALL","02   THE COMMON CLASSROOM","03   HISTORY PAVILION","04   YOUR PERSONAL CAMPUS"};
                string[] detail={"Eight furnished floors · memories · Pip's journal","Twelve seats · your courses · paper-play sandbox","Three displays · sourced Albion history · design workshop","Your own buildings · fantasy or campus palette"};
                for(int i=0;i<4;i++)
                {
                    float x=left+(i%2)*570,y=125+(i/2)*230;
                    Card(new Rect(x,y,545,205),i%2==0?AlbionUITheme.Cyan:AlbionUITheme.Gold);
                    Label(x+22,y+20,500,45,names[i],text);Label(x+22,y+72,480,60,detail[i],muted);
                    if(Button(x+22,y+140,220,FocusLabel(i,"Travel here")))Travel(i);
                    FocusBox(new Rect(x+22,y+140,220,38),i);
                }
                Label(left,620,1100,70,"All destinations can also be reached on foot. These connected spaces use original game architecture; the arrangement is not a surveyed map of Albion College.",muted);
            }
            else if(panel=="history")
            {
                Card(new Rect(left-16,180,1120,395),AlbionUITheme.Gold);
                for(int i=0;i<3;i++)if(Button(left+i*375,110,360,FocusLabel(i,CampusLessons.Titles[i])))history=i;
                FocusBox(new Rect(left,110,360,38),0);FocusBox(new Rect(left+375,110,360,38),1);FocusBox(new Rect(left+750,110,360,38),2);
                Label(left,190,1090,60,CampusLessons.Titles[history],heading);
                Label(left,270,1000,180,CampusLessons.Text[history],text);
                Label(left,485,1050,90,history<2?"Source: Albion College\n"+CampusLessons.Sources[history]:"Original game design workshop · not a historical claim",muted);
                if(history<2&&Button(left,590,260,FocusLabel(3,"Open college source")))Application.OpenURL(CampusLessons.Sources[history]);
                if(Button(left+280,590,260,FocusLabel(3,"Take a course")))SetPanel("courses");
                FocusBox(new Rect(left,590,260,38),3);FocusBox(new Rect(left+280,590,260,38),3);
            }
            else if(panel=="courses")DrawCourses(left);
            float footerY=Mathf.Min(height-36,safe.yMax-28);Label(Mathf.Max(left,safe.xMin),footerY,1120,24,"STICK  Navigate   ·   TRIGGER  Select   ·   MENU  Back",muted);
        }
        void DrawCourses(float x)
        {
            var school=game.state.school;
            Card(new Rect(x-16,120,380,390),AlbionUITheme.Cyan);
            Card(new Rect(x+359,120,380,390),AlbionUITheme.Purple);
            Card(new Rect(x+749,120,380,390),AlbionUITheme.Gold);
            Card(new Rect(x-16,500,1140,245),AlbionUITheme.Cyan);
            Label(x,88,1100,32,"LOCAL CLASSROOM · Keeper "+(game.state.active+1)+" · shared on this Mac · 12 seats per class",muted);
            Label(x,133,345,28,"CREATE A COURSE",text);
            courseName=GUI.TextField(new Rect(x,170,340,36),courseName,40);
            for(int i=0;i<3;i++)if(Button(x,220+i*46,340,(subject==i?"● ":"○ ")+CampusLessons.Titles[i]))subject=i;
            if(Button(x,370,340,FocusLabel(0,"Create in my Hall / Library")))
            {
                bool ok=school.Create(game.state,courseName,subject);
                if(ok){course=school.active;Commit("Course created. Enroll yourself or assign students.");}
                else feedback="Place an unused Hall or Library first with the button below. Use a title of 1–40 characters. Limit: six courses.";
            }
            if(Button(x,422,340,FocusLabel(2,"Place a Hall or Library")))game.shell.PlaceCampusBuildings();
            FocusBox(new Rect(x,370,340,38),0);FocusBox(new Rect(x,422,340,38),2);FocusBox(new Rect(x,220+subject*46,340,38),1);
            Label(x,473,340,39,"One course per Hall or Library. Remove its course before reclaiming that building.",muted);
            for(int i=0;i<6;i++)if(school.Exists(i)&&Button(x+375,133+i*48,355,(i==course?"● ":"")+school.courses[i].title))course=i;
            float rx=x+765;
            if(school.Exists(course))
            {
                var c=school.courses[course];bool owner=c.owner==game.state.active;
                Label(rx,130,355,70,c.title,text);
                Label(rx,194,355,106,$"Owner: Keeper {c.owner+1} · Plot {c.plot+1}\nSchedule: {c.ScheduleLabel}\nSeats {c.Seats}/12 · Simulated students {c.students}\nClass sessions: {c.sessions}",muted);
                bool enrolled=(c.enrolled&(1<<game.state.active))!=0;
                if(Button(rx,280,355,FocusLabel(3,enrolled?"Enrolled as Keeper "+(game.state.active+1):"Enroll this Keeper")))
                {if(school.Enroll(course,game.state.active))Commit("You are enrolled. Read the lesson and answer below.");else feedback=enrolled?"You are already enrolled.":"This class is full.";}
                GUI.enabled=owner;
                if(Button(rx,325,170,FocusLabel(4,"+ Student"))){if(school.AssignStudents(course,game.state.active,1))Commit("Simulated student assigned.");else feedback="The classroom is full.";}
                if(Button(rx+185,325,170,"− Student")){if(school.AssignStudents(course,game.state.active,-1))Commit("Student removed.");}
                if(Button(rx,370,355,FocusLabel(5,"Run class / travel to classroom")))
                {if(school.Teach(course,game.state.active)){Commit("Class is in session.");Travel(1);}else feedback="Enroll someone or add a student first.";}
                if(Button(rx,415,355,"Remove course")){school.Remove(course,game.state.active);Commit("Course removed. Its building can now be reclaimed.");}
                GUI.enabled=true;
                FocusBox(new Rect(rx,280,355,38),3);FocusBox(new Rect(rx,325,170,38),4);FocusBox(new Rect(rx,370,355,38),5);
                Label(x,515,1100,65,CampusLessons.Text[c.subject],muted);
                Label(x,590,1100,32,CampusLessons.Questions[c.subject],text);
                GUI.enabled=enrolled;
                for(int i=0;i<3;i++)if(Button(x+i*375,632,360,FocusLabel(6,CampusLessons.Answers[c.subject][i])))
                {
                    if((c.graduates&(1<<game.state.active))!=0)feedback="You already completed this lesson.";
                    else if(school.Answer(course,game.state.active,i))Commit("Correct! Lesson completed and saved for this Keeper.");
                    else feedback="Try again. Read the lesson above for the answer.";
                }
                GUI.enabled=true;
                FocusBox(new Rect(x,632,1100,38),6);
                Label(x,685,1100,28,(c.graduates&(1<<game.state.active))!=0?"✓ LESSON COMPLETED":enrolled?"Read, then choose an answer.":"Enroll to answer this lesson.",muted);
            }
            else Label(x+765,150,340,160,"Build a Hall or Library, create a course and give students a place to learn. The Common Classroom hosts your active class.",text);
            Label(x,735,1110,55,feedback,muted);
        }
    }
}
