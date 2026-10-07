"""Metric room fit for Ingham; staging geometry, not a surveyed building.
Do not activate in the player until bespoke stairs and all doorways are verified.
"""
import json
from collections import deque
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
source=json.loads((ROOT/'Art/Architecture/Ingham-room-dimensions.json').read_text())
M={int(r['room']):r for r in source['rooms']};T=.18
# Source axes are not located on the diagrams. These orientations are an explicit
# fitting interpretation, preserving the published arrangement and room sizes.
axes={101:False,102:True,103:False,104:False,201:False,202:True,203:True,204:True}
def size(n):
    a,b=M[n]['lengthMetres'],M[n]['widthMetres']
    return ((b+T,a+T) if axes[n] else (a+T,b+T))
W=sum(size(n)[0] for n in (202,203,204))
D=sum(size(n)[1] for n in (102,103,104))
levels=[]
def rect(x,z,w,d):return dict(x=round(x+w/2-W/2,5),z=round(z+d/2-D/2,5),width=round(w,5),depth=round(d,5))
def add(name,x,z,w,d,kind):
    assert w>0 and d>0
    s=dict(name=name,kind=kind,**rect(x,z,w,d));spaces.append(s);return s

def room(n,x,z):
    w,d=size(n);s=add(str(n),x,z,w,d,'room')
    s.update(clearWidth=round(w-T,5),clearDepth=round(d-T,5),dimensionSource=source['source'],dimensionStatus='Published pair fitted as clear rectangle; orientation and wall thickness inferred',sourceDiagramPage=1 if n<200 else 2)

for f in range(2):
    spaces=[]
    if f==0:
        w,d=size(101);room(101,0,D-d)
        add('Living room',0,0,w,D-d,'lounge')
        z=0
        for n in (102,103,104):
            w,d=size(n);room(n,W-w,z);z+=d
        add('First floor bath',size(101)[0],9.9,W-size(104)[0]-size(101)[0],D-9.9,'bath')
    else:
        x=0
        for n in (202,203,204):
            w,d=size(n);room(n,x,0);x+=w
        w,d=size(201);room(201,0,D-d)
        start=size(204)[1]+.18
        for i in range(3):add('Second floor bath '+str(i+1),W-2.8,start+i*(D-start)/3,2.8,(D-start)/3,'bath')
    # Reserve the source's central stair area. A rectangular U stair must not be
    # silently substituted for the depicted straight/winder flights.
    add('Central stair · geometry unresolved',5.65,6.35,2.45,3.45,'unresolvedStair')
    levels.append(dict(level=f,name=['First','Second'][f],footprints=[rect(0,0,W,D)],spaces=spaces,circulationCorners=[]))

# Door sides follow the published diagram. Widths and offsets remain estimates.
# Keep these explicit: choosing the longest shared edge would move source doors.
doorSides={101:('east','start'),102:('south','start'),103:('west','end'),104:('west','start'),201:('east','end'),202:('south','end'),203:('south','start'),204:('south','start')}
for level in levels:
    doors=[]
    for s in level['spaces']:
        if s['kind'] not in ('room','bath'):continue
        side,end=doorSides[int(s['name'])] if s['kind']=='room' else (('north','start') if level['level']==0 else ('west','middle'))
        horizontal=side in ('north','south')
        extent=s['width'] if horizontal else s['depth']
        centre=s['x'] if horizontal else s['z']
        # First-floor bath enters the narrow passage west of the stair.
        offset=.65 if s['kind']=='bath' and level['level']==0 else .7
        along=centre-extent/2+offset if end=='start' else centre+extent/2-offset if end=='end' else centre
        x=along if horizontal else s['x']+(-1 if side=='west' else 1)*s['width']/2
        z=s['z']+(-1 if side=='north' else 1)*s['depth']/2 if horizontal else along
        dx,dz={'west':(-1,0),'east':(1,0),'north':(0,-1),'south':(0,1)}[side]
        width=.95
        # Check a 0.95 m wide, 0.65 m deep approach outside every doorway.
        approach=dict(x=x+dx*.6,z=z+dz*.6,width=.65 if dx else width,depth=width if dx else .65)
        for other in level['spaces']:
            if other is s or other['kind']=='lounge':continue
            ox=(approach['width']+other['width'])/2-abs(approach['x']-other['x'])
            oz=(approach['depth']+other['depth'])/2-abs(approach['z']-other['z'])
            assert ox<=.0001 or oz<=.0001,('Blocked door approach',s['name'],other['name'])
        doors.append(dict(space=s['name'],side=side,x=round(x,5),z=round(z,5),width=width,status='Diagram side retained; exact offset, opening width and swing not measured',approach=approach))
    level['doors']=doors

