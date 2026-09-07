using System;
using System.Collections.Generic;
namespace AlbionOdyssey
{
    // Stable semantic keys: identical actions always select identical audio assets.
    public enum OdysseyCue
    {
        Memory, FirstDiscovery, ThreePlaces, EightFloors, AllBuildingKinds, GenerousKeeper, BeaconComplete,
        HistoryLesson, AstronomyLesson, DesignLesson, GardenBuilt, LibraryBuilt, ObservatoryBuilt, HallBuilt,
        Reclaim, Contribution, CourseCreated, StudentEnrolled, ClassStarted, PaperThrow
    }
    public sealed class OdysseyFeedback
    {
        readonly int[] memories=new int[4],milestones=new int[4],contributions=new int[4];
        readonly int[][] plots={new int[49],new int[49],new int[49],new int[49]};
        readonly CampusCourse[] courses=new CampusCourse[6];
        bool ready;
        public static bool IsAchievement(OdysseyCue cue)=>(int)cue>=1&&(int)cue<=9;
        public static string Caption(OdysseyCue cue)
        {
            int n=(int)cue;
            return n>=1&&n<=6?OdysseyStory.Chapters[n-1]:n>=7&&n<=9?CampusLessons.Titles[n-7]+" · lesson complete":cue==OdysseyCue.Memory?"Memory collected":cue.ToString();
        }
        public void Reset(OdysseyState state)
        {
            for(int i=0;i<4;i++)
            {memories[i]=state.keepers[i].memories;milestones[i]=state.keepers[i].milestones;contributions[i]=state.keepers[i].contribution;Array.Copy(state.keepers[i].plots,plots[i],49);}
            for(int i=0;i<6;i++)
            {
                var c=state.school.courses[i];
                courses[i]=new CampusCourse{created=c.created,owner=c.owner,plot=c.plot,subject=c.subject,students=c.students,enrolled=c.enrolled,graduates=c.graduates,sessions=c.sessions};
            }
            ready=true;
        }
        public List<OdysseyCue> Observe(OdysseyState state)
        {
            var events=new List<OdysseyCue>();
            if(!ready){Reset(state);return events;}
            // Ignore profiles merely being selected; only real state transitions earn sound.
            for(int k=0;k<4;k++)
            {
                var now=state.keepers[k];
                int found=now.memories&~memories[k];
                for(int i=0;i<12;i++)if((found&(1<<i))!=0)events.Add(OdysseyCue.Memory);
                for(int i=0;i<49;i++)
                {
                    if(now.plots[i]!=plots[k][i]&&now.plots[i]!=0)events.Add((OdysseyCue)((int)OdysseyCue.GardenBuilt+now.plots[i]-1));
                    else if(now.plots[i]==0&&plots[k][i]!=0)events.Add(OdysseyCue.Reclaim);
                }
                if(now.contribution>contributions[k])events.Add(OdysseyCue.Contribution);
                // The shared Beacon updates all four profiles. Announce it once for the player present.
                for(int i=0;i<6;i++)if((now.milestones&~milestones[k]&(1<<i))!=0&&(i!=5||k==state.active))events.Add((OdysseyCue)(i+1));
            }
            for(int i=0;i<6;i++)
            {
                var now=state.school.courses[i];var old=courses[i];if(!now.created)continue;
                if(!old.created)events.Add(OdysseyCue.CourseCreated);
                if((now.enrolled&~old.enrolled)!=0||now.students>old.students)events.Add(OdysseyCue.StudentEnrolled);
                if(now.sessions>old.sessions)events.Add(OdysseyCue.ClassStarted);
                for(int k=0;k<4;k++)if((now.graduates&~old.graduates&(1<<k))!=0)events.Add((OdysseyCue)((int)OdysseyCue.HistoryLesson+now.subject));
            }
            Reset(state);return events;
        }
    }
}
