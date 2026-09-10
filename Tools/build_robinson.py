"""Build the authored Robinson Hall exterior and playable interior.

The facade is a game-scale reconstruction from Albion's public campus tour,
campus map and Robinson history references.  Geometry is ordinary editable
Blender mesh data; Unity receives the same compact AOM1 mesh format used by
the Ferguson asset plus explicit box-collider metadata.
"""
import bpy, math, json, struct
from pathlib import Path
from mathutils import Vector

root = Path(__file__).resolve().parents[1]
res = root / "Unity/Assets/Resources/CampusCraft"
art = root / "Art"
bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete(use_global=False)

materials = {}
parts = {}
colliders = {}
active = "exterior"

def material(name, color, texture=None):
    m = bpy.data.materials.new(name)
    m.diffuse_color = (*color, 1)
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (*color, 1)
    bsdf.inputs["Roughness"].default_value = .72
    if texture:
        image = bpy.data.images.load(str(res / (texture + "_Color.jpg")))
        node = m.node_tree.nodes.new("ShaderNodeTexImage")
        node.image = image
        m.node_tree.links.new(node.outputs["Color"], bsdf.inputs["Base Color"])
    materials[name] = {"material": m, "color": [*color, 1], "texture": texture or ""}
    return m

brick = material("Robinson red brick", (.56, .24, .15), "red_brick_03")
stone = material("Robinson limestone", (.78, .73, .63))
trim = material("Robinson painted trim", (.84, .83, .77))
glass = material("Robinson blue glass", (.12, .24, .30))
roof = material("Robinson slate roof", (.12, .14, .16))
plaster = material("Robinson warm plaster", (.82, .79, .70))
floor_mat = material("Robinson worn stone floor", (.62, .60, .53), "concrete_pavement")
wood = material("Robinson oak furniture", (.34, .16, .07))
fabric = material("Robinson study upholstery", (.22, .27, .34))
metal = material("Robinson dark metal", (.07, .08, .09))
purple = material("Albion purple wayfinding", (.22, .09, .32))
light_mat = material("Robinson warm light", (.98, .86, .58))

def record(obj, solid=False):
    parts.setdefault(active, []).append(obj)
    if solid:
        colliders.setdefault(active, []).append({
            "name": obj.name,
            "center": [obj.location.x, obj.location.z, obj.location.y],
            "size": [obj.dimensions.x, obj.dimensions.z, obj.dimensions.y],
        })
    return obj

def box(name, loc, size, mat, solid=False, bevel=.012):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(mat)
    if bevel:
        mod = obj.modifiers.new("Crafted edge", "BEVEL")
        mod.width = bevel
        mod.segments = 2
        obj.modifiers.new("Weighted normals", "WEIGHTED_NORMAL")
    return record(obj, solid)

def mesh(name, vertices, faces, mat):
    data = bpy.data.meshes.new(name)
    data.from_pydata(vertices, [], faces)
    data.update()
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(mat)
    return record(obj)

def arch(name, cx, y, base, radius, thickness, depth, mat=stone):
    vertices, faces = [], []
    for i in range(25):
        angle = i * math.pi / 24
        for yy in (y - depth / 2, y + depth / 2):
            for rr in (radius, radius + thickness):
                vertices.append((cx + rr * math.cos(angle), yy, base + rr * math.sin(angle)))
    for i in range(24):
        j, k = i * 4, (i + 1) * 4
        faces += [(j, j + 1, k + 1, k), (j + 2, k + 2, k + 3, j + 3),
                  (j, k, k + 2, j + 2), (j + 1, j + 3, k + 3, k + 1)]
    faces += [(0, 2, 3, 1), (96, 97, 99, 98)]
    return mesh(name, vertices, faces, mat)

def window(x, y, z, width=1.22, height=1.65):
    box("Robinson inset glazing", (x, y, z), (width, .08, height), glass, False, 0)
    for sx in (-1, 1):
        box("Robinson window jamb", (x + sx * (width / 2 + .045), y - .04, z),
            (.08, .10, height + .14), trim, False, .006)
    for dz in (-height / 2 - .04, height / 2 + .04):
        box("Robinson stone sill", (x, y - .05, z + dz), (width + .25, .16, .10), stone, False, .006)
    box("Robinson window mullion", (x, y - .07, z), (.045, .075, height), trim, False, .003)
    box("Robinson meeting rail", (x, y - .07, z), (width, .075, .045), trim, False, .003)

