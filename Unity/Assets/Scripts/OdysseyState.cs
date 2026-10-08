using System;

namespace AlbionOdyssey
{
    [Serializable] public sealed class Keeper
    {
        public int acorns=6;
        public int memories;
        public int milestones;
        public int style=1;
        public int contribution;
        public int repairAcorns, gemsSpent;
        // Stable catalog IDs, stored as bits. Zero finish means the original outfit.
        public int ownedFinishes, equippedFinish;
        static readonly int[] FinishPrices={4,6,6,8,8,10};
        public static int FinishCost(int id)=>id>=1&&id<=6?FinishPrices[id-1]:0;
        public bool OwnsFinish(int id)=>id>=1&&id<=6&&(ownedFinishes&(1<<(id-1)))!=0;
        public int FinishSpend { get { int total=0;for(int i=1;i<=6;i++)if(OwnsFinish(i))total+=FinishCost(i);return total; } }
        public bool BuyFinish(int id)
        {
            int cost=FinishCost(id);if(cost==0||OwnsFinish(id)||acorns<cost)return false;
            acorns-=cost;ownedFinishes|=1<<(id-1);return true;
        }
        public bool EquipFinish(int id){if(id!=0&&!OwnsFinish(id))return false;equippedFinish=id;return true;}
        public int Gems => OdysseyState.Count(memories)-gemsSpent;
        public int[] plots=new int[49];
    }
    [Serializable] public sealed class OdysseyState
    {
        public int version=4;
        public VehicleDamage[] vehicles={new VehicleDamage(),new VehicleDamage(),new VehicleDamage()};
        public CampusSchool school=new CampusSchool();
        public int active;
        public int beacon;
        public Keeper[] keepers={new Keeper(),new Keeper(),new Keeper(),new Keeper()};
        public Keeper Current => keepers[active];
        public static int Cost(int kind) => kind==1?2:kind==2?4:kind==3?6:kind==4?3:0;
        public static int Count(int mask) { int n=0; for(int i=0;i<12;i++) n+=(mask>>i)&1; return n; }
        public bool Collect(int id)
        {
            if(id<0||id>=12||(Current.memories&(1<<id))!=0)return false;
            Current.memories|=1<<id; Current.acorns+=3; return true;
        }
        public bool Build(int cell,int kind)
        {
            if(cell<0||cell>=49||kind<1||kind>4||Current.plots[cell]!=0||Current.acorns<Cost(kind))return false;
            Current.acorns-=Cost(kind); Current.plots[cell]=kind; return true;
        }
        public bool Reclaim(int cell)
        {
            if(cell<0||cell>=49||Current.plots[cell]==0||school.UsesPlot(active,cell))return false;
            Current.acorns+=Cost(Current.plots[cell]); Current.plots[cell]=0; return true;
        }
        public bool Contribute()
        {
            if(beacon>=24||Current.acorns<2)return false;
            Current.acorns-=2; Current.contribution+=2; beacon+=2; return true;
        }
        public void UpgradeLegacySave()
        {
            if(version==1){school=new CampusSchool();version=2;}
            if(version==2){vehicles=new[]{new VehicleDamage(),new VehicleDamage(),new VehicleDamage()};version=3;}
            if(version==3&&keepers!=null){foreach(var keeper in keepers){if(keeper==null)continue;keeper.ownedFinishes=0;keeper.equippedFinish=0;}version=4;}
            if(school!=null)school.NormalizeSchedules();
        }
        public bool Valid()
        {
            if(version!=4||active<0||active>=4||beacon<0||beacon>24||keepers==null||keepers.Length!=4||vehicles==null||vehicles.Length!=3)return false;
            foreach(var vehicle in vehicles)if(vehicle==null||!vehicle.Valid())return false;
            int total=0;
            foreach(var p in keepers)
            {
                if(p==null||p.plots==null||p.plots.Length!=49||p.acorns<0||p.acorns>42||p.memories<0||p.memories>4095||p.milestones<0||p.milestones>63||p.style<0||p.style>1||p.contribution<0||p.contribution>24||p.contribution%2!=0)return false;
                if(p.repairAcorns<0||p.repairAcorns>42||p.gemsSpent<0||p.Gems<0)return false;
                if(p.ownedFinishes<0||p.ownedFinishes>63||p.equippedFinish<0||p.equippedFinish>6||(p.equippedFinish!=0&&!p.OwnsFinish(p.equippedFinish)))return false;
                int spent=p.contribution+p.repairAcorns+p.FinishSpend;
                foreach(int b in p.plots) { if(b<0||b>4)return false; spent+=Cost(b); }
                if(p.acorns+spent!=6+3*Count(p.memories))return false;
                total+=p.contribution;
            }
            return total==beacon&&school!=null&&school.Valid(this);
        }
    }
}
