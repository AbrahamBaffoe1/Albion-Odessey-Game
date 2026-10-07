"""Fit published Whitehouse clear room dimensions into the diagram's topology.
Wall thickness, structural infill, corridor widths and common-room dimensions are
provisional. Matching this table does not establish a surveyed building replica.
"""
import json,re
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
T=.18
measurements=json.loads((ROOT/'Art/Architecture/Whitehouse-room-dimensions.json').read_text())
measured={int(r['room']):r for r in measurements['rooms']}
def dims(name):
    n=int(re.match(r'\d+',str(name))[0]);a=measured.get(n)
    return (a['widthMetres']+T,a['lengthMetres']+T) if a else (3.5+T,6.3+T)
def total(names):return sum(dims(n)[0] for n in names)
leftcols={f:[f*100+n for n in [11,13,15,17,19]] for f in [2,3]}
rightcols={f:[f*100+n for n in [12,14,16,18]] for f in [2,3]}
west=max(dims(n)[1] for ns in leftcols.values() for n in ns)
corridor=2.4
right=west+corridor
outer=right+max(dims(n)[1] for ns in rightcols.values() for n in ns)
north=max(3.4,dims(200)[0])
northV=north+max(total(ns) for ns in rightcols.values())
mid=outer+max(total([226,228]),total([326,328]))
midEnd=mid+3.4
northRows={0:['032','034','036','038'],1:[132,134,136,138],2:[232,234,236,'238 · RA'],3:[332,334,336,'338 · RA']}
southRows={0:['031','033','035','037 · RA'],1:[121,123,125,127,129,131,133,'135 · RA',137],2:['221 · RA',223,225,227,229,231,233,235,237],3:['321 · RA',323,325,327,329,331,333,335,337]}
southStarts={0:midEnd,1:right,2:west,3:west}
mainEnd=max(max(midEnd+total(ns) for ns in northRows.values()),max(southStarts[f]+total(ns) for f,ns in southRows.items()))
eastBottom=mainEnd+3.4
eastStair=eastBottom+max(total([f*100+n for n in [41,43,45,47]]) for f in range(4))
eastEnd=eastStair+3.4
eastTop=eastEnd-max(total([f*100+n for n in [42,44,46,48]]) for f in range(4))
maxNorthDepth=max(dims(n)[1] for ns in northRows.values() for n in ns)
mainCorr=northV+maxNorthDepth
mainSouth=mainCorr+corridor
bottomNames=[n for ns in southRows.values() for n in ns]+['021','023','025','027']
D=mainSouth+max(dims(n)[1] for n in bottomNames)
W=eastEnd
frontV=D
eastV=northV-3.4
eastCorr=eastV+max(dims(f*100+n)[1] for f in range(4) for n in [42,44,46,48])
eastSouth=eastCorr+corridor
eastBack=eastSouth+max(dims(f*100+n)[1] for f in range(4) for n in [41,43,45,47])
# A shared stair envelope across every level; unmeasured flight geometry.
eastBack=max(eastBack,eastSouth+6.4)
leftLoungeBack=mainSouth+3.4
floors=[];audit=[]
def r(x,v,w,d):return dict(x=round(x+w/2-W/2,5),z=round(D/2-v-d/2,5),width=round(w,5),depth=round(d,5))
def add(name,x,v,w,d,kind='room',yaw=0):
    assert w>0 and d>0,(name,w,d)
    s=dict(name=str(name),kind=kind,stairYaw=yaw,**r(x,v,w,d));spaces.append(s);return s

