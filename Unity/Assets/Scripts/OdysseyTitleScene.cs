using UnityEngine;
namespace AlbionOdyssey {
 // A live GPU nebula and two independently animated squirrel cutouts, rendered only on the title screen.
 public sealed class OdysseyTitleScene:MonoBehaviour {
  Material material;RenderTexture frame;Texture2D medallion;float nextFrame;bool wasReduced;
  public void Draw(float w,float h){
   if(material==null){var shader=Resources.Load<Shader>("Presentation/CosmicTitle");if(shader==null)return;material=new Material(shader);material.SetTexture("_Squirrel",Resources.Load<Texture2D>("Presentation/TitleSquirrel"));medallion=Resources.Load<Texture2D>("Presentation/AcornMedallion");}
   int width=Mathf.Min(1600,Screen.width),height=Mathf.RoundToInt(width*h/w);
   if(frame==null||frame.width!=width||frame.height!=height){if(frame!=null){frame.Release();Destroy(frame);}frame=new RenderTexture(width,height,0,RenderTextureFormat.ARGB32);frame.Create();nextFrame=0;}
   bool reduced=OdysseyAccessibility.ReducedMotion;
   if(Time.unscaledTime>=nextFrame||wasReduced!=reduced){material.SetFloat("_Clock",reduced?4:Time.unscaledTime);material.SetFloat("_Aspect",w/h);var previous=RenderTexture.active;Graphics.Blit(null,frame,material);RenderTexture.active=previous;nextFrame=Time.unscaledTime+(reduced?3600:1f/30);wasReduced=reduced;}
   GUI.DrawTexture(new Rect(0,0,w,h),frame);
   if(medallion!=null){float size=Mathf.Min(430,h*.52f);var color=GUI.color;GUI.color=new Color(1,1,1,.3f);GUI.DrawTexture(new Rect(w*.5f-size*.5f,h*.025f,size,size),medallion,ScaleMode.ScaleToFit);GUI.color=color;}
  }
  void OnDestroy(){if(material!=null)Destroy(material);if(frame!=null){frame.Release();Destroy(frame);}}
 }
}
