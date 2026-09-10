using UnityEngine;
namespace AlbionOdyssey
{
    // Door opening, prompt and safe closing are shared by exterior and room entrances.
    public sealed class CampusDoor : MonoBehaviour
    {
        public string Label="Open door";public bool IsOpen {get;private set;}public Vector3 ClosedPosition;
        Transform leaf;BoxCollider barrier;CampusWorldLabel tag;float travel,width;
        public void Build(float opening,float height,Material material)
        {
            width=opening;ClosedPosition=transform.position;
            leaf=KeeperAvatar.Part(transform,"Sliding glazed door",PrimitiveType.Cube,new Vector3(0,height/2,0),new Vector3(opening,height,.075f),material).transform;
            barrier=gameObject.AddComponent<BoxCollider>();barrier.center=new Vector3(0,height/2,0);barrier.size=new Vector3(opening,height,.12f);
            for(int i=-1;i<=1;i++)KeeperAvatar.Part(leaf,"Door frame",PrimitiveType.Cube,new Vector3(i*.49f,0,-.1f),new Vector3(.025f,1.02f,1.3f),CraftModel.Surface("Door bronze",new Color(.16f,.18f,.18f)));
            tag=gameObject.AddComponent<CampusWorldLabel>();tag.Configure("E  OPEN "+Label.ToUpperInvariant(),new Color(1f,.76f,.28f),new Vector3(0,height+.28f,0),8f);UpdateLabel(height);
        }
        public bool Toggle(Explorer player)
        {
            if(IsOpen&&Vector3.Distance(player.transform.position,ClosedPosition)<1.2f)return false;
            IsOpen=!IsOpen;barrier.enabled=!IsOpen;UpdateLabel(leaf==null?2.5f:leaf.localScale.y);
            return true;
        }
        void UpdateLabel(float height){if(tag!=null)tag.SetText("E  "+(IsOpen?"CLOSE ":"OPEN ")+Label.ToUpperInvariant());}
        void Update(){travel=Mathf.MoveTowards(travel,IsOpen?1:0,Time.deltaTime*2.8f);leaf.localPosition=new Vector3(width*travel,leaf.localPosition.y,0);}
    }
}
