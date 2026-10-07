"""Author the Forest Treasure Run course in Blender and export it for Unity.

Two 210 m stretches of the Whitehouse-inspired course (A: river bend with the arched footbridge; B: marsh boardwalk) are built
procedurally, written as the AOM1 meshes the game loads (Unity/Assets/Resources/CampusCraft/forest_stretch_a/b.bytes plus
forest.json), saved as a .blend, and rendered to preview images with the game's camera, light and fog.

    PYTHONPATH=<dir containing the bpy wheel> python3 Tools/build_forest_course.py [--no-render] [--samples N] [--out DIR]

Reference: public photographs of Whitehouse Nature Center (Kalamazoo River, rust-brown arched footbridge, split-rail fence) and
the centre's published habitats: oak-hickory and flood-plain forest, marsh with a boardwalk, ponds and a tall-grass prairie.
"""
import json
import math
import random
import struct
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import bpy  # noqa: F401,E402  (registers mathutils when run as the pip module)
from mathutils import Vector  # noqa: E402
from forest_geometry import (LENGTH, TILE, Builder, blade_tuft, blob, box, cone, disc, smoothstep, sphere, tube)  # noqa: E402

ROOT = Path(__file__).resolve().parents[1]
RES = ROOT / "Unity" / "Assets" / "Resources" / "CampusCraft"

# name, colour, ground texture, smoothness. Names are what Unity shows; "glass" in a name gives the glossy water look.
MATERIALS = [
    ("Forest moss", (.30, .48, .16), ""), ("Forest trail earth", (.66, .52, .32), ""), ("Forest trail edge", (.43, .41, .22), ""),
    ("Forest trail lane", (.70, .58, .40), ""), ("Forest river mud", (.34, .26, .16), ""), ("Forest river glass water", (.07, .27, .31), ""),
    ("Forest foam", (.80, .90, .88), ""), ("Forest bark", (.30, .20, .12), ""), ("Forest dead wood", (.55, .50, .44), ""),
    ("Forest leaf sun", (.40, .56, .18), ""), ("Forest leaf mid", (.26, .42, .15), ""), ("Forest leaf shade", (.14, .28, .12), ""),
    ("Forest willow", (.58, .70, .30), ""), ("Forest grass blade", (.45, .65, .22), ""), ("Forest reed", (.55, .62, .28), ""),
    ("Forest cattail", (.33, .20, .12), ""), ("Forest stone", (.55, .57, .56), ""), ("Forest steel rust", (.46, .24, .13), ""),
    ("Forest plank wood", (.52, .38, .24), ""), ("Forest Albion purple", (.32, .10, .47), ""), ("Forest gold", (.94, .65, .15), ""),
    ("Forest flower white", (.95, .95, .88), ""), ("Forest flower lilac", (.68, .50, .88), ""), ("Forest flower gold", (.96, .78, .20), ""),
    ("Forest squirrel black", (.04, .04, .05), ""), ("Forest sign cream", (.86, .82, .70), ""), ("Forest fern", (.30, .52, .22), ""),
    ("Forest leaf litter", (.55, .34, .16), ""), ("Forest meadow", (.34, .52, .17), ""), ("Forest moss deep", (.26, .43, .14), ""),
    ("Forest dry grass", (.40, .52, .19), ""),
]
M = {n.replace("Forest ", "").replace(" ", "_"): n for n, _, _ in MATERIALS}
NAMES = [n for n, _, _ in MATERIALS]
LEAVES = (M["leaf_sun"], M["leaf_mid"], M["leaf_shade"])
CAMERA_FOG_DENSITY = .0095
MIST = (.74, .86, .86)


# ── terrain ───────────────────────────────────────────────────────────────────

def river_center(y):
    return 29.0 + 3.2 * math.sin(math.tau * y / LENGTH + .6)


