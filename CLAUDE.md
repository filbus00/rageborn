# Project overview

Rageborn: isometric Diablo-style action RPG for iOS. Unity 6000.6.2f1, URP 2D renderer, new Input System only (`activeInputHandler: 1`, so never use `UnityEngine.Input`).

The design docs in `Docs/` are the source of truth for design decisions. Start at `Docs/00-vision-and-scope.md`. If code and docs disagree, ask before changing either.

- 2D isometric sprites: Isometric `Grid`/`Tilemap` for the world, sprite characters.
- Portrait only, iPhone only (no iPad in 1.0), iOS 16+, target 60 fps (`GameBootstrap`). No online features in 1.0 (no Game Center or leaderboards); iCloud save sync only.
- One-thumb control: a floating stick is the only input. Attacks and skills fire automatically, so there are no skill buttons, no tap-to-move and no dodge button.
- Company Filbus Software, bundle ID `com.filipbusic.rageborn`.
- Diablo 1 structure, not a survivor game: the player starts in a safe walkable town and descends a dungeon level by level by stairs. Enemies are packs placed in rooms that idle until aggro, and they never spawn around the player. See `Docs/00-vision-and-scope.md` and `Docs/05-world-and-content.md`.

# Layout

- `Assets/_Project/` holds all game content. `Assets/Settings/` (URP and input assets) comes from the Unity template.
- `Scripts/Runtime` is the `ARPG.Runtime` assembly (namespace `ARPG`), `Scripts/Editor` is `ARPG.Editor`.
- Physics/sorting layer names live in `GameLayers.cs`. `Tools > ARPG > Apply Project Setup` (`ProjectSetup.cs`) creates them in TagManager and applies iOS, identity and sorting settings. It is idempotent. Keep the two files in sync.
- `Scenes/Sandbox.unity` is the dev scene (built by `Tools > ARPG > Create Sandbox Scene`, which rebuilds the Grid if you run it again): isometric Grid (cell 1 x 0.5) with a `Ground` Tilemap of generated placeholder tiles and a portrait-framed camera (orthographic size 8). Replace the tiles with real art later.

# Isometric conventions

- Tile art is 128x64 px diamonds at 128 pixels per unit (1 x 0.5 world units per cell).
- Transparency sort mode is Custom Axis (0,1,0): lower Y draws in front. Sprite pivots must be at the feet.
- Characters and props use the `Entities` sorting layer so they Y-sort against each other.
- Gameplay ranges are in ground space (1 unit = one tile width). Stick input is unprojected from screen space (screen Y doubled) before moving characters. See `Docs/01-core-gameplay.md`.
- No NavMesh in 2D. Enemy navigation is a grid pathfinder over the tilemap.
- In ground space the tile lattice is a square grid rotated 45 degrees: an edge neighbour is 0.707 away, a corner neighbour 1.0. `IsoMath.CellToGround`/`GroundToCell` convert (they assume the Grid sits at the world origin).

# Player, input and camera

- `StickMath` (pure) and `FloatingStickInput` (Enhanced Touch, mouse-simulated in the editor) produce a screen-space stick value. `PlayerController` converts it to ground space with `IsoMath` and drives a `Rigidbody2D`. `FollowCamera` follows with the lead and framing from the docs. `StickVisual` draws the stick on an overlay canvas.
- `Tools > ARPG > Add Player And Camera To Sandbox` generates the placeholder art, the `Player` prefab and the scene wiring. It rebuilds the Player, Stick Input and Stick Canvas objects if run again.
- Components find each other at runtime (`FindAnyObjectByType`) when their reference fields are empty.
- EditMode tests are in `Assets/_Project/Tests/EditMode`. The pure math (`StickMath`, `IsoMath`) has no Unity scene dependency.

# Enemies

