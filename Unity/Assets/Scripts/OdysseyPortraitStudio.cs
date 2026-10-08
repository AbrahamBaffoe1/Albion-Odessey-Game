using System;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
namespace AlbionOdyssey {
public sealed class OdysseyPortraitStudio:MonoBehaviour {
 [Serializable] class Payload {public string image,candidateId,error;public bool consent,enabled;}
 OdysseyGame game;Texture2D source,preview,current;byte[] sourceBytes;string candidate="",owner="",status="Choose a photo to create your student portrait.";int focus;bool busy,consent,enabledGeneration,serviceChecked;string serviceError="";UnityWebRequest pending;
 public Texture2D Current=>current;
 public bool IsOpen=>game!=null&&game.life.panel=="portrait";
 #if UNITY_STANDALONE_OSX && !UNITY_EDITOR
 [DllImport("PortraitPicker")] static extern IntPtr OdysseyChoosePortrait();
 [DllImport("PortraitPicker")] static extern void OdysseyFreePortraitPath(IntPtr path);
 #endif
 public void Setup(OdysseyGame g){game=g;game.accounts.ProfileChanged+=AccountChanged;}
 void AccountChanged(){if(owner==game.accounts.UserId)return;owner=game.accounts.UserId;pending?.Abort();Clear();if(game.accounts.SignedIn)StartCoroutine(Call("GET",null));}
 void Clear(){if(source!=null)Destroy(source);if(preview!=null)Destroy(preview);if(current!=null)Destroy(current);source=preview=current=null;sourceBytes=null;candidate="";consent=false;enabledGeneration=false;serviceChecked=false;serviceError="";}
 public void Open(){game.life.SetPanel("portrait");AccountChanged();if(!busy&&game.accounts.SignedIn)StartCoroutine(Call("GET",null));}
 public bool HandleInput(){if(!IsOpen)return false;
  if(AlbionUIInput.Poll(out var horizontal,out var vertical,out var choose,out var cancel)){
   if(cancel&&!busy){game.life.SetPanel("store");return true;}
   if(vertical!=0||horizontal!=0)focus=(focus+(vertical>0||horizontal<0?-1:1)+7)%7;
   if(choose&&!busy){if(focus==0)Choose();else if(focus==1&&game.accounts.SignedIn)consent=!consent;else if(focus==2){if(!game.accounts.SignedIn)game.accountPanel.Open();else if(consent&&sourceBytes!=null&&enabledGeneration)StartCoroutine(Call("POST",new Payload{image=Convert.ToBase64String(sourceBytes),consent=true}));}else if(focus==3&&preview!=null&&!string.IsNullOrEmpty(candidate))StartCoroutine(Call("PUT",new Payload{candidateId=candidate}));else if(focus==4&&current!=null)StartCoroutine(Call("DELETE",null));else if(focus==5)game.life.SetPanel("store");else if(focus==6)RefreshService();}
  }return true;
 }
 public void SelectFile(string path){
  try{var info=new FileInfo(path);if(!info.Exists||info.Length>6*1024*1024)throw new Exception();byte[] bytes=File.ReadAllBytes(path);if(bytes.Length<24)throw new Exception();
   // Decode only bounded PNG/JPEG dimensions, before allocating a texture.
   if(!PortraitImageBounds.Valid(bytes))throw new Exception();
   var image=new Texture2D(2,2);if(!image.LoadImage(bytes)){Destroy(image);throw new Exception();}if(source!=null)Destroy(source);if(preview!=null)Destroy(preview);preview=null;candidate="";source=image;sourceBytes=bytes;consent=false;status="Photo selected. Nothing has been uploaded yet.";
  }catch{status="Choose a PNG or JPEG photo between 128 and 4096 pixels, under 6 MB.";}
 }
 public static string Blocker(bool signedIn,bool working,bool checkedService,bool enabled,string error,bool photo,bool agreed){
  if(!signedIn)return "Sign in to create and save your portrait.";
  if(working)return "Please wait for the current request to finish.";
  if(!string.IsNullOrEmpty(error))return error+" Select Check connection to retry.";
  if(!checkedService)return "Check the portrait service connection before generating.";
  if(!enabled)return "Online portrait generation has not been activated. The game owner must finish the provider and spending-limit setup. Your photo stays on this Mac.";
  if(!photo)return "Choose a photo first.";
  if(!agreed)return "Tick the consent box below your photo to enable Generate portrait.";
  return "Ready to generate. Your photo is uploaded only when you select Generate portrait.";
 }
 void RefreshService(){if(!busy&&game.accounts.SignedIn)StartCoroutine(Call("GET",null));}
 void Choose(){
 #if UNITY_EDITOR
 string path=UnityEditor.EditorUtility.OpenFilePanel("Choose your photo","","png,jpg,jpeg");if(!string.IsNullOrEmpty(path))SelectFile(path);
 #elif UNITY_STANDALONE_OSX
 var ptr=OdysseyChoosePortrait();if(ptr!=IntPtr.Zero){string path=Marshal.PtrToStringAnsi(ptr);OdysseyFreePortraitPath(ptr);SelectFile(path);}
 #else
 status="Photo selection is currently available in the Mac edition.";
 #endif
 }
 IEnumerator Call(string method,Payload payload){
  if(busy||!game.accounts.SignedIn)yield break;busy=true;string requestOwner=game.accounts.UserId;
  status=method=="POST"?"Creating your portrait… this can take a minute.":"Connecting to your portrait collection…";
  using(var req=new UnityWebRequest(game.accounts.ApiUrl+"/portraits",method)){
   pending=req;req.timeout=180;req.redirectLimit=0;req.downloadHandler=new DownloadHandlerBuffer();req.SetRequestHeader("Authorization","Bearer "+game.accounts.AccessToken);
   if(payload!=null){req.uploadHandler=new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(payload)));req.SetRequestHeader("Content-Type","application/json");}
   yield return req.SendWebRequest();
   if(requestOwner==game.accounts.UserId){Payload result=null;try{result=JsonUtility.FromJson<Payload>(req.downloadHandler.text);}catch{}
    if(req.result!=UnityWebRequest.Result.Success){status=result?.error??"The portrait studio could not connect. Try again.";if(method=="GET"){enabledGeneration=false;serviceChecked=false;serviceError="Cannot reach your portrait service.";}}
    else if(method=="DELETE"){if(current!=null)Destroy(current);if(preview!=null)Destroy(preview);current=preview=null;candidate="";status="Portrait removed from your account.";}
    else if(method=="PUT"){if(current!=null)Destroy(current);current=preview;preview=null;candidate="";status="Portrait equipped on your student profile.";}
    else {if(method=="GET"){serviceChecked=result!=null;serviceError=result==null?"The portrait service returned an unreadable response.":"";}enabledGeneration=method=="GET"?result!=null&&result.enabled:enabledGeneration;
     if(method=="GET"&&string.IsNullOrEmpty(result?.image)){if(current!=null)Destroy(current);current=null;}
     if(!string.IsNullOrEmpty(result?.image)){var t=new Texture2D(2,2);bool loaded=false;try{byte[] b=Convert.FromBase64String(result.image);loaded=PortraitImageBounds.Valid(b)&&t.LoadImage(b);}catch{}
      if(loaded){if(method=="GET"){if(current!=null)Destroy(current);current=t;}else{if(preview!=null)Destroy(preview);preview=t;candidate=result.candidateId;}}else Destroy(t);
     }
     status=method=="POST"?"Your preview is ready. Use portrait to save it to your account.":enabledGeneration?"Your private portrait studio is ready.":"Photo preview is ready; online generation has not been activated yet.";
    }
   }
  }pending=null;busy=false;if(requestOwner!=game.accounts.UserId&&game.accounts.SignedIn)StartCoroutine(Call("GET",null));
 }
 void OnGUI(){if(!IsOpen)return;var old=GUI.matrix;float s=Mathf.Min(Screen.width/1440f,Screen.height/900f);GUI.matrix=Matrix4x4.Scale(Vector3.one*s);float w=Screen.width/s,h=Screen.height/s;float x=(w-1240)/2;
  OdysseyUI.Fill(new Rect(0,0,w,h),new Color(.015f,.019f,.025f));OdysseyCinematic.Atmosphere(w,h);
  OdysseyCinematic.Title(new Rect(x,40,1240,75),"The Portrait Atelier",44,new Color(.85f,.77f,.6f));
  OdysseyUI.Text(new Rect(x,118,1240,35),"YOUR PHOTO  /  YOUR IDENTITY  /  YOUR ALBION STORY",17,OdysseyUI.Muted);
  Texture2D[] images={source,preview??current};string[] labels={"01  YOUR PHOTOGRAPH","02  STUDENT PORTRAIT"};
  for(int i=0;i<2;i++){Rect r=new Rect(x+i*455,185,420,420);OdysseyUI.Frame(r,new Color(.48f,.4f,.29f));if(images[i]!=null)GUI.DrawTexture(new Rect(r.x+1,r.y+1,r.width-2,r.height-2),images[i],ScaleMode.ScaleToFit);else OdysseyCinematic.Title(new Rect(r.x,r.y+175,420,65),i==0?"Choose your photo":"Your next chapter",24,OdysseyUI.Muted);OdysseyUI.Text(new Rect(r.x,150,420,30),labels[i],16,OdysseyUI.Gold);}
  OdysseyUI.Text(new Rect(x+930,185,310,150),"A clear, naturally lit portrait, with your face and features preserved. Preview the result before you wear it.",23,OdysseyUI.White);
  OdysseyUI.Text(new Rect(x+930,365,310,190),"Your photo is sent to the game server and OpenRouter’s image provider only when you choose Generate. Originals are not saved by the game server. This creates a profile picture, not a 3D face scan.",18,OdysseyUI.Muted);
  bool before=GUI.enabled;GUI.enabled=!busy;
  if(OdysseyUI.Button(new Rect(x,630,420,48),"Choose photo…","portrait-file",focus==0))Choose();
  if(!game.accounts.SignedIn){if(OdysseyUI.Button(new Rect(x+455,630,420,48),"Sign in to create your portrait","portrait-login",focus==2))game.accountPanel.Open();}
  else {if(focus==1)OdysseyUI.Frame(new Rect(x-4,694,428,53),OdysseyUI.Gold);consent=GUI.Toggle(new Rect(x,698,420,48),consent,"This is my photo. I agree to send it for AI portrait processing.",new GUIStyle(GUI.skin.toggle){wordWrap=true,fontSize=15});GUI.enabled=!busy&&consent&&sourceBytes!=null&&enabledGeneration;
   if(OdysseyUI.Button(new Rect(x+455,630,420,48),"Generate portrait","portrait-generate",focus==2))StartCoroutine(Call("POST",new Payload{image=Convert.ToBase64String(sourceBytes),consent=true}));
   GUI.enabled=!busy&&preview!=null&&!string.IsNullOrEmpty(candidate);if(OdysseyUI.Button(new Rect(x+930,630,310,48),"Use portrait","portrait-use",focus==3))StartCoroutine(Call("PUT",new Payload{candidateId=candidate}));
   GUI.enabled=!busy&&current!=null;if(OdysseyUI.Button(new Rect(x+930,695,310,40),"Remove saved portrait","portrait-remove",focus==4))StartCoroutine(Call("DELETE",null));
  }
  GUI.enabled=before;
  string reason=Blocker(game.accounts.SignedIn,busy,serviceChecked,enabledGeneration,serviceError,sourceBytes!=null,consent);
  OdysseyUI.Text(new Rect(x+455,686,420,110),reason,17,enabledGeneration?OdysseyUI.White:OdysseyUI.Gold);
  OdysseyUI.Text(new Rect(x,754,420,65),status,17,OdysseyUI.White);
  GUI.enabled=!busy&&game.accounts.SignedIn;
  if(OdysseyUI.Button(new Rect(x+930,750,310,40),"Check connection","portrait-refresh",focus==6))RefreshService();
  GUI.enabled=!busy;
  if(OdysseyUI.Button(new Rect(x,h-65,250,42),"Back to Store","portrait-back",focus==5))game.life.SetPanel("store");GUI.enabled=before;GUI.matrix=old;
 }
 void OnDestroy(){if(game?.accounts!=null)game.accounts.ProfileChanged-=AccountChanged;pending?.Abort();Clear();}
}
public static class PortraitImageBounds {
 public static bool Valid(byte[] b){try{int w=0,h=0;if(b.Length>24&&b[0]==137&&b[1]==80&&b[2]==78&&b[3]==71){w=(b[16]<<24)|(b[17]<<16)|(b[18]<<8)|b[19];h=(b[20]<<24)|(b[21]<<16)|(b[22]<<8)|b[23];}
 else if(b.Length>4&&b[0]==255&&b[1]==216){int i=2;while(i+8<b.Length){if(b[i++]!=255)return false;while(b[i]==255)i++;int marker=b[i++];if(marker==217||marker==218)break;int len=(b[i]<<8)|b[i+1];if(len<2||i+len>b.Length)return false;if(marker==192||marker==193||marker==194){h=(b[i+3]<<8)|b[i+4];w=(b[i+5]<<8)|b[i+6];break;}i+=len;}}
 return w>=128&&h>=128&&w<=4096&&h<=4096&&(long)w*h<=16000000;}catch{return false;}}
}
}