class Terrain:
    def __init__(self, variant):
        self.variant = variant

    def height(self, x, y):
        """Ground height. Exactly zero on the trail (|x| < 5.5) so runners never float or sink."""
        rc = river_center(y)
        away = smoothstep(5.5, 16.0, abs(x))
        left = smoothstep(8, 95, -x)
        hills = 2.4 * left * (.55 + .45 * math.sin(math.tau * 2 * y / LENGTH + x * .03)) + .28 * math.sin(x * .21 + math.tau * 3 * y / LENGTH) * math.cos(x * .13)
        far = smoothstep(10, 70, x - rc) * 3.4 * (.7 + .3 * math.sin(math.tau * y / LENGTH * 2 + 1.0))
        ground = (hills + far) * away
        d = abs(x - rc)
        bed = -.95 + .1 * math.sin(x * .6 + y * .2)
        if self.variant == "A":                       # a grassy islet in the bend
            island = 1.45 * (1 - smoothstep(1.8, 4.8, math.hypot(x - rc + 1.0, y - 150)))
            bed += island
        bank = smoothstep(6.0, 10.0, d)
        h = bed * (1 - bank) + ground * bank
        if self.variant == "B":                        # marsh pond beside the boardwalk
            pond = 1 - smoothstep(7, 13.5, math.hypot(x + 26, y - 62))
            h = h * (1 - pond) + (-.85) * pond
        return h

    def normal(self, x, y):
        e = .5
        dx = self.height(x + e, y) - self.height(x - e, y)
        dy = self.height(x, y + e) - self.height(x, y - e)
        return Vector((-dx / (2 * e), -dy / (2 * e), 1)).normalized()

    def surface(self, x, y):
        h = self.height(x, y)
        if h < -.30:
            return M["river_mud"]
        if h < -.06 or (self.variant == "B" and math.hypot(x + 26, y - 62) < 14.5 and h < .12):
            return M["moss_deep"]                                  # wet, dark bank grass above the mud line
        n = (math.sin(x * .11 + math.tau * 3 * y / LENGTH) + .8 * math.sin(x * .07 - math.tau * 2 * y / LENGTH + 1.7) + .4 * math.sin(x * .23 + math.tau * 5 * y / LENGTH + .4))
        return M["moss_deep"] if n < -.9 else M["moss"] if n < .55 else M["meadow"] if n < 1.3 else M["dry_grass"]


def trail_edge(y, side, scale=1.0):
    """Half-width of the worn trail at y; the two sides wander independently and repeat every stretch."""
    k = 1 if side > 0 else 2
    wob = .45 * math.sin(math.tau * 7 * y / LENGTH + k) + .3 * math.sin(math.tau * 19 * y / LENGTH + 4 * k) + .15 * math.sin(math.tau * 41 * y / LENGTH + k)
    return (3.5 + wob) * scale


def build_trail(b):
    """Smooth ribbons over the flat ground: worn earth in the middle, an olive verge band either side."""
    ys = [i * 1.5 for i in range(int(LENGTH / 1.5) + 1)]
    for a, c in zip(ys, ys[1:]):
        for side in (-1, 1):
            e0, e1 = trail_edge(a, side), trail_edge(c, side)
            v0, v1 = e0 + 1.35 + .25 * math.sin(math.tau * 30 * a / LENGTH), e1 + 1.35 + .25 * math.sin(math.tau * 30 * c / LENGTH)
            points = [(side * e0, a, .02), (side * v0, a, .02), (side * v1, c, .02), (side * e1, c, .02)]
            b.quad(M["trail_edge"], *(points if side > 0 else reversed(points)))
        b.quad(M["trail_earth"], (-trail_edge(a, -1), a, .03), (trail_edge(a, 1), a, .03), (trail_edge(c, 1), c, .03), (-trail_edge(c, -1), c, .03))


