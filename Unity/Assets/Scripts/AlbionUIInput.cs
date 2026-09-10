using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace AlbionOdyssey
{
    // Frame-cached controller navigation for the immediate-mode menus. A single
    // poll keeps a thumbstick edge from being consumed by two panels in one frame.
    public static class AlbionUIInput
    {
        static readonly List<InputDevice> devices=new List<InputDevice>();
        static int frame=-1;static Vector2 axis;static bool accept,back,previousAccept,previousBack;static float nextStep;
        public static bool Poll(out int horizontal,out int vertical,out bool choose,out bool cancel)
        {
            if(frame!=Time.frameCount){frame=Time.frameCount;axis=Vector2.zero;bool a=false,b=false;devices.Clear();InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Controller,devices);foreach(var device in devices){if(device.TryGetFeatureValue(CommonUsages.primary2DAxis,out var stick)&&stick.sqrMagnitude>axis.sqrMagnitude)axis=stick;a|=Button(device,CommonUsages.primaryButton)||Button(device,CommonUsages.triggerButton);b|=Button(device,CommonUsages.menuButton)||Button(device,CommonUsages.secondaryButton);}accept=a&&!previousAccept;back=b&&!previousBack;previousAccept=a;previousBack=b;}
            horizontal=vertical=0;float now=Time.unscaledTime;if(now>=nextStep){if(Mathf.Abs(axis.x)>.6f){horizontal=axis.x>0?1:-1;nextStep=now+.22f;}else if(Mathf.Abs(axis.y)>.6f){vertical=axis.y>0?1:-1;nextStep=now+.22f;}}
            choose=accept;cancel=back;return horizontal!=0||vertical!=0||choose||cancel;
        }
        static bool Button(InputDevice device,InputFeatureUsage<bool> usage){return device.isValid&&device.TryGetFeatureValue(usage,out var pressed)&&pressed;}
    }
}
