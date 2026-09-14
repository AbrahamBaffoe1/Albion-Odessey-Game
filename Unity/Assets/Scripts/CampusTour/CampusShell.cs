using System;
using System.Linq;
using UnityEngine;
using AlbionOdyssey.BuildingDesigner;
namespace AlbionOdyssey
{
    public sealed class CampusShell : MonoBehaviour
    {
        OdysseyGame game; Texture2D hero; GUIStyle brand,display,heading,text,small,button;
        float seconds,menuOpenedAt; int initialMemories,initialBuildings; bool[] chapterSeen; bool pendingChapter,chapterEnd,allowQuit; int menuFocus;
        public bool SaveSucceeded {get;private set;}
        public string SaveMessage {get;private set;}="";
        public bool OwnsPanel=>game.life.panel=="launch"||game.life.panel=="sessionend"||IsPaused;
        public bool IsPaused=>game.life.panel=="pause";
        public bool IsSummary=>game.life.panel=="sessionend";
        public static bool Automated=>PlaytestMode.Active;
        static readonly Color Ink=new Color(.018f,.014f,.028f),Purple=new Color(.15f,.068f,.26f),Gold=new Color(.96f,.64f,.14f),Cream=new Color(.98f,.95f,.87f),Muted=new Color(.65f,.61f,.72f);
        public void Setup(OdysseyGame owner)
        {
            game=owner;hero=Resources.Load<Texture2D>("CampusCraft/FergusonHero");
            initialMemories=MemoryCount();initialBuildings=BuildingCount();chapterSeen=game.state.keepers.Select(HasCompletedChapter).ToArray();
            Application.wantsToQuit+=OnQuitRequested;
            if(!Automated)ShowLaunch();
        }
        public static bool HasCompletedChapter(Keeper keeper)=>keeper.milestones==63;
        int MemoryCount()=>game.state.keepers.Sum(k=>OdysseyState.Count(k.memories));
        int BuildingCount()=>game.state.keepers.Sum(k=>k.plots.Count(p=>p!=0));
        public void ShowLaunch(){menuFocus=0;menuOpenedAt=Time.unscaledTime;game.tour.StopMedia();game.life.SetPanel("launch");}
        public void OpenPause(){menuFocus=0;menuOpenedAt=Time.unscaledTime;game.tour.StopMedia();game.life.SetPanel("pause");}
        public void Play(){game.life.SetPanel("");game.notice="G opens building stories. Esc opens the menu. O shows movement buttons and frees the pointer.";}
        public void Stories(){game.tour.OpenDirectory();}
        public void Videos(){game.tour.OpenVideos();}
        public bool BuildYourOwn()
        {
            var studio=FindAnyObjectByType<BlueprintStudio>();
            if(studio==null||!studio.Ready){game.notice="The building studio is preparing. Try again in a moment.";return false;}
            return studio.Enter();
        }
        public void Courses(){game.life.SetPanel("courses");}
        public void PlaceCampusBuildings()
        {
            var studio=FindAnyObjectByType<BlueprintStudio>();if(studio!=null&&studio.Active){studio.Leave();if(studio.Active)return;}
            game.tour.StopMedia();game.life.SetPanel("");if(!game.building)game.ToggleMode();
        }
        public void EndSession(bool completed=false)
        {
            var studio=FindAnyObjectByType<BlueprintStudio>();
            if(studio!=null&&studio.Active){studio.Leave();if(studio.Active){SaveSucceeded=false;SaveMessage="Save your building before leaving the studio.";return;}}
            chapterEnd=completed;pendingChapter=false;game.tour.StopMedia();
            SaveSucceeded=game.Save();
            if(SaveSucceeded)
            {
                try{game.campus.SaveKeeper();SaveMessage="PROGRESS SAVED ON THIS MAC";}
                catch(Exception e){SaveSucceeded=false;Debug.LogWarning("Campus profile save failed: "+e.Message);}
            }
            if(!SaveSucceeded)SaveMessage="We could not save. Retry below, or keep playing.";
            menuOpenedAt=Time.unscaledTime;game.life.SetPanel("sessionend");
        }
        public void QuitSaved(){if(!SaveSucceeded){EndSession(chapterEnd);return;}allowQuit=true;Application.Quit();}
        bool OnQuitRequested()
        {
            if(Automated||allowQuit||game==null||!game.Ready)return true;
            if(IsSummary&&SaveSucceeded)return true;
            EndSession();return false;
        }
        void OnDestroy(){Application.wantsToQuit-=OnQuitRequested;}
        public bool HandleInput()
        {
            if(OwnsPanel)
            {
                if(AlbionUIInput.Poll(out var horizontal,out var vertical,out var choose,out var cancel))
                {
                    int count=IsSummary?3:IsPaused?7:10;
                    if(cancel){if(IsSummary)ShowLaunch();else Play();return true;}
                    if(horizontal!=0||vertical!=0)
                    {
                        int direction=horizontal!=0?(horizontal>0?1:-1):(vertical>0?-1:1);
                        menuFocus=(menuFocus+direction+count)%count;
                    }
                    if(choose)
                    {
                        if(IsPaused)ActivatePause(menuFocus);
                        else if(IsSummary)
                        {
                            if(menuFocus==0)Play();else if(menuFocus==1)ShowLaunch();else if(SaveSucceeded)QuitSaved();else EndSession(chapterEnd);
                        }
                        else
                        {
                            if(menuFocus==9){game.accountPanel.Open();return true;}
                            if(menuFocus==0)Play();else if(menuFocus==1)Videos();else if(menuFocus==2)Stories();else if(menuFocus==3)OpenStudioWithLoading();else if(menuFocus==4)Courses();else if(menuFocus==5)OpenStudentWithLoading();else if(menuFocus==6)game.life.SetPanel("welcome");else if(menuFocus==7)EndSession();else if(CampusBuildings.Instance!=null)CampusBuildings.Instance.Visit();
                        }
                    }
                    return true;
                }
                if(Input.GetKeyDown(KeyCode.B)){BuildYourOwn();return true;}
                if(Input.GetKeyDown(KeyCode.G)){Stories();return true;}
                if(Input.GetKeyDown(KeyCode.Return)||Input.GetKeyDown(KeyCode.KeypadEnter)||Input.GetKeyDown(KeyCode.Escape)){Play();return true;}
                return true;
            }
            if(!game.life.PanelOpen&&!game.journalOpen&&!game.building&&Input.GetKeyDown(KeyCode.B)){BuildYourOwn();return true;}
            if(game.life.panel=="welcome"&&Input.GetKeyDown(KeyCode.G)){Stories();return true;}
            if(!game.life.PanelOpen&&!game.journalOpen&&Input.GetKeyDown(KeyCode.Escape)){OpenPause();return true;}
            return false;
        }
        void Update()
        {
            if(game==null||!game.Ready)return;
            if(!game.life.PanelOpen&&Application.isFocused)seconds+=Time.unscaledDeltaTime;
            int active=game.state.active;
            if(!chapterSeen[active]&&HasCompletedChapter(game.state.Current)){chapterSeen[active]=true;pendingChapter=true;}
            if(pendingChapter&&!game.life.PanelOpen&&!game.building&&!game.journalOpen&&!Automated)EndSession(true);
        }
        void Styles()
        {
            if(brand!=null)return;
            brand=new GUIStyle(GUI.skin.label){font=AlbionUITheme.DisplayFont,fontSize=AlbionUITheme.TextSize(19),fontStyle=FontStyle.Bold,richText=false};
            display=new GUIStyle(brand){font=AlbionUITheme.DisplayFont,fontSize=AlbionUITheme.TextSize(62),wordWrap=true};heading=new GUIStyle(brand){font=AlbionUITheme.DisplayFont,fontSize=AlbionUITheme.TextSize(29),wordWrap=true};
            text=new GUIStyle(GUI.skin.label){font=AlbionUITheme.BodyFont,fontSize=AlbionUITheme.TextSize(19),wordWrap=true,richText=false};small=new GUIStyle(text){font=AlbionUITheme.BodyFont,fontSize=AlbionUITheme.TextSize(14)};
            button=new GUIStyle(text){font=AlbionUITheme.BodyFont,fontSize=AlbionUITheme.TextSize(18),fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleLeft,padding=new RectOffset(22,16,9,9),border=new RectOffset()};
            foreach(var s in new[]{button.normal,button.hover,button.active,button.focused}){s.background=Texture2D.whiteTexture;s.textColor=Cream;}
        }
        void Fill(Rect r,Color color){var c=GUI.color;GUI.color=color;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=c;}
        void Label(Rect r,string value,GUIStyle style,Color color){var c=GUI.contentColor;GUI.contentColor=color;GUI.Label(r,value,style);GUI.contentColor=c;}
        bool Button(Rect r,string label,bool primary=false)
        {
            return OdysseyUI.Button(r,label.TrimStart('▶',' '),"shell-"+r.x+"-"+r.y,label.StartsWith("▶"),primary);
        }
        string FocusLabel(int index,string label)=>menuFocus==index?"▶ "+label:label;
        void OnGUI()
        {
            if(game==null||!game.Ready)return;Styles();var oldMatrix=GUI.matrix;var oldColor=GUI.color;var oldContent=GUI.contentColor;var oldBackground=GUI.backgroundColor;int oldDepth=GUI.depth;
            float scale=Mathf.Min(Screen.width/1280f,Screen.height/800f);GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));GUI.color=GUI.contentColor=Color.white;GUI.backgroundColor=Color.white;GUI.depth=-20;
            float w=Screen.width/scale,h=Screen.height/scale;
            if(!OwnsPanel)
            {
                if(!game.life.PanelOpen&&!game.journalOpen&&game.building)
                {
                    if(Button(new Rect(w-230,26,205,38),"Explore on foot"))game.ToggleMode();
                    if(Button(new Rect(w-230,70,205,38),"Custom room studio"))BuildYourOwn();
                }
                if(!game.life.PanelOpen&&!game.journalOpen&&!game.building)
                {
                    // Available with the free pointer (O); Esc always opens these same actions as a menu.


                }
            }
            else if(IsPaused)DrawPause(w,h);
            else
            {

                Fill(new Rect(0,0,w,h),Ink);
                float right=w*.52f;
                if(hero!=null)GUI.DrawTexture(new Rect(right,0,w-right,h),hero,ScaleMode.ScaleAndCrop);
                Fill(new Rect(right,0,w-right,h),new Color(.075f,.025f,.12f,.48f));
                Fill(new Rect(right,0,5,h),Gold);
                Label(new Rect(44,32,580,28),"ALBION  /  ODYSSEY",brand,Cream);
                Label(new Rect(w-310,35,268,28),"ONE CAMPUS  ·  YOUR ODYSSEY",small,Cream);
                if(IsSummary)DrawSummary(w,h,right);else DrawLaunch(w,h,right);
            }
            GUI.matrix=oldMatrix;GUI.color=oldColor;GUI.contentColor=oldContent;GUI.backgroundColor=oldBackground;GUI.depth=oldDepth;
        }
        void DrawLaunch(float w,float h,float right)
        {
            game.presentation?.DrawBackdrop(w,h);
            float x=(w-1160)*.5f;
            OdysseyUI.Text(new Rect(x,37,250,38),"ALBION / ODYSSEY",21,OdysseyUI.White,true);
            if(OdysseyUI.Button(new Rect(x+290,27,160,57),"Explore","nav-explore",menuFocus==2))Stories();
            if(OdysseyUI.Button(new Rect(x+464,27,140,57),"Build","nav-build",menuFocus==3))OpenStudioWithLoading();
            if(OdysseyUI.Button(new Rect(x+618,27,140,57),"Learn","nav-learn",menuFocus==4))Courses();
            if(OdysseyUI.Button(new Rect(x+772,27,174,57),"Your student","nav-student",menuFocus==5))OpenStudentWithLoading();
            if(OdysseyUI.Button(new Rect(x+960,27,200,57),game.accounts.SignedIn?"My account":"Sign in / join","nav-account",menuFocus==9))game.accountPanel.Open();
            OdysseyUI.Card(new Rect(x,157,240,30),new Color(.14f,.34f,.29f));
            OdysseyUI.Text(new Rect(x+15,162,230,25),"A NEW DAY ON CAMPUS",13,OdysseyUI.Mint,true);
            OdysseyUI.Text(new Rect(x-3,222,600,180),"YOUR CAMPUS.\nYOUR STORY.",58,OdysseyUI.White,true);
            OdysseyUI.Text(new Rect(x,419,465,75),"Find your people. Discover the college.\nLeave your mark.",22,OdysseyUI.White);
            if(OdysseyUI.Button(new Rect(x,523,320,76),seconds>1?"CONTINUE EXPLORING  →":"ENTER CAMPUS  →","lobby-play",menuFocus==0,true))Play();
            if(OdysseyUI.Button(new Rect(x+338,535,158,56),"Controls","lobby-help",menuFocus==6))game.life.SetPanel("welcome");
            game.presentation?.DrawStudent(new Rect(x+620,111,560,525));
            OdysseyUI.Card(new Rect(x+710,600,365,43),new Color(.015f,.04f,.075f,.94f));
            OdysseyUI.Text(new Rect(x+735,610,320,28),game.campus.keeperName+"  ·  READY TO EXPLORE",15,OdysseyUI.White,true);
            float y=h-136;
            if(OdysseyUI.Button(new Rect(x,y,370,86),"01   EXPLORE FERGUSON HALL","tile-hall",menuFocus==8))CampusBuildings.Instance.Visit();
            if(OdysseyUI.Button(new Rect(x+395,y,370,86),"02   CAMPUS FILMS & STORIES","tile-film",menuFocus==1))Videos();
            if(OdysseyUI.Button(new Rect(x+790,y,370,86),"03   SAVE & FINISH SESSION","tile-exit",menuFocus==7))EndSession();
            OdysseyUI.Text(new Rect(x,h-32,1120,26),"WASD / ARROWS  Move    ·    E / F  Interact    ·    ESC  Menu                         ALBION COLLEGE, MICHIGAN",13,OdysseyUI.Muted);
        }
        void OpenStudioWithLoading(){game.loading.Transition("Preparing your building studio",()=>BuildYourOwn());}
        void OpenStudentWithLoading(){game.loading.Transition("Preparing your student",()=>game.life.SetPanel("settings"));}
        public void ActivatePause(int action)
        {
            if(!IsPaused)return;
            switch(action)
            {
                case 0: Play(); break;
                case 1: game.accountPanel.Open(); break;
                case 2: Stories(); break;
                case 3: Courses(); break;
                case 4: game.vr.OpenPanel(); break;
                case 5: ShowLaunch(); break;
                case 6: EndSession(); break;
            }
        }
        void DrawPause(float w,float h)
        {
            game.presentation?.DrawBackdrop(w,h);
            OdysseyUI.Fill(new Rect(0,0,w,h),new Color(.012f,.022f,.048f,.82f));
            float x=(w-1120)*.5f;
            OdysseyUI.Text(new Rect(x,62,700,35),"ALBION / ODYSSEY",22,OdysseyUI.Muted,true);
            OdysseyUI.Text(new Rect(x,121,650,86),"CAMPUS MENU",48,OdysseyUI.White,true);
            string[] labels={"Resume exploring","Student account","Building stories","Courses & classes","VR & comfort","Main menu","Save & finish session"};
            for(int i=0;i<labels.Length;i++)if(OdysseyUI.Button(new Rect(x,239+i*63,565,53),labels[i],"pause-"+i,menuFocus==i,i==0)){menuFocus=i;ActivatePause(i);}
            game.presentation?.DrawStudent(new Rect(x+650,117,470,488));
            OdysseyUI.Text(new Rect(x+650,627,470,40),game.campus.keeperName,27,OdysseyUI.White,true);
            OdysseyUI.Text(new Rect(x+650,677,470,44),game.life.Location,16,OdysseyUI.Muted);
            OdysseyUI.Text(new Rect(x,h-40,1120,30),"ESC  Resume     ·     ↑ ↓  Navigate     ·     ENTER  Select",14,OdysseyUI.Muted);
        }
        void Stat(float x,float y,float width,string value,string caption)
        {Fill(new Rect(x,y,width,93),new Color(.063f,.040f,.09f));Label(new Rect(x+14,y+10,width-28,42),value,heading,Gold);Label(new Rect(x+14,y+57,width-24,30),caption,small,Muted);}
        void DrawSummary(float w,float h,float right)
        {
            game.presentation?.DrawBackdrop(w,h);
            OdysseyUI.Fill(new Rect(0,0,w,h),new Color(.012f,.022f,.048f,.8f));
            float x=(w-1120)*.5f;
            OdysseyUI.Text(new Rect(x,65,900,32),"ALBION / ODYSSEY",22,OdysseyUI.Muted,true);
            OdysseyUI.Text(new Rect(x,144,1050,95),chapterEnd?"THE BEACON IS ALIGHT.":"YOUR STORY CONTINUES.",48,OdysseyUI.White,true);
            OdysseyUI.Text(new Rect(x,256,970,65),chapterEnd?"You completed the shared Beacon. Keep exploring and building your campus.":"Your discoveries are part of the story. Come back whenever you’re ready.",22,OdysseyUI.Muted);
            string[] values={game.tour.StoriesRead.ToString(),Math.Max(0,MemoryCount()-initialMemories).ToString(),Math.Max(0,BuildingCount()-initialBuildings).ToString(),TimeSpan.FromSeconds(seconds).ToString(@"hh\:mm\:ss")};
            string[] captions={"Stories explored","Memories collected","Buildings added","Time on campus"};
            for(int i=0;i<4;i++){float sx=x+i*286;OdysseyUI.Card(new Rect(sx,374,262,137),OdysseyUI.Surface);OdysseyUI.Text(new Rect(sx+20,393,222,54),values[i],34,OdysseyUI.White,true);OdysseyUI.Text(new Rect(sx+20,463,222,30),captions[i],16,OdysseyUI.Muted);}
            OdysseyUI.Text(new Rect(x,552,1080,60),SaveMessage,18,SaveSucceeded?OdysseyUI.Mint:OdysseyUI.Gold,true);
            if(OdysseyUI.Button(new Rect(x,h-143,356,62),"Keep exploring","summary-play",menuFocus==0,true))Play();
            if(OdysseyUI.Button(new Rect(x+382,h-143,356,62),"Main menu","summary-menu",menuFocus==1))ShowLaunch();
            if(OdysseyUI.Button(new Rect(x+764,h-143,356,62),SaveSucceeded?"Quit to desktop":"Retry saving","summary-quit",menuFocus==2)){if(SaveSucceeded)QuitSaved();else EndSession(chapterEnd);}
            OdysseyUI.Text(new Rect(x,h-45,1080,26),"↑ ↓  Navigate    ·    ENTER  Select",14,OdysseyUI.Muted);
        }
    }
}
