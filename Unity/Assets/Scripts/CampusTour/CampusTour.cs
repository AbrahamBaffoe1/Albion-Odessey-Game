using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Video;

namespace AlbionOdyssey
{
    public sealed class CampusTour : MonoBehaviour
    {
        public TourCatalog catalog; public TourPlace selected; public bool InRoom {get;private set;}
        public bool HasActiveMedia=>activeMedia!=null;
        public bool IsOpen=>game.life.panel=="tour";
        public string Status {get;private set;}="Choose a building to discover its story.";
        AudioSource storySound; AudioClip storyClip;
        AudioSource narration; AudioClip narrationClip;
        OdysseyGame game; string search=""; Vector2 listScroll,detailScroll;
        readonly HashSet<string> read=new HashSet<string>();
        readonly HashSet<string> videosStarted=new HashSet<string>();
        public int StoriesRead=>read.Count; public int VideosStarted=>videosStarted.Count;
        public bool VideosOnly {get;private set;}
        public int VisibleCount=>filtered==null?0:filtered.Length;
        Texture2D photo; Coroutine loading; UnityWebRequest request; TourMedia activeMedia;
        VideoPlayer video; AudioSource videoAudio; RenderTexture videoTexture,panoramaTexture;
        Camera panoramaCamera; GameObject panoramaSphere,room; Material panoramaMaterial;
        float yaw,pitch,mediaStarted,revealChars,openedAt;int directoryFocus,storyFocus,detailFocus; Vector3 returnPosition; Quaternion returnRotation; bool returnThird,voicePlaying,sidePanel,detailFocusActive;
        string[] narrativeLines=Array.Empty<string>(); int narrativePage;
        GUIStyle title,text,small,button,story,detailTitle,textButton; TourPlace[] filtered;
        public static readonly Vector3 RoomOrigin=new Vector3(1600,0,1600);
        public void Setup(OdysseyGame owner)
        {
            game=owner;storySound=gameObject.AddComponent<AudioSource>();storySound.playOnAwake=false;storySound.spatialBlend=0;storyClip=Resources.Load<AudioClip>("CampusTour/BuildingStory");narration=gameObject.AddComponent<AudioSource>();narration.playOnAwake=false;narration.spatialBlend=0;narration.volume=.9f;catalog=JsonUtility.FromJson<TourCatalog>(Resources.Load<TextAsset>("CampusTour/catalog").text);
            if(!catalog.Valid())throw new InvalidOperationException("Invalid campus tour catalog");
            // Four destinations from the game map do not have main-tour entries.
            var all=new List<TourPlace>(catalog.places);
            foreach(var p in CampusCatalog.Places)if(catalog.ForCampus(p.id)==null)
                all.Add(new TourPlace{id="map-"+p.id,name=p.name,category=p.category,campusIds=new[]{p.id},media=Array.Empty<TourMedia>(),source=CampusCatalog.MapSource,summary="This destination appears on the official campus map. A verified history and media reference have not yet been linked. Its exterior in this preview is approximate."});
            catalog.places=all.ToArray();filtered=catalog.Search("");selected=catalog.ForCampus("50");
            Debug.Log("CAMPUS_TOUR_READY: "+catalog.places.Length+" information records; all 61 map destinations linked.");
        }
        public TourPlace Nearby()
        {
            if(InRoom)return catalog.ForCampus("50");
            CampusPlace best=null;float distance=16;
            foreach(var p in CampusCatalog.Places)
            {
                Vector3 v=game.player.transform.position-p.position;
                float dx=Mathf.Max(0,Mathf.Abs(v.x)-p.width*.5f),dz=Mathf.Max(0,Mathf.Abs(v.z)-p.depth*.5f);
                float d=Mathf.Sqrt(dx*dx+dz*dz);
                if(d<distance&&Mathf.Abs(v.y)<p.height+3){distance=d;best=p;}
            }
            return best==null?null:catalog.ForCampus(best.id);
        }
        public bool HandleInput()
        {
            if(IsOpen)
            {
                if(AlbionUIInput.Poll(out var horizontal,out var vertical,out var choose,out var cancel)){if(cancel){Close();return true;}if(sidePanel){if(vertical!=0||horizontal!=0)storyFocus=(storyFocus+(vertical!=0?(vertical>0?-1:1):(horizontal>0?1:-1))+StoryActionCount())%StoryActionCount();if(choose){if(storyFocus==0)ToggleNarration();else if(storyFocus==1&&TourCatalog.SafeSource(selected.source))Application.OpenURL(selected.source);else if(storyFocus==2){sidePanel=false;Open(selected);}else if(storyFocus==3)EnterSelected();else Close();}}else{if(horizontal!=0||detailFocusActive&&vertical!=0){detailFocusActive=true;int count=activeMedia==null?DetailActionCount():MediaActionCount();detailFocus=(detailFocus+(horizontal!=0?(horizontal>0?1:-1):(vertical>0?-1:1))+count)%count;}else if(vertical!=0&&filtered!=null&&filtered.Length>0){detailFocusActive=false;directoryFocus=(directoryFocus+(vertical>0?-1:1)+filtered.Length)%filtered.Length;selected=filtered[directoryFocus];}if(choose&&selected!=null){if(detailFocusActive){if(activeMedia==null)ActivateDetailFocus();else ActivateMediaFocus();}else Open(selected);}}return true;}
                if(Input.GetKeyDown(KeyCode.Escape))Close();
                else if(sidePanel&&(Input.GetKeyDown(KeyCode.Return)||Input.GetKeyDown(KeyCode.KeypadEnter)||Input.GetKeyDown(KeyCode.Space))){AdvanceStory();}
                return true;
            }
            if(game.life.PanelOpen||game.building||game.journalOpen)return false;
            if(Input.GetKeyDown(KeyCode.G)||Input.GetKeyDown(KeyCode.F6)){OpenDirectory();return true;}
            if(Input.GetKeyDown(KeyCode.H)&&Nearby()!=null){VideosOnly=false;Filter();OpenStory(Nearby());return true;}
            if(!InRoom&&game.player.vehicle==null&&OdysseyAccessibility.InteractPressed()&&Vector3.Distance(game.player.transform.position,CampusExpansion.Find("50").Arrival)<5){EnterRoom();return true;}
            if(InRoom&&OdysseyAccessibility.InteractPressed()&&Vector3.Distance(game.player.transform.position,RoomOrigin+new Vector3(0,0,-3.5f))<2.5f){ExitRoom();return true;}
            return false;
        }
        void Filter(){filtered=catalog.Search(search);if(VideosOnly)filtered=filtered.Where(p=>p.media.Any(m=>m.kind=="video")).ToArray();}
        public void OpenDirectory(){VideosOnly=false;search="";Filter();listScroll=Vector2.zero;Open(Nearby()??selected);}
        public void OpenVideos(){VideosOnly=true;search="";Filter();listScroll=Vector2.zero;Open(filtered.FirstOrDefault());}
        // Shared actions are callable by a future XR controller adapter; no headset input is claimed here.
        public void OpenStory(TourPlace place){OpenInternal(place,true);}
        public void Open(TourPlace place){OpenInternal(place,false);}
        void OpenInternal(TourPlace place,bool compact)
        {
            if(place==null)return;
            StopMedia();sidePanel=compact;storyFocus=0;detailFocus=0;detailFocusActive=false;selected=place;directoryFocus=filtered==null?0:Mathf.Max(0,System.Array.IndexOf(filtered,place));detailScroll=Vector2.zero;PrepareNarration(place);revealChars=place.summary.Length;openedAt=Time.unscaledTime;game.life.SetPanel("tour");
            if(read.Add(game.state.active+"."+place.id)){storySound.volume=game.sound.Muted?0:game.sound.Volume;storySound.PlayOneShot(storyClip);}
            Status="";
        }
        string VoiceKey(TourPlace place)
        {
            if(place==null||place.campusIds==null)return null;
            if(Array.IndexOf(place.campusIds,"26")>=0)return "Ferguson";
            if(Array.IndexOf(place.campusIds,"16")>=0)return "Robinson";
            if(Array.IndexOf(place.campusIds,"1")>=0)return "Bonta";
            if(place.campusIds.Any(id=>id=="18k"||id=="18n"||id=="18p"||id=="18u"))return "ScienceComplex";
            return null;
        }
        void PrepareNarration(TourPlace place)
        {
            if(narration==null)return;
            narration.Stop();voicePlaying=false;narrationClip=null;
            narrativeLines=SplitStory(place==null?"":place.summary);narrativePage=0;
            var key=VoiceKey(place);if(!string.IsNullOrWhiteSpace(key))narrationClip=Resources.Load<AudioClip>("CampusTour/Voice/"+key);
            narration.clip=narrationClip;
        }
        string[] SplitStory(string value)
        {
            if(string.IsNullOrWhiteSpace(value))return new[]{"This story is still being prepared."};
            var parts=value.Split(new[]{'.','?','!'},StringSplitOptions.RemoveEmptyEntries).Select(part=>part.Trim()).Where(part=>part.Length>0).ToArray();
            return parts.Length==0?new[]{value.Trim()}:parts.Select(part=>part+".").ToArray();
        }
        string CurrentStory=>narrativeLines!=null&&narrativeLines.Length>0?narrativeLines[Mathf.Clamp(narrativePage,0,narrativeLines.Length-1)]:"";
        string DisplayStory=>selected==null?"":selected.summary;
        string RevealedStory()
        {
            int count=Mathf.Clamp(Mathf.FloorToInt(revealChars),0,DisplayStory.Length);
            return DisplayStory.Substring(0,count);
        }
        void AdvanceStory()
        {
            if(narration!=null)narration.Stop();voicePlaying=false;
            if(revealChars<DisplayStory.Length){revealChars=DisplayStory.Length;return;}
            Close();
        }
        void ToggleNarration()
        {
            if(narration==null||narrationClip==null){Status="Voice narration is being prepared for this building.";return;}
            if(narration.isPlaying){narration.Stop();voicePlaying=false;return;}
            revealChars=DisplayStory.Length;narration.Play();voicePlaying=true;
        }
        string NarrationLabel=>narrationClip==null?"Audio unavailable":voicePlaying?"Stop audio description":"Play audio description";
        string MediaCaption=>activeMedia==null||!OdysseyAccessibility.CaptionsEnabled?"":activeMedia.caption;
        int StoryActionCount()=>5;
        public void Close(){StopMedia();sidePanel=false;game.life.SetPanel("");}
        public bool EnterRoom()
        {
            if(InRoom){Close();return true;}
            if(!game.player.TryExitVehicle()){Status="Move the car into an open space first.";return false;}
            if(room==null)
            {
                room=TourRoom.Build(RoomOrigin);
                var light=new GameObject("Dorm warm fill").AddComponent<Light>();light.transform.SetParent(room.transform);light.transform.position=RoomOrigin+new Vector3(0,2.5f,0);light.type=LightType.Point;light.range=10;light.intensity=1.8f;
            }
            returnPosition=game.player.transform.position;returnRotation=game.player.transform.rotation;returnThird=game.player.thirdPerson;
            game.player.Teleport(RoomOrigin+new Vector3(0,.08f,-3.55f));game.player.transform.rotation=Quaternion.identity;game.player.thirdPerson=false;InRoom=true;Close();
            game.notice="Wesley reference room · WASD / arrows · H information · "+OdysseyAccessibility.InteractLabel+" by the doorway returns to campus. Dimensions and unseen surfaces are provisional.";
            return true;
        }
        public void ExitRoom()
        {
            if(!InRoom)return;
            game.player.Teleport(returnPosition);game.player.transform.rotation=returnRotation;game.player.thirdPerson=returnThird;InRoom=false;Close();
            game.notice="Back on campus. H reads a nearby building; G opens all building stories.";
        }
        public void ShowMedia(TourMedia item)
        {
            StopMedia();if(item==null||!TourCatalog.SafeMedia(item.url)){Status="This media link is unavailable.";return;}
            activeMedia=item;Status="Loading "+item.kind+"…";mediaStarted=Time.unscaledTime;
            if(item.kind=="video")
            {
                video=gameObject.AddComponent<VideoPlayer>();video.playOnAwake=false;video.source=VideoSource.Url;video.url=item.url;
                videoTexture=new RenderTexture(1280,720,0);videoTexture.Create();video.renderMode=VideoRenderMode.RenderTexture;video.targetTexture=videoTexture;
                videoAudio=gameObject.AddComponent<AudioSource>();videoAudio.spatialBlend=0;videoAudio.volume=game.sound.Muted?0:game.sound.Volume;
                video.audioOutputMode=VideoAudioOutputMode.AudioSource;video.controlledAudioTrackCount=1;video.SetTargetAudioSource(0,videoAudio);
                video.prepareCompleted+=VideoReady;video.errorReceived+=VideoError;video.Prepare();
            }
            else loading=StartCoroutine(LoadImage(item));
        }
        void VideoReady(VideoPlayer p){if(p!=video)return;videosStarted.Add(p.url);Status="Video · use Pause to stop playback.";p.Play();}
        void VideoError(VideoPlayer p,string error){if(p!=video)return;Status="Video could not play here. Retry or open the official page.";Debug.LogWarning(error);StopMedia(false);}
        IEnumerator LoadImage(TourMedia item)
        {
            request=UnityWebRequestTexture.GetTexture(item.url,true);request.timeout=25;
            yield return request.SendWebRequest();
            if(request.result!=UnityWebRequest.Result.Success){Status="Media could not load. Check the connection, then retry or open the official page.";request.Dispose();request=null;loading=null;yield break;}
            photo=DownloadHandlerTexture.GetContent(request);request.Dispose();request=null;loading=null;
            if(item.kind=="panorama")
            {
                panoramaTexture=new RenderTexture(1280,720,24);panoramaTexture.Create();
                panoramaCamera=new GameObject("Tour panorama camera").AddComponent<Camera>();panoramaCamera.transform.position=new Vector3(2500,100,2500);
                panoramaCamera.enabled=false;panoramaCamera.cullingMask=1<<29;panoramaCamera.clearFlags=CameraClearFlags.SolidColor;panoramaCamera.targetTexture=panoramaTexture;panoramaCamera.fieldOfView=75;panoramaCamera.farClipPlane=100;
                panoramaSphere=GameObject.CreatePrimitive(PrimitiveType.Sphere);Destroy(panoramaSphere.GetComponent<Collider>());panoramaSphere.layer=29;panoramaSphere.transform.position=panoramaCamera.transform.position;panoramaSphere.transform.localScale=Vector3.one*40;
                panoramaMaterial=new Material(Shader.Find("Odyssey/TourPanorama"));panoramaMaterial.mainTexture=photo;panoramaSphere.GetComponent<Renderer>().sharedMaterial=panoramaMaterial;yaw=0;pitch=0;
                Status="360° reference · drag to look around. This is one photographed viewpoint.";
            }
            else Status="Photo reference · proportions and hidden rooms still require verification.";
        }
        public void StopMedia(bool clear=true)
        {
            if(narration!=null){narration.Stop();voicePlaying=false;}
            if(loading!=null){StopCoroutine(loading);loading=null;}
            if(request!=null){request.Abort();request.Dispose();request=null;}
            if(video!=null){video.prepareCompleted-=VideoReady;video.errorReceived-=VideoError;video.Stop();Destroy(video);video=null;}
            if(videoAudio!=null){Destroy(videoAudio);videoAudio=null;}
            if(photo!=null){Destroy(photo);photo=null;}
            if(panoramaCamera!=null){Destroy(panoramaCamera.gameObject);panoramaCamera=null;}
            if(panoramaSphere!=null){Destroy(panoramaSphere);panoramaSphere=null;}
            if(panoramaMaterial!=null){Destroy(panoramaMaterial);panoramaMaterial=null;}
            if(panoramaTexture!=null){panoramaTexture.Release();Destroy(panoramaTexture);panoramaTexture=null;}
            if(videoTexture!=null){videoTexture.Release();Destroy(videoTexture);videoTexture=null;}
            if(clear)activeMedia=null;
        }
        void Update()
        {
            if(game==null)return;
            if(!IsOpen&&activeMedia!=null)StopMedia();
            if(IsOpen&&selected!=null&&activeMedia==null&&!voicePlaying){float target=selected.summary.Length;revealChars=Mathf.MoveTowards(revealChars,target,Time.unscaledDeltaTime*62f);}
            if(voicePlaying&&narration!=null&&narration.isPlaying&&narrationClip!=null&&narrativeLines.Length>0)narrativePage=Mathf.Clamp(Mathf.FloorToInt((narration.time/Mathf.Max(.01f,narrationClip.length))*narrativeLines.Length),0,narrativeLines.Length-1);
            if(voicePlaying&&narration!=null&&!narration.isPlaying){voicePlaying=false;Status="Story complete. Explore the references or enter the building.";}
            if(video!=null&&!video.isPrepared&&Time.unscaledTime-mediaStarted>30){Status="Video preparation timed out. Retry or use the official page.";StopMedia(false);}
            if(InRoom&&Vector3.Distance(game.player.transform.position,RoomOrigin)>30)InRoom=false;
        }
        void OnDestroy(){StopMedia();}
        void InitStyles()
        {
            if(title!=null)return;
            title=new GUIStyle(GUI.skin.label){font=AlbionUITheme.DisplayFont,fontSize=AlbionUITheme.TextSize(31),fontStyle=FontStyle.Bold,wordWrap=true,richText=false};title.normal.textColor=new Color(.96f,.94f,.86f);
            text=new GUIStyle(GUI.skin.label){font=AlbionUITheme.BodyFont,fontSize=AlbionUITheme.TextSize(18),wordWrap=true,richText=false};text.normal.textColor=new Color(.88f,.88f,.92f);story=new GUIStyle(text){font=AlbionUITheme.BodyFont,fontSize=AlbionUITheme.TextSize(21)};detailTitle=new GUIStyle(title){font=AlbionUITheme.DisplayFont,fontSize=AlbionUITheme.TextSize(26)};small=new GUIStyle(text){font=AlbionUITheme.BodyFont,fontSize=AlbionUITheme.TextSize(13)};small.normal.textColor=new Color(.52f,.84f,.9f);
            button=new GUIStyle(GUI.skin.button){font=AlbionUITheme.BodyFont,fontSize=AlbionUITheme.TextSize(16),wordWrap=true,richText=false,border=new RectOffset(),padding=new RectOffset(12,12,8,8)};
            foreach(var style in new[]{button.normal,button.hover,button.active,button.focused}){style.background=Texture2D.whiteTexture;style.textColor=Color.white;}
            textButton=new GUIStyle(GUI.skin.button){font=AlbionUITheme.BodyFont,fontSize=AlbionUITheme.TextSize(15),wordWrap=true,richText=false,alignment=TextAnchor.MiddleCenter,padding=new RectOffset(8,8,4,4),border=new RectOffset()};
            foreach(var style in new[]{textButton.normal,textButton.hover,textButton.active,textButton.focused}){style.background=null;style.textColor=new Color(.95f,.92f,.84f,1);}
        }
        void Box(Rect r,Color c){GUI.color=c;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=Color.white;}
        bool Button(Rect r,string value){var color=GUI.backgroundColor;if(color==Color.white)GUI.backgroundColor=new Color(.29f,.17f,.40f);bool hit=GUI.Button(r,value,button);GUI.backgroundColor=color;return hit;}
        bool ControlButton(Rect r,string value)
        {return GUI.Button(r,value,textButton);}
        string FocusLabel(int index,string value)=>storyFocus==index?"▶  "+value:value;
        string DetailFocusLabel(int index,string value)=>detailFocusActive&&detailFocus==index?"▶  "+value:value;
        bool NextButton(Rect r,string value)
        {return GUI.Button(r,value,textButton);}
        bool EnterSelected()
        {
            if(selected==null)return false;
            StopMedia();
            if(selected.id=="888"){CampusBuildings.Instance.Visit();return true;}
            if(selected.campusIds!=null&&Array.IndexOf(selected.campusIds,"16")>=0){CampusBuildings.Instance.VisitCampus("16");return true;}
            if(selected.campusIds!=null&&Array.IndexOf(selected.campusIds,"1")>=0){CampusBuildings.Instance.VisitCampus("1");return true;}
            if(selected.campusIds!=null&&(Array.IndexOf(selected.campusIds,"18k")>=0||Array.IndexOf(selected.campusIds,"18n")>=0||Array.IndexOf(selected.campusIds,"18p")>=0||Array.IndexOf(selected.campusIds,"18u")>=0)){CampusBuildings.Instance.VisitCampus("18");return true;}
            if(selected.id=="917"){EnterRoom();return true;}
            return false;
        }
        bool HasEntry(TourPlace place)
        {
            if(place==null)return false;
            if(place.id=="888"||place.id=="917")return true;
            if(place.campusIds==null)return false;
            return Array.IndexOf(place.campusIds,"16")>=0||Array.IndexOf(place.campusIds,"1")>=0||place.campusIds.Any(id=>id=="18k"||id=="18n"||id=="18p"||id=="18u");
        }
        int SourceFocus(){return selected==null?0:selected.media.Length+(narrationClip!=null?1:0)+(HasEntry(selected)?1:0);}
        int DetailActionCount(){return Mathf.Max(1,(selected==null?0:selected.media.Length)+(narrationClip!=null?1:0)+(HasEntry(selected)?1:0)+1);}
        int MediaActionCount(){int count=2;if(video!=null&&video.isPrepared)count++;if(videoAudio!=null)count++;return count;}
        void ActivateDetailFocus()
        {
            if(selected==null)return;
            int mediaCount=selected.media.Length;
            if(detailFocus<mediaCount){ShowMedia(selected.media[detailFocus]);return;}
            int cursor=mediaCount;
            if(narrationClip!=null){if(detailFocus==cursor){ToggleNarration();return;}cursor++;}
            if(HasEntry(selected)&&detailFocus==cursor){EnterSelected();return;}
            if(TourCatalog.SafeSource(selected.source))Application.OpenURL(selected.source);
        }
        void ActivateMediaFocus()
        {
            if(activeMedia==null)return;
            if(detailFocus==0)StopMedia();else if(detailFocus==1)ShowMedia(activeMedia);else if(detailFocus==2&&video!=null&&video.isPrepared){if(video.isPlaying)video.Pause();else video.Play();}else if(detailFocus==3&&videoAudio!=null)videoAudio.mute=!videoAudio.mute;
        }
        void DrawStorySide(float width,float height,Rect safe)
        {
            float panelWidth=Mathf.Min(505,width*.44f),x=Mathf.Clamp(width-panelWidth,safe.xMin,safe.xMax-panelWidth);
            Box(new Rect(x,190,panelWidth,height-190),new Color(.005f,.008f,.012f,.68f));
            GUI.Label(new Rect(x+30,28,panelWidth-90,22),"HISTORY",small);
            if(ControlButton(new Rect(x+panelWidth-105,22,78,34),"Close"))Close();
            GUI.Label(new Rect(x+30,67,panelWidth-60,90),selected.name,detailTitle);
            GUI.Label(new Rect(x+30,160,panelWidth-60,22),selected.category.ToUpperInvariant(),small);
            GUI.Label(new Rect(x+30,214,panelWidth-60,22),"THE STORY",small);
            GUI.Label(new Rect(x+30,250,panelWidth-60,245),RevealedStory(),story);
            if(OdysseyAccessibility.CaptionsEnabled&&narrationClip!=null){Box(new Rect(x+30,468,panelWidth-60,38),new Color(.04f,.07f,.09f,.95f));GUI.Label(new Rect(x+42,476,panelWidth-84,24),"CAPTIONS  ·  "+(voicePlaying?CurrentStory:"Audio description ready."),small);}
            if(ControlButton(new Rect(x+30,520,185,38),FocusLabel(0,NarrationLabel)))ToggleNarration();
            if(ControlButton(new Rect(x+235,520,185,38),FocusLabel(1,"Read online"))&&TourCatalog.SafeSource(selected.source))Application.OpenURL(selected.source);
            if(ControlButton(new Rect(x+30,575,185,38),FocusLabel(2,"More information"))){sidePanel=false;Open(selected);}
            if(ControlButton(new Rect(x+235,575,185,38),FocusLabel(3,"Enter building")))EnterSelected();
            if(ControlButton(new Rect(x+30,630,185,38),FocusLabel(4,"Close")))Close();
        }
        void OnGUI()
        {
            if(game==null)return;InitStyles();var old=GUI.matrix;
            float scale=Mathf.Min(Screen.width/1280f,Screen.height/800f);GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);
            float width=Screen.width/scale,height=Screen.height/scale;Rect safe=AlbionUITheme.SafeArea(scale);float left=Mathf.Max(24,safe.xMin+8),right=Mathf.Min(width-24,safe.xMax-8);GUI.matrix=AlbionUITheme.Slide(GUI.matrix,openedAt,OdysseyAccessibility.ReducedMotion);
            if(!IsOpen){GUI.matrix=old;return;}
            if(sidePanel){DrawStorySide(width,height,safe);GUI.matrix=old;return;}
            Box(new Rect(0,0,width,height),new Color(.025f,.028f,.052f));Box(new Rect(0,0,width,94),new Color(.075f,.055f,.12f));Box(new Rect(0,0,width,5),new Color(1f,.76f,.28f));
            GUI.Label(new Rect(28,14,width-260,22),"ALBION COLLEGE ARCHIVE",small);
            GUI.Label(new Rect(28,35,width-260,38),VideosOnly?"College videos":"Places & stories",title);
            GUI.Label(new Rect(30,72,width-300,20),"Explore the history and reference material for each campus place.",small);
            if(Button(new Rect(right-160,safe.yMin+23,160,45),"Return · Esc"))Close();
            var next=GUI.TextField(new Rect(left,110,310,38),search,80);if(next!=search){search=next;Filter();listScroll=Vector2.zero;}
            if(Button(new Rect(left,158,148,36),"All buildings")){VideosOnly=false;Filter();listScroll=Vector2.zero;}
            if(Button(new Rect(left+156,158,148,36),"College videos")){VideosOnly=true;Filter();listScroll=Vector2.zero;if(filtered.Length>0&&!selected.media.Any(m=>m.kind=="video"))Open(filtered[0]);}
            listScroll=GUI.BeginScrollView(new Rect(left-4,208,325,height-234),listScroll,new Rect(0,0,299,filtered.Length*67));
            for(int i=0;i<filtered.Length;i++)
            {
                GUI.backgroundColor=filtered[i]==selected?new Color(.73f,.52f,.19f):new Color(.10f,.15f,.17f);
                if(Button(new Rect(0,i*67,294,60),filtered[i].name))Open(filtered[i]);
            }
            GUI.EndScrollView();GUI.backgroundColor=Color.white;
            if(selected!=null)
            {
                float x=Mathf.Max(370,left+346),w=Mathf.Max(300,right-x);
                GUI.Label(new Rect(x,108,w,68),selected.name,detailTitle);
                GUI.Label(new Rect(x,184,w,24),selected.category.ToUpperInvariant()+"   ·   "+(selected.media.Length>0?selected.media.Length+" REFERENCE ITEMS":"TEXT ARCHIVE"),small);
                if(activeMedia==null)
                {
                    Box(new Rect(x,214,w,236),new Color(.075f,.105f,.115f));Box(new Rect(x,214,5,236),new Color(.86f,.63f,.22f));
                    GUI.Label(new Rect(x+24,230,w-48,22),"THE STORY",small);
                    detailScroll=GUI.BeginScrollView(new Rect(x+22,258,w-44,132),detailScroll,new Rect(0,0,w-66,Mathf.Max(120,story.CalcHeight(new GUIContent(selected.summary),w-66)+8)));
                    GUI.Label(new Rect(0,0,w-66,Mathf.Max(120,story.CalcHeight(new GUIContent(selected.summary),w-66))),RevealedStory(),story);GUI.EndScrollView();
                    GUI.Label(new Rect(x,462,w,27),"REFERENCE MATERIAL",small);
                    if(narrationClip!=null&&Button(new Rect(x,495,w,40),DetailFocusLabel(selected.media.Length,NarrationLabel)))ToggleNarration();
                    for(int i=0;i<selected.media.Length;i++)
                    {
                        var m=selected.media[i];string label=m.kind=="panorama"?"360° room view":m.kind=="video"?"Watch video":"Photo "+(i+1);
                        if(Button(new Rect(x+(i%3)*(w/3),555+(i/3)*49,w/3-10,42),DetailFocusLabel(i,label)))ShowMedia(m);
                    }
                    if(selected.media.Length==0)GUI.Label(new Rect(x,555,w,42),"This building has no attached media. Read the building story online below.",text);
                    int enterFocus=selected.media.Length;
                    if(selected.id=="888"&&Button(new Rect(x,650,w,45),DetailFocusLabel(enterFocus,"ENTER FERGUSON  /  THREE LEVELS"))){StopMedia();CampusBuildings.Instance.Visit();}
                    if(selected.campusIds!=null&&Array.IndexOf(selected.campusIds,"16")>=0&&Button(new Rect(x,650,w,45),DetailFocusLabel(enterFocus,"ENTER ROBINSON HALL  /  FOUR LEVELS"))){StopMedia();CampusBuildings.Instance.VisitCampus("16");}
                    if(selected.campusIds!=null&&Array.IndexOf(selected.campusIds,"1")>=0&&Button(new Rect(x,650,w,45),DetailFocusLabel(enterFocus,"ENTER BONTA ADMISSION CENTER"))){StopMedia();CampusBuildings.Instance.VisitCampus("1");}
                    if(selected.campusIds!=null&&(Array.IndexOf(selected.campusIds,"18k")>=0||Array.IndexOf(selected.campusIds,"18n")>=0||Array.IndexOf(selected.campusIds,"18p")>=0||Array.IndexOf(selected.campusIds,"18u")>=0)&&Button(new Rect(x,650,w,45),DetailFocusLabel(enterFocus,"ENTER SCIENCE COMPLEX  /  FOUR LEVELS"))){StopMedia();CampusBuildings.Instance.VisitCampus("18");}
                    if(selected.id=="917"&&Button(new Rect(x,650,w,45),DetailFocusLabel(enterFocus,InRoom?"RESUME THE 3D ROOM":"WALK INSIDE  /  WESLEY ROOM STUDY")))EnterRoom();
                }
                else
                {
                    var r=new Rect(x,216,w,Mathf.Min(400,height-380));
                    if(photo!=null&&activeMedia.kind=="photo")GUI.DrawTexture(r,photo,ScaleMode.ScaleToFit);
                    if(panoramaCamera!=null)
                    {
                        if(r.Contains(Event.current.mousePosition)&&Event.current.type==EventType.MouseDrag&&Event.current.button==0){yaw+=Event.current.delta.x*.3f;pitch=Mathf.Clamp(pitch+Event.current.delta.y*.3f,-80,80);Event.current.Use();}
                        panoramaCamera.transform.rotation=Quaternion.Euler(pitch,yaw,0);if(Event.current.type==EventType.Repaint)panoramaCamera.Render();GUI.DrawTexture(r,panoramaTexture,ScaleMode.ScaleToFit);
                    }
                    if(video!=null&&video.isPrepared)GUI.DrawTexture(r,videoTexture,ScaleMode.ScaleToFit);
                    float controlsY=r.yMax+(MediaCaption.Length>0?58:14);
                    if(MediaCaption.Length>0){Box(new Rect(x,r.yMax+8,w,42),new Color(.04f,.07f,.09f,.95f));GUI.Label(new Rect(x+12,r.yMax+15,w-24,28),"CAPTIONS  ·  "+MediaCaption,small);}
                    if(Button(new Rect(x,controlsY,170,42),DetailFocusLabel(0,"Back to information")))StopMedia();
                    if(activeMedia!=null&&Button(new Rect(x+185,controlsY,110,42),DetailFocusLabel(1,"Retry")))ShowMedia(activeMedia);
                    if(video!=null&&video.isPrepared&&Button(new Rect(x+310,controlsY,120,42),DetailFocusLabel(2,video.isPlaying?"Pause":"Play"))){if(video.isPlaying)video.Pause();else video.Play();}
                    if(videoAudio!=null&&Button(new Rect(x+445,controlsY,120,42),DetailFocusLabel(3,videoAudio.mute?"Unmute":"Mute")))videoAudio.mute=!videoAudio.mute;
                }
                GUI.Label(new Rect(x,height-99,w-205,77),Status,small);
                int readFocus=SourceFocus();if(Button(new Rect(right-180,safe.yMax-86,180,46),DetailFocusLabel(readFocus,"Read online"))&&TourCatalog.SafeSource(selected.source))Application.OpenURL(selected.source);
            }
            GUI.Label(new Rect(left,safe.yMax-22,right-left,18),AlbionControls.MenuFooter(game.xr!=null&&game.xr.Active,"Open"),small);
            GUI.matrix=old;
        }
    }
}
