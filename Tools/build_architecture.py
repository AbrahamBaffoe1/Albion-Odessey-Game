"""Build real, meter-scale editable architecture and Unreal collision meshes.

Blender --background --python Tools/build_architecture.py
Legacy Tower is an original eight-storey game building, not a surveyed replica
of an Albion residence. The .blend and FBXs are the deliverables, not the renders.
"""
import bpy
import json
import math
import numpy as np
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "Art" / "Architecture"
OUT.mkdir(parents=True, exist_ok=True)
FLOORS, RISE = 8, 3.6
bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete(use_global=False)
scene = bpy.context.scene
scene.name = "Legacy Tower - full size architecture"
scene.unit_settings.system = "METRIC"
scene.unit_settings.scale_length = 1.0
collections = {}

def collection(name):
    c = bpy.data.collections.new(name)
    scene.collection.children.link(c)
    collections[name] = c
    return c

environment = collection("00 Site and human scale")
levels = [collection(f"{i+1:02d} Level {i+1} - editable walls floors stairs") for i in range(FLOORS)]
roof = collection("09 Roof and plant enclosure")
collision_collection = collection("90 Unreal UCX collision - hidden")
export_collection = collection("91 Export meshes - hidden")
current = environment
collisions = {}

def mat(name, rgb, roughness=.65, metallic=0):
    m = bpy.data.materials.new(name)
    m.diffuse_color = (*rgb, 1)
    m.use_nodes = True
    p = m.node_tree.nodes.get("Principled BSDF")
    p.inputs["Base Color"].default_value = (*rgb, 1)
    p.inputs["Roughness"].default_value = roughness
    p.inputs["Metallic"].default_value = metallic
    return m

brick = mat("M_Architecture_Brick", (.36,.115,.055), .9)
stone = mat("M_Architecture_Stone", (.62,.61,.55), .8)
floor_mat = mat("M_Architecture_Floor", (.30,.32,.33), .78)
plaster = mat("M_Architecture_Plaster", (.78,.77,.70), .88)
metal = mat("M_Architecture_Metal", (.035,.046,.055), .3, .7)
glass = mat("M_Architecture_Glass", (.10,.22,.27), .16, .25)
glass.node_tree.nodes.get("Principled BSDF").inputs["Transmission Weight"].default_value=.65
wood = mat("M_Architecture_Wood", (.27,.12,.045), .55)
grass = mat("M_Architecture_Grass", (.16,.21,.095), .95)
asphalt = mat("M_Architecture_Asphalt", (.045,.05,.055), .94)
accent = mat("M_Architecture_Accent", (.30,.14,.44), .5)

# Original seamless textures. One UV unit is 1.44 meters: six bricks x 16 courses.
n=1024
yy,xx=np.mgrid[0:n,0:n]
rows=np.floor(yy/n*16).astype(int)
bx=(xx/n*6 + (rows%2)*.5)%1
by=(yy/n*16)%1
edge=np.minimum(np.minimum(bx,1-bx)*.24,np.minimum(by,1-by)*.09)
height=np.clip((edge-.003)/.004,0,1).astype(np.float32)
rng=np.random.default_rng(1835)
bricknoise=rng.uniform(-.035,.035,(16,7))
cols=np.floor(xx/n*6 + (rows%2)*.5).astype(int)
variation=bricknoise[rows,cols]+rng.normal(0,.012,(n,n))
base=np.zeros((n,n,4),dtype=np.float32)
for k,c in enumerate([.43,.18,.10]): base[:,:,k]=np.where(height>.1,c+variation,.39+variation*.2)
base[:,:,3]=1
gy,gx=np.gradient(height)
normal=np.dstack((-gx*1.5,-gy*1.5,np.ones((n,n))))
normal/=np.linalg.norm(normal,axis=2,keepdims=True)
normal=np.dstack((normal*.5+.5,np.ones((n,n)))).astype(np.float32)
rough=np.dstack([np.clip(.8+variation,0,1)]*3+[np.ones((n,n))]).astype(np.float32)
textures={}
for suffix,pixels in [("BaseColor",base),("Normal",normal),("Roughness",rough)]:
    im=bpy.data.images.new("T_Architecture_Brick_"+suffix,width=n,height=n,alpha=True)
    if suffix!="BaseColor": im.colorspace_settings.name="Non-Color"
    im.pixels.foreach_set(pixels.ravel())
    im.filepath_raw=str(OUT/(im.name+".png"))
    im.file_format="PNG"
    im.save()
    textures[suffix]=im
