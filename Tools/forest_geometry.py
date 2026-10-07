"""Geometry kit for the Forest Treasure Run course (pure Python + mathutils; no scene needed).

Every shape is appended to a Builder as triangles with explicit smooth or flat normals and one material each, so the very same
arrays can be written as the Unity AOM1 mesh the game loads and as a Blender object for preview renders.
Coordinates are Blender's: x across the trail, y along the course (the run direction), z up.
"""
import math
import random
import bpy  # noqa: F401  (registers mathutils when run as the pip module)
from mathutils import Matrix, Vector

LENGTH = 210.0           # one stretch of course, metres; terrain is periodic in y with exactly this period
TILE = 7.0               # ground texture repeats every TILE metres (LENGTH / TILE is a whole number)


def smoothstep(a, b, x):
    t = max(0.0, min(1.0, (x - a) / (b - a)))
    return t * t * (3 - 2 * t)


class Builder:
    """Triangles grouped by material name; vertices are shared when position, normal and uv match."""

    def __init__(self, materials):
        self.materials = list(materials)
        self.index = {name: i for i, name in enumerate(self.materials)}
        self.vertices = []
        self.lookup = {}
        self.groups = [[] for _ in self.materials]

    def vertex(self, p, n, uv):
        key = (round(p[0], 4), round(p[1], 4), round(p[2], 4), round(n[0], 3), round(n[1], 3), round(n[2], 3), round(uv[0], 4), round(uv[1], 4))
        found = self.lookup.get(key)
        if found is None:
            found = len(self.vertices)
            self.vertices.append((p[0], p[1], p[2], n[0], n[1], n[2], uv[0], uv[1]))
            self.lookup[key] = found
        return found

    def tri(self, material, a, b, c, na=None, nb=None, nc=None, uva=(0, 0), uvb=(0, 0), uvc=(0, 0)):
        a, b, c = Vector(a), Vector(b), Vector(c)
        flat = (b - a).cross(c - a)
        if flat.length < 1e-9:
            return
        flat.normalize()
        na, nb, nc = (na or flat), (nb or flat), (nc or flat)
        self.groups[self.index[material]].append((self.vertex(a, na, uva), self.vertex(b, nb, uvb), self.vertex(c, nc, uvc)))

    def quad(self, material, a, b, c, d, flat=True):
        self.tri(material, a, b, c)
        self.tri(material, a, c, d)

    @property
    def triangle_count(self):
        return sum(len(g) for g in self.groups)


# ── primitives ────────────────────────────────────────────────────────────────

def _basis(axis):
    axis = Vector(axis).normalized()
    helper = Vector((0, 0, 1)) if abs(axis.z) < .95 else Vector((1, 0, 0))
    u = axis.cross(helper).normalized()
    return u, axis.cross(u).normalized()


def tube(b, material, p0, p1, r0, r1, sides=7, caps=True):
    """A tapered cylinder with smooth radial normals."""
    p0, p1 = Vector(p0), Vector(p1)
    u, v = _basis(p1 - p0)
    ring = [(math.cos(2 * math.pi * i / sides) * u + math.sin(2 * math.pi * i / sides) * v) for i in range(sides)]
    for i in range(sides):
        j = (i + 1) % sides
        b.tri(material, p0 + ring[i] * r0, p0 + ring[j] * r0, p1 + ring[j] * r1, ring[i], ring[j], ring[j])
        b.tri(material, p0 + ring[i] * r0, p1 + ring[j] * r1, p1 + ring[i] * r1, ring[i], ring[j], ring[i])
        if caps:
            b.tri(material, p1, p1 + ring[i] * r1, p1 + ring[j] * r1)
            b.tri(material, p0, p0 + ring[j] * r0, p0 + ring[i] * r0)


def cone(b, material, base, tip, radius, sides=3):
    tube(b, material, base, tip, radius, 0.0005, sides, caps=False)


def box(b, material, center, size, rot_z=0.0, tilt=(0.0, 0.0)):
    """Flat-shaded box; size is (x, y, z); rotation about z, then tilt about x and y (radians)."""
    m = Matrix.Translation(center) @ Matrix.Rotation(rot_z, 4, 'Z') @ Matrix.Rotation(tilt[0], 4, 'X') @ Matrix.Rotation(tilt[1], 4, 'Y')
    sx, sy, sz = size[0] / 2, size[1] / 2, size[2] / 2
    corners = [m @ Vector((x * sx, y * sy, z * sz)) for x in (-1, 1) for y in (-1, 1) for z in (-1, 1)]
    c = lambda x, y, z: corners[(x > 0) * 4 + (y > 0) * 2 + (z > 0)]
    for quad in (((0, 0, 0), (0, 1, 0), (0, 1, 1), (0, 0, 1)), ((1, 0, 0), (1, 0, 1), (1, 1, 1), (1, 1, 0)),
                 ((0, 0, 0), (0, 0, 1), (1, 0, 1), (1, 0, 0)), ((0, 1, 0), (1, 1, 0), (1, 1, 1), (0, 1, 1)),
                 ((0, 0, 0), (1, 0, 0), (1, 1, 0), (0, 1, 0)), ((0, 0, 1), (0, 1, 1), (1, 1, 1), (1, 0, 1))):
        pts = [c(*q) for q in quad]
        b.quad(material, *pts)


