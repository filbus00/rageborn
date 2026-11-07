# Shared tools for the characters drawn in the 16x16 style (2026-11-07, the owner: "remake all assets to be 16x16",
# "make new enemies, no need to stick with old models"). A character is five views drawn as letter grids (s, se, e,
# ne, n; the other three mirrored), a palette, and a role; every clip the game plays is made from the views here:
#   idle   a breath (a bob) and a blink (eyes, the letter E, shut)
#   walk / run   a step: the lower rows' left half lifted, then the right half, the body bobbing
#   attack a wind-up (drawn back), the strike (thrown forward with a white streak where it lands), the recovery;
#          an archer looses a streak from its bow hand, a caster lights its hands
#   hit    a jolt back;  death  the body sinks and folds to the ground
#   dash   (chargers) crouched, a low leap forward with a streak, landing;  roll_back a hop back
#   throw  a wind-up and a flick with a streak
# Each drawn pixel is 2 game pixels. Writes ArtSource/pixel/characters/<name> for Tools > ARPG > Import Drawn Characters,
# and a preview (every clip in five directions) to ArtSource/pixel/previews/<name>_moves.png.
# Use from a character's script run by Blender: import kit; kit.build(...)
import bpy, os, json
import numpy as np

HERE = os.path.dirname(os.path.abspath(__file__))
P = os.path.abspath(os.path.join(HERE, "..", "..", ".."))
BG = np.array([0.16, 0.21, 0.13, 1], np.float32)
SCALE = 2
DIRECTIONS = [("s", "s", False), ("sw", "se", True), ("w", "e", True), ("nw", "ne", True), ("n", "n", False),
              ("ne", "ne", False), ("e", "e", False), ("se", "se", False)]
# The way each view faces on the sheet (x right, y down the screen).
FACING = {"s": (0, 1), "se": (1, 1), "e": (1, 0), "ne": (1, -1), "n": (0, -1)}
COMMON = {"K": "140f1c", "n": "ffffff"}