def build_terrain(b, t):
    xs, x = [], -135.0                                             # fine cells near the trail keep its worn edge smooth
    while x < 135.0:
        xs.append(x)
        x += 1.0 if -30 <= x < 42 else 2.0 if -70 <= x < 90 else 6.0
    xs.append(135.0)
    ys = [i * 1.5 for i in range(int(LENGTH / 1.5) + 1)]
    grid = [[(x, y, t.height(x, y)) for x in xs] for y in ys]
    nrm = [[t.normal(x, y) for x in xs] for y in ys]
    uv = lambda p: (p[0] / TILE, p[1] / TILE)
    for j in range(len(ys) - 1):
        for i in range(len(xs) - 1):
            p00, p10, p11, p01 = grid[j][i], grid[j][i + 1], grid[j + 1][i + 1], grid[j + 1][i]
            n00, n10, n11, n01 = nrm[j][i], nrm[j][i + 1], nrm[j + 1][i + 1], nrm[j + 1][i]
            cx, cy = (p00[0] + p11[0]) / 2, (p00[1] + p11[1]) / 2
            m = t.surface(cx, cy)
            v = lambda p: (p[0], p[1], p[2])
            b.tri(m, v(p00), v(p10), v(p11), n00, n10, n11, uv(p00), uv(p10), uv(p11))
            b.tri(m, v(p00), v(p11), v(p01), n00, n11, n01, uv(p00), uv(p11), uv(p01))
    # river surface: a strip that follows the meander, sitting just under the banks
    prev = None
    for y in ys:
        rc = river_center(y)
        row = [(rc - 9.5, y, -.25), (rc + 9.5, y, -.25)]
        if prev:
            b.quad(M["river_glass_water"], prev[0], prev[1], row[1], row[0])
        prev = row
    if t.variant == "B":
        disc(b, M["river_glass_water"], (-26, 62, -.22), 13.2, 28)


# ── plants ────────────────────────────────────────────────────────────────────

def oak(b, p, s, rng):
    p = Vector(p); h = rng.uniform(3.2, 4.4) * s; r = rng.uniform(.26, .34) * s
    top = p + Vector((rng.uniform(-.25, .25) * s, rng.uniform(-.25, .25) * s, h))
    tube(b, M["bark"], p - Vector((0, 0, .3)), p + Vector((0, 0, .9 * s)), r * 1.7, r * 1.25, 7, caps=False)
    tube(b, M["bark"], p + Vector((0, 0, .8 * s)), top, r * 1.25, r * .8, 7)
    for _ in range(3):
        a = rng.random() * math.tau
        end = top + Vector((math.cos(a) * rng.uniform(1.3, 2.0), math.sin(a) * rng.uniform(1.3, 2.0), rng.uniform(.6, 1.3))) * s
        tube(b, M["bark"], top - Vector((0, 0, .4 * s)), end, r * .55, r * .22, 5)
    crown = top + Vector((0, 0, 2.0 * s))
    for _ in range(rng.randint(5, 7)):
        o = Vector((rng.uniform(-2.2, 2.2), rng.uniform(-2.2, 2.2), rng.uniform(-.9, 1.7))) * s
        blob(b, LEAVES, crown + o, (rng.uniform(1.7, 2.5) * s, rng.uniform(1.7, 2.5) * s, rng.uniform(1.3, 1.9) * s), rng.random() * 3, 1, .13, rng.randrange(10 ** 6))


def hickory(b, p, s, rng):
    p = Vector(p); h = rng.uniform(5.5, 7.5) * s; r = rng.uniform(.2, .27) * s
    tube(b, M["bark"], p - Vector((0, 0, .3)), p + Vector((0, 0, h)), r * 1.4, r * .6, 7)
    for k in range(rng.randint(4, 5)):
        z = h * (.55 + .1 * k) + rng.uniform(0, .6)
        blob(b, LEAVES, p + Vector((rng.uniform(-.5, .5), rng.uniform(-.5, .5), z)), (rng.uniform(1.3, 1.9) * s, rng.uniform(1.3, 1.9) * s, rng.uniform(1.5, 2.1) * s), rng.random() * 3, 1, .14, rng.randrange(10 ** 6))


