using System;
using UnityEngine;
namespace AlbionOdyssey
{
    // Any building exported with the CraftDescription schema can use this loader.
    // Separate load/unload distances avoid rapid toggling at an entrance boundary.
    public sealed class StreamedInterior : MonoBehaviour
    {
        public float LoadDistance=55,HideDistance=85;
        public GameObject Content {get;private set;}public GameObject Exterior {get;private set;}
        Transform observer;CraftDescription description;string assetKey;Action<GameObject> decorate;
        public void Initialize(string key,CraftDescription data,Transform viewer,Action<GameObject> onLoaded)
        {
            assetKey=key;description=data;observer=viewer;decorate=onLoaded;
            foreach(var section in data.sections)if(section.name=="exterior")Exterior=CraftModel.Load(assetKey+"-"+section.name,section,data.materials,transform);
        }
        public void EnsureLoaded()
        {
            if(Content!=null){Content.SetActive(true);return;}
            Content=new GameObject(assetKey+" walkable interior");Content.transform.SetParent(transform,false);
            foreach(var section in description.sections)if(section.name!="exterior")CraftModel.Load(assetKey+"-"+section.name,section,description.materials,Content.transform);
            decorate?.Invoke(Content);Physics.SyncTransforms();
        }
        void Update()
        {
            if(observer==null)return;
            var d=observer.position-transform.position;d.y=0;
            if(d.sqrMagnitude<LoadDistance*LoadDistance)EnsureLoaded();
            else if(d.sqrMagnitude>HideDistance*HideDistance&&Content!=null&&Content.activeSelf)Content.SetActive(false);
        }
    }
}
