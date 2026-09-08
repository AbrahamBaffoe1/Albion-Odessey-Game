using System;
using System.Text.Json;
using AlbionOdyssey.BuildingDesigner;
class Program
{
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 static void Main()
 {
  var b=new BuildingBlueprint();Check(b.Valid(),"empty blueprint");
  Check(!b.Place(StudioPart.Roof,1,1,true,0,false)&&!b.Place(StudioPart.Wall,1,1,true,0,false),"no floating parts");
  Check(b.Place(StudioPart.Floor,1,1,true,0,false),"floor");
  Check(b.Place(StudioPart.Door,1,1,true,0,false),"door edge");Check(b.horizontal[13]==2,"door identity");
  Check(b.Place(StudioPart.Window,1,1,true,0,false)&&b.horizontal[13]==3,"replace wall type");
  Check(b.Place(StudioPart.Table,1,1,true,3,false)&&b.rotations[13]==3,"furniture rotation");
  Check(b.Place(StudioPart.Roof,1,1,true,0,false),"roof support");
  Check(b.Place(StudioPart.Floor,1,1,true,0,true)&&b.Valid()&&b.horizontal[13]==0&&b.furniture[13]==0&&b.roofs[13]==0,"floor removal cleans dependencies");
  b=BuildingBlueprint.Classroom();Check(b.Valid(),"classroom template");
  var history=new BlueprintHistory();history.Record(b);b.Place(StudioPart.Roof,5,5,true,0,false);
  b=history.Undo(b);Check(b.roofs[65]==0,"undo");b=history.Redo(b);Check(b.roofs[65]==1,"redo");
  var opts=new JsonSerializerOptions{IncludeFields=true};var loaded=JsonSerializer.Deserialize<BuildingBlueprint>(JsonSerializer.Serialize(b,opts),opts);Check(loaded.Valid()&&loaded.horizontal[3*12+5]==2,"roundtrip door and shape");
  loaded.horizontal=new int[999];Check(!loaded.Valid(),"bad array length rejected");
  var random=new Random(1206);
  for(int i=0;i<100000;i++)
  {if(i%1000==0)b=BuildingBlueprint.Classroom();b.Place((StudioPart)random.Next(-2,10),random.Next(-1,14),random.Next(-1,14),random.Next(2)==0,random.Next(-1,5),random.Next(3)==0);Check(b.Valid(),"random edit "+i);}
  for(int i=0;i<100;i++)history.Record(b);Check(history.UndoCount==40,"bounded undo memory");
  Console.WriteLine("BUILDING_BLUEPRINT_OK: 100000 edits, support rules, door/window replacement, furniture, cascade removal, roundtrip, undo and redo");
 }
}