nodes=brick.node_tree.nodes
links=brick.node_tree.links
p=nodes.get("Principled BSDF")
for suffix,socket in [("BaseColor","Base Color"),("Roughness","Roughness")]:
    tex=nodes.new("ShaderNodeTexImage"); tex.image=textures[suffix]
    links.new(tex.outputs["Color"],p.inputs[socket])
tex=nodes.new("ShaderNodeTexImage"); tex.image=textures["Normal"]
normalnode=nodes.new("ShaderNodeNormalMap")
links.new(tex.outputs["Color"],normalnode.inputs["Color"])
links.new(normalnode.outputs["Normal"],p.inputs["Normal"])

def move_to(obj, coll):
    for c in list(obj.users_collection): c.objects.unlink(obj)
    coll.objects.link(obj)

def cube_object(name, pos, size, target):
    x,y,z=(v/2 for v in size)
    vertices=[(-x,-y,-z),(x,-y,-z),(x,y,-z),(-x,y,-z),(-x,-y,z),(x,-y,z),(x,y,z),(-x,y,z)]
    faces=[(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)]
    mesh=bpy.data.meshes.new(name)
    mesh.from_pydata(vertices,[],faces)
    mesh.update()
    mesh.uv_layers.new(name="UVMap")
    obj=bpy.data.objects.new(name,mesh)
    target.objects.link(obj)
    obj.location=pos
    return obj

def box(name, pos, size, material, solid=True, bevel=.015):
    obj=cube_object(name,pos,size,current)
    obj.data.materials.append(material)
    # World-scale face UVs preserve brick size on long walls, piers, and bands.
    uv=obj.data.uv_layers.active
    for face in obj.data.polygons:
        ax=max(range(3),key=lambda k:abs(face.normal[k]))
        axes=[k for k in range(3) if k!=ax]
        for li in face.loop_indices:
            v=obj.data.vertices[obj.data.loops[li].vertex_index].co+obj.location
            uv.data[li].uv=(v[axes[0]]/1.44,v[axes[1]]/1.44)
    if bevel:
        mod=obj.modifiers.new("Construction edge chamfer","BEVEL"); mod.width=bevel; mod.segments=1
        obj.modifiers.new("Weighted normals","WEIGHTED_NORMAL")
    if solid:
        collisions.setdefault(current.name,[]).append({"center":list(pos),"size":list(size),"name":name})
    return obj

def rail(a,b,z):
    # Axis-aligned rails, with a solid top rail and spaced vertical balusters.
    dx,dy=b[0]-a[0],b[1]-a[1]
    length=math.hypot(dx,dy)
    box("Guardrail handrail",((a[0]+b[0])/2,(a[1]+b[1])/2,z+1.05),(abs(dx)+.05,abs(dy)+.05,.06),metal)
    for t in np.linspace(0,1,max(2,int(length/.7)+1)):
        box("Guardrail post",(a[0]+dx*t,a[1]+dy*t,z+.52),(.045,.045,1.04),metal,False)

def window(x,y,z,side=False):
    if not side:
        box("Recessed glass pane",(x,y,z),(2.0,.065,1.90),glass,True,0)
        for dx in [-1.025,0,1.025]: box("Window vertical frame",(x+dx,y-.035,z),(.065,.12,2.02),metal,False,.004)
        for dz in [-.97,.97]: box("Window horizontal frame",(x,y-.035,z+dz),(2.1,.12,.07),metal,False,.004)
        box("Projecting stone sill",(x,y-.08,z-1.01),(2.18,.28,.12),stone,False)
    else:
        box("Recessed side glass",(x,y,z),(.065,2.0,1.90),glass,True,0)
        for dy in [-1.025,0,1.025]: box("Side vertical frame",(x,y+dy,z),(.12,.065,2.02),metal,False,.004)
        for dz in [-.97,.97]: box("Side horizontal frame",(x,y,z+dz),(.12,2.1,.07),metal,False,.004)
        box("Side stone sill",(x,y,z-1.01),(.28,2.18,.12),stone,False)

