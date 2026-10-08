using System;
using AlbionOdyssey;
static class StoreTests
{
 static void Check(bool condition,string reason){if(!condition)throw new Exception(reason);}
 public static void Run()
 {
  var s=new OdysseyState();Check(!s.Current.BuyFinish(0)&&!s.Current.BuyFinish(7),"invalid catalog IDs");
  Check(!s.Current.EquipFinish(1),"unowned finish cannot equip");Check(!s.Current.BuyFinish(6)&&s.Current.acorns==6,"insufficient funds do not charge");
  Check(s.Current.BuyFinish(1)&&s.Current.acorns==2&&s.Valid(),"purchase conserves earned currency");
  Check(!s.Current.BuyFinish(1)&&s.Current.acorns==2,"duplicate purchase never charges");
  Check(s.Current.EquipFinish(1)&&s.Valid(),"owned finish equip");Check(s.Current.EquipFinish(0)&&s.Valid(),"original outfit restore");
  s.active=1;Check(s.Current.ownedFinishes==0&&s.Current.acorns==6,"keeper inventory isolation");
  s.Current.ownedFinishes=64;Check(!s.Valid(),"unknown inventory bits rejected");s.Current.ownedFinishes=0;s.Current.equippedFinish=1;Check(!s.Valid(),"unowned equipped state rejected");
  var old=new OdysseyState{version=3};old.Collect(2);old.UpgradeLegacySave();Check(old.version==4&&old.Valid()&&old.Current.acorns==9,"v3 migration preserves progress");
  var rng=new Random(992);for(int run=0;run<100;run++){var state=new OdysseyState();for(int n=0;n<500;n++){state.active=rng.Next(4);switch(rng.Next(5)){case 0:state.Collect(rng.Next(12));break;case 1:state.Current.BuyFinish(rng.Next(8));break;case 2:state.Current.EquipFinish(rng.Next(8));break;case 3:state.Build(rng.Next(49),rng.Next(1,5));break;case 4:state.Reclaim(rng.Next(49));break;}Check(state.Valid(),"commerce and construction conservation");}}
  Console.WriteLine("STORE_RULES_OK: migration, isolation, ownership, equip, duplicates, insufficient funds and 50000 mixed transactions");
 }
}
