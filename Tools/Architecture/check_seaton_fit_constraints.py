#!/usr/bin/env python3
"""Check whether measured rooms fit existing Seaton cells without moving walls."""
import json,re
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
plan=json.loads((ROOT/'Unity/Assets/Resources/CampusCraft/seaton-plan.json').read_text())
source=json.loads((ROOT/'Art/Architecture/Seaton-room-dimensions.json').read_text())
lookup={r['room']:r for r in source['rooms']}
wall=.18
rows=[];excluded=[]
for floor in plan['floors']:
 for s in floor['spaces']:
  match=re.match(r'^(\d+)(?:$|[- ·])',s['name'])
  if not match or s['kind']!='room':continue
  r=lookup.get(match[1])
  if not r:continue
  a,b=r.get('lengthMetres'),r.get('widthMetres')
  if not isinstance(a,(float,int)) or not isinstance(b,(float,int)):
   excluded.append({'room':r['room'],'status':r['status']});continue
  current=(s['width']-wall,s['depth']-wall)
  options=[(a,b),(b,a)]
  target=min(options,key=lambda p:max(abs(p[0]-current[0]),abs(p[1]-current[1])))
  dx,dz=target[0]-current[0],target[1]-current[1]
  rows.append({'room':r['room'],'level':floor['level'],'existingClearRectangle':list(current),'publishedPair':[a,b],'bestOrientationTarget':list(target),'widthChange':round(dx,5),'depthChange':round(dz,5),'fitsWithoutExpansion':dx<=.001 and dz<=.001,'sourcePage':r['sourcePage']})
rows.sort(key=lambda r:max(r['widthChange'],r['depthChange']),reverse=True)
report={'method':'Best pair orientation only; no source-axis claim. Existing clear cells assume 0.18m partitions, not raycast geometry.','compared':len(rows),'requiresCellExpansion':sum(not r['fitsWithoutExpansion'] for r in rows),'excluded':excluded,'rooms':rows,'nextAction':'Repack connected room rows and preserve corridor/stair topology before changing live walls. Do not enlarge rooms independently into neighbours or circulation.'}
(ROOT/'Art/Architecture/Seaton-fit-constraints.json').write_text(json.dumps(report,indent=2)+'\n')
lines=['SEATON MEASURED-ROOM FIT CONSTRAINTS','',report['method'],f"{len(rows)} usable room pairs; {report['requiresCellExpansion']} need cell expansion.",'Ambiguous/conflicting source entries remain excluded.','','Largest required changes (metres):']
for r in rows[:15]:lines.append(f"Room {r['room']}: width {r['widthChange']:+.3f}, depth {r['depthChange']:+.3f}")
lines+=['',report['nextAction'],'No live walls changed by this analysis.']
(ROOT/'Docs/SeatonFitConstraints.txt').write_text('\n'.join(lines)+'\n')
print('\n'.join(lines))
