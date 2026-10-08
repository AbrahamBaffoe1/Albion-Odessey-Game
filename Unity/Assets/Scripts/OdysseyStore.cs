using UnityEngine;
namespace AlbionOdyssey
{
    // Purchases are earned-currency, per keeper and local to this saved game.
    public sealed class OdysseyStore : MonoBehaviour
    {
        public static readonly string[] Names={"MIDNIGHT SCHOLAR","GLACIER EDITION","CRIMSON LEGACY","WHITEHOUSE GREEN","TWILIGHT SOCIETY","GOLDEN HOUR"};
        static readonly int[] Featured={1,2,3,4,5,6},Alphabetical={3,2,6,1,5,4};
        int[] Order=>category==1?Alphabetical:Featured;
        static readonly string[] Stories={"Quiet confidence. A deep midnight finish for the campus after dark.","Silver-blue fabric inspired by a Michigan winter morning.","A rich crimson finish for a new chapter in your Albion story.","A woodland finish inspired by the trails of Whitehouse Nature Center.","A dusky violet finish for the last light over the quad.","Bring the warmth of an autumn afternoon wherever you explore."};
        OdysseyGame game;GameObject stage;KeeperAvatar model;Camera camera;RenderTexture[] cards;RenderTexture detail;
        int selected=1,category,focus,previewAppearance=-1;bool confirm,dirty=true,cardsDirty=true;string returnPanel="launch",message="";float lastRender;
        internal bool Confirming=>confirm;
        public bool IsOpen=>game!=null&&game.life.panel=="store";
        public void Setup(OdysseyGame owner){game=owner;}
        public void Open()
        {
            GUIUtility.keyboardControl=0;returnPanel=game.life.panel;game.tour.StopMedia();game.life.SetPanel("store");confirm=false;message="";focus=0;dirty=true;cardsDirty=true;
            if(stage==null)CreatePreview();
        }
        public void Close(){confirm=false;game.life.SetPanel(returnPanel=="pause"?"pause":"launch");}
        public bool Purchase(int id)
        {
            var keeper=game.state.Current;int balance=keeper.acorns,owned=keeper.ownedFinishes;
            if(!keeper.BuyFinish(id)){message=keeper.OwnsFinish(id)?"Already in your collection.":"Explore Legacy Hall to earn more acorns.";return false;}
            if(!game.Save()){keeper.acorns=balance;keeper.ownedFinishes=owned;message="Purchase not saved. Your acorns have been returned. Try again.";return false;}
            message="Added to your collection. Select Equip to wear it.";return true;
        }
        public bool Equip(int id)
        {
            var keeper=game.state.Current;int previous=keeper.equippedFinish;
            if(!keeper.EquipFinish(id))return false;
            if(!game.Save()){keeper.equippedFinish=previous;message="Could not save your selection. Try again.";return false;}
            game.campus.RefreshAvatar();message=id==0?"Original outfit restored.":"Finish equipped. Ready for campus.";return true;
        }
        void Select(int id){selected=id;message="";confirm=false;dirty=true;}
        bool Visible(int id)=>category!=2||game.state.Current.OwnsFinish(id);
        void Category(int value)
        {
            category=value;confirm=false;message="";
            if(category==2)for(int i=1;i<=6;i++)if(game.state.Current.OwnsFinish(i)){Select(i);break;}
        }
        void Step(int direction)
        {
            int index=System.Array.IndexOf(Order,selected);
            for(int n=1;n<=6;n++){int id=Order[(index+direction*n+36)%6];if(Visible(id)){Select(id);break;}}
        }
        void Action()
        {
            if(category==2&&game.state.Current.ownedFinishes==0){Category(1);return;}
            if(game.state.Current.OwnsFinish(selected))Equip(game.state.Current.equippedFinish==selected?0:selected);
            else if(game.state.Current.acorns>=Keeper.FinishCost(selected))confirm=true;
            else message="Find golden memories in Legacy Hall. Each one earns 3 acorns.";
        }
        public bool HandleInput()
        {
            if(!IsOpen)return false;
            if(AlbionUIInput.Poll(out int horizontal,out int vertical,out bool choose,out bool cancel))Navigate(horizontal,vertical,choose,cancel);
            return true;
        }
        internal void Navigate(int horizontal,int vertical,bool choose,bool cancel)
        {
            if(!IsOpen)return;
            if(cancel){if(confirm)confirm=false;else Close();return;}
            if(confirm){if(choose){Purchase(selected);confirm=false;}return;}
            if(horizontal!=0)Step(horizontal>0?1:-1);
            if(vertical!=0)focus=(focus+(vertical>0?2:1))%3;
            if(choose){if(focus==0)Action();else if(focus==1)Category((category+1)%3);else Close();}
        }
        void CreatePreview()
        {
            stage=new GameObject("Store fitting studio");stage.transform.position=new Vector3(40,-1500,0);
            model=new GameObject("Actual student finish preview").AddComponent<KeeperAvatar>();model.transform.SetParent(stage.transform,false);
            camera=new GameObject("Store preview camera").AddComponent<Camera>();camera.transform.SetParent(stage.transform,false);camera.transform.localPosition=new Vector3(0,1.3f,4.5f);camera.transform.LookAt(stage.transform.position+Vector3.up*1.05f);
            camera.fieldOfView=29;camera.cullingMask=1<<29;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;camera.enabled=false;
            for(int i=0;i<2;i++){var light=new GameObject("Fitting light").AddComponent<Light>();light.transform.SetParent(stage.transform,false);light.transform.localPosition=new Vector3(i==0?-2:2,3,3);light.type=LightType.Point;light.range=8;light.intensity=2.5f;light.color=i==0?new Color(1,.9f,.78f):new Color(.5f,.78f,1);light.cullingMask=1<<29;}
            cards=new RenderTexture[6];for(int i=0;i<6;i++){cards[i]=new RenderTexture(280,360,24);cards[i].Create();}
            detail=new RenderTexture(600,800,24);detail.Create();
        }
        void Dress(int id)
        {
            int appearance=game.campus.skin+game.campus.outfit*10+game.campus.hair*100+(game.campus.backpack?1000:0);
            if(previewAppearance!=appearance){previewAppearance=appearance;model.Build(game.campus.skin,game.campus.outfit,game.campus.hair,game.campus.backpack);}
            model.ApplyFinish(id);
            foreach(var child in model.GetComponentsInChildren<Transform>(true))child.gameObject.layer=29;
            model.transform.localRotation=Quaternion.Euler(0,-12,0);
        }
        void RenderPreviews()
        {
            if(Event.current.type!=EventType.Repaint)return;
            if(cardsDirty){for(int i=0;i<6;i++){Dress(i+1);camera.targetTexture=cards[i];camera.Render();}cardsDirty=false;}
            if(dirty){Dress(selected);dirty=false;}
            if(Time.unscaledTime-lastRender>.05f){lastRender=Time.unscaledTime;model.transform.localRotation=Quaternion.Euler(0,OdysseyAccessibility.ReducedMotion?-12:Mathf.Sin(Time.unscaledTime*.35f)*18,0);camera.targetTexture=detail;camera.Render();}
        }
        void OnDestroy(){if(stage!=null)Destroy(stage);if(cards!=null)foreach(var card in cards){card.Release();Destroy(card);}if(detail!=null){detail.Release();Destroy(detail);}}
        void OnGUI()
        {
            if(!IsOpen)return;OdysseyCinematic.ConsumeMenuKeys();RenderPreviews();
            var matrix=GUI.matrix;int depth=GUI.depth;var color=GUI.color;GUI.color=Color.white;GUI.depth=-100;
            float scale=Mathf.Min(Screen.width/1440f,Screen.height/900f);GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);float w=Screen.width/scale,h=Screen.height/scale,x=(w-1320)/2;
            OdysseyUI.Fill(new Rect(0,0,w,h),new Color(.012f,.016f,.02f));
            OdysseyUI.Text(new Rect(x,32,650,40),"STORE",32,OdysseyUI.Gold);
            OdysseyUI.Text(new Rect(x,73,650,28),category==2?"YOUR COLLECTION":"THE CAMPUS COLLECTION",19,OdysseyUI.White);
            OdysseyUI.Text(new Rect(x+995,38,325,35),"ACORNS   /   "+game.state.Current.acorns.ToString("00"),25,OdysseyUI.Mint);
            OdysseyUI.Text(new Rect(x+995,78,325,26),"EARNED ON YOUR ODYSSEY",13,OdysseyUI.Muted);
            OdysseyUI.Fill(new Rect(x,119,1320,1),OdysseyUI.Gold);
            bool wasEnabled=GUI.enabled;GUI.enabled=wasEnabled&&!confirm;
            string[] categories={"FEATURED","ALL FINISHES","OWNED"};
            for(int i=0;i<3;i++)if(OdysseyUI.Button(new Rect(x,139+i*86,145,76),categories[i],"store-cat"+i,category==i)){Category(i);}
            OdysseyUI.Text(new Rect(x,424,144,120),"WEAR YOUR\nNEXT CHAPTER",20,OdysseyUI.Muted);
            OdysseyUI.Text(new Rect(x,622,143,145),"Cosmetic finishes.\nNo gameplay advantage.\n\nSaved on this Mac.",14,OdysseyUI.Muted);
            float content=x+167;
            OdysseyUI.Fill(new Rect(content,139,710,163),new Color(.035f,.095f,.13f));
            GUI.BeginGroup(new Rect(content+452,139,258,163));GUI.DrawTexture(new Rect(10,-15,220,285),cards[selected-1],ScaleMode.ScaleToFit,true);GUI.EndGroup();
            OdysseyUI.Text(new Rect(content+24,155,510,22),"ALBION ORIGINALS / 01",13,OdysseyUI.Mint);
            OdysseyUI.Text(new Rect(content+24,185,510,48),Names[selected-1],32,OdysseyUI.White);
            OdysseyUI.Text(new Rect(content+24,241,500,40),"Discover. Collect. Make it yours.",19,OdysseyUI.Mint);
            if(OdysseyUI.Button(new Rect(content+576,240,48,40),"‹","store-prev"))Step(-1);
            if(OdysseyUI.Button(new Rect(content+636,240,48,40),"›","store-next"))Step(1);
            int index=0;
            foreach(int id in Order)
            {
                if(!Visible(id))continue;
                Rect cell=new Rect(content+(index%3)*240,323+(index/3)*223,230,211);index++;
                OdysseyUI.Fill(cell,selected==id?new Color(.035f,.14f,.19f):new Color(.025f,.03f,.037f));
                OdysseyUI.Fill(new Rect(cell.x,cell.y,cell.width,1),OdysseyUI.Mint);
                GUI.DrawTexture(new Rect(cell.x+40,cell.y+8,150,156),cards[id-1],ScaleMode.ScaleToFit,true);
                OdysseyUI.Text(new Rect(cell.x+12,cell.y+9,215,24),game.state.Current.OwnsFinish(id)?"OWNED":Keeper.FinishCost(id)+" ACORNS",13,OdysseyUI.Mint);
                if(OdysseyUI.Button(new Rect(cell.x,cell.y+168,cell.width,43),Names[id-1],"store-item"+id,selected==id))Select(id);
            }
            if(index==0)OdysseyUI.Text(new Rect(content+25,395,650,130),"YOUR COLLECTION STARTS HERE\nBrowse finishes and spend the acorns you earn exploring.",25,OdysseyUI.Muted);
            float rx=x+901;
            OdysseyUI.Fill(new Rect(rx,139,419,480),new Color(.023f,.033f,.042f));
            GUI.DrawTexture(new Rect(rx+35,149,349,464),detail,ScaleMode.ScaleToFit,true);
            OdysseyUI.Text(new Rect(rx+18,151,380,24),"LIVE FITTING / STUDENT FINISH",13,OdysseyUI.Mint);
            OdysseyUI.Text(new Rect(rx,632,419,52),Names[selected-1],29,OdysseyUI.White);
            OdysseyUI.Text(new Rect(rx,688,419,67),Stories[selected-1],17,OdysseyUI.Muted);
            bool owned=game.state.Current.OwnsFinish(selected),equipped=game.state.Current.equippedFinish==selected;
            string action=equipped?"RESTORE ORIGINAL OUTFIT":owned?"EQUIP FINISH":game.state.Current.acorns<Keeper.FinishCost(selected)?"EARN MORE ACORNS":"UNLOCK / "+Keeper.FinishCost(selected)+" ACORNS";
            if(category==2&&index==0){if(OdysseyUI.Button(new Rect(rx,764,419,48),"BROWSE ALL FINISHES","store-empty",focus==0,true))Category(1);}
            else if(OdysseyUI.Button(new Rect(rx,764,419,48),action,"store-buy",focus==0,true))Action();
            OdysseyUI.Fill(new Rect(x,h-64,1320,1),OdysseyUI.Gold);
            if(OdysseyUI.Button(new Rect(x,h-49,145,35),"BACK","store-close",focus==2))Close();
            OdysseyUI.Text(new Rect(x+170,h-46,1140,43),message.Length>0?message:"← →  Browse items    /    ↑ ↓  Action, category, back    /    ENTER  Select    /    ESC  Back",16,message.Length>0?OdysseyUI.Mint:OdysseyUI.Muted);
            GUI.enabled=wasEnabled;
            if(confirm)
            {
                OdysseyUI.Fill(new Rect(0,0,w,h),new Color(0,0,0,.94f));float cx=w/2-300,cy=h/2-140;
                OdysseyUI.Card(new Rect(cx,cy,600,280),OdysseyUI.Surface);
                OdysseyUI.Text(new Rect(cx+28,cy+24,544,37),"UNLOCK "+Names[selected-1]+"?",27,OdysseyUI.White);
                OdysseyUI.Text(new Rect(cx+28,cy+84,544,78),"Spend "+Keeper.FinishCost(selected)+" earned acorns.\nBalance after purchase: "+(game.state.Current.acorns-Keeper.FinishCost(selected))+" acorns.",21,OdysseyUI.Mint);
                if(OdysseyUI.Button(new Rect(cx+28,cy+192,265,54),"CONFIRM UNLOCK","store-confirm",true,true)){Purchase(selected);confirm=false;}
                if(OdysseyUI.Button(new Rect(cx+308,cy+192,265,54),"CANCEL","store-cancel"))confirm=false;
            }
            GUI.matrix=matrix;GUI.depth=depth;GUI.color=color;
        }
    }
}
