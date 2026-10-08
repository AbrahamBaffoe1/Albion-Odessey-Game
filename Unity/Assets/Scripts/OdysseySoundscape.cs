using System.Collections.Generic;
using UnityEngine;
namespace AlbionOdyssey {
 // Original, seamlessly looped tonal beds. No external recordings or paid service.
 public sealed class OdysseySoundscape:MonoBehaviour {
  OdysseyGame game;readonly Dictionary<string,AudioSource> beds=new Dictionary<string,AudioSource>();readonly List<AudioClip> owned=new List<AudioClip>();AudioSource effects;AudioClip impact,navigate;
  public void Setup(OdysseyGame owner){game=owner;effects=Source();navigate=Tone("Navigation",.09f,740,false);impact=Tone("Meteor impact",1.4f,90,true);
   Add("launch",110,165,220);Add("campus",196,247,294);Add("forestrun",146,220,293);Add("store",220,277,330);Add("telescope",130,196,261);Add("fieldguide",174,220,261);
  }
  AudioSource Source(){var s=gameObject.AddComponent<AudioSource>();s.playOnAwake=false;s.spatialBlend=0;s.priority=180;s.volume=0;return s;}
  AudioClip Keep(string name,float[] data,int rate){var clip=AudioClip.Create(name,data.Length,1,rate,false);clip.SetData(data,0);owned.Add(clip);return clip;}
  void Add(string name,float a,float b,float c){const int rate=22050,seconds=12;var data=new float[rate*seconds];for(int i=0;i<data.Length;i++){float t=i/(float)rate;float swell=.65f+.35f*Mathf.Cos(2*Mathf.PI*t/seconds);float v=(Mathf.Sin(t*a*2*Mathf.PI)+.5f*Mathf.Sin(t*b*2*Mathf.PI)+.3f*Mathf.Sin(t*c*2*Mathf.PI))*.075f*swell;
    if(name=="forestrun")v+=Mathf.Sin(t*73*2*Mathf.PI)*Mathf.Exp(-(t% .5f)*22)*.13f;
    data[i]=v;}var source=Source();source.clip=Keep("Odyssey "+name,data,rate);source.loop=true;beds.Add(name,source);}
  AudioClip Tone(string name,float length,float hz,bool noise){const int rate=22050;var data=new float[(int)(rate*length)];var random=new System.Random(1884);float low=0;for(int i=0;i<data.Length;i++){float t=i/(float)rate;low=Mathf.Lerp(low,(float)random.NextDouble()*2-1,.1f);data[i]=(Mathf.Sin(2*Mathf.PI*hz*t)*.2f+(noise?low*.45f:0))*Mathf.Exp(-t*(noise?5:45))*Mathf.Min(1,t*500);}return Keep(name,data,rate);}
  void Update(){if(game==null||!game.Ready||game.sound==null)return;string panel=game.life.panel;string phase=string.IsNullOrEmpty(panel)?"campus":panel;float volume=game.sound.Muted||!Application.isFocused||game.loading.Busy?0:game.sound.Volume;
   foreach(var pair in beds){var s=pair.Value;s.mute=volume==0;float target=pair.Key==phase?volume*.32f:0;s.volume=Mathf.MoveTowards(s.volume,target,Time.unscaledDeltaTime*.2f);if(target>0&&!s.isPlaying)s.Play();if(target==0&&s.volume<=0&&s.isPlaying)s.Stop();}effects.volume=volume;effects.mute=volume==0;
  }
  public void Navigate(){if(game.sound!=null&&!game.sound.Muted)effects.PlayOneShot(navigate,.35f);}
  public void Impact(){if(game.sound!=null&&!game.sound.Muted)effects.PlayOneShot(impact,.55f);}
  void OnDestroy(){foreach(var clip in owned)Destroy(clip);}
 }
}
