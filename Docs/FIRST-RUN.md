# First run of the bows build

Everything from the switch to bows onward (the Wild Arrow, Focus, the four arrow skills, stat points, pets, the 24
legendaries, the Codex, the new affixes, smart drops and the pixel art import) was written in a cloud session
**without Unity**. On 2026-10-01 it was compiled there against stand-ins of the Unity API (`Tools/CompileCheck`) and
524 of the 529 EditMode tests passed (the other five build real tilemaps and need Unity), so step 1 should go
cleanly; it has still never been run in Unity or played. This page is the order to bring it up in,
what you should see at each step, and what to send back when something is off.

Work through it top to bottom and stop at the first step that fails: later steps depend on earlier ones.

## 0. Before you start

- Commit or stash anything of your own, then pull `claude/dazzling-goodall-f16u2a`.
- Back up your save if you care about it: `Tools > ARPG > Save > Show Save Folder`. A Wrathborn save (format 11 or
  older) is set aside as `*.wrathborn-<time>` on the first load and a new Wild Arrow starts; nothing is deleted.

## 1. Compile

Open the project and let it import.

- **Expect:** the Console has no red errors (yellow warnings are fine).
- **If it fails:** copy every red error, the full text with file and line, and send it. Do not fix one and carry on;
  one mistake can cause dozens of follow-on errors, and the first few lines are the useful ones.

## 2. EditMode tests

`Tools > ARPG > Run EditMode Tests`. It writes `Logs/EditModeTests.txt`.

- **Expect:** all pass. The new test files are `FocusPoolTests`, `GripTests`, `AttributeTests`, `PetTests`,
  `LegendaryTests`, `WildArrowAffixTests` and `PixelArtImportTests`; `AffixRollerTests`, `SaveStoreTests` and
  `GameSessionTests` were changed.
- **If it fails:** send `Logs/EditModeTests.txt`.

## 3. Apply the bows change

`Tools > ARPG > Apply Bows Change (skills and stand-in bake)`. Say yes to saving the open scene.

It creates the four skill assets in `Data/Skills` (Split Arrow, Pierce Arrow, Homing Arrow, Explosive Arrow), puts
them on the Player Combat of the Sandbox and Dungeon scenes and in `Resources/ClassSkills.asset`, then bakes the
stand-in Wild Arrow (the Wrathborn's leather body with the bandit archer's clips) into `Resources/Characters/wild_arrow`.

- **Expect:** the Console logs "Wild Arrow skills created and assigned", then the bake's progress and finish. The bake
  takes a few minutes.
- **If it fails:** send the Console output. If only the bake fails, the game still runs; the character shows as the
  Wrathborn (or the placeholder capsule) until it is baked.

## 4. Play in town

Open `Scenes/Town.unity` and press Play.

| Check | Expect |
|---|---|
| The character | The baked Wild Arrow (or the Wrathborn as a fallback), idling and running in 8 directions |
| The Bag | Tabs: Inventory, Skills, Stats, Codex, Settings. A Common Bow and a Common Quiver are worn |
| Stats tab | Strength, Agility, Vitality, Speed, Focus; 0 points at level 1 |
| Codex tab | "0 of 24 legendaries found", every row "Not yet found" with its kind and build, each with an orange **DEV: give** button |
| Pet vendor | A figure at town cell (5, 1); walking into it opens the vendor sheet (Wolf 500, Raven 1500, Boar 3000 gold) |
| The blue arc | Under the feet, full (Focus) |

## 5. Fight

Take the orange **DEV: depth 3** stairs in town (they hold husks, ghouls and archers).

| Check | Expect |
|---|---|
| Basic attack | An arrow flies at the nearest enemy in sight (never through a wall), with a glow trail; it sticks in walls |
| Hits | Blood sprays on hits, white numbers, orange bigger crits |
| Focus | The arc refills over time and by 4 per arrow that hits |
| Skills | Split Arrow fires from level 1 when 2 or more enemies are ahead. Pierce Arrow (level 2), Homing Arrow (4) and Explosive Arrow (6) unlock as you level; a "NEW SKILL" callout shows |
| Pierce Arrow | Goes through every enemy on its line and drips a blood trail that splashes and dries on the floor |
| Explosive Arrow | Bursts with a flash, fire ring, embers and a scorch mark |
| Level up | 5 stat points ("Stats +5" on the tab); spending one changes the stats on the Inventory page |

## 6. Legendaries

In the Bag's Codex, press **DEV: give** on a few and equip them from the backpack. Good ones to try first, since the
effect is easy to see:

| Legendary | What to look for |
|---|---|
| Quiver of Endless Splinters | Every basic arrow splits in two on its first hit |
| Magpie's Nest | Arrows that hit a wall bounce toward an enemy |
| Ember-Tongue | Enemies hit by basic arrows show embers and take burn numbers |
| Gallowsreach (with Pierce Arrow) | Dark pools stay on the floor where the arrow passed; enemies in them bleed |
| Cinder-Stitched Jerkin (with Explosive Arrow) | The burst leaves a glowing patch that burns and slows |
| Galeheart | Once you have been moving a while (full Momentum), each shot is three arrows |

- **Expect:** each item's sheet shows its power in orange and its build; a gold **Build** tag when its power names a
  skill in your loadout. The Codex counts it as found and shows its home.
- **Also:** drop some loot. Rares and Legendaries may show the new affixes (fork chance, arrow speed, burn damage, pet
  damage, and others).

## 7. Pets

Earn gold (or play long enough) and buy the Wolf at the vendor. Make it active.

- **Expect:** it follows you, bites enemies, sometimes takes hits meant for you, and brings drops outside your pick-up
  reach to your feet. At no life it is knocked out for 20 s.

## 8. Pixel art import (optional, needs art)

Put a PNG named `floor_flagstone_1.png` (40 x 20, or 160 x 80) in `ArtSource/pixel/dungeon/` and run
`Tools > ARPG > Import Pixel Art`.

- **Expect:** `Logs/PixelArtImport.txt` lists it; the next play shows it in flagstone rooms and on the town's paths.

## What to send back

For anything that is off: which step and check, what you saw instead (a screenshot helps), and the Console's red lines.
Also worth sending even when it works: anything that feels wrong in play (arrow speed, Focus running dry, a skill
firing too often or never). Every number in this build is a first guess.
