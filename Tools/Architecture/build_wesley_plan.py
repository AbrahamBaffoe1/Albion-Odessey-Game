"""Transcribe published Wesley room adjacency, not a measured building survey.
Coordinates describe the source diagram's topology; metric envelope is provisional.
No geometry is inferred from photographs hidden behind the visible facade.
"""
import json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
source='https://www.albion.edu/wp-content/uploads/2022/02/Wesley-Floor-Plan-and-Room-Dimensions.pdf'
# Diagram columns: residential depth, corridor, residential depth on each wing.
widths=[4.3,2.2,4.3,4.3,2.2,4.3]+[3.4]*6+[4.3,2.2,4.3,4.3,2.2,4.3]
xs=[0]
for w in widths: xs.append(xs[-1]+w)
W=xs[-1]; D=71.4
floors=[]
def rect(x,z,w,d):
    return dict(x=round((xs[x]+xs[x+w])/2-W/2,4),z=round(D/2-(z+d/2)*3.4,4),width=round(xs[x+w]-xs[x],4),depth=round(d*3.4,4))
def add(name,x,z,w=1,d=1,kind='room'):
    spaces.append(dict(name=str(name),kind=kind,stairYaw=180 if kind=='stairs' and any(k in str(name) for k in ['west-middle','west-front','east-rear']) else 0,**rect(x,z,w,d)))
def row(names,x,z,w=1,span=None):
    delta=(span/len(names)) if span else 1
    for i,n in enumerate(names): add(n,x,z+i*delta,w,delta,'bath' if n=='Bath' else 'room')
def cross(names,x,z):
    for i,n in enumerate(names):add(n,x+i,z)
def footprints(f):
    r=[rect(0,0,3,11),rect(15,0,3,11),rect(3,8,3,13),rect(12,8,3,13),rect(6,18,6,3)]
    if f<2:r.append(rect(6,8,6,3))
    if f==0:r.append(rect(6,11,6,7))
    return r
