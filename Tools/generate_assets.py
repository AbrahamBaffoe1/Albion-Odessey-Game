"""Run with Blender --background --python Tools/generate_assets.py.
Creates original modular concept meshes, two preview scenes, and FBX exports.
No third-party photographs/textures are embedded. All coordinates are meters.
"""
import bpy
import math
import random
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[1]
ART = ROOT / "Art"
ART.mkdir(exist_ok=True)
random.seed(1835)

def material(name, color, metallic=0, emission=0):
    m = bpy.data.materials.new(name)
    m.diffuse_color = (*color, 1)
    m.use_nodes = True
    p = m.node_tree.nodes.get("Principled BSDF")
    p.inputs["Base Color"].default_value = (*color, 1)
    p.inputs["Roughness"].default_value = .65
    p.inputs["Metallic"].default_value = metallic
    p.inputs["Emission Color"].default_value = (*color, 1)
    p.inputs["Emission Strength"].default_value = emission
    return m

def finish(obj, name, mat, bevel=0):
    obj.name = name
    obj.data.materials.append(mat)
    if bevel:
        b = obj.modifiers.new("Soft crafted edges", "BEVEL")
        b.width = bevel
        b.segments = 2
        obj.modifiers.new("Weighted normals", "WEIGHTED_NORMAL")
    return obj

def cube(name, pos, size, mat, bevel=.03):
    bpy.ops.mesh.primitive_cube_add(size=1, location=pos)
    o = bpy.context.object
    o.scale = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return finish(o, name, mat, bevel)

def cylinder(name, pos, radius, depth, mat, vertices=24):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=pos)
    return finish(bpy.context.object, name, mat, .025)

def sphere(name, pos, scale, mat):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=24, ring_count=12, radius=1, location=pos)
    o=bpy.context.object
    o.scale=scale
    return finish(o,name,mat)

def cone(name,pos,radius,depth,mat):
    bpy.ops.mesh.primitive_cone_add(vertices=8,radius1=radius,radius2=0,depth=depth,location=pos)
    return finish(bpy.context.object,name,mat)

def tree(x,y,z,p):
    cylinder("Tree trunk",(x,y,z+.6),.11,1.2,p["wood"],12)
    for dx,dy,dz,s in [(0,0,1.45,.65),(.3,.1,1.7,.5),(-.2,-.1,1.9,.47)]:
        sphere("Tree crown",(x+dx,y+dy,z+dz),(s,s,s),p["leaf"])

def building(kind,p,fantasy):
    cube("Limestone foundation",(0,0,.12),(3.25,2.9,.24),p["stone"])
    if kind=="Garden":
        cube("Garden bed",(0,0,.27),(2.9,2.55,.18),p["grass"])
        cylinder("Memory fountain",(0,0,.48),.60,.32,p["stone"])
        cylinder("Water mirror",(0,0,.66),.48,.03,p["water"])
        tree(-.85,.60,.30,p)
        cube("Bench",(.8,-.6,.57),(.75,.27,.12),p["wood"])
        for x in [.52,1.05]: cube("Bench legs",(x,-.6,.4),(.09,.2,.25),p["stone"])
        for x,y in [(-1,-.9),(-.6,-1),(.9,.85),(1.1,.45)]:
            sphere("Flower",(x,y,.4),(.15,.15,.13),p["gold"])
        return
    h=1.7 if kind=="Hall" else 2.1
    cube("Brick walls",(0,0,h/2+.24),(2.9,2.5,h),p["wall"])
    for z in [.35,h+.18]: cube("Stone belt",(0,0,z),(3.03,2.62,.13),p["stone"])
    for y in [-1.265,1.265]:
        for x in [-1.05,-.53,.53,1.05]:
            for z in ([.9,1.65] if h>1.8 else [.95]):
                cube("Window surround",(x,y,z),(.35,.07,.59),p["stone"],.01)
                cube("Window glow",(x,y*1.03,z),(.25,.045,.47),p["glass"],.005)
    cube("Door surround",(0,-1.29,.80),(.48,.12,1.14),p["stone"])
    cube("Door",(0,-1.36,.77),(.34,.04,1.0),p["wood"])
    for i in range(3): cube("Entry step",(0,-1.58-i*.13,.22-i*.06),(.9,.35,.13),p["stone"])
    if kind=="Observatory":
        cylinder("Observatory drum",(0,0,h+.48),1.3,.5,p["stone"])
        sphere("Copper observatory dome",(0,0,h+.75),(1.3,1.3,.85),p["roof"])
        cube("Dome slit",(0,-.7,h+1.15),(.13,1.55,.1),p["gold"])
        if fantasy:
            sphere("Captured star",(0,0,h+2.0),(.16,.16,.16),p["glass"])
            for z,r in [(h+1.7,.58),(h+2.0,.38)]:
                bpy.ops.mesh.primitive_torus_add(major_radius=r,minor_radius=.025,location=(0,0,z))
                finish(bpy.context.object,"Celestial orbit",p["gold"])
    else:
        for sign in [-1,1]:
            o=cube("Pitched roof",(0,sign*.68,h+.60),(3.25,1.65,.16),p["roof"])
            o.rotation_euler[0]=sign*math.radians(-29)
        if kind=="Library":
            for x in [-.80,.80]: cylinder("Portico column",(x,-1.58,1.15),.11,1.75,p["stone"])
            cube("Portico lintel",(0,-1.58,2.08),(2.0,.52,.17),p["stone"])
            cylinder("Clock tower",(0,0,h+1.1),.48,.9,p["stone"],4)
            cone("Tower roof",(0,0,h+1.9),.66,.95,p["roof"])
            if fantasy: sphere("Tower star",(0,0,h+2.5),(.11,.11,.11),p["glass"])
        else:
            cube("Chimney",(.87,.4,h+.83),(.28,.32,.75),p["wall"])

