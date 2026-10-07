using UnityEngine;
namespace AlbionOdyssey
{
    public static class ConsoleMenuStyle
    {
        public static void Background(OdysseyGame game,float width,float height)
        {
            game.presentation?.DrawBackdrop(width,height);
            OdysseyUI.Fill(new Rect(0,0,width,height),new Color(.002f,.003f,.002f,.96f));
            for(float y=0;y<height;y+=4)OdysseyUI.Fill(new Rect(0,y,width,1),new Color(0,0,0,.09f));
            OdysseyUI.Fill(new Rect(60,height-74,width-120,1),new Color(1,.43f,.035f,.3f));
            OdysseyUI.Fill(new Rect(0,height-73,width,74),new Color(.009f,.011f,.010f,.96f));
        }
        public static void Heading(float x,string section,string title,string description)
        {
            OdysseyUI.Text(new Rect(x,55,1000,28),"ALBION ODYSSEY  /  "+section,14,OdysseyUI.Gold,true);
            OdysseyUI.Text(new Rect(x,102,1080,72),title.ToUpperInvariant(),44,OdysseyUI.White);
            OdysseyUI.Text(new Rect(x,180,1080,50),description,18,OdysseyUI.Muted);
            OdysseyUI.Fill(new Rect(x,230,1120,1),new Color(1,.43f,.035f,.4f));
        }
        public static void Footer(float x,float height,string text)
        {OdysseyUI.Text(new Rect(x,height-49,1120,30),text,15,OdysseyUI.Muted);}
        static GUIStyle input;
        public static GUIStyle Field(int size=24)
        {
            if(input!=null)return input;
            var field=new GUIStyle(GUI.skin.textField){font=AlbionUITheme.BodyFont,fontSize=size,richText=false,padding=new RectOffset(18,18,12,12)};
            field.normal.textColor=OdysseyUI.White;field.focused.textColor=OdysseyUI.White;
            var normal=new Texture2D(1,1);normal.SetPixel(0,0,new Color(.10f,.075f,.045f));normal.Apply();
            var selected=new Texture2D(1,1);selected.SetPixel(0,0,new Color(.22f,.12f,.035f));selected.Apply();
            field.normal.background=normal;field.focused.background=selected;field.hover.background=selected;input=field;return field;
        }
    }
}