# Robinson's compact four-storey academic block: deep brick wings, limestone
# base/cornices, a central arched portico and a steep slate roof.
width, depth, floors, floor_h = 28.0, 13.5, 4, 3.35
roof_y = floors * floor_h
box("Robinson limestone foundation", (0, 0, .15), (width + .6, depth + .6, .30), stone, True)
box("Robinson rear wall", (0, depth / 2, roof_y / 2), (width, .34, roof_y), brick, True)
box("Robinson west wall", (-width / 2, 0, roof_y / 2), (.34, depth, roof_y), brick, True)
box("Robinson east wall", (width / 2, 0, roof_y / 2), (.34, depth, roof_y), brick, True)
for side in (-1, 1):
    box("Robinson front wing", (side * width * .25, -depth / 2, roof_y / 2),
        (width * .5 - 2.45, .34, roof_y), brick, True)
for f in range(floors):
    z = 1.55 + f * floor_h
    for x in (-12.2, -9.1, -6.0, -2.8, 2.8, 6.0, 9.1, 12.2):
        window(x, -depth / 2 - .07, z)
        window(x, depth / 2 + .07, z)
    box("Robinson limestone floor band", (0, 0, f * floor_h + .08),
        (width + .30, depth + .08, .16), stone, False, .008)
box("Robinson limestone cornice", (0, 0, roof_y - .20), (width + .60, depth + .55, .42), stone, False, .015)

# Central portico, carved arch, entry canopy, lanterns and quad approach.
for x in (-2.65, 2.65):
    box("Robinson portico column", (x, -depth / 2 - .72, 1.55), (.62, 1.25, 3.10), stone, True, .028)
    box("Robinson portico base", (x, -depth / 2 - .72, .14), (1.0, 1.45, .28), stone, True, .018)
    box("Robinson portico capital", (x, -depth / 2 - .72, 3.06), (1.0, 1.40, .22), stone, False, .014)
arch("Robinson carved entrance arch", 0, -depth / 2 - .72, 2.55, 2.25, .36, 1.55)
box("Robinson portico entablature", (0, -depth / 2 - .72, 5.05), (6.35, 1.68, .30), stone, False, .018)
box("Robinson entry gable", (0, -depth / 2 - .72, 6.02), (5.40, 1.25, 1.45), brick, False, .018)
for x in (-1.45, 0, 1.45): window(x, -depth / 2 - 1.53, 6.05, 1.10, 1.45)
for x in (-2.8, 2.8): box("Robinson entry lantern", (x, -depth / 2 - 1.54, 2.45), (.24, .18, .60), light_mat, False, .01)
box("Robinson quad approach", (0, -depth / 2 - 3.8, .04), (6.8, 7.4, .08), floor_mat, True, .006)

# Steep hipped roof and two dormers make the authored silhouette distinct from
# the generic shell.  The roof itself is render-only; the cornice carries the
# physical roof boundary for the player.
mesh("Robinson hipped slate roof",
     [(-14.4, -7.0, roof_y), (14.4, -7.0, roof_y), (14.4, 7.0, roof_y),
      (-14.4, 7.0, roof_y), (-10.2, 0, roof_y + 2.65), (10.2, 0, roof_y + 2.65)],
     [(0, 1, 5, 4), (1, 2, 5), (2, 3, 4, 5), (3, 0, 4)], roof)
for x in (-7.2, 7.2):
    box("Robinson dormer", (x, -2.5, roof_y + .85), (2.2, 1.8, 1.55), trim, False, .025)
    box("Robinson dormer glazing", (x, -3.42, roof_y + .85), (1.15, .08, .82), glass, False, 0)

# Four real floors, divided seminar rooms, a central corridor, furniture and
# a stairwell opening.  The stair is intentionally assembled from climbable
# 0.167 m risers instead of a visual ramp.
for f in range(floors):
    active = "floor" + str(f)
    y = f * floor_h
    box("Robinson interior floor", (0, 0, y - .10), (width - .72, depth - .72, .20), floor_mat, True)
    box("Robinson corridor wall", (0, 0, y + 1.58), (.15, depth - 3.0, 3.16), plaster, True)
    for side in (-1, 1):
        x = side * 8.4
        box("Robinson seminar partition", (x, 2.55, y + 1.58), (4.15, .15, 3.16), plaster, True)
        box("Robinson seminar partition", (x, -3.9, y + 1.58), (4.15, .15, 3.16), plaster, True)
    for x in (-9.2, -3.1, 3.1, 9.2):
        box("Robinson seminar table", (x, 3.55, y + .74), (2.25, 1.00, .12), wood, True, .035)
        box("Robinson seminar table", (x, -3.55, y + .74), (2.25, 1.00, .12), wood, True, .035)
        for z in (-.36, .36):
            box("Robinson table leg", (x - .82, 3.55 + z, y + .36), (.07, .07, .72), metal)
            box("Robinson table leg", (x + .82, 3.55 + z, y + .36), (.07, .07, .72), metal)
        box("Robinson study chair", (x, 2.55, y + .48), (.60, .58, .12), fabric, True, .05)
        box("Robinson study chair", (x, -2.55, y + .48), (.60, .58, .12), fabric, True, .05)
    box("Robinson teaching board", (0, -5.72, y + 1.72), (3.6, .08, 1.35), purple, False, .01)
    for x in (-9.5, -3.0, 3.0, 9.5):
        box("Robinson ceiling light", (x, 0, y + 3.10), (.38, 1.20, .05), light_mat, False, 0)
    if f < floors - 1:
        for step in range(20):
            yy = -4.55 + step * .43
            rise = (step + 1) * .167
            box("Robinson stair tread", (11.05, yy, y + rise - .0835), (2.20, .43, .167), stone, True, .006)
        box("Robinson stair landing", (11.05, 4.9, y + floor_h - .10), (2.40, 1.35, .20), floor_mat, True)
        for side in (-1, 1):
            box("Robinson stair handrail", (11.05 + side * 1.18, .0, y + 1.48), (.06, 9.5, .06), wood, False, .014)
    active = "exterior"

