using System;
using AlbionOdyssey;
class Program {
 static void Check(bool v,string m){if(!v)throw new Exception(m);}
 static void Main(){
 var s=new OdysseyState();Check(s.Valid(),"new state");Check(s.Build(0,3),"build");Check(s.Current.acorns==0,"cost");Check(!s.Build(1,1),"overspend");Check(s.Reclaim(0)&&s.Current.acorns==6,"refund");
 Check(s.Collect(0)&&!s.Collect(0),"one collection");Check(s.Current.acorns==9,"reward");
 s.active=1;Check(s.Current.memories==0&&s.Current.acorns==6,"keeper isolation");
 var rng=new Random(492);for(int i=0;i<100000;i++){s.active=rng.Next(4);switch(rng.Next(5)){case 0:s.Build(rng.Next(-2,52),rng.Next(-1,6));break;case 1:s.Reclaim(rng.Next(-2,52));break;case 2:s.Collect(rng.Next(-2,15));break;case 3:s.Contribute();break;case 4:s.Current.style=1-s.Current.style;break;}Check(s.Valid(),"transaction "+i);}
 s.Current.acorns++;Check(!s.Valid(),"tamper detection");Console.WriteLine("UNITY_RULES_OK: 100000 randomized transactions plus economy, isolation and tamper cases");
 }
}
