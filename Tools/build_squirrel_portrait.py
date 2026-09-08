import bpy
import math
import os
import random
from mathutils import Vector

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ART = os.path.join(ROOT, "Art", "Wildlife")
os.makedirs(ART, exist_ok=True)
BLEND = os.path.join(ART, "LeucisticSquirrelPortrait.blend")
RENDER = os.path.join(ART, "LeucisticSquirrelPortrait.png")
FBX = os.path.join(ART, "Squirrel_Leucistic.fbx")

random.seed(17)


def make_material(name, color, roughness=0.72, metallic=0.0):
    mat = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    mat.diffuse_color = (*color, 1.0)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (*color, 1.0)
    bsdf.inputs["Roughness"].default_value = roughness
    bsdf.inputs["Metallic"].default_value = metallic
    return mat


def add_fur_shader(mat, shadow_tone, highlight_tone):
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    bsdf = nodes.get("Principled BSDF")
    texcoord = nodes.new("ShaderNodeTexCoord")
    noise = nodes.new("ShaderNodeTexNoise")
    noise.inputs["Scale"].default_value = 34.0
    noise.inputs["Detail"].default_value = 5.0
    noise.inputs["Roughness"].default_value = 0.78
    ramp = nodes.new("ShaderNodeValToRGB")
    ramp.color_ramp.elements[0].color = (*shadow_tone, 1.0)
    ramp.color_ramp.elements[1].color = (*highlight_tone, 1.0)
    bump = nodes.new("ShaderNodeBump")
    bump.inputs["Strength"].default_value = 0.12
    bump.inputs["Distance"].default_value = 0.035
    links.new(texcoord.outputs["Generated"], noise.inputs["Vector"])
    links.new(noise.outputs["Fac"], ramp.inputs["Fac"])
    links.new(ramp.outputs["Color"], bsdf.inputs["Base Color"])
    links.new(noise.outputs["Fac"], bump.inputs["Height"])
    links.new(bump.outputs["Normal"], bsdf.inputs["Normal"])


def collection(name):
    col = bpy.data.collections.get(name) or bpy.data.collections.new(name)
    if col.name not in bpy.context.scene.collection.children:
        bpy.context.scene.collection.children.link(col)
    return col


def move_to(obj, col):
    for current in list(obj.users_collection):
        current.objects.unlink(obj)
    col.objects.link(obj)
    return obj


def assign(obj, material):
    obj.data.materials.clear()
    obj.data.materials.append(material)
    return obj


def smooth(obj):
    if hasattr(obj.data, "polygons"):
        for poly in obj.data.polygons:
            poly.use_smooth = True
    return obj


def uv_sphere(name, location, scale, material, col, segments=32, rings=20, export=True):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=rings, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    assign(smooth(obj), material)
    move_to(obj, col)
    obj["squirrel_export"] = export
    return obj


def cone(name, location, radius, depth, material, col, rotation=(0, 0, 0), export=True):
    bpy.ops.mesh.primitive_cone_add(vertices=24, radius1=radius, radius2=radius * 0.14, depth=depth, location=location, rotation=rotation)
    obj = bpy.context.object
    obj.name = name
    assign(smooth(obj), material)
    move_to(obj, col)
    obj["squirrel_export"] = export
    return obj


def cylinder_between(name, start, end, radius, material, col, vertices=32, export=False):
    start = Vector(start)
    end = Vector(end)
    direction = end - start
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=direction.length, location=(start + end) * 0.5)
    obj = bpy.context.object
    obj.name = name
    obj.rotation_mode = "QUATERNION"
    obj.rotation_quaternion = Vector((0, 0, 1)).rotation_difference(direction.normalized())
    assign(smooth(obj), material)
    move_to(obj, col)
    obj["squirrel_export"] = export
    return obj


def curve_strand(name, points, material, col, bevel=0.018, export=False):
    data = bpy.data.curves.new(name, "CURVE")
    data.dimensions = "3D"
    data.resolution_u = 4
    data.bevel_depth = bevel
    data.bevel_resolution = 2
    spline = data.splines.new("BEZIER")
    spline.bezier_points.add(len(points) - 1)
    for point, co in zip(spline.bezier_points, points):
        point.co = co
        point.handle_left_type = "AUTO"
        point.handle_right_type = "AUTO"
    obj = bpy.data.objects.new(name, data)
    col.objects.link(obj)
    assign(obj, material)
    obj["squirrel_export"] = export
    return obj


