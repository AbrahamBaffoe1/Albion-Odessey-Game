using System;
using AlbionOdyssey;
static class ClockTests
{
    static void Check(bool ok,string message){if(!ok)throw new Exception("Campus clock: "+message);}
    public static void Run()
    {
        Check(Math.Abs(CampusClock.Wrap(25f)-1f)<1e-4&&Math.Abs(CampusClock.Wrap(-1f)-23f)<1e-4,"wraps across midnight");
        Check(Math.Abs(CampusClock.Advance(0f,CampusClock.DayLengthSeconds)-0f)<1e-3,"one day length returns to start");
        Check(Math.Abs(CampusClock.Delta(23f,1f)-2f)<1e-4&&Math.Abs(CampusClock.Delta(1f,23f)+2f)<1e-4,"shortest delta across midnight");
        Check(CampusClock.SunElevation(12f)>60f&&CampusClock.SunElevation(0f)<-60f,"sun peaks at noon and is far below at midnight");
        Check(CampusClock.Daylight(12f)>.99f&&CampusClock.Daylight(0f)<.01f,"day and night daylight");
        Check(CampusClock.Golden(6.5f)>.5f&&CampusClock.Golden(12f)==0f,"golden hour near the horizon only");
        Check(CampusClock.SunElevation(CampusClock.StartHour)>30f,"start hour is a bright afternoon");
        Check(Math.Abs(CampusClock.Follow(23.5f,.1f,.016f)-.1f)<1e-3,"snaps only past the threshold");
        float near=CampusClock.Follow(10f,10.1f,.016f);Check(near>10f&&near<10.1f,"smooth follow while close");
        Check(CampusClock.Label(0f)=="12:00 AM"&&CampusClock.Label(15.5f)=="3:30 PM","12-hour labels");
    }
}
