using UnityEngine;
namespace AlbionOdyssey {
 // A live GPU nebula and two independently animated squirrel cutouts, rendered only on the title screen.
 public sealed class OdysseyTitleScene:MonoBehaviour {
  Material material;RenderTexture frame;Texture2D medallion;float nextFrame;bool wasReduced;float began;int impactCycle=-1;
  public void Restart(){began=Time.unscaledTime;impactCycle=-1;}
  public void Draw(float w,float h){
   if(material==null){var shader=Resources.Load<Shader>("Presentation/CosmicTitle");if(shader==null)return;material=new Material(shader);material.SetTexture("_Squirrel",Resources.Load<Texture2D>("Presentation/TitleSquirrel"));medallion=Resources.Load<Texture2D>("Presentation/AcornMedallion");}
   int width=Mathf.Min(1600,Screen.width),height=Mathf.RoundToInt(width*h/w);
   if(frame==null||frame.width!=width||frame.height!=height){if(frame!=null){frame.Release();Destroy(frame);}frame=new RenderTexture(width,height,0,RenderTextureFormat.ARGBHalf);frame.Create();nextFrame=0;}
   bool reduced=OdysseyAccessibility.ReducedMotion;float clock=Time.unscaledTime-began;
   int cycle=Mathf.FloorToInt(clock/12);if(!reduced&&clock%12>=4&&impactCycle!=cycle){impactCycle=cycle;GetComponent<OdysseySoundscape>()?.Impact();}
   if(Time.unscaledTime>=nextFrame||wasReduced!=reduced){material.SetFloat("_Clock",reduced?0:clock);material.SetTexture("_Coin",medallion);material.SetFloat("_CoinSize",Mathf.Min(430,h*.52f)/h);material.SetFloat("_Aspect",w/h);var previous=RenderTexture.active;Graphics.Blit(null,frame,material);RenderTexture.active=previous;nextFrame=Time.unscaledTime+(reduced?3600:1f/30);wasReduced=reduced;}
   GUI.DrawTexture(new Rect(0,0,w,h),frame);

  }
  void OnDestroy(){if(material!=null)Destroy(material);if(frame!=null){frame.Release();Destroy(frame);}}
 }
}
