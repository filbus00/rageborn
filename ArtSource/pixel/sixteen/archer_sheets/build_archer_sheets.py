# The Wild Arrow from the owner's sheet (2026-11-11, the owner: "check downloads, new sheets and animations for the
# character"): a blonde archer in green with a red cape, first in 16 x 16 cells, then (the same day) wild_arrow_archer_24x24.png
# in 24 x 24 cells, a row for each of
# the 8 directions in the game's order (s, sw, w, nw, n, ne, e, se), 14 columns: idle 0-1, walk 2-5, shot 6-9, dive
# (crouch, two rolls, up) 10-13; wild_arrow_archer_preview.gif shows them playing. Written as the game's sheets for the
# drawn character "wild_arrow_drawn" (each pixel 2 game pixels, as the enemies), in place of wild_arrow_16.py's, which
# would overwrite them. The sheet has no hit or death: the hit is the crouch, the death the crouch and then her standing
# frame laid on its side. Every armour look gets the same pictures. (wild_arrow_arrow_16x16.png, eight 10-pixel sticks,
# and its 24 x 24 version are not used: her drawn arrows stay.)
# 21 pixels tall (2026-11-11, the owner: "rework the character to be 21px tall"): the sheet draws her 23 tall, so two
# rows are taken out of every frame (one out of the rolls), the rows that repeat a neighbour most: one in her tunic
# and one in her legs (never her head: a row taken from her hair flattened it), the same rows through a direction's
# standing frames, a row either way where a frame loses less. The frames as shortened are written to wild_arrow_archer_21.png beside the sheet.
# Run: Blender -b -P ArtSource/pixel/sixteen/archer_sheets/build_archer_sheets.py, then Tools > ARPG > Import Drawn Characters.
import bpy, os, json
import numpy as np

HERE = os.path.dirname(os.path.abspath(__file__))
P = os.path.abspath(os.path.join(HERE, "..", "..", "..", ".."))
OUT = os.path.join(P, "ArtSource", "pixel", "characters", "wild_arrow_drawn")
C = 24
SOURCE = "wild_arrow_archer_24x24.png"
SCALE = 2


def load(path):
    im = bpy.data.images.load(path)
    w, h = im.size
    a = np.array(im.pixels[:], np.float32).reshape(h, w, 4)[::-1].copy()   # top row first
    bpy.data.images.remove(im)
    return a


def save(img, path):
    h, w = img.shape[:2]
    im = bpy.data.images.new(os.path.basename(path), w, h, alpha=True)
    im.pixels = img[::-1].ravel()
    im.filepath_raw = path
    im.file_format = "PNG"
    im.save()
    bpy.data.images.remove(im)


sheet = load(os.path.join(HERE, SOURCE))


def raw(row, col):
    return sheet[row * C:(row + 1) * C, col * C:(col + 1) * C].copy()


def cell(row, col):
    return short[row][col].copy()


TALL = 21


def row_costs(frame):
    """For each row, the pixels it does not share with the nearer-matching of its neighbours: what taking it out loses."""
    drawn = np.concatenate([frame[..., :3] * frame[..., 3:], frame[..., 3:]], -1)
    diff = (np.abs(drawn[1:] - drawn[:-1]).sum(-1) > 0.05).sum(1).astype(float)     # row i against i + 1
    cost = np.full(C, 1e6)
    cost[1:-1] = np.minimum(diff[:-1], diff[1:])
    return cost


def without(frame, rows):
    keep = [y for y in range(C) if y not in rows]
    out = np.zeros_like(frame)
    out[len(rows):] = frame[keep]
    return out


def shorten(frames):
    """A direction's 14 frames, her standing ones (0 to 9) 21 tall."""
    costs = [row_costs(f) for f in frames]
    tops = [int(np.where(f[..., 3] > 0.5)[0].min()) for f in frames]
    top = min(tops[:10])
    total = sum(costs[:10])
    # Never her head (2026-11-11, the owner: "her head is really flat": the first try took a row of the hair over her
    # face and the dome went): one row of her tunic and one of her legs, at least three apart.
    body = top + 10 + int(np.argmin(total[top + 10:top + 16]))
    legs = top + 16 + int(np.argmin(total[top + 16:C - 2]))
    out = []
    for i, f in enumerate(frames):
        height = C - tops[i]
        if i < 10:
            rows = [min((body - 1, body, body + 1), key=lambda y: costs[i][y] + (0.5 if y != body else 0)),
                    min((legs - 1, legs, legs + 1), key=lambda y: costs[i][y] + (0.5 if y != legs else 0))]
            if abs(rows[0] - rows[1]) < 2:
                rows = [body, legs]
        else:
            # The dive: the crouches lose two rows, the rolls one, wherever it shows least.
            order = [int(y) for y in np.argsort(costs[i]) if tops[i] < y < C - 1]
            rows = [order[0]]
            if height > 16:
                rows.append(next(y for y in order[1:] if abs(y - order[0]) > 2))
        out.append(without(f, rows))
    return out


