using UnityEngine;
namespace AlbionOdyssey
{
    public sealed class OdysseyAdventure : MonoBehaviour
    {
        OdysseyGame game;int focus;
        public AdventureProgress Progress=>game.state.Current.adventure;
        public void Setup(OdysseyGame owner){game=owner;}
        public bool AtHome=>Vector3.Distance(game.player.transform.position,new Vector3(0,.05f,-22))<12;
        public void Open(){focus=0;game.life.SetPanel("adventure");}
        public bool Advance()=>Progress.Advance(game.state.Current,AtHome,game.Save);
        public void Observe(int target){int old=Progress.observed;if(Progress.Observe(target)&&!game.Save())Progress.observed=old;}
        public void Arrived(){if(Progress.chapter!=3||Progress.arrived)return;Progress.arrived=true;if(!game.Save())Progress.arrived=false;}
        public bool HandleInput()
        {
            if(game.life.panel=="adventure"){
                if(AlbionUIInput.Poll(out var horizontal,out var vertical,out var choose,out var cancel)){if(cancel)game.life.SetPanel("");else{if(horizontal!=0||vertical!=0)focus=(focus+(horizontal>0||vertical<0?1:2))%3;if(choose){if(focus==0)Advance();else if(focus==1&&Progress.chapter>0&&Progress.chapter<5)Travel();else game.life.SetPanel("");}}}
                return true;
            }
            if(!game.life.PanelOpen&&!game.journalOpen&&Input.GetKeyDown(KeyCode.F6)){Open();return true;}
            return false;
        }
        public void Travel()
        {
            if(!game.player.TryExitVehicle()){game.notice="Exit the vehicle safely before traveling.";return;}
            if(Progress.chapter!=3)FindAnyObjectByType<WhitehouseTrailWorld>()?.LeaveVisit();
            if(Progress.chapter==2)game.campus.Travel(CampusExpansion.Find("5"));
            else if(Progress.chapter==3)game.destinations.ArriveNature();
            else game.life.Travel(0);
        }
        void OnGUI()
        {
            if(game==null||!game.Ready)return;
            var matrix=GUI.matrix;float scale=Mathf.Min(Screen.width/1440f,Screen.height/900f);GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);
            float w=Screen.width/scale,h=Screen.height/scale;
            if(game.life.panel=="adventure"){
                ConsoleMenuStyle.Background(game,w,h);
                OdysseyCinematic.Title(new Rect(100,80,w-200,65),AdventureProgress.Titles[Progress.chapter],42,OdysseyUI.Gold);
                OdysseyUI.Text(new Rect(100,180,w-200,150),AdventureProgress.Objectives[Progress.chapter],25,OdysseyUI.White);
                string detail=Progress.chapter==1?OdysseyState.Count(game.state.Current.memories)+" / 3 memories":Progress.chapter==2?OdysseyState.Count(Progress.observed)+" / 3 sky objects":Progress.chapter==3?OdysseyState.Count(game.state.Current.fieldJournal)+" / 3 habitats":Progress.chapter==4&&!AtHome?"Return to the Legacy Hall entrance":"";
                OdysseyUI.Text(new Rect(100,370,w-200,50),detail,23,OdysseyUI.White);
                OdysseyUI.Text(new Rect(100,455,w-200,50),Progress.Reward,22,OdysseyUI.Gold);
                bool previous=GUI.enabled;GUI.enabled=Progress.CanAdvance(game.state.Current,AtHome);
                if(OdysseyUI.Button(new Rect(100,h-210,360,55),Progress.chapter==0?"Accept Pip's invitation":Progress.chapter==4?"Share the expedition":"Complete chapter","adventure-next",focus==0))Advance();
                GUI.enabled=previous;
                if(Progress.chapter>0&&Progress.chapter<5&&OdysseyUI.Button(new Rect(490,h-210,360,55),"Travel to objective","adventure-travel",focus==1))Travel();
                if(OdysseyUI.Button(new Rect(100,h-125,360,50),"Continue exploring · Esc","adventure-close",focus==2))game.life.SetPanel("");
            }else if(!game.life.PanelOpen&&!game.journalOpen&&!game.building){
                OdysseyUI.Fill(new Rect(30,100,430,80),new Color(.015f,.02f,.05f,.8f));
                OdysseyUI.Text(new Rect(45,108,400,54),"F6 · "+AdventureProgress.Titles[Progress.chapter]+"\nF7 · Clubs and assignments",19,OdysseyUI.White);
            }
            GUI.matrix=matrix;
        }
    }
}
