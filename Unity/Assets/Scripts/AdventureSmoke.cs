using System;
using System.Collections;
using System.IO;
using UnityEngine;
namespace AlbionOdyssey {
public sealed class AdventureSmoke:MonoBehaviour {
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Boot(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-adventureSmoke")>=0)new GameObject("Adventure acceptance").AddComponent<AdventureSmoke>();}
 void Check(bool ok,string why){if(!ok)throw new Exception(why);}
 IEnumerator Start(){var run=Run();while(true){object current;try{if(!run.MoveNext())break;current=run.Current;}catch(Exception ex){Debug.LogError("ADVENTURE_SMOKE_FAILED "+ex);Application.Quit(1);yield break;}yield return current;}Debug.Log("ADVENTURE_SMOKE_OK");Application.Quit(0);}
 IEnumerator Run(){OdysseyGame g=null;float end=Time.realtimeSinceStartup+140;while(g==null||!g.Ready||g.loading.Busy){g=FindAnyObjectByType<OdysseyGame>();Check(Time.realtimeSinceStartup<end,"startup timeout");yield return null;}
  for(int i=0;i<3;i++)Check(g.activities.Join(i),"join club "+i);
  var a=g.adventure;a.Open();Check(!g.player.controls,"opening journal releases movement");yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot("/tmp/albion-adventure-intro.png");yield return new WaitForSecondsRealtime(.3f);
  Check(a.Advance()&&!a.Advance(),"intro accepted, objective enforced");
  for(int i=0;i<3;i++)g.state.Collect(i);g.life.SetPanel("");g.player.Teleport(new Vector3(0,.05f,-22));g.SetJournal(true);Check(g.activities.Submit(0,1),"archive assignment");g.SetJournal(false);Check(a.Advance(),"memories connect to observatory");a.Travel();Check(Vector3.Distance(g.player.transform.position,CampusExpansion.Find("5").position)<50,"observatory arrival");
  g.player.Teleport(g.destinations.TelescopePosition+new Vector3(-1,0,-1));g.destinations.OpenTelescope();Check(a.Progress.observed==0,"looking alone does not record");
  var aim=typeof(OdysseyDestinations).GetMethod("Aim",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
  for(int i=0;i<3;i++){aim.Invoke(g.destinations,new object[]{i});g.destinations.RecordObservation();}
  Check(g.activities.Submit(1,0),"observatory assignment");g.destinations.CloseTelescope();a.Open();Check(a.Advance(),"recorded telescope observations");a.Travel();yield return null;Check(g.life.panel=="arrival","nature arrival cutscene");g.destinations.FinishArrival();Check(a.Progress.arrived,"arrival checkpoint");
  g.player.Teleport(g.player.transform.position+new Vector3(5,0,8));g.life.SetPanel("fieldguide");for(int i=0;i<3;i++)Check(g.destinations.RecordHabitat(i),"record habitat "+i);
  Check(g.activities.Submit(2,0),"nature assignment");g.life.SetPanel("activities");yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot("/tmp/albion-campus-activities.png");yield return new WaitForSecondsRealtime(.3f);a.Open();Check(a.Advance()&&!a.Advance(),"nature objectives and return requirement");a.Travel();Check(a.AtHome&&!g.environment.Locked,"return restores campus atmosphere");a.Open();Check(a.Advance()&&!a.Advance(),"ending once");
  yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot("/tmp/albion-adventure-ending.png");yield return new WaitForSecondsRealtime(.5f);
  var saved=JsonUtility.FromJson<OdysseyState>(File.ReadAllText(Path.Combine(Application.persistentDataPath,"Playtests",PlaytestMode.Name,"save.json")));Check(saved.Valid()&&saved.Current.adventure.chapter==5&&saved.Current.activities.completed[0]==1&&saved.Current.activities.completed[1]==1&&saved.Current.activities.completed[2]==1,"saved ending roundtrip");
  g.state=saved;a.Open();Check(a.Progress.chapter==5,"resume completed story");
 }
}}
