#!/usr/bin/env python3
"""Isolated room-fit experiment. Expanded envelope/infill are NOT surveyed architecture."""
import copy,json,re
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
base=json.loads((ROOT/'Unity/Assets/Resources/CampusCraft/seaton-plan.json').read_text())
checks=json.loads((ROOT/'Art/Architecture/Seaton-fit-constraints.json').read_text())
by={(r['level'],r['room']):r for r in checks['rooms']}
T=.18
sx=sz=1.
for f in base['floors']:
 for s in f['spaces']:
  n=re.match(r'\d+',s['name']);r=by.get((f['level'],n[0])) if n else None
  if r and s['kind']=='room':
   sx=max(sx,(r['bestOrientationTarget'][0]+T)/s['width']);sz=max(sz,(r['bestOrientationTarget'][1]+T)/s['depth'])
plan=copy.deepcopy(base);plan.update(building='Seaton metric fitting experiment',runtimeReady=False,exactReplica=False,status='110 published room rectangles fitted inside a uniformly expanded provisional topology. Infill and expanded shell are experimental, not source-backed architecture.',width=base['width']*sx,depth=base['depth']*sz,entryX=base['entryX']*sx)
audit=[]
def rect(name,kind,x0,x1,z0,z1):
 return dict(name=name,kind=kind,x=(x0+x1)/2,z=(z0+z1)/2,width=x1-x0,depth=z1-z0,stairYaw=0)
for f in plan['floors']:
 additions=[]
 for s in f['footprints']+f['spaces']:
  s['x']*=sx;s['z']*=sz;s['width']*=sx;s['depth']*=sz
 for s in f['spaces']:
  n=re.match(r'\d+',s['name']);r=by.get((f['level'],n[0])) if n else None
  if not r or s['kind']!='room':continue
  x0,x1=s['x']-s['width']/2,s['x']+s['width']/2
  z0,z1=s['z']-s['depth']/2,s['z']+s['depth']/2
  w,d=[v+T for v in r['bestOrientationTarget']]
  # Original topology: main front row, main back row, inner/outer return wing.
  originalX=s['x']/sx+base['width']/2;originalZ=s['z']/sz+base['depth']/2
  side='east' if originalX<55 and originalZ>10.8 else 'west' if originalX>55 and originalZ>6 else 'north' if originalZ<4.3 else 'south'
  a,b=(x0+x1-w)/2,(z0+z1-d)/2
  if side=='east':a=x1-w
  elif side=='west':a=x0
  elif side=='north':b=z1-d
  else:b=z0
  s.update(x=a+w/2,z=b+d/2,width=w,depth=d,clearWidth=w-T,clearDepth=d-T,dimensionSourcePage=5,doorSideEstimate=side)
  # Keep unused cell area blocked rather than inventing corridors between bedrooms.
  for ax,bx,az,bz in [(x0,a,z0,z1),(a+w,x1,z0,z1),(a,a+w,z0,b),(a,a+w,b+d,z1)]:
   if bx-ax>1e-5 and bz-az>1e-5:additions.append(rect('Provisional infill beside '+s['name'],'void',ax,bx,az,bz))
  error=max(abs((w-T)-r['bestOrientationTarget'][0]),abs((d-T)-r['bestOrientationTarget'][1]))
  audit.append({'room':n[0],'level':f['level'],'errorMetres':error,'doorSideEstimate':side})
 f['spaces']+=additions
 # Check every pair, not just measured bedrooms.
 for i,a in enumerate(f['spaces']):
  for b in f['spaces'][i+1:]:
   dx=min(a['x']+a['width']/2,b['x']+b['width']/2)-max(a['x']-a['width']/2,b['x']-b['width']/2)
   dz=min(a['z']+a['depth']/2,b['z']+b['depth']/2)-max(a['z']-a['depth']/2,b['z']-b['depth']/2)
   assert min(dx,dz)<1e-5,(f['level'],a['name'],b['name'],dx,dz)
assert len(audit)==110
(ROOT/'Art/Architecture/Seaton-metric-stage.json').write_text(json.dumps(plan,indent=2)+'\n')
report={'fittedRooms':len(audit),'maxPairErrorMetres':max(r['errorMetres'] for r in audit),'overlaps':0,'expansionX':sx,'expansionZ':sz,'width':plan['width'],'depth':plan['depth'],'runtimeVerified':False,'liveInstalled':False,'sourceAxisOrientationVerified':False,'exactReplica':False,'rooms':audit}
(ROOT/'Art/Architecture/Seaton-metric-stage-audit.json').write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps({k:v for k,v in report.items() if k!='rooms'},indent=2))
