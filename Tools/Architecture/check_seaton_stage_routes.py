#!/usr/bin/env python3
"""Conservative 2D clearance check; does not verify 3D stair landing traversal."""
import json,re
from pathlib import Path
from collections import deque
import numpy as np
ROOT=Path(__file__).resolve().parents[2]
p=json.loads((ROOT/'Art/Architecture/Seaton-metric-stage.json').read_text())
step=.1;radius=.42+.09
xs=np.arange(-p['width']/2,p['width']/2+step,step);zs=np.arange(-p['depth']/2,p['depth']/2+step,step)
X,Z=np.meshgrid(xs,zs)
results=[]
for f in p['floors']:
 mask=np.zeros(X.shape,dtype=bool)
 for s in f['footprints']:mask|=(abs(X-s['x'])<s['width']/2)&(abs(Z-s['z'])<s['depth']/2)
 # Erode the UNION, not each rectangle: internal footprint seams are not walls.
 raw=mask.copy();margin=int(np.ceil(radius/step))
 padded=np.pad(raw,margin,constant_values=False)
 for dz in range(2*margin+1):
  for dx in range(2*margin+1):mask&=padded[dz:dz+raw.shape[0],dx:dx+raw.shape[1]]
 for s in f['spaces']:mask&=~((abs(X-s['x'])<s['width']/2+radius)&(abs(Z-s['z'])<s['depth']/2+radius))
 labels=np.full(mask.shape,-1,dtype=np.int32);component=0;sizes=[]
 for iz,ix in zip(*np.where(mask)):
  if labels[iz,ix]>=0:continue
  q=deque([(iz,ix)]);labels[iz,ix]=component;size=0
  while q:
   z,x=q.popleft();size+=1
   for a,b in [(z-1,x),(z+1,x),(z,x-1),(z,x+1)]:
    if 0<=a<mask.shape[0] and 0<=b<mask.shape[1] and mask[a,b] and labels[a,b]<0:labels[a,b]=component;q.append((a,b))
  sizes.append(size);component+=1
 main=max(range(len(sizes)),key=sizes.__getitem__)
 for s in f['spaces']:
  if 'doorSideEstimate' not in s:continue
  x,z=s['x'],s['z'];side=s['doorSideEstimate']
  if side=='east':x+=s['width']/2+.65
  elif side=='west':x-=s['width']/2+.65
  elif side=='north':z+=s['depth']/2+.65
  else:z-=s['depth']/2+.65
  stairs=[o['name'] for o in f['spaces'] if o['kind']=='stairs' and abs(x-o['x'])<o['width']/2 and abs(z-o['z'])<o['depth']/2]
  ix=int(round((x-xs[0])/step));iz=int(round((z-zs[0])/step));reachable=0<=iz<len(zs) and 0<=ix<len(xs) and labels[iz,ix]==main
  status='stair landing requires 3D validation' if stairs else 'main corridor reachable' if reachable else 'blocked or disconnected'
  results.append({'room':s['name'],'level':f['level'],'status':status,'approach':[x,z]})
report={'method':'0.1m grid; player radius 0.42m plus 0.09m assumed half-wall margin. Stair footprints excluded from planar circulation. Door positions inferred, not source verified.','roomsChecked':len(results),'corridorReachable':sum(r['status']=='main corridor reachable' for r in results),'stairLandingUnverified':sum(r['status'].startswith('stair landing') for r in results),'blockedOrDisconnected':sum(r['status']=='blocked or disconnected' for r in results),'physicalTraversalVerified':False,'rooms':results}
(ROOT/'Art/Architecture/Seaton-stage-routes.json').write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps({k:v for k,v in report.items() if k!='rooms'},indent=2))
for r in results:
 if r['status']=='blocked or disconnected':print(r)
