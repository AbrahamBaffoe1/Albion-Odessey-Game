using UnityEngine;
using UnityEngine.Rendering;

namespace AlbionOdyssey
{
    // Natural warm sunlight, a cooler skylight and restrained distance haze.
    // Weather owns snow geometry; this layer only adjusts the lighting response.
    public sealed class CampusArtDirection : MonoBehaviour
    {
        OdysseyGame game;
        public void Setup(OdysseyGame owner){game=owner;Apply(0);}
        void Update(){if(game!=null&&game.Ready)Apply(game.weather.IsSnowing?1:0);}
        void Apply(float winter)
        {
            RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=Color.Lerp(new Color(.36f,.48f,.65f),new Color(.48f,.54f,.64f),winter);
            RenderSettings.ambientEquatorColor=Color.Lerp(new Color(.23f,.28f,.30f),new Color(.32f,.37f,.44f),winter);
            RenderSettings.ambientGroundColor=Color.Lerp(new Color(.16f,.17f,.13f),new Color(.30f,.33f,.37f),winter);
            RenderSettings.fogColor=Color.Lerp(new Color(.64f,.73f,.80f),new Color(.73f,.79f,.84f),winter);
            RenderSettings.fogDensity=Mathf.Lerp(.0010f,.0018f,winter);
            if(RenderSettings.sun!=null){RenderSettings.sun.color=Color.Lerp(new Color(1,.92f,.79f),new Color(.88f,.93f,1),winter);RenderSettings.sun.intensity=Mathf.Lerp(1.45f,.9f,winter);}
        }
    }
}