def lying(frame, row, body=None):
    """Her standing frame turned on its side, lying on the cell's bottom, her head toward where she faced. A helm's
    frame is turned and placed as her body's (body) is."""
    k = 1 if row in (1, 2, 3) else -1
    turned = np.rot90(frame, k)
    ys, xs = np.where(np.rot90(frame if body is None else body, k)[..., 3] > 0.5)
    part = turned[ys.min():ys.max() + 1, xs.min():xs.max() + 1]
    out = np.zeros_like(frame)
    h, w = part.shape[:2]
    left = (C - w) // 2
    out[C - h:, left:left + w] = part
    return out


short = [shorten([raw(row, col) for col in range(14)]) for row in range(8)]
save(np.concatenate([np.concatenate(frames, 1) for frames in short], 0), os.path.join(HERE, "wild_arrow_archer_21.png"))

# --- Helms (2026-11-11, the owner: "Start working on the sprites for the different helms") ------------------------------
# A helm is a layer over her body, a sheet for every clip as the body's (the game's helm layer: "helm_<look>_1h_<clip>").
# It is made from her own head in every frame, so it follows her in all eight directions with nothing drawn by hand
# twice: her head is found in the frame (its dome matched against the standing frame's), its pixels read as hair, skin
# or eyes, and the helm says what each becomes; her long hair hangs out under it. Horns, plumes and feathers are a few
# pixels put on top with an outline. In the two rolls of the dive, where her head is not found, the hair in the top of
# the ball takes the helm's colours. This is the first pass: one rule set for every direction.
def rgb(*c):
    return np.array(c, np.float32) / 255.0


INK = rgb(18, 14, 24)
HAIR = [rgb(240, 200, 104), rgb(230, 194, 92), rgb(190, 134, 52)]      # the first two are its light
SKIN = [rgb(236, 190, 154), rgb(226, 150, 132), rgb(192, 134, 106)]
EYE = rgb(36, 28, 44)


def is_colour(px, colours):
    return any(np.abs(px[:3] - c).max() < 0.02 for c in colours) and px[3] > 0.5


# light, mid, dark; dome: how many rows of the dome it covers from the top (4 all); sides: the hair beside her face;
# face: "open", "mask" (cloth over mouth and chin), "barbute" (a T left open), "visor" (shut but a dark slit);
# band: only a band over the hair (rows of the dome); extra: pixels put on top.
HELMS = {
    "hood": dict(ramp=[rgb(62, 132, 66), rgb(36, 84, 50), rgb(24, 56, 40)], dome=4, sides=True, face="open"),
    "mask": dict(ramp=[rgb(92, 100, 112), rgb(60, 66, 80), rgb(40, 44, 56)], dome=0, band=[3], sides=False, face="mask"),
    "barbute": dict(ramp=[rgb(176, 184, 196), rgb(120, 130, 148), rgb(72, 80, 100)], dome=4, sides=True, face="barbute"),
    "visored": dict(ramp=[rgb(210, 216, 226), rgb(150, 160, 178), rgb(90, 98, 120)], dome=4, sides=True, face="visor", extra="plume"),
    "horned": dict(ramp=[rgb(116, 116, 134), rgb(74, 74, 94), rgb(44, 44, 60)], dome=4, sides=True, face="visor", extra="horns"),
    "falconer": dict(ramp=[rgb(170, 118, 62), rgb(132, 86, 46), rgb(84, 52, 30)], dome=4, sides=True, face="open", extra="feather"),
    "unblinking_crown": dict(ramp=[rgb(232, 140, 36), rgb(176, 92, 28), rgb(110, 58, 24)], dome=0, band=[2, 3], sides=False, face="open", extra="crown"),
}
BONE = [rgb(242, 238, 222), rgb(200, 190, 160)]
PLUME = [rgb(192, 42, 46), rgb(122, 22, 38)]
GEM = rgb(110, 220, 232)
HEAD_ROWS = 8          # the dome's outline row, three rows of dome, four of face
HEAD_LEFT, HEAD_WIDE = 7, 10