def willow(b, p, s, rng):
    p = Vector(p); h = 3.4 * s
    tube(b, M["bark"], p - Vector((0, 0, .3)), p + Vector((.5, 0, h)), .5 * s, .3 * s, 7)
    crown = p + Vector((.5, 0, h + 1.0 * s))
    blob(b, (M["willow"], M["willow"], M["leaf_mid"]), crown, (3.4 * s, 3.4 * s, 1.7 * s), 0, 1, .1, rng.randrange(10 ** 6))
    for k in range(18):
        a = math.tau * k / 18 + rng.random() * .3
        base = crown + Vector((math.cos(a) * 3.0 * s, math.sin(a) * 3.0 * s, -.5 * s))
        cone(b, M["willow"], base, base + Vector((0, 0, -rng.uniform(2.2, 3.4) * s)), .2 * s, 3)


def snag(b, p, s, rng):
    p = Vector(p); h = 6.5 * s
    tube(b, M["dead_wood"], p - Vector((0, 0, .3)), p + Vector((.2, 0, h)), .38 * s, .14 * s, 7)
    for k in range(5):
        a = rng.random() * math.tau; z = h * (.45 + .1 * k)
        base = p + Vector((.15, 0, z))
        tube(b, M["dead_wood"], base, base + Vector((math.cos(a) * 1.8, math.sin(a) * 1.8, rng.uniform(.5, 1.4))) * s, .1 * s, .03 * s, 5)


def shrub(b, p, s, rng):
    p = Vector(p)
    for _ in range(3):
        blob(b, LEAVES, p + Vector((rng.uniform(-.6, .6), rng.uniform(-.6, .6), .55 * s)), (rng.uniform(.9, 1.4) * s, rng.uniform(.9, 1.4) * s, rng.uniform(.6, .9) * s), rng.random() * 3, 1, .12, rng.randrange(10 ** 6))


def boulder(b, p, s, rng):
    blob(b, (M["leaf_mid"], M["stone"], M["stone"]), Vector(p) + Vector((0, 0, .15 * s)), (s * rng.uniform(.8, 1.3), s * rng.uniform(.7, 1.1), s * rng.uniform(.5, .8)), rng.random() * 3, 1, .22, rng.randrange(10 ** 6))


def fallen_log(b, p, rng, length=None):
    p = Vector(p); L = length or rng.uniform(3, 5); a = rng.random() * math.tau; d = Vector((math.cos(a), math.sin(a), 0))
    tube(b, M["bark"], p - d * L / 2, p + d * L / 2, .3, .27, 8)
    disc(b, M["plank_wood"], p + d * L / 2 + Vector((0, 0, 0)), .01, 3)


def fern(b, p, rng):
    for _ in range(3):
        blob(b, (M["fern"],) * 3, Vector(p) + Vector((rng.uniform(-.35, .35), rng.uniform(-.35, .35), .14)), (rng.uniform(.5, .8), rng.uniform(.18, .3), .12), rng.random() * 3.14, 1, .1, rng.randrange(10 ** 6))


def flower(b, p, rng):
    mat = rng.choice([M["flower_white"], M["flower_lilac"], M["flower_gold"]])
    p = Vector(p); h = rng.uniform(.45, .8)
    cone(b, M["grass_blade"], p, p + Vector((0, 0, h)), .02, 3)
    sphere(b, mat, p + Vector((0, 0, h)), rng.uniform(.07, .11), 1, (1, 1, .7))


def reed(b, p, rng, cattail=True):
    p = Vector(p); h = rng.uniform(1.1, 2.3)
    tip = p + Vector((rng.uniform(-.15, .15), rng.uniform(-.15, .15), h))
    cone(b, M["reed"], p, tip, .04, 3)
    if cattail and rng.random() < .35:
        tube(b, M["cattail"], tip - Vector((0, 0, .32)), tip - Vector((0, 0, .06)), .05, .05, 5)