# Add a simple authoring guide for the interior review and save the source.
scene = bpy.context.scene
scene.world.color = (.28, .32, .40)
bpy.ops.object.light_add(type="SUN", location=(10, -18, 25))
bpy.context.object.rotation_euler = (0.55, -0.35, -0.45)
bpy.context.object.data.energy = 2.2
bpy.ops.object.light_add(type="AREA", location=(0, -20, 13))
bpy.context.object.data.energy = 1450
bpy.context.object.data.size = 16
bpy.context.object.rotation_euler = (Vector((0, 0, 5)) - bpy.context.object.location).to_track_quat("-Z", "Y").to_euler()
bpy.ops.object.camera_add(location=(30, -38, 15))
camera = bpy.context.object
camera.rotation_euler = (Vector((0, -1, 6.1)) - camera.location).to_track_quat("-Z", "Y").to_euler()
camera.data.lens = 48
scene.camera = camera
scene.render.engine = "BLENDER_EEVEE"
scene.render.resolution_x, scene.render.resolution_y, scene.render.resolution_percentage = 1400, 900, 100
scene.render.filepath = str(art / "Robinson-review.png")

names = list(materials)
def export(group, objects):
    vertices = []
    indices = [[] for _ in names]
    deps = bpy.context.evaluated_depsgraph_get()
    for obj in objects:
        if obj.type != "MESH":
            continue
        evaluated = obj.evaluated_get(deps)
        me = evaluated.to_mesh()
        me.calc_loop_triangles()
        normal_matrix = obj.matrix_world.to_3x3().inverted().transposed()
        for tri in me.loop_triangles:
            ids = []
            mat_index = names.index(me.materials[tri.material_index].name)
            for loop_index in tri.loops:
                loop = me.loops[loop_index]
                position = obj.matrix_world @ me.vertices[loop.vertex_index].co
                normal = (normal_matrix @ me.corner_normals[loop_index].vector).normalized()
                uv = (position.x / 2, position.y / 2) if abs(normal.z) > .7 else ((position.y / 2, position.z / 2) if abs(normal.x) > .7 else (position.x / 2, position.z / 2))
                ids.append(len(vertices))
                vertices.append((position.x, position.z, position.y, normal.x, normal.z, normal.y, uv[0], uv[1]))
            indices[mat_index].extend(reversed(ids))
        evaluated.to_mesh_clear()
    with (res / ("robinson-" + group + ".bytes")).open("wb") as output:
        output.write(b"AOM1")
        output.write(struct.pack("<ii", len(vertices), len(names)))
        for value in vertices:
            output.write(struct.pack("<8f", *value))
        for material_indices in indices:
            output.write(struct.pack("<i", len(material_indices)))
            if material_indices:
                output.write(struct.pack("<" + "i" * len(material_indices), *material_indices))
    print(group, len(vertices), "vertices", len(colliders.get(group, [])), "colliders")

for group, objects in parts.items():
    export(group, objects)
model = {
    "materials": [{"name": name, "color": materials[name]["color"], "texture": materials[name]["texture"]} for name in names],
    "sections": [{"name": name, "colliders": colliders.get(name, [])} for name in parts],
    "note": "Robinson Hall is a reference-driven, game-scale reconstruction based on public Albion campus references; exact room dimensions remain provisional without a supplied floor plan.",
}
(res / "robinson.json").write_text(json.dumps(model, indent=2))
bpy.ops.file.pack_all()
bpy.ops.wm.save_as_mainfile(filepath=str(art / "RobinsonHall.blend"))
bpy.ops.render.render(write_still=True)
print("ROBINSON_MODEL_OK")
