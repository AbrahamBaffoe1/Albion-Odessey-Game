using UnityEngine;

namespace AlbionOdyssey
{
    // Shared visual primitives for the immediate-mode panels. Keeping this tiny and
    // asset-free lets the Mac, Android and XR players use the same theme.
    public static class AlbionUITheme
    {
        public static readonly Color Ink=new Color(.025f,.028f,.052f), Gold=new Color(1f,.76f,.28f), Purple=new Color(.40f,.22f,.57f), Cyan=new Color(.35f,.84f,.92f);
        public static int TextSize(int size){return OdysseyAccessibility.LargeText?Mathf.RoundToInt(size*1.18f):size;}
        static Texture2D pixel;
        static Font displayFont, bodyFont;
        static Texture2D Pixel(){if(pixel==null){pixel=new Texture2D(1,1,TextureFormat.RGBA32,false);pixel.SetPixel(0,0,Color.white);pixel.Apply();}return pixel;}
        public static Font DisplayFont { get { if(displayFont==null) displayFont=ResolveFont("Avenir Next Condensed"); return displayFont; } }
        public static Font BodyFont { get { if(bodyFont==null) bodyFont=ResolveFont("Helvetica Neue"); return bodyFont; } }
        static Font ResolveFont(string preferred)
        {
#if !UNITY_ANDROID
            try { return Font.CreateDynamicFontFromOSFont(preferred,64); }
            catch { }
#endif
            try { return Resources.GetBuiltinResource<Font>("Arial.ttf"); }
            catch { return null; }
        }
        public static GUIStyle Button(int size=16)
        {
            var style=new GUIStyle(GUI.skin.button){font=BodyFont,fontSize=TextSize(size),fontStyle=FontStyle.Bold,padding=new RectOffset(12,12,7,7),border=new RectOffset()};
            style.normal.background=Pixel();style.normal.textColor=new Color(.98f,.96f,.9f);style.hover.background=Pixel();style.hover.textColor=Color.white;style.active.background=Pixel();style.active.textColor=Gold;style.focused.background=Pixel();style.focused.textColor=Cyan;
            return style;
        }
        // IMGUI uses a top-left origin while Screen.safeArea uses a bottom-left
        // origin. Convert once so HUD cards stay clear of notches, camera cutouts,
        // and headset compositor margins on every target resolution.
        public static Rect SafeArea(float scale)
        {
            Rect safe=Screen.safeArea;
            return new Rect(safe.xMin/scale,(Screen.height-safe.yMax)/scale,safe.width/scale,safe.height/scale);
        }
        public static Matrix4x4 Slide(Matrix4x4 matrix,float openedAt,bool reducedMotion)
        {
            float t=reducedMotion?1:Mathf.Clamp01((Time.unscaledTime-openedAt)*8f);return matrix*Matrix4x4.Translate(new Vector3(0,Mathf.SmoothStep(12,0,t),0));
        }
        public static void TopRule(float width){var old=GUI.color;GUI.color=Gold;GUI.DrawTexture(new Rect(0,0,width,5),Pixel());GUI.color=old;}
    }
}
