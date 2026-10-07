#!/usr/bin/env python3
"""Inventory actual construction routes; never equate playability with fidelity."""
import re,json,collections
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
catalog=(ROOT/'Unity/Assets/Scripts/CampusCatalog.cs').read_text()
builders=(ROOT/'Unity/Assets/Scripts/CampusCraft/CampusBuildings.cs').read_text()
walkable=set(re.findall(r'CampusExpansion.Find\("([^"]+)"\)',builders))
plans={'46':'mitchell','49':'seaton','50':'wesley','51':'whitehouse'}
rows=[]
for id,name,category,shape in re.findall(r'new CampusPlace\("([^"]+)","([^"]+)","([^"]+)","([^"]+)"',catalog):
 route='generic sealed exterior';evidence=[];gaps=['Measured exterior and interior geometry','Floor elevations and stairs','Current facade details']
 if id in plans:
  route='published-plan reconstruction';p=ROOT/f'Unity/Assets/Resources/CampusCraft/{plans[id]}-plan.json';assert p.exists(),p;evidence=[str(p.relative_to(ROOT))];gaps=['Complete measured geometry','Current detailed elevations','Unresolved rooms, partitions and stair dimensions']
 elif id=='26':route='authored Ferguson model; dimensions unverified'
 elif id=='16':route='authored Robinson model; dimensions unverified'
 elif id in ['18k','18n','18p','18u']:route='combined generic Science Complex interior';gaps=['Four distinct building envelopes and connections','Measured laboratory layouts','Floor elevations and stairs']
 elif id in walkable or category=='Greek life':route='generic walkable reconstruction'
 elif id=='44':route='photo-informed exterior; interior only staged';evidence=['Art/Architecture/Ingham-staged-plan.json'];gaps=['Measured stair geometry and vertical alignment','Reconcile staged footprint with exterior','Complete current elevations']
 elif shape in ['field','stadium','baseball']:route='generic athletics geometry';gaps=['Surveyed field and stand geometry','Current site detail']
 if id=='20':evidence=['Art/Architecture/Stockwell-shell-trace.json'];gaps=['Install reconciled full building','Current floor/stack elevations and stairs','Mudd connection','Measured side/rear elevations'];route+='; separate Stockwell study'
 rows.append({'id':id,'name':name,'category':category,'liveConstruction':route,'supportingArtifacts':evidence,'remaining':gaps,'exactReplicaVerified':False})
assert len(rows)==len({r['id'] for r in rows})
report={'scope':'Every CampusCatalog destination; not a claim that the catalog exhausts campus structures','catalogCount':len(rows),'exactReplicasVerified':0,'siteStatus':'Schematic map positions scaled at 1.5 game metres per pixel; no surveyed site/terrain/planting validation','coverageCounts':dict(collections.Counter(r['liveConstruction'] for r in rows)),'limitations':['Static code-route audit only, not a rendered or physical verification','Supporting artifacts are evidence references, not proof of full accuracy','Small structures, utility spaces and grounds absent from the catalog still need a source inventory'],'destinations':rows}
(ROOT/'Art/Architecture/Campus-replica-coverage.json').write_text(json.dumps(report,indent=2)+'\n')
lines=['CAMPUS REPLICA COVERAGE','',f'{len(rows)} catalog destinations reviewed. No complete exact replica verified.','Site coordinates remain schematic, not surveyed.','','Construction route counts:']
lines += [f'{count}: {route}' for route,count in report['coverageCounts'].items()]
lines += ['','Per-destination status:']
for r in rows:lines += [f"{r['id']} — {r['name']}: {r['liveConstruction']}", '  Remaining: '+ '; '.join(r['remaining'])]
lines+=['','This is a static implementation inventory. Runtime tests for selected routes do not establish campus-wide accuracy.','The catalog itself is not a surveyed inventory of every campus structure or landscape feature.']
(ROOT/'Docs/CampusReplicaCoverage.txt').write_text('\n'.join(lines)+'\n')
print(json.dumps(report['coverageCounts'],indent=2))
