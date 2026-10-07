using System;
using UnityEngine;
namespace AlbionOdyssey
{
    // Keep the existing command compatible with the full floor-plan walkthrough.
    public sealed class HousingSmoke : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-housingSmoke")<0)return;
            if(string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WESLEY_OUTPUT")))
                Environment.SetEnvironmentVariable("WESLEY_OUTPUT",Environment.GetEnvironmentVariable("HOUSING_OUTPUT"));
            new GameObject("Housing verification").AddComponent<WesleyPlanSmoke>();
        }
    }
}
