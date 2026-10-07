"""Published Seaton topology. Do not mistake diagram cells for survey dimensions.
The plan is rotated 180 degrees in-world: Cass north, return wing west toward Baldwin.
"""
import json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
W,D=60.6,29.4
floors=[]
def r(x,z,w,d):return dict(x=round(x+w/2-W/2,4),z=round(z+d/2-D/2,4),width=round(w,4),depth=round(d,4))
def add(name,x,z,w,d,kind='room',yaw=0):spaces.append(dict(name=str(name),kind=kind,stairYaw=yaw,**r(x,z,w,d)))
def bar(name,col,side='front',span=1,kind='room'):
    add(name,4.6+(col-1)*3.5,0 if side=='front' else 6.5,span*3.5,4.3,kind)
def row(names,x,z,width=4.3):
    for i,name in enumerate(names):add(name,x,z+i*3.5,width,3.5)
for f in range(4):
    spaces=[]
    add('Stairs east end',0,4.3,4.6,6.5,'stairs',180)
    bar('Stairs middle',11,'back',kind='stairs');spaces[-1]['stairYaw']=-90
    add('Stairs Baldwin end',54.1,24.8,6.5,4.6,'stairs',-90)
    bar('Bathroom east',1,'back',2,'bath');add('Bathroom west',46.6,6.5,7.5,4.3,'bath')
    if f==0:
        for i,n in enumerate(['24-H',25,26,27,28,29,30,31]):bar(n,3+i,'back')
        bar(32,12,'back')
        bar(21,1);bar('Storage east',2,kind='service')
        for i,n in enumerate([18,17,16,'15-RA']):bar(n,3+i)
        bar('Storage middle',7,span=4,kind='service');bar(12,11);bar(11,12);bar('Laundry',13,span=2,kind='laundry');bar('10-RA',15);bar(9,16)
        row([39,40,41,'42-S'],49.8,10.8);add('43-S',49.8,24.8,4.3,4.6)
        add(6,56.3,6.5,4.3,4.3);row([5,4,3,2],56.3,10.8)
    elif f==1:
        for i,n in enumerate([135,136,137,'138-RA']):bar(n,3+i,'back')
        bar('Lounge',7,'back',4,'lounge');bar('140 · RD office',12,'back',kind='service')
        for i,n in enumerate([128,127,126,125,124,'122-S','121-S','RA office']):bar(n,i+1,kind='service' if i==7 else 'room')
        # Column nine is the documented front entrance, not a bedroom.
        bar('RD apartment',10,span=3,kind='apartment')
        for i,n in enumerate([111,110,'109-RA',108]):bar(n,13+i)
        row([147,148,149,'150-S'],49.8,10.8);add('151-S',49.8,24.8,4.3,4.6)
        add(105,56.3,6.5,4.3,4.3);row([104,103,102,101],56.3,10.8)
    elif f==2:
        for i,n in enumerate([229,230,231,'232-RA',233,234,235,236]):bar(n,i+3,'back')
        bar(238,12,'back')
        for i,n in enumerate([223,222,221,220,'219 · Computer lab',218,217,216,215,214,213,212,211,210,'209-RA',208]):bar(n,i+1,kind='lounge' if i==4 else 'room')
        row([244,245,246,'247-S'],49.8,10.8);add('248-S',49.8,24.8,4.3,4.6)
        add(205,56.3,6.5,4.3,4.3);row([204,203,202,201],56.3,10.8)
    else:
        for i,n in enumerate([328,329,330,'331-RA',332,333,334,335]):bar(n,i+3,'back')
        bar(337,12,'back')
        for i,n in enumerate([322,321,320,319,'318 · Study lounge',317,316,315,314,313,312,311,310,309,'308-RA',307]):bar(n,i+1,kind='lounge' if i==4 else 'room')
        row([344,345,346,'347 · Storage'],49.8,10.8);spaces[-1]['kind']='service'
        # The third-floor diagram shows an unlabelled space over the lower single.
        add('Unlabelled space above 248',49.8,24.8,4.3,4.6,'service')
        add(305,56.3,6.5,4.3,4.3);row([304,303,302,301],56.3,10.8)
    footprint=[r(4.6,0,W-4.6,10.8),r(0,4.3,4.6,6.5),r(49.8,10.8,10.8,D-10.8)]
    floors.append(dict(level=f,name=['Ground','First','Second','Third'][f],footprints=footprint,spaces=spaces,circulationCorners=[]))
plan=dict(building='Seaton Hall',source='https://www.albion.edu/wp-content/uploads/2022/02/Seaton-Floor-Plan-and-Room-Dimensions.pdf',sourcePages=[1,2,3,4],status='Published room sequence and L-shaped topology; provisional metric envelope, elevations, window bays and stair dimensions. Source diagrams are not drawn to scale.',width=W,depth=D,floorHeight=3.2,entryLevel=1,entryX=4.05,rotation=180,baseElevation=-2.4,floors=floors)
p=ROOT/'Unity/Assets/Resources/CampusCraft/seaton-plan.json';p.write_text(json.dumps(plan,indent=2)+'\n');print(len([s for f in floors for s in f['spaces']]),'labelled spaces')
