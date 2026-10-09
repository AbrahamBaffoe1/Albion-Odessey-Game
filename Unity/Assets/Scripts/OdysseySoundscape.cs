using System.Collections.Generic;
using UnityEngine;
namespace AlbionOdyssey {
 // User-supplied music with phase crossfades; original forest and interface effects.
 public sealed class OdysseySoundscape:MonoBehaviour {
  OdysseyGame game;readonly Dictionary<string,AudioSource> beds=new Dictionary<string,AudioSource>();readonly Dictionary<string,float> gains=new Dictionary<string,float>();readonly List<AudioClip> owned=new List<AudioClip>();AudioSource effects;AudioClip impact,navigate;
  public void Setup(OdysseyGame owner){game=owner;effects=Source();navigate=Tone("Navigation",.09f,740,false);impact=Tone("Meteor impact",1.4f,90,true);
   Music("future","WhenTheFutureWhispers");Music("beauty","TheBeautyOfYourSoul");Add("forestrun",146,220,293);
  }
  void Music(string key,string asset){var clip=Resources.Load<AudioClip>("Audio/Music/"+asset);if(clip==null){Debug.LogError("Missing soundtrack: "+asset);return;}var source=Source();source.clip=clip;source.loop=true;beds.Add(key,source);gains.Add(key,0);}
  public static string Phase(string panel){switch(panel){case "launch":case "telescope":return "future";case "":case "store":case "portrait":case "fieldguide":case "arrival":return "beauty";case "forestrun":return "forestrun";default:return "";}}
  AudioSource Source(){var s=gameObject.AddComponent<AudioSource>();s.playOnAwake=false;s.spatialBlend=0;s.priority=180;s.volume=0;return s;}
  AudioClip Keep(string name,float[] data,int rate){var clip=AudioClip.Create(name,data.Length,1,rate,false);clip.SetData(data,0);owned.Add(clip);return clip;}
  void Add(string name,float a,float b,float c){const int rate=22050,seconds=12;var data=new float[rate*seconds];for(int i=0;i<data.Length;i++){float t=i/(float)rate;float swell=.65f+.35f*Mathf.Cos(2*Mathf.PI*t/seconds);float v=(Mathf.Sin(t*a*2*Mathf.PI)+.5f*Mathf.Sin(t*b*2*Mathf.PI)+.3f*Mathf.Sin(t*c*2*Mathf.PI))*.075f*swell;
    if(name=="forestrun")v+=Mathf.Sin(t*73*2*Mathf.PI)*Mathf.Exp(-(t% .5f)*22)*.13f;
    data[i]=v;}var source=Source();source.clip=Keep("Odyssey "+name,data,rate);source.loop=true;beds.Add(name,source);gains.Add(name,0);}
  AudioClip Tone(string name,float length,float hz,bool noise){const int rate=22050;var data=new float[(int)(rate*length)];var random=new System.Random(1884);float low=0;for(int i=0;i<data.Length;i++){float t=i/(float)rate;low=Mathf.Lerp(low,(float)random.NextDouble()*2-1,.1f);data[i]=(Mathf.Sin(2*Mathf.PI*hz*t)*.2f+(noise?low*.45f:0))*Mathf.Exp(-t*(noise?5:45))*Mathf.Min(1,t*500);}return Keep(name,data,rate);}
  void Update(){if(game==null||!game.Ready||game.sound==null)return;string panel=game.life.panel;string phase=Phase(panel);float volume=game.sound.Muted||!Application.isFocused||game.loading.Busy?0:game.sound.Volume;
   foreach(var pair in beds){var s=pair.Value;s.mute=volume==0;float envelope=pair.Key=="forestrun"?1:Mathf.Min(Mathf.Clamp01(s.time/2f),Mathf.Clamp01((s.clip.length-s.time)/3f));float target=pair.Key==phase?volume*.32f:0;gains[pair.Key]=Mathf.MoveTowards(gains[pair.Key],target,Time.unscaledDeltaTime*.12f);s.volume=gains[pair.Key]*envelope;if(target>0&&!s.isPlaying)s.Play();if(target==0&&gains[pair.Key]<=0&&s.isPlaying)s.Stop();}effects.volume=volume;effects.mute=volume==0;
  }
  public void Navigate(){if(game.sound!=null&&!game.sound.Muted)effects.PlayOneShot(navigate,.35f);}
  public void Impact(){if(game.sound!=null&&!game.sound.Muted)effects.PlayOneShot(impact,.55f);}
  void OnDestroy(){foreach(var clip in owned)Destroy(clip);}
 }
}