def find_head(frame, stand, top):
    """Where her head's box (its top left in the standing frame: HEAD_LEFT, top) is in this frame, or None."""
    patch = stand[top:top + 4, HEAD_LEFT - 1:HEAD_LEFT + HEAD_WIDE + 1]
    solid = patch[..., 3] > 0.5
    best = None
    for dy in range(-4, 11):
        for dx in range(-7, 8):
            y, x = top + dy, HEAD_LEFT - 1 + dx
            if y < 0 or x < 0 or y + 4 > C or x + patch.shape[1] > C:
                continue
            here = frame[y:y + 4, x:x + patch.shape[1]]
            wrong = (np.abs(here - patch).max(-1) > 0.02) & solid
            score = wrong.sum() / max(1, solid.sum()) + 0.002 * (abs(dx) + abs(dy))
            if best is None or score < best[0]:
                best = (score, x + 1, y)
    return (best[1], best[2]) if best[0] < 0.3 else None


def put(helm, frame, x, y, colour, stamped):
    if 0 <= x < C and 0 <= y < C:
        helm[y, x, :3] = colour
        helm[y, x, 3] = 1
        if frame[y, x, 3] < 0.5:
            stamped.append((x, y))


def helm_frame(frame, spec, head, row):
    """The helm's pixels over this frame of her body."""
    helm = np.zeros_like(frame)
    light, mid, dark = spec["ramp"]
    if head is None:
        # A roll or a dive her head's dome is not found in: the head is the hair round her face (or, with no face
        # showing, the top of the ball), and the helm covers it as it covers her standing head.
        solid = frame[..., 3] > 0.5
        face = [(x, y) for y in range(C) for x in range(C) if is_colour(frame[y, x], SKIN + [EYE]) and any(
            0 <= x + dx < C and 0 <= y + dy < C and is_colour(frame[y + dy, x + dx], HAIR)
            for dx in (-1, 0, 1) for dy in (-1, 0, 1))]
        hair = [(x, y) for y in range(C) for x in range(C) if is_colour(frame[y, x], HAIR)]
        if not hair:
            return helm
        if face:
            mx, my = sum(x for x, _ in face) / len(face), sum(y for _, y in face) / len(face)
        else:
            top = min(y for _, y in hair)
            near = [(x, y) for x, y in hair if y < top + 5]
            mx, my = sum(x for x, _ in near) / len(near), sum(y for _, y in near) / len(near)
        full = spec["dome"] > 0
        for x, y in hair:
            if max(abs(x - mx), abs(y - my)) <= 4.5 and (full or abs(y - my) < 1.0 and spec.get("band")):
                put(helm, frame, x, y, light if is_colour(frame[y, x], HAIR[:2]) else mid, [])
        for x, y in face:
            if spec["face"] == "visor" or (spec["face"] == "mask" and y >= my):
                put(helm, frame, x, y, INK if spec["face"] == "visor" and abs(y - my) < 0.6 else mid, [])
        return helm
    hx, hy = head
    stamped = []
    faces = [[x for x in range(hx - 1, hx + HEAD_WIDE + 1) if 0 <= x < C and is_colour(frame[hy + r, x], SKIN + [EYE])]
             if hy + r < C else [] for r in range(HEAD_ROWS)]
    seen = any(faces[r] for r in range(4, HEAD_ROWS))
    for r in range(HEAD_ROWS):
        y = hy + r
        if y >= C:
            break
        for x in range(max(0, hx - 1), min(C, hx + HEAD_WIDE + 1)):
            px = frame[y, x]
            hair = is_colour(px, HAIR)
            bright = is_colour(px, HAIR[:2])
            if hair and (r < spec["dome"] or (r >= 4 and spec["sides"]) or r in spec.get("band", ())):
                shade = light if bright else mid
                # The rim: the helm's last row, and the row over her face.
                if spec["sides"] and (r == HEAD_ROWS - 1 or (r == 3 and seen and faces[4] and min(faces[4]) <= x <= max(faces[4]))):
                    shade = dark if not bright else mid
                put(helm, frame, x, y, shade, stamped)
            elif r >= 4 and is_colour(px, SKIN + [EYE]) and faces[r]:
                middle = (min(faces[5] or faces[r]) + max(faces[5] or faces[r])) / 2.0
                face = spec["face"]
                if face == "mask" and r >= 6:
                    put(helm, frame, x, y, light if r == 6 else mid, stamped)
                elif face == "barbute" and (r == 4 or (r >= 6 and abs(x - middle) > 1)):
                    put(helm, frame, x, y, mid if r == 4 else light, stamped)
                elif face == "visor":
                    put(helm, frame, x, y, light if r == 4 else INK if r == 5 else mid if r == 6 else dark, stamped)
    # What stands on top. West-facing rows lean their pieces the other way.
    extra = spec.get("extra")
    east = row in (5, 6, 7)
    side = row in (2, 6)
    cx = hx + 4
    if extra == "horns":
        if side:
            for dx, dy, c in ((0, 1, 1), (1, 1, 1), (1, 0, 1), (2, 0, 0), (2, -1, 0), (3, -1, 0), (3, -2, 0)):
                put(helm, frame, cx + (dx if not east else 1 - dx), hy + dy, BONE[c], stamped)
        else:
            for dx, dy, c in ((0, 2, 1), (-1, 2, 1), (-1, 1, 1), (-2, 1, 0), (-2, 0, 0), (-3, 0, 0), (-3, -1, 0), (-3, -2, 0)):
                put(helm, frame, hx + dx, hy + dy, BONE[c], stamped)
                put(helm, frame, hx + HEAD_WIDE - 1 - dx, hy + dy, BONE[c], stamped)
    elif extra == "plume":
        for dx, dy, c in ((0, 0, 1), (1, 0, 1), (0, -1, 0), (1, -1, 0), (1, -2, 0)):
            put(helm, frame, cx + (dx if not east else 1 - dx), hy + dy, PLUME[c], stamped)
    elif extra == "feather":
        for dx, dy, c in ((7, 1, 1), (8, 0, 0), (8, -1, 0), (9, -2, 0)):
            put(helm, frame, hx + (dx if not east else HEAD_WIDE - 1 - dx), hy + dy, BONE[c] if dy < 1 else PLUME[0], stamped)
    elif extra == "crown":
        # Its points over the band, and the eye in the middle of it where her face shows.
        for x in range(hx + 1, hx + HEAD_WIDE - 1):
            if (x - hx) % 2 == 1 and hy + 1 < C and is_colour(frame[hy + 1, x], HAIR):
                put(helm, frame, x, hy + 1, rgb(255, 232, 140), stamped)
        if row in (0, 1, 7) and faces[5] and max(faces[5]) - min(faces[5]) >= 3:
            gx = int(round((min(faces[5]) + max(faces[5])) / 2.0))
            for x in (gx, gx + (0 if (max(faces[5]) - min(faces[5])) % 2 == 0 else 1)):
                if is_colour(frame[hy + 3, x], HAIR) or helm[hy + 3, x, 3] > 0.5:
                    put(helm, frame, x, hy + 3, GEM, stamped)
    # An outline round what stands outside her.
    for x, y in stamped:
        for nx, ny in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)):
            if 0 <= nx < C and 0 <= ny < C and frame[ny, nx, 3] < 0.5 and helm[ny, nx, 3] < 0.5:
                helm[ny, nx, :3] = INK
                helm[ny, nx, 3] = 1
    return helm


