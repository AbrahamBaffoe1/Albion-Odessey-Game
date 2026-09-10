using UnityEngine;

namespace AlbionOdyssey
{
    // A small world-space label that only appears when the player can reasonably
    // read it. It keeps the campus alive without turning every NPC into a HUD panel.
    public sealed class CampusWorldLabel : MonoBehaviour
    {
        TextMesh mesh; Camera targetCamera; Vector3 offset; float maxDistance=22f;
        public void Configure(string text,Color accent,Vector3 localOffset,float distance=22f)
        {
            offset=localOffset;maxDistance=distance;var labelObject=new GameObject("World label");labelObject.transform.SetParent(transform,false);mesh=labelObject.AddComponent<TextMesh>();mesh.text=text;mesh.fontSize=42;mesh.characterSize=.045f;mesh.anchor=TextAnchor.MiddleCenter;mesh.alignment=TextAlignment.Center;mesh.color=accent;mesh.fontStyle=FontStyle.Bold;
        }
        void LateUpdate()
        {
            if(mesh==null)return;
            if(targetCamera==null){targetCamera=Camera.main;if(targetCamera==null)foreach(var camera in FindObjectsByType<Camera>(FindObjectsInactive.Exclude,FindObjectsSortMode.None))if(camera.enabled){targetCamera=camera;break;}}if(targetCamera==null)return;
            Vector3 world=transform.TransformPoint(offset);float distance=Vector3.Distance(targetCamera.transform.position,world);Vector3 screen=targetCamera.WorldToScreenPoint(world);
            bool visible=distance<maxDistance&&screen.z>.2f;
            mesh.transform.position=world;mesh.transform.rotation=Quaternion.LookRotation(mesh.transform.position-targetCamera.transform.position,Vector3.up);mesh.gameObject.SetActive(visible);
        }
    }
}
