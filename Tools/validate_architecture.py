"""Check generated full-size geometry and a headroom route through all floors.
This validates exported collider dimensions, not Unreal's physics implementation.
"""
import json
import math
from pathlib import Path

root=Path(__file__).resolve().parents[1]
folder=root/"Art"/"Architecture"
m=json.loads((folder/"architecture_manifest.json").read_text())
assert m["units"]=="meters" and m["floors"]==8
assert 28<=m["height_m"]<=40
assert m["entrance_clear_width_m"]>=1.2
assert m["stairs"]["riser_m"]<=.20 and m["stairs"]["tread_m"]>=.28
assert len(m["assets"])==12
boxes=[]
for a in m["assets"]:
    assert (folder/a["file"]).read_bytes().startswith(b"Kaydara FBX Binary")
    assert a["collision_hulls"]==len(a["collision_boxes"])>0
    assert a["triangles"]>100
    for b in a["collision_boxes"]:
        assert all(math.isfinite(v) for v in b["center"]+b["size"])
        assert all(v>0 for v in b["size"])
        lo=[c-s/2 for c,s in zip(b["center"],b["size"])]
        hi=[c+s/2 for c,s in zip(b["center"],b["size"])]
        boxes.append((lo,hi,b["name"]))

checks=0
def check_position(x,y,feet):
    global checks
    checks+=1
    for lo,hi,name in boxes:
        # Conservative cylinder around a 1.92 m character above its step height.
        if hi[2]<=feet+.43 or lo[2]>=feet+1.92: continue
        dx=max(lo[0]-x,0,x-hi[0]); dy=max(lo[1]-y,0,y-hi[1])
        assert dx*dx+dy*dy>=.42*.42, f"Blocked player headroom at {(x,y,feet)} by {name} {lo} {hi}"
    support=[hi[2] for lo,hi,_ in boxes if lo[0]-1e-6<=x<=hi[0]+1e-6 and lo[1]-1e-6<=y<=hi[1]+1e-6 and feet-.45<=hi[2]<=feet+.01]
    assert support, f"No supporting surface at {(x,y,feet)}"

def route(points):
    for a,b in zip(points,points[1:]):
        count=max(1,math.ceil(math.dist(a,b)/.15))
        for i in range(count+1): check_position(*(a[k]+(b[k]-a[k])*i/count for k in range(3)))

route([(0,-22,0),(0,3,0),(0,0,0),(3.1,0,0),(3.1,-1.4,0),(9.1,-1.4,0),(9.1,1.2,0)])
for floor in range(7):
    z=floor*3.6
    for step in range(10): check_position(9.1,1.2+(step+.5)*.3,z+(step+1)*.18)
    route([(9.1,4.05,z+1.8),(9.1,4.8,z+1.8),(11.5,4.8,z+1.8),(11.5,4.2,z+1.8)])
    for step in range(10): check_position(11.5,4.2-(step+.5)*.3,z+1.8+(step+1)*.18)
    z+=3.6
    route([(11.5,1.35,z),(11.5,.6,z),(11.5,-1.4,z),(3.1,-1.4,z),(3.1,0,z),(0,0,z),(0,3,z),(0,0,z),(3.1,0,z),(3.1,-1.4,z),(9.1,-1.4,z),(9.1,1.2,z)])
route([(0,-26,0),(-40,-26,0),(-40,6,0)])
route([(-40,-14,0),(-64,-14,0),(-64,3,0),(-67.5,3,0),(-60.5,3,0)])
print(f"PASS: {len(m['assets'])} full-size FBX modules, {len(boxes)} authored collision hulls, {checks} supported headroom samples across eight floors")