heads = []
for row in range(8):
    stand = short[row][0]
    top = int(np.where(stand[..., 3] > 0.5)[0].min())
    heads.append([find_head(short[row][col], stand, top) for col in range(14)])
    print("heads", row, heads[row])
helm_cells = {look: [[helm_frame(short[row][col], spec, heads[row][col], row) for col in range(14)] for row in range(8)]
              for look, spec in HELMS.items()}
# Every helm on her, to look at: a helm a block, her standing frame, a walk, a shot and the dive's four in 8 directions.
SHOWN = [0, 3, 8, 10, 11, 12, 13]
sheet_of_helms = []
for look in HELMS:
    block = []
    for col in SHOWN:
        line = []
        for row in range(8):
            cellpx = short[row][col].copy()
            h = helm_cells[look][row][col]
            cellpx[h[..., 3] > 0.5] = h[h[..., 3] > 0.5]
            line.append(cellpx)
        block.append(np.concatenate(line, 1))
    sheet_of_helms.append(np.concatenate(block, 0))
save(np.concatenate(sheet_of_helms, 1), os.path.join(HERE, "wild_arrow_helms_preview.png"))

# --- Armour and bows (2026-11-11, the owner: "add sprites for armor and bows") -----------------------------------------
# Chest armour is her body's own sheets with her tunic in the armour's colours (the game's body layer has a sheet a
# look: "body_<look>"); a bow is a layer over her, her drawn bow and its string in the bow's colours ("weapon_<look>").
GREENS = [rgb(116, 182, 92), rgb(62, 132, 66), rgb(36, 84, 50)]        # her tunic: light, mid, dark
REDS = [rgb(192, 42, 46), rgb(122, 22, 38)]                             # her cape
WOOD = [rgb(154, 104, 54), rgb(132, 86, 46), rgb(84, 52, 30)]           # her bow, belt and straps: light, mid, dark
STRING = [rgb(242, 238, 222), rgb(228, 224, 202), rgb(214, 220, 230)]

