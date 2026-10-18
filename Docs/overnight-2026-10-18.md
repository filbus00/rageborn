# Overnight, 2026-10-18

The branch `overnight-2026-10-18`, not merged. Nothing was run in Unity: everything below was compiled and tested with the offline compile check only. No design, balance number, art, bake, project setting or save format was changed.

## Fixed

### Build warnings

- **CS0618, `FindObjectsSortMode` obsolete**: `Core/LightingDirector.cs` (2 calls), `Core/DeviceBenchmark.cs` (3), `Core/DevAutopilot.cs` (1) now call `FindObjectsByType<T>()` without a sort mode. These were the only calls with a sort mode in Scripts. None of them needed an order: LightingDirector sets every global light's colour and paints the tilemap named "Ground"; DeviceBenchmark only counts; DevAutopilot picks a loot drop by its own rule. The stubs already had the overloads.
- **UAC0009, `DEVELOPMENT_BUILD` deprecated**: `Audio/AudioCapture.cs`, `Core/DevAutopilot.cs` and `Audio/Sfx.cs` (2 places) now use `#if UNITY_EDITOR || UNITY_INCLUDE_INSTRUMENTATION`. Unity 6.6's manual (Managed code variants) says a build with the Release managed code variant defines none of the new symbols, and that the Development Build option "still defines UNITY_ASSERTIONS and UNITY_INCLUDE_INSTRUMENTATION" (it adds that this "is subject to change"). The project's `managedCodeVariant` is empty (the default). So, as now: the dev tools are in the editor and in development builds, and out of release builds. The trade-off: if a later Unity stops defining `UNITY_INCLUDE_INSTRUMENTATION` for development builds, the autopilot and the audio capture drop out of development builds (they would never leak into a release build; the failure is the safe way). If a release build is ever made with the Checked or Debug variant, they would be in it. The compile check's `.csproj` files define the new symbol too.
- **UAC1001, `CharacterPortrait.Driver.Owner` skipped by serialization**: the field is now `[System.NonSerialized]`. It is set from code right after `AddComponent`, so nothing changes.

### Code review

- **A landed telegraph kept pulsing** (`Enemies/Boss/GroundMarker.cs`). The new pulse ran whenever `t >= 0.75`, also after the attack had landed. `BossFight.LastingZone` keeps advancing its marker for the zone's whole life, so every lasting boss zone (the Tidewife's, Saint Marrow's and others) flickered at about 4 Hz as if about to land the whole time it was dangerous. Markers that a boss keeps until its attack ends (`Track`, cleared by `ClearAttackMarkers`) did the same after landing. Now a marker that is `Done` shows its own colour again. Elite beams and chains (which call `Advance(1f)` every frame on a 0.01 s marker) now show their plain colour, not 98 percent alpha. Rings have no fill and still never pulse.
- **Shock rings drew one frame wrong** (`Enemies/EnemyHazards.cs`). A shock made during `Tick` is first sized on the next frame, so a new one drew for a frame one unit across at full colour, a reused one at its last size. It is now sized and coloured when it starts.
- **Per-frame allocations** (`Enemies/EnemySpecials.cs`, `Enemies/EliteAffixes.cs`). Both ran `foreach` over `EnemyManager.Active`, an `IReadOnlyList`, which boxes its enumerator every frame; both now loop by index. `EnemySpecials` also read `Definition.name` for every awake enemy every frame (`Object.name` makes a new string on each read) and matched it against 27 table rows; the special is now looked up once per definition and kept.
- `TelegraphArt.Bubbles` built a new array of spots for every one of its 16,384 pixels when the sprite was made (once). The array is now made once.

### Tests added

