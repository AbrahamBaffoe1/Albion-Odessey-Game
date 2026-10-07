"""Blender preview renders of the Forest Treasure Run course, using the game's camera, sun direction and fog.

The geometry is exactly what gets exported for Unity; the look is an approximation (Cycles lighting plus the game's exponential-squared
fog applied afterwards), so use it to judge layout, colour and composition, not Unity's final shading.
"""
import math
import random
import sys
from pathlib import Path

import bpy  # noqa: F401
import numpy as np
from mathutils import Vector

from forest_geometry import Builder, LENGTH, blob, box, disc, sphere, tube

WIDTH, HEIGHT = 1280, 720
EXPOSURE = 1.0
SUN_UNITY_FORWARD = Vector((-.429, .686, -.588))          # Unity Euler(36,-32,0) light direction, mapped to Blender axes below
EXTRA = [("Preview skin", (.85, .66, .52), ""), ("Preview jeans", (.18, .24, .42), ""), ("Preview silver", (.65, .68, .74), "")]


def srgb_to_linear(c):
    return tuple(((v + .055) / 1.055) ** 2.4 if v > .04045 else v / 12.92 for v in c)


def make_materials(course):
    out = {}
    for name, color, _ in list(course.MATERIALS) + EXTRA:
        m = bpy.data.materials.new(name); m.use_nodes = True
        bsdf = m.node_tree.nodes["Principled BSDF"]
        bsdf.inputs["Base Color"].default_value = (*srgb_to_linear(color), 1)
        water = "water" in name
        bsdf.inputs["Roughness"].default_value = .08 if water else .85
        if "Metallic" in bsdf.inputs and water:
            bsdf.inputs["Metallic"].default_value = .3
        out[name] = m
    return out


def to_object(name, builder, materials, offset=(0, 0, 0)):
    mesh = bpy.data.meshes.new(name)
    verts = [(v[0], v[1], v[2]) for v in builder.vertices]
    faces, mat_index, normals = [], [], []
    for gi, group in enumerate(builder.groups):
        for tri in group:
            faces.append(tri); mat_index.extend([gi]); normals.extend([builder.vertices[i][3:6] for i in tri])
    mesh.from_pydata(verts, [], faces); mesh.update()
    for mname in builder.materials:
        mesh.materials.append(materials[mname])
    mesh.polygons.foreach_set("material_index", mat_index)
    mesh.normals_split_custom_set(normals)
    obj = bpy.data.objects.new(name, mesh); obj.location = offset
    bpy.context.scene.collection.objects.link(obj)
    return obj


def person(b, base, shirt, helmet=False):
    x, y, z = base
    for s in (-.11, .11):
        tube(b, "Preview jeans", (x + s, y, z + .05), (x + s, y, z + .85), .08, .075, 6)
    tube(b, shirt, (x, y, z + .8), (x, y, z + 1.45), .21, .19, 8)
    for s in (-.3, .3):
        tube(b, shirt, (x + s, y, z + 1.4), (x + s * 1.1, y + .12, z + 1.0), .06, .05, 5)
    sphere(b, "Preview skin", (x, y, z + 1.62), .13, 1)
    if helmet:
        sphere(b, "Preview silver", (x, y, z + 1.66), .15, 1, (1, 1, 1.05))
        box(b, "Forest Albion purple", (x, y, z + 1.85), (.05, .3, .14))


def event_props(b, y0):
    """The same obstacles and pickups the game builds for log, rock, branch, treasure and seed."""
    tube(b, "Forest bark", (-1.15, y0 + 14, .38), (1.15, y0 + 14, .38), .38, .38, 8)
    disc(b, "Forest leaf mid", (.2, y0 + 14, .78), .35, 8, .6)
    sphere(b, "Forest gold", (-2.4, y0 + 24, 1.0), .38, 1, (1, .1, 1))
    sphere(b, "Forest stone", (2.4, y0 + 30, .9), .8, 2, (1, .93, 1.1))
    tube(b, "Forest bark", (-1.35, y0 + 40, 1.55), (1.35, y0 + 40, 1.55), .11, .11, 6)
    for x in (-.95, .1, 1.0):
        sphere(b, "Forest leaf shade", (x, y0 + 40, 1.6), .31, 1, (1, .9, .7))
    sphere(b, "Forest gold", (0, y0 + 52, 1.0), .38, 1, (1, .1, 1))
    for k, lane in enumerate((-2.4, 0, 2.4)):
        sphere(b, "Forest foam", (lane, y0 + 62 + k * 2, 1.0), .21, 1, (1, 1.3, 1))


def setup_world(course):
    world = bpy.data.worlds.new("Forest sky"); world.use_nodes = True
    nodes, links = world.node_tree.nodes, world.node_tree.links
    sky = nodes.new("ShaderNodeTexSky"); sky.sky_type = "SINGLE_SCATTERING"; sky.sun_disc = False; sky.sun_elevation = math.radians(36); sky.sun_rotation = math.radians(180 + 32)
    sky.air_density = 1.0
    bg = nodes["Background"]; bg.inputs["Strength"].default_value = .38
    links.new(sky.outputs["Color"], bg.inputs["Color"])
    bpy.context.scene.world = world


