# Builds the props in models.py and renders them for the game. Run with Blender:
#   Blender -b -P ArtSource/tools/props/render_props.py -- [names...]
# With no names it renders everything. Dungeon pieces (props, decals, walls) go to ArtSource/pixel/dungeon for
# Tools > ARPG > Import Pixel Art; the larger world pieces (stairs, chests, waypoints, town buildings) go to
# ArtSource/pixel/world with manifest.json for Tools > ARPG > Import World Art.
import sys, os, json
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import importlib
import kit
import models
importlib.reload(kit)
importlib.reload(models)

ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "pixel"))
argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
wanted = set(argv)

manifest_path = os.path.join(ROOT, "world", "manifest.json")
# The manifest lists every world piece's size and pivot (render pixels): {"items": [{"name", "width", "height",
# "pivotX", "pivotY"}]}, the shape Unity's JsonUtility reads.
manifest = {}
if os.path.exists(manifest_path):
    with open(manifest_path) as f:
        manifest = {item["name"]: item for item in json.load(f).get("items", [])}

for name, (kind, build, frame) in models.ALL.items():
    if wanted and name not in wanted and not any(name.startswith(w.rstrip("*")) for w in wanted if w.endswith("*")):
        continue
    kit.reset()
    kit._materials.clear()
    build()
    if kind == "dungeon":
        width, height, ox, oy = frame
        kit.render(os.path.join(ROOT, "dungeon", name + ".png"), width, height, ox, oy)
        print("REPORT %s %d x %d" % (name, width, height))
    else:
        width, height, ox, oy = frame
        kit.render_cropped(os.path.join(ROOT, "world", name + ".png"), width, height, ox, oy, manifest, name)

os.makedirs(os.path.dirname(manifest_path), exist_ok=True)
with open(manifest_path, "w") as f:
    items = []
    for key in sorted(manifest):
        item = dict(manifest[key])
        item["name"] = key
        items.append(item)
    json.dump({"items": items}, f, indent=1, sort_keys=True)
