using UnityEngine;
using UnityEngine.Rendering;
namespace AlbionOdyssey
{
    // One nearby reference-building probe, updated only after lighting changes.
    // Captures are time-sliced and never refreshed while a nature scene owns the sky.
    public sealed class ArchitectureReflection : MonoBehaviour
    {
        ReflectionProbe probe;OdysseyGame game;float nextCheck,lastHour=-100;bool lastSnow;int renderId=-1;
        public static void Attach(Transform building,OdysseyGame owner)
        {
            var o=new GameObject("Ferguson courtyard reflections");o.transform.SetParent(building,false);o.transform.localPosition=new Vector3(0,4,-9);
            var component=o.AddComponent<ArchitectureReflection>();component.game=owner;
            var p=component.probe=o.AddComponent<ReflectionProbe>();p.mode=ReflectionProbeMode.Realtime;p.refreshMode=ReflectionProbeRefreshMode.ViaScripting;
            p.timeSlicingMode=ReflectionProbeTimeSlicingMode.IndividualFaces;p.resolution=128;p.hdr=true;p.boxProjection=true;
            p.center=new Vector3(0,3,9);p.size=new Vector3(38,18,28);p.blendDistance=3;p.nearClipPlane=.25f;p.farClipPlane=95;p.shadowDistance=45;
        }
        void Update()
        {
            if(game==null||!game.Ready||game.environment==null||game.environment.Locked||Time.unscaledTime<nextCheck)return;
            if((game.player.transform.position-transform.position).sqrMagnitude>120*120)return;
            if(renderId>=0&&!probe.IsFinishedRendering(renderId))return;
            nextCheck=Time.unscaledTime+10;
            float hour=game.environment.Hour;bool snow=game.weather!=null&&game.weather.IsSnowing;
            if(lastHour>-99&&Mathf.Abs(Mathf.DeltaAngle(lastHour*15,hour*15))<3.75f&&snow==lastSnow)return;
            lastHour=hour;lastSnow=snow;renderId=probe.RenderProbe();
        }
    }
}
