using UnityEngine;

namespace AlbionOdyssey
{
    // A small activity-aware speech indicator. It keeps the social layer readable
    // from the path while remaining quiet when a student is travelling between
    // destinations.
    public sealed class CampusConversationBubble : MonoBehaviour
    {
        TextMesh mesh;
        Camera targetCamera;
        CampusActivityKind kind;
        string studentName;
        Vector3 basePosition;
        float maxDistance = 16f;
        bool requested;
        float phase;

        public void Configure(string name, CampusActivityKind activity, Vector3 localOffset, float distance = 16f)
        {
            studentName = name;
            kind = activity;
            maxDistance = distance;
            basePosition = localOffset;
            var bubble = new GameObject("Conversation bubble");
            bubble.transform.SetParent(transform, false);
            mesh = bubble.AddComponent<TextMesh>();
            mesh.fontSize = 38;
            mesh.characterSize = .045f;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.fontStyle = FontStyle.Bold;
            mesh.color = new Color(.78f, .95f, .98f);
            mesh.text = Prompt();
            bubble.SetActive(false);
        }

        public void SetVisible(bool value)
        {
            requested = value;
            if (mesh != null && !value) mesh.gameObject.SetActive(false);
        }

        public void SetActivity(CampusActivityKind activity)
        {
            kind = activity;
            if (mesh != null) mesh.text = Prompt();
        }

        void LateUpdate()
        {
            if (mesh == null || !requested) return;
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
                if (targetCamera == null)
                    foreach (var camera in FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                        if (camera.enabled) { targetCamera = camera; break; }
            }
            if (targetCamera == null) return;
            Vector3 world = transform.TransformPoint(basePosition);
            float distance = Vector3.Distance(targetCamera.transform.position, world);
            Vector3 screen = targetCamera.WorldToScreenPoint(world);
            bool visible = distance < maxDistance && screen.z > .2f;
            mesh.gameObject.SetActive(visible);
            if (!visible) return;
            phase += Time.deltaTime * 2.4f;
            mesh.transform.position = world + Vector3.up * (Mathf.Sin(phase) * .035f);
            mesh.transform.rotation = Quaternion.LookRotation(mesh.transform.position - targetCamera.transform.position, Vector3.up);
        }

        string Prompt()
        {
            switch (kind)
            {
                case CampusActivityKind.Learn: return studentName + "  ·  discussing class";
                case CampusActivityKind.Eat: return studentName + "  ·  lunch chat";
                case CampusActivityKind.Teach: return studentName + "  ·  lesson in session";
                case CampusActivityKind.Play: return studentName + "  ·  your turn?";
                case CampusActivityKind.Serve: return studentName + "  ·  serving";
                case CampusActivityKind.Buy: return studentName + "  ·  at checkout";
                default: return studentName + "  ·  talking";
            }
        }
    }
}