def front_wall(y,z,floor):
    front=y<0
    if floor==0 and front:
        for x in [-7.65,7.65]: box("Entry ground spandrel",(x,y,z+.45),(12.7,.30,.90),brick)
    else: box("Brick spandrel",(0,y,z+.45),(28,.30,.90),brick)
    box("Continuous lintel",(0,y,z+3.25),(28,.30,.70),brick)
    cursor=-14
    for x in range(-12,13,3):
        left=x-1.05
        if left>cursor: box("Brick pier",((cursor+left)/2,y,z+1.90),(left-cursor,.30,2.0),brick)
        if floor==0 and front and x==0:
            # Actual open doorway, not a picture of a door on a solid wall.
            pass
        else: window(x,y,z+1.90)
        cursor=x+1.05
    box("Corner pier",((cursor+14)/2,y,z+1.90),(14-cursor,.30,2.0),brick)

def side_wall(x,z):
    box("Side spandrel",(x,0,z+.45),(.30,20,.9),brick)
    box("Side lintel",(x,0,z+3.25),(.30,20,.70),brick)
    cursor=-10
    for y in [-7.5,-4.5,-1.5,1.5,4.5,7.5]:
        left=y-1.05
        box("Side pier",(x,(cursor+left)/2,z+1.90),(.30,left-cursor,2.0),brick)
        window(x,y,z+1.90,True)
        cursor=y+1.05
    box("Side corner pier",(x,(cursor+10)/2,z+1.90),(.30,10-cursor,2.0),brick)

def stairs(z):
    # Two 1.8 m wide flights, 18 cm risers and 30 cm treads, 1.5 m landing.
    for i in range(10):
        h=(i+1)*.18
        box("Lower flight tread",(9.1,1.2+(i+.5)*.30,z+h-.06),(1.8,.30,.12),stone)
        box("Upper flight tread",(11.5,4.2-(i+.5)*.30,z+1.8+h-.06),(1.8,.30,.12),stone)
    box("Stair half landing",(10.3,4.95,z+1.7),(4.2,1.5,.20),stone)
    rail((8.2,4.2),(8.2,5.7),z+1.8)
    rail((8.2,5.7),(12.4,5.7),z+1.8)
    rail((12.4,4.2),(12.4,5.7),z+1.8)
    for x in [8.16,10.04,10.56,12.44]:
        for i in range(11):
            h=(i/10)*1.8 if x<10.1 else 1.8+(1-i/10)*1.8
            box("Stair baluster",(x,1.2+i*.3,z+h+.50),(.04,.04,1.0),metal,False)
        # Inclined rails are visual meshes; tread and landing collision is explicit.
        o=box("Sloping handrail",(x,2.7,z+(0.9 if x<10.1 else 2.7)+1.0),(.055,math.hypot(3,1.8),.055),metal,False)
        o.rotation_euler[0]=math.atan2(1.8,3)*(1 if x<10.1 else -1)

for i in range(FLOORS):
    print(f"Building editable level {i+1}/{FLOORS}",flush=True)
    current=levels[i]
    z=i*RISE
    if i==0:
        box("Ground floor slab",(0,0,-.12),(28,20,.24),floor_mat)
    else:
        # Four slabs surround a genuine stair opening x=8..12.6, y=1.2..5.9.
        box("Floor west of stairwell",(-3,0,z-.12),(22,20,.24),floor_mat)
        box("Floor east strip",(13.3,0,z-.12),(1.4,20,.24),floor_mat)
        box("Floor south of stairwell",(10.3,-4.4,z-.12),(4.6,11.2,.24),floor_mat)
        box("Floor north of stairwell",(10.3,7.95,z-.12),(4.6,4.1,.24),floor_mat)
        rail((8,1.2),(8,5.9),z)
        rail((8,5.9),(12.6,5.9),z)
        rail((12.6,1.2),(12.6,5.9),z)
    front_wall(-10,z,i); front_wall(10,z,i)
    side_wall(-14,z); side_wall(14,z)
    for y in [-10.20,10.20]: box("Limestone floor belt",(0,y,z+.05),(28.5,.26,.18),stone)
    for x in [-14.20,14.20]: box("Side floor belt",(x,0,z+.05),(.26,20.5,.18),stone)
    # A central circulation hall and side rooms, with real door openings.
    for x in [-2.3,2.3]:
        for ya,yb in [(-9.8,-5.9),(-4.1,-.9),(.9,4.1),(5.9,9.8)]:
            box("Hall partition",(x,(ya+yb)/2,z+1.65),(.16,yb-ya,3.3),plaster)
        for y in [-5,0,5]: box("Doorway header",(x,y,z+2.95),(.16,1.8,.70),plaster)
    for x in [-8,5.3]:
        for y in [-3.0,7.5]:
            box("Concrete column",(x,y,z+1.68),(.40,.40,3.36),stone)
    if i<FLOORS-1: stairs(z)
    # Human-scale desks and benches make the interior legible at player height.
    for x,y in [(-7,-6),(-7,3),(6,-6)]:
        box("Study desk top",(x,y,z+.76),(1.4,.70,.06),wood)
        for dx in [-.6,.6]: box("Desk leg",(x+dx,y,z+.36),(.06,.55,.72),metal)
    for x,y in [(-6,0),(6,0)]:
        box("Lobby bench seat",(x,y,z+.48),(2.1,.65,.14),wood)
        box("Lobby bench back",(x,y+.29,z+.85),(2.1,.08,.65),wood)
        for dx in [-.8,.8]: box("Bench support",(x+dx,y,z+.22),(.10,.5,.44),metal)

