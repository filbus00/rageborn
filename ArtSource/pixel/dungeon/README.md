# Dungeon pixel art: drop PNGs here

Put the dungeon's floor tiles, decals, walls and props here as PNGs, then run **Tools > ARPG > Import Pixel Art** in
Unity. Each file replaces the code-drawn piece it names; anything you have not made yet keeps the placeholder, so the
art can be swapped one piece at a time. The import report is `Logs/PixelArtImport.txt`.

## Names (lower case)

| File | What it replaces | Size at 1x |
|---|---|---|
| `floor_flagstone_1.png` ... `_16` | Floor variants of a room style. Styles: `flagstone`, `brick`, `earth`, `moss` (the town uses flagstone and earth). Once one variant of a style exists, only the imported ones are used for that style | 40 x 20, the 2:1 diamond touching the edge midpoints |
| `decal_<kind>_1.png` ... | A mark laid on a floor tile: `cracks`, `bones`, `blood`, `rubble`, `moss`, `skull`, `puddle`. Clear around the mark | 40 x 20 |
| `wall.png` | Every full-height wall block (pillars too) | 40 wide, any height; the footprint diamond is the bottom 20 px |
| `wall_low.png` | The walls between the camera and the player, cut low | 40 wide, about 25 tall |
| `prop_<kind>.png` | `barrel`, `crate`, `urn`, `bone_pile`, `rubble`, `broken_column`, `sarcophagus`, `brazier`, `candles` | 40 wide, any height; standing on the bottom 20 px |

Every piece stands on the cell's middle, 10 px up from its bottom edge.

## What the import does

- **Shrinks** to 40 px wide. Draw at 1x, or at 2x, 4x or any whole multiple (80, 160 px wide) for the cleanest result;
  other widths work but blur a little.
- **Cuts** floors to the diamond, so a square tile becomes a diamond.
- **Hard alpha**: anything under half see-through is gone, the rest is solid.
- **Snaps every colour** to the game's palette (about 45 colours, `PixelArt.cs`; load it into Aseprite for the best
  match). The report says how far off the colours were; over 25 means the look will change.
- **Outlines props** with a one-pixel warm near-black line. Floors, decals and walls get none.

## Rules for the art (Docs/09-art-brief.md, 0.9 and 3.1)

- Floors tile seamlessly with every neighbour, have no lighting gradient and no dark rim (the game lights them).
- Make at least 4 variants of a floor style, differing in cracks and stains, not brightness.
- Shade in 2 to 4 steps of a colour ramp; no smooth gradients, no noisy dithering over large areas.
- Check at 3x zoom with nearest-neighbour scaling: that is how the phone shows it.

The sources stay here, outside `Assets`, so Unity does not import them; the processed sprites go to
`Assets/_Project/Resources/Art/Dungeon/`. Delete or rename a source and the next import removes its sprite.