# ramp: the tunic's light, mid, dark; marks: (colour, every how many pixels) dotted over it; weave: light and mid
# alternate (scales, mail); cape: her cape's two reds.
ARMOUR = {
    "bare": None,
    "ranger": dict(ramp=[rgb(150, 148, 72), rgb(104, 106, 52), rgb(62, 68, 40)]),
    "brigand": dict(ramp=[rgb(140, 84, 66), rgb(98, 56, 50), rgb(60, 36, 38)], marks=(rgb(206, 190, 160), 3)),
    "scale": dict(ramp=[rgb(218, 170, 84), rgb(168, 116, 52), rgb(106, 70, 40)], weave=True),
    "plate": dict(ramp=[rgb(196, 204, 216), rgb(138, 148, 166), rgb(82, 90, 110)]),
    "knight": dict(ramp=[rgb(232, 236, 242), rgb(166, 176, 194), rgb(98, 106, 128)], marks=(rgb(240, 200, 104), 4),
                   cape=[rgb(62, 98, 180), rgb(36, 54, 116)]),
    "stag_hide": dict(ramp=[rgb(216, 172, 112), rgb(172, 126, 76), rgb(112, 80, 52)], marks=(rgb(242, 238, 222), 3)),
    "cinder_jerkin": dict(ramp=[rgb(92, 84, 90), rgb(58, 52, 60), rgb(36, 32, 40)], marks=(rgb(244, 138, 44), 3),
                          cape=[rgb(70, 62, 70), rgb(44, 38, 48)]),
}
# half: half its length in pixels; depth: how far its middle stands from the string; recurve: tips turned forward;
# thick: two pixels through the middle; wood: light, mid, dark; string.
BOWS = {
    "hunting_bow": dict(half=5, depth=2, wood=WOOD, string=rgb(242, 238, 222)),
    "recurve_bow": dict(half=5, depth=2.2, recurve=1.5, wood=[rgb(176, 84, 56), rgb(132, 52, 44), rgb(82, 30, 34)], string=rgb(242, 238, 222)),
    "horn_bow": dict(half=4, depth=2.6, recurve=1, wood=[rgb(226, 218, 190), rgb(170, 160, 136), rgb(104, 96, 88)], string=rgb(60, 48, 44)),
    "yew_longbow": dict(half=7, depth=2, wood=[rgb(232, 200, 130), rgb(196, 152, 86), rgb(128, 92, 52)], string=rgb(242, 238, 222)),
    "war_bow": dict(half=7, depth=2.5, thick=True, wood=[rgb(156, 162, 178), rgb(96, 92, 100), rgb(44, 38, 42)], string=rgb(214, 220, 230)),
    "great_bow": dict(half=8, depth=3, thick=True, wood=[rgb(112, 104, 132), rgb(240, 200, 104), rgb(44, 38, 58)], string=rgb(240, 200, 104)),
    "splinterbough": dict(half=5, depth=2, wood=[rgb(150, 190, 92), rgb(96, 120, 60), rgb(60, 70, 40)], string=rgb(206, 232, 150)),
    "ember_tongue": dict(half=6, depth=2.5, recurve=1, wood=[rgb(255, 196, 80), rgb(236, 112, 40), rgb(150, 40, 30)], string=rgb(255, 232, 150)),
    "hunters_promise": dict(half=6, depth=2, wood=[rgb(210, 220, 236), rgb(120, 146, 196), rgb(60, 78, 130)], string=rgb(242, 238, 222)),
    "galeheart": dict(half=5, depth=2.2, recurve=1.5, wood=[rgb(220, 250, 250), rgb(130, 214, 220), rgb(60, 140, 160)], string=rgb(240, 255, 255)),
    "widows_draw": dict(half=6, depth=3, recurve=1, wood=[rgb(170, 110, 200), rgb(104, 60, 140), rgb(52, 30, 78)], string=rgb(220, 190, 240)),
    "gallowsreach": dict(half=8, depth=2.5, wood=[rgb(170, 164, 150), rgb(112, 108, 100), rgb(62, 60, 60)], string=rgb(150, 120, 80)),
    "stillwater_yew": dict(half=8, depth=2, wood=[rgb(150, 220, 200), rgb(70, 160, 150), rgb(36, 96, 100)], string=rgb(220, 250, 240)),
    "the_long_silence": dict(half=9, depth=3, thick=True, wood=[rgb(132, 130, 164), rgb(78, 76, 104), rgb(24, 22, 34)], string=rgb(200, 204, 220)),
}


def armour_frame(frame, spec):
    """Her body with her tunic (and cape) in an armour's colours."""
    if spec is None:
        return frame
    out = frame.copy()
    for y in range(C):
        for x in range(C):
            px = frame[y, x]
            if px[3] < 0.5:
                continue
            for i, green in enumerate(GREENS):
                if is_colour(px, [green]):
                    colour = spec["ramp"][i]
                    if spec.get("weave") and i < 2 and (x + y) % 2 == 0:
                        colour = spec["ramp"][1 - i]
                    marks = spec.get("marks")
                    if marks and i == 1 and x % marks[1] == 0 and y % marks[1] == 1:
                        colour = marks[0]
                    out[y, x, :3] = colour
            if "cape" in spec:
                for i, red in enumerate(REDS):
                    if is_colour(px, [red]):
                        out[y, x, :3] = spec["cape"][i]
    return out


