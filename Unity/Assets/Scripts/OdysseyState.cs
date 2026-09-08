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
        public int[] plots=new int[49];
    }
    [Serializable] public sealed class OdysseyState
    {
        public int version=2;
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
            if(school!=null)school.NormalizeSchedules();
        }
        public bool Valid()
        {
            if(version!=2||active<0||active>=4||beacon<0||beacon>24||keepers==null||keepers.Length!=4)return false;
            int total=0;
            foreach(var p in keepers)
            {
                if(p==null||p.plots==null||p.plots.Length!=49||p.acorns<0||p.acorns>42||p.memories<0||p.memories>4095||p.milestones<0||p.milestones>63||p.style<0||p.style>1||p.contribution<0||p.contribution>24||p.contribution%2!=0)return false;
                int spent=p.contribution;
                foreach(int b in p.plots) { if(b<0||b>4)return false; spent+=Cost(b); }
                if(p.acorns+spent!=6+3*Count(p.memories))return false;
                total+=p.contribution;
            }
            return total==beacon&&school!=null&&school.Valid(this);
        }
    }
}