current=roof
z=FLOORS*RISE
box("Roof slab",(0,0,z),(28.4,20.4,.3),floor_mat)
for y in [-10,10]: box("Roof parapet",(0,y,z+.6),(28.4,.3,1.2),brick)
for x in [-14,14]: box("Roof parapet side",(x,0,z+.6),(.3,20.4,1.2),brick)
for y in [-10.05,10.05]: box("Roof coping",(0,y,z+1.25),(28.6,.5,.12),stone)
for x in [-14.05,14.05]: box("Roof coping side",(x,0,z+1.25),(.5,20.6,.12),stone)
box("Roof plant enclosure",(5,4,z+2),(5,5,4),metal)
for y in [1.45,6.55]:
    for k in range(12): box("Plant ventilation louver",(5,y,z+.3+k*.30),(4.7,.09,.09),stone,False)

current=environment
box("Campus ground",(0,0,-.42),(180,180,.5),grass)
box("Entry plaza",(0,-18,-.12),(42,16,.24),stone)
box("Street",(0,-33,-.11),(180,9,.22),asphalt)
for x in range(-80,81,8): box("Road marking",(x,-33,.007),(4,.12,.015),stone,False,0)
for x in [-17,17]: box("Sidewalk",(x,4.5,-.12),(4,29,.24),stone)
box("Entry canopy",(0,-11.8,3.4),(7.5,4,.24),metal)
for x in [-3.4,3.4]: box("Canopy support",(x,-13,1.6),(.18,.18,3.2),metal)
# Entrance stays unobstructed between the two jambs. No closed door collision.
for x in [-1.13,1.13]: box("Entry jamb",(x,-10.18,1.45),(.12,.16,2.9),metal)
box("Entry transom",(0,-10.18,2.96),(2.38,.16,.12),metal)

def label(text,pos,size):
    curve=bpy.data.curves.new(text,"FONT")
    curve.body=text; curve.align_x="CENTER"; curve.size=size; curve.extrude=.004
    o=bpy.data.objects.new(text,curve); current.objects.link(o)
    o.location=pos; o.rotation_euler=(math.pi/2,0,0); o.data.materials.append(stone)
label("LEGACY HALL",(0,-10.32,3.82),.58)

# An explicit 1.80 m scale figure is part of the Blender scene, not the game export.
human=collection("80 Human reference - 1.80 meters")
current=human
box("Human torso - 1.80m reference",(0,-16,1.18),(.44,.24,.62),accent,False,.08)
for x in [-.13,.13]: box("Human leg",(x,-16,.45),(.18,.20,.90),metal,False,.05)
bpy.ops.mesh.primitive_uv_sphere_add(segments=16,ring_count=8,radius=.14,location=(0,-16,1.66))
bpy.context.object.name="Human head - top at 1.80m"
bpy.context.object.data.materials.append(stone)
move_to(bpy.context.object,human)

# Export joined render meshes per level, with separately named convex box hulls.
# Keep the original editable objects untouched in their floor collections.
manifest={"schema":1,"name":"Legacy Hall","original_concept":True,"units":"meters",
          "floors":FLOORS,"floor_height_m":RISE,"wall_height_m":FLOORS*RISE,
          "building_width_m":28,"building_depth_m":20,"height_m":32.8,
          "entrance_clear_width_m":2.1,"stairs":{"width_m":1.8,"riser_m":.18,"tread_m":.30},"assets":[]}
