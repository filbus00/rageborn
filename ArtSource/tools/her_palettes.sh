#!/bin/sh
# The Wild Arrow's palettes (2026-11-01, the owner: "I want the character to be more distinct ... for the gear to be
# noticable"; her colour crimson): from her gear palette, a base with her greens crimson and her trousers darker, and one
# per armour tier with its own metal and trims, so each tier reads at her size. The bake swaps them in by look
# (WildArrowBakeSetup.Palettes). Colours are 0-255 RGB swatches measured from the meshes' UVs.
#   sh ArtSource/tools/her_palettes.sh
cd "$(dirname "$0")/../.." || exit 1
B=/Applications/Blender.app/Contents/MacOS/Blender
SRC=Assets/_Project/Art/Models/WildArrow/wild_arrow_gear_albedo.png
OUT=Assets/_Project/Art/Models/WildArrow/Palettes
T=ArtSource/tools/recolor_swatches.py
BASE="46,70,44=128,24,28 34,48,28=104,18,24 54,72,40=150,32,34 32,48,32=96,18,22 84,76,70=54,48,46"
# Her own clothes, the hood and mask: crimson.
$B -b -P $T -- $SRC $OUT/base.png $BASE
# Brigand (tier 2): brown leather, bronze studs.
$B -b -P $T -- $SRC $OUT/brigand.png $BASE 70,15,14=78,52,30 148,70,18=176,120,50 108,24,20=150,100,45
# Scale (tier 3) and the barbute: cold blue steel.
$B -b -P $T -- $SRC $OUT/scale.png $BASE 74,69,66=70,82,96 134,128,120=150,165,180 102,96,91=96,110,126 50,46,45=44,52,62
# Plate (tier 4) and the visored helm: bright steel, a crimson tabard, gold trim.
$B -b -P $T -- $SRC $OUT/plate.png $BASE 74,69,66=100,102,110 134,128,120=170,172,182 102,96,91=134,136,146 108,24,20=196,156,62 70,15,14=110,20,26
# Knight (tier 5) and the horned helm: blackened steel, crimson cloth, bright gold.
$B -b -P $T -- $SRC $OUT/knight.png $BASE 50,46,45=34,30,34 70,15,14=110,20,26 108,24,20=218,172,72 134,128,120=210,168,84
# The hide quiver's pale feathers dimmed (a white blob on her back).
$B -b -P $T -- Assets/_Project/Art/Models/WildArrow/wild_arrow_offhand_hide_quiver_albedo.png $OUT/hide_quiver_dim.png white=150,140,120