def bow_recolour(frame, spec, head, row):
    """The dive's frames, where her drawn bow stays in her picture: her drawn bow, its string and the arrow on it, in the bow's colours. Her belt
    (wood with her tunic over and under it), the quiver's strap across her cape (seen from behind, wood with the cape on
    both sides) and the feather in her hair are left."""
    bow = np.zeros_like(frame)

    def tunic(x, y, step):
        y += step
        while 0 <= y < C and is_colour(frame[y, x], WOOD):
            y += step
        return 0 <= y < C and is_colour(frame[y, x], GREENS)

    def cape(x, y, step):
        x += step
        while 0 <= x < C and is_colour(frame[y, x], WOOD):
            x += step
        return 0 <= x < C and is_colour(frame[y, x], REDS)

    for y in range(C):
        for x in range(C):
            px = frame[y, x]
            if px[3] < 0.5:
                continue
            strap = row in (3, 4, 5) and cape(x, y, -1) and cape(x, y, 1)
            for i, wood in enumerate(WOOD):
                if is_colour(px, [wood]) and not strap and not (tunic(x, y, -1) and tunic(x, y, 1)):
                    put(bow, frame, x, y, spec["wood"][i], [])
            if is_colour(px, STRING):
                feather = any(0 <= x + dx < C and 0 <= y + dy < C and is_colour(frame[y + dy, x + dx], HAIR)
                              for dx in (-2, -1, 0, 1, 2) for dy in (-1, 0, 1))
                if head is not None:
                    feather = feather and head[1] <= y <= head[1] + 5
                if not feather:
                    put(bow, frame, x, y, spec["string"], [])
    return bow


def drawn_bow(frame, head, row):
    """Where her drawn bow, its string and the arrow are in this frame (as bow_recolour picks them)."""
    probe = bow_recolour(frame, dict(wood=[(1, 1, 1)] * 3, string=(1, 1, 1)), head, row)
    return probe[..., 3] > 0.5


def without_bow(frame, head, row):
    """Her body with the drawn bow taken out: its pixels take the colour beside them, or go when nothing of her is
    beside them; the outline is closed again."""
    out = frame.copy()
    todo = drawn_bow(frame, head, row)
    ink = np.array([[is_colour(frame[y, x], [INK]) and frame[y, x, 3] > 0.5 for x in range(C)] for y in range(C)])
    body = (frame[..., 3] > 0.5) & ~ink & ~todo
    for _ in range(3):
        filled = []
        for y, x in zip(*np.where(todo)):
            near = [(yy, xx) for yy, xx in ((y, x - 1), (y, x + 1), (y - 1, x), (y + 1, x))
                    if 0 <= yy < C and 0 <= xx < C and body[yy, xx]]
            if len(near) >= 2:
                filled.append((y, x, out[near[0]].copy()))
        for y, x, colour in filled:
            out[y, x] = colour
            body[y, x] = True
            todo[y, x] = False
    out[todo] = 0
    for y, x in zip(*np.where(ink)):          # outline that outlined only the bow
        if not body[max(0, y - 1):y + 2, max(0, x - 1):x + 2].any():
            out[y, x] = 0
    solid = out[..., 3] > 0.5
    for y, x in zip(*np.where(body)):          # and the gaps the bow left in her outline
        for yy, xx in ((y, x - 1), (y, x + 1), (y - 1, x), (y + 1, x)):
            if 0 <= yy < C and 0 <= xx < C and not solid[yy, xx]:
                out[yy, xx] = (*INK, 1)
    return out


# Where she holds a bow, by direction (s, sw, w, nw, n, ne, e, se): the grip, the way the bow's back faces, how much of
# its curve shows (1 from the side, 0 edge on), whether it is beyond her (drawn only where she is not), and whether the
# arrow on the string shows (it points at or away from the camera otherwise).
# "carry": standing and walking, in her hand; "aim": the four frames of a shot.
D = 0.447, 0.894
BOW_POSES = {
    "carry": [((3, 14), (-1, 0), 1, False, False), ((20, 14), (1, 0), 1, False, False),
              ((5, 14), (-1, 0), 1, False, False), ((4, 13), (-1, 0), 0.7, False, False),
              ((19, 13), (1, 0), 0.7, False, False), ((19, 13), (1, 0), 0.7, False, False),
              ((18, 14), (1, 0), 1, False, False), ((3, 14), (-1, 0), 1, False, False)],
    "aim": [((7, 15), (-1, 0), 0.5, False, False), ((4, 16), (-D[1], D[0]), 0.9, False, True),
            ((4, 12), (-1, 0), 1, False, True), ((3, 11), (-D[1], -D[0]), 0.9, True, True),
            ((20, 11), (1, 0), 0.5, False, False), ((20, 11), (D[1], -D[0]), 0.9, True, True),
            ((19, 12), (1, 0), 1, False, True), ((19, 16), (D[1], D[0]), 0.9, False, True)],
}
PULL = {7: 2, 8: 3}      # the string drawn back, in pixels, by the shot's frame
ARROW = rgb(206, 170, 120), rgb(236, 236, 240)