# ── set pieces ────────────────────────────────────────────────────────────────

def arched_bridge(b, t, y0, rng):
    """Rust-brown steel arch footbridge with a plank rail, after the one at Whitehouse Nature Center."""
    rc = river_center(y0); x1, x2 = rc - 10.5, rc + 10.5; deck_z = .55; span = x2 - x1
    for side in (-1, 1):                                            # stone abutments
        box(b, M["stone"], (x1 if side < 0 else x2, y0, .2), (1.2, 2.8, .9))
    for k in range(int(span / .42)):                                # deck planks
        box(b, M["plank_wood"], (x1 + .3 + k * .42, y0, deck_z), (.36, 2.4, .1))
    for edge in (-1.15, 1.15):                                      # steel curb beams
        box(b, M["steel_rust"], ((x1 + x2) / 2, y0 + edge, deck_z - .12), (span, .12, .2))
    sag = 2.6; R = (span ** 2 / 4 + sag ** 2) / (2 * sag)
    arc = lambda u: (x1 + span * u, deck_z + (math.sqrt(max(R * R - (span * (u - .5)) ** 2, 0)) - (R - sag)))
    for edge in (-1.15, 1.15):
        pts = [arc(i / 16) for i in range(17)]
        for a, c in zip(pts, pts[1:]):
            tube(b, M["steel_rust"], (a[0], y0 + edge, a[1]), (c[0], y0 + edge, c[1]), .13, .13, 6)
        for k in range(1, int(span / .38)):                         # slatted steel side, as in the photograph
            u = k * .38 / span; x, z = arc(u)
            if z - deck_z > .2:
                box(b, M["steel_rust"], (x, y0 + edge, (z + deck_z) / 2), (.07, .06, z - deck_z))
        for k in range(0, int(span / 1.6) + 1):                     # rail posts and plank rail on the deck
            x = x1 + .5 + k * 1.6
            box(b, M["plank_wood"], (x, y0 + edge * .86, deck_z + .55), (.12, .12, 1.1))
        for z in (deck_z + .5, deck_z + .95):
            box(b, M["plank_wood"], ((x1 + x2) / 2, y0 + edge * .86, z), (span - 1, .05, .13))
    # the black squirrel from the photograph, sitting on the arch (about twice life size so players can see it)
    ax, az = arc(.36); s = 2.0; q = Vector((ax, y0 - 1.15, az + .13))
    sphere(b, M["squirrel_black"], q, .1 * s, 1, (1.6, .8, .8))
    sphere(b, M["squirrel_black"], q + Vector((-.15 * s, 0, .03 * s)), .065 * s, 1)
    for k, (dx, dz) in enumerate(((.12, .1), (.2, .22), (.22, .36))):
        sphere(b, M["squirrel_black"], q + Vector((dx * s, 0, dz * s)), (.07 - .01 * k) * s, 1, (1, .7, 1))


def trail_gate(b, y0):
    """A weathered timber trailhead arch with an Albion-purple banner and a gold star, 5.5 m clear above the trail."""
    for side in (-1, 1):
        box(b, M["plank_wood"], (side * 5.9, y0, 2.9), (.5, .5, 5.8))
        box(b, M["stone"], (side * 5.9, y0, .15), (.9, .9, .3))
    pts = [(-5.9 + 11.8 * i / 10, 5.7 + 1.0 * math.sin(math.pi * i / 10)) for i in range(11)]
    for a, c in zip(pts, pts[1:]):
        tube(b, M["plank_wood"], (a[0], y0, a[1]), (c[0], y0, c[1]), .24, .24, 6)
    box(b, M["gold"], (0, y0 - .3, 4.55), (5.0, .06, 1.34)); box(b, M["Albion_purple"], (0, y0 - .34, 4.55), (4.7, .06, 1.1))
    box(b, M["gold"], (0, y0 - .4, 4.55), (.56, .05, .56), rot_z=0, tilt=(0, math.pi / 4))
    for side in (-1, 1):
        tube(b, M["plank_wood"], (side * 5.9, y0, 4.4), (side * 3.4, y0, 5.8), .09, .09, 5)


