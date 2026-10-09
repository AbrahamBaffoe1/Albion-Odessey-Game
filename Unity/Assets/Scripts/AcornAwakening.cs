using System.Collections;
using UnityEngine;
namespace AlbionOdyssey {
public sealed class AcornAwakening:MonoBehaviour {
 Texture2D emblem;AudioSource audioSource;AudioClip thunder;float began;bool active;
 public bool Active=>active;
 public IEnumerator Play(bool smokePreview=false){emblem=Resources.Load<Texture2D>("Presentation/AcornMedallion");if(emblem==null||(OdysseyAccessibility.ReducedMotion&&!(smokePreview&&PlaytestMode.Active)))yield break;began=Time.unscaledTime;active=true;
  while(active&&Time.unscaledTime-began<4.4f){if(Time.unscaledTime-began>1.05f&&audioSource==null)Impact();yield return null;}active=false;if(audioSource!=null)audioSource.Stop();
 }
 void Update(){if(active&&(Input.GetKeyDown(KeyCode.Escape)||Input.GetKeyDown(KeyCode.Space)||Input.GetKeyDown(KeyCode.Return)))active=false;}
 void Impact(){audioSource=gameObject.AddComponent<AudioSource>();audioSource.spatialBlend=0;var settings=FindAnyObjectByType<OdysseyAudio>();audioSource.volume=settings==null||settings.Muted?0:.28f*settings.Volume;int rate=22050;float[] samples=new float[rate*3];var rng=new System.Random(1884);float low=0;
  for(int i=0;i<samples.Length;i++){float t=i/(float)rate;low=Mathf.Lerp(low,(float)rng.NextDouble()*2-1,.06f);samples[i]=Mathf.Clamp((low*.8f+Mathf.Sin(t*2*Mathf.PI*(48-t*7))*.32f)*Mathf.Exp(-t*1.8f),-.8f,.8f);}thunder=AudioClip.Create("Original acorn impact and thunder",samples.Length,1,rate,false);thunder.SetData(samples,0);audioSource.PlayOneShot(thunder);
 }
 static void Line(Vector2 a,Vector2 b,Color color,float thickness){var old=GUI.matrix;GUIUtility.RotateAroundPivot(Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg,a);OdysseyUI.Fill(new Rect(a.x,a.y,Vector2.Distance(a,b),thickness),color);GUI.matrix=old;}
 void OnGUI(){if(!active)return;var matrix=GUI.matrix;var tint=GUI.color;GUI.depth=-11000;float s=Mathf.Min(Screen.width/1440f,Screen.height/900f);GUI.matrix=Matrix4x4.Scale(Vector3.one*s);float w=Screen.width/s,h=Screen.height/s,t=Time.unscaledTime-began;
  OdysseyUI.Fill(new Rect(0,0,w,h),new Color(.005f,.009f,.018f));OdysseyCinematic.Atmosphere(w,h);
  float drop=Mathf.Clamp01(t/1.05f),impact=Mathf.Max(0,t-1.05f),split=Mathf.SmoothStep(0,1,Mathf.Clamp01((t-1.8f)/1.4f));float y=h*.4f-(1-drop*drop)*h,sz=510,dx=split*150;float shake=impact<.5f?Mathf.Sin(impact*65)*6*(1-impact*2):0;
  Rect left=new Rect(w/2-sz/2-dx+shake,y-sz/2,sz/2,sz),right=new Rect(w/2+dx+shake,y-sz/2,sz/2,sz);
  GUI.color=new Color(1,1,1,1-Mathf.Clamp01((t-3.4f)/.9f));GUI.DrawTextureWithTexCoords(left,emblem,new Rect(0,0,.5f,1));GUI.DrawTextureWithTexCoords(right,emblem,new Rect(.5f,0,.5f,1));GUI.color=Color.white;
  if(impact>0&&impact<1.7f){Color c=new Color(.45f,.72f,1,(1-impact/1.7f)*.65f);for(int side=-1;side<=1;side+=2){Vector2 a=new Vector2(w/2+side*235,y);for(int i=0;i<7;i++){Vector2 b=a+new Vector2(side*55,Mathf.Sin(i*7+Mathf.Floor(t*7))*70);Line(a,b,c,2);a=b;}}}
  if(split>0){for(int i=8;i>=1;i--)OdysseyUI.Fill(new Rect(w/2-i*2,y-180,i*4,360),new Color(.65f,.82f,1,.018f*(1-split)));}
  OdysseyCinematic.Title(new Rect(0,h*.75f,w,65),t<1.8f?"A story takes root":"A new chapter awakens",31,new Color(.85f,.8f,.68f));OdysseyUI.Text(new Rect(w-255,h-55,230,30),"ENTER / SPACE  Skip",16,OdysseyUI.Muted);
  GUI.matrix=matrix;GUI.color=tint;
 }
 void OnDestroy(){if(audioSource!=null)Destroy(audioSource);if(thunder!=null)Destroy(thunder);}
}}
