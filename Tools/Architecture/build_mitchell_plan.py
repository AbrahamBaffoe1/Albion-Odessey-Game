"""Four official not-to-scale diagrams: two towers, first-floor link only.
No bathroom subdivisions or room dimensions are supplied by this PDF.
"""
import json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
W,D=57.2,29.6
floors=[]
def r(x,v,w,d):return dict(x=round(x+w/2-W/2,4),z=round(D/2-v-d/2,4),width=round(w,4),depth=round(d,4))
def add(n,x,v,w,d,kind='room',yaw=0,group=''):spaces.append(dict(name=str(n),kind=kind,stairYaw=yaw,group=group,**r(x,v,w,d)))
for f in range(4):
    spaces=[];footprints=[];base=(f+1)*100
    for tower,off in [('North',0),('South',33.4)]:
        north=tower=='North'
        footprints.extend([r(off,2.8,23.8,21.2),r(off+3.4,0,17,29.6)])
        add(tower+' Mingo stairs',off+10.2,0,3.4,5.6,'stairs',-90)
        add(tower+' campus stairs',off+10.2,24,3.4,5.6,'stairs',90)
        add(tower+' elevator shaft',off+10.2,18.8,3.4,2.8,'service')
        add(tower+' central lounge',off+6.8,8,10.2,10.8,'lounge')
        add(tower+' Mingo study A',off,2.8,3.4,5.2,'service')
        add(tower+' Mingo study B',off+20.4,2.8,3.4,5.2,'service')
        nums=[9,10,11,12] if north else [32,31,30,29]
        for n,x in zip(nums,[3.4,6.8,13.6,17]):add(base+n,off+x,0,3.4,5.6)
        left=[8,7,6,5] if north else [33,34,35,36]
        right=[13,14,15,16] if north else [28,27,26,25]
        for i,n in enumerate(left):
            suffix=' · RA' if n==7 else ' SH' if f==0 and n==36 else ''
            add(str(base+n)+suffix,off,8+3.4*i,6.8,3.4)
        for i,n in enumerate(right):
            if f==0 and ((north and n==16) or (not north and n in [26,25])):continue
            suffix=' · RA' if n==27 else ' PB' if f==0 and n==15 else ''
            add(str(base+n)+suffix,off+17,8+3.4*i,6.8,3.4)
        if f==0:
            if north:
                add('Spare RD apartment · 103',off,21.6,10.2,2.4,'apartment',group='north-rd')
                add('103 apartment sleeping area',off+3.4,24,6.8,5.6,'apartment',group='north-rd')
                add('Bathroom',off+17,18.8,3.4,2.8,'bath')
                add('Computer room',off+20.4,18.8,3.4,2.8,'service')
                for n,x in [(102,13.6),(101,17)]:add(n,off+x,24,3.4,5.6)
                add('Link lounge',off+20.4,24,3.4,5.6,'lounge');footprints.append(r(off+20.4,24,3.4,5.6))
            else:
                add('RD apartment living area',off+17,14.8,6.8,9.2,'apartment',group='south-rd')
                add('RD apartment sleeping area',off+13.6,24,6.8,5.6,'apartment',group='south-rd')
                add('Vending',off,24,3.4,5.6,'service');footprints.append(r(off,24,3.4,5.6))
                for n,x in [(121,3.4),(122,6.8)]:add(n,off+x,24,3.4,5.6)
        else:
            for n,x in zip([4,3,2,1] if north else [21,22,23,24],[3.4,6.8,13.6,17]):add(base+n,off+x,24,3.4,5.6)
            add(tower+' sink area',off+(0 if north else 20.4),21.6,3.4,2.4,'service')
            add(tower+' janitor',off+(20.4 if north else 0),21.6,3.4,2.4,'service')
    if f==0:
        footprints.append(r(23.8,18.8,9.6,10.8))
        add('RD office',23.8,18.8,3.2,2.8,'service')
        add('Lobby stair · elevation unresolved',27,18.8,3.2,5.6,'unresolvedStair')
        add('RA office',27,24.4,3.2,2.8,'service')
        add('Connecting lobby',23.8,27.2,9.6,2.4,'lounge')
    floors.append(dict(level=f,name=['First','Second','Third','Fourth'][f],footprints=footprints,spaces=spaces,circulationCorners=[]))
plan=dict(building='Mitchell Towers',source='https://www.albion.edu/wp-content/uploads/2022/02/Mitchell-Towers-Floor-Plan.pdf',sourcePages=[1,2,3,4],status='Published two-tower topology and room sequence. Envelope, heights and window bays are estimates. Suite-bathroom partitions and lobby-stair elevation remain unresolved.',width=W,depth=D,floorHeight=3.2,entryX=0,entryLevel=0,rotation=90,baseElevation=0,floors=floors)
(ROOT/'Unity/Assets/Resources/CampusCraft/mitchell-plan.json').write_text(json.dumps(plan,indent=2)+'\n')
for f in floors:print(f['name'],len(f['spaces']),'spaces;',sum(s['kind']=='room' for s in f['spaces']),'bedrooms')