for idx,c in enumerate(levels+[roof,environment]):
    name=f"SM_LegacyTower_Level{idx:02d}" if idx<8 else "SM_LegacyTower_Roof" if idx==8 else "SM_LegacyTower_Site"
    bpy.ops.object.select_all(action="DESELECT")
    copies=[]
    for source in list(c.objects):
        o=source.copy(); o.data=source.data.copy(); export_collection.objects.link(o); copies.append(o); o.select_set(True)
    bpy.context.view_layer.objects.active=copies[0]
    bpy.ops.object.convert(target="MESH")
    bpy.ops.object.join()
    joined=bpy.context.object; joined.name=name
    scene.cursor.location=(0,0,0)
    bpy.ops.object.origin_set(type="ORIGIN_CURSOR")
    hulls=[]
    for j,record in enumerate(collisions.get(c.name,[])):
        hull=cube_object(f"UCX_{name}_{j:03d}",record["center"],record["size"],collision_collection)
        hulls.append(hull)
    bpy.ops.object.select_all(action="DESELECT")
    joined.select_set(True)
    for h in hulls: h.select_set(True)
    bpy.context.view_layer.objects.active=joined
    print("Exporting "+name,flush=True)
    bpy.ops.export_scene.fbx(filepath=str(OUT/(name+".fbx")),use_selection=True,object_types={"MESH"},axis_forward="-Y",axis_up="Z",bake_anim=False,apply_unit_scale=True,path_mode="RELATIVE")
    triangles=sum(len(p.vertices)-2 for p in joined.data.polygons)
    manifest["assets"].append({"name":name,"file":name+".fbx","triangles":triangles,"collision_hulls":len(hulls),"collision_boxes":collisions.get(c.name,[])})
    joined.hide_render=True; joined.hide_set(True)
    for h in hulls: h.hide_render=True; h.hide_set(True)
collision_collection.hide_render=True
export_collection.hide_render=True
(OUT/"architecture_manifest.json").write_text(json.dumps(manifest,indent=2))

current=environment
world=bpy.data.worlds.new("Campus daylight")
world.use_nodes=True
world.node_tree.nodes["Background"].inputs[0].default_value=(.42,.55,.72,1)
world.node_tree.nodes["Background"].inputs[1].default_value=.5
scene.world=world
sun_data=bpy.data.lights.new("Late afternoon sunlight","SUN"); sun_data.energy=2.2; sun_data.angle=math.radians(2)
sun=bpy.data.objects.new("Late afternoon sunlight",sun_data); environment.objects.link(sun); sun.rotation_euler=(math.radians(28),math.radians(-24),math.radians(-35))
for i in range(FLOORS):
    data=bpy.data.lights.new(f"Floor {i+1} ambient","AREA"); data.energy=600; data.shape="RECTANGLE"; data.size=12; data.size_y=3
    o=bpy.data.objects.new(data.name,data); levels[i].objects.link(o); o.location=(0,0,i*RISE+3.1)

def camera(name,pos,target,lens):
    data=bpy.data.cameras.new(name); data.lens=lens; data.clip_end=500
    obj=bpy.data.objects.new(name,data); environment.objects.link(obj); obj.location=pos
    obj.rotation_euler=(Vector(target)-obj.location).to_track_quat("-Z","Y").to_euler()
    return obj
street_camera=camera("01 Street view - full-size tower",(34,-48,8),(0,0,14),30)
eye_camera=camera("02 Player eye height - lobby",(0,-8,1.65),(0,6,1.65),24)
scene.camera=street_camera
scene.render.engine="CYCLES"; scene.cycles.samples=24; scene.cycles.use_denoising=True
scene.render.resolution_x=1600; scene.render.resolution_y=1200; scene.render.resolution_percentage=100
scene.render.image_settings.file_format="PNG"
scene.view_settings.view_transform="AgX"
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type=="VIEW_3D":
            area.spaces.active.clip_end=500
            area.spaces.active.region_3d.view_perspective="CAMERA"
bpy.ops.object.select_all(action="DESELECT")
# Pack the original texture images so the .blend opens independently.
bpy.ops.file.pack_all()
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/"LegacyHall_FullScale.blend"),compress=True)
for cam,filename in [(street_camera,"StreetView.png"),(eye_camera,"LobbyEyeHeight.png")]:
    scene.camera=cam
    scene.render.filepath=str(OUT/filename)
    bpy.ops.render.render(write_still=True)
print("ARCHITECTURE_BUILD_OK",json.dumps({"floors":FLOORS,"height_m":32.8,"assets":len(manifest["assets"]),"triangles":sum(a["triangles"] for a in manifest["assets"]),"collision_hulls":sum(a["collision_hulls"] for a in manifest["assets"])}))