def boardwalk(b, x0, y_from, y_to):
    for k in range(int((y_to - y_from) / .4)):
        box(b, M["plank_wood"], (x0, y_from + .2 + k * .4, .33), (1.8, .34, .07))
    for side in (-.85, .85):
        box(b, M["bark"], (x0 + side, (y_from + y_to) / 2, .26), (.12, y_to - y_from, .1))
    for k in range(int((y_to - y_from) / 3) + 1):
        for side in (-.85, .85):
            box(b, M["bark"], (x0 + side, y_from + k * 3, .0), (.14, .14, .7))


def split_rail_fence(b, x0, y_from, y_to):
    n = int((y_to - y_from) / 3.2)
    for k in range(n + 1):
        box(b, M["plank_wood"], (x0, y_from + k * 3.2, .65), (.14, .14, 1.3))
    for z in (.5, .95):
        box(b, M["plank_wood"], (x0 + .05, (y_from + y_to) / 2, z), (.06, y_to - y_from, .12))


def info_sign(b, x, y):
    box(b, M["bark"], (x, y, .6), (.12, .12, 1.2)); box(b, M["bark"], (x + .5, y, .6), (.12, .12, 1.2))
    box(b, M["sign_cream"], (x + .25, y - .09, 1.15), (1.2, .06, .62))
    for k, w in enumerate((.9, .7, .8)):
        box(b, M["bark"], (x + .25, y - .13, 1.3 - k * .15), (w, .02, .045))


def trail_details(b, t, rng):
    for k in range(int(LENGTH / 7)):                                # pale lane dashes give the run something to read speed from
        for side in (-1.2, 1.2):
            box(b, M["trail_lane"], (side, 3.5 + k * 7, .035), (.11, 1.7, .03))
    for _ in range(26):
        sphere(b, M["leaf_litter"], (rng.uniform(-3.6, 3.6), rng.uniform(0, LENGTH), .03), rng.uniform(.3, .6), 1, (1.2, .8, .06))
    for _ in range(34):
        d = rng.uniform(.1, .22); sphere(b, M["stone"], ((1 if rng.random() < .5 else -1) * rng.uniform(3.2, 4.8), rng.uniform(0, LENGTH), d * .3), d, 1, (1, 1, .6))


# ── stretches ────────────────────────────────────────────────────────────────

