using System;
using System.Linq;
using AlbionOdyssey;
static class WorldSnapshotTests
{
    static void Check(bool ok,string message){if(!ok)throw new Exception("World snapshot: "+message);}
    public static void Run()
    {
        Check(!CampusWorldSnapshot.ValidPose(0,0,0,float.PositiveInfinity,0),"infinite car yaw rejected");
        Check(!CampusWorldSnapshot.ValidPose(0,0,0,0,float.NegativeInfinity),"infinite car speed rejected");
        Check(!CampusWorldSnapshot.ValidPose(0,0,0,0,301),"excessive car speed rejected");
        Check(CampusWorldSnapshot.ValidPose(-930,0,430,180,12),"downtown car pose accepted");
        var s=new CampusWorldSnapshot();
        for(int i=0;i<16;i++)s.students.Add(new CampusPose(100+i*3.217f,.08f,300-i*1.5f,i*22.5f,1.2f));
        for(int i=0;i<6;i++)s.cars.Add(new CampusPose(-500+i*7f,.1f,200+i,350f,-4.5f));
        s.openDoors.Add(CampusWorldSnapshot.DoorId(12.3f,0f,45.6f));s.openDoors.Add(CampusWorldSnapshot.DoorId(-3f,3.5f,9f));
        string wire=s.Encode();
        Check(wire.Length<1400,"snapshot fits one datagram ("+wire.Length+" chars)");
        Check(CampusWorldSnapshot.TryDecode(wire,out var back),"round trip decodes");
        Check(back.students.Count==16&&back.cars.Count==6&&back.openDoors.SequenceEqual(s.openDoors),"counts and doors survive");
        Check(Math.Abs(back.students[5].x-s.students[5].x)<.01f&&Math.Abs(back.students[5].z-s.students[5].z)<.01f,"cm position precision");
        Check(Math.Abs(back.students[4].yaw-90f)<.11f&&Math.Abs(back.cars[0].speed+4.5f)<.11f,"yaw and speed precision");
        Check(CampusWorldSnapshot.DoorId(12.3f,0f,45.6f)!=CampusWorldSnapshot.DoorId(12.3f,0f,45.7f),"door ids separate neighbouring doors");
        Check(!CampusWorldSnapshot.TryDecode("",out _)&&!CampusWorldSnapshot.TryDecode(null,out _),"empty rejected");
        Check(!CampusWorldSnapshot.TryDecode("not base64!!",out _),"garbage rejected");
        Check(!CampusWorldSnapshot.TryDecode(wire.Substring(0,wire.Length/2),out _),"truncated rejected");
        Check(!CampusWorldSnapshot.TryDecode(Convert.ToBase64String(new byte[]{9,0,0,0}),out _),"wrong version rejected");
        Check(!CampusWorldSnapshot.TryDecode(Convert.ToBase64String(new byte[]{1,200,0,0}),out _),"actor count above limit rejected");
        var trailing=Convert.FromBase64String(wire).Concat(new byte[]{1}).ToArray();
        Check(!CampusWorldSnapshot.TryDecode(Convert.ToBase64String(trailing),out _),"trailing bytes rejected");
        var wild=new CampusWorldSnapshot();wild.students.Add(new CampusPose(float.NaN,1e9f,-1e9f,-45f,float.PositiveInfinity));
        Check(CampusWorldSnapshot.TryDecode(wild.Encode(),out var tame)&&Math.Abs(tame.students[0].x)<=2000f&&Math.Abs(tame.students[0].y)<=2000f&&tame.students[0].yaw>=0f,"hostile floats are clamped on encode");
    }
}