def tube_mesh(name, points, radii, material, col, export=True, sides=24):
    """Create one continuous smooth tube along a 2D tail path."""
    verts = []
    faces = []
    for index, center in enumerate(points):
        center = Vector(center)
        prev_point = Vector(points[max(0, index - 1)])
        next_point = Vector(points[min(len(points) - 1, index + 1)])
        tangent = (next_point - prev_point).normalized()
        depth_axis = Vector((0, 0, 1))
        normal = depth_axis.cross(tangent).normalized()
        for side in range(sides):
            angle = (math.tau * side) / sides
            ring = center + normal * math.cos(angle) * radii[index] + depth_axis * math.sin(angle) * radii[index] * 0.78
            verts.append(tuple(ring))
    for index in range(len(points) - 1):
        for side in range(sides):
            a = index * sides + side
            b = index * sides + (side + 1) % sides
            c = (index + 1) * sides + (side + 1) % sides
            d = (index + 1) * sides + side
            faces.append((a, b, c, d))
    mesh = bpy.data.meshes.new(name + " Mesh")
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    col.objects.link(obj)
    assign(smooth(obj), material)
    obj["squirrel_export"] = export
    return obj


def leaf(name, location, scale, material, col):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2, radius=1, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    obj.rotation_euler = (random.uniform(-0.4, 0.4), random.uniform(-0.6, 0.6), random.uniform(-1.0, 1.0))
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    assign(smooth(obj), material)
    move_to(obj, col)
    return obj


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for datablocks in (bpy.data.meshes, bpy.data.curves, bpy.data.materials, bpy.data.cameras, bpy.data.lights):
        for block in list(datablocks):
            if block.users == 0:
                datablocks.remove(block)


clear_scene()
scene = bpy.context.scene
scene.render.engine = "BLENDER_EEVEE"
scene.render.resolution_x = 1600
scene.render.resolution_y = 1000
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = "PNG"
scene.render.filepath = RENDER
scene.render.film_transparent = False
scene.render.image_settings.color_mode = "RGBA"
scene.render.image_settings.color_depth = "8"
scene.view_settings.look = "AgX - Medium High Contrast"
scene.world.color = (0.48, 0.54, 0.62)
world_nodes = scene.world.node_tree.nodes
world_bg = world_nodes.get("Background")
world_bg.inputs["Color"].default_value = (0.48, 0.54, 0.62, 1)
world_bg.inputs["Strength"].default_value = 0.38

squirrel_col = collection("Squirrel_Leucistic")
branch_col = collection("Snow_Branch")
background_col = collection("Blurred_Autumn_Background")

ivory = make_material("Fur warm ivory leucistic", (0.78, 0.73, 0.62), 0.92)
ivory_light = make_material("Fur pale guard hairs", (0.93, 0.88, 0.76), 0.95)
add_fur_shader(ivory, (0.62, 0.57, 0.48), (0.91, 0.86, 0.75))
add_fur_shader(ivory_light, (0.74, 0.69, 0.60), (0.98, 0.94, 0.84))
belly = make_material("Fur cream underside", (0.93, 0.86, 0.71), 0.94)
pink = make_material("Skin soft pink", (0.64, 0.24, 0.27), 0.68)
ruby = make_material("Eye deep ruby", (0.09, 0.004, 0.008), 0.25)
eye_glint = make_material("Eye catchlight", (1.0, 0.93, 0.82), 0.08)
bark = make_material("Branch winter bark", (0.16, 0.09, 0.055), 0.96)
bark_light = make_material("Branch cut bark", (0.27, 0.16, 0.09), 0.96)
snow = make_material("Fresh snow", (0.95, 0.97, 1.0), 0.84)
leaf_mats = [
    make_material("Autumn ochre", (0.55, 0.22, 0.045), 0.92),
    make_material("Autumn umber", (0.24, 0.075, 0.025), 0.95),
    make_material("Autumn muted gold", (0.72, 0.39, 0.08), 0.90),
]

# One diagonal branch is the entire foreground environment.
branch_start = (-4.5, 0.88, 0.0)
branch_end = (4.5, 1.47, 0.0)
cylinder_between("Single diagonal branch", branch_start, branch_end, 0.24, bark, branch_col, export=False)
cylinder_between("Branch broken side twig", (2.3, 1.30, 0.0), (3.6, 2.05, 0.0), 0.095, bark_light, branch_col, vertices=20)
cylinder_between("Branch short twig", (-1.5, 1.02, 0.0), (-2.2, 1.72, 0.0), 0.07, bark_light, branch_col, vertices=20)

