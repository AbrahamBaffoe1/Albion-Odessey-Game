using System;
using AlbionOdyssey;
class Program {
 static void Check(bool v,string m){if(!v)throw new Exception(m);}
 static void Main(){
 var s=new OdysseyState();Check(s.Valid(),"new state");Check(s.Build(0,3),"build");Check(s.Current.acorns==0,"cost");Check(!s.Build(1,1),"overspend");Check(s.Reclaim(0)&&s.Current.acorns==6,"refund");
 Check(s.Collect(0)&&!s.Collect(0),"one collection");Check(s.Current.acorns==9,"reward");
 s.active=1;Check(s.Current.memories==0&&s.Current.acorns==6,"keeper isolation");
 var chapter=new OdysseyState();chapter.Collect(0);chapter.Build(0,1);chapter.Build(1,1);chapter.Build(2,1);OdysseyStory.Refresh(chapter);
 Check(chapter.Current.milestones==3,"first chapters");chapter.Reclaim(2);OdysseyStory.Refresh(chapter);Check((chapter.Current.milestones&2)!=0,"earned chapter survives redesign");
 for(int i=1;i<8;i++)chapter.Collect(i);OdysseyStory.Refresh(chapter);Check((chapter.Current.milestones&4)!=0,"eight-floor chapter");
 Check(OdysseyStory.Titles.Length==12&&OdysseyStory.Entries.Length==12&&OdysseyStory.Floors.Length==8,"journal content");
 var rng=new Random(492);for(int i=0;i<100000;i++){s.active=rng.Next(4);switch(rng.Next(5)){case 0:s.Build(rng.Next(-2,52),rng.Next(-1,6));break;case 1:s.Reclaim(rng.Next(-2,52));break;case 2:s.Collect(rng.Next(-2,15));break;case 3:s.Contribute();break;case 4:s.Current.style=1-s.Current.style;break;}OdysseyStory.Refresh(s);Check(s.Valid(),"transaction "+i);}
 s.Current.acorns++;Check(!s.Valid(),"tamper detection");Console.WriteLine("UNITY_RULES_OK: 100000 randomized transactions plus economy, isolation and tamper cases");
 }
}