class Character:
    def __init__(self, name, views, palette, role="melee", hand=None, eyes="E", margin=(6, 8, 6, 2), swap=None,
                 baked_scale=0.0, timing=None, step_rows=3, death_split=None, glow="n"):
        """views: {view: rows}; palette: letter -> hex; role: melee, archer, caster, charger, swarm (melee without
        extra moves); hand: {view: (x, y)} where shots and casts come from (archer, caster); margin: free pixels
        left, top, right, bottom round the views; swap: letters exchanged in the mirrored views (light and shade)."""
        self.name, self.views, self.role = name, views, role
        self.palette = dict(COMMON, **palette)
        self.hand = hand or {}
        self.eyes = eyes
        self.swap = swap or {}
        self.baked_scale = baked_scale
        self.step_rows = step_rows
        self.glow = glow
        h = max(len(v) for v in views.values())
        w = max(len(v[0]) for v in views.values())
        for key, rows in views.items():
            assert len(rows) == h and all(len(r) == w for r in rows), (name, key, [len(r) for r in rows])
            for r in rows:
                for ch in r:
                    assert ch == "." or ch in self.palette, (name, key, ch)
        self.gw, self.gh = w, h
        left, top, right, bottom = margin
        self.x0, self.y0 = left, top
        self.cw, self.ch = left + w + right, top + h + bottom
        # The mirror's axis is the cell's middle: centre the views on it.
        if (self.cw - w) % 2:
            self.cw += 1
        self.x0 = (self.cw - w) // 2
        self.death_split = death_split if death_split is not None else h // 2
        self.timing = timing or {}

    # Drawing.
    def blank(self):
        return [["."] * self.cw for _ in range(self.ch)]

    def put(self, cell, x, y, c, over=True):
        if 0 <= x < self.cw and 0 <= y < self.ch and c != "." and (over or cell[y][x] == "."):
            cell[y][x] = c

    def stamp(self, cell, rows, dx=0, dy=0, shut=False, step=None, crop=None):
        n = len(rows)
        for y, row in enumerate(rows):
            for x, c in enumerate(row):
                if c == ".":
                    continue
                if shut and c in self.eyes:
                    c = "K"
                yy = y
                # A step lifts the lower rows on one side of the middle.
                if step is not None and y >= n - self.step_rows and ((x < self.gw / 2) == (step == 0)):
                    yy -= 1
                py = self.y0 + yy + dy
                if crop is not None and py > crop:
                    continue
                self.put(cell, self.x0 + x + dx, py, c)

    def streak(self, cell, x, y, way, length=4):
        fx, fy = way
        for k in range(length):
            self.put(cell, x + fx * k, y + fy * k, self.glow, over=False)

    def front(self, view):
        """Where a strike lands: just past the body's edge, in the middle of its height, toward the facing."""
        fx, fy = FACING[view]
        cx = self.x0 + self.gw // 2 + fx * (self.gw // 2 + 1)
        cy = self.y0 + self.gh // 2 + fy * (self.gh // 3)
        return cx, cy

    def frame(self, view, dx=0, dy=0, shut=False, step=None, streak=None, glow_hands=False, rows=None, crop=None):
        cell = self.blank()
        self.stamp(cell, rows or self.views[view], dx, dy, shut, step, crop)
        if glow_hands and view in self.hand:
            hx, hy = self.hand[view]
            for ox, oy in ((0, 0), (1, 0), (0, -1), (-1, 0), (0, 1)):
                self.put(cell, self.x0 + hx + ox + dx, self.y0 + hy + oy + dy, self.glow, over=ox == 0 and oy == 0)
        if streak:
            x, y, way, length = streak
            self.streak(cell, x + dx, y + dy, way, length)
        return cell

    def death(self, view, k, frames):
        rows = self.views[view]
        cell = self.blank()
        ground = self.y0 + self.gh - 1
        sink = round(k * (self.gh * 0.75) / (frames - 1))
        for y, row in enumerate(rows):
            shift = sink if y < self.death_split else sink + sink // 2
            if self.y0 + y + shift > ground:
                continue
            for x, c in enumerate(row):
                if c != ".":
                    self.put(cell, self.x0 + x, self.y0 + y + shift, "K" if c in self.eyes else c)
        return cell

    # The clips.
    def clips(self, view):
        fx, fy = FACING[view]
        back = (-fx, -fy)
        fwd = (fx, fy)
        f = self.frame
        c = {
            "idle": [f(view), f(view), f(view, dy=1), f(view, shut=True)],
            "walk": [f(view), f(view, step=0, dy=-1), f(view), f(view, step=1, dy=-1)],
            "run": [f(view, dx=fx), f(view, dx=fx, step=0, dy=-1), f(view, dx=fx), f(view, dx=fx, step=1, dy=-1)],
            "hit": [f(view, dx=back[0], dy=back[1] - 1, shut=True), f(view)],
            "death": [self.death(view, k, 6) for k in range(6)],
        }
        sx, sy = self.front(view)
        if self.role == "archer":
            hx, hy = self.hand.get(view, (self.gw // 2, self.gh // 2))
            hx, hy = self.x0 + hx, self.y0 + hy
            c["attack"] = [f(view), f(view, dx=back[0], dy=back[1]), f(view, dx=back[0], dy=back[1], glow_hands=True),
                           f(view, streak=(hx + fx * 2, hy + fy * 2, fwd, 6)), f(view)]
        elif self.role == "caster":
            c["attack"] = [f(view, glow_hands=True), f(view, dy=-1, glow_hands=True), f(view, dy=-2, glow_hands=True),
                           f(view, dx=fx, dy=fy, streak=(sx, sy, fwd, 3)), f(view)]
        else:
            c["attack"] = [f(view, dx=back[0] * 2, dy=back[1] * 2 - 1), f(view, dx=back[0], dy=back[1] - 1),
                           f(view, dx=fx * 2, dy=fy * 2, streak=(sx, sy - 2, (fx or 1, 1), 5)), f(view, dx=fx, dy=fy), f(view)]
        if self.role in ("charger", "melee"):
            c["dash"] = [f(view, dy=1), f(view, dx=fx * 2, dy=fy * 2 - 2, streak=(sx - fx * (self.gw + 4), sy, back, 4)),
                         f(view, dx=fx * 3, dy=fy * 3 - 1), f(view, dx=fx, dy=fy)]
            c["roll_back"] = [f(view, dy=1), f(view, dx=back[0] * 2, dy=back[1] * 2 - 3), f(view, dx=back[0] * 2, dy=back[1] * 2 - 1), f(view, dy=1)]
        if self.role in ("melee", "archer", "caster"):
            c["throw"] = [f(view, dx=back[0], dy=back[1] - 1), f(view, dx=back[0] * 2, dy=back[1] * 2 - 1),
                          f(view, dx=fx, dy=fy, streak=(sx, sy - 2, fwd, 5)), f(view)]
        return c

    # Output.
    def rgba(self, cell, mirror):
        out = np.zeros((self.ch, self.cw, 4), np.float32)
        for y in range(self.ch):
            for x in range(self.cw):
                c = cell[y][self.cw - 1 - x] if mirror else cell[y][x]
                if mirror:
                    c = self.swap.get(c, c)
                if c != ".":
                    h = self.palette[c]
                    out[y, x] = [int(h[i:i + 2], 16) / 255 for i in (0, 2, 4)] + [1]
        return out

    def build(self):
        views = {v: self.clips(v) for v in self.views}
        out = os.path.join(P, "ArtSource", "pixel", "characters", self.name)
        os.makedirs(out, exist_ok=True)
        for old in os.listdir(out):
            if old.endswith(".png"):
                os.remove(os.path.join(out, old))
        animations = []
        timing = {"idle": (1.6, 0), "walk": (0.9, 1.0), "run": (0.45, 3.0), "attack": (0.8, 0), "hit": (0.25, 0),
                  "death": (0.9, 0), "dash": (0.5, 0), "roll_back": (0.6, 0), "throw": (0.6, 0)}
        timing.update(self.timing)
        for clip in views["s"]:
            rows = [np.concatenate([self.rgba(fr, mirror) for fr in views[view][clip]], 1) for _, view, mirror in DIRECTIONS]
            save(up(np.concatenate(rows, 0), SCALE), os.path.join(out, f"{self.name}_{clip}.png"))
            seconds, speed = timing[clip]
            animations.append({"name": clip, "frames": len(views["s"][clip]), "seconds": seconds, "speed": speed})
        with open(os.path.join(out, "sheet.json"), "w") as fh:
            json.dump({"cellWidth": self.cw * SCALE, "cellHeight": self.ch * SCALE, "pivotX": self.cw * SCALE // 2,
                       "pivotY": self.ch * SCALE - (self.y0 + self.gh) * SCALE + SCALE // 2, "directions": 8,
                       "animations": animations, "bakedScale": self.baked_scale}, fh)
        # The preview: each clip in the front, left, back and right rows, 3 times.
        strips = []
        width = max(len(fr) for fr in views["s"].values()) * self.cw
        for clip in views["s"]:
            for _, view, mirror in [DIRECTIONS[i] for i in (0, 2, 4, 6)]:
                strip = np.concatenate([on_bg(self.rgba(fr, mirror)) for fr in views[view][clip]], 1)
                strips.append(np.concatenate([strip, np.tile(BG, (self.ch, width - strip.shape[1], 1))], 1))
            strips.append(np.tile(np.array([0.08, 0.1, 0.06, 1], np.float32), (1, width, 1)))
        save(up(np.concatenate(strips, 0), 3), os.path.join(P, "ArtSource", "pixel", "previews", f"{self.name}_moves.png"))
        big = np.concatenate([on_bg(self.rgba(self.frame(v), False)) for v in ("s", "se", "e", "ne", "n")], 1)
        save(up(big, 6), os.path.join(P, "ArtSource", "pixel", "previews", f"{self.name}_views.png"))
        print("wrote", self.name, len(animations), "clips")


def up(img, k):
    return np.repeat(np.repeat(img, k, 0), k, 1)


def on_bg(img):
    return np.where(img[..., 3:4] > 0.5, img, BG)


def save(img, path):
    h, w = img.shape[:2]
    im = bpy.data.images.new(os.path.basename(path), w, h, alpha=True)
    im.pixels = img[::-1].ravel()
    im.filepath_raw = path
    im.file_format = "PNG"
    im.save()
    bpy.data.images.remove(im)


def mirror_rows(rows):
    """A view drawn facing left made to face right (or the other way)."""
    return [r[::-1] for r in rows]
