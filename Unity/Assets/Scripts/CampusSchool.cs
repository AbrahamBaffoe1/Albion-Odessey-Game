using System;
namespace AlbionOdyssey
{
    [Serializable] public sealed class CampusCourse
    {
        public bool created;
        public string title;
        public int owner,plot,subject,students,enrolled,graduates,sessions;
        public int Seats => students+OdysseyState.Count(enrolled);
    }
    [Serializable] public sealed class CampusSchool
    {
        public CampusCourse[] courses=EmptyCourses();
        static CampusCourse[] EmptyCourses()
        {var slots=new CampusCourse[6];for(int i=0;i<slots.Length;i++)slots[i]=new CampusCourse();return slots;}
        public int active=-1;
        public bool Create(OdysseyState state,string name,int subject)
        {
            name=(name??"").Trim();
            if(!GoodTitle(name)||subject<0||subject>2)return false;
            int slot=Array.FindIndex(courses,c=>c!=null&&!c.created);
            if(slot<0)return false;
            for(int p=0;p<49;p++)
            {
                int kind=state.Current.plots[p];
                if((kind==2||kind==4)&&Array.FindIndex(courses,c=>c!=null&&c.created&&c.owner==state.active&&c.plot==p)<0)
                {courses[slot]=new CampusCourse{created=true,title=name,owner=state.active,plot=p,subject=subject};active=slot;return true;}
            }
            return false;
        }
        public bool Enroll(int slot,int keeper)
        {
            if(!Exists(slot)||keeper<0||keeper>3)return false;
            var c=courses[slot];int bit=1<<keeper;
            if((c.enrolled&bit)!=0||c.Seats>=12)return false;
            c.enrolled|=bit;return true;
        }
        public bool AssignStudents(int slot,int owner,int change)
        {
            if(!Exists(slot)||courses[slot].owner!=owner||Math.Abs((long)change)!=1)return false;
            var c=courses[slot];if(c.students+change<0||c.Seats+change>12)return false;
            c.students+=change;return true;
        }
        public bool Answer(int slot,int keeper,int answer)
        {
            if(!Exists(slot)||keeper<0||keeper>3)return false;
            var c=courses[slot];int bit=1<<keeper;
            if((c.enrolled&bit)==0||(c.graduates&bit)!=0||answer!=CampusLessons.Correct[c.subject])return false;
            c.graduates|=bit;return true;
        }
        public bool Teach(int slot,int owner)
        {
            if(!Exists(slot)||courses[slot].owner!=owner||courses[slot].Seats==0||courses[slot].sessions>=10000)return false;
            active=slot;courses[slot].sessions++;return true;
        }
        public bool Remove(int slot,int owner)
        {
            if(!Exists(slot)||courses[slot].owner!=owner)return false;
            courses[slot]=new CampusCourse();if(active==slot)active=-1;return true;
        }
        public bool Exists(int i)=>i>=0&&i<courses.Length&&courses[i]!=null&&courses[i].created;
        public bool UsesPlot(int keeper,int plot)=>Array.FindIndex(courses,c=>c!=null&&c.created&&c.owner==keeper&&c.plot==plot)>=0;
        static bool GoodTitle(string name)
        {
            if(string.IsNullOrWhiteSpace(name)||name.Length>40)return false;
            foreach(char c in name)if(char.IsControl(c)||c=='<'||c=='>')return false;
            return true;
        }
        public bool Valid(OdysseyState state)
        {
            if(courses==null||courses.Length!=6||active< -1||active>=6||(active>=0&&!Exists(active)))return false;
            for(int i=0;i<6;i++)
            {
                var c=courses[i];if(c==null)return false;
                if(!c.created){if(!string.IsNullOrEmpty(c.title)||c.owner!=0||c.plot!=0||c.subject!=0||c.students!=0||c.enrolled!=0||c.graduates!=0||c.sessions!=0)return false;continue;}
                if(!GoodTitle(c.title)||c.owner<0||c.owner>3||c.plot<0||c.plot>=49||c.subject<0||c.subject>2||c.students<0||c.students>12||c.enrolled<0||c.enrolled>15||c.graduates<0||c.graduates>15||(c.graduates&~c.enrolled)!=0||c.Seats>12||c.sessions<0||c.sessions>10000)return false;
                int kind=state.keepers[c.owner].plots[c.plot];if(kind!=2&&kind!=4)return false;
                for(int j=0;j<i;j++)if(courses[j]!=null&&courses[j].created&&courses[j].owner==c.owner&&courses[j].plot==c.plot)return false;
            }
            return true;
        }
    }
    public static class CampusLessons
    {
        public static readonly string[] Titles={"Albion's beginnings","Observing the stars","Design a learning space"};
        public static readonly string[] Text={
            "Albion received its charter from the Michigan Territorial Legislature in 1835. In 1861, the legislature authorized four-year degrees for both men and women. The first permanent building's cornerstone was laid in 1840 on the site now known as the Quad.",
            "Albion's observatory cornerstone was laid on September 8, 1883. Construction finished in summer 1884. Its first-floor room originally served physics, mathematics and astronomy classes. The observatory combined teaching with astronomical observation.",
            "Game design workshop: a learning space needs a clear entrance, an open walking route and seating that faces the lesson. In this prototype, build a Hall or Library in your personal campus, then give that building a course. This workshop is original game content."
        };
        public static readonly string[] Sources={"https://www.albion.edu/about/at-a-glance/our-history/","https://www.albion.edu/departments/physics/observatory-history/",""};
        public static readonly string[] Questions={"In which year did Albion receive its charter?","When was the observatory completed?","What should a classroom keep clear?"};
        public static readonly string[][] Answers={new[]{"1835","1884","1901"},new[]{"1835","1884","1922"},new[]{"Only the roof","Every display wall","The entrance and walking route"}};
        public static readonly int[] Correct={0,1,2};
    }
}
