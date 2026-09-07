using System;
using System.Linq;
using AlbionOdyssey;
static class FeedbackTests
{
    static void Check(bool ok,string message){if(!ok)throw new Exception("Audio feedback: "+message);}
    public static void Run()
    {
        var s=new OdysseyState();var f=new OdysseyFeedback();
        Check(f.Observe(s).Count==0,"initialization is silent");
        s.Collect(0);OdysseyStory.Refresh(s);
        Check(f.Observe(s).SequenceEqual(new[]{OdysseyCue.Memory,OdysseyCue.FirstDiscovery}),"pickup then first milestone");
        Check(!s.Collect(0)&&f.Observe(s).Count==0,"duplicate pickup is silent");
        s.Collect(1);OdysseyStory.Refresh(s);
        Check(f.Observe(s).SequenceEqual(new[]{OdysseyCue.Memory}),"same collection kind uses same cue");
        s.active=1;Check(f.Observe(s).Count==0,"profile switch is silent");
        s.Collect(0);OdysseyStory.Refresh(s);
        Check(f.Observe(s).SequenceEqual(new[]{OdysseyCue.Memory,OdysseyCue.FirstDiscovery}),"same achievement kind across keepers");
        f.Reset(s);Check(f.Observe(s).Count==0,"loaded achievements do not replay");
        for(int kind=1;kind<=4;kind++)
        {
            s=new OdysseyState();f.Reset(s);s.Build(0,kind);OdysseyStory.Refresh(s);
            Check(f.Observe(s).Single()==(OdysseyCue)((int)OdysseyCue.GardenBuilt+kind-1),"building kind "+kind);
            Check(!s.Build(0,kind)&&f.Observe(s).Count==0,"failed placement silent");
            s.Reclaim(0);Check(f.Observe(s).Single()==OdysseyCue.Reclaim,"refund sound");
        }
        for(int lesson=0;lesson<3;lesson++)
        {
            s=new OdysseyState();s.Build(0,4);OdysseyStory.Refresh(s);f.Reset(s);
            s.school.Create(s,"A class",lesson);Check(f.Observe(s).Single()==OdysseyCue.CourseCreated,"create course");
            s.school.Enroll(0,0);Check(f.Observe(s).Single()==OdysseyCue.StudentEnrolled,"enroll");
            s.school.Answer(0,0,(CampusLessons.Correct[lesson]+1)%3);Check(f.Observe(s).Count==0,"wrong answer silent");
            s.school.Answer(0,0,CampusLessons.Correct[lesson]);Check(f.Observe(s).Single()==(OdysseyCue)((int)OdysseyCue.HistoryLesson+lesson),"lesson-specific achievement");
            s.school.Answer(0,0,CampusLessons.Correct[lesson]);Check(f.Observe(s).Count==0,"completed quiz silent");
        }
        s=new OdysseyState();for(int i=0;i<12;i++)s.Collect(i);OdysseyStory.Refresh(s);f.Reset(s);
        for(int i=0;i<11;i++){s.Contribute();OdysseyStory.Refresh(s);f.Observe(s);}
        s.Contribute();OdysseyStory.Refresh(s);var cues=f.Observe(s);
        Check(cues.Count(c=>c==OdysseyCue.BeaconComplete)==1,"shared Beacon announces once, not four times");
        s.active=2;Check(f.Observe(s).Count==0,"switch to keeper with shared achievement does not replay");
        Check(Enum.GetValues(typeof(OdysseyCue)).Length==20,"all twenty cue kinds");
        Console.WriteLine("AUDIO_FEEDBACK_OK: stable kinds, achievements, building/lesson sounds, failed actions, load/profile silence and shared Beacon deduplication");
    }
}
