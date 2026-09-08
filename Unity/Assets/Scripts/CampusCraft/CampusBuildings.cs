using System.Collections.Generic;
using UnityEngine;
namespace AlbionOdyssey
{
    public sealed class CampusBuildings : MonoBehaviour
    {
        public static CampusBuildings Instance {get;private set;}
        public GameObject Exterior=>stream.Exterior;public GameObject Interior=>stream.Content;
        StreamedInterior stream;
        public readonly List<CampusDoor> Doors=new List<CampusDoor>();
        readonly List<WalkableCampusBuilding> additional=new List<WalkableCampusBuilding>();
        OdysseyGame game;CraftDescription description;Transform site;
        public Vector3 Origin=>CampusExpansion.Find("26").position;
        public bool Inside {get {if(game==null)return false;var p=game.player.transform.position-Origin;return Mathf.Abs(p.x)<15.6f&&Mathf.Abs(p.z)<5.85f&&p.y<10.4f;}}
        public int Floor=>game==null?0:Mathf.Clamp(Mathf.FloorToInt((game.player.transform.position.y+.15f)/3.5f),0,2);
        public string Location=>Inside?"Ferguson Hall · Level "+(Floor+1):"Ferguson Hall";
        public CampusDoor NearbyDoor
        {
            get {if(game==null)return null;CampusDoor best=null;float dist=2.7f;foreach(var door in Doors){if(!door.gameObject.activeInHierarchy)continue;float d=Vector3.Distance(game.player.transform.position,door.ClosedPosition);if(d<dist){dist=d;best=door;}}return best;}
        }
        public void Setup(OdysseyGame owner)
        {
            game=owner;Instance=this;site=new GameObject("26 · Ferguson Hall").transform;site.position=Origin;
            description=JsonUtility.FromJson<CraftDescription>(Resources.Load<TextAsset>("CampusCraft/ferguson").text);
            stream=site.gameObject.AddComponent<StreamedInterior>();stream.Initialize("ferguson",description,game.player.transform,DecorateInterior);
            // A small permanent collision shell is already present; occupied interior geometry loads by proximity.
            var plaque=new GameObject("Ferguson name").transform;plaque.SetParent(site,false);plaque.localPosition=new Vector3(0,5.84f,-7.22f);
            var label=plaque.gameObject.AddComponent<TextMesh>();label.text="FERGUSON HALL";label.fontSize=64;label.characterSize=.085f;label.anchor=TextAnchor.MiddleCenter;label.color=new Color(.18f,.2f,.18f);
            additional.Add(new WalkableCampusBuilding(game,CampusExpansion.Find("16"),28f,13.5f,4,3.35f,true,"Robinson Hall stands on the site of Albion’s original Central Building. The 1843 building was rebuilt after fire and renovated in 1992; its present use includes humanities and social-science classrooms and offices. The central atrium in this game follows the documented renovation description, while room placement remains a reconstruction."));
            additional.Add(new WalkableCampusBuilding(game,CampusExpansion.Find("1"),16.5f,11.2f,1,3.7f,false,"The Bonta Admission Center is Albion College’s visitor front door at 100 N. Hannah Street. It was named for Dean of Admissions Frank Bonta in 1996. This playable lobby and office layout is reconstructed from the official tour, campus photographs and public descriptions; hidden room dimensions remain unverified."));
            var science=new CampusPlace("18","Science Complex","Academic","science",503f,120f,22f,16f,14f);
            additional.Add(new WalkableCampusBuilding(game,science,33f,24f,4,3.6f,true,"Albion’s Science Complex brings Kresge, Norris, Palenske and Putnam together around a documented four-story atrium of about 7,000 square feet. Public descriptions identify teaching and research laboratories, collection displays, the Norris lecture hall, elevators and accessible restrooms. The game models those public-facing functions; private laboratories and exact room dimensions remain provisional until drawings are supplied."));
        }
        public void EnsureInterior()=>stream.EnsureLoaded();
        public WalkableCampusBuilding Building(string id)=>additional.Find(b=>b.Id==id);
        public bool VisitCampus(string id){var b=Building(id);if(b==null)return false;b.Visit();return true;}
        public WalkableCampusBuilding AdditionalInside(){foreach(var b in additional)if(b.Inside)return b;return null;}
        void DecorateInterior(GameObject interior)
        {
            Door(new Vector3(0,0,-6),2.7f,2.8f,"Ferguson entrance");
            for(int f=0;f<3;f++)foreach(float z in new[]{-1.6f,1.6f})foreach(float x in new[]{-10f,-4f,4f})Door(new Vector3(x,f*3.5f,z),1.36f,2.5f,"Room door");
            for(int f=0;f<3;f++)foreach(float x in new[]{-11f,-4f,4f,11f})
            {
                var l=new GameObject("Interior ceiling light").AddComponent<Light>();l.transform.SetParent(Interior.transform,false);l.transform.localPosition=new Vector3(x,f*3.5f+3.12f,0);l.type=LightType.Point;l.range=8;l.intensity=1.8f;l.color=new Color(1,.93f,.8f);l.shadows=LightShadows.None;
            }
            for(int f=0;f<3;f++)
            {
                Sign(new Vector3(8.5f,f*3.5f+2.15f,-1.7f),"STAIRS  →\nLEVEL "+(f+1));
                Sign(new Vector3(-10,f*3.5f+2.7f,-1.71f),f==0?"STUDENT SERVICES":f==1?"STUDY ROOM":"CAREER EXPLORATION");
                FergusonFurniture(f);
            }
            Physics.SyncTransforms();
        }
        void FergusonFurniture(int floor)
        {
            Material wood=CraftModel.Surface("Ferguson oak furniture",new Color(.34f,.17f,.08f));
            Material fabric=CraftModel.Surface("Ferguson seating",new Color(.22f,.28f,.34f));
            Vector3 at=new Vector3(-6+floor*2.2f,floor*3.5f+.48f,-3.1f);
            KeeperAvatar.Part(Interior.transform,"Ferguson waiting bench",PrimitiveType.Cube,at+Vector3.up*.25f,new Vector3(3.1f,.22f,.65f),fabric,true);
            KeeperAvatar.Part(Interior.transform,"Ferguson bench back",PrimitiveType.Cube,at+Vector3.up*.75f+Vector3.back*.25f,new Vector3(3.1f,.75f,.12f),wood,true);
            KeeperAvatar.Part(Interior.transform,"Ferguson information table",PrimitiveType.Cube,new Vector3(5,floor*3.5f+.72f,2.8f),new Vector3(2.4f,.12f,.9f),wood,true);
            Sign(new Vector3(5,floor*3.5f+2.2f,2.35f),floor==0?"WELCOME / COUNSELING":floor==1?"QUIET STUDY":"CAREER RESOURCE DESK");
        }
        void Sign(Vector3 at,string text){var o=new GameObject(text);o.transform.SetParent(Interior.transform,false);o.transform.localPosition=at;var m=o.AddComponent<TextMesh>();m.text=text;m.characterSize=.065f;m.fontSize=48;m.anchor=TextAnchor.MiddleCenter;m.color=new Color(.22f,.15f,.3f);}
        void Door(Vector3 at,float width,float height,string label){var o=new GameObject(label);o.transform.SetParent(Interior.transform,false);o.transform.localPosition=at;var door=o.AddComponent<CampusDoor>();door.Label=label;door.Build(width,height,CraftModel.Surface("Door glass",new Color(.27f,.38f,.4f),"",.6f));Doors.Add(door);}
        public void Visit(){game.tour.StopMedia();EnsureInterior();if(game.tour.InRoom)game.tour.ExitRoom();if(game.building)game.ToggleMode();if(!game.player.TryExitVehicle())return;game.player.Teleport(Origin+new Vector3(0,.08f,-10));game.player.transform.rotation=Quaternion.identity;game.life.SetPanel("");}
        public bool HandleInput()
        {
            if(game.life.PanelOpen||game.building||game.journalOpen)return false;
            foreach(var b in additional)if(b.HandleInput())return true;
            if(Input.GetKeyDown(KeyCode.E)&&NearbyDoor!=null){NearbyDoor.Toggle(game.player);return true;}
            if(Input.GetKeyDown(KeyCode.H)&&Inside){game.tour.OpenStory(game.tour.catalog.ForCampus("26"));return true;}
            return false;
        }
    }
}
