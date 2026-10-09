using UnityEngine;
namespace AlbionOdyssey
{
    // Original presentation based on the supplied glass-panel composition.
    public sealed class ConversationPresentation
    {
        Font font,boldFont;RenderTexture scene,portrait;Texture2D portraitImage;Material blur;
        public Texture Portrait=>portraitImage;
        public GUIStyle Text(int size,Color color,bool bold=false)
        {
            if(font==null)font=Resources.Load<Font>("Fonts/FiraSans-Regular");
            if(boldFont==null)boldFont=Resources.Load<Font>("Fonts/FiraSans-SemiBold");
            return new GUIStyle{font=bold?boldFont:font,fontSize=size,wordWrap=true,richText=false,normal={textColor=color}};
        }
        public void Panel(Rect r,Color c,float radius=6){var old=GUI.color;GUI.color=Color.white;GUI.DrawTexture(r,Texture2D.whiteTexture,ScaleMode.StretchToFill,true,0,QualitySettings.activeColorSpace==ColorSpace.Linear?c.linear:c,0,Mathf.Min(radius,r.height/2));GUI.color=old;}
        public bool Button(Rect r,string label,bool primary=false,bool focused=false){bool hover=focused||r.Contains(Event.current.mousePosition);Color cyan=new Color(.35f,1,1);if(hover){Panel(new Rect(r.x-7,r.y-7,r.width+14,r.height+14),new Color(.2f,.9f,1,.12f));Panel(new Rect(r.x-2,r.y-2,r.width+4,r.height+4),cyan);}Panel(r,primary?new Color(.28f,.26f,.84f):new Color(.08f,.06f,.29f,.9f));var t=Text(19,Color.white,true);t.alignment=TextAnchor.MiddleCenter;GUI.Label(r,label,t);return GUI.Button(r,GUIContent.none,GUIStyle.none);}
        public void Capture(OdysseyGame game)
        {
            var originalTarget=RenderTexture.active;Dispose();scene=new RenderTexture(640,400,0);var raw=RenderTexture.GetTemporary(640,400,24);var camera=game.player.eyes;var old=camera.targetTexture;camera.targetTexture=raw;camera.Render();camera.targetTexture=old;
            var shader=Resources.Load<Shader>("Shaders/ConversationBlur");if(shader!=null){blur=new Material(shader);Graphics.Blit(raw,scene,blur);}else Graphics.Blit(raw,scene);RenderTexture.ReleaseTemporary(raw);
            var avatar=game.player.avatar.gameObject;bool active=avatar.activeSelf;avatar.SetActive(true);
            var nodes=avatar.GetComponentsInChildren<Transform>(true);var layers=new int[nodes.Length];for(int i=0;i<nodes.Length;i++){layers[i]=nodes[i].gameObject.layer;nodes[i].gameObject.layer=30;}
            var co=new GameObject("Conversation portrait camera");var cam=co.AddComponent<Camera>();cam.enabled=false;cam.cullingMask=1<<30;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.04f,.01f,.16f,0);cam.fieldOfView=30;cam.nearClipPlane=.03f;cam.farClipPlane=30;
            var target=avatar.transform.position+Vector3.up*1.55f;cam.transform.position=target+avatar.transform.forward*1.12f;cam.transform.LookAt(target);
            portrait=new RenderTexture(480,480,24);portrait.Create();cam.targetTexture=portrait;
            var light=co.AddComponent<Light>();light.type=LightType.Point;light.color=new Color(.72f,.68f,1);light.intensity=.7f;light.range=8;light.cullingMask=1<<30;cam.Render();
            var previous=RenderTexture.active;RenderTexture.active=portrait;portraitImage=new Texture2D(480,480,TextureFormat.RGBA32,false);portraitImage.ReadPixels(new Rect(0,0,480,480),0,0);portraitImage.Apply();RenderTexture.active=previous;
            if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-consoleUiSmoke")>=0)System.IO.File.WriteAllBytes("/tmp/albion-profile-preview.png",portraitImage.EncodeToPNG());
            for(int i=0;i<nodes.Length;i++)nodes[i].gameObject.layer=layers[i];avatar.SetActive(active);Object.Destroy(co);RenderTexture.active=originalTarget;

        }
        public void Background(float w,float h){if(scene!=null)GUI.DrawTexture(new Rect(0,0,w,h),scene,ScaleMode.ScaleAndCrop);OdysseyUI.Fill(new Rect(0,0,w,h),new Color(.04f,.006f,.22f,.50f));}
        public void Dispose(){if(portraitImage!=null)Object.Destroy(portraitImage);if(scene!=null){scene.Release();Object.Destroy(scene);}if(portrait!=null){portrait.Release();Object.Destroy(portrait);}if(blur!=null)Object.Destroy(blur);}
    }
}
