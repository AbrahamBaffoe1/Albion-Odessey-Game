using System;
using System.Collections;
using UnityEngine;

namespace AlbionOdyssey
{
    public sealed class OdysseyLoading : MonoBehaviour
    {
        public bool Busy {get;private set;}=true;
        public string Stage {get;private set;}="Opening Albion Odyssey";
        public int Completed {get;private set;}
        public int Total {get;private set;}=6;
        public bool Failed {get;private set;}
        float ended=-1;
        public void Report(string label,int completed,int total=6){Stage=label;Completed=completed;Total=total;Busy=true;Failed=false;ended=-1;}
        public void Finish(){Completed=Total;Stage="Ready to explore";Busy=false;ended=Time.unscaledTime;}
        public void Fail(){Failed=true;Busy=true;Stage="Campus preparation was interrupted";}
        public void Transition(string label,Action action){if(Busy)return;StartCoroutine(Perform(label,action));}
        IEnumerator Perform(string label,Action action)
        {
            Report(label,0,1);yield return null;
            try{action();Finish();}catch(Exception error){Debug.LogException(error);Fail();}
        }
        void OnGUI()
        {
            if(!Busy&&(ended<0||Time.unscaledTime-ended>.22f))return;
            var matrix=GUI.matrix;var color=GUI.color;var content=GUI.contentColor;int depth=GUI.depth;
            float scale=Mathf.Min(Screen.width/1280f,Screen.height/800f),w=Screen.width/scale,h=Screen.height/scale;
            GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));GUI.depth=-10000;GUI.color=GUI.contentColor=Color.white;
            OdysseyUI.Fill(new Rect(0,0,w,h),OdysseyUI.Navy);
            float x=(w-1100)*.5f;
            OdysseyUI.Text(new Rect(x,62,1000,32),"ALBION  /  ODYSSEY",24,OdysseyUI.White,true);
            OdysseyUI.Text(new Rect(x,210,670,210),"A campus full\nof possibilities.",58,OdysseyUI.White,true);
            OdysseyUI.Text(new Rect(x,455,570,80),"Explore its stories. Meet its people.\nBuild something that’s yours.",23,OdysseyUI.Muted);
            if(OdysseyPresentation.Instance!=null)OdysseyPresentation.Instance.DrawStudent(new Rect(w-510,76,420,560));
            else
            {
                // Original campus doorway motif remains cheap enough to render before assets load.
                for(int i=0;i<4;i++){float width=230-i*44;OdysseyUI.Card(new Rect(w-365-width*.5f,230+i*25,width,300-i*36),i%2==0?new Color(.06f,.13f,.19f):OdysseyUI.Navy);}
                OdysseyUI.Spinner(new Rect(w-400,340,70,70));
            }
            OdysseyUI.Text(new Rect(x,h-150,980,35),Stage,22,OdysseyUI.White,true);
            OdysseyUI.Card(new Rect(x,h-89,1100,7),new Color(.12f,.19f,.26f));
            OdysseyUI.Card(new Rect(x,h-89,1100*Mathf.Clamp01(Completed/(float)Total),7),OdysseyUI.Mint);
            OdysseyUI.Text(new Rect(x,h-63,900,30),Failed?"Please restart the game. Your existing campus save is preserved.":Completed+" / "+Total+" preparation steps complete",15,OdysseyUI.Muted);
            if(Failed&&OdysseyUI.Button(new Rect(w-330,h-158,235,56),"Quit game","load-exit"))Application.Quit();
            GUI.matrix=matrix;GUI.color=color;GUI.contentColor=content;GUI.depth=depth;
        }
    }
}
