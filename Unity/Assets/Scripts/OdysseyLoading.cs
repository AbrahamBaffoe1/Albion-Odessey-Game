using System;
using System.Collections;
using UnityEngine;
namespace AlbionOdyssey
{
    public sealed class OdysseyLoading : MonoBehaviour
    {
        public bool Busy {get;private set;}=true;
        public string Stage {get;private set;}="Opening your campus";
        public int Completed {get;private set;}
        public int Total {get;private set;}=6;
        public bool Failed {get;private set;}
        float ended=-1,opened;Texture2D artwork;
        static readonly string[] Tips={"G opens the building guide. Find a hall and discover its story.","Open Student account from the Esc menu. Your saved avatar follows your account.","Choose Online rooms. Find a public campus or share a private invite code.","Golden memories earn acorn coins and gems. Explore Legacy Hall to find them.","WASD or the arrow keys move you. O shows on-screen movement buttons.","Build your own campus with F2. Start with a garden, library or hall."};
        void Awake(){opened=Time.unscaledTime;artwork=Resources.Load<Texture2D>("Presentation/LaunchCampus");if(artwork==null)artwork=Resources.Load<Texture2D>("CampusCraft/FergusonHero");}
        public void Report(string label,int completed,int total=6){Stage=label;Completed=completed;Total=total;Busy=true;Failed=false;ended=-1;}
        public void Finish(){Completed=Total;Stage="Ready to explore";Busy=false;ended=Time.unscaledTime;}
        public void Fail(){Failed=true;Busy=true;Stage="We couldn’t finish opening the campus";}
        public void Transition(string label,Action action){if(Busy)return;StartCoroutine(Perform(label,action));}
        IEnumerator Perform(string label,Action action)
        {
            Report(label,0,1);yield return null;
            try{action();Finish();}catch(Exception error){Debug.LogException(error);Fail();}
        }
        void OnGUI()
        {
            if(!Busy&&(ended<0||Time.unscaledTime-ended>.28f))return;
            var matrix=GUI.matrix;var color=GUI.color;var content=GUI.contentColor;int depth=GUI.depth;
            float scale=Mathf.Min(Screen.width/1440f,Screen.height/900f),w=Screen.width/scale,h=Screen.height/scale;
            GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);GUI.depth=-10000;GUI.color=GUI.contentColor=Color.white;
            OdysseyUI.Fill(new Rect(0,0,w,h),OdysseyUI.Navy);
            if(artwork!=null)GUI.DrawTexture(new Rect(w*.36f,0,w*.64f,h),artwork,ScaleMode.ScaleAndCrop);
            OdysseyUI.Fill(new Rect(0,0,w,h),new Color(.009f,.012f,.009f,.83f));
            for(float y=0;y<h;y+=4)OdysseyUI.Fill(new Rect(0,y,w,1),new Color(0,0,0,.12f));
            float x=70;
            OdysseyUI.Text(new Rect(x,50,800,32),"ALBION ODYSSEY  /  WORLD INITIALIZATION",18,OdysseyUI.Gold);
            OdysseyUI.Fill(new Rect(x,99,w-140,1),OdysseyUI.Gold);
            OdysseyUI.Text(new Rect(x,173,720,104),"THE NEXT CHAPTER",76,OdysseyUI.White);
            OdysseyUI.Text(new Rect(x,277,700,85),"IS YOURS TO EXPLORE.",57,OdysseyUI.Gold);
            OdysseyUI.Text(new Rect(x,399,580,65),"ALBION COLLEGE  /  MICHIGAN\nCAMPUS EXPLORATION NETWORK",19,OdysseyUI.Muted);
            float progress=Mathf.Clamp01(Completed/(float)Mathf.Max(1,Total));
            var center=new Vector2(w-260,330);float rotation=OdysseyAccessibility.ReducedMotion?0:Time.unscaledTime*.2f;
            for(int i=0;i<48;i++){float a=i*Mathf.PI/24;var c=i/48f<progress?OdysseyUI.Gold:new Color(.22f,.14f,.07f);var v=new Vector2(Mathf.Sin(a),Mathf.Cos(a));OdysseyUI.Line(center+v*117,center+v*132,c,3);}
            for(int i=0;i<6;i++){float a=i*Mathf.PI/3+rotation,b=(i+1)*Mathf.PI/3+rotation;OdysseyUI.Line(center+new Vector2(Mathf.Sin(a),Mathf.Cos(a))*82,center+new Vector2(Mathf.Sin(b),Mathf.Cos(b))*82,OdysseyUI.Mint,2);}
            OdysseyUI.Text(new Rect(center.x-30,center.y-33,120,66),Completed.ToString("00"),48,OdysseyUI.White);
            OdysseyUI.Text(new Rect(center.x-38,center.y+35,110,25),"/ "+Total.ToString("00")+" STAGES",13,OdysseyUI.Gold);
            float bottom=h-230;
            OdysseyUI.Text(new Rect(x,bottom,w-230,36),Stage.ToUpperInvariant(),23,OdysseyUI.Gold);
            if(!Failed)OdysseyUI.Spinner(new Rect(w-111,bottom,30,30));
            float width=w-140;
            for(int i=0;i<Total;i++)OdysseyUI.Fill(new Rect(x+i*width/Total,bottom+56,width/Total-5,7),i<Completed?OdysseyUI.Gold:new Color(.2f,.13f,.055f));
            string hint=Failed?"Your saved progress is preserved. Restart the game to try again.":Tips[Mathf.FloorToInt((Time.unscaledTime-opened)/7)%Tips.Length];
            OdysseyUI.Text(new Rect(x,bottom+95,width-120,60),hint,19,OdysseyUI.Muted);
            OdysseyUI.Text(new Rect(x,h-44,500,25),"FIELD TERMINAL  /  v"+Application.version,13,OdysseyUI.Gold);
            if(Failed&&OdysseyUI.Button(new Rect(w-295,bottom-9,231,48),"QUIT & RESTART","load-exit"))Application.Quit();
            GUI.matrix=matrix;GUI.color=color;GUI.contentColor=content;GUI.depth=depth;
        }
    }
}
