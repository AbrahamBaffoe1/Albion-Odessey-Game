using UnityEngine;

namespace AlbionOdyssey
{
    // Mac-friendly accessibility and input layer. Settings survive a restart and are
    // deliberately independent from the local Keeper save.
    public sealed class OdysseyAccessibility : MonoBehaviour
    {
        public static KeyCode ForwardKey { get; private set; } = KeyCode.W;
        public static KeyCode BackKey { get; private set; } = KeyCode.S;
        public static KeyCode LeftKey { get; private set; } = KeyCode.A;
        public static KeyCode RightKey { get; private set; } = KeyCode.D;
        public static KeyCode JumpKey { get; private set; } = KeyCode.Space;
        public static KeyCode InteractKey { get; private set; } = KeyCode.E;
        public static bool CaptionsEnabled { get; private set; } = true;
        public static bool LargeText { get; private set; }
        public static bool HighContrast { get; private set; }
        public static bool ReducedMotion { get; private set; }
        static readonly string[] bindingNames={"Forward","Back","Left","Right","Jump","Interact"};

        OdysseyGame game; bool open; float openedAt;int focus,rebinding=-1;string rebindMessage=""; GUIStyle title, text, button;
        public void Setup(OdysseyGame owner)
        {
            game = owner; CaptionsEnabled = PlayerPrefs.GetInt("Odyssey.Captions", 1) == 1; LargeText = PlayerPrefs.GetInt("Odyssey.LargeText", 0) == 1; HighContrast = PlayerPrefs.GetInt("Odyssey.HighContrast", 0) == 1; ReducedMotion = PlayerPrefs.GetInt("Odyssey.ReducedMotion", 0) == 1;
            bool alternate = PlayerPrefs.GetInt("Odyssey.AlternateKeys", 0) == 1; ApplyKeys(alternate);
        }
        static KeyCode StoredKey(string name,KeyCode fallback){int value=PlayerPrefs.GetInt("Odyssey.Key."+name,(int)fallback);return System.Enum.IsDefined(typeof(KeyCode),value)?(KeyCode)value:fallback;}
        public static bool InteractPressed(){return Input.GetKeyDown(InteractKey)||Input.GetKeyDown(KeyCode.F)||Input.GetKeyDown(KeyCode.JoystickButton1);}
        public static string InteractLabel=>InteractKey==KeyCode.E?"E / F":InteractKey==KeyCode.F?"F":InteractKey.ToString();
        static void ApplyKeys(bool alternate)
        {
            ForwardKey=StoredKey("Forward",alternate?KeyCode.I:KeyCode.W);BackKey=StoredKey("Back",alternate?KeyCode.K:KeyCode.S);LeftKey=StoredKey("Left",alternate?KeyCode.J:KeyCode.A);RightKey=StoredKey("Right",alternate?KeyCode.L:KeyCode.D);JumpKey=StoredKey("Jump",KeyCode.Space);InteractKey=StoredKey("Interact",KeyCode.E);
        }
        void Save(string key, int value) { PlayerPrefs.SetInt(key, value); PlayerPrefs.Save(); }
        public bool HandleInput()
        {
            if (!open && Input.GetKeyDown(KeyCode.F4)) { open = true; focus=0; openedAt=Time.unscaledTime; game.player.controls = false; Cursor.lockState = CursorLockMode.None; Cursor.visible = true; return true; }
            if (!open) return false;
            if(rebinding>=0)return true;
            if(AlbionUIInput.Poll(out var horizontal,out var vertical,out var choose,out var cancel)){if(cancel){ClosePanel();}else{const int count=13;if(vertical!=0)focus=(focus+(vertical>0?-1:1)+count)%count;if(choose){if(focus==0){CaptionsEnabled=!CaptionsEnabled;Save("Odyssey.Captions",CaptionsEnabled?1:0);}else if(focus==1){LargeText=!LargeText;Save("Odyssey.LargeText",LargeText?1:0);}else if(focus==2){HighContrast=!HighContrast;Save("Odyssey.HighContrast",HighContrast?1:0);}else if(focus==3){ReducedMotion=!ReducedMotion;Save("Odyssey.ReducedMotion",ReducedMotion?1:0);}else if(focus==4){bool alternate=PlayerPrefs.GetInt("Odyssey.AlternateKeys",0)==1;ApplyKeys(!alternate);Save("Odyssey.AlternateKeys",!alternate?1:0);}else if(focus>=5&&focus<=10)BeginRebind(focus-5);else if(focus==11)ResetBindings();else ClosePanel();}}return true;}
            if (Input.GetKeyDown(KeyCode.Escape)) { ClosePanel(); return true; }
            return true;
        }
        void ClosePanel(){open=false;game.player.controls=!game.building&&!game.life.PanelOpen;Cursor.lockState=game.player.pointerControls?CursorLockMode.None:CursorLockMode.Locked;Cursor.visible=game.player.pointerControls;}
        KeyCode Binding(int index){switch(index){case 0:return ForwardKey;case 1:return BackKey;case 2:return LeftKey;case 3:return RightKey;case 4:return JumpKey;default:return InteractKey;}}
        void BeginRebind(int index){rebinding=Mathf.Clamp(index,0,5);rebindMessage="Press a key for "+bindingNames[rebinding]+". Esc cancels.";}
        void ResetBindings(){for(int i=0;i<bindingNames.Length;i++)PlayerPrefs.DeleteKey("Odyssey.Key."+bindingNames[i]);bool alternate=PlayerPrefs.GetInt("Odyssey.AlternateKeys",0)==1;ApplyKeys(alternate);PlayerPrefs.Save();rebindMessage="Default keyboard controls restored.";}
        void ApplyBinding(int index,KeyCode key)
        {
            if(key==KeyCode.None)return;
            for(int i=0;i<6;i++)if(i!=index&&Binding(i)==key){rebindMessage=key+" is already assigned to "+bindingNames[i]+".";return;}
            switch(index){case 0:ForwardKey=key;break;case 1:BackKey=key;break;case 2:LeftKey=key;break;case 3:RightKey=key;break;case 4:JumpKey=key;break;default:InteractKey=key;break;}
            PlayerPrefs.SetInt("Odyssey.Key."+bindingNames[index],(int)key);PlayerPrefs.Save();rebindMessage=bindingNames[index]+" set to "+key+".";
        }
        void OnGUI()
        {
            if (!open || game == null) return;
            if (title == null) { title = new GUIStyle(GUI.skin.label) { font=AlbionUITheme.DisplayFont,fontSize = AlbionUITheme.TextSize(27), fontStyle = FontStyle.Bold }; title.normal.textColor=new Color(.96f,.94f,.86f); text = new GUIStyle(GUI.skin.label) { font=AlbionUITheme.BodyFont,fontSize = AlbionUITheme.TextSize(17), wordWrap = true }; text.normal.textColor=new Color(.88f,.88f,.92f); button = AlbionUITheme.Button(16); }
            if(rebinding>=0&&Event.current.type==EventType.KeyDown){if(Event.current.keyCode==KeyCode.Escape){rebinding=-1;rebindMessage="Rebinding cancelled.";Event.current.Use();}else if(Event.current.keyCode!=KeyCode.None){ApplyBinding(rebinding,Event.current.keyCode);rebinding=-1;Event.current.Use();}}
            float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 800f); GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1)); GUI.matrix=AlbionUITheme.Slide(GUI.matrix,openedAt,ReducedMotion); float w = Screen.width / scale, h = Screen.height / scale; Rect safe = AlbionUITheme.SafeArea(scale);
            GUI.color = HighContrast ? Color.black : new Color(.025f, .028f, .052f, .98f); GUI.DrawTexture(new Rect(0, 0, w, h), Texture2D.whiteTexture); GUI.color = Color.white;
            float x = Mathf.Clamp((w - 780) * .5f, safe.xMin + 24, safe.xMax - 780 - 24); GUI.Label(new Rect(x, 70, 740, 48), "ACCESSIBILITY & CONTROLS", title); GUI.Label(new Rect(x, 125, 740, 54), "F4 opens this panel. Settings are saved for this Mac. Gamepad left stick moves, right stick looks, A/Cross jumps and B/Circle interacts.", text);
            if (GUI.Button(new Rect(x, 210, 360, 46), (CaptionsEnabled ? "✓ " : "○ ") + "Captions and achievement text", button)) { CaptionsEnabled = !CaptionsEnabled; Save("Odyssey.Captions", CaptionsEnabled ? 1 : 0); }
            if (GUI.Button(new Rect(x, 270, 360, 46), (LargeText ? "✓ " : "○ ") + "Large readable interface text", button)) { LargeText = !LargeText; Save("Odyssey.LargeText", LargeText ? 1 : 0); }
            if (GUI.Button(new Rect(x, 330, 360, 46), (HighContrast ? "✓ " : "○ ") + "High contrast panels", button)) { HighContrast = !HighContrast; Save("Odyssey.HighContrast", HighContrast ? 1 : 0); }
            if (GUI.Button(new Rect(x, 390, 360, 46), (ReducedMotion ? "✓ " : "○ ") + "Reduce interface and NPC motion", button)) { ReducedMotion = !ReducedMotion; Save("Odyssey.ReducedMotion", ReducedMotion ? 1 : 0); }
            bool alternate = PlayerPrefs.GetInt("Odyssey.AlternateKeys", 0) == 1;
            if (GUI.Button(new Rect(x, 470, 360, 46), (alternate ? "✓ " : "○ ") + (alternate ? "I J K L movement" : "W A S D movement"), button)) { alternate = !alternate; ApplyKeys(alternate); Save("Odyssey.AlternateKeys", alternate ? 1 : 0); }
            GUI.Label(new Rect(x + 405, 205, 360, 30), "KEYBOARD SHORTCUTS", text);
            for(int i=0;i<6;i++){int row=248+i*43;string label=rebinding==i?"PRESS A KEY…":Binding(i).ToString();if(GUI.Button(new Rect(x+405,row,160,35),label,button))BeginRebind(i);GUI.Label(new Rect(x+575,row+4,170,28),bindingNames[i],text);}
            if(rebindMessage.Length>0)GUI.Label(new Rect(x+405,510,360,42),rebindMessage,text);
            if(GUI.Button(new Rect(x+405,560,170,42),"Reset keys",button))ResetBindings();
            if(GUI.Button(new Rect(x+585,560,180,42),"Close · F4 / Esc",button))ClosePanel();
        }
    }
}
