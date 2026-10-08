using System.Collections.Generic;
using UnityEngine;

namespace AlbionOdyssey
{
    // Shared screen furniture. The character and campus views are rendered from
    // game assets, not promotional images standing in for gameplay.
    public static class OdysseyUI
    {
        public static readonly Color Navy=new Color(.018f,.019f,.018f), Surface=new Color(.105f,.061f,.025f), Gold=new Color(1f,.43f,.035f), White=new Color(.92f,.93f,.90f), Muted=new Color(.64f,.65f,.61f), Mint=new Color(.12f,.83f,.86f);
        static Texture2D round,gradient;
        static GUIStyle panel;
        static readonly Dictionary<int,GUIStyle> fonts=new Dictionary<int,GUIStyle>();
        static readonly Dictionary<string,float> hover=new Dictionary<string,float>();
        public static GUIStyle Font(int size,bool bold=false)
        {
            int key=size*2+(bold?1:0);
            if(!fonts.TryGetValue(key,out var style))
            {
                style=new GUIStyle(GUI.skin.label){font=size>=28?AlbionUITheme.DisplayFont:AlbionUITheme.BodyFont,fontSize=AlbionUITheme.TextSize(size),fontStyle=bold?FontStyle.Bold:FontStyle.Normal,wordWrap=true,richText=false};
                style.normal.textColor=Color.white;fonts[key]=style;
            }
            style.fontSize=AlbionUITheme.TextSize(size);return style;
        }
        public static void Fill(Rect r,Color c){var old=GUI.color;GUI.color=QualitySettings.activeColorSpace==ColorSpace.Linear?c.linear:c;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=old;}
        public static void Text(Rect r,string value,int size,Color c,bool bold=false){var old=GUI.contentColor;GUI.contentColor=c;GUI.Label(r,value,Font(size,bold));GUI.contentColor=old;}
        public static void Card(Rect r,Color c)
        {
            Fill(r,c); Frame(r,new Color(Gold.r,Gold.g,Gold.b,.24f));
        }
        public static void Frame(Rect r,Color c,float thickness=1)
        {Fill(new Rect(r.x,r.y,r.width,thickness),c);Fill(new Rect(r.x,r.yMax-thickness,r.width,thickness),c);Fill(new Rect(r.x,r.y,thickness,r.height),c);Fill(new Rect(r.xMax-thickness,r.y,thickness,r.height),c);}
        static Texture2D lineCanvas;static Color32[] linePixels;static bool raster;
        public static void BeginLineCanvas(int width,int height)
        {
            raster=true;if(Event.current.type!=EventType.Repaint)return;
            if(lineCanvas==null||lineCanvas.width!=width||lineCanvas.height!=height){if(lineCanvas!=null)Object.Destroy(lineCanvas);lineCanvas=new Texture2D(width,height,TextureFormat.RGBA32,false){filterMode=FilterMode.Bilinear};linePixels=new Color32[width*height];}
            System.Array.Clear(linePixels,0,linePixels.Length);
        }
        public static void EndLineCanvas()
        {
            raster=false;if(Event.current.type!=EventType.Repaint)return;lineCanvas.SetPixels32(linePixels);lineCanvas.Apply(false);var c=GUI.color;GUI.color=Color.white;GUI.DrawTexture(new Rect(0,0,lineCanvas.width,lineCanvas.height),lineCanvas);GUI.color=c;
        }
        public static void Line(Vector2 a,Vector2 b,Color c,float thickness=1)
        {
            if(Event.current.type!=EventType.Repaint)return;
            if(raster)
            {
                int count=Mathf.CeilToInt(Mathf.Max(Mathf.Abs(b.x-a.x),Mathf.Abs(b.y-a.y)));int width=lineCanvas.width,height=lineCanvas.height;
                for(int i=0;i<=count;i++){var p=Vector2.Lerp(a,b,count==0?0:i/(float)count);int x=Mathf.RoundToInt(p.x),y=Mathf.RoundToInt(p.y);for(int j=0;j<Mathf.CeilToInt(thickness);j++)if(x>=0&&x<width&&y+j>=0&&y+j<height)linePixels[(height-1-y-j)*width+x]=c;}
                return;
            }
            var m=GUI.matrix;GUI.matrix=m*Matrix4x4.TRS(new Vector3(a.x,a.y,0),Quaternion.Euler(0,0,Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg),Vector3.one);Fill(new Rect(0,0,(b-a).magnitude,thickness),c);GUI.matrix=m;
        }
        public static bool Button(Rect r,string label,string id,bool selected=false,bool primary=false)
        {
            bool over=GUI.enabled&&r.Contains(Event.current.mousePosition),active=selected||over||primary;
            Fill(r,!GUI.enabled?new Color(.06f,.06f,.055f):active?Gold:Surface);
            Fill(new Rect(r.x,r.yMax-1,r.width,1),new Color(1,.43f,.035f,active?1:.3f));
            if(over||selected)Frame(new Rect(r.x-2,r.y-2,r.width+4,r.height+4),new Color(1,.55f,.08f,.5f));
            float padding=r.width<160?9:16;int size=r.height<44||r.width<160?14:18;float th=Mathf.Min(r.height,AlbionUITheme.TextSize(size)+10);
            Text(new Rect(r.x+padding,r.y+(r.height-th)*.5f,r.width-padding*2,th),label.ToUpperInvariant(),size,!GUI.enabled?Muted:active?Navy:Gold,false);
            bool hit=GUI.Button(r,GUIContent.none,GUIStyle.none);if(hit){GUIUtility.keyboardControl=0;OdysseyPresentation.Instance?.Click();}return hit;
        }
        public static void Shade(Rect r)
        {
            if(gradient==null){gradient=new Texture2D(128,1,TextureFormat.RGBA32,false);for(int x=0;x<128;x++)gradient.SetPixel(x,0,new Color(Navy.r,Navy.g,Navy.b,Mathf.Lerp(.98f,.04f,x/127f)));gradient.Apply();}
            GUI.DrawTexture(r,gradient);
        }
        public static void Spinner(Rect r,float progress=-1)
        {
            int bright=OdysseyAccessibility.ReducedMotion?0:(int)(Time.unscaledTime*10)%12;
            for(int i=0;i<12;i++){float angle=i*Mathf.PI/6;float a=progress>=0?(i/12f<progress?1:.15f):.15f+.85f*((i-bright+12)%12)/11f;var c=Mint;c.a=a;Card(new Rect(r.center.x+Mathf.Sin(angle)*r.width*.36f-3,r.center.y+Mathf.Cos(angle)*r.height*.36f-3,6,6),c);}
        }
    }