def room(name,x,v,sideways=False):
    w,d=dims(name);w,d=(d,w) if sideways else (w,d)
    s=add(name,x,v,w,d);n=int(re.match(r'\d+',str(name))[0]);m=measured.get(n)
    s.update(dimensionSourcePage=5 if m else 0,clearWidth=round(w-T,5),clearDepth=round(d-T,5),dimensionStatus='Published values interpreted as a clear rectangle; measurement method, wall thickness and placement provisional' if m else 'No published measurement; provisional room 226')
    audit.append(dict(room=n,level=f,modelClearWidth=s['clearWidth'],modelClearDepth=s['clearDepth'],publishedLength=m['lengthMetres'] if m else None,publishedWidth=m['widthMetres'] if m else None,sideways=sideways))
    return w,d

def row(names,x,v,front=False):
    for n in names:
        w,d=dims(n);room(n,x,v-d if front else v);x+=w
    return x

for f in range(4):
    spaces=[]
    add('Stairs Hannah end',0,0,west,north,'stairs',180)
    add('Stairs middle',mid,northV,3.4,mainCorr-northV,'stairs',-90)
    add('Stairs east end',eastStair,eastSouth,3.4,eastBack-eastSouth,'stairs',90)
    if f>=2:
        v=north
        for n in leftcols[f]:_,d=room(n,0,v,True);v+=d
        leftEnd=v
        v=north
        for n in rightcols[f]:w,d=dims(n);room(n,outer-d,v,True);v+=w
        rightEnd=v
        if f==2:w,d=dims(200);room('200-S',outer-d,0,True)
        else:add('Unlabelled blocked space',right,0,outer-right,north,'void')
        start=mid-total([f*100+26,f*100+28])
        if start>outer+.001:add('Unmeasured structural infill',outer,northV,start-outer,mainCorr-northV,'void')
        row([f*100+26,f*100+28],start,northV)
        serviceV=rightEnd
    else:
        serviceV=mainCorr-3
        add('North lounge',right,north,outer-right,serviceV-north,'lounge')
        add('Lounge extension',outer,northV-6.4,mid-outer,serviceV-(northV-6.4),'lounge')
        add('Bathrooms',outer,serviceV,mid-outer,mainCorr-serviceV,'bath')
        if f==0:
            v=north
            for name,height,kind in [('Kitchen',2,'service'),('Laundry',3.4,'laundry'),('Custodial west',5,'service')]:add(name,0,v,west,height,kind);v+=height
            add('Mechanical',0,v,west,serviceV-v,'service');leftEnd=serviceV
            row(['021','023','025','027'],right,frontV,True)
        else:
            add('RA office',right,0,(outer-right)/2,north,'service');add('RD office',right+(outer-right)/2,0,(outer-right)/2,north,'service')
            room('119',0,northV,True);leftEnd=northV+dims(119)[0]
            add('RD apartment',0,north,west,northV-north,'apartment')
    split=(outer-right)/2
    add('Elevator shaft',right,serviceV,split,mainCorr-serviceV,'service')
    add('Custodial' if f==0 else 'Study by elevator',right+split,serviceV,split,mainCorr-serviceV,'service')
    rowEnd=row(northRows[f],midEnd,northV)
    southEnd=row(southRows[f],southStarts[f],frontV,True)
    lowerLeft=eastStair-total([f*100+n for n in [41,43,45,47]])
    upperLeft=eastEnd-total([f*100+n for n in [42,44,46,48]])
    # Lounges absorb the unmeasured difference between the independently sized rows.
    add('East lounge',rowEnd,eastV,upperLeft-rowEnd,mainCorr-eastV,'lounge')
    add('Computer lounge' if f==0 else 'Study east' if f==1 else 'South lounge',southEnd,mainSouth,lowerLeft-southEnd,D-mainSouth,'service' if f==1 else 'lounge')
    row([f*100+n for n in [42,44,46,48]],upperLeft,eastV)
    row([f*100+n for n in [41,43,45,47]],lowerLeft,eastBack,True)
    add('Hannah lounge',0,leftEnd,west if f>=2 else right,leftLoungeBack-leftEnd,'lounge')
    footprints=[r(0,0,outer,leftLoungeBack),r(west,northV,lowerLeft-west,D-northV),r(rowEnd,eastV,eastEnd-rowEnd,eastBack-eastV)]
    if f<2:footprints.append(r(outer,northV-6.4,mid-outer,6.4))
    if f==0:footprints.append(r(west,0,mid-west,north))
    floors.append(dict(level=f,name=['Ground','First','Second','Third'][f],footprints=footprints,spaces=spaces,circulationCorners=[]))