def build_stretch(variant):
    rng = random.Random(1835 if variant == "A" else 1877)
    b = Builder(NAMES); t = Terrain(variant)
    build_terrain(b, t)
    build_trail(b)
    trail_details(b, t, rng)
    placed = []

    def free(x, y, r):
        return all((x - px) ** 2 + (y - py) ** 2 > (r + pr) ** 2 * .6 for px, py, pr in placed)

    def place(fn, count, xr, yr, radius, scale=(.85, 1.3), tries=40, clear=None):
        done = 0
        for _ in range(count * tries):
            if done >= count:
                break
            x, y = rng.uniform(*xr), rng.uniform(*yr)
            if clear and not clear(x, y):
                continue
            if abs(x) < 9.2 and radius > .8 or not free(x, y, radius):
                continue
            h = t.height(x, y)
            if h < -.12:
                continue
            s = rng.uniform(*scale)
            fn(b, (x, y, h), s, rng); placed.append((x, y, radius * s)); done += 1

    rc = lambda y: river_center(y)
    keep_clear = lambda x, y: abs(x - rc(y)) > 11.5 and not (variant == "B" and math.hypot(x + 26, y - 62) < 17) and not (variant == "B" and -16 < x < -9 and 15 < y < 115)
    place(oak, 30, (-95, -10), (8, LENGTH - 8), 2.6, scale=(1.0, 1.6), clear=keep_clear)
    place(hickory, 22, (-95, -10), (8, LENGTH - 8), 1.6, scale=(1.0, 1.5), clear=keep_clear)
    place(oak, 18, (rc(100) + 12, rc(100) + 85), (8, LENGTH - 8), 2.6, scale=(1.1, 1.7), clear=lambda x, y: x > rc(y) + 11.5)
    place(hickory, 16, (rc(100) + 12, rc(100) + 85), (8, LENGTH - 8), 1.6, scale=(1.1, 1.6), clear=lambda x, y: x > rc(y) + 11.5)
    place(oak, 6, (9.8, 17), (8, LENGTH - 8), 2.2, scale=(.7, 1.0), clear=lambda x, y: x < rc(y) - 12.5)
    place(hickory, 7, (9.8, 17), (8, LENGTH - 8), 1.4, scale=(.8, 1.1), clear=lambda x, y: x < rc(y) - 12.5)
    place(oak, 8, (-24, -10), (8, LENGTH - 8), 2.4, scale=(.8, 1.2), clear=keep_clear)
    place(willow, 6, (rc(100) - 17, rc(100) + 21), (8, LENGTH - 8), 3.4, scale=(.8, 1.1), clear=lambda x, y: 9 < abs(x - rc(y)) < 14 and not (variant == "A" and abs(y - 70) < 18))
    place(lambda b, p, s, r: shrub(b, p, s, r), 60, (-40, 18), (0, LENGTH), 1.2, scale=(.8, 1.5), clear=lambda x, y: abs(x) > 7 and keep_clear(x, y))
    place(boulder, 10, (-60, 22), (0, LENGTH), .9, scale=(.7, 1.5), clear=lambda x, y: abs(x) > 7)
    place(lambda b, p, s, r: fallen_log(b, p, r), 5, (-30, -7), (0, LENGTH), 1.8, clear=lambda x, y: keep_clear(x, y))
    place(lambda b, p, s, r: fern(b, p, r), 90, (-45, 21), (0, LENGTH), .5, clear=lambda x, y: abs(x) > 6.2 and keep_clear(x, y))
    # river banks: tall reeds and cattails at the waterline, lily pads and flowers on the water
    for _ in range(120):
        y = rng.uniform(0, LENGTH); side = rng.choice((-1, 1)); x = rc(y) + side * rng.uniform(6.4, 8.6)
        if t.height(x, y) < .35 and not (variant == "A" and abs(y - 70) < 3):
            reed(b, (x, y, max(t.height(x, y), -.3)), rng)
    for _ in range(46):
        y = rng.uniform(0, LENGTH); x = rc(y) + rng.uniform(-4.5, 4.5)
        if t.height(x, y) < -.4:
            r = rng.uniform(.35, .6); disc(b, M["leaf_sun"], (x, y, -.2), r, 9)
            if rng.random() < .3:
                sphere(b, M["flower_white"], (x, y, -.15), .1, 1, (1, 1, .6))
    for _ in range(420):                                           # tall grass and wildflowers crowd the verges so the trail feels cut through living woods
        y = rng.uniform(0, LENGTH); x = (1 if rng.random() < .5 else -1) * rng.uniform(5.7, 14)
        if x < rc(y) - 9 and (variant != "B" or not (-16 < x < -9 and 15 < y < 115)):
            blade_tuft(b, M["grass_blade"], (x, y, t.height(x, y)), rng, 8, rng.uniform(.5, 1.0))
            if rng.random() < .35:
                flower(b, (x + rng.uniform(-.4, .4), y, t.height(x, y)), rng)
    if variant == "A":
        arched_bridge(b, t, 70, rng)
        isl = Vector((rc(150) - 1.0, 150, t.height(rc(150) - 1.0, 150)))
        snag(b, isl + Vector((.4, .3, 0)), 1.0, rng)
        for _ in range(22):
            blade_tuft(b, M["grass_blade"], isl + Vector((rng.uniform(-3, 3), rng.uniform(-3, 3), 0)), rng, 10, rng.uniform(.6, 1.1))
        fallen_log(b, (rc(112), 112, -.1), rng, 4.2)
        for _ in range(130):                                          # tall-grass prairie and wildflowers on the left meadow
            x, y = rng.uniform(-46, -11), rng.uniform(115, 200)
            blade_tuft(b, M["grass_blade"], (x, y, t.height(x, y)), rng, 7, rng.uniform(.6, 1.1))
        for _ in range(110):
            x, y = rng.uniform(-46, -11), rng.uniform(115, 200); flower(b, (x, y, t.height(x, y)), rng)
        info_sign(b, -7.6, 34)
    else:
        boardwalk(b, -12.8, 18, 112)
        for _ in range(26):                                           # cattails ringing the marsh pond
            a = rng.random() * math.tau; r = rng.uniform(11, 15.5)
            x, y = -26 + math.cos(a) * r, 62 + math.sin(a) * r
            if not (-16 < x < -9.5):
                for _ in range(4):
                    reed(b, (x + rng.uniform(-.5, .5), y + rng.uniform(-.5, .5), max(t.height(x, y), -.3)), rng)
        for _ in range(34):
            a = rng.random() * math.tau; r = rng.uniform(1, 11.5)
            disc(b, M["leaf_sun"], (-26 + math.cos(a) * r, 62 + math.sin(a) * r, -.18), rng.uniform(.4, .7), 9)
        split_rail_fence(b, -9.6, 128, 194)
        snag(b, (-38, 40, t.height(-38, 40)), 1.1, rng)
        info_sign(b, -7.6, 100)
        for _ in range(60):
            x, y = rng.uniform(-30, -10), rng.uniform(125, 195); flower(b, (x, y, t.height(x, y)), rng)
    trail_gate(b, 105)
    return b