# Spatial connectivity check for the proposed common circulation only. This is
# not a substitute for Unity controller tests, stair traversal or field dimensions.
for level in levels:
    step=.05;radius=.42
    nx,nz=int(W/step),int(D/step)
    obstacles=[s for s in level['spaces'] if s['kind']!='lounge']
    def point(i,j):return (-W/2+(i+.5)*step,-D/2+(j+.5)*step)
    def free(i,j):
        x,z=point(i,j)
        return abs(x)<W/2-radius and abs(z)<D/2-radius and not any(abs(x-s['x'])<s['width']/2+radius+T/2 and abs(z-s['z'])<s['depth']/2+radius+T/2 for s in obstacles)
    available={(i,j) for i in range(nx) for j in range(nz) if free(i,j)}
    if level['level']==0:origin=((size(101)[0]+W-size(102)[0])/2-W/2,-D/2+.4)
    else:
        stair=next(s for s in level['spaces'] if s['kind']=='unresolvedStair')
        origin=(stair['x'],stair['z']-stair['depth']/2-.45)
    def nearest(p):return min(available,key=lambda ij:(point(*ij)[0]-p[0])**2+(point(*ij)[1]-p[1])**2)
    seed=nearest(origin);seen={seed};queue=deque([seed])
    while queue:
        i,j=queue.popleft()
        for k in [(i-1,j),(i+1,j),(i,j-1),(i,j+1)]:
            if k in available and k not in seen:seen.add(k);queue.append(k)
    for door in level['doors']:
        a=door['approach'];target=nearest((a['x'],a['z']))
        assert target in seen,('Disconnected approach',level['name'],door['space'])
        tx,tz=point(*target)
        assert (tx-a['x'])**2+(tz-a['z'])**2<.1**2,('Approach has insufficient clearance',door['space'])
    level['circulationCheck']=dict(method='2D 0.05m grid with 0.42m radius plus 0.09m half-wall; unmodeled stairs treated as obstruction',connectedDoorApproaches=len(level['doors']),runtimeTraversalVerified=False)

# Check enclosed metric rooms: no overlaps, within envelope, correct clear pairs.
for level in levels:
    rooms=[s for s in level['spaces'] if s['kind']=='room']
    for s in rooms:
        a=M[int(s['name'])];assert sorted([s['clearWidth'],s['clearDepth']])==sorted([a['lengthMetres'],a['widthMetres']])
        assert abs(s['x'])+s['width']/2<=W/2+.00001 and abs(s['z'])+s['depth']/2<=D/2+.00001
    for i,a in enumerate(level['spaces']):
        for b in level['spaces'][i+1:]:
            ox=min(a['x']+a['width']/2,b['x']+b['width']/2)-max(a['x']-a['width']/2,b['x']-b['width']/2)
            oz=min(a['z']+a['depth']/2,b['z']+b['depth']/2)-max(a['z']-a['depth']/2,b['z']-b['depth']/2)
            assert ox<.0001 or oz<.0001,(a['name'],b['name'],ox,oz)
plan=dict(building='Ingham Hall',source=source['planSource'],measurementSource=source['source'],status='STAGING ONLY: eight room pairs fitted, common spaces provisional; doors, floor heights, stairs, nonrectangular details and envelope not verified',runtimeReady=False,width=W,depth=D,wallThickness=T,entryX=(size(101)[0]+W-size(102)[0])/2-W/2,rotation=90,floors=levels)
(ROOT/'Art/Architecture/Ingham-staged-plan.json').write_text(json.dumps(plan,indent=2)+'\n')
print('Staged two floors, eight dimensioned bedrooms, four baths; no overlapping spaces; 12 doorway approaches clear. Not runtime-ready.')
