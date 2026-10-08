using System;
using System.Collections;
using System.IO;
using UnityEngine;
namespace AlbionOdyssey
{
 public sealed class StoreSmoke:MonoBehaviour
 {
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Boot(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-storeSmoke")>=0)new GameObject("Store validation").AddComponent<StoreSmoke>();}
  IEnumerator Start(){Application.runInBackground=true;
   if(Array.IndexOf(Environment.GetCommandLineArgs(),"-storeReview")>=0){OdysseyGame g=null;while(g==null||!g.Ready||g.loading.Busy){g=FindAnyObjectByType<OdysseyGame>();yield return null;}g.shell.ShowLaunch();yield break;}
   var test=Check();while(true){object item;try{if(!test.MoveNext())break;item=test.Current;}catch(Exception e){Debug.LogError("STORE_SMOKE_FAILED: "+e);Application.Quit(1);yield break;}yield return item;}Debug.Log("STORE_SMOKE_OK: preview, purchase, equip, save/reload, rollback, title and pause return");Application.Quit(0);}
  void Require(bool value,string reason){if(!value)throw new Exception(reason);}
  void Capture(string name)=>ScreenCapture.CaptureScreenshot(Path.Combine(Application.dataPath,"../../"+name+".png"));
  IEnumerator Check()
  {
   OdysseyGame game=null;float deadline=Time.realtimeSinceStartup+90;
   while(game==null||!game.Ready){game=FindAnyObjectByType<OdysseyGame>();Require(Time.realtimeSinceStartup<deadline,"startup timeout");yield return null;}
   game.shell.ShowLaunch();yield return new WaitForEndOfFrame();Capture("Store-loading");
   while(game.loading.Busy)yield return null;
   yield return new WaitForSecondsRealtime(.3f);yield return new WaitForEndOfFrame();Capture("Store-title");
   game.store.Open();Require(!game.player.controls&&game.store.IsOpen,"store must stop player");
   yield return new WaitForSecondsRealtime(.6f);yield return new WaitForEndOfFrame();Capture("Store-catalog");
   game.store.Navigate(0,0,true,false);Require(game.store.Confirming&&game.state.Current.acorns==6,"confirmation before payment");
   yield return new WaitForEndOfFrame();Capture("Store-confirmation");
   game.store.Navigate(0,0,false,true);Require(!game.store.Confirming&&game.store.IsOpen&&game.state.Current.acorns==6,"cancel retains balance and store");
   game.store.Navigate(0,0,true,false);game.store.Navigate(0,0,true,false);Require(game.state.Current.OwnsFinish(1)&&game.state.Current.acorns==2,"confirmed purchase");Require(!game.store.Purchase(1)&&game.state.Current.acorns==2,"no duplicate charge");Require(!game.store.Purchase(6),"insufficient funds");
   Require(game.store.Equip(1)&&game.state.Current.equippedFinish==1,"equip");
   var saved=JsonUtility.FromJson<OdysseyState>(File.ReadAllText(Path.Combine(Application.persistentDataPath,"Playtests",PlaytestMode.Name,"save.json")));Require(saved.Valid()&&saved.Current.OwnsFinish(1)&&saved.Current.equippedFinish==1,"reload ownership");
   yield return new WaitForEndOfFrame();Capture("Store-equipped");
   Require(game.store.Equip(0),"restore original");
   // Invalid unrelated data forces Save to fail: ownership and wallet must roll back.
   game.state.Collect(0);game.state.Collect(1);int balance=game.state.Current.acorns,owned=game.state.Current.ownedFinishes;game.state.beacon=1;
   Require(!game.store.Purchase(2)&&game.state.Current.acorns==balance&&game.state.Current.ownedFinishes==owned,"save failure purchase rollback");Require(!game.store.Equip(1)&&game.state.Current.equippedFinish==0,"save failure equip rollback");game.state.beacon=0;
   Require(game.state.Valid(),"valid after rollback");game.store.Close();Require(game.life.panel=="launch","title return");game.shell.OpenPause();game.shell.ActivatePause(10);Require(game.store.IsOpen,"pause store route");game.store.Close();Require(game.shell.IsPaused,"pause return");
  }
 }
}
