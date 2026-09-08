using System;
using System.Collections.Generic;
namespace AlbionOdyssey.BuildingDesigner
{
    public enum StudioPart { Floor, Wall, Door, Window, Roof, Table, Chair, Planter }
    [Serializable] public sealed class BuildingBlueprint
    {
        public const int Width=12,Cells=144,Edges=156;
        public int version=1;
        public string title="My campus building";
        public int[] floors=new int[Cells],roofs=new int[Cells],furniture=new int[Cells],rotations=new int[Cells];
        public int[] horizontal=new int[Edges],vertical=new int[Edges];
        public static bool Cell(int x,int z)=>x>=0&&x<Width&&z>=0&&z<Width;
        public bool HasFloor(int x,int z)=>Cell(x,z)&&floors[z*Width+x]==1;
        public bool SupportedEdge(bool alongX,int x,int z)=>alongX?(HasFloor(x,z-1)||HasFloor(x,z)):(HasFloor(x-1,z)||HasFloor(x,z));
        public static bool Edge(bool alongX,int x,int z)=>alongX?x>=0&&x<Width&&z>=0&&z<=Width:x>=0&&x<=Width&&z>=0&&z<Width;
        public BuildingBlueprint Copy()=>new BuildingBlueprint{version=version,title=title,floors=(int[])floors.Clone(),roofs=(int[])roofs.Clone(),furniture=(int[])furniture.Clone(),rotations=(int[])rotations.Clone(),horizontal=(int[])horizontal.Clone(),vertical=(int[])vertical.Clone()};
        public bool Place(StudioPart part,int x,int z,bool alongX,int rotation,bool erase)
        {
            if((int)part<0||(int)part>7||rotation<0||rotation>3)return false;
            if(part>=StudioPart.Wall&&part<=StudioPart.Window)
            {
                if(!Edge(alongX,x,z)||!SupportedEdge(alongX,x,z))return false;
                int[] edges=alongX?horizontal:vertical;int i=z*(alongX?Width:Width+1)+x;
                int value=erase?0:(int)part;if(edges[i]==value)return false;edges[i]=value;return true;
            }
            if(!Cell(x,z))return false;
            int index=z*Width+x;
            if(part==StudioPart.Floor)
            {
                int value=erase?0:1;if(floors[index]==value)return false;floors[index]=value;
                if(erase)
                {
                    roofs[index]=furniture[index]=rotations[index]=0;
                    for(int zz=0;zz<=Width;zz++)for(int xx=0;xx<Width;xx++)if(!SupportedEdge(true,xx,zz))horizontal[zz*Width+xx]=0;
                    for(int zz=0;zz<Width;zz++)for(int xx=0;xx<=Width;xx++)if(!SupportedEdge(false,xx,zz))vertical[zz*(Width+1)+xx]=0;
                }
                return true;
            }
            if(!HasFloor(x,z))return false;
            if(part==StudioPart.Roof){int value=erase?0:1;if(roofs[index]==value)return false;roofs[index]=value;return true;}
            int kind=erase?0:(int)part-4;
            if(furniture[index]==kind&&(erase||rotations[index]==rotation))return false;
            furniture[index]=kind;rotations[index]=erase?0:rotation;return true;
        }
        public bool Valid()
        {
            if(version!=1||string.IsNullOrWhiteSpace(title)||title.Length>40)return false;
            foreach(char c in title)if(char.IsControl(c)||c=='<'||c=='>')return false;
            if(floors==null||floors.Length!=Cells||roofs==null||roofs.Length!=Cells||furniture==null||furniture.Length!=Cells||rotations==null||rotations.Length!=Cells||horizontal==null||horizontal.Length!=Edges||vertical==null||vertical.Length!=Edges)return false;
            for(int i=0;i<Cells;i++)
            {
                if(floors[i]<0||floors[i]>1||roofs[i]<0||roofs[i]>1||furniture[i]<0||furniture[i]>3||rotations[i]<0||rotations[i]>3)return false;
                if(floors[i]==0&&(roofs[i]!=0||furniture[i]!=0)||furniture[i]==0&&rotations[i]!=0)return false;
            }
            for(int z=0;z<=Width;z++)for(int x=0;x<Width;x++){int v=horizontal[z*Width+x];if(v<0||v>3||v!=0&&!SupportedEdge(true,x,z))return false;}
            for(int z=0;z<Width;z++)for(int x=0;x<=Width;x++){int v=vertical[z*(Width+1)+x];if(v<0||v>3||v!=0&&!SupportedEdge(false,x,z))return false;}
            return true;
        }
        public static BuildingBlueprint Classroom()
        {
            var b=new BuildingBlueprint{title="My learning studio"};
            for(int z=3;z<=8;z++)for(int x=3;x<=8;x++)b.Place(StudioPart.Floor,x,z,true,0,false);
            for(int x=3;x<=8;x++){b.Place(x==5?StudioPart.Door:StudioPart.Wall,x,3,true,0,false);b.Place(x%2==0?StudioPart.Window:StudioPart.Wall,x,9,true,0,false);}
            for(int z=3;z<=8;z++){b.Place(z%2==0?StudioPart.Window:StudioPart.Wall,3,z,false,0,false);b.Place(z%2==0?StudioPart.Window:StudioPart.Wall,9,z,false,0,false);}
            foreach(int x in new[]{4,7})foreach(int z in new[]{5,7}){b.Place(StudioPart.Table,x,z,true,0,false);b.Place(StudioPart.Chair,x,z-1,true,0,false);}
            b.Place(StudioPart.Planter,8,8,true,0,false);return b;
        }
    }
    public sealed class BlueprintHistory
    {
        readonly List<BuildingBlueprint> undo=new List<BuildingBlueprint>(),redo=new List<BuildingBlueprint>();
        public int UndoCount=>undo.Count;
        public void Record(BuildingBlueprint b){undo.Add(b.Copy());if(undo.Count>40)undo.RemoveAt(0);redo.Clear();}
        public BuildingBlueprint Undo(BuildingBlueprint b){if(undo.Count==0)return b;redo.Add(b.Copy());var result=undo[undo.Count-1];undo.RemoveAt(undo.Count-1);return result;}
        public BuildingBlueprint Redo(BuildingBlueprint b){if(redo.Count==0)return b;undo.Add(b.Copy());var result=redo[redo.Count-1];redo.RemoveAt(redo.Count-1);return result;}
        public void Clear(){undo.Clear();redo.Clear();}
    }
}
