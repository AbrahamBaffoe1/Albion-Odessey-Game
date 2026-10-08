using System;
using System.Linq;
using UnityEngine;
using AlbionOdyssey.BuildingDesigner;
namespace AlbionOdyssey
{
    public sealed class CampusShell : MonoBehaviour
    {
        OdysseyGame game; Texture2D hero,keyArt; bool hub; GUIStyle brand,display,heading,text,small,button;
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
            game=owner;keyArt=Resources.Load<Texture2D>("Presentation/OdysseyTitleArt");hero=Resources.Load<Texture2D>("CampusCraft/FergusonHero");
            initialMemories=MemoryCount();initialBuildings=BuildingCount();chapterSeen=game.state.keepers.Select(HasCompletedChapter).ToArray();
            Application.wantsToQuit+=OnQuitRequested;
            if(!Automated)ShowLaunch();
        }
        public static bool HasCompletedChapter(Keeper keeper)=>keeper.milestones==63;
        int MemoryCount()=>game.state.keepers.Sum(k=>OdysseyState.Count(k.memories));
        int BuildingCount()=>game.state.keepers.Sum(k=>k.plots.Count(p=>p!=0));
        public void ShowLaunch(){hub=false;menuFocus=0;menuOpenedAt=Time.unscaledTime;game.tour.StopMedia();game.life.SetPanel("launch");}
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
                if(!IsSummary&&Input.GetKeyDown(KeyCode.D)){Play();game.city?.Travel();return true;}
                if(!IsSummary&&Input.GetKeyDown(KeyCode.M)){game.life.SetPanel("campus");return true;}
                if(AlbionUIInput.Poll(out var horizontal,out var vertical,out var choose,out var cancel))
                {
                    int[] navigation=IsSummary?new[]{0,1,2}:IsPaused?new[]{0,7,8,9,10,1,2,3,4,5,6}:!hub?new[]{0,13,14,10,6,7}:new[]{0,10,11,12,14,5,9,3,4,2,1,6,7,8};
                    int count=navigation.Length;
                    if(cancel){if(IsSummary||hub){ShowLaunch();}else Play();return true;}
                    if(horizontal!=0||vertical!=0)
                    {
                        int direction=horizontal!=0?(horizontal>0?1:-1):(vertical>0?-1:1);
                        int current=Array.IndexOf(navigation,menuFocus);menuFocus=navigation[(Math.Max(0,current)+direction+count)%count];
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
                            if(menuFocus==13){hub=true;menuFocus=0;return true;}
                            if(menuFocus==14){game.store.Open();return true;}
                            if(menuFocus==10){game.shared.Open();return true;}
                            if(menuFocus==11){game.shared.OpenForest();return true;}
                            if(menuFocus==12){FindAnyObjectByType<WhitehouseTrailWorld>()?.ToggleVisit();return true;}
                            if(menuFocus==9){game.accountPanel.Open();return true;}
                            if(menuFocus==0)Play();else if(menuFocus==1)Videos();else if(menuFocus==2)Stories();else if(menuFocus==3)OpenStudioWithLoading();else if(menuFocus==4)Courses();else if(menuFocus==5)OpenStudentWithLoading();else if(menuFocus==6)game.life.SetPanel("welcome");else if(menuFocus==7)EndSession();else {Play();game.city?.Travel();}
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
            if(game==null||!game.Ready)return;bool enabled=GUI.enabled;GUI.enabled=enabled&&!game.loading.Busy;Styles();var oldMatrix=GUI.matrix;var oldColor=GUI.color;var oldContent=GUI.contentColor;var oldBackground=GUI.backgroundColor;int oldDepth=GUI.depth;
            float scale=Mathf.Min(Screen.width/1280f,Screen.height/800f);GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));GUI.color=GUI.contentColor=Color.white;GUI.backgroundColor=Color.white;GUI.depth=-20;
            float w=Screen.width/scale,h=Screen.height/scale;
            if(OwnsPanel)OdysseyCinematic.ConsumeMenuKeys();
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
            else if(game.life.panel=="launch"&&!hub)DrawTitle(w,h);
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
            GUI.matrix=oldMatrix;GUI.color=oldColor;GUI.contentColor=oldContent;GUI.backgroundColor=oldBackground;GUI.depth=oldDepth;GUI.enabled=enabled;
        }
        void DrawTitle(float w,float h)
        {
            OdysseyUI.Fill(new Rect(0,0,w,h),new Color(.008f,.012f,.028f));
            if(keyArt!=null){var previous=GUI.color;GUI.color=Color.white;GUI.DrawTexture(new Rect(0,0,w,h),keyArt,ScaleMode.ScaleAndCrop);GUI.color=previous;}
            OdysseyUI.Fill(new Rect(0,0,w,h),new Color(.009f,.014f,.028f,.3f));
            OdysseyCinematic.Atmosphere(w,h);
            float center=w/2;
            OdysseyCinematic.Title(new Rect(center-490,h*.14f,980,120),"ALBION",99,new Color(.91f,.91f,.87f));
            OdysseyCinematic.Title(new Rect(center-490,h*.14f+112,980,80),"O D Y S S E Y",44,new Color(.8f,.81f,.81f));
            OdysseyUI.Fill(new Rect(center-320,h*.14f+203,640,1),new Color(.67f,.57f,.43f,.7f));
            OdysseyCinematic.Title(new Rect(center-350,h*.14f+220,700,32),"Every path becomes a story",17,new Color(.62f,.66f,.7f));
            int[] actions={0,13,14,10,6,7};string[] labels={"Continue","Explore Albion","Store","Play Online","Controls & Help","Save & Quit"};
            for(int i=0;i<actions.Length;i++)
            {
                Rect r=new Rect(center-180,h*.55f+i*39,360,36);bool selected=menuFocus==actions[i]||r.Contains(Event.current.mousePosition);
                if(selected)
                {
                    for(int n=7;n>=1;n--)OdysseyUI.Fill(new Rect(center-390,r.center.y-n,780,n*2),new Color(.85f,.21f,.075f,.018f));
                    OdysseyUI.Fill(new Rect(center-260,r.yMax,520,1),new Color(.96f,.36f,.13f,.7f));
                }
                OdysseyCinematic.Title(r,labels[i],20,selected?OdysseyUI.White:new Color(.57f,.57f,.59f));
                if(GUI.Button(r,GUIContent.none,GUIStyle.none))
                {
                    GUIUtility.keyboardControl=0;menuFocus=actions[i];game.presentation?.Click();
                    switch(actions[i]){case 0:Play();break;case 13:hub=true;menuFocus=0;break;case 14:game.store.Open();break;case 10:game.shared.Open();break;case 6:game.life.SetPanel("welcome");break;case 7:EndSession();break;}
                }
            }
            OdysseyUI.Text(new Rect(42,h-47,600,27),"ALBION ODYSSEY  /  AN ORIGINAL CAMPUS ADVENTURE",12,OdysseyUI.Muted);
            OdysseyUI.Text(new Rect(w-260,h-47,220,27),"ENTER  SELECT     /     v"+Application.version,12,OdysseyUI.Muted);
        }
        void DrawLaunch(float w,float h,float right)
        {
            float x=(w-1160)*.5f;ConsoleMenuStyle.Background(game,w,h);
            OdysseyUI.Text(new Rect(x,32,500,64),"ALBION ODYSSEY",48,OdysseyUI.White);
            OdysseyUI.Text(new Rect(x+2,99,600,24),"EXPLORE  /  CONNECT  /  DISCOVER",14,OdysseyUI.Gold);
            OdysseyUI.Text(new Rect(w-360,42,280,30),game.accounts.SignedIn?"STUDENT PROFILE CONNECTED":"GUEST EXPLORER",18,OdysseyUI.White);
            OdysseyUI.Text(new Rect(w-360,78,280,26),"ALBION, MICHIGAN  /  v"+Application.version,14,OdysseyUI.Gold);
            OdysseyUI.Fill(new Rect(x,149,1160,1),OdysseyUI.Gold);
            int[] actions={0,10,11,12,14,5,9,3,4,2,1,6,7};
            string[] labels={"Enter campus","Online rooms","Forest pursuit","Nature trails","Store","Your student","Account / sign in","Build your own","Courses & classes","Campus stories","College films","Controls","Save & finish"};
            for(int i=0;i<actions.Length;i++)if(OdysseyUI.Button(new Rect(x,176+i*37,330,33),labels[i],"command-"+i,menuFocus==actions[i]))
            {
                menuFocus=actions[i];switch(actions[i]){case 14:game.store.Open();break;case 0:Play();break;case 10:game.shared.Open();break;case 11:game.shared.OpenForest();break;case 12:FindAnyObjectByType<WhitehouseTrailWorld>()?.ToggleVisit();break;case 5:OpenStudentWithLoading();break;case 9:game.accountPanel.Open();break;case 3:OpenStudioWithLoading();break;case 4:Courses();break;case 2:Stories();break;case 1:Videos();break;case 6:game.life.SetPanel("welcome");break;case 7:EndSession();break;}
            }
            float rx=x+375;OdysseyUI.Card(new Rect(rx,181,785,350),new Color(.02f,.025f,.025f,.7f));
            if(game.presentation?.CampusView!=null)GUI.DrawTexture(new Rect(rx+1,182,783,348),game.presentation.CampusView,ScaleMode.ScaleAndCrop);
            OdysseyUI.Fill(new Rect(rx,441,785,90),new Color(.015f,.017f,.016f,.94f));
            OdysseyUI.Text(new Rect(rx+20,448,700,46),"YOUR NEXT DISCOVERY STARTS HERE",32,OdysseyUI.White);
            OdysseyUI.Text(new Rect(rx+20,495,700,24),"FERGUSON HALL  /  CAMPUS EXPLORATION",14,OdysseyUI.Gold);
            if(OdysseyUI.Button(new Rect(rx,550,380,50),"Open navigation map","hub-navigation"))game.life.SetPanel("campus");
            if(OdysseyUI.Button(new Rect(rx+397,550,388,50),"Explore downtown Albion","hub-hall",menuFocus==8)){Play();game.city?.Travel();}
            OdysseyUI.Text(new Rect(rx,620,780,44),"Choose a destination. Follow the trails. Build your story together.",19,OdysseyUI.Muted);
            ConsoleMenuStyle.Footer(x,h,"ARROWS  Browse     /     ENTER  Select     /     M  Map     /     D  Downtown     /     ESC  Play           ALBION ODYSSEY  ·  FIELD TERMINAL");
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
                case 7: game.shared.Open(); break;
                case 8: game.shared.OpenForest(); break;
                case 9: FindAnyObjectByType<WhitehouseTrailWorld>()?.ToggleVisit(); break;
                case 10: game.store.Open(); break;
            }
        }
        void DrawPause(float w,float h)
        {
            float x=(w-1120)*.5f;ConsoleMenuStyle.Background(game,w,h);
            ConsoleMenuStyle.Heading(x,"SESSION / PAUSED","Albion Odyssey","Your next move.");
            string[] labels={"Resume","Student account","Building stories","Courses & classes","VR & comfort","Main menu","Save & finish session","Online rooms","Forest pursuit","Nature trails / return","Store"};
            int[] order={0,7,8,9,10,1,2,3,4,5,6};
            for(int i=0;i<order.Length;i++){int action=order[i];if(OdysseyUI.Button(new Rect(x,235+i*39,410,34),labels[action],"pause-"+action,menuFocus==action)){menuFocus=action;ActivatePause(action);}}
            OdysseyUI.Card(new Rect(x+460,253,660,421),new Color(.025f,.029f,.026f,.6f));
            game.presentation?.DrawStudent(new Rect(x+770,260,300,400));
            OdysseyUI.Text(new Rect(x+488,280,310,26),"EXPLORER STATUS",18,OdysseyUI.Gold);
            OdysseyUI.Text(new Rect(x+488,331,285,110),game.accounts.SignedIn?"PROFILE\nCONNECTED":"LOCAL\nEXPLORER",34,OdysseyUI.White);
            OdysseyUI.Text(new Rect(x+488,446,265,60),"CAMPUS / ALBION COLLEGE\nSESSION / PAUSED",16,OdysseyUI.Muted);
            if(OdysseyUI.Button(new Rect(x+485,594,300,48),"Navigation map","pause-map"))game.life.SetPanel("campus");
            ConsoleMenuStyle.Footer(x,h,"ESC  Resume     /     ARROWS  Browse     /     ENTER  Select     /     M  Map");
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