def disc(b, material, center, radius, sides=10, squash=1.0, rot=0.0):
    center = Vector(center)
    for i in range(sides):
        a0, a1 = rot + 2 * math.pi * i / sides, rot + 2 * math.pi * (i + 1) / sides
        b.tri(material, center, center + Vector((math.cos(a1) * radius, math.sin(a1) * radius * squash, 0)),
              center + Vector((math.cos(a0) * radius, math.sin(a0) * radius * squash, 0)), (0, 0, 1), (0, 0, 1), (0, 0, 1))


_ICO = None


def _icosphere(subdiv):
    global _ICO
    if _ICO and subdiv in _ICO:
        return _ICO[subdiv]
    t = (1 + 5 ** .5) / 2
    verts = [Vector(v).normalized() for v in ((-1, t, 0), (1, t, 0), (-1, -t, 0), (1, -t, 0), (0, -1, t), (0, 1, t), (0, -1, -t), (0, 1, -t), (t, 0, -1), (t, 0, 1), (-t, 0, -1), (-t, 0, 1))]
    faces = [(0, 11, 5), (0, 5, 1), (0, 1, 7), (0, 7, 10), (0, 10, 11), (1, 5, 9), (5, 11, 4), (11, 10, 2), (10, 7, 6), (7, 1, 8),
             (3, 9, 4), (3, 4, 2), (3, 2, 6), (3, 6, 8), (3, 8, 9), (4, 9, 5), (2, 4, 11), (6, 2, 10), (8, 6, 7), (9, 8, 1)]
    for _ in range(subdiv):
        cache, nf = {}, []

        def mid(i, j):
            key = (min(i, j), max(i, j))
            if key not in cache:
                verts.append(((verts[i] + verts[j]) / 2).normalized())
                cache[key] = len(verts) - 1
            return cache[key]
        for a, bb, c in faces:
            ab, bc, ca = mid(a, bb), mid(bb, c), mid(c, a)
            nf += [(a, ab, ca), (bb, bc, ab), (c, ca, bc), (ab, bc, ca)]
        faces = nf
    _ICO = _ICO or {}
    _ICO[subdiv] = (verts, faces)
    return _ICO[subdiv]


def blob(b, materials, center, radii, rot_z=0.0, subdiv=1, wobble=0.0, seed=0):
    """A smooth ellipsoid with a per-vertex wobble. `materials` is (top, side, under); faces pick by how far up they face."""
    verts, faces = _icosphere(subdiv)
    rng = random.Random(seed)
    bumps = {}
    def displaced(i):
        if i not in bumps:
            bumps[i] = 1 + wobble * (rng.random() - .5) * 2
        return bumps[i]
    rot = Matrix.Rotation(rot_z, 3, 'Z')
    center = Vector(center)
    def point(i):
        u = verts[i]; k = displaced(i)
        return center + rot @ Vector((u.x * radii[0] * k, u.y * radii[1] * k, u.z * radii[2] * k))
    def normal(i):
        u = verts[i]
        return (rot @ Vector((u.x / radii[0], u.y / radii[1], u.z / radii[2]))).normalized()
    for a, bb, c in faces:
        up = (verts[a].z + verts[bb].z + verts[c].z) / 3
        m = materials[0] if up > .4 else materials[1] if up > -.15 else materials[2]
        b.tri(m, point(a), point(bb), point(c), normal(a), normal(bb), normal(c))


def sphere(b, material, center, radius, subdiv=1, squash=(1, 1, 1), rot_z=0.0):
    blob(b, (material, material, material), center, (radius * squash[0], radius * squash[1], radius * squash[2]), rot_z, subdiv)


def blade_tuft(b, material, center, rng, count=7, height=.7, spread=.35):
    """A clump of tall grass: thin leaning three-sided blades."""
    c = Vector(center)
    for _ in range(count):
        a = rng.random() * math.tau
        base = c + Vector((math.cos(a) * spread * rng.random(), math.sin(a) * spread * rng.random(), 0))
        lean = Vector((math.cos(a), math.sin(a), 0)) * height * .3 * rng.random()
        cone(b, material, base, base + lean + Vector((0, 0, height * (.65 + .6 * rng.random()))), .035 + .02 * rng.random(), 3)
