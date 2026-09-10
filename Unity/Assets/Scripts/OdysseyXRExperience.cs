using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Hands;

namespace AlbionOdyssey
{
    // Runtime XR layer for the existing campus controller. It intentionally uses
    // Unity's device APIs instead of replacing the desktop Explorer, so the same
    // buildings, interactions and collision map work in room play and on Mac.
    public sealed class OdysseyXRExperience : MonoBehaviour
    {
        public bool Active { get; private set; }
        public bool SnapTurn = true;
        public bool ComfortVignette = true;
        public bool RoomScale = true;
        public bool HandTrackingEnabled = true;
        OdysseyGame game;
        InputDevice leftController, rightController;
        Transform leftAnchor, rightAnchor;
        GameObject[] leftHandJoints, rightHandJoints;
        XRHandSubsystem handSubsystem;
        readonly List<XRHandSubsystem> handSubsystems = new List<XRHandSubsystem>();
        static readonly XRHandJointID[] visibleJoints = { XRHandJointID.Wrist, XRHandJointID.Palm, XRHandJointID.ThumbTip, XRHandJointID.IndexTip, XRHandJointID.MiddleTip };
        Vector3 lastHeadLocal;
        bool haveHeadPose, wasSelect, wasGrip;
        float turnCooldown, averageFrame, lowFrameSeconds, previousViewportScale = 1f;
        bool recovering, performanceReduced;
        Material handMaterial;
        public bool Tracking => leftController.isValid || rightController.isValid || handSubsystem != null;

        public void Setup(OdysseyGame owner)
        {
            game = owner;
            SnapTurn = PlayerPrefs.GetInt("Odyssey.XR.SnapTurn", 1) == 1;
            ComfortVignette = PlayerPrefs.GetInt("Odyssey.XR.Vignette", 1) == 1;
            RoomScale = PlayerPrefs.GetInt("Odyssey.XR.RoomScale", 1) == 1;
            HandTrackingEnabled = PlayerPrefs.GetInt("Odyssey.XR.Hands", 1) == 1;
            Application.targetFrameRate = 72;
            handMaterial = TowerGeometry.Material("XR hand tracking", new Color(.55f, .78f, .95f), 0, .55f);
            leftAnchor = Anchor("XR left controller");
            rightAnchor = Anchor("XR right controller");
            leftHandJoints = HandVisuals("XR left hand");
            rightHandJoints = HandVisuals("XR right hand");
            RefreshDevices();
        }

        void Update()
        {
            averageFrame = Mathf.Lerp(averageFrame <= 0 ? Time.unscaledDeltaTime : averageFrame, Time.unscaledDeltaTime, .08f);
            if (averageFrame > 1f / 55f) lowFrameSeconds += Time.unscaledDeltaTime; else lowFrameSeconds = Mathf.Max(0, lowFrameSeconds - Time.unscaledDeltaTime * 2f);
            bool deviceActive = XRSettings.isDeviceActive;
            if (!deviceActive)
            {
                if (Active) Deactivate();
                return;
            }
            if (!Active) Activate();
            if (!leftController.isValid && !rightController.isValid) RefreshDevices();
            UpdateController(leftController, leftAnchor, true);
            UpdateController(rightController, rightAnchor, false);
            UpdateHands();
            ApplyRoomScale();
            ApplyLocomotion();
            ApplyActions();
            ApplyPerformanceGuard();
            if (turnCooldown > 0) turnCooldown -= Time.unscaledDeltaTime;
        }

        void Activate()
        {
            Active = true;
            game.player.controls = false;
            game.player.thirdPerson = false;
            game.player.cameraDistance = 1.8f;
            haveHeadPose = false;
            previousViewportScale = XRSettings.renderViewportScale;
            performanceReduced = false;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            Debug.Log("ODYSSEY_XR_ACTIVE: room-scale locomotion, controller input and hand tracking ready");
        }

        void Deactivate()
        {
            Active = false;
            if (game != null && game.player != null) game.player.controls = true;
            if (leftAnchor != null) leftAnchor.gameObject.SetActive(false);
            if (rightAnchor != null) rightAnchor.gameObject.SetActive(false);
            SetHandsVisible(false);
            haveHeadPose = false;
            if (performanceReduced) XRSettings.renderViewportScale = previousViewportScale;
            performanceReduced = false;
            Debug.Log("ODYSSEY_XR_INACTIVE: desktop controls restored");
        }

