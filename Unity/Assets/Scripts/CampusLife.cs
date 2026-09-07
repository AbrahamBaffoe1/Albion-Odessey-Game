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
        readonly Queue<GameObject> papers=new Queue<GameObject>();
        float nextPaper;
        GUIStyle heading,text,muted,button;
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
        public string Location=>InClass?"THE COMMON CLASSROOM":Mathf.Abs(game.player.transform.position.x+64)<7&&Mathf.Abs(game.player.transform.position.z)<7?"HISTORY PAVILION":game.player.transform.position.x>30?"YOUR CAMPUS":"LEGACY HALL";
        public void SetPanel(string value)
        {
            if(value!=""&&game.journalOpen)game.SetJournal(false);
            panel=value;game.player.controls=!PanelOpen&&!game.building;
            game.player.buttonMove=Vector2.zero;game.player.buttonTurn=0;
            bool free=PanelOpen||game.building||game.player.pointerControls;
            Cursor.lockState=free?CursorLockMode.None:CursorLockMode.Locked;Cursor.visible=free;
        }
        public void Travel(int destination)
        {
            if(game.building)game.ToggleMode();
            game.player.Teleport(destination==0?new Vector3(0,.05f,-22):destination==1?new Vector3(-40,.05f,-12):destination==2?new Vector3(-64,.05f,-9):new Vector3(48,.05f,-20));
            game.player.transform.rotation=Quaternion.identity;SetPanel("");
            game.notice=destination==1?"Enter the classroom. K opens courses. H opens the history lesson.":destination==2?"Enter the pavilion. Stand by a display and press H.":destination==3?"Your personal campus. F2 opens the building tools.":"Legacy Hall: aim at golden memories and press E or F.";
        }
        public bool HandleInput()
        {
            if(PanelOpen)
            {
                if(Input.GetKeyDown(KeyCode.Escape))SetPanel("");
                return true;
            }
            if(game.journalOpen)return false;
            if(Input.GetKeyDown(KeyCode.Escape)||Input.GetKeyDown(KeyCode.F1)){SetPanel("welcome");return true;}
            if(Input.GetKeyDown(KeyCode.M)){SetPanel("map");return true;}
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
            if(roster!=null){roster.SetActive(false);Destroy(roster);}roster=new GameObject("Local classroom students");
            var school=game.state.school;if(!school.Exists(school.active))return;
            var c=school.courses[school.active];
            for(int i=0;i<c.Seats;i++)
            {
                float x=-40+new[]{-5.5f,-2.8f,2.8f,5.5f}[i%4],z=-3+(i/4)*2.5f-.85f;
                var mat=TowerGeometry.Material("Student coat "+i,new Color(.18f+(i%3)*.13f,.3f,.5f+(i%2)*.15f));
                var torso=GameObject.CreatePrimitive(PrimitiveType.Capsule);torso.name="Simulated student "+(i+1);torso.transform.SetParent(roster.transform);
                torso.transform.position=new Vector3(x,.94f,z);torso.transform.localScale=new Vector3(.4f,.35f,.3f);torso.GetComponent<Renderer>().sharedMaterial=mat;Destroy(torso.GetComponent<Collider>());
                var head=GameObject.CreatePrimitive(PrimitiveType.Sphere);head.transform.SetParent(torso.transform,false);head.transform.localPosition=new Vector3(0,1.3f,0);head.transform.localScale=new Vector3(.7f,.8f,.85f);
                head.GetComponent<Renderer>().sharedMaterial=TowerGeometry.Material("Student skin "+i,new Color(.34f+(i%4)*.14f,.22f+(i%4)*.12f,.16f+(i%4)*.10f));Destroy(head.GetComponent<Collider>());
            }
        }
        void Commit(string message)
        {
            feedback=game.Save()?message:game.notice;RefreshRoster();
        }
        bool Button(float x,float y,float w,string label)=>GUI.Button(new Rect(x,y,w,38),label,button);
        void Label(float x,float y,float w,float h,string value,GUIStyle style=null)=>GUI.Label(new Rect(x,y,w,h),value,style??text);
        void OnGUI()
        {
            if(game==null||game.player==null)return;
            if(heading==null)
            {
                heading=new GUIStyle(GUI.skin.label){fontSize=30,fontStyle=FontStyle.Bold};
                text=new GUIStyle(GUI.skin.label){fontSize=18,wordWrap=true,richText=false};
                muted=new GUIStyle(text){fontSize=14};
                button=new GUIStyle(GUI.skin.button){fontSize=16,richText=false,border=new RectOffset(),padding=new RectOffset(12,12,4,4)};
                foreach(var appearance in new[]{button.normal,button.hover,button.active,button.focused})
                {appearance.background=Texture2D.whiteTexture;appearance.textColor=Color.white;}
            }
            float scale=Mathf.Min(Screen.width/1280f,Screen.height/800f);GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));
            float width=Screen.width/scale,height=Screen.height/scale;
            GUI.backgroundColor=new Color(.12f,.27f,.30f);
            if(game.sound.AchievementCaption.Length>0)
            {
                GUI.color=new Color(.025f,.09f,.10f,.96f);GUI.DrawTexture(new Rect(width-440,138,420,78),Texture2D.whiteTexture);GUI.color=Color.white;
                Label(width-423,147,390,22,"ACHIEVEMENT UNLOCKED",muted);
                Label(width-423,174,390,40,game.sound.AchievementCaption,text);
            }
            if(!PanelOpen)
            {
                if(game.building||game.journalOpen)return;
                if(NearbyHistory>=0)Label(width/2-180,height-180,500,25,InClass?"H · History     K · Courses     P · Paper play":"H · Read this place's history",muted);
                if(Physics.Raycast(game.player.eyes.transform.position,game.player.eyes.transform.forward,out var hit,3.5f)&&(hit.collider.GetComponent<MemoryMarker>()!=null||hit.collider.GetComponent<GuideMarker>()!=null))Label(width/2-110,height/2+25,250,28,"E / F · Pick up or interact",text);
                if(game.player.pointerControls)
                {
                    float x=width-290,y=height-290;
                    game.player.buttonMove=new Vector2((GUI.RepeatButton(new Rect(x+120,y+54,58,46),"→",button)?1:0)-(GUI.RepeatButton(new Rect(x,y+54,58,46),"←",button)?1:0),(GUI.RepeatButton(new Rect(x+60,y,58,46),"↑",button)?1:0)-(GUI.RepeatButton(new Rect(x+60,y+54,58,46),"↓",button)?1:0));
                    game.player.buttonTurn=(GUI.RepeatButton(new Rect(x+180,y,70,46),"Turn R",button)?1:0)-(GUI.RepeatButton(new Rect(x-75,y,70,46),"Turn L",button)?1:0);
                    if(Button(x-75,y+105,155,"Pick up / interact"))game.Interact();
                    if(Button(x+90,y+105,90,"Jump"))game.player.buttonJump=true;
                }
                return;
            }
            GUI.color=new Color(.018f,.03f,.043f,1f);GUI.DrawTexture(new Rect(0,0,width,height),Texture2D.whiteTexture);GUI.color=Color.white;
            float left=(width-1120)/2;
            Label(left,28,920,50,panel=="map"?"EXPLORE THE CAMPUS":panel=="history"?"ALBION / LEARN THE STORY":panel=="courses"?"YOUR CAMPUS / COURSES":"ALBION ODYSSEY",heading);
            if(Button(left+965,32,155,"Return · Esc"))SetPanel("");
            if(panel=="welcome")
            {
                Label(left,95,1080,50,"Build your campus. Discover its stories. Start a class.",text);
                Label(left,155,540,340,"MOVE  W A S D or ↑ ↓ ← →\nLOOK  Mouse   ·   RUN  Shift   ·   JUMP  Space\nPICK UP / TALK  E or F, aimed at the object\nHISTORY  H inside a learning space\nCOURSES  K   ·   MAP / TRAVEL  M\nBUILD MODE  F2   ·   JOURNAL  J\nKEEPER  Tab   ·   BEACON  C\nON-SCREEN ARROWS  O   ·   TURN  Z / X\nHELP / PAUSE  Esc or F1",text);
                Label(left+590,155,510,220,"START HERE\nCollect the golden memories at Legacy Hall.\nPress F2, then 4, and click a tile to build a Hall.\nPress K to name a course for that building.\nEnroll, assign students and travel to class.\nRead the lesson and answer its question.",text);
                Label(left+590,400,510,100,"Version 0.5 · Four local Keepers on this Mac.\nStudents are simulated. Online accounts and multiplayer are still in development.",muted);
                if(Button(left,530,260,"Play / resume"))SetPanel("");
                if(Button(left+280,530,260,"Explore the map"))SetPanel("map");
                if(Button(left+560,530,260,"Create a course"))SetPanel("courses");
                if(Button(left,600,260,"Save and quit")){if(game.Save())Application.Quit();}
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
                    GUI.color=new Color(.07f,.13f,.16f);GUI.DrawTexture(new Rect(x,y,545,205),Texture2D.whiteTexture);GUI.color=Color.white;
                    Label(x+22,y+20,500,45,names[i],text);Label(x+22,y+72,480,60,detail[i],muted);
                    if(Button(x+22,y+140,220,"Travel here"))Travel(i);
                }
                Label(left,620,1100,70,"All destinations can also be reached on foot. These connected spaces use original game architecture; the arrangement is not a surveyed map of Albion College.",muted);
            }
            else if(panel=="history")
            {
                for(int i=0;i<3;i++)if(Button(left+i*375,110,360,CampusLessons.Titles[i]))history=i;
                Label(left,190,1090,60,CampusLessons.Titles[history],heading);
                Label(left,270,1000,180,CampusLessons.Text[history],text);
                Label(left,485,1050,90,history<2?"Source: Albion College\n"+CampusLessons.Sources[history]:"Original game design workshop · not a historical claim",muted);
                if(history<2&&Button(left,590,260,"Open college source"))Application.OpenURL(CampusLessons.Sources[history]);
                if(Button(left+280,590,260,"Take a course"))SetPanel("courses");
            }
            else if(panel=="courses")DrawCourses(left);
        }
        void DrawCourses(float x)
        {
            var school=game.state.school;
            Label(x,88,1100,32,"LOCAL CLASSROOM · Keeper "+(game.state.active+1)+" · shared on this Mac · 12 seats per class",muted);
            Label(x,133,345,28,"CREATE A COURSE",text);
            courseName=GUI.TextField(new Rect(x,170,340,36),courseName,40);
            for(int i=0;i<3;i++)if(Button(x,220+i*46,340,(subject==i?"● ":"○ ")+CampusLessons.Titles[i]))subject=i;
            if(Button(x,370,340,"Create in my Hall / Library"))
            {
                bool ok=school.Create(game.state,courseName,subject);
                if(ok){course=school.active;Commit("Course created. Enroll yourself or assign students.");}
                else feedback="Build an unused Hall or Library first (F2). Use a title of 1–40 characters. Limit: six courses.";
            }
            Label(x,430,340,75,"One course per Hall or Library. Remove its course before reclaiming that building.",muted);
            for(int i=0;i<6;i++)if(school.Exists(i)&&Button(x+375,133+i*48,355,(i==course?"● ":"")+school.courses[i].title))course=i;
            float rx=x+765;
            if(school.Exists(course))
            {
                var c=school.courses[course];bool owner=c.owner==game.state.active;
                Label(rx,130,355,70,c.title,text);
                Label(rx,194,355,88,$"Owner: Keeper {c.owner+1} · Plot {c.plot+1}\nSeats {c.Seats}/12 · Simulated students {c.students}\nClass sessions: {c.sessions}",muted);
                bool enrolled=(c.enrolled&(1<<game.state.active))!=0;
                if(Button(rx,280,355,enrolled?"Enrolled as Keeper "+(game.state.active+1):"Enroll this Keeper"))
                {if(school.Enroll(course,game.state.active))Commit("You are enrolled. Read the lesson and answer below.");else feedback=enrolled?"You are already enrolled.":"This class is full.";}
                GUI.enabled=owner;
                if(Button(rx,325,170,"+ Student")){if(school.AssignStudents(course,game.state.active,1))Commit("Simulated student assigned.");else feedback="The classroom is full.";}
                if(Button(rx+185,325,170,"− Student")){if(school.AssignStudents(course,game.state.active,-1))Commit("Student removed.");}
                if(Button(rx,370,355,"Run class / travel to classroom"))
                {if(school.Teach(course,game.state.active)){Commit("Class is in session.");Travel(1);}else feedback="Enroll someone or add a student first.";}
                if(Button(rx,415,355,"Remove course")){school.Remove(course,game.state.active);Commit("Course removed. Its building can now be reclaimed.");}
                GUI.enabled=true;
                Label(x,515,1100,65,CampusLessons.Text[c.subject],muted);
                Label(x,590,1100,32,CampusLessons.Questions[c.subject],text);
                GUI.enabled=enrolled;
                for(int i=0;i<3;i++)if(Button(x+i*375,632,360,CampusLessons.Answers[c.subject][i]))
                {
                    if((c.graduates&(1<<game.state.active))!=0)feedback="You already completed this lesson.";
                    else if(school.Answer(course,game.state.active,i))Commit("Correct! Lesson completed and saved for this Keeper.");
                    else feedback="Try again. Read the lesson above for the answer.";
                }
                GUI.enabled=true;
                Label(x,685,1100,28,(c.graduates&(1<<game.state.active))!=0?"✓ LESSON COMPLETED":enrolled?"Read, then choose an answer.":"Enroll to answer this lesson.",muted);
            }
            else Label(x+765,150,340,160,"Build a Hall or Library, create a course and give students a place to learn. The Common Classroom hosts your active class.",text);
            Label(x,735,1110,55,feedback,muted);
        }
    }
}
