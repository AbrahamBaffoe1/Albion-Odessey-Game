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
        float opened,progress,fade=1;bool first=true;Texture2D artwork;
        void Awake(){opened=Time.unscaledTime;artwork=Resources.Load<Texture2D>("Presentation/OdysseyKeyArt");}
        public void Report(string label,int completed,int total=6){Stage=label;Completed=completed;Total=total;Busy=true;Failed=false;fade=1;}
        public void Finish(){Completed=Total;Stage="Your next chapter awaits";StartCoroutine(Reveal());}
        IEnumerator Reveal()
        {
            float until=first&&!PlaytestMode.Active&&!OdysseyAccessibility.ReducedMotion?Mathf.Max(opened+4.5f,Time.unscaledTime+.6f):Time.unscaledTime+.2f;
            while(Time.unscaledTime<until)yield return null;
            while(fade>0){fade-=Time.unscaledDeltaTime/(OdysseyAccessibility.ReducedMotion?.05f:.8f);yield return null;}
            Busy=false;first=false;
        }
        void Update(){progress=Mathf.MoveTowards(progress,Completed/(float)Mathf.Max(1,Total),Time.unscaledDeltaTime*.55f);}
        public void Fail(){Failed=true;Busy=true;fade=1;Stage="We couldn’t finish opening the campus";}
        public void Transition(string label,Action action){if(Busy)return;progress=0;StartCoroutine(Perform(label,action));}
        IEnumerator Perform(string label,Action action)
        {
            Report(label,0,1);yield return null;
            try{action();Finish();}catch(Exception error){Debug.LogException(error);Fail();}
        }
        void Fill(Rect rect,Color color){color.a*=Mathf.Clamp01(fade);OdysseyUI.Fill(rect,color);}
        void OnGUI()
        {
            if(!Busy)return;
            var matrix=GUI.matrix;var color=GUI.color;int depth=GUI.depth;
            float scale=Mathf.Min(Screen.width/1440f,Screen.height/900f),w=Screen.width/scale,h=Screen.height/scale;
            GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);GUI.depth=-10000;GUI.color=new Color(1,1,1,Mathf.Clamp01(fade));
            Fill(new Rect(0,0,w,h),new Color(.1f,.12f,.14f));
            if(artwork!=null)GUI.DrawTexture(new Rect(0,0,w,h),artwork,ScaleMode.ScaleAndCrop);
            OdysseyCinematic.Title(new Rect(w*.04f,h*.35f,w*.52f,96),"ALBION",84,new Color(.075f,.085f,.095f));
            OdysseyCinematic.Title(new Rect(w*.04f,h*.35f+91,w*.52f,66),"O D Y S S E Y",39,new Color(.075f,.085f,.095f));
            Fill(new Rect(w*.09f,h*.35f+165,w*.42f,1),new Color(.16f,.18f,.19f,.7f));
            OdysseyUI.Text(new Rect(w*.1f,h*.35f+184,w*.43f,30),"A CAMPUS. A WILDERNESS. YOUR STORY.",17,new Color(.15f,.17f,.18f));
            Fill(new Rect(0,h-144,w,144),new Color(.013f,.02f,.03f,.94f));
            OdysseyUI.Text(new Rect(64,h-112,w-195,30),Stage.ToUpperInvariant(),18,OdysseyUI.White);
            if(!Failed)OdysseyUI.Spinner(new Rect(w-98,h-113,26,26));
            Fill(new Rect(64,h-62,w-128,2),new Color(.23f,.25f,.28f));
            Fill(new Rect(64,h-62,(w-128)*progress,2),OdysseyUI.White);
            OdysseyUI.Text(new Rect(64,h-44,w-140,26),Failed?"Saved progress is preserved. Restart the game to try again.":"ALBION ODYSSEY   /   "+Completed+" OF "+Total+" STAGES",12,OdysseyUI.Muted);
            if(Failed&&OdysseyUI.Button(new Rect(w-320,h-120,250,46),"QUIT & RESTART","load-exit"))Application.Quit();
            GUI.matrix=matrix;GUI.color=color;GUI.depth=depth;
        }
    }
}
