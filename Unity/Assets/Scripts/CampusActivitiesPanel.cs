using System;
using UnityEngine;
namespace AlbionOdyssey {
 public sealed class CampusActivitiesPanel:MonoBehaviour {
  OdysseyGame game;int focus;string feedback="";
  public CampusActivities State=>game.state.Current.activities;
  public static int Today=>(int)(DateTime.UtcNow.Date-new DateTime(1970,1,1)).TotalDays;
  public void Setup(OdysseyGame owner){game=owner;}
  public void Record(int club){int old=State.evidence;if(State.Record(club,Today)&&!game.Save())State.evidence=old;}
  public bool Join(int club){int old=State.clubs;if(!State.Join(club))return false;if(game.Save())return true;State.clubs=old;return false;}
  public bool Submit(int club,int answer){bool ok=State.Submit(club,answer,Today,game.Save);feedback=ok?"Assignment saved. Your club record has grown. Return tomorrow for another field session.":"Read the question and finish today's field activity first. You can retry an incorrect answer.";return ok;}
  public bool HandleInput(){if(game.life.panel=="activities"){
   if(AlbionUIInput.Poll(out var horizontal,out var vertical,out var choose,out var cancel)){
    if(cancel)game.life.SetPanel("");else{if(horizontal!=0||vertical!=0)focus=(focus+(horizontal>0||vertical<0?1:10))%11;
     if(choose){if(focus==9)game.life.SetPanel("");else if(focus==10)game.life.SetPanel("courses");else{int club=focus/3;if((State.clubs&(1<<club))==0)Join(club);else if(focus%3!=0)Submit(club,focus%3-1);}}}
   }
   if(Input.GetKeyDown(KeyCode.K))game.life.SetPanel("courses");return true;
  }if(!game.life.PanelOpen&&!game.journalOpen&&Input.GetKeyDown(KeyCode.F7)){focus=0;game.life.SetPanel("activities");return true;}return false;}

  void OnGUI(){if(game==null||!game.Ready||game.life.panel!="activities")return;var old=GUI.matrix;float scale=Mathf.Min(Screen.width/1440f,Screen.height/900f);GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);float w=Screen.width/scale,h=Screen.height/scale;ConsoleMenuStyle.Background(game,w,h);
   OdysseyCinematic.Title(new Rect(70,35,w-140,60),"Your campus life",38,OdysseyUI.Gold);
   OdysseyUI.Text(new Rect(70,100,w-140,65),"Original game clubs · self-paced daily fieldwork · resets at 00:00 UTC\nJoin a club, visit its location, record your work, then submit your reflection.",20,OdysseyUI.White);
   float cw=(w-180)/3;
   for(int i=0;i<3;i++){float x=70+i*(cw+20);OdysseyUI.Fill(new Rect(x,195,cw,470),new Color(.035f,.025f,.1f,.85f));OdysseyCinematic.Title(new Rect(x+15,210,cw-30,45),CampusActivities.Names[i],27,OdysseyUI.Gold);OdysseyUI.Text(new Rect(x+15,270,cw-30,115),CampusActivities.Tasks[i],20,OdysseyUI.White);
    if((State.clubs&(1<<i))==0){if(OdysseyUI.Button(new Rect(x+15,415,cw-30,48),"Join club","club-join-"+i,focus/3==i))Join(i);continue;}
    OdysseyUI.Text(new Rect(x+15,380,cw-30,60),State.Badge(i)+" · "+State.completed[i]+" assignments\n"+(State.lastDay[i]>=Today?"Today's session complete":(State.evidence&(1<<i))!=0?"Fieldwork recorded":"Fieldwork still needed"),18,OdysseyUI.White);
    OdysseyUI.Text(new Rect(x+15,455,cw-30,45),CampusActivities.Questions[i],19,OdysseyUI.White);
    for(int n=0;n<2;n++)if(OdysseyUI.Button(new Rect(x+15,510+n*62,cw-30,52),CampusActivities.Answers[i][n],"club-answer-"+i+"-"+n,focus==i*3+n+1))Submit(i,n);
   }
   OdysseyUI.Text(new Rect(70,690,w-140,70),feedback,20,OdysseyUI.White);
   if(OdysseyUI.Button(new Rect(430,h-95,320,48),"Course timetable · K","activities-courses",focus==10))game.life.SetPanel("courses");
   if(OdysseyUI.Button(new Rect(70,h-95,320,48),"Return · Esc","activities-close",focus==9))game.life.SetPanel("");GUI.matrix=old;
  }
 }
}
