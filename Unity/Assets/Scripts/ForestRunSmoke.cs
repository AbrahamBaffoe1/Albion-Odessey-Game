using System;
using System.Collections;
using System.IO;
using UnityEngine;
namespace AlbionOdyssey
{
 public sealed class ForestRunSmoke:MonoBehaviour
 {
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Boot(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-forestSmoke")>=0)new GameObject("Forest visual verification").AddComponent<ForestRunSmoke>();}
  IEnumerator Start(){Application.runInBackground=true;OdysseyGame game=null;float deadline=Time.realtimeSinceStartup+60;while(game==null||!game.Ready){game=FindAnyObjectByType<OdysseyGame>();if(Time.realtimeSinceStartup>deadline){Application.Quit(1);yield break;}yield return null;}
   var run=game.GetComponent<CampusForestRun>();var state=new ForestSnapshot{seed=12,phase="running",length=1200,seeds=4,treasures=7,rescues=1,players=new[]{new ForestRunner{id="visual-a",display="Your runner",lane=0,distance=100,gap=18,pose="run"},new ForestRunner{id="visual-b",display="Teammate",lane=-1,distance=109,gap=20,pose="run"}},events=new[]{new ForestEvent{id=1,z=119,lane=0,kind="log"},new ForestEvent{id=2,z=130,lane=-1,kind="seed"},new ForestEvent{id=3,z=145,lane=1,kind="treasure"},new ForestEvent{id=4,z=156,lane=-1,kind="branch"},new ForestEvent{id=5,z=173,lane=0,kind="rock"}}};
   var campusSun=RenderSettings.sun;bool campusSunEnabled=campusSun!=null&&campusSun.enabled;bool oldFog=RenderSettings.fog;float oldDensity=RenderSettings.fogDensity;
   run.PreviewForSmoke(state,"visual-a");yield return new WaitForSecondsRealtime(3);string output=Environment.GetEnvironmentVariable("FOREST_OUTPUT");Directory.CreateDirectory(output);yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,"Forest-treasure-run.png"));yield return new WaitForSecondsRealtime(1);bool locked=!game.player.controls&&game.life.panel=="forestrun"&&game.environment.Locked;
   state.players[0].distance=421;state.players[1].distance=430;run.Accept(state);yield return new WaitForSecondsRealtime(2);yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,"Forest-after-two-chunks.png"));yield return null;locked=locked&&GroundCoversCamera();
   run.Close();bool restored=game.player.controls&&game.life.panel==""&&!game.environment.Locked&&RenderSettings.sun==campusSun&&(!campusSun||campusSun.enabled==campusSunEnabled)&&RenderSettings.fog==oldFog&&Mathf.Approximately(RenderSettings.fogDensity,oldDensity);File.WriteAllText(Path.Combine(output,"visual-check.json"),JsonUtility.ToJson(new Result{passed=locked&&restored,controlsLockedDuringRun=locked,controlsRestoredAfterExit=restored}));Application.Quit(locked&&restored?0:1);
  }
  static bool GroundCoversCamera()
  {
   var camera=GameObject.Find("Treasure run camera");if(camera==null)return false;
   float min=float.PositiveInfinity,max=float.NegativeInfinity;
   foreach(var renderer in camera.transform.parent.GetComponentsInChildren<MeshRenderer>())if(renderer.sharedMaterial!=null&&renderer.sharedMaterial.name=="Nature trail earth")
   {min=Mathf.Min(min,renderer.bounds.min.z);max=Mathf.Max(max,renderer.bounds.max.z);}
   return min<=camera.transform.position.z&&max>=camera.transform.position.z+170f;
  }
  [Serializable]class Result{public bool passed,controlsLockedDuringRun,controlsRestoredAfterExit;public string scope="Rendered fixture only; not live authentication or multiplayer verification";}
 }
}