`Tests/EditMode/RogueliteEdgeTests.cs`, 9 tests of today's behaviour (none found a bug): the boon offer is never longer than what is open, an offer of 0 is empty, the same seed gives the same offer, a maxed boon is not offered again; Thunder Arrow's count at 0, below and at multiples, the prey on an empty list and on a tie; the blade's bounce in an open spot (straight back), in a corner (both ways), off a wall on one side (the open side kept) and at zero speed; Garrett's prices for nothing, a blueprint, item level 0, and the roguelite factor; every upgrade's cost rising each rank in whole tens, stopping at its last rank, and a saved rank above the last capped; blueprint keys that do not parse, no roll when every kind is known, the forge cost and level; the run summary's durations (with hours), the won and portal titles, the bosses line, no blueprint line when none were found.

### Tools

`Tools/BalanceModel` (not in the game, not in Assets): a console program on the game's pure formulas that prints the table below. Run it with `DOTNET_ROLL_FORWARD=Major dotnet run --project Tools/BalanceModel/BalanceModel.csproj -p:TF=net8.0`.

## Found, not fixed

- **The one-frame error "More than one global light on layer ... for light blend style index 0" during the iOS build.** Town, Dungeon and Sandbox each hold exactly one Global Light 2D (`m_LightType: 4`), no code creates a global light (`LightingDirector` only recolours the scene's own, and `WorldLights.Add` makes point lights), and nothing global is kept across scenes. The only other global light is in `Assets/Settings/Scenes/URP2DSceneTemplate.unity`, a scene template not in the build. Docs/11 already notes the same: it shows "whenever the editor has both scenes open at once". So the likely cause is the build processing one scene while the editor still has the other (or the template) loaded; in the game, scenes load one at a time and there is only one global light. I did not change a scene: the fix, if wanted, needs Unity to check. Two things to try on the Mac: build with only an empty scene open (File > New Scene before Build iOS), and see if the error goes; if it does, `IOSBuild.Build` could open a new empty scene first (`EditorSceneManager.NewScene(NewSceneSetup.EmptyScene)`, after asking to save).
- **The bouncing blade turns straight back off most walls.** `ProjectileKinds.Bounce` flips the ground x or y of the velocity on the side that is blocked. Cells are diamonds, so the dungeon's walls run at 45 degrees to ground x and y, and a blade striking a wall along a cell edge is usually blocked on both probes and flies back the way it came, not off at the mirror angle. It still bounces three times; whether a mirror bounce is wanted is the owner's call (probing along the cell lattice, (1, 1) and (1, -1), would give it). The new tests pin today's behaviour.
- `EnemySpecials`' pending throws hold an `EnemyController`. If that enemy dies and the pool hands the same controller to a new enemy before the throw is released (0.35 s plus the fill), the new enemy throws it. Unlikely and harmless; left alone.
- The Forge's reroll and temper (`ForgeRules`) call `AffixRoller.RollValue` and `ValueRange` without an item level, so a reforged flat affix would roll at the full level 60 range. The Forge is switched off (`Features.Forge`), so it cannot happen now; worth fixing if the Forge comes back.
- `QuestObjectives.EnemyNear` also loops `foreach` over `manager.Active` (boxing); quests are off, so it does not run.
- CLAUDE.md still describes cover ("beside a wall or prop she is in cover ... ambush shot", `CoverShield`) under Combat, though cover was removed on 2026-10-17 (Docs/08). Not edited, since the owner keeps CLAUDE.md.
- Also checked and fine: `ProjectileArt`, `RangedRules`, `AimCone` (three cached arc sprites, statics reset), `PlayerCombat.Boons` (deaths handled after the shots, the kill hook removed on destroy, prey checked with Unity's null), `SheetKit`, `BoonListScreen` (lists the same nodes `BoonRules.CanTake` offers), `TownSheets`, `PetVendorScreen`, `CodexScreen` (the DEV give button is behind `Debug.isDebugBuild`), `AffixRoller` (every drop path passes the item level). Every boss fight works with the new markers: circles and lines are advanced until `Done` and then cleared or hidden; the two arena rings (`GroundMarker.Ring`) have no fill and do not pulse.

## Needs Unity to check

1. **Compile and tests in Unity**: `Tools > ARPG > Run EditMode Tests`, then `Logs/EditModeTests.txt` (expect every test to pass, the 5 NavGrid tests included). Check that the iOS build no longer logs CS0618, UAC0009 or UAC1001.
2. **Dev tools still in a development build**: build `Tools > ARPG > Build iOS Simulator`, and on the simulator check the PerfOverlay shows and that `DevAutopilot` and `AudioCapture` are present (for example: start the autopilot in the editor; and in the Xcode project's `Il2CppOutputProject`, search for `DevAutopilot`). Then a release build (Development Build off) should not contain `DevAutopilot`.
3. **The balance probe** (Docs/11): with the Town scene open, through the Unity MCP bridge (`Unity_RunCommand`): `UnityEditor.EditorPrefs.SetBool("ARPG.AutopilotProbe", true); UnityEditor.EditorApplication.EnterPlaymode();`. It plays a fresh run at depths 3, 9, 15, 21, 27 and 33 (skipped boons at random, Magic gear of the depth, no potions, 150 s each) and writes `Logs/BalanceProbe.md`. Copy the old report to `Logs/BalanceProbe.before.md` first, to compare hits taken, time alive and kills.
4. **Every boss**, by the DEV path (deep entrances, or the autopilot): the Cinder Warden (6), the Tidewife (12), the Gaoler (18), the Hanged Magistrate (24), Saint Marrow (30), the Ash Archivist (36), the Rift Mother (42), the First Watchman (48). Watch: circles fill and pulse only in their last quarter and stop pulsing when they land; lanes fill from the boss outward along their length (`LaneFill`'s pivot at its left edge, placed at -0.5 inside the line); lasting zones hold a steady colour while they hurt; the Warden's fire ring and Saint Marrow's ring and the First Watchman's edge never pulse.
5. **Elites**: Arcane Beam and Fire Chains draw steady (no flicker) while active; Lightning Lances pulse before they strike.
6. **Allocations**: in the Profiler (Deep Profile off), a dungeon fight with elites and specials should show no GC Alloc from `EnemySpecials.Tick` or `EliteAffixes.Tick`. In the editor, measure with `GC.GetTotalMemory(false)` over thousands of frames (CLAUDE.md: `GetAllocatedBytesForCurrentThread` is 0 in the editor).
7. **Frame rate and memory on the phone**: the DeviceBenchmark (`benchmark.flag` in the app's Documents, Release configuration) at a deep level with many projectiles and pools; compare `benchmark.txt` with the last run.
8. **Shock rings**: no one-frame white disc where an attack lands.

## Offline balance model

From `Tools/BalanceModel`. A model, not a measurement. Assumptions:

- A normal enemy of the depth's enemy level (`DungeonRules.EnemyLevel`): life `CombatFormulas.EnemyLife` (8 x 1.2 x level^1.6), its hit `EnemyHitDamage` (4.5 x level^1.45) with a damage multiplier of 1, armour 0 (every enemy definition has 0 until tuned). Champions, elites and each type's own multipliers are left out. Projectiles hit the same (`ProjectileDamage` 1); ranged enemies attack twice as often (`QuickerRanged` 2) and everyone walks at 0.9, which change how often she is hit, not how hard; packs are 8 to 15.
- Her level is the depth's enemy level (`BoonRules.LevelAt`); her life `CharacterLife(level)` plus gear life; no meta upgrades, no boons, no pet, no dodge.
- Starting kit: the Common level 1 short bow and quiver (`EquipmentState.Starting`), kept at every depth (the roguelite does not level the kit).
- Magic gear: a short bow, a quiver and all eight worn pieces Magic at the depth's enemy level, with affixes rolled by `AffixRoller` (the new item-level scale included) and averaged over 3,000 sets.
- Damage per arrow: `CombatFormulas.HitDamage` with skill multiplier 1 (the basic arrow), crits averaged in; far and point blank affixes and the legendaries left out.
- Arrows to kill and hits to kill her are rounded up.

| Depth | Enemy level | Enemy life | Enemy hit | Her level | Her life (kit / Magic) | Arrow, kit | Arrow, Magic | Arrows to kill (kit / Magic) | Basic attacks a second (kit / Magic) | Enemy hits to kill her (kit / Magic) |
|---|---|---|---|---|---|---|---|---|---|---|
| 1 | 1 | 10 | 4.5 | 1 | 100 / 110 | 8.6 | 13.4 | 2 / 1 | 1.47 / 1.50 | 23 / 27 |
| 3 | 3 | 56 | 22.1 | 3 | 268 / 282 | 8.6 | 24.1 | 7 / 3 | 1.47 / 1.50 | 13 / 15 |
| 6 | 7 | 216 | 75.6 | 7 | 604 / 624 | 8.6 | 53.1 | 26 / 5 | 1.47 / 1.55 | 8 / 10 |
| 12 | 11 | 445 | 145.6 | 11 | 940 / 967 | 8.6 | 87.5 | 52 / 6 | 1.47 / 1.56 | 7 / 8 |
| 18 | 15 | 731 | 228.3 | 15 | 1276 / 1320 | 8.6 | 133.6 | 86 / 6 | 1.47 / 1.56 | 6 / 7 |
| 24 | 18 | 979 | 297.4 | 18 | 1528 / 1579 | 8.6 | 166.7 | 114 / 6 | 1.47 / 1.56 | 6 / 7 |
| 36 | 25 | 1656 | 478.9 | 25 | 2116 / 2182 | 8.6 | 250.4 | 193 / 7 | 1.47 / 1.56 | 5 / 6 |
| 48 | 33 | 2582 | 716.2 | 33 | 2788 / 2892 | 8.6 | 371.5 | 301 / 7 | 1.47 / 1.57 | 4 / 5 |

What it shows, in plain numbers: with Magic gear of the depth a normal enemy takes 5 to 7 arrows from depth 6 down, so her killing speed holds steady; with only the starting kit it climbs from 2 arrows at depth 1 to 26 at depth 6 and 300 at 48, so gear that keeps up is required, and the 10 percent drop rate decides how fast it comes. Her life against a normal enemy's hit falls from about 25 hits at depth 1 to 5 at depth 48 (armour on Magic gear adds about one hit). Most of the Magic arrow's damage is the bow's own base damage at its item level; the affixes add a fifth to a quarter.

Boon multipliers on her basic attack's damage, every arrow finding a target (as against a pack; against one enemy at range Extra Arrow's copies, fanned 7 degrees apart, mostly miss):

| Boon | Rank 1 | Rank 2 | Rank 3 |
|---|---|---|---|
| Extra Arrow (+1 arrow a rank) | 2x | 3x | 4x |
| Extra Shot (the bow looses again a rank) | 2x | 3x | 4x |
| Extra Arrow and Extra Shot together | 4x | 9x | 16x |

Thunder Arrow (every 8th basic attack, 0.8 of the arrow to every other enemy within 2.5). Extra Arrow's copies are copies of the first arrow and carry the thunder too, so it multiplies on top of Extra Arrow by the same amount:

| Other enemies within reach | Multiplier |
|---|---|
| 0 | 1.00x |
| 3 | 1.30x |
| 6 | 1.60x |
| 10 | 2.00x |

(Extra Shot's later loosings do not carry the thunder: only the attack's first arrow and its Extra Arrow copies do.)

## Compile check

`DOTNET_ROLL_FORWARD=Major Tools/CompileCheck/run.sh` after the last change: the build succeeded (Runtime, Editor and tests, as .NET Standard 2.1); 672 tests passed, 5 failed, 0 skipped. The 5 failures are the NavGridBakerTests that always fail offline (the stand-in Tilemap).

## Also on the branch

`Docs/review-list-2026-10-18.md`: every Docs/08 row still marked "Claude, to review", one line each, grouped by topic, newest first, with notes where a row no longer matches the game.