def palette(fantasy):
    style="Fantasy" if fantasy else "Campus"
    colors={"wall":(.35,.22,.55) if fantasy else (.48,.19,.105),"stone":(.80,.74,.58),
            "roof":(.13,.16,.32) if fantasy else (.13,.28,.25),"glass":(.98,.65,.17),
            "wood":(.22,.105,.05),"grass":(.075,.23,.20) if fantasy else (.24,.36,.115),
            "leaf":(.19,.38,.37) if fantasy else (.31,.46,.12),"gold":(.94,.56,.12),"water":(.12,.50,.61)}
    return {k:material(style+"_"+k,c,metallic=.45 if k in ["roof","gold"] else 0,emission=.7 if k=="glass" else 0) for k,c in colors.items()}

bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete(use_global=False)
mesh_assets={}
for fantasy in [False,True]:
    p=palette(fantasy)
    for kind in ["Garden","Library","Observatory","Hall"]:
        bpy.ops.object.select_all(action="DESELECT")
        before=set(bpy.data.objects)
        building(kind,p,fantasy)
        created=list(set(bpy.data.objects)-before)
        for o in created:
            o.select_set(True)
        bpy.context.view_layer.objects.active=created[0]
        bpy.ops.object.convert(target="MESH")
        bpy.ops.object.join()
        o=bpy.context.object
        bpy.context.scene.cursor.location=(0,0,0)
        bpy.ops.object.origin_set(type="ORIGIN_CURSOR")
        name=f"SM_{kind}_{'Fantasy' if fantasy else 'Campus'}"
        o.name=name
        bpy.ops.export_scene.fbx(filepath=str(ART/(name+".fbx")),use_selection=True,object_types={"MESH"},axis_forward="-Y",axis_up="Z",bake_anim=False)
        mesh_assets[(fantasy,kind)]=o
        o.hide_render=True
        o.hide_set(True)

