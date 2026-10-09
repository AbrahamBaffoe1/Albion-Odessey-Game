using System;
using AlbionOdyssey;
static class AdventureTests
{
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    public static void Run(){
        var s=new OdysseyState();var k=s.Current;var p=k.adventure;
        Check(!p.Observe(0),"cannot earn later chapter evidence early");
        Check(!p.Advance(k,false,()=>false)&&p.chapter==0,"failed save retains invitation");
        Check(p.Advance(k,false,()=>s.Valid()),"accept invitation");
        Check(!p.Advance(k,false,()=>true),"memory objective cannot be skipped");
        for(int i=0;i<3;i++)s.Collect(i);
        Check(p.Advance(k,false,()=>s.Valid()),"memory chapter");
        Check(!p.Observe(-1)&&!p.Observe(3),"invalid sky target");
        for(int i=0;i<3;i++)Check(p.Observe(i)&&!p.Observe(i),"sky targets counted once");
        Check(p.Advance(k,false,()=>s.Valid()),"sky chapter");
        k.fieldJournal=7;Check(!p.Advance(k,false,()=>true),"nature arrival required");p.arrived=true;
        Check(p.Advance(k,false,()=>s.Valid()),"nature chapter");
        Check(!p.Advance(k,false,()=>true),"must return home");
        try{p.Advance(k,true,()=>throw new Exception("disk"));}catch(Exception){}
        Check(p.chapter==4,"save exception rollback");
        Check(p.Advance(k,true,()=>s.Valid())&&!p.Advance(k,true,()=>true),"ending is one-time");
        Check(k.acorns==15&&s.Valid(),"story never mints currency");s.active=1;Check(s.Current.adventure.chapter==0,"keeper isolation");
        s.version=4;s.keepers[0].adventure=null;s.UpgradeLegacySave();Check(s.Valid()&&s.keepers[0].memories==7,"migration preserves existing discoveries");
        s.Current.adventure.chapter=5;Check(!s.Valid(),"inconsistent chapter evidence rejected");
        Console.WriteLine("ADVENTURE_RULES_OK: prerequisites, rollback, ending, isolation, migration and currency invariants");
    }
}