# ── Unity export ──────────────────────────────────────────────────────────────

def export_unity(b, path):
    """AOM1: the same buffers CraftModel.Load reads (Blender x,y,z -> Unity x,z,y with reversed winding)."""
    with path.open("wb") as f:
        f.write(b"AOM1"); f.write(struct.pack("<ii", len(b.vertices), len(b.groups)))
        for v in b.vertices:
            f.write(struct.pack("<8f", v[0], v[2], v[1], v[3], v[5], v[4], v[6], v[7]))
        for group in b.groups:
            idx = [i for tri in group for i in reversed(tri)]
            f.write(struct.pack("<i", len(idx))); f.write(struct.pack("<" + "i" * len(idx), *idx))


def write_descriptor():
    data = {"materials": [{"name": n, "color": [c[0], c[1], c[2], 1.0], "texture": tex} for n, c, tex in MATERIALS],
            "sections": [{"name": "forest", "colliders": []}],
            "note": "Authored by Tools/build_forest_course.py; stretch meshes forest_stretch_a/b share this material list."}
    (RES / "forest.json").write_text(json.dumps(data, indent=1))


if __name__ == "__main__":
    args = sys.argv[1:]
    stretches = {v: build_stretch(v) for v in "AB"}
    RES.mkdir(parents=True, exist_ok=True)
    for v, b in stretches.items():
        export_unity(b, RES / f"forest_stretch_{v.lower()}.bytes")
        print(f"stretch {v}: {b.triangle_count} triangles, {len(b.vertices)} vertices")
    write_descriptor()
    if "--no-render" not in args:
        import forest_preview
        forest_preview.render(stretches, args, sys.modules[__name__])