def render_shot(name, cam_loc, target, out_dir, course, samples, fog_scale=1.0):
    sc = bpy.context.scene
    cam_data = bpy.data.cameras.new(name); cam_data.sensor_fit = "VERTICAL"; cam_data.angle = math.radians(62); cam_data.clip_end = 600
    cam = bpy.data.objects.new(name, cam_data); sc.collection.objects.link(cam); sc.camera = cam
    cam.location = Vector(cam_loc)
    cam.rotation_euler = (Vector(target) - Vector(cam_loc)).to_track_quat("-Z", "Y").to_euler()
    bpy.context.view_layer.update()
    sc.render.resolution_x, sc.render.resolution_y, sc.render.resolution_percentage = WIDTH, HEIGHT, 100
    sc.cycles.samples = samples; sc.cycles.use_denoising = True
    raw = out_dir / (name + ".exr")
    sc.render.image_settings.file_format = "OPEN_EXR"; sc.render.image_settings.color_depth = "32"; sc.render.filepath = str(raw)
    bpy.ops.render.render(write_still=True)
    img = bpy.data.images.load(str(raw))
    px = np.empty(WIDTH * HEIGHT * 4, dtype=np.float32); img.pixels.foreach_get(px)
    rgb = px.reshape(HEIGHT, WIDTH, 4)[::-1, :, :3].copy()         # top row first
    # depth by ray casting from the camera, so the game's exponential-squared fog can be applied exactly
    deps = bpy.context.evaluated_depsgraph_get()
    q = cam.matrix_world.to_quaternion(); fwd, right, up = q @ Vector((0, 0, -1)), q @ Vector((1, 0, 0)), q @ Vector((0, 1, 0))
    ty = math.tan(math.radians(31)); tx = ty * WIDTH / HEIGHT
    depth = np.full((HEIGHT, WIDTH), np.inf, dtype=np.float32); origin = cam.location.copy()
    step = 1
    for j in range(0, HEIGHT, step):
        dy = (1 - 2 * (j + .5) / HEIGHT) * ty
        for i in range(0, WIDTH, step):
            d = (fwd + right * ((2 * (i + .5) / WIDTH - 1) * tx) + up * dy).normalized()
            hit, loc, _, _, _, _ = sc.ray_cast(deps, origin, d, distance=600)
            if hit:
                depth[j, i] = (loc - origin).dot(fwd)
    land = np.isfinite(depth)
    fog = (1 - np.exp(-(course.CAMERA_FOG_DENSITY * fog_scale * np.where(land, depth, 0)) ** 2))[..., None]
    mist = np.array(srgb_to_linear(course.MIST), dtype=np.float32)
    rgb = np.where(land[..., None], rgb * (1 - fog) + mist * fog, rgb)
    rgb = 1 - np.exp(-rgb * EXPOSURE)                                  # soft shoulder
    out = np.clip(rgb, 0, 1) ** (1 / 2.2)
    result = bpy.data.images.new("preview " + name, WIDTH, HEIGHT, alpha=False)
    flat = np.ones((HEIGHT, WIDTH, 4), dtype=np.float32); flat[..., :3] = out[::-1]
    result.pixels.foreach_set(flat.ravel()); result.file_format = "PNG"
    path = out_dir / (name + ".png"); result.filepath_raw = str(path); result.save()
    raw.unlink(); print("rendered", path)


def render(stretches, args, course):
    samples = int(args[args.index("--samples") + 1]) if "--samples" in args else 48
    out_dir = Path(args[args.index("--out") + 1]) if "--out" in args else Path.cwd() / "forest-preview"
    out_dir.mkdir(parents=True, exist_ok=True)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.preferences.addon_enable(module="cycles")
    sc = bpy.context.scene; sc.render.engine = "CYCLES"; sc.cycles.device = "CPU"
    sc.view_settings.view_transform = "Raw"                      # linear output; tone mapping is done in post
    materials = make_materials(course)
    to_object("Stretch B", stretches["B"], materials, (0, 0, 0)); to_object("Stretch A", stretches["A"], materials, (0, -LENGTH, 0))
    to_object("Stretch B before A", stretches["B"], materials, (0, -2 * LENGTH, 0))          # the course repeats A, B, A, B...
    props = Builder(course.NAMES + [n for n, _, _ in EXTRA])
    for y0 in (0, -LENGTH):
        person(props, (0, y0, 0), "Forest Albion purple"); person(props, (0, y0 - 3.6, 0), "Forest steel rust", True); event_props(props, y0)
    to_object("Runners and props", props, materials)
    setup_world(course)
    sun_data = bpy.data.lights.new("Sun", "SUN"); sun_data.energy = 3.0; sun_data.color = (1, .93, .78); sun_data.angle = math.radians(1.2)
    sun = bpy.data.objects.new("Sun", sun_data); sc.collection.objects.link(sun)
    direction = Vector((SUN_UNITY_FORWARD.x, SUN_UNITY_FORWARD.z, SUN_UNITY_FORWARD.y))        # Unity (x,y,z) -> Blender (x,z,y)
    sun.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
    shots = [("run-stretch-b", (0, -10, 4.6), (0, 10, 4.6 - math.tan(math.radians(13)) * 20)),
             ("run-stretch-a", (0, -LENGTH - 10, 4.6), (0, -LENGTH + 10, 4.6 - math.tan(math.radians(13)) * 20)),
             ("bridge-closeup", (4, -LENGTH + 44, 2.6), (course.river_center(70), -LENGTH + 70, 2.0)),
             ("overview", (-95, -LENGTH - 25, 62), (12, -LENGTH / 2 - 10, 0), .12)]
    only = [a for a in args if a.startswith("--shot=")]
    for name, loc, target, *fog in shots:
        if only and "--shot=" + name not in only:
            continue
        render_shot(name, loc, target, out_dir, course, samples, fog[0] if fog else 1.0)
