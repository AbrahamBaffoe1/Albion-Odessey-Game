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
        OdysseyGame game; string search=""; Vector2 listScroll,detailScroll;
        readonly HashSet<string> read=new HashSet<string>();
        readonly HashSet<string> videosStarted=new HashSet<string>();
        public int StoriesRead=>read.Count; public int VideosStarted=>videosStarted.Count;
        public bool VideosOnly {get;private set;}
        public int VisibleCount=>filtered==null?0:filtered.Length;
        Texture2D photo; Coroutine loading; UnityWebRequest request; TourMedia activeMedia;
        VideoPlayer video; AudioSource videoAudio; RenderTexture videoTexture,panoramaTexture;
        Camera panoramaCamera; GameObject panoramaSphere,room; Material panoramaMaterial;
        float yaw,pitch,mediaStarted; Vector3 returnPosition; Quaternion returnRotation; bool returnThird;
        GUIStyle title,text,small,button; TourPlace[] filtered;
        public static readonly Vector3 RoomOrigin=new Vector3(1600,0,1600);
        public void Setup(OdysseyGame owner)
        {
            game=owner;storySound=gameObject.AddComponent<AudioSource>();storySound.playOnAwake=false;storySound.spatialBlend=0;storyClip=Resources.Load<AudioClip>("CampusTour/BuildingStory");catalog=JsonUtility.FromJson<TourCatalog>(Resources.Load<TextAsset>("CampusTour/catalog").text);
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
                if(Input.GetKeyDown(KeyCode.Escape))Close();
                return true;
            }
            if(game.life.PanelOpen||game.building||game.journalOpen)return false;
            if(Input.GetKeyDown(KeyCode.G)||Input.GetKeyDown(KeyCode.F6)){OpenDirectory();return true;}
            if(Input.GetKeyDown(KeyCode.H)&&Nearby()!=null){VideosOnly=false;Filter();Open(Nearby());return true;}
            if(!InRoom&&game.player.vehicle==null&&Input.GetKeyDown(KeyCode.E)&&Vector3.Distance(game.player.transform.position,CampusExpansion.Find("50").Arrival)<5){EnterRoom();return true;}
            if(InRoom&&Input.GetKeyDown(KeyCode.E)&&Vector3.Distance(game.player.transform.position,RoomOrigin+new Vector3(0,0,-3.5f))<2.5f){ExitRoom();return true;}
            return false;
        }
        void Filter(){filtered=catalog.Search(search);if(VideosOnly)filtered=filtered.Where(p=>p.media.Any(m=>m.kind=="video")).ToArray();}
        public void OpenDirectory(){VideosOnly=false;search="";Filter();listScroll=Vector2.zero;Open(Nearby()??selected);}
        public void OpenVideos(){VideosOnly=true;search="";Filter();listScroll=Vector2.zero;Open(filtered.FirstOrDefault());}
        // Shared actions are callable by a future XR controller adapter; no headset input is claimed here.
        public void Open(TourPlace place)
        {
            if(place==null)return;
            StopMedia();selected=place;detailScroll=Vector2.zero;game.life.SetPanel("tour");
            if(read.Add(game.state.active+"."+place.id)){storySound.volume=game.sound.Muted?0:game.sound.Volume;storySound.PlayOneShot(storyClip);}
            Status="Information summarized from the official tour. Check the source for current services.";
        }
        public void Close(){StopMedia();game.life.SetPanel("");}
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
            game.notice="Wesley reference room · WASD / arrows · H information · E by the doorway returns to campus. Dimensions and unseen surfaces are provisional.";
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
            if(video!=null&&!video.isPrepared&&Time.unscaledTime-mediaStarted>30){Status="Video preparation timed out. Retry or use the official page.";StopMedia(false);}
            if(InRoom&&Vector3.Distance(game.player.transform.position,RoomOrigin)>30)InRoom=false;
        }
        void OnDestroy(){StopMedia();}
        void InitStyles()
        {
            if(title!=null)return;
            title=new GUIStyle(GUI.skin.label){fontSize=27,fontStyle=FontStyle.Bold,wordWrap=true,richText=false};
            text=new GUIStyle(GUI.skin.label){fontSize=18,wordWrap=true,richText=false};small=new GUIStyle(text){fontSize=14};
            button=new GUIStyle(GUI.skin.button){fontSize=16,wordWrap=true,richText=false,border=new RectOffset(),padding=new RectOffset(12,12,8,8)};
            foreach(var style in new[]{button.normal,button.hover,button.active,button.focused}){style.background=Texture2D.whiteTexture;style.textColor=Color.white;}
        }
        void Box(Rect r,Color c){GUI.color=c;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=Color.white;}
        bool Button(Rect r,string value){var color=GUI.backgroundColor;if(color==Color.white)GUI.backgroundColor=new Color(.29f,.17f,.40f);bool hit=GUI.Button(r,value,button);GUI.backgroundColor=color;return hit;}
        void OnGUI()
        {
            if(game==null)return;InitStyles();var old=GUI.matrix;
            float scale=Mathf.Min(Screen.width/1280f,Screen.height/800f);GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);
            float width=Screen.width/scale,height=Screen.height/scale;
            if(!IsOpen){GUI.matrix=old;return;}
            Box(new Rect(0,0,width,height),new Color(.035f,.028f,.055f));Box(new Rect(0,0,width,94),new Color(.22f,.12f,.32f));
            GUI.Label(new Rect(28,16,width-260,38),VideosOnly?"ALBION / COLLEGE VIDEOS":"ALBION / PLACES & STORIES",title);
            GUI.Label(new Rect(30,58,width-300,26),"Discover the campus through its buildings, people and spaces.",small);
            if(Button(new Rect(width-190,23,160,45),"Return · Esc"))Close();
            var next=GUI.TextField(new Rect(24,110,310,38),search,80);if(next!=search){search=next;Filter();listScroll=Vector2.zero;}
            if(Button(new Rect(24,158,148,36),"All buildings")){VideosOnly=false;Filter();listScroll=Vector2.zero;}
            if(Button(new Rect(180,158,148,36),"College videos")){VideosOnly=true;Filter();listScroll=Vector2.zero;if(filtered.Length>0&&!selected.media.Any(m=>m.kind=="video"))Open(filtered[0]);}
            listScroll=GUI.BeginScrollView(new Rect(20,208,325,height-234),listScroll,new Rect(0,0,299,filtered.Length*67));
            for(int i=0;i<filtered.Length;i++)
            {
                GUI.backgroundColor=filtered[i]==selected?new Color(.7f,.48f,.12f):new Color(.29f,.20f,.40f);
                if(Button(new Rect(0,i*67,294,60),filtered[i].name))Open(filtered[i]);
            }
            GUI.EndScrollView();GUI.backgroundColor=Color.white;
            if(selected!=null)
            {
                float x=370,w=width-400;
                GUI.Label(new Rect(x,108,w,72),selected.name,title);
                GUI.Label(new Rect(x,184,w,24),selected.category.ToUpperInvariant(),small);
                if(activeMedia==null)
                {
                    detailScroll=GUI.BeginScrollView(new Rect(x,222,w,190),detailScroll,new Rect(0,0,w-24,Mathf.Max(185,text.CalcHeight(new GUIContent(selected.summary),w-36)+20)));
                    GUI.Label(new Rect(0,0,w-36,Mathf.Max(185,text.CalcHeight(new GUIContent(selected.summary),w-36))),selected.summary,text);GUI.EndScrollView();
                    GUI.Label(new Rect(x,435,w,27),"EXPLORE THE REFERENCES",small);
                    for(int i=0;i<selected.media.Length;i++)
                    {
                        var m=selected.media[i];string label=m.kind=="panorama"?"360° room view":m.kind=="video"?"Watch video":"Photo "+(i+1);
                        if(Button(new Rect(x+(i%3)*(w/3),475+(i/3)*49,w/3-10,42),label))ShowMedia(m);
                    }
                    if(selected.media.Length==0)GUI.Label(new Rect(x,478,w,60),"This entry has no photo, video or panorama in the main tour.",text);
                    if(selected.id=="888"&&Button(new Rect(x,630,w,48),"Explore Ferguson · three levels")){StopMedia();CampusBuildings.Instance.Visit();}
                    if(selected.campusIds!=null&&Array.IndexOf(selected.campusIds,"16")>=0&&Button(new Rect(x,630,w,48),"Explore Robinson Hall · four levels")){StopMedia();CampusBuildings.Instance.VisitCampus("16");}
                    if(selected.campusIds!=null&&Array.IndexOf(selected.campusIds,"1")>=0&&Button(new Rect(x,630,w,48),"Explore Bonta Admission Center")){StopMedia();CampusBuildings.Instance.VisitCampus("1");}
                    if(selected.id=="917"&&Button(new Rect(x,630,w,48),InRoom?"Resume the 3D room":"Walk inside · Wesley room study"))EnterRoom();
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
                    if(Button(new Rect(x,r.yMax+14,170,42),"Back to information"))StopMedia();
                    if(activeMedia!=null&&Button(new Rect(x+185,r.yMax+14,110,42),"Retry"))ShowMedia(activeMedia);
                    if(video!=null&&video.isPrepared&&Button(new Rect(x+310,r.yMax+14,120,42),video.isPlaying?"Pause":"Play")){if(video.isPlaying)video.Pause();else video.Play();}
                    if(videoAudio!=null&&Button(new Rect(x+445,r.yMax+14,120,42),videoAudio.mute?"Unmute":"Mute"))videoAudio.mute=!videoAudio.mute;
                }
                GUI.Label(new Rect(x,height-99,w-205,77),Status,small);
                if(Button(new Rect(width-210,height-86,180,46),"Official tour page")&&TourCatalog.SafeSource(selected.source))Application.OpenURL(selected.source);
            }
            GUI.matrix=old;
        }
    }
}