for f in range(4):
    spaces=[]
    # Vertical cores align across levels. The drawing is not a dimensional stair survey.
    for x,z,side in [(2,0,'west-rear'),(15,0,'east-rear'),(3,10,'west-middle'),(14,10,'east-middle'),(3,18,'west-front'),(14,18,'east-front')]:
        if f==0 and side=='west-front':continue # Not present on the published ground-floor plan.
        add('Stairs '+side,x,z,kind='stairs')
    if f==0:
        row(['31-S',27,23,19,'13-RA'],0,0,span=5)
        row([29,25,21,17,15,11],2,1,span=6);add('Bath',2,7,1,2,'bath')
        row(['40-S',36,32,28,24,'20-RA',16,14,12,10],17,0,span=10)
        row([38,34,30,26,22,18],15,1,span=6);add('Bath',15,7,1,2,'bath')
        add('Study lounge',3,8,3,1,'lounge');add('Computer lab',12,8,3,1,'lounge')
        add('Lounge',6,8,6,3,'lounge');add('Kresge Commons',3,13,9,8,'lounge')
        add('Laundry',14,11,1,4,'laundry');add('Bath west',12,17,1,1,'bath');add('Bath east',14,17,1,1,'bath')
    elif f==1:
        row(['159-S',155,151,147,143,139,135,133,131,129,'127-SRA'],0,0)
        row([157,153,149,145,141,137],2,1);add('Bath',2,7,1,2,'bath')
        row(['154-S',150,146,142,138,134,130,128,126,124,'122-SRA'],17,0)
        row([152,148,144,140,136,132],15,1);add('Bath',15,7,1,2,'bath')
        add('Vending west',3,8);add('161-Quad',4,8,2);add('156-Quad',12,8,2);add('Vending east',14,8)
        add('Recreation lounge',6,8,6,3,'lounge')
        add('Storage',5,10);add('Kitchen',12,10,kind='kitchen')
        row([123,119,'115-RA',111,107,105,103],3,11)
        row([125,121,117,113,109],5,11);add('Bath',5,16,1,3,'bath')
        row([120,116,112,108,104],12,11);add('Bath',12,16,1,3,'bath')
        row([118,114,110,'106-RA'],14,11);add('Custodial',14,15,1,2);add(102,14,17)
        add('Living room',8,17,3,2,'lounge');add('Custodial',6,18);add('Elevator',11,18,kind='service')
        add('RD apartment',3,20,4);add('RA office',9,20,2);add('RD office',11,20);add('Staff apartment',12,20,3)
    elif f==2:
        row(['287-S',283,279,275,271,267,263,261,'259-RA',257,'255-PB'],0,0)
        row([285,281,277,273,269,265],2,1);add('Bath',2,7,1,2,'bath')
        row(['286-S',282,278,274,270,266,262,260,'258-RA',256,'254-PB'],17,0)
        row([284,280,276,272,268,264],15,1);add('Bath',15,7,1,2,'bath')
        cross([249,247,245],3,8);row([243,241],5,9);cross([253,251],1,10)
        cross([244,246,248],12,8);row([242,240],12,9);cross([250,252],15,10)
        row([237,233,229,225,221,219,217],3,11);row([239,235,231,227,223],5,11);add('Bath',5,16,1,3,'bath')
        row([238,234,230,226,222],12,11);add('Bath',12,16,1,3,'bath');row([236,232,228,224,220,218,216],14,11)
        cross(['Custodial',205,201,'200-RA',204,'Elevator'],6,18)
        cross([215,'213-RA',211,209,207,203,202,206,208,210,'212-RA',214],3,20)
    else:
        row(['365-S',361,357,353,349,345,341,'339-RA','Lounge',335,333],0,0)
        row([363,359,355,351,347,343],2,1);add('Bath',2,7,1,2,'bath')
        row(['364-S',360,356,352,348,344,340,'338-RA','Lounge',334,332],17,0)
        row([362,358,354,350,346,342],15,1);add('Bath',15,7,1,2,'bath')
        cross([327,325,323],3,8);row([321,319],5,9);cross(['331-S','329-S'],1,10)
        cross([322,324,326],12,8);row([320,318],12,9);cross(['328-S','330-S'],15,10)
        for name,x,z in [('317 Suite',3,11),('313 Suite',3,14),('315 Suite',5,13),('316 Suite',14,11),('312 Suite',14,14),('314 Suite',12,13)]:add(name,x,z,1,3,'suite')
        add('319A Triple',5,11,1,2);add('Kitchen',12,11,1,2,'kitchen')
        add('311-S',3,17);add('310-S',14,17);add('Bath',5,16,1,3,'bath');add('Bath',12,16,1,3,'bath')
        cross(['Custodial',303,301,300,302,'Elevator'],6,18)
        add(309,3,20);add('307 Suite',4,20,3,1,'suite');add('305 Suite',7,20,3,1,'suite');add('304 Suite',10,20,3,1,'suite');add(306,13,20);add(308,14,20)
    corners=[]
    if f>=2:
        # Drawings omit doors: provisional recessed corner vestibules give the
        # four diagonal corner rooms access without routing through a bedroom.
        for x,z in [(1,10),(17,10),(5,9),(13,9)]:
            corners.append(dict(x=round(xs[x]-W/2,4),z=round(D/2-z*3.4,4),width=2.8,depth=2.8))
    floors.append(dict(level=f,name=['Ground','First','Second','Third'][f],footprints=footprints(f),spaces=spaces,circulationCorners=corners))
plan=dict(building='Wesley Hall',source=source,sourcePages=[1,2,3,4],status='Published room adjacency; provisional metric envelope. Source explicitly not drawn to scale. Suite subdivisions, elevations and room measurement integration remain incomplete.',width=W,depth=D,floorHeight=3.2,entryLevel=1,entryX=-3.4,rotation=0,floors=floors)
target=ROOT/'Unity/Assets/Resources/CampusCraft/wesley-plan.json'
target.write_text(json.dumps(plan,indent=2)+'\n')
print(f'{target}: {sum(len(f["spaces"]) for f in floors)} labelled spaces, four floors')
