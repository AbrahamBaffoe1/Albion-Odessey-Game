using UnityEngine;
namespace AlbionOdyssey
{
    public sealed class CampusVehicleRepairs : MonoBehaviour
    {
        OdysseyGame game;CampusCar selected;int focus;string message="";
        RenderTexture preview;Camera previewCamera;AudioSource sound;AudioClip crash,bodyHit,repair;
        public void Setup(OdysseyGame owner)
        {
            game=owner;sound=gameObject.AddComponent<AudioSource>();sound.playOnAwake=false;
            crash=MakeSound("Vehicle collision",.32f,75,true);bodyHit=MakeSound("Student impact",.15f,110,true);repair=MakeSound("Vehicle repaired",.5f,660,false);
        }
        AudioClip MakeSound(string name,float duration,float pitch,bool noise)
        {
            int count=(int)(44100*duration);var data=new float[count];var random=new System.Random(1835);
            for(int i=0;i<count;i++){float t=i/44100f,envelope=Mathf.Pow(1-t/duration,2);float frequency=noise?pitch*(1-t/duration*.5f):pitch*(t<duration*.45f?1:1.5f);data[i]=(Mathf.Sin(2*Mathf.PI*frequency*t)*(noise?.65f:1)+(noise?(float)(random.NextDouble()*2-1)*.3f:0))*envelope*.36f;}
            var clip=AudioClip.Create(name,count,1,44100,false);clip.SetData(data,0);return clip;
        }
        void Play(AudioClip clip){if(game.sound!=null&&!game.sound.Muted)sound.PlayOneShot(clip,game.sound.Volume);}
        public void ImpactSound(bool solid)=>Play(solid?crash:bodyHit);
        CampusCar Nearby()
        {
            if(game.player.vehicle!=null)return game.player.vehicle;
            CampusCar best=null;float distance=5;
            foreach(var car in game.campus.cars){float d=Vector3.Distance(car.transform.position,game.player.transform.position);if(d<distance){best=car;distance=d;}}
            return best;
        }
        public bool Open(CampusCar car)
        {
            if(car==null||Mathf.Abs(car.speed)>.5f){game.notice="Stop the car before opening repairs.";return false;}
            if(Vector3.Distance(car.transform.position,game.player.transform.position)>5)return false;
            selected=car;focus=0;message="";game.tour.StopMedia();game.life.SetPanel("repair");RenderPreview();return true;
        }
        public bool Pay(bool gems)
        {
            if(selected==null||game.life.panel!="repair")return false;
            if(Mathf.Abs(selected.speed)>.5f||Vector3.Distance(selected.transform.position,game.player.transform.position)>5){message="Return to the stopped car to repair it.";return false;}
            if(selected.Damage.amount==0){message="This car is already fully repaired.";return false;}
            if(gems?game.state.Current.Gems<selected.Damage.GemCost:game.state.Current.acorns<selected.Damage.CoinCost){message="Not enough "+(gems?"gems":"acorn coins")+". Collect golden memories to earn more.";return false;}
            int cost=gems?selected.Damage.GemCost:selected.Damage.CoinCost;
            if(!VehicleRepair.TryPay(game.state,selected.CarId,gems,game.Save)){message="Could not save your repair. Nothing was charged. Try again.";return false;}
            selected.Visual.ApplyDamage(selected.Damage);RenderPreview();Play(repair);message="Repair complete · "+cost+(gems?" gems":" acorn coins")+" paid. Saved.";game.notice=message;return true;
        }
        public bool HandleInput()
        {
            if(game.life.panel=="repair")
            {
                if(Input.GetKeyDown(KeyCode.Escape)){game.life.SetPanel("");return true;}
                if(AlbionUIInput.Poll(out int x,out int y,out bool choose,out bool cancel))
                {if(cancel)game.life.SetPanel("");else{if(x!=0||y!=0)focus=(focus+(x>0||y<0?1:2))%3;if(choose){if(focus==2)game.life.SetPanel("");else Pay(focus==1);}}}
                return true;
            }
            if(game.life.PanelOpen||game.building||game.journalOpen)return false;
            if(Input.GetKeyDown(KeyCode.R)){var car=Nearby();if(car!=null){Open(car);return true;}}
            return false;
        }
        void RenderPreview()
        {
            if(preview==null){preview=new RenderTexture(1000,560,24,RenderTextureFormat.ARGB32){antiAliasing=2};preview.Create();previewCamera=new GameObject("Repair inspection camera").AddComponent<Camera>();previewCamera.enabled=false;}
            previewCamera.CopyFrom(game.player.eyes);previewCamera.enabled=false;previewCamera.targetTexture=preview;previewCamera.fieldOfView=38;
            float direction=selected.Damage.dents.Length>0&&selected.Damage.dents[selected.Damage.dents.Length-1].z<0?-1:1;
            previewCamera.transform.position=selected.transform.TransformPoint(new Vector3(-4,2.4f,6*direction));previewCamera.transform.LookAt(selected.transform.position+Vector3.up*.7f);
            previewCamera.aspect=1000f/560;previewCamera.clearFlags=CameraClearFlags.SolidColor;previewCamera.backgroundColor=Color.clear;previewCamera.useOcclusionCulling=false;previewCamera.cullingMask=1<<28;
            // Inspect only the actual car, even when it has stopped against a wall.
            var parts=selected.Visual.Detail.GetComponentsInChildren<Transform>(true);var layers=new int[parts.Length];
            for(int i=0;i<parts.Length;i++){layers[i]=parts[i].gameObject.layer;parts[i].gameObject.layer=28;}
            try{previewCamera.Render();}finally{for(int i=0;i<parts.Length;i++)parts[i].gameObject.layer=layers[i];}
        }
        void OnGUI()
        {
            if(game==null||!game.Ready||game.loading.Busy)return;
            var matrix=GUI.matrix;float scale=Mathf.Min(Screen.width/1440f,Screen.height/900f);GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);float w=Screen.width/scale,h=Screen.height/scale;
            if(game.life.panel=="repair"&&selected!=null)
            {
                OdysseyUI.Fill(new Rect(0,0,w,h),OdysseyUI.Navy);float x=(w-1120)/2,y=(h-650)/2;
                OdysseyUI.Text(new Rect(x,y,600,24),"CAMPUS MOTOR WORKS",14,OdysseyUI.Mint,true);
                OdysseyUI.Text(new Rect(x,y+34,600,62),"BACK IN SHAPE.",44,OdysseyUI.White,true);
                OdysseyUI.Text(new Rect(x+770,y+12,350,32),game.state.Current.acorns+" ACORN COINS   /   "+game.state.Current.Gems+" GEMS",17,OdysseyUI.Gold,true);
                OdysseyUI.Card(new Rect(x,y+120,400,380),OdysseyUI.Surface);
                OdysseyUI.Text(new Rect(x+28,y+145,344,30),selected.name.ToUpperInvariant(),20,OdysseyUI.White,true);
                OdysseyUI.Text(new Rect(x+28,y+194,344,22),"BODY CONDITION",13,OdysseyUI.Muted,true);
                OdysseyUI.Text(new Rect(x+28,y+223,344,80),(100-selected.Damage.amount)+"%",64,selected.Damage.amount>60?OdysseyUI.Gold:OdysseyUI.Mint,true);
                OdysseyUI.Card(new Rect(x+28,y+314,344,8),OdysseyUI.Navy);OdysseyUI.Card(new Rect(x+28,y+314,344*(100-selected.Damage.amount)/100f,8),OdysseyUI.Mint);
                OdysseyUI.Text(new Rect(x+28,y+352,344,54),selected.Damage.amount==0?"Bodywork restored. Ready to drive.":"Reshape dented panels and restore full driving performance.",18,OdysseyUI.White);
                OdysseyUI.Text(new Rect(x+28,y+433,344,42),"Each golden memory earns 3 acorn coins and 1 gem.",15,OdysseyUI.Muted);
                OdysseyUI.Card(new Rect(x+420,y+120,700,380),OdysseyUI.Surface);
                if(preview!=null)GUI.DrawTexture(new Rect(x+430,y+130,680,360),preview,ScaleMode.ScaleToFit);
                OdysseyUI.Text(new Rect(x+446,y+140,570,28),selected.Damage.amount>0?"DAMAGE INSPECTION":"REPAIR COMPLETE",13,OdysseyUI.White,true);
                bool oldEnabled=GUI.enabled;
                GUI.enabled=selected.Damage.amount>0&&game.state.Current.acorns>=selected.Damage.CoinCost;
                if(OdysseyUI.Button(new Rect(x,y+524,400,62),"PAY "+selected.Damage.CoinCost+" ACORN COINS","repair-coins",focus==0,true))Pay(false);
                GUI.enabled=selected.Damage.amount>0&&game.state.Current.Gems>=selected.Damage.GemCost;
                if(OdysseyUI.Button(new Rect(x+420,y+524,340,62),"PAY "+selected.Damage.GemCost+" GEMS","repair-gems",focus==1))Pay(true);
                GUI.enabled=oldEnabled;if(OdysseyUI.Button(new Rect(x+780,y+524,340,62),"BACK TO CAMPUS  ·  ESC","repair-close",focus==2))game.life.SetPanel("");
                OdysseyUI.Text(new Rect(x,y+609,1120,40),message.Length>0?message:"Choose one payment method. Your repair and remaining balance are saved together.",17,OdysseyUI.Muted);
            }
            else if(!game.life.PanelOpen&&!game.building&&!game.journalOpen&&game.player.vehicle!=null)
            {
                var car=game.player.vehicle;float x=w-300,y=335;
                OdysseyUI.Card(new Rect(x,y,276,142),OdysseyUI.Navy);OdysseyUI.Text(new Rect(x+18,y+14,240,24),"BODY  "+(100-car.Damage.amount)+"%",17,OdysseyUI.White,true);
                OdysseyUI.Text(new Rect(x+18,y+44,240,22),game.state.Current.acorns+" COINS  /  "+game.state.Current.Gems+" GEMS",14,OdysseyUI.Gold,true);
                if(OdysseyUI.Button(new Rect(x+14,y+81,248,44),car.Damage.amount>0?"REPAIR  ·  R  /  "+car.Damage.CoinCost+" COINS":"INSPECT CAR  ·  R","repair-hud"))Open(car);
            }
            GUI.matrix=matrix;
        }
        void OnDestroy(){if(previewCamera!=null)Destroy(previewCamera.gameObject);if(preview!=null){preview.Release();Destroy(preview);}foreach(var clip in new[]{crash,bodyHit,repair})if(clip!=null)Destroy(clip);}
    }
}
