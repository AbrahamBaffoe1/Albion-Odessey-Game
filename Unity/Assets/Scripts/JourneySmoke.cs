using System;
using System.Collections;
using System.IO;
using UnityEngine;
namespace AlbionOdyssey {
public sealed class JourneySmoke:MonoBehaviour {
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Boot(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-journeySmoke")>=0)new GameObject("Journey tests").AddComponent<JourneySmoke>();}
 void Check(bool ok,string reason){if(!ok)throw new Exception(reason);}
 void Capture(string name)=>ScreenCapture.CaptureScreenshot(Path.Combine(Application.dataPath,"../../Journey-"+name+".png"));
 IEnumerator Start(){Application.runInBackground=true;var test=Run();while(true){object next;try{if(!test.MoveNext())break;next=test.Current;}catch(Exception e){Debug.LogError("JOURNEY_SMOKE_FAILED "+e);Application.Quit(1);yield break;}yield return next;}Debug.Log("JOURNEY_SMOKE_OK");Application.Quit(0);}
 IEnumerator Run(){OdysseyGame g=null;float deadline=Time.realtimeSinceStartup+100;while(g==null||!g.Ready||g.loading.Busy){g=FindAnyObjectByType<OdysseyGame>();Check(Time.realtimeSinceStartup<deadline,"startup timeout");yield return null;}
  var intro=g.gameObject.AddComponent<AcornAwakening>();StartCoroutine(intro.Play(true));yield return new WaitForSecondsRealtime(2.3f);Check(intro.Active,"acorn reveal active");yield return new WaitForEndOfFrame();Capture("Acorn");while(intro.Active)yield return null;Destroy(intro);
  g.shell.Play();g.player.Teleport(CampusExpansion.Find("5").position+new Vector3(0,.2f,-17));g.player.transform.rotation=Quaternion.identity;yield return new WaitForSecondsRealtime(1);yield return new WaitForEndOfFrame();Capture("Observatory");
  Check(g.destinations.Observatory!=null,"building created");Physics.SyncTransforms();var pos=CampusExpansion.Find("5").position;Check(!Physics.Raycast(pos+new Vector3(0,1,-8),Vector3.forward,2),"entry must be open");Check(Physics.Raycast(pos+new Vector3(5,1,-8),Vector3.forward,2),"front wall must collide");
  g.player.Teleport(g.destinations.TelescopePosition+new Vector3(-1,0,-1));g.destinations.OpenTelescope();yield return new WaitForSecondsRealtime(.3f);Check(!g.player.controls,"scope stops movement");yield return new WaitForEndOfFrame();Capture("Telescope");g.destinations.CloseTelescope();Check(g.player.controls,"scope returns controls");
  g.destinations.ArriveNature();yield return new WaitForSecondsRealtime(2);yield return new WaitForEndOfFrame();Capture("Arrival");g.destinations.FinishArrival();Check(g.player.controls,"arrival returns movement");Check(g.player.transform.position.x>5900,"nature center location");
  g.life.SetPanel("fieldguide");yield return new WaitForEndOfFrame();Capture("Journal");g.portraits.Open();yield return new WaitForEndOfFrame();Capture("Portraits");Check(!g.player.controls,"studio stops movement");
  Check(!PortraitImageBounds.Valid(new byte[]{137,80,78,71}),"reject truncated photo");g.store.Open();yield return new WaitForEndOfFrame();Capture("Store");
 }
}}
