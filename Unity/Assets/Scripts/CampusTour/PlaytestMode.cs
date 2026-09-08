using System;
namespace AlbionOdyssey
{
    public static class PlaytestMode
    {
        static readonly string[] Flags={"-odysseySmoke","-tourSmoke","-shellSmoke","-blueprintSmoke","-combinedSmoke","-craftSmoke"};
        public static string Name { get {foreach(var flag in Flags)if(Array.IndexOf(Environment.GetCommandLineArgs(),flag)>=0)return flag.Substring(1);return "";} }
        public static bool Active=>Name.Length>0;
    }
}