    public sealed class OdysseyPresentation : MonoBehaviour
    {
        public static OdysseyPresentation Instance {get;private set;}
        OdysseyGame game;KeeperAvatar student;Camera portraitCamera,vistaCamera;GameObject stage;
        RenderTexture portrait,vista;AudioSource uiAudio;AudioClip click;float lastFrame;
        int appearance=-1;
        public bool CharacterReady=>student!=null&&student.IsRigged;
        public RenderTexture CampusView=>vista;
        public void Setup(OdysseyGame owner)
        {
            Instance=this;game=owner;
            stage=new GameObject("Lobby student showcase");stage.transform.position=new Vector3(0,-1500,0);
            student=new GameObject("Your student · animated preview").AddComponent<KeeperAvatar>();student.transform.SetParent(stage.transform,false);
            RefreshStudent();
            portrait=new RenderTexture(700,900,24,RenderTextureFormat.ARGB32){name="Live student portrait"};portrait.Create();
            portraitCamera=new GameObject("Lobby portrait camera").AddComponent<Camera>();portraitCamera.transform.SetParent(stage.transform,false);
            portraitCamera.transform.localPosition=new Vector3(0,1.22f,4.7f);portraitCamera.transform.LookAt(stage.transform.position+Vector3.up*.98f);
            portraitCamera.fieldOfView=29;portraitCamera.cullingMask=1<<29;portraitCamera.clearFlags=CameraClearFlags.SolidColor;portraitCamera.backgroundColor=Color.clear;portraitCamera.targetTexture=portrait;portraitCamera.enabled=false;
            AddLight("Showcase key",new Vector3(-2,3,3),new Color(1,.91f,.80f),2.2f);
            AddLight("Showcase rim",new Vector3(2,2,-1),new Color(.42f,.72f,1),2.5f);
            vista=new RenderTexture(1440,900,24){name="Albion campus lobby vista"};vista.Create();
            vistaCamera=new GameObject("Campus vista camera").AddComponent<Camera>();vistaCamera.enabled=false;vistaCamera.cullingMask=~((1<<29)|(1<<30));vistaCamera.targetTexture=vista;
            vistaCamera.fieldOfView=55;vistaCamera.farClipPlane=450;vistaCamera.clearFlags=CameraClearFlags.Skybox;
            var site=CampusExpansion.Find("26").position;vistaCamera.transform.position=site+new Vector3(30,13,-44);vistaCamera.transform.LookAt(site+new Vector3(0,4,0));vistaCamera.Render();
            uiAudio=gameObject.AddComponent<AudioSource>();uiAudio.playOnAwake=false;uiAudio.spatialBlend=0;
            click=AudioClip.Create("Menu select",2205,1,44100,false);var samples=new float[2205];
            for(int i=0;i<samples.Length;i++){float t=i/44100f;samples[i]=Mathf.Sin(t*880*Mathf.PI*2)*Mathf.Exp(-t*100)*.16f;}
            click.SetData(samples,0);
        }
        void AddLight(string label,Vector3 pos,Color color,float intensity)
        {var light=new GameObject(label).AddComponent<Light>();light.transform.SetParent(stage.transform,false);light.transform.localPosition=pos;light.type=LightType.Point;light.range=8;light.color=color;light.intensity=intensity;light.cullingMask=1<<29;}
        void RefreshStudent()
        {
            int next=game.campus.skin+game.campus.outfit*10+game.campus.hair*100+(game.campus.backpack?1000:0)+game.state.Current.equippedFinish*10000;if(next==appearance)return;appearance=next;
            student.Build(game.campus.skin,game.campus.outfit,game.campus.hair,game.campus.backpack);student.ApplyFinish(game.state.Current.equippedFinish);
            foreach(var item in student.GetComponentsInChildren<Transform>(true))item.gameObject.layer=29;
        }
        public void Click(){if(uiAudio!=null&&game.sound!=null&&!game.sound.Muted)uiAudio.PlayOneShot(click,game.sound.Volume*.55f);}
        public void DrawStudent(Rect rect)
        {
            if(student==null)return;RefreshStudent();
            if(Event.current.type==EventType.Repaint&&Time.unscaledTime-lastFrame>1f/30)
            {
                lastFrame=Time.unscaledTime;
                student.transform.localRotation=Quaternion.Euler(0,OdysseyAccessibility.ReducedMotion?0:Mathf.Sin(Time.unscaledTime*.22f)*12,0);
                student.Animate(0,false);portraitCamera.Render();
            }
            GUI.DrawTexture(rect,portrait,ScaleMode.ScaleToFit,true);
        }
        public void DrawBackdrop(float w,float h)
        {OdysseyUI.Fill(new Rect(0,0,w,h),OdysseyUI.Navy);if(vista!=null)GUI.DrawTexture(new Rect(0,0,w,h),vista,ScaleMode.ScaleAndCrop);OdysseyUI.Fill(new Rect(0,0,w,h),new Color(.008f,.015f,.028f,.30f));OdysseyUI.Shade(new Rect(0,0,w,h));}
        void OnDestroy()
        {if(Instance==this)Instance=null;if(portrait!=null){portrait.Release();Destroy(portrait);}if(vista!=null){vista.Release();Destroy(vista);}if(stage!=null)Destroy(stage);if(vistaCamera!=null)Destroy(vistaCamera.gameObject);if(click!=null)Destroy(click);}
    }
}
