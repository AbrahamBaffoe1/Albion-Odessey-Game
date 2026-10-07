using System;
namespace AlbionOdyssey
{
    [Serializable] public sealed class VehicleDent
    {
        public float x,y,z,nx,ny,nz,depth;
        public bool Valid()=>Finite(x,1.2f)&&Finite(y,1.6f)&&y>=0&&Finite(z,2.3f)&&Finite(nx,1)&&Finite(ny,1)&&Finite(nz,1)&&Finite(depth,.5f)&&depth>0&&nx*nx+ny*ny+nz*nz>.9f&&nx*nx+ny*ny+nz*nz<1.1f;
        static bool Finite(float n,float limit)=>!float.IsNaN(n)&&!float.IsInfinity(n)&&Math.Abs(n)<=limit;
    }
    [Serializable] public sealed class VehicleDamage
    {
        public int amount;
        public VehicleDent[] dents=Array.Empty<VehicleDent>();
        public int CoinCost=>(amount+9)/10;
        public int GemCost=>(amount+34)/35;
        public bool Valid()
        {
            if(amount<0||amount>100||dents==null||dents.Length>8||((amount==0)!=(dents.Length==0)))return false;
            foreach(var dent in dents)if(dent==null||!dent.Valid())return false;
            return true;
        }
        public void Impact(int points,VehicleDent dent)
        {
            if(points<=0||dent==null||!dent.Valid())return;
            amount=Math.Min(100,amount+points);
            var next=new VehicleDent[Math.Min(8,dents.Length+1)];
            int start=dents.Length==8?1:0;Array.Copy(dents,start,next,0,next.Length-1);next[next.Length-1]=dent;dents=next;
        }
    }
    public static class VehicleRepair
    {
        // Persist the debit and damage removal together; a failed write restores both.
        public static bool TryPay(OdysseyState state,int car,bool gems,Func<bool> persist)
        {
            if(state==null||!state.Valid()||car<0||car>=state.vehicles.Length||persist==null)return false;
            var damage=state.vehicles[car];var wallet=state.Current;
            if(damage.amount==0|| (gems?wallet.Gems<damage.GemCost:wallet.acorns<damage.CoinCost))return false;
            int coins=wallet.acorns,spent=wallet.repairAcorns,gemSpent=wallet.gemsSpent;
            if(gems)wallet.gemsSpent+=damage.GemCost;else{wallet.acorns-=damage.CoinCost;wallet.repairAcorns+=damage.CoinCost;}
            state.vehicles[car]=new VehicleDamage();bool saved=false;
            try{saved=persist();return saved;}
            finally{if(!saved){wallet.acorns=coins;wallet.repairAcorns=spent;wallet.gemsSpent=gemSpent;state.vehicles[car]=damage;}}
        }
    }
}
