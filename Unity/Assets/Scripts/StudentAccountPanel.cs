using UnityEngine;

namespace AlbionOdyssey
{
    public sealed class StudentAccountPanel : MonoBehaviour
    {
        OdysseyGame game;
        StudentAccountService account;
        string email = "", code = "", displayName = "", loadedId = "", loadedName = "";
        bool createAccount, keyboard;
        int focus, keyboardFocus, editing;
        GUIStyle title, text, field, button;
        const string Keys = "abcdefghijklmnopqrstuvwxyz0123456789@._-+";
        public bool IsOpen => game != null && game.life.panel == "account";
        public void Setup(OdysseyGame owner) { game = owner; account = owner.accounts; }
        public void Open() { focus = 0; keyboard = false; game.tour.StopMedia(); game.life.SetPanel("account"); }
        void Close() { code = ""; keyboard = false; game.shell.ShowLaunch(); }
        public bool HandleInput()
        {
            if (!IsOpen && Input.GetKeyDown(KeyCode.F7)) { Open(); return true; }
            if (!IsOpen) return false;
            if (Input.GetKeyDown(KeyCode.Escape)) { if (keyboard) keyboard = false; else Close(); return true; }
            // Let native text fields receive arrows/Return while a person is typing.
            bool typing = GUIUtility.keyboardControl != 0 && !keyboard;
            if (!typing && AlbionUIInput.Poll(out var x, out var y, out var choose, out var back))
            {
                if (back) { if (keyboard) keyboard = false; else Close(); return true; }
                if (keyboard)
                {
                    if (x != 0 || y != 0) keyboardFocus = (keyboardFocus + x - y * 10 + Keys.Length + 3) % (Keys.Length + 3);
                    if (choose) Key(keyboardFocus);
                }
                else
                {
                    int count = account.SignedIn ? 5 : 6;
                    if (x != 0 || y != 0) focus = (focus + (y != 0 ? -y : x) + count) % count;
                    if (choose) Activate(focus);
                }
            }
            return true;
        }
        void Activate(int action)
        {
            if (account.Busy) return;
            if (account.SignedIn)
            {
                if (action == 0) Edit(2);
                else if (action == 1) account.SaveDisplayName(displayName);
                else if (action == 2) account.ReloadProfile();
                else if (action == 3) { account.SignOut(); code = ""; loadedId = ""; }
                else Close();
            }
            else
            {
                if (action == 0) Edit(0);
                else if (action == 1) account.RequestCode(email, createAccount);
                else if (action == 2) Edit(1);
                else if (action == 3) { if (account.CodeMatchesEmail(email)) { account.VerifyCode(code); code = ""; } }
                else if (action == 4) createAccount = !createAccount;
                else Close();
            }
        }
        void Edit(int target) { editing = target; keyboard = true; keyboardFocus = 0; GUI.FocusControl(null); }
        void Key(int index)
        {
            string value = editing == 0 ? email : editing == 1 ? code : displayName;
            if (index == Keys.Length + 2) { keyboard = false; return; }
            if (index == Keys.Length) { if (value.Length > 0) value = value.Substring(0, value.Length - 1); }
            else if (index == Keys.Length + 1) { if (editing == 2) value += " "; }
            else if (editing != 1 || char.IsDigit(Keys[index])) value += Keys[index];
            int limit = editing == 0 ? 254 : editing == 1 ? 10 : 24;
            if (value.Length > limit) value = value.Substring(0, limit);
            if (editing == 0) email = value; else if (editing == 1) code = value; else displayName = value;
        }
        bool Action(Rect rect, int index, string label)
        {
            var old = GUI.backgroundColor; var oldContent = GUI.contentColor;
            GUI.backgroundColor = focus == index ? AlbionUITheme.Gold : AlbionUITheme.Purple;
            GUI.contentColor = focus == index ? new Color(.12f,.08f,.17f) : Color.white;
            GUI.backgroundColor=Color.white;GUI.contentColor=Color.white;
            bool hit = OdysseyUI.Button(rect,label,"account-"+index,focus==index,index==1||index==3); GUI.backgroundColor = old; GUI.contentColor = oldContent;
            if (hit) { focus = index; GUI.FocusControl(null); Activate(index); }
            return hit;
        }
        void OnGUI()
        {
            if (!IsOpen) return;
            if (!account.SignedIn) loadedId = "";
            if (account.SignedIn && account.ProfileLoaded && (loadedId != account.UserId || loadedName != account.DisplayName)) { loadedId = account.UserId; loadedName = account.DisplayName; displayName = account.DisplayName; }
            if (title == null)
            {
                title = new GUIStyle(GUI.skin.label) { font = AlbionUITheme.DisplayFont, fontSize = 36, fontStyle = FontStyle.Bold };
                text = new GUIStyle(GUI.skin.label) { font = AlbionUITheme.BodyFont, fontSize = 18, wordWrap = true, richText = false };
                text.normal.textColor = Color.white; title.normal.textColor = Color.white;
                field = new GUIStyle(GUI.skin.textField) { font = AlbionUITheme.BodyFont, fontSize = 22, padding = new RectOffset(14,14,8,8), richText = false };
                button = AlbionUITheme.Button(17);
            }
            var previous = GUI.matrix; int depth = GUI.depth; var oldColor = GUI.color; var oldContent = GUI.contentColor; var oldBackground = GUI.backgroundColor; bool oldEnabled = GUI.enabled; GUI.depth = -40; GUI.contentColor = Color.white;
            float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 800f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
            float w = Screen.width / scale, h = Screen.height / scale, x = (w - 720) / 2;
            GUI.color = new Color(.025f,.028f,.052f); GUI.DrawTexture(new Rect(0,0,w,h),Texture2D.whiteTexture);
            GUI.color = AlbionUITheme.Gold; GUI.DrawTexture(new Rect(x,58,720,4),Texture2D.whiteTexture); GUI.color = Color.white;
            GUI.Label(new Rect(x,80,720,52),account.SignedIn ? "YOUR STUDENT ACCOUNT" : createAccount ? "JOIN ALBION ODYSSEY" : "WELCOME BACK", title);
            GUI.Label(new Rect(x,143,720,58),account.SignedIn ? "Verified email · " + account.Email : "Your email. One code. Your own online identity.\nUse a fresh code whenever you need to sign in again.",text);
            GUI.enabled = !account.Busy && account.Configured && !keyboard;
            if (account.SignedIn)
            {
                GUI.Label(new Rect(x,220,500,28),"DISPLAY NAME",text);
                displayName = GUI.TextField(new Rect(x,254,510,48),displayName,24,field);
                Action(new Rect(x+530,254,190,48),0,"Keyboard");
                Action(new Rect(x,325,350,50),1,"Save profile"); Action(new Rect(x+370,325,350,50),2,"Reload profile");
                Action(new Rect(x,400,350,50),3,"Sign out"); Action(new Rect(x+370,400,350,50),4,"Return to campus menu");
                GUI.Label(new Rect(x,488,720,72),"This is your online game account. Campus membership and teacher permissions are assigned separately. Local Keeper progress is stored on this device.",text);
            }
            else
            {
                GUI.Label(new Rect(x,218,500,26),"EMAIL ADDRESS",text);
                email = GUI.TextField(new Rect(x,251,510,46),email,254,field); Action(new Rect(x+530,251,190,46),0,"Keyboard");
                bool enabled = GUI.enabled; GUI.enabled = enabled && account.ResendSeconds == 0;
                Action(new Rect(x,315,720,48),1,account.ResendSeconds > 0 ? "Resend in " + account.ResendSeconds + "s" : "Send email code"); GUI.enabled = enabled;
                GUI.Label(new Rect(x,389,500,26),"EMAIL CODE",text);
                code = GUI.TextField(new Rect(x,420,510,46),code,10,field); Action(new Rect(x+530,420,190,46),2,"Keyboard");
                GUI.enabled = enabled && account.CodeMatchesEmail(email); Action(new Rect(x,484,720,48),3,"Verify & sign in"); GUI.enabled = enabled;
                Action(new Rect(x,550,350,44),4,createAccount ? "Have an account? Sign in" : "New here? Create account");
                Action(new Rect(x+370,550,350,44),5,"Continue as guest");
            }
            GUI.enabled = true;
            GUI.Label(new Rect(x,640,720,72),account.Status,text);
            if(account.Busy)OdysseyUI.Spinner(new Rect(x+670,715,36,36));
            GUI.Label(new Rect(x,738,720,36),"F7  Account    ·    Esc  Back    ·    Stick / arrows & Select",text);
            if (keyboard)
            {
                GUI.color = new Color(.05f,.055f,.10f); GUI.DrawTexture(new Rect(x-10,342,740,280),Texture2D.whiteTexture); GUI.color = Color.white;
                GUI.Label(new Rect(x,350,720,28),"TYPE WITH YOUR CONTROLLER",text);
                string inputValue = editing == 0 ? email : editing == 1 ? code : displayName;
                if (inputValue.Length > 48) inputValue = "…" + inputValue.Substring(inputValue.Length - 48);
                GUI.Label(new Rect(x,380,720,30),inputValue + "│",text);
                for (int i=0;i<Keys.Length+3;i++)
                {
                    string label = i < Keys.Length ? Keys[i].ToString() : i == Keys.Length ? "Del" : i == Keys.Length+1 ? "Space" : "Done";
                    GUI.backgroundColor = i == keyboardFocus ? AlbionUITheme.Gold : AlbionUITheme.Purple;
                    GUI.contentColor = i == keyboardFocus ? new Color(.12f,.08f,.17f) : Color.white;
                    GUI.backgroundColor=Color.white;GUI.contentColor=Color.white;
                    if (OdysseyUI.Button(new Rect(x+(i%10)*72,416+(i/10)*39,68,35),label,"account-key-"+i,i==keyboardFocus)) Key(i);
                }
                GUI.backgroundColor = Color.white;
            }
            GUI.matrix = previous; GUI.depth = depth; GUI.color = oldColor; GUI.contentColor = oldContent; GUI.backgroundColor = oldBackground; GUI.enabled = oldEnabled;
        }
    }
}
