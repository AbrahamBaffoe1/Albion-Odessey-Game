using System;
namespace AlbionOdyssey {
 [Serializable] public sealed class CampusActivities {
  public int clubs,evidence;
  public int[] completed={0,0,0},lastDay={-1,-1,-1};
  public static readonly string[] Names={"Storykeepers", "Skywatchers", "Field Naturalists"};
  public static readonly string[] Tasks={"Return to Legacy Hall and read a collected memory in your journal (J), then reflect on the archive.","Record a telescope observation upstairs in the observatory, then answer the sky question.","Record a habitat at Whitehouse Nature Center, then answer the ecology question."};
  public static readonly string[] Questions={"An archive should preserve…", "A telescope's magnification changes…", "To protect a habitat, visitors should…"};
  public static readonly string[][] Answers={new[]{"Only the best-known stories","Many different people's stories"},new[]{"The object's apparent size","The object's real size"},new[]{"Stay on marked paths","Collect every interesting plant"}};
  static readonly int[] Correct={1,0,0};
  public bool Valid(){if(clubs<0||clubs>7||evidence<0||evidence>7||(evidence&~clubs)!=0||completed==null||lastDay==null||completed.Length!=3||lastDay.Length!=3)return false;for(int i=0;i<3;i++)if(completed[i]<0||completed[i]>999||lastDay[i]<-1||lastDay[i]>4000000||(completed[i]==0)!=(lastDay[i]==-1))return false;return true;}
  public bool Join(int i){if(i<0||i>2||(clubs&(1<<i))!=0)return false;clubs|=1<<i;return true;}
  public bool Record(int i,int day){if(i<0||i>2||(clubs&(1<<i))==0||day<=lastDay[i]||(evidence&(1<<i))!=0)return false;evidence|=1<<i;return true;}
  public bool Submit(int i,int answer,int day,Func<bool> save){if(i<0||i>2||(evidence&(1<<i))==0||answer!=Correct[i]||day<=lastDay[i]||day>4000000||completed[i]>=999)return false;int old=lastDay[i];completed[i]++;lastDay[i]=day;evidence&=~(1<<i);try{if(save())return true;}catch{completed[i]--;lastDay[i]=old;evidence|=1<<i;throw;}completed[i]--;lastDay[i]=old;evidence|=1<<i;return false;}
  public string Badge(int i)=>completed[i]>=10?"Mentor":completed[i]>=3?"Contributor":completed[i]>=1?"Member":"New member";
 }
}
