using UnityEngine;

namespace AlbionOdyssey
{
    // Short-lived world-space chat for nearby online Keepers. Messages are
    // distance-capped and expire automatically so the campus remains readable.
    public sealed class CampusOnlineChatBubble : MonoBehaviour
    {
        TextMesh mesh;
        Camera targetCamera;
        Vector3 offset;
        float maxDistance = 20f;
        float visibleUntil;
        float phase;

        public void Configure(Vector3 localOffset, float distance = 20f)
        {
            offset = localOffset;
            maxDistance = distance;
            var bubble = new GameObject("Online chat bubble");
            bubble.transform.SetParent(transform, false);
            mesh = bubble.AddComponent<TextMesh>();
            mesh.fontSize = 34;
            mesh.characterSize = .042f;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.fontStyle = FontStyle.Bold;
            mesh.color = new Color(.98f, .88f, .54f);
            bubble.SetActive(false);
        }

        public void Show(string display, string text)
        {
            if (mesh == null) return;
            mesh.text = display + ": " + Sanitize(text);
            visibleUntil = Time.unscaledTime + 4f;
        }

        void LateUpdate()
        {
            if (mesh == null || Time.unscaledTime >= visibleUntil)
            {
                if (mesh != null) mesh.gameObject.SetActive(false);
                return;
            }
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
                if (targetCamera == null)
                    foreach (var camera in FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                        if (camera.enabled) { targetCamera = camera; break; }
            }
            if (targetCamera == null) return;
            Vector3 world = transform.TransformPoint(offset);
            float distance = Vector3.Distance(targetCamera.transform.position, world);
            Vector3 screen = targetCamera.WorldToScreenPoint(world);
            bool visible = distance < maxDistance && screen.z > .2f;
            mesh.gameObject.SetActive(visible);
            if (!visible) return;
            phase += Time.deltaTime * 2.8f;
            mesh.transform.position = world + Vector3.up * (Mathf.Sin(phase) * .025f);
            mesh.transform.rotation = Quaternion.LookRotation(mesh.transform.position - targetCamera.transform.position, Vector3.up);
        }

        static string Sanitize(string value)
        {
            value = (value ?? "").Trim();
            if (value.Length > 80) value = value.Substring(0, 80);
            return value.Replace("<", "").Replace(">", "").Replace("\n", " ");
        }
    }
}
