namespace AlbionOdyssey
{
    // One vocabulary for menu legends and action prompts. Keeping the labels in
    // one place prevents controller and XR screens from drifting apart.
    public static class AlbionControls
    {
        public static string MenuFooter(bool xr, string action)
        {
            return xr ? "POINT  Navigate   ·   TRIGGER  " + action + "   ·   MENU  Back" : "LEFT STICK  Navigate   ·   A / CROSS  " + action + "   ·   B / CIRCLE  Back";
        }

        public static string CompactMenuFooter(bool xr, string action)
        {
            return xr ? "POINT Navigate · TRIGGER " + action + " · MENU Back" : "STICK Navigate · A/CROSS " + action + " · B/CIRCLE Back";
        }

        public static string GameplayFooter(bool xr)
        {
            return xr ? "LEFT STICK  Move   ·   RIGHT STICK  Turn   ·   TRIGGER  Interact   ·   GRIP  Grab   ·   MENU  Pause" : "LEFT STICK  Move   ·   RIGHT STICK  Look   ·   A / CROSS  Jump   ·   B / CIRCLE  Interact   ·   MENU  Pause";
        }

        public static string Select(bool xr) { return xr ? "TRIGGER" : "A / CROSS"; }
        public static string Back(bool xr) { return xr ? "MENU" : "B / CIRCLE"; }
    }
}
