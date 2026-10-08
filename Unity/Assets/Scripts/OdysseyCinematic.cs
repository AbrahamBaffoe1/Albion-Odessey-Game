using UnityEngine;
namespace AlbionOdyssey
{
    public static class OdysseyCinematic
    {
        static Font serif;
        public static void Title(Rect rect,string value,int size,Color color,TextAnchor alignment=TextAnchor.MiddleCenter)
        {
            if(serif==null)serif=Resources.Load<Font>("Fonts/Cinzel");
            var style=new GUIStyle(GUI.skin.label){font=serif,fontSize=size,alignment=alignment,wordWrap=false};style.normal.textColor=color;GUI.Label(rect,value,style);
        }
        // Navigation belongs to AlbionUIInput, not IMGUI's retained keyboard focus.
        public static void ConsumeMenuKeys()
        {
            if(Event.current.type!=EventType.KeyDown)return;
            var key=Event.current.keyCode;
            if(key==KeyCode.Return||key==KeyCode.KeypadEnter||key==KeyCode.Space||key==KeyCode.Escape||key==KeyCode.UpArrow||key==KeyCode.DownArrow||key==KeyCode.LeftArrow||key==KeyCode.RightArrow)Event.current.Use();
        }
        public static void Atmosphere(float w,float h)
        {
            if(OdysseyAccessibility.ReducedMotion)return;
            for(int i=0;i<52;i++)
            {
                float phase=i*17.137f,t=Time.unscaledTime;
                float x=Mathf.Repeat(phase*39+t*(i%2==0?3:-2),w),y=h-Mathf.Repeat(phase*13+t*(5+i%7),h);
                float alpha=(.2f+.3f*Mathf.Sin(phase+t))*(1-y/h);
                OdysseyUI.Fill(new Rect(x,y,i%4==0?3:1.5f,2),x<w*.5f?new Color(1,.38f,.08f,alpha):new Color(.2f,.65f,1,alpha));
            }
        }
    }
}