# Snow sits only on the upper surface of the branch as one continuous soft cap.
snow_points = []
for i in range(13):
    t = i / 12.0
    x = branch_start[0] + (branch_end[0] - branch_start[0]) * t
    y = branch_start[1] + (branch_end[1] - branch_start[1]) * t + 0.21
    snow_points.append((x, y, 0.0))
curve_strand("Continuous snow cap", snow_points, snow, branch_col, bevel=0.22)

# Leucistic squirrel in side profile, facing left. The eye sits near the
# upper-left third and the tail owns the right half of the composition.
uv_sphere("Squirrel elongated torso", (0.05, 2.24, 0.0), (1.58, 0.67, 0.51), ivory, squirrel_col)
uv_sphere("Squirrel powerful hindquarters", (0.95, 2.15, 0.0), (0.94, 0.82, 0.60), ivory, squirrel_col)
uv_sphere("Squirrel cream chest", (-0.78, 2.13, 0.27), (0.55, 0.61, 0.20), belly, squirrel_col)
uv_sphere("Squirrel alert head", (-1.38, 2.84, 0.0), (0.56, 0.52, 0.47), ivory, squirrel_col)
uv_sphere("Squirrel pointed muzzle", (-1.82, 2.67, 0.0), (0.60, 0.29, 0.31), belly, squirrel_col)
uv_sphere("Squirrel nose", (-2.26, 2.67, 0.0), (0.14, 0.105, 0.12), pink, squirrel_col)

# Upright rounded ears with pink inner surfaces and a visible near-side ruby eye.
uv_sphere("Near ear", (-1.42, 3.32, 0.03), (0.22, 0.34, 0.16), ivory, squirrel_col)
uv_sphere("Near ear inner", (-1.42, 3.31, 0.22), (0.13, 0.24, 0.045), pink, squirrel_col)
uv_sphere("Far ear", (-0.98, 3.26, -0.18), (0.18, 0.29, 0.14), ivory, squirrel_col)
uv_sphere("Ruby near eye", (-1.59, 2.96, 0.43), (0.13, 0.13, 0.09), ruby, squirrel_col)
uv_sphere("Eye catchlight", (-1.63, 3.00, 0.505), (0.035, 0.035, 0.018), eye_glint, squirrel_col)

# Paws grip the branch; hind feet support the animal's crouched side-profile pose.
for side in (-1, 1):
    z = side * 0.35
    uv_sphere("Front paw", (-0.78, 1.63, z), (0.15, 0.28, 0.12), pink, squirrel_col)
    uv_sphere("Front paw toes", (-0.94, 1.49, z + 0.02), (0.21, 0.075, 0.095), pink, squirrel_col)
    uv_sphere("Hind foot", (0.83, 1.38, z), (0.30, 0.15, 0.15), pink, squirrel_col)

# Overlapping tapered volumes produce a broad, readable bushy tail silhouette.
tail_segments = [
    ((1.25, 2.16, -0.02), 0.52),
    ((1.80, 2.36, -0.02), 0.68),
    ((2.38, 2.48, -0.02), 0.77),
    ((2.96, 2.44, -0.02), 0.78),
    ((3.50, 2.24, -0.02), 0.67),
    ((3.91, 1.91, -0.02), 0.55),
    ((4.12, 1.52, -0.02), 0.40),
    ((4.09, 1.20, -0.02), 0.27),
]
tube_mesh("Single continuous bushy tail", [location for location, _ in tail_segments], [radius for _, radius in tail_segments], ivory, squirrel_col)

# A restrained layer of individual guard-hair curves is added after the pose
# and camera composition. They are deliberately short and soft, not a halo.
for i in range(85):
    x = random.uniform(-1.35, 1.35)
    body_height = math.sqrt(max(0.0, 1 - (x / 1.6) ** 2)) * 0.62
    y = 2.12 + random.uniform(-0.82, 0.82) * body_height
    z = 0.49 + random.uniform(-0.03, 0.04)
    outward = Vector((random.uniform(-0.04, 0.04), random.uniform(-0.12, 0.12), random.uniform(0.08, 0.17)))
    curve_strand("Body guard hair", [(x, y, z), (x + outward.x, y + outward.y, z + outward.z)], ivory_light, squirrel_col, bevel=0.006)
