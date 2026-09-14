using System;
using AlbionOdyssey;
static class VehicleTests
{
 static void Check(bool ok,string reason){if(!ok)throw new Exception(reason);}
 public static void Run()
 {
  var s=new OdysseyState();s.version=2;s.vehicles=null;s.Collect(0);s.UpgradeLegacySave();
  Check(s.Valid()&&s.Current.Gems==1&&s.Current.acorns==9,"v2 migration preserves coins and credits discovered memories with gems");
  var dent=new VehicleDent{x=0,y=.8f,z=2.1f,nz=-1,depth=.3f};s.vehicles[0].Impact(35,dent);
  Check(s.vehicles[0].CoinCost==4&&s.vehicles[0].GemCost==1,"quote rounds up");
  var saved=s.vehicles[0];Check(!VehicleRepair.TryPay(s,0,false,()=>false)&&s.Current.acorns==9&&s.Current.repairAcorns==0&&s.vehicles[0]==saved&&s.Valid(),"failed persistence rolls back debit and damage");
  try{VehicleRepair.TryPay(s,0,true,()=>throw new Exception("disk failed"));}catch(Exception){}
  Check(s.Current.Gems==1&&s.vehicles[0]==saved&&s.Valid(),"exception rolls back gem charge");
  Check(VehicleRepair.TryPay(s,0,false,()=>s.Valid())&&s.Current.acorns==5&&s.Current.repairAcorns==4&&s.vehicles[0].amount==0&&s.Valid(),"coin repair ledger balances");
  Check(!VehicleRepair.TryPay(s,0,false,()=>true)&&s.Current.acorns==5,"duplicate repair cannot charge");
  s.vehicles[1].Impact(70,dent);Check(!VehicleRepair.TryPay(s,1,true,()=>true)&&s.Current.Gems==1&&s.vehicles[1].amount==70,"insufficient gems preserves damage");
  Check(!VehicleRepair.TryPay(s,1,false,()=>true)&&s.Current.acorns==5,"insufficient coins cannot overspend");
  s.Collect(1);Check(VehicleRepair.TryPay(s,1,true,()=>s.Valid())&&s.Current.Gems==0&&s.Current.acorns==8,"gem payment leaves coins untouched");
  s.active=1;Check(s.Current.Gems==0&&s.Current.acorns==6,"wallet belongs to active keeper");
  for(int i=0;i<100;i++)s.vehicles[2].Impact(50,dent);
  Check(s.vehicles[2].amount==100&&s.vehicles[2].dents.Length==8&&s.Valid(),"damage and dent history bounded");
  s.vehicles[2].dents[0]=new VehicleDent{x=float.NaN};Check(!s.Valid(),"corrupt geometry rejected");
  var rng=new Random(1919);s=new OdysseyState();
  for(int i=0;i<20000;i++){
   s.active=rng.Next(4);int car=rng.Next(3);
   switch(rng.Next(5)){case 0:s.Collect(rng.Next(12));break;case 1:s.vehicles[car].Impact(rng.Next(1,66),dent);break;case 2:VehicleRepair.TryPay(s,car,rng.Next(2)==0,()=>rng.Next(3)>0);break;case 3:s.Build(rng.Next(49),rng.Next(1,5));break;case 4:s.Reclaim(rng.Next(49));break;}
   Check(s.Valid(),"repair/build/collection randomized transaction "+i);
  }
  Console.WriteLine("VEHICLE_RULES_OK: migration, two currencies, failed/duplicate payments, isolated wallets, bounded dents and 20000 transactions");
 }
}
