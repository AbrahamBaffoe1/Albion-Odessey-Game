using System;
namespace AlbionOdyssey
{
    public static class PlaytestMode
    {
        static readonly string[] Flags={"-adventureSmoke","-journeySmoke","-storeSmoke","-consoleUiSmoke","-odysseySmoke","-tourSmoke","-shellSmoke","-blueprintSmoke","-combinedSmoke","-craftSmoke","-accountSmoke","-presentationSmoke","-vehicleSmoke","-impactSmoke","-launchArtBake","-socialSmoke","-housingSmoke","-realismSmoke","-wesleyPlanSmoke","-seatonPlanSmoke","-forestSmoke","-architectureSmoke","-natureTrailSmoke", "-whitehousePlanSmoke","-mitchellPlanSmoke"};
        public static string Name { get {foreach(var flag in Flags)if(Array.IndexOf(Environment.GetCommandLineArgs(),flag)>=0)return flag.Substring(1);return "";} }
        public static bool Active=>Name.Length>0;
    }
}