for i, (base, radius) in enumerate(tail_segments):
    bx, by, bz = base
    for strand in range(14):
        angle = random.uniform(-math.pi, math.pi)
        offset = Vector((random.uniform(-radius, radius), random.uniform(-radius, radius), 0.43))
        start = Vector((bx, by, bz)) + offset * 0.72
        end = start + Vector((math.cos(angle) * 0.20, math.sin(angle) * 0.20, random.uniform(0.02, 0.12)))
        curve_strand("Tail guard hair", [start, end], ivory_light, squirrel_col, bevel=0.008)

# Blurred branches and a few autumn leaves stay behind the animal only.
for index, (start, end, radius) in enumerate([
    ((-4.2, 2.8, -2.4), (4.5, 4.1, -2.4), 0.10),
    ((-2.8, 4.7, -2.8), (2.8, 2.9, -2.8), 0.075),
    ((1.0, 1.4, -3.0), (4.5, 4.4, -3.0), 0.08),
]):
    cylinder_between("Blurred background branch %02d" % index, start, end, radius, bark_light, background_col, vertices=16)
for index in range(34):
    leaf("Blurred autumn leaf %02d" % index,
         (random.uniform(-4.8, 4.8), random.uniform(1.8, 5.0), random.uniform(-3.3, -2.2)),
         (random.uniform(0.10, 0.23), random.uniform(0.16, 0.34), random.uniform(0.025, 0.06)),
         random.choice(leaf_mats), background_col)

# Telephoto camera: close, eye-level and focused on the visible eye.
bpy.ops.object.empty_add(type="PLAIN_AXES", location=(-1.59, 2.96, 0.43))
focus = bpy.context.object
focus.name = "Focus on squirrel eye"
bpy.ops.object.camera_add(location=(-0.10, 2.52, 31.0))
camera = bpy.context.object
camera.name = "Wildlife portrait camera 120mm"
camera.data.lens = 120
camera.data.sensor_width = 36
camera.data.dof.use_dof = True
camera.data.dof.focus_object = focus
camera.data.dof.aperture_fstop = 0.60
camera.data.dof.aperture_blades = 7
# Keep the camera's horizon level. Blender's generic track quaternion can
# introduce a 90-degree roll when the view is almost exactly along -Z, which
# would turn the side-profile portrait on its side.
direction = Vector((0.0, 2.50, 0.0)) - camera.location
camera.rotation_euler = (
    math.atan2(direction.y, -direction.z),
    -math.atan2(direction.x, -direction.z),
    0.0,
)
scene.camera = camera

# Soft overcast winter light, plus a cool fill to keep the ivory fur readable.
bpy.ops.object.light_add(type="AREA", location=(-4.5, 7.0, 7.5))
key = bpy.context.object
key.name = "Overcast key"
key.data.energy = 720
key.data.shape = "DISK"
key.data.size = 6.0
key.data.color = (1.0, 0.93, 0.82)
key.rotation_euler = (math.radians(28), 0, math.radians(-28))
bpy.ops.object.light_add(type="AREA", location=(3.5, 3.0, 5.0))
fill = bpy.context.object
fill.name = "Snow fill"
fill.data.energy = 380
fill.data.size = 5.0
fill.data.color = (0.72, 0.82, 1.0)
fill.rotation_euler = (math.radians(70), 0, math.radians(145))

# Keep the portrait clean: no ground, roads, buildings or landscape planes.
scene.render.filepath = RENDER
bpy.ops.wm.save_as_mainfile(filepath=BLEND)

# Export only the posed base squirrel meshes for the game asset pipeline.
bpy.ops.object.select_all(action="DESELECT")
exported = []
for obj in squirrel_col.objects:
    if obj.get("squirrel_export") and obj.type == "MESH":
        obj.select_set(True)
        exported.append(obj)
bpy.context.view_layer.objects.active = exported[0]
try:
    bpy.ops.export_scene.fbx(filepath=FBX, use_selection=True, object_types={"MESH"}, apply_scale_options="FBX_SCALE_ALL", add_leaf_bones=False, bake_anim=False)
except Exception as exc:
    print("FBX_EXPORT_WARNING", exc)
bpy.ops.object.select_all(action="DESELECT")

bpy.ops.render.render(write_still=True)
print("SQUIRREL_PORTRAIT_OK", BLEND, RENDER, FBX)
