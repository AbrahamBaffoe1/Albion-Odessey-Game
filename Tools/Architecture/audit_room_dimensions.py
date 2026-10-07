"""Compare only corresponding published spaces with model wall-centre rectangles.
Suite bedrooms/common rooms are distinct spaces, never aliases for their suite.
This is a discrepancy audit, not a physical clearance or survey certificate.
"""
import json,re
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]

def identity(label):
    match=re.match(r'^(\d+)([A-B])?(?=\b|[- ·])',str(label).strip(),re.I)
    if not match:return None
    number=str(int(match[1]));suffix=(match[2] or '').upper()
    if re.search(r'\bCommon\b',str(label),re.I):suffix=' Common'
    return number+suffix

def audit(name):
    source=json.loads((ROOT/f'Art/Architecture/{name}-room-dimensions.json').read_text())
    plan=json.loads((ROOT/f'Unity/Assets/Resources/CampusCraft/{name.lower()}-plan.json').read_text())
    rooms={};suites={}
    for floor in plan['floors']:
        for space in floor['spaces']:
            key=identity(space['name'])
            if not key:continue
            if space['kind']=='suite':suites[key]=space;continue
            if key in rooms:raise ValueError(f'Duplicate model space identity: {name} {key}')
            rooms[key]=(floor['level'],space)
    rows=[];unknown=[];unmatched=[];subdivisions=[];nonmetric=[];conflicts=[]
    for measurement in source['rooms']:
        key=identity(measurement['room'])
        a=measurement.get('lengthMetres',measurement.get('metres1'))
        b=measurement.get('widthMetres',measurement.get('metres2'))
        if a is None or b is None:
            entry=dict(room=measurement['room'],status=measurement.get('status','No numeric source dimension'))
            if 'source conflict' in entry['status']:
                entry.update(extractedLengthText=measurement.get('extractedLengthText'),visualLengthText=measurement.get('visualLengthText'))
                conflicts.append(entry)
            elif 'Storage' in str(measurement):nonmetric.append(entry)
            else:unknown.append(entry)
            continue
        if key not in rooms:
            parent=re.match(r'\d+',key or '')
            entry=dict(room=measurement['room'],publishedLength=a,publishedWidth=b,sourcePage=measurement.get('sourcePage',5))
            if parent and parent[0] in suites:
                entry.update(modelSuite=suites[parent[0]]['name'],status='Suite envelope exists; this individual bedroom/common space is not modeled')
                subdivisions.append(entry)
            else:unmatched.append(entry)
            continue
        level,space=rooms[key];w,d=space['width']-.18,space['depth']-.18
        error=min(max(abs(w-a),abs(d-b)),max(abs(w-b),abs(d-a)))
        rows.append(dict(room=measurement['room'],modelSpace=space['name'],level=level,publishedLength=a,publishedWidth=b,nominalClearWidth=round(w,5),nominalClearDepth=round(d,5),maximumAxisError=round(error,5)))
    return dict(building=name,source=source.get('source'),method='Corresponding individual spaces only; best orientation match, assuming 0.18 m partitions. Runtime finish thickness is not raycast. No surveyed accuracy claim.',compared=len(rows),ambiguousSourceRooms=unknown,conflictingSourceRooms=conflicts,nonMetricSourceEntries=nonmetric,unmatchedSourceRooms=unmatched,unmodeledSuiteSpaces=subdivisions,maximumAxisErrorMetres=max((r['maximumAxisError'] for r in rows),default=None),rooms=sorted(rows,key=lambda r:-r['maximumAxisError']))

if __name__=='__main__':
    reports=[audit(name) for name in ['Wesley','Seaton','Whitehouse']]
    path=ROOT/'Art/Architecture/Room-dimension-discrepancies.json';path.write_text(json.dumps(reports,indent=2)+'\n')
    for r in reports:print(r['building'],r['compared'],'compared; largest nominal discrepancy',r['maximumAxisErrorMetres'],'m; unmodeled suite spaces',len(r['unmodeledSuiteSpaces']),'ambiguous',len(r['ambiguousSourceRooms']),'source conflicts',len(r['conflictingSourceRooms']))
