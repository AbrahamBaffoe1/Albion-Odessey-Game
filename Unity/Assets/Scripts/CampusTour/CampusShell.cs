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
        public bool OwnsPanel=>game.life.panel=="launch"||game.life.panel=="sessionend";
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
            if(Automated||allowQuit||game==null)return true;
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
                    int count=IsSummary?3:9;
                    if(cancel){if(IsSummary)ShowLaunch();else Play();return true;}
                    if(horizontal!=0||vertical!=0)
                    {
                        int direction=horizontal!=0?(horizontal>0?1:-1):(vertical>0?-1:1);
                        menuFocus=(menuFocus+direction+count)%count;
                    }
                    if(choose)
                    {
                        if(IsSummary)
                        {
                            if(menuFocus==0)Play();else if(menuFocus==1)ShowLaunch();else if(SaveSucceeded)QuitSaved();else EndSession(chapterEnd);
                        }
                        else
                        {
                            if(menuFocus==0)Play();else if(menuFocus==1)Videos();else if(menuFocus==2)Stories();else if(menuFocus==3)BuildYourOwn();else if(menuFocus==4)Courses();else if(menuFocus==5)game.life.SetPanel("settings");else if(menuFocus==6)game.life.SetPanel("welcome");else if(menuFocus==7)EndSession();else if(CampusBuildings.Instance!=null)CampusBuildings.Instance.Visit();
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
            if(!game.life.PanelOpen&&!game.journalOpen&&Input.GetKeyDown(KeyCode.Escape)){ShowLaunch();return true;}
            return false;
        }
        void Update()
        {
            if(game==null)return;
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
            var bg=GUI.backgroundColor;var fg=GUI.contentColor;
            GUI.backgroundColor=primary?Gold:Purple;GUI.contentColor=primary?Ink:Cream;
            // State text colors are explicit so the primary action remains dark on gold.
            foreach(var s in new[]{button.normal,button.hover,button.active,button.focused})s.textColor=primary?Ink:Cream;
            bool clicked=GUI.Button(r,label,button);GUI.backgroundColor=bg;GUI.contentColor=fg;return clicked;
        }
        string FocusLabel(int index,string label)=>menuFocus==index?"▶ "+label:label;
        void OnGUI()
        {
            if(game==null)return;Styles();var oldMatrix=GUI.matrix;var oldColor=GUI.color;var oldContent=GUI.contentColor;var oldBackground=GUI.backgroundColor;int oldDepth=GUI.depth;
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
            else
            {
                GUI.matrix=AlbionUITheme.Slide(GUI.matrix,menuOpenedAt,OdysseyAccessibility.ReducedMotion);
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
            Label(new Rect(46,111,right-80,28),"A CAMPUS FULL OF STORIES",small,Gold);
            Label(new Rect(40,155,right-72,155),"Make yourself\nat Albion.",display,Cream);
            Label(new Rect(46,321,right-94,75),"Explore the college. Watch its stories.\nBuild a place of your own.",text,Muted);
            string play=seconds>1?"Continue exploring   →":"Enter campus   →";
            if(Button(new Rect(46,428,right-94,60),FocusLabel(0,play),true))Play();
            float bw=(right-106)/2;
            if(Button(new Rect(46,502,bw,55),FocusLabel(1,"College videos")))Videos();
            if(Button(new Rect(58+bw,502,bw,55),FocusLabel(2,"Building stories")))Stories();
            if(Button(new Rect(46,569,bw,48),FocusLabel(3,"Build your own · B")))BuildYourOwn();
            if(Button(new Rect(58+bw,569,bw,48),FocusLabel(4,"Courses & classes")))Courses();
            if(Button(new Rect(46,629,bw,42),FocusLabel(5,"Your character")))game.life.SetPanel("settings");
            if(Button(new Rect(58+bw,629,bw,42),FocusLabel(6,"Controls & sound")))game.life.SetPanel("welcome");
            if(Button(new Rect(46,683,right-94,36),FocusLabel(7,"Finish session")))EndSession();
            string controls=game.xr!=null&&game.xr.Active?"VR  Point at a button    ·    Trigger select    ·    Menu back":AlbionUIInput.ControllerPresent?"GAMEPAD  Left stick navigate    ·    A / Cross select    ·    B / Circle back":"G  Stories & media    ·    B  Building studio    ·    Esc  Menu\nWASD / arrows  Move    ·    O  On-screen controls";
            Label(new Rect(46,h-74,right-90,46),controls,small,Muted);
            Fill(new Rect(right+28,h-250,w-right-58,212),new Color(.025f,.018f,.042f,.91f));
            Label(new Rect(right+50,h-227,w-right-98,26),"STEP INSIDE / FERGUSON HALL",small,Gold);
            Label(new Rect(right+50,h-190,w-right-100,63),"Explore beyond the doors.",heading,Cream);
            Label(new Rect(right+50,h-139,w-right-100,44),"Three levels, openable doors and rooms to explore.",small,Muted);
            if(Button(new Rect(right+50,h-82,w-right-100,36),FocusLabel(8,"Explore Ferguson Hall   →"))){CampusBuildings.Instance.Visit();}
        }
        void Stat(float x,float y,float width,string value,string caption)
        {Fill(new Rect(x,y,width,93),new Color(.063f,.040f,.09f));Label(new Rect(x+14,y+10,width-28,42),value,heading,Gold);Label(new Rect(x+14,y+57,width-24,30),caption,small,Muted);}
        void DrawSummary(float w,float h,float right)
        {
            Label(new Rect(46,111,right-80,26),chapterEnd?"FIRST CHAPTER COMPLETE":"SESSION WRAP-UP",small,Gold);
            Label(new Rect(40,154,right-75,161),chapterEnd?"The Beacon\nis alight.":"Your story\ncontinues.",display,Cream);
            Label(new Rect(46,329,right-94,58),chapterEnd?"You completed the shared Beacon. Keep exploring and building your campus.":"Every visit adds a little more to your campus story. Come back whenever you’re ready.",text,Muted);
            float sw=(right-108)/2;
            Stat(46,417,sw,game.tour.StoriesRead.ToString(),"building stories opened");
            Stat(60+sw,417,sw,game.tour.VideosStarted.ToString(),"college videos started");
            Stat(46,524,sw,Math.Max(0,MemoryCount()-initialMemories).ToString(),"new memories collected");
            Stat(60+sw,524,sw,TimeSpan.FromSeconds(seconds).ToString(@"hh\:mm\:ss"),"time exploring");
            Label(new Rect(46,640,right-90,48),SaveMessage,small,SaveSucceeded?new Color(.43f,.81f,.64f):Gold);
            Fill(new Rect(right+30,h-363,w-right-62,319),new Color(.025f,.018f,.042f,.94f));
            Label(new Rect(right+53,h-340,w-right-102,32),"WHERE NEXT?",heading,Cream);
            Label(new Rect(right+53,h-291,w-right-106,45),Math.Max(0,BuildingCount()-initialBuildings)+" more buildings on your campus this session.",small,Muted);
            if(Button(new Rect(right+53,h-237,w-right-108,51),FocusLabel(0,"Keep exploring   →"),true))Play();
            if(Button(new Rect(right+53,h-171,w-right-108,45),FocusLabel(1,"Return to launch screen")))ShowLaunch();
            if(Button(new Rect(right+53,h-111,w-right-108,45),FocusLabel(2,SaveSucceeded?"Quit to desktop":"Retry saving"))){if(SaveSucceeded)QuitSaved();else EndSession(chapterEnd);}
        }
    }
}
