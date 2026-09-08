using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace AlbionOdyssey
{
    public enum CampusWeatherState { Clear, Snow }

    /// <summary>
    /// A lightweight campus weather layer. Snow is visual-only: the blanket
    /// and roof caps have no colliders, so arrivals, doors, cars and walking
    /// keep using the same authored collision as clear weather.
    /// </summary>
    public sealed class CampusWeather : MonoBehaviour
    {
        OdysseyGame game;
        GameObject flakeRoot;
        GameObject snowCover;
        readonly List<GameObject> roofCaps=new List<GameObject>();
        readonly List<Transform> flakes=new List<Transform>();
        readonly List<Vector3> flakeOffsets=new List<Vector3>();
        readonly List<float> flakeSpeeds=new List<float>();
        Material snowMaterial,flakeMaterial;
        CampusWeatherState state=CampusWeatherState.Clear;
        float nextTransition;
        int transitions;
        bool ready;

        public CampusWeatherState State=>state;
        public bool IsSnowing=>state==CampusWeatherState.Snow;
        public int SnowTransitions=>transitions;
        public bool SnowCoverVisible=>snowCover!=null&&snowCover.activeSelf;
        public int SnowflakesVisible=>flakeRoot!=null&&flakeRoot.activeSelf?flakes.Count:0;

        public void Setup(OdysseyGame owner)
        {
            game=owner;
            BuildSnowflakes();
            BuildSnowCover();
            BuildRoofCaps();
            state=CampusWeatherState.Clear;
            nextTransition=Time.time+180f;
            ready=true;
            ApplyVisuals();
        }

        void BuildSnowflakes()
        {
            flakeRoot=new GameObject("Campus snowfall · falling flakes");flakeRoot.transform.SetParent(transform,false);
            flakeMaterial=BuildFlakeMaterial();
            var random=new System.Random(1701);
            for(int i=0;i<150;i++)
            {
                var flake=GameObject.CreatePrimitive(PrimitiveType.Sphere);flake.name="Snowflake "+(i+1).ToString("000");flake.transform.SetParent(flakeRoot.transform,false);
                Destroy(flake.GetComponent<Collider>());flake.GetComponent<Renderer>().sharedMaterial=flakeMaterial;
                float size=.035f+(float)random.NextDouble()*.07f;flake.transform.localScale=Vector3.one*size;
                flakeOffsets.Add(new Vector3((float)random.NextDouble()*68f-34f,(float)random.NextDouble()*16f-2f,(float)random.NextDouble()*68f-34f));
                flakeSpeeds.Add(.7f+(float)random.NextDouble()*1.25f);flakes.Add(flake.transform);
            }
            flakeRoot.SetActive(false);
        }

        void BuildSnowCover()
        {
            snowCover=GameObject.CreatePrimitive(PrimitiveType.Cube);snowCover.name="Campus snow blanket · visual only";
            snowCover.transform.position=new Vector3(0,.095f,450);snowCover.transform.localScale=new Vector3(1200f,.07f,750f);
            Destroy(snowCover.GetComponent<Collider>());snowCover.GetComponent<Renderer>().sharedMaterial=snowMaterial=TowerGeometry.Material("Campus snow cover",new Color(.80f,.87f,.94f),0,.82f);
        }

        void BuildRoofCaps()
        {
            var renderers=FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
            foreach(var source in renderers)
            {
                string name=source.gameObject.name;
                if(name.IndexOf("roof",StringComparison.OrdinalIgnoreCase)<0||source.bounds.size.x<1f||source.bounds.size.z<1f)continue;
                var cap=GameObject.CreatePrimitive(PrimitiveType.Cube);cap.name="Snow cap · "+name;
                var b=source.bounds;cap.transform.position=new Vector3(b.center.x,b.max.y+.04f,b.center.z);
                cap.transform.localScale=new Vector3(Mathf.Max(1.1f,b.size.x*1.04f),.075f,Mathf.Max(1.1f,b.size.z*1.04f));
                Destroy(cap.GetComponent<Collider>());cap.GetComponent<Renderer>().sharedMaterial=snowMaterial;roofCaps.Add(cap);
            }
        }

        Material BuildFlakeMaterial()
        {
            var shader=Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if(shader==null)shader=Shader.Find("Particles/Standard Unlit");
            if(shader==null)shader=Shader.Find("Standard");
            var material=new Material(shader){name="Campus snowflakes"};
            if(material.HasProperty("_BaseColor"))material.SetColor("_BaseColor",new Color(.94f,.98f,1f,.93f));
            if(material.HasProperty("_Color"))material.SetColor("_Color",new Color(.94f,.98f,1f,.93f));
            if(shader.name=="Standard")
            {
                material.SetFloat("_Mode",3);material.SetInt("_SrcBlend",(int)BlendMode.SrcAlpha);material.SetInt("_DstBlend",(int)BlendMode.OneMinusSrcAlpha);material.SetInt("_ZWrite",0);
                material.EnableKeyword("_ALPHABLEND_ON");material.renderQueue=3000;
            }
            return material;
        }

        public bool HandleInput()
        {
            if(!ready||game==null||game.life!=null&&game.life.PanelOpen)return false;
            if(Input.GetKeyDown(KeyCode.Y)){SetSnow(!IsSnowing,false);return true;}
            return false;
        }

        public void SetSnowForVerification(bool enabled){if(!ready)return;SetSnow(enabled,true);}

        void SetSnow(bool enabled,bool verification)
        {
            var next=enabled?CampusWeatherState.Snow:CampusWeatherState.Clear;
            if(next!=state)transitions++;
            state=next;
            nextTransition=Time.time+(enabled?150f:210f);
            ApplyVisuals();
            if(!verification&&game!=null)game.notice=enabled?"Snowfall has started across campus. Press Y to clear the weather.":"The snow has cleared. Press Y whenever you want another snowfall.";
        }

        void Update()
        {
            if(!ready||game==null)return;
            if(Time.time>=nextTransition)SetSnow(!IsSnowing,false);
            if(flakeRoot!=null&&game.player!=null)
            {
                flakeRoot.transform.position=game.player.transform.position+Vector3.up*7f;
                bool campus=game.campus!=null&&game.campus.OnCampus;
                bool active=IsSnowing&&campus;
                if(active&&!flakeRoot.activeSelf)flakeRoot.SetActive(true);
                if(active)AnimateFlakes();
                else if(!active&&flakeRoot.activeSelf)flakeRoot.SetActive(false);
                if(snowCover!=null)snowCover.SetActive(active);
                foreach(var cap in roofCaps)if(cap!=null)cap.SetActive(active);
            }
        }

        void ApplyVisuals()
        {
            if(flakeRoot==null||game==null||game.player==null)return;
            bool campus=game.campus!=null&&game.campus.OnCampus;
            bool active=IsSnowing&&campus;
            flakeRoot.SetActive(active);
            if(snowCover!=null)snowCover.SetActive(active);
            foreach(var cap in roofCaps)if(cap!=null)cap.SetActive(active);
            if(active)AnimateFlakes();
        }

        void AnimateFlakes()
        {
            float now=Time.time;
            for(int i=0;i<flakes.Count;i++)
            {
                var p=flakeOffsets[i];
                p.y=Mathf.Repeat(flakeOffsets[i].y-now*flakeSpeeds[i],18f)-2f;
                p.x+=Mathf.Sin(now*.35f+i*.71f)*.35f;p.z+=Mathf.Cos(now*.28f+i*.53f)*.35f;
                flakes[i].localPosition=p;
            }
        }
    }
}