- Enemies are placed as packs, never spawned around the player (see the structure note above). `EnemyPack` (3 to 12 members) spawns its members from the pool around its position when the level loads. Members idle at their home spot until the player is within aggro range, chase, and after the leash breaks (beyond 20 units for 4 s) walk home and idle again (`EnemyState.Return`). Killed enemies stay dead: a pack never respawns a member and counts `KilledCount` (per-level saving of that state is not built yet).
- `EnemyManager` owns the pool and everything enemies share: a `NavGrid` baked by `NavGridBaker` (a cell is walkable when the Ground tilemap has a tile and no tilemap on the Obstacle layer does), a `FlowField` (one Dijkstra from the player's cell, not A* per enemy) and a `SpatialHash` of enemy positions. It ticks every `EnemyController` once per frame, so enemies have no `Update`. Each pack builds a lazy flow field to its own center so returning enemies route around walls.
- Enemies have no Rigidbody or collider. They live in ground space and only the transform is projected with `IsoMath`. Find neighbours and targets through the spatial hash. A wall is one cell thick (0.707), so `EnemyController.Move` splits long steps into pieces of 0.25 or less; without that, a frame hitch let an enemy jump a wall. Keep the per-frame path allocation free (measured at 0 bytes and about 0.02 ms per Update for 40 enemies in the editor).
- `EnemyDefinition` (ScriptableObject) holds the tuning. Its values marked as tuning are not in the docs yet. Existing states are Idle, Approach, Return and Dead (a short fade, then back to the pool); Attack and Recover come with enemy attacks. `TakeDamage(float)` takes damage that has already been through the hit formula, and a hit wakes an idle or returning enemy.
- Walls are a `Walls` Tilemap on the Obstacle layer (Individual render mode so they Y-sort against characters, Grid collider type, merged into one static collider for the player).
- `Tools > ARPG > Add Enemies To Sandbox` builds the swarmer prefab, the definition (`Data/Enemies/Swarmer.asset`) and the Enemy Manager. `Tools > ARPG > Add Test Room To Sandbox` builds a walled room with a divider that has a two cell gap, four packs of 10 and the player start. Run it after `Create Sandbox Scene` rebuilds the Grid, which removes the walls. Rerunning either keeps the tuned definition.
- The pure classes (`NavGrid`, `FlowField`, `SpatialHash`, the pack slot layout) and `NavGridBaker` are covered by EditMode tests. Enemy behaviour was checked in play mode by script (aggro, routing through the gap, leash and return, wall tunneling); there are no PlayMode tests yet.

# Combat

- The simulation math is pure and tested: `CombatFormulas` (the base curves and the hit formula from `Docs/03-itemization.md`: weapon damage, enemy life, armor reduction with its 80 percent cap, increased and more multipliers, crits), `FocusPool` (0 to 100, regen 6 per second) and `SweepGeometry` (sweep containment and target choice).
- `PlayerCombat` lives on a `Player Combat` scene object and finds the player and the enemy manager at runtime, so rebuilding the player does not remove it. Every frame it picks the target (nearest enemy in reach inside the 200 degree forward cone, else nearest in reach), swings the basic attack at 1.4 per second (120 degree sweep, reach 2.0) and auto-casts skills in slot order when the 0.35 s global cast timer is free, the skill is off cooldown, its Focus is paid and a target is in reach. There is no input. It runs in Update; the docs call for a fixed timestep, which is not done yet.
- `SkillDefinition` (ScriptableObject) holds a sweep skill's numbers. The one skill is **Cleave** from the Warden draft, a placeholder until the Wrathborn's skills are designed (`Docs/08-production.md`, open question 8).
- Choices the docs leave open, made here: Focus is gained once per basic swing that hits (not per enemy), a fresh character starts with full Focus, and the basic attack and skills run on independent timers.
- Enemies get their queries from `EnemyManager.QueryEnemies`, which reads the same spatial hash. `EnemyManager` runs first (`DefaultExecutionOrder(-100)`).
- The slash wedges (`SweepEffect`) live under a parent scaled to half height, so effects are positioned and rotated in ground space.
- `Tools > ARPG > Add Combat To Sandbox` builds the wedge art, `Data/Skills/Cleave.asset` and the `Player Combat` object. Scene build order: Create Sandbox Scene, Add Player And Camera, Add Enemies, Add Test Room, Add Combat.
- Not built yet: player life, enemy attacks, damage numbers, hit stop, drops. Enemies cannot hurt the player.
- Measured in the editor: 300 pairs of manager and combat updates allocate 0 bytes, including kills, death fades and pool returns.

# Notes

- Unity holds a lock on the project while the editor is open. Do not hand-edit `ProjectSettings/*.asset` then; apply settings through editor scripts instead.