def bow_frame(frame, spec, head, row, col):
    """A bow's layer over this frame: the bow drawn at its own size where she holds it."""
    if col >= 10:
        return bow_recolour(frame, spec, head, row)
    (gx, gy), (nx, ny), shows, beyond, arrow = BOW_POSES["aim" if col >= 6 else "carry"][row]
    if head is not None and heads[row][0] is not None:
        gx, gy = gx + head[0] - heads[row][0][0], gy + head[1] - heads[row][0][1]
    ax, ay = -ny, nx
    half, depth, recurve = spec["half"], spec["depth"] * shows, spec.get("recurve", 0) * shows
    reach = half * abs(ay)                     # a long bow stays off the ground and in the picture
    gy = min(gy, 22 - reach)
    gy = max(gy, 1 + reach)
    bow = np.zeros_like(frame)
    wood = set()

    def dot(x, y, colour, into=None):
        x, y = int(round(x)), int(round(y))
        if 0 <= x < C and 0 <= y < C and not (beyond and frame[y, x, 3] > 0.5):
            bow[y, x] = (*colour, 1)
            if into is not None:
                into.add((x, y))

    def line(x0, y0, x1, y1, colour):
        steps = int(max(abs(x1 - x0), abs(y1 - y0)) * 2) + 1
        for i in range(steps + 1):
            dot(x0 + (x1 - x0) * i / steps, y0 + (y1 - y0) * i / steps, colour)

    def bulge(t):
        return -depth * t * t + recurve * max(0.0, abs(t) - 0.7) / 0.3

    def limb(t):
        return gx + ax * t * half + nx * bulge(t), gy + ay * t * half + ny * bulge(t)

    pull = PULL.get(col, 0)
    nock = gx - nx * (depth + pull), gy - ny * (depth + pull)
    for tip in (limb(-1), limb(1)):
        line(*tip, *nock, spec["string"])
    if head is not None:                       # never a string across her face
        bow[:int(head[1]) + 8][frame[:int(head[1]) + 8, :, 3] > 0.5] = 0
    if pull and arrow:
        line(*nock, gx + nx * 2, gy + ny * 2, ARROW[0])
        dot(gx + nx * 3, gy + ny * 3, ARROW[1])
    elif pull:
        dot(*nock, ARROW[1])
    light, mid, dark = spec["wood"]
    for i in range(-48, 49):
        t = i / 48
        x, y = limb(t)
        if spec.get("thick") and 0.14 <= abs(t) < 0.6:
            dot(x + nx, y + ny, mid, wood)
        dot(x, y, dark if abs(t) < 0.14 else mid if abs(t) > 0.85 else light, wood)
    for x, y in wood:                          # outlined where it stands clear of her, not between wood and string
        for xx, yy in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)):
            if not (0 <= xx < C and 0 <= yy < C) or bow[yy, xx, 3] > 0.5 or frame[yy, xx, 3] > 0.5:
                continue
            t = ((xx - gx) * ax + (yy - gy) * ay) / half
            out = (xx - gx) * nx + (yy - gy) * ny
            if abs(t) <= 1 and out < bulge(t):
                continue
            bow[yy, xx] = (*INK, 1)
    return bow


# Her body without the bow she is drawn with (the dive keeps it: the bow's layer recolours it there).
bowless = [[short[row][col] if col >= 10 else without_bow(short[row][col], heads[row][col], row) for col in range(14)]
           for row in range(8)]
armour_cells = {look: [[armour_frame(bowless[row][col], spec) for col in range(14)] for row in range(8)]
                for look, spec in ARMOUR.items()}
bow_cells = {look: [[bow_frame(bowless[row][col], spec, heads[row][col], row, col) if col < 10 else
                     bow_recolour(short[row][col], spec, heads[row][col], row) for col in range(14)]
                    for row in range(8)] for look, spec in BOWS.items()}


