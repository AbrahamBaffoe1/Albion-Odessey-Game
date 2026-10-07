using UnityEngine;
namespace AlbionOdyssey
{
    // Explicit chords avoid Mac media keys and do not intercept typing in forms.
    public static class CampusMenuShortcuts
    {
        public static bool Pressed(KeyCode key, OdysseyGame game)
        {
            return game != null && game.Ready && (!game.life.PanelOpen || game.shell.OwnsPanel)
                && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                && Input.GetKeyDown(key);
        }
    }
}
