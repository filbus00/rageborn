# Gemini prompt: inventory item icons

Paste the style block first (with the two reference images), then one item line at a time. One image per item.
Pixel art in the game's current style (the owner, 2026-10-19).
The aspect ratio after each item is the shape of its block in the backpack, so the picture fills its tiles.

## Style block (paste once, at the start of the chat)

Upload `style-reference-game.png` (the game) and `style-reference-bag.png` (today's simple icons, to be replaced)
with it.

```
You are making inventory icons for a dark fantasy action RPG. They must be pixel art in the game's own style: look at
the two images I attached (a scene from the game, and its current placeholder icons, which these will replace). I
will give you one item at a time. For every item:

- Pixel art, matching the game: hard-edged square pixels, no anti-aliasing, no blur, no soft gradients, no painterly
  or 3D-rendered look. A one-pixel near-black outline (#140E0C) round the whole item.
- Drawn at a low native resolution of 24 pixels per inventory cell (so a 2 x 4 cell bow is 48 x 96 pixels, a ring
  24 x 24), then scaled up 8 times with nearest neighbour so every pixel stays a crisp square block.
- A small, muted, gritty palette like the game's: dark iron and steel greys, worn browns of leather and wood, bone,
  tarnished brass and gold, deep blood red; a few shades per material, clear light and dark sides. Colours slightly
  desaturated, never bright or candy-like.
- Light from the upper left: a one-pixel highlight on the upper-left edges, darker shades on the lower right.
- A single item, alone, centred, filling about 85 percent of the canvas, nothing cut off at the edges.
- Shown flat-on, upright, the way it would lie in an inventory grid. Bows stand vertically with the grip in the
  middle; quivers stand upright with the arrow fletchings at the top.
- Background: one flat, solid, pure magenta (#FF00FF), with no shadow, floor or texture, so it can be cut out.
  The item itself must contain no magenta.
- No text, no letters, no frame, no border, no UI, no hands or characters.
- Keep the same pixel scale, outline and lighting across all items, so they look like one set.
- Output in the aspect ratio I give.

Reply only with the image.
```

## Items

Format: file name, what to draw, aspect ratio (width:height).

### Bows (2 x 4 cells, 1:2)

- `hunting_bow`: a plain short hunting bow of dark wood, simple leather grip, waxed string. 1:2
- `recurve_bow`: a short recurve bow, the limb tips curling forward, horn nocks, wrapped grip. 1:2
- `horn_bow`: a compact composite bow of laminated horn and sinew, pale ivory and dark brown layers, bone nocks. 1:2
- `yew_longbow`: a tall English yew longbow, pale sapwood face and dark heartwood back, simple grip. 1:2
- `war_bow`: a heavy war longbow, thick limbs bound with iron rings at the tips, worn leather grip. 1:2
- `great_bow`: a huge, menacing great bow of black wood reinforced with iron plates and spikes at the limb tips. 1:2

### Quivers (2 x 3 cells, 2:3)

- `hide_quiver`: a simple rawhide quiver with a fur trim and a shoulder strap, grey-fletched arrows. 2:3
- `studded_quiver`: a stiff dark leather quiver with iron studs and a brass rim, red-fletched arrows. 2:3
- `bone_quiver`: a quiver bound with ribs and small bones, a skull ornament, black-fletched arrows. 2:3

### Chest armour (2 x 3 cells, 2:3)

- `ranger`: a ranger's long hooded coat of green-grey wool and leather, belted. 2:3
- `brigand`: a brigandine jerkin of dark leather riveted with small iron plates. 2:3
- `scale`: a shirt of overlapping iron scales on a leather backing, short sleeves. 2:3
- `plate`: a dented iron breastplate with pauldrons, leather straps. 2:3
- `knight`: a full knight's cuirass, fluted dark steel with gold trim and a tattered red tabard. 2:3

### Helms (2 x 2 cells, 1:1)

- `hood`: a deep cloth archer's hood, dark green-grey, frayed edges. 1:1
- `mask`: a leather face mask with eye holes and a laced back. 1:1
- `barbute`: an iron barbute helm with a T-shaped face opening. 1:1
- `visored`: a closed helm with a pointed, slitted visor, dark steel. 1:1
- `horned`: a menacing full helm with two curved horns, blackened iron. 1:1

### Gloves (2 x 2 cells, 1:1)

- `wraps`: cloth hand wraps, a pair, stained. 1:1
- `gloves`: a pair of plain brown leather gloves with cuffs. 1:1
- `bracers`: a pair of hard leather archer's bracers with fingerless gloves. 1:1
- `gauntlets`: a pair of jointed iron gauntlets. 1:1
- `claws`: a pair of dark iron gauntlets ending in curved claws. 1:1

### Boots (2 x 2 cells, 1:1)

- `wrapped`: cloth-wrapped boots tied with cords, a pair. 1:1
- `leather_boots`: a pair of plain worn leather boots. 1:1
- `strapped`: a pair of tall boots with many buckled straps. 1:1
- `sabatons`: a pair of armoured iron sabatons over leather. 1:1
- `spiked`: a pair of heavy iron greaves with spikes on the knees and toes. 1:1

### Belts (2 x 1 cells, 2:1)

- `rope`: a knotted rope belt. 2:1
- `pouch_belt`: a leather belt with two small pouches. 2:1
- `studded`: a wide leather belt studded with iron, square buckle. 2:1
- `tassets`: a belt with hanging armoured plates (tassets). 2:1
- `war_girdle`: a heavy war girdle of iron plates with a skull buckle. 2:1

### Rings (1 x 1 cell, 1:1)

- `iron_ring`: a plain iron ring. 1:1
- `copper_ring`: a copper ring with a green patina. 1:1
- `silver_ring`: a tarnished silver ring with an engraved band. 1:1
- `gold_ring`: a heavy gold signet ring. 1:1
- `jewelled_ring`: a gold ring set with a dark red gem. 1:1

### Amulets (1 x 1 cell, 1:1)

- `bone_charm`: a carved bone charm on a leather cord. 1:1
- `copper_amulet`: a round copper amulet on a chain. 1:1
- `silver_amulet`: a silver amulet with a crescent shape on a chain. 1:1
- `gold_amulet`: an ornate gold amulet on a chain. 1:1
- `jewelled_amulet`: a gold amulet with a large glowing blue gem on a chain. 1:1

### Blueprint (1 x 1 cell, 1:1)

- `blueprint`: a rolled, scorched parchment scroll tied with a red cord, a faint drawing of a bow visible. 1:1

### Legendaries (named, unique; add "legendary, unique, with a faint golden glow" to each)

- `splinterbough` (bow, 1:2): a short bow grown from a splintered, cracked branch, slivers of wood bristling along the limbs.
- `ember_tongue` (bow, 1:2): a short bow of charred black wood with glowing ember cracks, the string glowing orange.
- `hunters_promise` (bow, 1:2): an elegant short bow of pale wood wrapped in green cloth, a carved hawk at the grip.
- `galeheart` (bow, 1:2): a sleek pale bow with feathered limb tips and wind-blue cloth ribbons.
- `widows_draw` (bow, 1:2): a black lacquered bow with a black lace wrap and a small silver spider at the grip.
- `gallowsreach` (longbow, 1:2): a tall bow of grey gallows wood, a noose of rope coiled round the grip, iron nails in the limbs.
- `stillwater_yew` (longbow, 1:2): a calm, perfect yew longbow with blue-grey water-like grain, silver tips.
- `the_long_silence` (longbow, 1:2): a very tall black longbow with bone tips and a cloth gag wrapped round the grip.
- `quiver_of_endless_splinters` (quiver, 2:3): a quiver packed with jagged, splintered arrows, wood shards sticking out.
- `ashfall_quiver` (quiver, 2:3): a charred quiver smouldering with embers and ash, arrows with glowing tips.
- `wind_sworn_quiver` (quiver, 2:3): a pale leather quiver with white feathers and blue cords fluttering.
- `magpies_nest` (quiver, 2:3): a quiver woven like a bird's nest of twigs, black and white feathers, a glint of stolen trinkets.
- `quiver_of_the_hollow_hound` (quiver, 2:3): a quiver of grey wolf fur with a hollow-eyed hound skull on the front.
- `falconers_hood` (helm, 1:1): a leather falconer's hood with brass fittings and a tuft of hawk feathers.
- `unblinking_crown` (helm, 1:1): a dark iron crown with one large open eye in the brow.
- `stag_hide` (chest, 2:3): a coat of tanned stag hide with antler toggles and fur at the collar.
- `cinder_jerkin` (chest, 2:3): a leather jerkin stitched with glowing ember-red thread, scorched edges.
- `fletcher` (gloves, 1:1): fletcher's fingerless gloves with feathers and small tools tucked into the cuffs.
- `bloodletter` (gloves, 1:1): dark red leather grips with small blades on the knuckles, blood-stained.
- `windrunner` (boots, 1:1): light pale boots with feathered ankles and blue wind ribbons.
- `stalker` (boots, 1:1): soft dark hunter's boots with fur lining, silent and worn.
- `bandolier` (belt, 2:1): a bandolier belt hung with small dried heads and trophies.
- `pack_leaders_signet` (ring, 1:1): a silver signet ring with a wolf's head.
- `eye_of_the_storm` (amulet, 1:1): an amulet holding a swirling storm-grey orb with a lightning spark inside.
