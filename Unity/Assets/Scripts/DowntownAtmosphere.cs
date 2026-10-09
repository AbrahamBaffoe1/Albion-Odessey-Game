using System.Collections.Generic;
using UnityEngine;

namespace AlbionOdyssey
{
    // Original, fictional shop dressing within the approximate Albion district.
    public sealed class DowntownAtmosphere : MonoBehaviour
    {
        OdysseyGame game;
        readonly List<Light> lights = new List<Light>();
        Material trim, wood, warm, teal, purple, paper;
        ParticleSystem rain;
        public void Setup(OdysseyGame owner)
        {
            game = owner;
            trim = TowerGeometry.Material("Downtown iron", new Color(.065f,.085f,.09f), .6f,.55f);
            wood = TowerGeometry.Material("Downtown walnut", new Color(.22f,.12f,.065f));
            paper = TowerGeometry.Material("Downtown paper", new Color(.65f,.54f,.37f));
            warm = Glow("Downtown amber",new Color(1,.57f,.21f));
            teal = Glow("Downtown mint",new Color(.22f,.9f,.75f));
            purple = Glow("Downtown rose",new Color(.65f,.29f,.42f));
            var go = new GameObject("Local downtown rain"); go.transform.SetParent(transform);
            rain=go.AddComponent<ParticleSystem>(); rain.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=rain.main; main.startLifetime=1.1f; main.startSpeed=0; main.startSize=.025f; main.maxParticles=1400; main.simulationSpace=ParticleSystemSimulationSpace.World;
            main.startColor=new Color(.65f,.8f,.87f,.3f);
            var emission=rain.emission; emission.rateOverTime=900;
            var shape=rain.shape; shape.shapeType=ParticleSystemShapeType.Box; shape.scale=new Vector3(34,.1f,34);
            var velocity=rain.velocityOverLifetime;velocity.enabled=true;velocity.space=ParticleSystemSimulationSpace.World;velocity.x=-1.4f;velocity.y=-19;velocity.z=.7f;
            var renderer=rain.GetComponent<ParticleSystemRenderer>();renderer.renderMode=ParticleSystemRenderMode.Stretch;renderer.lengthScale=7;renderer.velocityScale=.025f;
            renderer.sharedMaterial=new Material(Resources.Load<Shader>("Shaders/DowntownRain"));
        }
        Material Glow(string name,Color color){var m=TowerGeometry.Material(name,color, .1f,.5f);m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",color*.8f);return m;}
        GameObject Part(string name,Vector3 p,Vector3 size,Material m,bool solid=false)
        {return KeeperAvatar.Part(transform,name,PrimitiveType.Cube,AlbionCity.Origin+p,size,m,solid);}
        public void Dress(int side,float z,float length,float height,int index)
        {
            float face=side*11.85f;
            Material glow=index%3==0?teal:index%3==1?warm:purple;
            // Recessed display room: a floor, back wall and furniture behind an open framed frontage.
            Part("Display floor",new Vector3(side*14, .2f,z),new Vector3(4,.25f,length-.6f),wood,true);
            Part("Display back",new Vector3(side*16.2f,1.65f,z),new Vector3(.2f,3,length-.6f),wood,true);
            for(int j=-1;j<=1;j++)
            {
                float wz=z+j*(length-2)/3;
                Part("Window sill",new Vector3(face, .65f,wz),new Vector3(.5f,.22f,(length-2)/3),paper);
                Part("Display frame",new Vector3(face,1.7f,wz-(length-2)/6),new Vector3(.24f,2.4f,.16f),trim,true);
                Part("Upper inset",new Vector3(face,height-2,wz),new Vector3(.12f,1.65f,2.1f),trim);
                Part("Upper lamplit pane",new Vector3(face-side*.08f,height-2,wz),new Vector3(.05f,1.35f,1.82f),j==0?purple:warm);
                Part("Window mullion",new Vector3(face-side*.12f,height-2,wz),new Vector3(.07f,1.5f,.08f),trim);
                Part("Window crossbar",new Vector3(face-side*.13f,height-2,wz),new Vector3(.07f,.08f,1.9f),trim);
                Part("Display counter",new Vector3(side*14, .8f,wz),new Vector3(1.1f,1.3f,2),wood);
                for(int k=0;k<5;k++)Part("Books and parcels",new Vector3(side*14,1.65f,wz-.75f+k*.32f),new Vector3(.5f,.3f+(k%3)*.15f,.22f),k%2==0?paper:trim);
            }
            Part("Shop fascia",new Vector3(face-side*.08f,3.55f,z),new Vector3(.28f,.85f,length-.7f),trim);
            Part("Neon underline",new Vector3(face-side*.25f,3.25f,z),new Vector3(.08f,.055f,length-1.5f),glow);
            string[] names={"BOOKS & COFFEE","THE ARCADE","CORNER MARKET","RECORDS","ALBION WORKSHOP","FLOWERS"};
            var label=new GameObject("Fictional shop sign");label.transform.SetParent(transform);label.transform.position=AlbionCity.ToWorld(face-side*.26f,3.6f,z);
            label.transform.rotation=Quaternion.Euler(0,side*90,0);
            var text=label.AddComponent<TextMesh>();text.text=names[index%names.Length];text.fontSize=64;text.characterSize=.095f;text.anchor=TextAnchor.MiddleCenter;text.color=glow.color;
            // Small pools of light; only the closest four are enabled at runtime.
            var bulb=new GameObject("Shop spill light");bulb.transform.SetParent(transform);bulb.transform.position=AlbionCity.ToWorld(side*10.8f,2.8f,z);
            var light=bulb.AddComponent<Light>();light.type=LightType.Point;light.color=glow.color;light.range=10;light.intensity=2.2f;light.shadows=LightShadows.None;light.enabled=false;lights.Add(light);
            Part("Rain drain",new Vector3(side*6.2f,.09f,z),new Vector3(.55f,.03f,1.2f),trim);
            Part("Sidewalk planter",new Vector3(side*10.5f,.45f,z+length*.4f),new Vector3(.8f,.8f,1.5f),wood,true);
            Part("Planter greenery",new Vector3(side*10.5f,.97f,z+length*.4f),new Vector3(.85f,.32f,1.55f),TowerGeometry.Material("Downtown planting",new Color(.12f,.23f,.11f)));
            if(index%3==0){Part("Bench seat",new Vector3(side*10.2f,.6f,z-length*.4f),new Vector3(.65f,.16f,1.9f),wood,true);Part("Bench back",new Vector3(side*10.6f,1,z-length*.4f),new Vector3(.12f,.65f,1.9f),wood,true);}
        }
        void Update()
        {
            if(game==null||game.player==null)return;
            var p=game.player.transform.position;
            bool active=game.city!=null&&game.city.InCity&&Mathf.Abs(p.x-CityCatalog.OriginX)<11&&p.z-CityCatalog.OriginZ>25 && !(game.weather!=null&&game.weather.IsSnowing);
            rain.transform.position=p+Vector3.up*18;
            if(active&&!rain.isPlaying)rain.Play();else if(!active&&rain.isPlaying)rain.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            lights.Sort((a,b)=>(a.transform.position-p).sqrMagnitude.CompareTo((b.transform.position-p).sqrMagnitude));
            for(int i=0;i<lights.Count;i++)lights[i].enabled=game.city.InCity&&i<4&&(lights[i].transform.position-p).sqrMagnitude<900;
        }
    }
}