# Exposed lower roofs, derived by subtracting the next floor's occupancy.
def occupied(level,x,v):
    return any(abs(x-a['x'])<a['width']/2-.000001 and abs(v-a['z'])<a['depth']/2-.000001 for a in level['footprints']+level['spaces'])
terraces=[]
for f in range(3):
    shapes=floors[f]['footprints']+floors[f]['spaces']+floors[f+1]['footprints']+floors[f+1]['spaces']
    xs=sorted(set(round(a['x']+sign*a['width']/2,5) for a in shapes for sign in [-1,1]));zs=sorted(set(round(a['z']+sign*a['depth']/2,5) for a in shapes for sign in [-1,1]))
    cells={(i,j) for i in range(len(xs)-1) for j in range(len(zs)-1) if occupied(floors[f],(xs[i]+xs[i+1])/2,(zs[j]+zs[j+1])/2) and not occupied(floors[f+1],(xs[i]+xs[i+1])/2,(zs[j]+zs[j+1])/2)}
    while cells:
        i,j=min(cells);e=i+1
        while (e,j) in cells:e+=1
        k=j+1
        while all((a,k) in cells for a in range(i,e)):k+=1
        for a in range(i,e):
            for b in range(j,k):cells.remove((a,b))
        if xs[e]-xs[i]>.001 and zs[k]-zs[j]>.001:terraces.append(dict(x=(xs[e]+xs[i])/2,z=(zs[k]+zs[j])/2,width=xs[e]-xs[i],depth=zs[k]-zs[j],level=f+1))
first={int(re.match(r'\d+',s['name'])[0]):s for s in floors[1]['spaces'] if s['kind']=='room'}
facadeLeft=first[129]['x']-first[129]['width']/2;facadeRight=first[133]['x']+first[133]['width']/2
entry=(right+total(['021','023','025','027'])+midEnd)/2-W/2
plan=dict(building='Whitehouse Hall',source=measurements['source'],sourcePages=[1,2,3,4,5],status='106 published length/width pairs fitted as clear rectangles; overall structure, wall thickness, corridors, elevations and facade remain provisional. Room 226 has no published dimensions.',width=W,depth=D,floorHeight=3.2,wallThickness=T,entryX=entry,entryLevel=0,rotation=0,baseElevation=0,facadeLeft=facadeLeft,facadeRight=facadeRight,terraces=terraces,roofSections=[r(west,northV,mainEnd-west,D-northV),r(0,0,outer,northV)],floors=floors)
(ROOT/'Unity/Assets/Resources/CampusCraft/whitehouse-plan.json').write_text(json.dumps(plan,indent=2)+'\n')
for a in audit:
    if a['publishedLength'] is not None:
        expect=(a['publishedLength'],a['publishedWidth']) if a['sideways'] else (a['publishedWidth'],a['publishedLength'])
        assert abs(a['modelClearWidth']-expect[0])<.00001 and abs(a['modelClearDepth']-expect[1])<.00001
assert len(audit)==107 and sum(a['publishedLength'] is not None for a in audit)==106
(ROOT/'Art/Architecture/Whitehouse-metric-fit.json').write_text(json.dumps(dict(status='Data-fit audit only; runtime clearances verified separately. Full-building accuracy remains unproven.',wallThicknessEstimate=T,width=W,depth=D,rooms=audit),indent=2)+'\n')
print('Envelope',W,D,'; 106 dimensioned rooms, one unmeasured room; terrace pieces',len(terraces))
