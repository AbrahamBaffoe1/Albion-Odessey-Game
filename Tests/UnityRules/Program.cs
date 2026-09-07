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
 var legacy=new OdysseyState();legacy.version=1;legacy.school=null;legacy.Collect(0);legacy.UpgradeLegacySave();Check(legacy.Valid()&&legacy.Current.memories==1,"legacy save migration preserves progress");
 var learn=new OdysseyState();var school=learn.school;
 Check(!school.Create(learn,"History",0),"building required");learn.Build(0,4);
 Check(!school.Create(learn,"<bad>",0)&&!school.Create(learn,"",0),"valid titles required");
 Check(school.Create(learn,"My class",0)&&learn.Valid(),"course creation");
 Check(!learn.Reclaim(0),"assigned course protects building");
 Check(!school.Create(learn,"Second class",1),"one course per building");
 Check(school.Enroll(0,0)&&!school.Enroll(0,0),"enrollment idempotency");
 Check(!school.Answer(0,1,0)&&!school.Answer(0,0,1)&&school.Answer(0,0,0)&&!school.Answer(0,0,0),"lesson enrollment and completion rules");
 for(int i=0;i<11;i++)Check(school.AssignStudents(0,0,1),"fill seats");
 Check(!school.AssignStudents(0,0,1)&&!school.Enroll(0,1),"capacity enforced");
 Check(!school.AssignStudents(0,1,-1)&&!school.Remove(0,1)&&!school.Teach(0,1),"owner permissions");
 Check(school.Teach(0,0)&&school.courses[0].sessions==1&&learn.Valid(),"class session");
 Check(school.Remove(0,0)&&learn.Reclaim(0)&&learn.Valid(),"remove course releases building");
 var schoolRng=new Random(1835);
 for(int i=0;i<10000;i++){
 learn.active=schoolRng.Next(4);int slot=schoolRng.Next(-1,7);
 switch(schoolRng.Next(9)){
 case 0:learn.Build(schoolRng.Next(49),schoolRng.Next(1,5));break;
 case 1:school.Create(learn,"Course "+i,schoolRng.Next(3));break;
 case 2:school.Enroll(slot,learn.active);break;
 case 3:school.AssignStudents(slot,learn.active,schoolRng.Next(2)*2-1);break;
 case 4:school.Teach(slot,learn.active);break;
 case 5:school.Answer(slot,learn.active,schoolRng.Next(3));break;
 case 6:school.Remove(slot,learn.active);break;
 case 7:learn.Reclaim(schoolRng.Next(49));break;
 case 8:learn.Collect(schoolRng.Next(12));break;}
 Check(learn.Valid(),"school transaction "+i);
 }
 Console.WriteLine("CAMPUS_SCHOOL_OK: migration, ownership, capacity, quizzes and 10000 randomized classroom transactions");
 var rng=new Random(492);for(int i=0;i<100000;i++){s.active=rng.Next(4);switch(rng.Next(5)){case 0:s.Build(rng.Next(-2,52),rng.Next(-1,6));break;case 1:s.Reclaim(rng.Next(-2,52));break;case 2:s.Collect(rng.Next(-2,15));break;case 3:s.Contribute();break;case 4:s.Current.style=1-s.Current.style;break;}OdysseyStory.Refresh(s);Check(s.Valid(),"transaction "+i);}
 s.Current.acorns++;Check(!s.Valid(),"tamper detection");Console.WriteLine("UNITY_RULES_OK: 100000 randomized transactions plus economy, isolation and tamper cases");
 }
}
