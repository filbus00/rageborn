# Art brief (for AI generation)

This brief is written to be handed, section by section, to an AI image, 3D or animation tool, or to an agent that drives one. Every asset has a file name, a canvas size, a pivot, a background rule and a prompt. Work through the steps in order: later steps reuse the references made in earlier ones.

The user's direction (2026-09-27): **in the style of the original Diablo 2, but not a copy.**

Sources this brief follows: `05-world-and-content.md` (setting, palette, enemy roster, art direction), `06-ui-ux.md` (HUD, screens, UI style, rarity colors), `02-classes-and-skills.md` (the Wrathborn and his skills), `03-itemization.md` (slots, materials), and the engine's fixed numbers in `CLAUDE.md` (tile size, pixels per unit, pivots).

Contents:

0. Rules for every asset
1. Style bible
2. Step 1: style frames (do these first)
3. Step 2: terrain
4. Step 3: the player character
5. Step 4: enemies
6. Step 5: the act 1 boss
7. Step 6: NPCs
8. Step 7: props and interactables
9. Step 8: visual effects
10. Step 9: items and icons
11. Step 10: UI
12. Step 11: key art, app icon, title screen
13. Delivery checklist
14. Decisions

---

## 0. Rules for every asset

### 0.1 Output format

- **File type:** PNG, 32-bit RGBA, **true transparent background** (alpha 0 outside the subject). No checkerboard pattern baked in, no white or black matte, no drop shadow unless a section asks for one.
  - If a tool cannot output transparency: generate on a flat pure magenta background (#FF00FF) with no magenta anywhere in the subject, then key it out. Anti-aliased edges must not keep a magenta fringe.
- **Colour space:** sRGB.
- **Resolution rule:** generate at **4x** the final size, then downscale with a good filter (Lanczos) to the final size. The final size is what this brief lists. Never deliver an upscaled small image.
- **No text** inside any image unless the section says so (AI text is unreliable, and all in-game text is set by the engine).
- **No signatures, watermarks, frames or borders.**

### 0.2 Engine facts the art must match

| Fact | Value |
|---|---|
| Pixels per unit | **64**. One world unit = 64 px (halved on 2026-09-28, see 0.8) |
| Floor tile | a 2:1 diamond, **64 x 32 px** |
| Camera | isometric 2:1 dimetric: looking down at **30 degrees** of elevation, rotated **45 degrees** around the vertical axis |
| Screen | iPhone portrait. The game camera shows about 7 x 15 world units, and the world is drawn at a phone's point resolution (402 x 874 on an iPhone 17) and enlarged in hard 3 x 3 pixels, Diablo 2 style (0.8). Art at 64 px a unit is seen at about 90 percent, pixel for pixel. Detail smaller than 1 px is lost; design for silhouettes and clusters of 2 to 3 px |
| Sorting | lower on screen draws in front. **Every character and prop pivot is at its feet**, where it touches the ground |
| Shadows | the engine draws a soft ellipse under every character. **Do not paint a cast shadow** into character sprites. Props may have a short contact shadow painted in |
| Lighting | the engine lights the scene with real 2D lights (a warm light around the player, dark dungeons). Paint sprites with **soft light from the top left** and **no strong coloured light**, so the engine's lights can colour them |

### 0.3 Directions

Characters face 8 directions; the player character 16 (decision of 2026-09-27, as Diablo 2 did for its heroes: with 8, a character moving between two directions shows up to 22 degrees off its path). Name and order them like this, always, in every sprite sheet:

| Row | Code | Faces (on screen) |
|---|---|---|
| 0 | S | toward the camera, straight down the screen |
| 1 | SW | down and left |
| 2 | W | left |
| 3 | NW | up and left |
| 4 | N | away from the camera, straight up the screen |
| 5 | NE | up and right |
| 6 | E | right |
| 7 | SE | down and right |

Generate all 8. Do not mirror the left side to make the right: weapons are held in the right hand, and mirroring swaps hands.

The player's 16 add a row between each pair, in the same clockwise order: S, SSW, SW, WSW, W, WNW, NW, NNW, N, NNE, NE, ENE, E, ESE, SE, SSE. With 16 rows of 128 px a sheet is 2048 px tall; the sprite bake makes these from the 3D model, so nobody draws them.

### 0.4 Sprite sheet layout

- One PNG per character per animation: `<character>_<animation>.png`.
- A grid of equal cells. **Rows are the 8 directions in the order above. Columns are frames, left to right.**
- No gaps or padding between cells. The subject is centred horizontally in every cell, with its feet at the same pixel in every cell (the pivot, listed per character).
- **No sheet larger than 4096 x 4096 px** (the texture limit the game targets). When a sheet would be larger, deliver one file per direction instead, `<character>_<animation>_<dir>.png` (for example `cinder_warden_death_sw.png`), its frames filling rows left to right and then top to bottom, as many cells per row as fit in 4096 px (16 for the boss's 256 px cells). A boss sheet longer than 16 frames is split this way.
- Frame rate: **12 frames per second** unless stated.
- The first and last frames of a looping animation must join seamlessly.

### 0.5 File names and folders

Lower case, words joined by underscores, no spaces. Deliver in these folders (they map to `Assets/_Project/Art/` in the project):

```
art/
  terrain/        tiles
  characters/     player and NPC sheets
  enemies/        enemy sheets
  bosses/         boss sheets
  props/          stairs, chests, waypoints and the like
  vfx/            effect sheets and ground decals
  items/          item icons
  ui/             panels, buttons, HUD, skill icons
  marketing/      app icon, title art
```

### 0.6 How to keep a set consistent

AI tools drift. Hold everything together this way:

1. Make the **style frames in Step 1 first**, and have the user approve them.
2. Attach the approved style frames as **image references** to every later prompt.
3. Start every prompt with the **style block** (1.5) and end it with the **negative block** (1.6).
4. For each character, make a **model sheet** (front, side, back, three-quarter) before animating. Every animation frame references it.
5. Keep one seed per asset family where the tool allows it.

### 0.7 Recommended pipeline per asset type

| Asset | Recommended route | Why |
|---|---|---|
| Characters, enemies, bosses (animated, 8 directions) | **Route A, pre-rendered 3D:** image-to-3D model from the approved model sheet, then an automatic humanoid rig, stock or AI-generated animations, then an orthographic render of every frame from the 8 fixed cameras (settings below) | This is how Diablo 2 itself was made. It is the only reliable way to get 8 matching directions and smooth frames. Direct 2D generation drifts frame to frame |
| Tiles, walls, props, icons, UI, key art | **Route B, direct 2D image generation** | Single images, no animation |
| Effects | Route B for single images; frame sequences as described in Step 8 | |

**Route A in this project: the sprite bake tool.** The Unity editor bakes a rigged model and its animations into finished sheets with every rule below (camera, scale, directions, pivot, transparency, layout, splitting, import): make a Sprite Bake Job asset and run Tools > ARPG > Sprite Bake > Bake Selected Jobs (details in `CLAUDE.md`, Sprite bake). Hand over the model as FBX (humanoid rig) and the animations as FBX clips, in place (no root motion), rather than rendered frames. The settings below are for rendering in another 3D tool instead.

**Route A render settings (Blender or any 3D tool):**

- Camera: **orthographic**. Rotation: X 60 degrees (a 30 degree look-down), Z 45 degrees plus 45 degrees per direction step: S = 45, SW = 90, W = 135, NW = 180, N = 225, NE = 270, E = 315, SE = 0. Check this so that direction S shows the model's front.
- Scale: one game tile is a ground square **0.707 m on a side, turned 45 degrees** (its diagonal is 1 m). Set the orthographic scale so that such a square renders as an exact **64 x 32 px diamond** at the final size; equivalently, 1 m measured left to right across the screen is 64 px. Check it by rendering a 0.707 m square plane once.
- Character scale: scale each model so it renders at the height its section gives (the Wrathborn about 85 px), not by real-world metres; the game's ranges are tuned to that size.
- Lighting: one soft key light from the camera's upper left, a dim fill, **no cast shadow onto the ground** (the ground plane is not rendered), ambient occlusion on.
- Output: transparent film/background, render at 4x and downscale.
- Look: a painterly or slightly gritty material and texture treatment, **not** clean plastic 3D. Post-process each frame for the hand-finished look in 1.2 if the tool allows.

### 0.8 Resolution: Diablo 2 style (2026-09-28)

The owner's decisions of 2026-09-28: the game "can drop more in resolution. It can be the same as d2", then "Do it, diablo 2 style on all and move the camera 25% closer". So:

- The whole world (floor, walls, characters, props, effects) is drawn at a low resolution, about 870 px on the long side (a phone's point resolution: 402 x 874 on an iPhone 17), and enlarged in hard, square pixels to fill the screen, as Diablo 2's 800 x 600 looks on a big monitor. The HUD, menus, item icons and text are drawn afterwards at full resolution and stay sharp (sections 10 to 12 are unchanged).
- The camera shows 15 world units top to bottom (it was 20), so one world unit is about 58 rendered pixels.
- **In-world art is made at 64 pixels per unit**, half of the brief's first version: every size in sections 3 to 9 and 15 (canvas, cell, pivot, height) was halved on 2026-09-28. At 58 px a unit it is shown at about 90 percent, close to pixel for pixel. Art at the old 128 px a unit would be shrunk more than half and waste its detail.
- Make in-world art for the look it will have: **strong silhouettes, readable value groups, and detail in clusters of 2 to 3 px**. Fine hatching, 1 px outlines on characters and small text-like marks vanish or shimmer. Generate at 4x (0.1) and downscale with a good filter, then check the result at 1:1 and at 3x with nearest-neighbour scaling, which is how the phone shows it.
- The Wrathborn is already baked this way (the sprite bake's `pixelsPerUnit` 64). Sections 13 and 14 keep their first wording where they record what was decided at the time.

---

## 1. Style bible

### 1.1 The target

The **mood and craft** of the original Diablo 2 (2000): pre-rendered, grim, gothic dark fantasy seen from a high isometric camera. Characters are small and readable, with strong silhouettes. Surfaces are worn, dirty and heavy. Colour is muted and earthy, and the few saturated colours are saved for fire, magic and loot. Shadows are deep and nearly black.

**Not a copy:** use none of Diablo 2's characters, monsters, class designs, logos, fonts, UI frame shapes (no gargoyles or angels framing the screen, no red and blue globes), item art or place names. Rageborn's own world is the Vigil and the Hollow (`05-world-and-content.md`).

### 1.2 Look in one paragraph

Pre-rendered gritty dark fantasy, like a late-90s isometric CRPG remastered at a higher resolution: soft, painted-over 3D forms, crisp readable silhouettes, weathered stone, rusted iron, scorched wood, ash-covered ground. Light comes from fire and embers only; everything else falls into cold shadow. Nothing is shiny or clean. Proportions are realistic (no chibi, no big heads, no anime).

### 1.3 Palette

Base palette (from `05-world-and-content.md`: desaturated stone, ash and rust; warm ember orange for loot, the player and the Vigil fires; cold blue and sickly green for enemies):

| Role | Colours |
|---|---|
| Stone and ground | #2B2926, #3D3934, #57514A, #756D62 |
| Ash | #8C877F, #A6A097, #BDB6AA |
| Rust and old iron | #4A2E22, #6E3B26, #8F4A2A |
| Dried blood | #4A1515, #6B1D1A |
| Ember (the player's colour: fire, loot, the Vigil) | #7A2E0E, #C2521A, #E07A20, #FFB45A, #FFE1A8 (hottest) |
| Enemy cold | #1E2A3A, #34506B, #5E7FA0 |
| Enemy sick | #2F3A1E, #56662C, #8A9A45 |
| Hollow (the corruption, magic) | #1A1224, #3B2352, #6B3FA0 |

Rules:

- About 85 percent of any image is in the stone, ash, rust and shadow ranges.
- Saturated ember orange is reserved for fire, the player's effects and loot.
- Enemies carry cold blue or sick green accents (eyes, wounds, glows) against rust and grey.
- Rarity colours are UI-only and fixed: Common #9A9A9A, Magic #4A7BD4, Rare #E0C040, Legendary #E07A20, Cursed #9B4FD0.

### 1.4 Light and value

- Painted light: soft, from the upper left, low contrast within the lit side.
- Shadow side: dark, cool, near black; keep 2 to 3 value steps of detail in it.
- Silhouettes read as a solid dark shape against a mid-grey floor. Test every character by filling it black: its pose and weapon must still read.

### 1.5 Style block (put at the start of every prompt)

> Dark gothic fantasy game art, pre-rendered isometric sprite in the style of late 1990s PC action RPGs, high isometric camera looking down at 30 degrees, rotated 45 degrees, orthographic, no perspective distortion. Gritty painterly textures, weathered stone, rusted iron, scorched wood, ash and dust. Muted desaturated earth palette of grey stone, ash and rust, with warm ember orange only on fire and magic. Soft light from the upper left, deep cool shadows. Realistic proportions, strong readable silhouette at small size. Transparent background.

### 1.6 Negative block (put at the end of every prompt)

> Not: anime, cartoon, chibi, cel shading, pixel art, low poly, glossy plastic, neon, oversaturated colours, clean or new surfaces, lens flare, bloom, depth of field, perspective camera, text, letters, logo, watermark, signature, frame, border, background scenery (unless asked), cast shadow on the ground (unless asked), Diablo characters, Diablo logos, Blizzard assets.

---

## 2. Step 1: style frames (do these first)

Three images that fix the look before any production asset. The user approves them; every later prompt uses them as references. Since 2026-09-28 (0.8) the world in a screenshot is chunky pixel art: make frames 1 and 2 at **390 x 844** (the world's own resolution) and enlarge them 3x with nearest-neighbour scaling to 1170 x 2532; the warrior stands about 95 px tall in the 390 x 844 image. Frame 3's HUD is painted at full resolution over the enlarged frame.

| # | File | Size | Content |
|---|---|---|---|
| 1 | `marketing/style_frame_dungeon.png` | 1170 x 2532 | A mock gameplay screenshot, portrait: a stone dungeon room seen from the game camera, pitch dark except a warm ember light pool around a barbarian warrior in the centre (about 95 px tall in the 390 x 844 image, see above), a pack of pale husks at the edge of the light, a rusted chest, a doorway into darkness. No UI |
| 2 | `marketing/style_frame_town.png` | 1170 x 2532 | The act 1 town at dusk: a ruined chapel, a smith's forge with fire, a standing waystone with blue runes, muddy cobbles, the same warrior. No UI |
| 3 | `marketing/style_frame_ui.png` | 1170 x 2532 | Style frame 1 with the combat HUD painted over it, following Step 10's layout: slim life bar at the top, level number, potion pips, a mini-map top right, a floating stick at the bottom, a Bag button bottom right |

Prompt for frame 1:

> [style block] Portrait mobile game screenshot at low resolution, chunky visible pixels like a late 1990s PC game, 9:19.5, isometric dark dungeon room with floor of cracked ash-grey flagstones, heavy stone walls, one broad-shouldered barbarian warrior with a rusted axe standing in the centre, a warm orange ember light around him fading to near-black at the screen edges, six pale emaciated undead husks at the edge of the light, a rusted iron-bound chest, an arched doorway into darkness. [negative block]

---

## 3. Step 2: terrain

All tiles use the 2:1 diamond: **64 px wide, 32 px tall at the floor**. Taller tiles (walls) keep the same 64 x 32 footprint at their bottom and grow upward. Tiles must tile seamlessly with every neighbour, including themselves in every position.

### 3.1 Tile technical rules

- Floor tile canvas: **64 x 32**. The diamond touches the canvas midpoints exactly: top (32, 0), right (64, 16), bottom (32, 32), left (0, 16). Outside the diamond is transparent.
- Wall tile canvas: **64 x 96**. The bottom 32 px holds the 64 x 32 footprint diamond; the wall rises 64 px above it. Pivot: bottom centre (32, 0 from the bottom) — the engine places the footprint on its cell.
- No lighting gradient across a tile (the engine lights it). No dark rim around the diamond edge (it would draw a grid).
- Every tile family has **at least 4 variants** of the same material so the floor does not repeat visibly. Variants differ in cracks, stains, debris, not in overall brightness.
- Ground detail scale: one flagstone is about 1/4 to 1/2 of a tile.

### 3.2 Act 1 dungeon (the Ashfields catacombs)

| File | Count | Size | Content |
|---|---|---|---|
| `terrain/dungeon_floor_01..08.png` | 8 | 64 x 32 | Cracked grey flagstones dusted with ash; 2 of the 8 with a little rubble, 1 with a dried blood stain, 1 with a drain grate |
| `terrain/dungeon_floor_worn_01..04.png` | 4 | 64 x 32 | Broken flagstones with packed dirt between them, for corridors |
| `terrain/dungeon_wall_block_01..04.png` | 4 | 64 x 96 | A free-standing wall block of rough grey stone masonry, mortar crumbling, soot stains at the top: this is used for every wall cell today, so it must look right next to itself on all sides and alone as a pillar |
| `terrain/dungeon_wall_low_01..04.png` | 4 | 64 x 48 | The same wall cut down to 16 px high, broken off, for walls between the camera and the player (decision 3 in section 14) |

Prompt for a floor tile:

> [style block] Single isometric floor tile, exact 2:1 diamond shape 64 by 32 pixels, top-down 30 degree view, cracked grey flagstones covered in fine ash, subtle rust-brown stains, seamless tileable edges, even flat lighting, no border, no shadow at the edges, transparent outside the diamond. [negative block]

Prompt for a wall block:

> [style block] Single isometric wall block for a tile-based game, footprint exactly one 2:1 diamond tile 64 by 32 pixels, rising 64 pixels, rough grey stone masonry, crumbling mortar, soot at the top, two visible faces (left face lit, right face in shadow) and a broken stone top, seamless where it meets identical blocks on its sides, transparent background. [negative block]

### 3.3 Act 1 town (the ruined chapel village)

| File | Count | Size | Content |
|---|---|---|---|
| `terrain/town_mud_01..06.png` | 6 | 64 x 32 | Trampled dark mud with puddles and straw |
| `terrain/town_cobble_01..06.png` | 6 | 64 x 32 | Old uneven cobbles, moss in the gaps |
| `terrain/town_grass_01..06.png` | 6 | 64 x 32 | Dead yellow-grey grass over dirt |
| `terrain/town_transition_mud_cobble_01..04.png` | 4 | 64 x 32 | Cobbles breaking up into mud, for the edges of the square |

### 3.4 Void edge

| File | Size | Content |
|---|---|---|
| `terrain/void_edge_01..04.png` | 64 x 32 | Floor crumbling into black darkness, for the outer edge of the town map |

---

## 4. Step 3: the player character, the Wrathborn

### 4.1 Who he is

A survivor who carries the last ember of the Vigil (`05-world-and-content.md`). A barbarian-style warrior (`02-classes-and-skills.md`), fighting with rage and momentum. Not Diablo 2's Barbarian: no horned helm, no bare-chested fur-and-loincloth look.

**Look:** a heavy, broad man in his forties. Scarred, shaved head, short dark beard with ash in it. Layered, mismatched salvaged armour: a dented iron pauldron on the left shoulder, a boiled-leather cuirass under a rust-red wrapped sash, chain skirt, heavy boots, bandaged forearms. A small **iron lantern-cage hangs at his belt with a glowing ember inside**: his signature, and the reason the light follows him. Weapon: a single-handed, heavy, bearded axe. **Equipped gear shows on him** (decision of 2026-09-27): his armour, helm and weapon change with what he wears, so he is built from pieces, see 4.5. The model sheet shows him in the middle armour tier (leather), bare-headed, with the middle weapon tier (the bearded axe).

Silhouette notes: wide shoulders, the lantern at the hip, the axe head always clear of the body.

### 4.2 Model sheet (make this first)

| File | Size | Content |
|---|---|---|
| `characters/wrathborn_model_sheet.png` | 2048 x 1024 | Four full-body views on a flat mid-grey background (this one image is not transparent): front, left side, back, three-quarter front. Neutral standing pose, axe in the right hand, no camera tilt. Used as the reference for Route A and for every frame |

### 4.3 Sprite size

- Cell: **128 x 128 px**.
- The character stands about **85 px tall** at the final size (about 1.3 world units), as wide as roughly half the cell at rest. This height is a lever for the sprite budget: the owner allows it to drop to Diablo 2's (roughly 80 to 50 px) if the phone measurement needs it (decided 2026-09-27, Q3; 07, Sprite memory and app size). Models are made the same either way; only the bake's target height changes.
- **Pivot: (64, 20)** measured from the cell's bottom-left, that is, centred, 20 px up. The soles of the feet touch this point in every frame. Attacks may reach into the rest of the cell but must not leave it.

### 4.4 Animations

| File | Frames | Loop | Content |
|---|---|---|---|
| `characters/wrathborn_idle.png` | 12 | yes | Breathing, weight shift, the ember in the lantern flickering |
| `characters/wrathborn_run.png` | 10 | yes | A heavy run (the game has no walk), axe low in the right hand |
| `characters/wrathborn_attack.png` | 10 | no | The basic attack: a wide horizontal axe sweep, right to left, 120 degrees. The hit lands on frame 5 |
| `characters/wrathborn_hew.png` | 12 | no | Skill Hew: a bigger, two-handed 180 degree sweep with a step in. Hit on frame 6 |
| `characters/wrathborn_hurl_axe.png` | 10 | no | Skill Hurl Axe: an overhand throw. The axe leaves the hand on frame 5 and **a second axe is drawn from the back** by frame 9 (so he is never empty-handed) |
| `characters/wrathborn_bull_rush.png` | 8 | yes | Skill Bull Rush: head down, shoulder first charge. Loops during the dash |
| `characters/wrathborn_ground_breaker.png` | 14 | no | Skill Ground Breaker: a two-handed leap-free overhead slam into the ground. Impact on frame 8 |
| `characters/wrathborn_hit.png` | 4 | no | A flinch from a hit |
| `characters/wrathborn_death.png` | 16 | no | Falls to his knees, then forward. The last frame is held as the corpse |

Size of each sheet: 8 rows x frames columns of 128 px cells (for example idle: 1536 x 1024; death, the widest, 2048 x 1024).

Later skills (not needed yet): Battle Roar, Rending Spin, Blood Frenzy, Skullsplitter.

With gear shown (4.5), each animation above is baked once per layer: the file names become `wrathborn_body_<look>_<animation>.png`, `wrathborn_helm_<look>_<animation>.png` and `wrathborn_weapon_<look>_<animation>.png`, same cells, same pivot, same frames. For Hurl Axe the weapon layer is simply empty from the throw until the new axe is drawn.

### 4.5 Equipped gear on the character

Decided 2026-09-27 (`08-production.md`): the game is gear oriented like Diablo 2, and equipped gear is displayed on the character. Shown slots: **weapon, off-hand, helm, chest armour** (the chest armour sets the torso, arms and legs). Looks: **3 tiers per slot in act 1**, and **a unique model for every legendary** (`03-itemization.md`, Appearance).

**How the pieces are built.** Soft armour that bends with the body is hard to fit onto a model as a separate AI-generated piece, while rigid things are easy. So:

| Layer | What it is | How it is made | Attached to |
|---|---|---|---|
| Body | The whole man in one chest armour look: torso, arms, legs, boots, the lantern. **Bare head, empty hands** | A full character model per chest look, generated and rigged like the first (Steps 1 and 2 of the 3D workflow). All bodies must come from the **same A-pose turnaround** with only the armour changed, and be rigged with the same Mixamo marker placement, so helms and weapons fit every body | The skeleton (Mixamo rig) |
| Helm | One helm | A rigid prop, generated like the axe (no rig) | The head bone |
| Weapon | One weapon | A rigid prop, like the axe | The right hand bone |
| Off-hand | A shield, or a one-handed weapon in the left hand (dual wield) | A rigid prop; an off-hand weapon reuses the one-handed weapon models | A shield on the left forearm bone, an off-hand weapon in the left hand bone |

**Grips** (decision of 2026-09-27): the off-hand holds nothing, an off-hand weapon or a shield, or he uses a two-handed weapon. Each grip has **its own animation set**, as in Diablo 2, so every animation in 4.4 is needed four times, one per grip, each chosen in Mixamo to suit it: `1h` (one weapon, off-hand empty), `dual` (a weapon in each hand), `shield` (weapon and shield: shield held up while running and fighting), `2h` (a two-handed weapon: both hands on the haft, heavier swings). File names carry the grip: `wrathborn_body_leather_2h_run.png`. Two-handed weapons are their own models: for act 1 three looks, `great_axe` (a long-hafted broad axe), `maul` (a heavy iron-headed maul) and `bardiche` (a long crescent-bladed pole axe), plus shields `buckler` (a small round iron-bossed wooden shield), `round_shield` (a larger round shield with a rusted rim) and `kite_shield` (a tall scorched iron-banded kite shield).

**How they are baked.** The sprite bake renders each layer on its own with the other pieces present but invisible, so they still hide what is behind them: a weapon swung behind his back comes out cut exactly where the body covers it. The game then draws body, then helm, then weapon (and off-hand) on top of each other with no per-direction draw-order table, which Diablo 2 needed. Helm, weapon and off-hand layers are baked against the middle body tier; the other bodies share its proportions, so the cut lines match closely enough at 85 px.

**What to make for act 1:**

| Piece | Looks | Notes |
|---|---|---|
| Body | `bare` (nothing equipped in the chest slot: plain shirt and trousers), `padded` (item levels 1 to 3), `leather` (4 to 6, **the approved model sheet**), `mail` (7 and up) | Make `padded` and `mail` by editing the approved A-pose turnaround (prompts below), not from scratch, so the man and his proportions stay identical |
| Helm | `cap`, `nasal`, `great` | Rigid, like the axe. The same looks as the helm icons in 10.2 |
| Weapon | `hatchet`, `bearded_axe` (**the approved axe**), `war_axe` | Rigid. The same looks as the weapon icons in 10.2 |
| Legendary pieces | One per legendary in a shown slot | Added as legendaries are designed (`03-itemization.md`, Legendary items) |

Everything keeps his signature: the ember lantern on the left hip is part of **every** body.

**Prompt for a body variant** (attach the approved A-pose turnaround as the reference image):

> Edit the reference image. Keep the same man, face, beard, build, pose, camera, lighting, background, the same bandaged forearms, the same iron lantern with the glowing ember at his left hip, the same boots. Change only his armour to: [ARMOUR]. Three views as in the reference: front, left profile, back. Bare head, empty hands. Not: helmet, weapon, different pose, different proportions, text, watermark.

- `bare`: *a plain, sleeveless, dirty linen shirt tucked into worn wool trousers, a leather belt, no armour at all*
- `padded`: *a patched, quilted grey-brown gambeson with a rope belt, no metal, a torn cloth sash, plain wool trousers*
- `mail`: *a knee-length rusted chainmail hauberk over leather, a dented iron breastplate strapped over it, iron pauldrons on both shoulders, the rust-red sash over the mail*

**Prompt for a helm** (attach the approved model sheet as the style reference):

> Helmet design sheet for the barbarian in the reference image, three views on a flat mid-grey background: front, left side, back. [HELM]. Worn, dented, darkened iron with rust, matching the reference's materials. Orthographic, even soft light, no head inside, no shadow. Not: horns, wings, ornament, glowing, clean new steel, text, watermark.

- `cap`: *a simple dented iron skullcap with a leather chin strap*
- `nasal`: *a conical iron helm with a nasal guard and a mail aventail hanging at the back and sides*
- `great`: *a closed, flat-topped great helm with breathing holes and a narrow eye slit, scorched*

Weapons use the axe prompt given earlier, with the hatchet (a small rusted hatchet with a wrapped wooden handle) and the war axe (a broad war axe with a spiked back and iron bands on the haft) in place of the bearded axe.

**Engine work this needs** (not built yet): the sprite bake's layer mode (render one piece with the others as invisible occluders), a character renderer that stacks the layers frame by frame, and a table from an equipped item (slot, tier by item level, or its legendary) to its look.

---

## 5. Step 4: enemies

### 5.1 Rules for every enemy

- Cell **128 x 128**, pivot **(64, 20)**, 8 directions, 12 fps (unless stated), same sheet layout as the player.
- Every enemy has 5 animations: `idle`, `run`, `attack`, `hit`, `death`. The death's last frame is **not** held: the engine burns the body away with a dissolve effect, so the last frame is simply the body on the ground.
- Attack frames must make the **wind-up read at a glance**: the engine gives the player a warning before an enemy hit lands (0.35 s for a husk), so the first half of the attack is a clear, exaggerated rear-back.
- **Rank variants** (the engine swaps the whole body sprite, it cannot recolour): every enemy also needs a **Champion** sheet set (the same creature, bigger build, **gold and bronze** accents, a trophy or mark of rank) and an **Elite** sheet set (**crimson** accents, corrupted, with faint Hollow-purple glow in the eyes). The engine scales Champions by 1.4 and Elites by 1.6, so they are drawn at the normal size.
- Enemy accent colours come from the enemy cold and sick ranges (1.3), never ember orange.

### 5.2 Act 1 roster

| Enemy | Role in play | Look | Size in the cell |
|---|---|---|---|
| **Husk** (`husk`) | Swarmer, packs of 6 to 12, weak | The dead of the burned lands: an emaciated, ash-grey corpse, cracked skin showing dull blue beneath, empty eyes with a faint cold-blue glow, rags, walks hunched, claws | about 70 px tall |
| **Ghoul** (`ghoul`) | Brute, slow ground slam of 2 units | A bloated, hunched, grave-eating brute, sick green-grey skin, an oversized right arm ending in a stone-hard fist, bent forward, heavy | about 88 px tall (the engine also scales it by 1.2) |
| **Bandit Archer** (`bandit_archer`) | Archer, keeps its distance and shoots | A living, desperate human: hooded, tattered brown and grey cloak, a crude shortbow, a quiver of black-fletched arrows, face wrapped in cloth | about 80 px tall |
| **Wolf** (`ash_wolf`), later | Not built yet (no archetype) | A starved grey wolf with ash in its fur, visible ribs, blue-white eyes | about 45 px tall, longer body |

Animation specifics:

| Enemy | Attack | Notes |
|---|---|---|
| Husk | 10 frames: rears back (frames 1 to 5), lunging claw swipe, hit on frame 7 | `run` is a shambling lope, 8 frames |
| Ghoul | 16 frames: raises the fist high (frames 1 to 9, slow: the slam warning is 0.9 s), slams the ground on frame 11, holds | The slam must visibly hit the ground in front of it, where the engine paints the impact circle |
| Bandit Archer | 12 frames: draws the bow (1 to 6, holds the aim), releases on frame 8 | The arrow itself is a separate effect (Step 8), **do not paint the flying arrow** into the sheet |

Files: `enemies/<enemy>_<animation>.png`, `enemies/<enemy>_champion_<animation>.png`, `enemies/<enemy>_elite_<animation>.png`. So each enemy is 15 sheets (5 animations x 3 ranks).

### 5.3 Modifier icons (small)

The engine shows up to 2 small dots over an Elite's head for its modifiers. Replace the dots with glyphs:

| File | Size | Content |
|---|---|---|
| `enemies/modifier_hasted.png` | 16 x 16 | A yellow lightning-feather glyph |
| `enemies/modifier_vampiric.png` | 16 x 16 | A green blood-drop with fangs (green on purpose: it must stand out against red enemies) |
| `enemies/modifier_frozen.png` | 16 x 16 | A cyan snowflake shard |

Flat, bold glyphs with a 1 px dark outline, readable at 12 px.

---

## 6. Step 5: the act 1 boss, the Cinder Warden

### 6.1 Who it is

The keeper of the first Vigil fire, burned hollow when the fire died (`05-world-and-content.md`). A towering armoured figure: blackened plate fused to charred flesh, cracks across the armour glowing with dying ember light, a great iron brazier-cage in place of a head with embers smouldering inside, a huge two-handed maul of cooled slag. Smoke trails from it. It is the one enemy allowed ember orange, because it **is** a dying fire.

### 6.2 Sprite size

- Cell **256 x 256** (`05-world-and-content.md`), pivot **(128, 36)**.
- It stands about **170 px tall** in the cell. (The engine currently also scales the boss by 1.6 for the placeholder; with real art the scale goes back to 1.)

### 6.3 Animations

| File | Frames | Loop | Content |
|---|---|---|---|
| `bosses/cinder_warden_idle.png` | 12 | yes | Heaving, embers drifting from the cracks |
| `bosses/cinder_warden_walk.png` | 12 | yes | A slow, heavy stride |
| `bosses/cinder_warden_slam.png` | 18 | no | The maul raised overhead (frames 1 to 11, the 1.1 s warning), slam on frame 13 |
| `bosses/cinder_warden_volley.png` | 14 | no | Throws the head-brazier's embers forward in a spray; the embers themselves are effects (Step 8) |
| `bosses/cinder_warden_charge.png` | 8 | yes | Lowered shoulder, dragging the maul, for the phase 3 charges |
| `bosses/cinder_warden_stagger.png` | 12 | yes | Staggered: bent over, maul in the ground, embers guttering |
| `bosses/cinder_warden_phase.png` | 16 | no | A roar that flares every crack bright, for the change to phases 2 and 3 |
| `bosses/cinder_warden_death.png` | 24 | no | Collapses to one knee, the fire in the head goes out, falls |

---

## 7. Step 6: NPCs

NPCs stand still in town (they do not walk), so they need only an idle animation, in the **S, SW and SE** directions (3 rows). Cell 128 x 128, pivot (64, 20), 12 frames looped.

| NPC | File | Look |
|---|---|---|
| The smith (the Forge) | `characters/npc_smith_idle.png` | A stocky old woman smith, leather apron, burned forearms, hammer on shoulder; idle is a hammer rest and a glance at the forge |
| The Wanderer (gives the Portal Tome, stands in the dungeon) | `characters/npc_wanderer_idle.png` | A tall, thin, hooded traveller in a grey road cloak, face in shadow, a staff hung with small bells and a worn book at the belt |
| Stash keeper, later | `characters/npc_stash_idle.png` | A one-eyed quartermaster with a ledger and keys |
| Class trainer, later | `characters/npc_trainer_idle.png` | A scarred old Vigil veteran with a banner pole |

---

## 8. Step 7: props and interactables

Pivot for every prop: bottom centre of its footprint, **(width / 2, 16)** unless stated. Props may have a short, soft contact shadow painted in. Transparent background.

| File | Size | Content |
|---|---|---|
| `props/stairs_down.png` | 128 x 96 | A square stone stairwell sunk into the floor, steps descending into darkness, footprint 2 x 2 tiles |
| `props/stairs_up.png` | 128 x 128 | Stone steps rising to a dark arched opening in a wall fragment, footprint 2 x 2 tiles |
| `props/chest_closed.png` | 64 x 64 | A heavy rusted iron-bound wooden chest with a big lock, 1 tile |
| `props/chest_open.png` | 64 x 64 | The same chest open and empty, lid back |
| `props/waypoint_inactive.png` | 128 x 80 | A round stone platform set in the floor, 2 tiles across, carved with a ring of runes that are dark |
| `props/waypoint_active.png` | 128 x 80 | The same with the runes glowing cold blue #5E7FA0 to #BFD9FF |
| `props/waystone.png` | 96 x 160 | The town's Waystone: a tall standing stone, worn and leaning, with blue-glowing runes, footprint 1 tile |
| `props/town_portal.png` | 8 frames, 96 x 160 each, one row | An upright oval rift of cold blue light with swirling edges, looped |
| `props/forge.png` | 128 x 128 | The smith's forge: a stone hearth with a bellows, an anvil beside it, glowing coals (ember orange). Footprint 2 x 1 tiles |
| `props/brazier.png` | 8 frames, 48 x 80, one row | An iron brazier on a tripod with a small fire, looped. Decoration for the town and the start of each level |
| `props/barrel_01..03.png` | 48 x 64 | Rotting barrels, one broken |
| `props/bones_01..04.png` | 64 x 32 | Scattered bones and skulls, flat on the floor (no height) |
| `props/rubble_01..04.png` | 64 x 48 | Piles of fallen masonry |
| `props/corpse_marker.png` | 64 x 32 | The player's own grave marker after death: a pile of the character's rusted gear with the ember lantern, dark |
| `props/chapel_ruin.png` | 384 x 384 | The ruined chapel of the act 1 town (the town's landmark): a broken bell tower and roofless nave, footprint 5 x 4 tiles. Pivot at (192, 48) |

Prompt pattern for a prop:

> [style block] Single isometric game prop on a transparent background, [description], seen from the fixed 30 degree isometric camera rotated 45 degrees, footprint [n] tiles of 64 by 32 pixel diamonds, soft short contact shadow, [palette notes]. [negative block]

---

## 9. Step 8: visual effects

Effects draw **unlit and additively or alpha-blended** on top of the scene in the engine, so they should be painted on **transparent**, bright in the middle, fading to fully transparent at the edges. Effects that lie on the ground are drawn **flat, from straight above** (the engine squashes them onto the isometric floor itself), marked "top-down" below. Effect sheets are one row of frames, 24 fps unless stated.

| File | Frames x size | Content |
|---|---|---|
| `vfx/slash_basic.png` | 6 x 128 x 128, top-down | A 120 degree arc of pale ash-white motion streaks with orange sparks at the leading edge, pointing right (the engine rotates it) |
| `vfx/slash_hew.png` | 6 x 192 x 192, top-down | The 180 degree version, thicker, more embers |
| `vfx/axe_spin.png` | 8 x 48 x 48 | The thrown axe spinning, one full turn over 8 frames, side view |
| `vfx/bull_rush_trail.png` | 6 x 128 x 64 | A dust and ash burst trailing behind a charge, pointing right |
| `vfx/ground_breaker_impact.png` | 10 x 256 x 256, top-down | A ring of cracked ground bursting outward from the centre, dust, glowing fissures, then fading |
| `vfx/hit_spark.png` | 5 x 48 x 48 | A small burst of sparks and dark blood flecks |
| `vfx/crit_spark.png` | 6 x 64 x 64 | A bigger, brighter burst, ember orange |
| `vfx/arrow.png` | 1 x 32 x 8 | A black-fletched arrow, side view, pointing right |
| `vfx/ember_projectile.png` | 6 x 32 x 32 | A flying lump of burning ember with a short trail, pointing right |
| `vfx/fire_ring_segment.png` | 8 x 128 x 64, top-down, loops at 12 fps | Low burning flames on the ground, tileable left to right, for the boss arena's burning edge |
| `vfx/ember_rain_impact.png` | 8 x 128 x 128, top-down | An ember falling and bursting on the ground |
| `vfx/level_up.png` | 12 x 128 x 192 | A column of rising ember sparks and a burst of warm light around a figure's position (no figure) |
| `vfx/potion_heal.png` | 10 x 96 x 128 | Soft rising green-white motes |
| `vfx/loot_beam.png` | 1 x 24 x 256 | A vertical shaft of light, **pure white** fading to transparent at the top and sides (the engine tints it by rarity) |
| `vfx/loot_ground.png` | 1 x 48 x 24 | A small glowing diamond mark on the ground, white (tinted by the engine) |

### 9.1 Warning shapes on the ground (telegraphs)

These are gameplay-critical: the player dodges them. They must read instantly on dark floors and must not look like decoration. Drawn top-down, **pure white on transparent** (the engine colours them red-orange and fills them over time).

| File | Size | Content |
|---|---|---|
| `vfx/telegraph_circle_edge.png` | 256 x 256 | A crisp circle outline, 6 px thick, with a jagged runic inner edge |
| `vfx/telegraph_circle_fill.png` | 256 x 256 | A soft filled disc, brighter at the rim |
| `vfx/telegraph_ring.png` | 256 x 256 | A thick ring band (inner radius 70 percent of the outer), for the fire ring |
| `vfx/telegraph_line.png` | 256 x 32 | A straight band with arrow chevrons along it, pointing right, for charges and aimed shots |

---

## 10. Step 9: items and icons

### 10.1 Rules

- Canvas **128 x 128**, transparent. The object fills about 85 percent of the canvas, **diagonal** from lower left to upper right for weapons.
- Painted like a museum-lit object: soft top-left light, dark background falloff is **not** painted (transparent).
- **No rarity colour and no glow in the icon**: the UI frames the icon in its rarity colour. Only Legendary items (later) get a unique painted icon with a subtle ember glow.
- Items are worn, used, dark: no bright or clean steel.

### 10.2 Base types needed now (3 slots exist: weapon, chest, helm)

Each icon matches the look the item shows on the character (4.5): the icon of a tier and its 3D piece are the same design.

Two to three looks per slot, picked by item level band so gear visibly improves as the player descends:

| File | Content |
|---|---|
| `items/weapon_hatchet.png` | A small rusted hatchet with a wrapped wooden handle (item levels 1 to 3) |
| `items/weapon_bearded_axe.png` | A heavy bearded axe, darkened iron, leather grip (4 to 6) |
| `items/weapon_war_axe.png` | A broad war axe with a spiked back and iron bands on the haft (7 and up) |
| `items/chest_padded.png` | A patched, quilted gambeson (1 to 3) |
| `items/chest_leather.png` | A boiled-leather cuirass with iron studs (4 to 6) |
| `items/chest_mail.png` | A rusted chainmail hauberk over leather (7 and up) |
| `items/helm_cap.png` | A dented iron skullcap (1 to 3) |
| `items/helm_nasal.png` | A nasal helm with a leather aventail (4 to 6) |
| `items/helm_great.png` | A closed great helm with breathing holes, scorched (7 and up) |

### 10.3 Later slots (not needed yet, from `03-itemization.md`)

Off-hand (shield), gloves, boots, belt, amulet, ring: 3 icons each on the same rules, when those slots are built.

### 10.4 Materials, currency and special items

| File | Content |
|---|---|
| `items/material_ash.png` | A small heap of grey ash in cupped cloth |
| `items/material_cinders.png` | A handful of glowing blue-edged cinders |
| `items/material_bloodstone.png` | A dark red, faceted stone with a gold vein |
| `items/material_soulglass.png` | A shard of smoky orange glass with a trapped flame inside |
| `items/gold.png` | A small pile of worn, dark gold coins |
| `items/portal_tome.png` | A thick, worn book with an iron clasp and a blue sigil on the cover |
| `items/potion.png` | A squat corked glass flask of red liquid (for the potion pips) |

---

## 11. Step 10: UI

### 11.1 Rules

- The UI canvas is **1170 x 2532** (an iPhone at 3x). All sizes below are **pixels at 3x**; "pt" means points (1 pt = 3 px).
- Style from `06-ui-ux.md`: **dark stone panels with thin ember-coloured strokes, angular corners, minimal gradients.** Diablo 2's mood, but flat enough to stay legible on a phone: no ornate gargoyles, skulls or chains framing the screen.
- Panels and buttons are delivered as **9-slice** images: the corners are drawn and the middle stretches. Mark the slice borders in the file name as `_9s<border>`, for example `panel_9s48.png` has 48 px corners.
- Minimum tap target 48 pt (144 px), preferred 56 pt (168 px).
- Fonts are **not generated**. Use licensed fonts: a serif display face for titles and item names (suggested: *Cinzel* or *IM Fell English*, both SIL Open Font License) and a legible sans with tabular figures for numbers (suggested: *Inter*).

### 11.2 Panels and controls

| File | Size | Content |
|---|---|---|
| `ui/panel_9s48.png` | 384 x 384 | Dark stone panel #1A1816 to #24211E, subtle carved texture, 3 px ember stroke #C2521A inset 6 px, angular (cut) corners |
| `ui/sheet_top_9s48.png` | 384 x 192 | The bottom sheet's top edge: the same panel with a drag handle notch centred |
| `ui/button_9s36.png` | 288 x 168 | A button: dark iron plate, bevelled, 2 px ember stroke |
| `ui/button_pressed_9s36.png` | 288 x 168 | Pressed: darker, stroke brighter #FFB45A |
| `ui/button_disabled_9s36.png` | 288 x 168 | Disabled: grey stroke, flatter |
| `ui/tab_9s24.png`, `ui/tab_active_9s24.png` | 240 x 120 | Forge tabs |
| `ui/backdrop.png` | 32 x 32 | Flat black, 50 percent alpha (dims the game behind a sheet) |
| `ui/item_frame_9s12.png` | 168 x 168 | The frame around an item icon: **white** stroke on dark, the engine tints it by rarity |
| `ui/tier_dot_on.png`, `ui/tier_dot_off.png` | 24 x 24 | Affix tier dots |
| `ui/arrow_up.png`, `ui/arrow_down.png`, `ui/equal.png` | 48 x 48 | The upgrade markers: green #4CC35A up, red #D0443A down, grey equal |

### 11.3 Combat HUD

Layout (top of screen is for reading only; `06-ui-ux.md`):

| Element | File | Size | Content |
|---|---|---|---|
| Life bar frame | `ui/hud_life_frame_9s18.png` | 600 x 60 | A slim iron frame, dark inside |
| Life bar fill | `ui/hud_life_fill.png` | 600 x 42 | Deep blood red #8A1C1C to #C23A2A, a faint vertical texture; stretched by the engine |
| XP bar frame and fill | `ui/hud_xp_frame_9s9.png`, `ui/hud_xp_fill.png` | 600 x 18 | Thin, gold #C9A24A fill |
| Level badge | `ui/hud_level_badge.png` | 96 x 96 | A small shield-shaped iron badge the level number is set on |
| Potion pip | `ui/hud_potion_empty.png`, `ui/hud_potion_full.png` | 60 x 60 | The flask from 10.4 in a small square socket, empty and full |
| Rage arc | `ui/hud_rage_arc.png` | 512 x 512 | A 144 degree arc of **white** texture, top-down (the engine lays it under the feet and fills it in red) |
| Stance pips | `ui/hud_stance_pip.png` | 36 x 36 | A small white diamond, tinted blue or amber by the engine |
| Mini-map frame | `ui/hud_minimap_frame.png` | 264 x 264 | A square iron frame with the corners cut, dark translucent inside |
| Boss bar frame | `ui/hud_boss_frame_9s24.png` | 960 x 96 | Wider, heavier than the life bar, small skull-free ember ornament at the centre top |
| Stick base | `ui/stick_base.png` | 384 x 384 | A worn iron ring, 50 percent opacity look, faint runes |
| Stick knob | `ui/stick_knob.png` | 168 x 168 | A darker iron disc with an ember centre |
| Bag button | `ui/button_bag.png` | 252 x 252 | A leather satchel icon on a round dark iron button |
| Portal button | `ui/button_portal.png` | 252 x 252 | The Portal Tome icon on the same button style |
| Hint banner | `ui/banner_9s36.png` | 1050 x 180 | A dark parchment-edged strip with an ember stroke |

### 11.4 Skill icons

Square, **144 x 144**, painted, framed by the engine. Each shows the action, not the character: a strong single motif, ember orange on dark.

| File | Motif |
|---|---|
| `ui/skill_hew.png` | A broad axe arc cutting across |
| `ui/skill_hurl_axe.png` | A spinning axe in flight |
| `ui/skill_bull_rush.png` | A lowered shoulder bursting through dust |
| `ui/skill_ground_breaker.png` | A fist or axe cracking the ground open |
| later: `skill_battle_roar`, `skill_rending_spin`, `skill_blood_frenzy`, `skill_skullsplitter` | |

---

## 12. Step 11: key art, app icon, title screen

| File | Size | Content |
|---|---|---|
| `marketing/app_icon.png` | 1024 x 1024, **no transparency, no rounded corners** (iOS rounds them) | The Wrathborn's ember lantern held up in a scarred fist against black, ember light on the knuckles. Must read at 60 x 60 |
| `marketing/title_background.png` | 1170 x 2532 | Portrait: the Wrathborn seen from behind, standing at the top of a stairwell descending into darkness, the ember at his hip the only warm light, ash falling. Keep the top 30 percent quiet for the game's title and the bottom 35 percent dark for the menu buttons |
| `marketing/launch_screen.png` | 1170 x 2532 | Black with a single small ember in the centre |
| `marketing/store_screenshot_frame_01..05.png` | 1290 x 2796 | Later, from real gameplay captures, not generated |

The title "RAGEBORN" is set in the engine with the display font, not painted.

---

## 13. Delivery checklist

In order. Each step waits for the user to approve the previous one.

| Step | Section | Files | Approx. count |
|---|---|---|---|
| 1 | Style frames | 3 | 3 |
| 2 | Terrain | dungeon 20, town 22, void 4 | 46 |
| 3 | Player | model sheet 1, animation sheets 9 | 10 |
| 4 | Enemies (act 1) | 3 enemies x 15 sheets, 3 modifier icons | 48 |
| 5 | Boss | 8 sheets | 8 |
| 6 | NPCs | 2 now (smith, Wanderer), 2 later | 2 |
| 7 | Props | about 25 | 25 |
| 8 | Effects and telegraphs | 15 + 4 | 19 |
| 9 | Items | 9 base types, 7 materials and specials | 16 |
| 10 | UI | about 30 panels and HUD pieces, 4 skill icons | 34 |
| 11 | Key art | 3 | 3 |

For every delivered file, also give: the prompt used, the tool and model, the seed, and the references attached, in a `prompts.md` beside the files, so any asset can be regenerated to match.

**Acceptance test for each asset** (reject and regenerate if any fails):

1. Transparent background, no fringe, exact canvas size.
2. Correct camera angle: floor tiles are exact 2:1 diamonds; a character's feet sit on the pivot in every frame.
3. All 8 directions show the same character, same colours, same gear.
4. Reads at 1:1 on a phone: the silhouette is clear at 85 px tall, in hard pixels (0.8).
5. Palette: no neon, no saturation outside fire, magic and loot.
6. Nothing that is recognisably Diablo 2's own design.

---

## 14. Decisions (the user, 2026-09-27 and 2026-09-28)

1. **Animation method: frame sprites pre-rendered from 3D** (Route A in 0.7), not skeletal 2D animation. `00-vision-and-scope.md`, `05-world-and-content.md` and `07-technical.md` now say so.
2. **Frame budget: the full set for act 1** (8 directions, 12 fps, 256 px cells, 512 for the boss), measured for memory and frame rate on a phone before anything is cut. The player's 9 sheets are 768 cells, about 190 MB uncompressed and about 50 MB compressed (ASTC 4x4).
3. **Walls: low walls on the camera side**, as Diablo 2 did. Walls on a room's camera-facing sides use the low variants (3.2), so the character is never hidden. The engine needs work to pick the low variant for those cells.
4. **Equipped gear is displayed on the character** ("The game will be gear oriented just like diablo 2. It is very important that new gear equipped is displayed on the model."). Shown slots as in Diablo 2: weapon, off-hand, helm, chest armour.
5. **Looks: tiers plus unique legendaries.** 3 tiers per shown slot in act 1, more per act, and every legendary its own model. How it is built: 4.5.
6. **Off-hand and grips:** "can in offhand hold: nothing, offhand weapon, shield, or use two hand", with **an animation set per grip** (4.5).
7. **Resolution, Diablo 2 style** (2026-09-28): "the game is quite high res, it can drop more in resolution. It can be the same as d2", then "Do it, diablo 2 style on all and move the camera 25% closer. The player really small and far away now". The world is drawn at a phone's point resolution in hard pixels and all in-world art is made at 64 px a unit; the sizes in decision 2 above are the first version's (0.8).

Memory consequence of 4 and 5: every body look is a full set of sheets (about 50 MB compressed for the Wrathborn), so the game must keep **only the equipped looks** in memory and load a look when it is equipped, never all of them at once. Helm and weapon layers are mostly empty space and pack small once trimmed. To be measured on a phone with the first real bodies.

---

## 15. Full-game asset lists (proposed, not decided)

Written 2026-09-27 for the full-game plan (`10-full-game-plan.md`). Sections 0 to 14 above are unchanged and apply to everything here: the rules, the style and negative blocks, the directions (8 for enemies, 16 for the player), 12 fps, the sheet layout, and the decided pipeline: **images, then image-to-3D, then Mixamo, then the project's sprite bake tool** (0.7, Route A) for everything animated; Route B (direct 2D) for tiles, props, icons and UI. The content (enemy names, looks, legendaries) is the proposal in 03 and 05 and changes with the owner's answers. How much of this fits in memory and on disk is open (Q3, `07-technical.md`); counts below are the full wish list before any cut.

### 15.1 The Wrathborn, remaining work

| Asset | Files | Notes |
|---|---|---|
| Later skill animations, per grip | `wrathborn_body_<look>_<grip>_battle_roar` (12 frames), `_rending_spin` (8, loop), `_blood_frenzy` (6), `_skullsplitter` (12, hit on frame 7) | Mixamo clips: a roar, a spin, a short flex, an overhead chop. Same for every helm, weapon and off-hand layer |
| Body looks, acts 2 to 5 | 12 more bodies (03, the band table: scale, brigandine, lamellar ... vigil plate) | Edit the approved A-pose turnaround as in 4.5; same man, same lantern |
| Helms, acts 2 to 5 | 12 rigid pieces | As 4.5 |
| One-handed weapons, acts 2 to 5 | 12 axes | `prepare_weapon.py` as for the bearded axe |
| Two-handed weapons | 15 (3 per act) | Act 1's great axe, maul and bardiche are in 4.5 |
| Shields | 15 (3 per act) | Left forearm bone |
| Legendary pieces | 20 (03: 5 one-handed, 4 two-handed, 3 shields, 4 helms, 4 chests) | Each its own model and icon; chest legendaries are full bodies |

### 15.2 Terrain per act

Same rules as section 3 (64 x 32 floors, 64 x 96 walls, 64 x 48 low walls, at least 4 variants each). Each act has two tilesets (05: depths 1 to 3 and 4 to 6) and a town set.

| Act | Tileset | Floors | Walls and low walls | Town ground |
|---|---|---|---|---|
| 1 | Crypts (section 3.2), burned barrows | `barrow_floor_01..08`: packed ash earth, charred roots | `barrow_wall_block_01..04`, `_low_01..04`: earth-and-timber shoring, collapsed | Section 3.3 |
| 2 | Flooded cellars, drowned halls | `cellar_floor_01..08`: wet flagstones, shallow water sheen; `hall_floor_01..08`: coral-grown stone | Rotting timber and wet stone; `water_edge_01..04` (a wall cell drawn as dark water) | `saltmere_boards_01..06`, `saltmere_mud_01..06` |
| 3 | Ossuary galleries, saint's crypts | Bone-inlaid flagstones; cracked marble with grave dirt | Stacked skull walls; marble with candle niches | `gravesend_grass_01..06` (dead orchard grass), `gravesend_path_01..06` |
| 4 | Barracks and forges, oath halls | Iron-plated floor, soot; black iron with chain grooves | Iron-banded stone; black iron with banners | `camp_mud_01..06`, `camp_planks_01..06` |
| 5 | Broken Vigil, the void | Half-dissolved stone with violet cracks; floating slabs over blackness | Crumbling ruin; jagged void rock (#1A1224 to #6B3FA0 accents) | `refuge_ash_01..06` |

Boss arenas: one floor set per act boss (`<boss>_arena_floor_01..04`), with a centre decal (256 x 128, top-down squashed by the engine) that marks the arena.

### 15.3 Enemies, acts 1 to 5

Section 5.1's rules apply (128 cells, pivot (64, 20), 8 directions, 5 animations: idle, run, attack, hit, death; a clear wind-up). Section 5.1 asks for a champion and an elite sheet set per enemy; `07-technical.md` proposes replacing those with a colour shader to save memory (Q3), in which case only the normal set is made. Behaviours and telegraphs are in 05.

| Act | File name | Look (one line; enemy accents from the cold, sick and Hollow ranges, never ember except fire casters) | Height | Attack frames (hit frame) |
|---|---|---|---|---|
| 1 | `ash_wolf` | Section 5.2 | 45, longer body | 10 (6), plus a `lunge` loop of 6 |
| 1 | `bandit_cutthroat` | A lean bandit with two knives, hood, cloth mask | 78 | 10 (4 and 7, two strikes) |
| 1 | `ember_acolyte` | A burned cultist in scorched robes holding a smoking censer; the one act 1 enemy with ember light | 80 | 14 (9, the cast) |
| 1 | `pyre_keeper` | A hunched figure carrying a brazier on its back, embers trailing | 75 | 12 (7) |
| 1 | `carrion_bloat` | A swollen corpse, skin tight and green-grey, stumbling | 75, wide | 12 (burst on death frame 8) |
| 2 | `drowned` | A pale bloated sailor, weed in the hair, cold blue eyes | 73 | 10 (7) |
| 2 | `leech_swarm` | A cluster of black fist-sized leeches, low to the ground | 25 | 8 (5) |
| 2 | `marsh_witch` | A thin old woman in wet rags, a staff of driftwood, green glow | 80 | 14 (9) |
| 2 | `drowned_watchman` | A huge armoured watchman, helmet full of water, barnacled | 93 | 16 (11) |
| 2 | `harpooner` | A fisherman with a rope harpoon, oilskin coat | 80 | 12 (8) |
| 2 | `bog_lurker` | A long, flat, eyeless amphibian | 40, long | 10 (6), plus `submerged` idle |
| 2 | `tide_priest` | A robed figure with a shell mask, water dripping upward | 83 | 12 (7) |
| 2 | `mire_hound` | A drowned hound, fur plastered, cold eyes | 45, long | 10 (6) |
| 3 | `grave_rat` | A swarm of three rats in one sprite | 20 | 8 (5) |
| 3 | `skeleton_knight` | Bones in rusted plate with a tower shield | 88 | 12 (8) |
| 3 | `bone_charger` | A skeletal horse-like beast of fused ribs with a skull ram | 75, long | 10 (6), plus `charge` loop of 6 |
| 3 | `grave_priest` | A gaunt priest with a censer of green smoke | 83 | 12 (7) |
| 3 | `ossuary_archer` | A skeleton with a bone bow | 80 | 12 (8) |
| 3 | `mourner` | A veiled woman in black, floating slightly | 80 | 14 (9) |
| 3 | `bone_weaver` | A many-armed thing of sewn bones | 85 | 14 (9) |
| 3 | `corpse_hulk` | A stitched giant of corpses | 100 | 16 (11) |
| 4 | `sworn_squire` | A young knight in black-iron half armour | 80 | 10 (7) |
| 4 | `armored_knight` | A full black-iron knight with a longsword | 88 | 16 (6 and 12, two blows) |
| 4 | `siege_brute` | A giant in siege armour with a ram-headed hammer | 105 | 18 (12) |
| 4 | `banner_bearer` | A knight with a tall black banner of the Hollow's sigil | 90 with the banner | 12 (7) |
| 4 | `crossbowman` | A heavy crossbowman with a pavise on his back | 80 | 14 (10) |
| 4 | `warhound` | An armoured war dog with a spiked collar | 48, long | 10 (6) |
| 4 | `iron_chaplain` | A priest in iron vestments with a burning book | 83 | 14 (9) |
| 4 | `oathbound_lancer` | A knight on foot with a long lance | 88 | 10 (6), plus `charge` loop of 6 |
| 5 | `hollowed` | A husk made of violet smoke and ash, faster | 73 | 10 (6) |
| 5 | `void_spawn` | A small tumbling shard-creature | 35 | 8 (5) |
| 5 | `void_wraith` | A tall thin shade trailing darkness | 88 | 10 (6), plus `blink` of 6 |
| 5 | `corrupted_watchman` | A Vigil watchman, armour cracked with violet light, bow and sword | 85 | 12 (8) bow, 10 (6) sword |
| 5 | `rift_caller` | A hooded figure holding a floating void orb | 83 | 14 (9) |
| 5 | `hollow_colossus` | A giant of fused void stone | 115 | 18 (8 and 14) |
| 5 | `whisperer` | A floating mask with ribbons of shadow | 75 | 12 (7) |
| 5 | `fallen_vigil_knight` | A Vigil knight in scorched armour, shield and sword | 90 | 16 (6 and 12) |

Modifier icons (5.3), 12 more at 16 x 16: `modifier_molten`, `_shielded`, `_teleporting`, `_splitting`, `_cursing`, `_plagued`, `_mortar`, `_fire_chains`, `_juggernaut`, `_enraged`, `_linked`, `_desecrator`, in the colours listed in 05.

### 15.4 Bosses

Section 6's rules (256 cells, pivot (128, 36), about 170 px tall, split per direction). Animations for each: `idle` 12, `walk` 12, one sheet per attack in 05's table (14 to 18 frames, the telegraph time before the impact frame), `stagger` 12 (loop), `phase` 16, `death` 24.

| Boss | File | Look |
|---|---|---|
| The Tidewife | `tidewife` | A towering drowned woman in a captain's coat, lower body a mass of tentacles and kelp, a lantern of cold blue light |
| Saint Marrow | `saint_marrow` | A skeletal saint in rotted vestments, a halo of floating bones, a crozier topped with a skull |
| Warlord Kaeth | `warlord_kaeth` | A huge knight in black iron with a tower shield and a great cleaver, the Hollow's sigil burned into his breastplate |
| The First Watchman | `first_watchman` | The first Vigil knight, armour of pale bronze half dissolved into violet void, a guttering ember in his chest |
| The Hunger Below | `hunger_below` | A room-sized worm; only its head and front body rise from the ground (idle is `submerged`) |
| The Ashen Twins | `ashen_twin_a`, `ashen_twin_b` | Two burned knights bound by a chain of fire, mirror poses |
| The Sexton of Tolls | `sexton` | A gaunt bell-ringer with a bell on a yoke, a long rope |
| The Mother of Hounds | `mother_of_hounds` | A hound the size of a horse, scarred, iron-collared |

Boss effects in section 9's style: one per attack (tentacle lash, flood, wave band, bone spikes, bone ring, cleave arc, void field, void rain).

### 15.5 NPCs

Section 7's rules (idle only, S, SW and SE, 12 frames). Adds to the smith and the Wanderer:

| NPC | File | Look |
|---|---|---|
| Stash keeper (every town, one model) | `npc_stash_idle` | Section 7 |
| Trainer (every town) | `npc_trainer_idle` | Section 7 |
| The Watcher | `npc_watcher_idle` | A blindfolded woman in a grey cloak holding an hourglass of black sand |
| Mother Aldis | `npc_aldis_idle` | An old priestess in ash-grey vestments with a cold fire bowl |
| Bram | `npc_bram_idle` | A ferryman with a pole and a wide hat |
| Sister Ivy | `npc_ivy_idle` | A young gravedigger nun with a spade |
| Captain Hale | `npc_hale_idle` | A deserter knight with his order's sigil scratched off |
| Portraits for story scenes | `portrait_<npc>.png`, 384 x 384 | Head and shoulders, painted, one per speaking NPC and one of the Wrathborn |

### 15.6 Props per act

Section 8's rules. Each act: a stairs down and up pair in its tileset's material, a chest, a waypoint, a Waystone for its town, 6 to 10 decor props, and its town buildings.

| Act | Props |
|---|---|
| 1 | Section 8 plus `shrine_plinth` (128 x 128: a stone plinth with a bowl; glows in the shrine's colour), `lore_stone` (64 x 96), `bone_pile_dormant` (for 05's ambush rooms and summoners) |
| 2 | `boat_wreck`, `net_rack`, `stilt_house` (town, 256 x 256), `watch_tower_beached` (town landmark, 384 x 384), `kelp_01..03`, `dormant_drowned` (a body in the water) |
| 3 | `sarcophagus_01..03`, `grave_candles`, `dead_tree_01..04` (town), `mausoleum` (town stash, 256 x 256), `bell_tower` (for the Sexton) |
| 4 | `siege_wagon`, `tent_01..03` (town), `weapon_rack`, `forge_cold`, `banner_black_01..02`, `spire_gate` (384 x 384) |
| 5 | `void_crystal_01..03`, `broken_vigil_fire` (the act's landmark, 256 x 256), `refuge_fire` (the town's new fire, 3 states: small, medium, great) |
| All | `inn_bed` for sleep (05, Q14), `rift_portal` (8 frames, violet), `abyss_gate` (static, 128 x 192) |

### 15.7 Effects

Section 9's rules. New: `slash_rending_spin` (8 x 192 x 192, top-down, loop), `battle_roar_ring` (8 x 256 x 256, top-down), `blood_frenzy_aura` (8 x 128 x 128, loop), `skullsplitter_impact` (8 x 128 x 128), `bleed_drip` (6 x 32 x 32), one per Caster and Support effect in 05 (fire circle, water circle, poison pool, heal ring, shield ring, banner aura, void circle, curse tether), `block_flash` (5 x 64 x 64), `stun_stars` (6 x 32 x 32, loop), a gem glint (4 x 16 x 16). Telegraph shapes (9.1) add `telegraph_cone.png` (256 x 256, 120 degree wedge) and `telegraph_half_disc.png` (256 x 128).

### 15.8 Item icons, all slots and tiers

Section 10's rules (128 x 128, no rarity colour). The icon of a shown-slot base is the same design as its look (4.5).

| Slot | Icons | Names |
|---|---|---|
| One-handed weapon | 15 | 03's band table |
| Two-handed weapon | 15 | 03 |
| Shield | 15 | 03 |
| Helm | 15 | 03 |
| Chest | 15 | 03 |
| Gloves | 5 (one per act band) | wraps, leather gloves, chain gloves, gauntlets, void-iron gauntlets |
| Boots | 5 | foot wraps, leather boots, hobnailed boots, greaves, void-iron greaves |
| Belt | 5 | rope belt, leather belt, studded belt, plated belt, chain girdle |
| Amulet | 5 | bone charm, iron pendant, saint's medal, oath seal, ember locket |
| Ring | 5 | iron band, bone ring, signet, black-iron ring, voidglass ring |
| Legendaries | 40 | 03's list; painted, with a subtle ember glow (10.1) |
| Gems | 20 (4 colours x 5 tiers) | Ruby, Sapphire, Emerald, Onyx; chipped to perfect grow in size and clarity |
| Keys and sigils | 3 | `rift_key` (a black iron key with a violet stone), `boss_sigil` (a wax seal with a skull), `abyss_token` |

Total: 100 base icons (75 for the shown slots, 25 for the others), 40 legendary icons, 23 others.

### 15.9 UI

Section 11's rules. New pieces:

| File | Size | Content |
|---|---|---|
| `ui/skill_battle_roar.png`, `_rending_spin`, `_blood_frenzy`, `_skullsplitter` | 144 x 144 | 11.4's style |
| `ui/modifier_<skill>_<n>.png` | 96 x 96 | 72 small modifier icons, or 24 (one per skill and tier) with the option's name as text (cheaper; proposed) |
| `ui/keystone_berserker.png`, `_juggernaut` | 192 x 192 | |
| `ui/tree_node_minor.png`, `_notable`, `_keystone`, `_gateway`, each `_on` and `_off` | 132 to 168 | Stone discs with ember strokes |
| `ui/tree_icon_<stat>.png` | 72 x 72 | 12 glyphs for node effects (damage, crit, life, armor, speed, dodge, Rage, Momentum, Stillness, potion, bleed, block) |
| `ui/slot_empty_<slot>.png` | 168 x 168 | 10 slot outlines for the paper doll |
| `ui/tab_<name>.png` | 120 x 120 | Gear, Skills, Tree, Stats, Filter, Journal, Settings |
| `ui/tier_badge_1..5.png` | 72 x 72 | Vigil tier numerals as carved stone (the engine sets the digits; these are the plates) |
| `ui/story_panel_9s36.png` | 1170 x 540 | The story scene panel |
| `marketing/act_<n>_title.png` | 1170 x 2532 | One painted title card per act for the act's opening scene |

### 15.10 Delivery order (proposed)

Per act, the same order as section 13: style check against the approved frames, terrain, the act's enemies, its boss, its NPCs, props, effects, then icons and UI for any new system. Acts in order 2, 3, 4, 5, with the Wrathborn's looks for an act made together with that act. Estimated counts per act: about 50 terrain files, 8 enemies, 1 boss (plus the optional boss that lives there), 1 or 2 NPCs, 15 props, 10 effects, 25 item icons, 9 looks.
