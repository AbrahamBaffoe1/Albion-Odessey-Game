using UnityEngine;

namespace AlbionOdyssey
{
    public sealed class StudentAccountPanel : MonoBehaviour
    {
        OdysseyGame game;
        StudentAccountService account;
        string email = "", code = "", displayName = "", loadedId = "", loadedName = "";
        bool createAccount, keyboard, previousCodeStep;
        int focus, keyboardFocus, editing;
        GUIStyle title, text, field, button;
        const string Keys = "abcdefghijklmnopqrstuvwxyz0123456789@._-+";
        public bool IsOpen => game != null && game.life.panel == "account";
        public void Setup(OdysseyGame owner) { game = owner; account = owner.accounts; }
        public void Open() { focus = 0; keyboard = false; game.tour.StopMedia(); game.life.SetPanel("account"); }
        void Close() { code = ""; keyboard = false; game.shell.ShowLaunch(); }
        public bool HandleInput()
        {
            if (!IsOpen && (Input.GetKeyDown(KeyCode.F7) || CampusMenuShortcuts.Pressed(KeyCode.A,game))) { Open(); return true; }
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
                    int[] actions=account.SignedIn?new[]{0,1,2,7,4,5,6,3}:account.CodeMatchesEmail(email)?new[]{2,3,1,4}:new[]{0,1,5};
                    int at=System.Array.IndexOf(actions,focus);if(at<0)at=0;
                    if(x!=0||y!=0)focus=actions[(at+(y!=0?-y:x)+actions.Length)%actions.Length];
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
                else if(action==5)account.GenerateAvatar();
                else if(action==6)account.SaveAvatar(game.campus.skin,game.campus.outfit,game.campus.hair,game.campus.backpack);
                else if(action==7)game.shared.Open();
                else Close();
            }
            else
            {
                if (action == 0) Edit(0);
                else if (action == 1) account.RequestCode(email, createAccount);
                else if (action == 2) Edit(1);
                else if (action == 3) { if (account.CodeMatchesEmail(email)) { account.VerifyCode(code); } }
                else if (action == 4) { account.ChangeEmail(); code=""; GUI.FocusControl("account-email"); }
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
            if (account.SignedIn && account.ProfileLoaded && (loadedId != account.UserId || loadedName != account.DisplayName)) { loadedId = account.UserId; loadedName = account.DisplayName; displayName = account.DisplayName; code=""; }
            if(field==null)field=ConsoleMenuStyle.Field();
            var previous=GUI.matrix;int depth=GUI.depth;bool oldEnabled=GUI.enabled;GUI.depth=-40;
            float scale=Mathf.Min(Screen.width/1280f,Screen.height/800f);GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);
            float w=Screen.width/scale,h=Screen.height/scale,x=(w-1120)/2;
            bool codeStep=account.CodeMatchesEmail(email);
            if(codeStep&&!previousCodeStep)focus=3;previousCodeStep=codeStep;
            ConsoleMenuStyle.Background(game,w,h);
            ConsoleMenuStyle.Heading(x,"PLAYER ACCOUNT",account.SignedIn?"You're signed in.":codeStep?"Check your inbox.":"Your next chapter starts here.",account.SignedIn?"Your online identity is ready. Choose where to go next.":codeStep?"Enter the newest email code, then press Enter or select Continue.":"New player or returning? One email code gets you into Albion Odyssey.");
            OdysseyUI.Card(new Rect(x,249,660,365),new Color(.035f,.030f,.023f,.96f));
            GUI.enabled=!account.Busy&&account.Configured&&!keyboard;
            if(account.SignedIn)
            {
                OdysseyUI.Text(new Rect(x+28,269,600,28),account.ProfileLoaded?"VERIFIED PLAYER":"SIGNED IN · LOADING PROFILE",14,OdysseyUI.Mint,true);
                GUI.SetNextControlName("account-name");displayName=GUI.TextField(new Rect(x+28,313,430,55),displayName,24,field);Action(new Rect(x+474,313,158,55),0,"Keyboard");
                Action(new Rect(x+28,388,288,51),1,"Save name");Action(new Rect(x+332,388,300,51),2,"Reload profile");
                Action(new Rect(x+28,455,288,51),7,"Play online");Action(new Rect(x+332,455,300,51),4,"Campus menu");
                Action(new Rect(x+28,530,288,48),5,"New avatar look");Action(new Rect(x+332,530,300,48),6,"Save avatar");
                Action(new Rect(x+746,585,320,48),3,"Sign out");
            }
            else if(!codeStep)
            {
                OdysseyUI.Text(new Rect(x+28,280,600,28),"01  /  YOUR EMAIL",14,OdysseyUI.Mint,true);
                GUI.SetNextControlName("account-email");email=GUI.TextField(new Rect(x+28,327,430,58),email,254,field);Action(new Rect(x+474,327,158,58),0,"Keyboard");
                bool enabled=GUI.enabled;GUI.enabled=enabled&&account.ResendSeconds==0;
                Action(new Rect(x+28,421,604,60),1,account.Busy?"Sending…":account.ResendSeconds>0?"Try again in "+account.ResendSeconds+"s":"Send my code  →");GUI.enabled=enabled;
                Action(new Rect(x+28,522,604,48),5,"Explore offline for now");
            }
            else
            {
                OdysseyUI.Text(new Rect(x+28,278,600,28),"02  /  ENTER YOUR CODE",14,OdysseyUI.Mint,true);
                OdysseyUI.Text(new Rect(x+28,311,600,42),email,18,OdysseyUI.Muted);
                GUI.SetNextControlName("account-code");code=GUI.TextField(new Rect(x+28,365,430,62),code,16,field);Action(new Rect(x+474,365,158,62),2,"Keyboard");
                Action(new Rect(x+28,449,604,61),3,account.Busy?"Verifying…":"Continue  →");
                bool enabled=GUI.enabled;GUI.enabled=enabled&&account.ResendSeconds==0;
                Action(new Rect(x+28,539,288,48),1,account.ResendSeconds>0?"Resend in "+account.ResendSeconds+"s":"Resend code");GUI.enabled=enabled;
                Action(new Rect(x+332,539,300,48),4,"Change email");
            }
            GUI.enabled=true;
            game.presentation?.DrawStudent(new Rect(x+716,235,370,335));
            OdysseyUI.Text(new Rect(x+738,553,350,30),account.SignedIn?account.DisplayName:"MAKE YOURSELF AT HOME",18,OdysseyUI.White,true);
            var statusColor=account.HasError?new Color(1,.51f,.45f):account.Busy?OdysseyUI.Mint:OdysseyUI.Muted;
            OdysseyUI.Card(new Rect(x,633,660,80),new Color(.035f,.030f,.023f,.96f));
            if(account.Busy)OdysseyUI.Spinner(new Rect(x+18,650,38,38));
            OdysseyUI.Text(new Rect(x+(account.Busy?70:24),647,account.Busy?562:612,58),account.Busy?account.Status+"\n"+account.BusyHint:account.Status,16,statusColor);
            ConsoleMenuStyle.Footer(x,h,"ENTER  Continue     ·     ESC  Back     ·     Arrow keys + Enter or controller to select");
            if(!account.Busy&&!keyboard&&Event.current.type==EventType.KeyDown&&(Event.current.keyCode==KeyCode.Return||Event.current.keyCode==KeyCode.KeypadEnter)&&GUIUtility.keyboardControl!=0){Activate(account.SignedIn?1:codeStep?3:1);Event.current.Use();}
            if(keyboard)
            {
                OdysseyUI.Card(new Rect(x,300,735,300),OdysseyUI.Navy);
                OdysseyUI.Text(new Rect(x+16,315,700,32),"ON-SCREEN KEYBOARD",18,OdysseyUI.White,true);
                string value=editing==0?email:editing==1?code:displayName;
                OdysseyUI.Text(new Rect(x+16,350,700,32),value+"│",20,OdysseyUI.White);
                for(int i=0;i<Keys.Length+3;i++){string label=i<Keys.Length?Keys[i].ToString():i==Keys.Length?"Del":i==Keys.Length+1?"Space":"Done";if(OdysseyUI.Button(new Rect(x+12+i%10*71,403+i/10*40,67,36),label,"account-key-"+i,i==keyboardFocus))Key(i);}
            }
            GUI.matrix=previous;GUI.depth=depth;GUI.enabled=oldEnabled;
        }
    }
}