        void RefreshDevices()
        {
            var devices = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.Left, devices);
            if (devices.Count > 0) leftController = devices[0];
            devices.Clear();
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.Right, devices);
            if (devices.Count > 0) rightController = devices[0];
            handSubsystems.Clear();
            SubsystemManager.GetSubsystems(handSubsystems);
            handSubsystem = handSubsystems.Count > 0 && handSubsystems[0].running ? handSubsystems[0] : null;
        }

        Transform Anchor(string name)
        {
            var anchor = new GameObject(name).transform;
            anchor.SetParent(transform, false);
            anchor.gameObject.SetActive(false);
            var visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name = name + " model";
            visual.transform.SetParent(anchor, false);
            visual.transform.localScale = Vector3.one * .09f;
            visual.GetComponent<Renderer>().sharedMaterial = handMaterial;
            Destroy(visual.GetComponent<Collider>());
            return anchor;
        }

        GameObject[] HandVisuals(string name)
        {
            var result = new GameObject[visibleJoints.Length];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                result[i].name = name + " joint " + visibleJoints[i];
                result[i].transform.SetParent(transform, false);
                result[i].transform.localScale = Vector3.one * .045f;
                result[i].GetComponent<Renderer>().sharedMaterial = handMaterial;
                Destroy(result[i].GetComponent<Collider>());
                result[i].SetActive(false);
            }
            return result;
        }

        void UpdateController(InputDevice device, Transform anchor, bool left)
        {
            if (!device.isValid) { anchor.gameObject.SetActive(false); return; }
            if (device.TryGetFeatureValue(CommonUsages.devicePosition, out var position)) anchor.localPosition = position;
            if (device.TryGetFeatureValue(CommonUsages.deviceRotation, out var rotation)) anchor.localRotation = rotation;
            anchor.gameObject.SetActive(true);
        }

        void UpdateHands()
        {
            if (!HandTrackingEnabled || handSubsystem == null || !handSubsystem.running)
            {
                SetHandsVisible(false); return;
            }
            UpdateHand(handSubsystem.leftHand, leftHandJoints);
            UpdateHand(handSubsystem.rightHand, rightHandJoints);
        }

        void UpdateHand(XRHand hand, GameObject[] visuals)
        {
            bool tracked = hand.isTracked;
            for (int i = 0; i < visuals.Length; i++)
            {
                if (tracked && hand.GetJoint(visibleJoints[i]).TryGetPose(out var pose))
                {
                    visuals[i].transform.SetPositionAndRotation(pose.position, pose.rotation);
                    visuals[i].SetActive(true);
                }
                else visuals[i].SetActive(false);
            }
        }

        void SetHandsVisible(bool visible)
        {
            if (leftHandJoints != null) foreach (var joint in leftHandJoints) joint.SetActive(visible);
            if (rightHandJoints != null) foreach (var joint in rightHandJoints) joint.SetActive(visible);
        }

        void ApplyRoomScale()
        {
            if (!RoomScale || game.player.eyes == null) return;
            Vector3 local = game.player.eyes.transform.localPosition;
            if (!haveHeadPose) { lastHeadLocal = local; haveHeadPose = true; return; }
            Vector3 delta = local - lastHeadLocal;
            lastHeadLocal = local;
            delta.y = 0;
            if (delta.sqrMagnitude > .000001f && delta.sqrMagnitude < .16f)
                game.player.body.Move(game.player.transform.TransformVector(delta));
        }

        void ApplyLocomotion()
        {
            if (!leftController.isValid || game.player.body == null) return;
            if (!leftController.TryGetFeatureValue(CommonUsages.primary2DAxis, out var axis)) return;
            if (axis.sqrMagnitude < .01f) return;
            Vector3 forward = game.player.transform.forward; forward.y = 0; forward.Normalize();
            Vector3 right = game.player.transform.right; right.y = 0; right.Normalize();
            Vector3 motion = (right * axis.x + forward * axis.y) * 2.2f * Time.unscaledDeltaTime;
            game.player.body.Move(motion);
        }

        void ApplyActions()
        {
            if (rightController.isValid && rightController.TryGetFeatureValue(CommonUsages.primary2DAxis, out var turnAxis))
            {
                if (SnapTurn && Mathf.Abs(turnAxis.x) > .65f && turnCooldown <= 0)
                {
                    game.player.transform.Rotate(0, turnAxis.x > 0 ? 30f : -30f, 0);
                    turnCooldown = .28f;
                }
            }
            bool select = Button(rightController, CommonUsages.primaryButton) || Button(rightController, CommonUsages.triggerButton);
            if (select && !wasSelect) game.Interact();
            wasSelect = select;
            bool grip = Button(rightController, CommonUsages.gripButton);
            if (grip && !wasGrip) TryGrab(rightAnchor);
            if (!grip && wasGrip) ReleaseGrab();
            wasGrip = grip;
        }

        void ApplyPerformanceGuard()
        {
            if (lowFrameSeconds > 1f && !performanceReduced)
            {
                previousViewportScale = XRSettings.renderViewportScale;
                XRSettings.renderViewportScale = Mathf.Clamp(previousViewportScale * .82f, .65f, 1f);
                performanceReduced = true;
                Debug.Log("ODYSSEY_XR_PERFORMANCE_GUARD: viewport scale reduced for comfort");
            }
            else if (performanceReduced && lowFrameSeconds < .2f)
            {
                XRSettings.renderViewportScale = previousViewportScale;
                performanceReduced = false;
                Debug.Log("ODYSSEY_XR_PERFORMANCE_RECOVERED: viewport scale restored");
            }
        }

        static bool Button(InputDevice device, InputFeatureUsage<bool> usage)
        {
            return device.isValid && device.TryGetFeatureValue(usage, out var pressed) && pressed;
        }

        void TryGrab(Transform hand)
        {
            if (hand == null || !hand.gameObject.activeSelf || !Physics.Raycast(hand.position, hand.forward, out var hit, 2.0f)) return;
            var marker = hit.collider.GetComponentInParent<OdysseyXRGrabTarget>();
            if (marker != null) marker.BeginGrab(hand);
        }

        void ReleaseGrab()
        {
            var grabbed = FindObjectsByType<OdysseyXRGrabTarget>(FindObjectsSortMode.None);
            foreach (var target in grabbed) target.EndGrab();
        }

        void OnApplicationPause(bool pause)
        {
            if (pause) recovering = true;
            else if (recovering) { recovering = false; RefreshDevices(); haveHeadPose = false; Debug.Log("ODYSSEY_XR_RECOVERED: tracking devices reacquired after pause"); }
        }

        void OnGUI()
        {
            if (!Active) return;
            float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 800f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
            float width = Screen.width / scale;
            GUI.color = Color.white;
            GUI.Label(new Rect(22, 20, 580, 28), "XR CAMPUS WALK · " + (Tracking ? "TRACKING" : "SEARCHING FOR HEADSET"));
            GUI.Label(new Rect(22, 49, 700, 24), "Left stick move · right stick snap-turn · trigger interact · grip grab · F8 menu");
            if (recovering) GUI.Label(new Rect(22, 78, 480, 24), "Resuming headset tracking…");
            if (lowFrameSeconds > 1f) GUI.Label(new Rect(22, 78, 620, 24), "Comfort warning: performance below 55 FPS");
            if (performanceReduced) GUI.Label(new Rect(22, 106, 620, 24), "Performance guard active · visual scale reduced temporarily");
            if (ComfortVignette)
            {
                GUI.color = new Color(0, 0, 0, .42f);
                GUI.DrawTexture(new Rect(0, 0, 90, Screen.height / scale), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(width - 90, 0, 90, Screen.height / scale), Texture2D.whiteTexture);
            }
            GUI.color = Color.white;
        }
    }

    public sealed class OdysseyXRGrabTarget : MonoBehaviour
    {
        Transform hand;
        Vector3 localOffset;
        Quaternion localRotation;
        public void BeginGrab(Transform source)
        {
            if (hand != null) return;
            hand = source; localOffset = Quaternion.Inverse(source.rotation) * (transform.position - source.position); localRotation = Quaternion.Inverse(source.rotation) * transform.rotation;
        }
        public void EndGrab() { hand = null; }
        void LateUpdate()
        {
            if (hand == null) return;
            transform.position = hand.position + hand.rotation * localOffset;
            transform.rotation = hand.rotation * localRotation;
        }
    }
}