def make_scene(fantasy):
    scene=bpy.data.scenes.new("Echo Fantasy" if fantasy else "Campus Concept")
    bpy.context.window.scene=scene
    p=palette(fantasy)
    for j in range(7):
        for i in range(7):
            x,y=(i-3)*4.2,(j-3)*4.2
            cube("Living campus tile",(x,y,-.12),(4.04,4.04,.24),p["grass"],.10)
    base=material("Midnight basalt" if fantasy else "Earth",(.035,.05,.10) if fantasy else (.13,.11,.08))
    cube("Floating campus foundation",(0,0,-.85),(30,30,1.4),base,.45)
    for x,y,kind in [(-8,5,"Library"),(0,7,"Observatory"),(8,4,"Hall"),(-8,-3,"Hall"),(8,-4,"Library"),(-1,-7,"Garden"),(5,10,"Garden"),(-11,10,"Garden")]:
        o=mesh_assets[(fantasy,kind)].copy()
        o.data=o.data.copy()
        scene.collection.objects.link(o)
        o.hide_render=False
        o.hide_set(False)
        o.location=(x,y,.03)
        o.scale=(1.3,1.3,1.3)
    for x in [-2,2]: cube("Quad walkway",(x,0,.04),(1.2,27,.08),p["stone"],.015)
    cube("Cross-campus path",(0,-1,.05),(27,1.2,.08),p["stone"],.02)
    cylinder("Beacon base",(0,1,.32),1.6,.6,p["stone"])
    cylinder("Beacon pillar",(0,1,1.6),.40,2.4,p["gold"])
    sphere("Constellation Beacon",(0,1,3.1),(.72,.72,.72),p["glass"])
    for a in range(8):
        angle=a*math.pi/4
        x,y=math.cos(angle)*12.7,math.sin(angle)*12.7
        tree(x,y,.03,p)
        cylinder("Lantern post",(x*.8,y*.8,.8),.06,1.6,p["gold"],8)
        sphere("Lantern",(x*.8,y*.8,1.7),(.18,.18,.25),p["glass"])
    # Original squirrel guide sculpture on the foreground platform.
    sphere("Squirrel body",(6,-10,.9),(.48,.38,.65),p["gold"])
    sphere("Squirrel head",(6,-10.15,1.6),(.40,.37,.38),p["gold"])
    for x in [5.76,6.24]: cone("Squirrel ear",(x,-10.15,1.98),.13,.35,p["gold"])
    sphere("Curled squirrel tail",(6,-9.4,1.4),(.55,.35,.8),p["gold"])
    for x in [5.85,6.15]: sphere("Squirrel eyes",(x,-10.49,1.7),(.05,.03,.06),base)
    if fantasy:
        for i in range(20):
            x,y=random.uniform(-16,16),random.uniform(-15,15)
            sphere("Drifting memory light",(x,y,random.uniform(1,5)),(.04,.04,.04),p["glass"])
    backdrop=material("Backdrop",(.018,.028,.052) if fantasy else (.28,.33,.29))
    cube("Studio floor",(0,0,-2.0),(200,200,.3),backdrop,0)
    world=bpy.data.worlds.new(scene.name+" world")
    world.use_nodes=True
    world.node_tree.nodes["Background"].inputs[0].default_value=(.11,.16,.27,1) if fantasy else (.45,.55,.7,1)
    world.node_tree.nodes["Background"].inputs[1].default_value=.35
    scene.world=world
    for name,pos,power,color,size in [("Sunbox",(5,-12,25),6500,(1,.82,.62),12),("Sky fill",(-15,-3,15),4500,(.45,.61,1),15),("Rim",(2,16,20),8000,(.8,.6,1) if fantasy else (1,.9,.7),10)]:
        data=bpy.data.lights.new(name,"AREA")
        data.energy=power
        data.color=color
        data.shape="DISK"
        data.size=size
        o=bpy.data.objects.new(name,data)
        scene.collection.objects.link(o)
        o.location=pos
        o.rotation_euler=(Vector((0,0,0))-o.location).to_track_quat("-Z","Y").to_euler()
    data=bpy.data.cameras.new("Campus portrait")
    cam=bpy.data.objects.new("Campus portrait",data)
    scene.collection.objects.link(cam)
    cam.location=(34,-43,38)
    cam.rotation_euler=(Vector((0,0,0))-cam.location).to_track_quat("-Z","Y").to_euler()
    data.type="ORTHO"
    data.ortho_scale=49
    scene.camera=cam
    scene.render.engine="CYCLES"
    scene.cycles.samples=24
    scene.cycles.use_denoising=True
    scene.render.resolution_x=1600
    scene.render.resolution_y=1200
    scene.render.resolution_percentage=100
    scene.render.image_settings.file_format="PNG"
    scene.render.filepath=str(ART/("EchoFantasy.png" if fantasy else "CampusConcept.png"))
    scene.view_settings.view_transform="AgX"
    return scene

scenes=[make_scene(False),make_scene(True)]
bpy.ops.wm.save_as_mainfile(filepath=str(ART/"AlbionConceptKit.blend"))
for s in scenes:
    bpy.context.window.scene=s
    bpy.ops.render.render(write_still=True)
print("ASSET_BUILD_OK: 8 FBX modules, 2 scenes, original Blender source and two rendered previews")
