using System.Collections.Generic;
using UnityEngine;
namespace AlbionOdyssey
{
    public sealed class WorldTextDepth:MonoBehaviour
    {
        readonly HashSet<TextMesh> applied=new HashSet<TextMesh>();float next;
        void LateUpdate()
        {
            if(Time.unscaledTime<next)return;next=Time.unscaledTime+2;
            foreach(var text in FindObjectsByType<TextMesh>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            {
                if(!applied.Add(text))continue;var r=text.GetComponent<MeshRenderer>();if(r==null||r.sharedMaterial==null)continue;
                var m=new Material(Shader.Find("Odyssey/WorldText"));m.mainTexture=r.sharedMaterial.mainTexture;r.sharedMaterial=m;
            }
        }
    }
}
