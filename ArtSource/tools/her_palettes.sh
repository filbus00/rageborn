#!/bin/sh
# The Wild Arrow's palettes (2026-11-01, the owner: "I want the character to be more distinct ... for the gear to be
# noticable"; her colour crimson): from her gear palette, a base with her greens crimson and her trousers darker, and one
# per armour tier with its own metal and trims, so each tier reads at her size. The bake swaps them in by look
# (WildArrowBakeSetup.Palettes). Colours are 0-255 RGB swatches measured from the meshes' UVs.
#   sh ArtSource/tools/her_palettes.sh
cd "$(dirname "$0")/../.." || exit 1
B=/Applications/Blender.app/Contents/MacOS/Blender
OUT=Assets/_Project/Art/Models/WildArrow/Palettes
T=ArtSource/tools/recolor_swatches.py
# The tier palettes (base, brigand, scale, plate, knight) were dropped on 2026-11-02: her new model wears crimson in its
# own texture, and the gear rebuilt after the owner's sheets carries each tier's colours itself.
# The hide quiver's pale feathers dimmed (a white blob on her back).
$B -b -P $T -- Assets/_Project/Art/Models/WildArrow/wild_arrow_offhand_hide_quiver_albedo.png $OUT/hide_quiver_dim.png white=150,140,120
