using UnityEngine;

namespace AlbionOdyssey
{
    // A small world-space label that only appears when the player can reasonably
    // read it. It keeps the campus alive without turning every NPC into a HUD panel.
    public sealed class CampusWorldLabel : MonoBehaviour
    {
        TextMesh mesh; Camera targetCamera; Transform plate; Renderer plateRenderer; Vector3 offset; float maxDistance=22f;
        Color textColor; float nextVisibilityCheck;
        public void Configure(string text,Color accent,Vector3 localOffset,float distance=22f)
        {
            offset=localOffset;maxDistance=distance;textColor=accent;
            var labelObject=new GameObject("World label");labelObject.transform.SetParent(transform,false);
            mesh=labelObject.AddComponent<TextMesh>();mesh.text=text;mesh.fontSize=42;mesh.characterSize=.045f;mesh.anchor=TextAnchor.MiddleCenter;mesh.alignment=TextAlignment.Center;mesh.color=accent;mesh.fontStyle=FontStyle.Bold;
            var textRenderer=mesh.GetComponent<Renderer>();if(textRenderer!=null)textRenderer.sortingOrder=10;
            var backing=GameObject.CreatePrimitive(PrimitiveType.Quad);backing.name="World label backing";backing.transform.SetParent(labelObject.transform,false);backing.transform.localPosition=new Vector3(0,0,-.028f);backing.transform.localScale=BackingScale(text);var collider=backing.GetComponent<Collider>();if(collider!=null)Destroy(collider);plate=backing.transform;plateRenderer=backing.GetComponent<Renderer>();if(plateRenderer!=null){var source=TowerGeometry.Material("World label backing source",new Color(.025f,.035f,.065f));var material=new Material(source.shader);material.color=new Color(.025f,.035f,.065f,.88f);plateRenderer.sharedMaterial=material;plateRenderer.sortingOrder=1;}
        }
        public void SetText(string value){if(mesh!=null){mesh.text=value;if(plate!=null)plate.localScale=BackingScale(value);}}
        public void SetColor(Color value){textColor=value;if(mesh!=null)mesh.color=value;}
        void LateUpdate()
        {
            if(mesh==null)return;
            if(targetCamera==null){targetCamera=Camera.main;if(targetCamera==null)foreach(var camera in FindObjectsByType<Camera>(FindObjectsInactive.Exclude,FindObjectsSortMode.None))if(camera.enabled){targetCamera=camera;break;}}if(targetCamera==null)return;
            Vector3 world=transform.TransformPoint(offset);float distance=Vector3.Distance(targetCamera.transform.position,world);Vector3 screen=targetCamera.WorldToScreenPoint(world);
            bool visible=distance<maxDistance&&screen.z>.2f;
            if(visible&&Time.unscaledTime>=nextVisibilityCheck){nextVisibilityCheck=Time.unscaledTime+.12f;Vector3 ray=world-targetCamera.transform.position;if(Physics.Raycast(targetCamera.transform.position,ray.normalized,out var hit,ray.magnitude,~0,QueryTriggerInteraction.Ignore)&&!hit.transform.IsChildOf(transform))visible=false;}
            float fade=Mathf.Clamp01(Mathf.InverseLerp(maxDistance,Mathf.Min(2.5f,maxDistance*.22f),distance));
            Color faded=textColor;faded.a=Mathf.Clamp01(fade);mesh.color=faded;
            if(plateRenderer!=null){Color backing=plateRenderer.sharedMaterial.color;backing.a=.88f*Mathf.Clamp01(fade);plateRenderer.sharedMaterial.color=backing;}
            mesh.transform.position=world;mesh.transform.rotation=Quaternion.LookRotation(targetCamera.transform.position-mesh.transform.position,Vector3.up);mesh.gameObject.SetActive(visible);
        }
        static Vector3 BackingScale(string value){int lines=string.IsNullOrEmpty(value)?1:value.Split('\n').Length;float width=Mathf.Clamp(.055f*(value??"").Length+.48f,.78f,5.8f);return new Vector3(width,lines>1 ? .62f : .34f,1);}
    }
}