def looks_sheet(layers, path, cols=(0, 8)):
    """Every look in a row a line, to look at: her standing and shooting in eight directions."""
    lines = []
    for body, over in layers:
        line = []
        for col in cols:
            for row in range(8):
                cellpx = body[row][col].copy()
                if over is not None:
                    o = over[row][col]
                    cellpx[o[..., 3] > 0.5] = o[o[..., 3] > 0.5]
                line.append(cellpx)
        lines.append(np.concatenate(line, 1))
    save(np.concatenate(lines, 0), path)


looks_sheet([(armour_cells[look], None) for look in ARMOUR], os.path.join(HERE, "wild_arrow_armour_look.png"))
looks_sheet([(armour_cells["bare"], bow_cells[look]) for look in BOWS], os.path.join(HERE, "wild_arrow_bows_look.png"), cols=(0, 3, 6, 8))
looks_sheet([(short, helm_cells[look]) for look in HELMS], os.path.join(HERE, "wild_arrow_helms_look.png"), cols=(0, 11))

# Each clip's frames by column; "lie" is the death's last frame.
WALK = [2, 3, 4, 5]
SHOT = [6, 7, 8, 9]
CLIPS = {
    "idle": [0, 1], "walk": WALK, "run": WALK, "run_back": WALK, "attack": SHOT, "overdraw": SHOT, "recoil": SHOT,
    "hit": [10, 0], "death": [10, "lie"], "dive": [10, 11, 12, 13],
    "walk_aim_forward": WALK, "walk_aim_back": WALK, "walk_aim_right": WALK, "walk_aim_left": WALK,
}
for name in ("attack", "overdraw", "recoil"):
    for suffix in ("", "_back", "_left", "_right"):
        CLIPS[name + "_move" + suffix] = SHOT

# Seconds and ground speeds. The loops at the sheet's own pace (idle 350 ms a frame, walk 130); a walk's speed is where
# that pace fits her feet, scaled from the old drawing's 0.8 s walk. The one-shots keep her old times (the game
# stretches them to the action).
PACE = 0.8 / 0.52
TIMING = {"idle": (0.7, 0), "run": (0.52, 3.4), "walk": (0.52, 1.1 * PACE), "run_back": (0.52, 2.7),
          "attack": (1.033, 0), "overdraw": (3.767, 0), "recoil": (0.7, 0), "hit": (0.5, 0), "death": (3.1, 0),
          "dive": (1.633, 0), "walk_aim_forward": (0.52, 1.1 * PACE), "walk_aim_back": (0.52, 0.9 * PACE),
          "walk_aim_right": (0.52, 1.27 * PACE), "walk_aim_left": (0.52, 1.41 * PACE)}
for name in ("attack", "overdraw", "recoil"):
    for suffix in ("", "_back", "_left", "_right"):
        TIMING[name + "_move" + suffix] = (0.7, 0)

# Clips the game shows from another's sheet (AppearanceRules.SheetStandIn): their timing only.
SHARED = {"run_back", "walk_aim_back", "walk_aim_left", "walk_aim_right", "attack_move_back", "attack_move_left",
          "attack_move_right", "recoil_move", "recoil_move_back", "recoil_move_left", "recoil_move_right",
          "overdraw_move_back", "overdraw_move_left", "overdraw_move_right"}


def strip(clip, cells=None):
    """A clip's sheet from her body's frames, or from a helm's (cells)."""
    rows = []
    for row in range(8):
        of = short[row] if cells is None else cells[row]
        frames = [lying(of[0], row, short[row][0]) if col == "lie" else of[col].copy() for col in CLIPS[clip]]
        rows.append(np.concatenate(frames, 1))
    return np.repeat(np.repeat(np.concatenate(rows, 0), SCALE, 0), SCALE, 1)


os.makedirs(OUT, exist_ok=True)
for old in os.listdir(OUT):
    if old.endswith(".png"):
        os.remove(os.path.join(OUT, old))
animations = []
pictures = 0
for layer, looks in (("body", armour_cells), ("helm", helm_cells), ("weapon", bow_cells)):
    for look, cells in looks.items():
        for clip, (seconds, speed) in TIMING.items():
            name = f"{layer}_{look}_1h_{clip}"
            entry = {"name": name, "frames": len(CLIPS[clip]), "seconds": seconds, "speed": speed, "key": "1h_" + clip}
            if clip in SHARED:
                entry["shared"] = True
            else:
                save(strip(clip, cells), os.path.join(OUT, f"wild_arrow_drawn_{name}.png"))
                pictures += 1
            animations.append(entry)
with open(os.path.join(OUT, "sheet.json"), "w") as f:
    json.dump({"cellWidth": C * SCALE, "cellHeight": C * SCALE, "pivotX": C * SCALE // 2, "pivotY": SCALE // 2,
               "directions": 8, "animations": animations}, f)
print("wrote", len(animations), "animations,", pictures, "pictures")
