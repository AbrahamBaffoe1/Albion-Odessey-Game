using System;
using AlbionOdyssey;
static class ActivitiesTests {
 static void Check(bool v,string why){if(!v)throw new Exception(why);}
 public static void Run(){var s=new OdysseyState();var a=s.Current.activities;Check(!a.Record(0,1)&&!a.Join(3),"membership required");Check(a.Join(0)&&!a.Join(0),"join once");Check(!a.Submit(0,1,1,()=>true),"fieldwork required");Check(a.Record(0,1),"fieldwork");Check(!a.Submit(0,0,1,()=>true),"wrong answer");Check(!a.Submit(0,1,1,()=>false)&&a.completed[0]==0&&a.evidence==1,"failed save rollback");Check(a.Submit(0,1,1,()=>s.Valid()),"submission");Check(!a.Record(0,1)&&!a.Record(0,0)&&a.Record(0,2),"daily revisit and clock rollback");Check(a.Submit(0,1,2,()=>s.Valid())&&a.completed[0]==2,"next day");Check(s.Current.acorns==6,"no local online currency");s.active=1;Check(s.Current.activities.clubs==0,"keeper isolation");a.lastDay=null;Check(!a.Valid(),"corrupt arrays rejected");Console.WriteLine("ACTIVITIES_RULES_OK: membership, fieldwork, daily reset, rollback, isolation");}
}
