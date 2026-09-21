# Project overview

Rageborn: isometric Diablo-style action RPG for iOS. Unity 6000.6.2f1, URP 2D renderer, new Input System only (`activeInputHandler: 1`, so never use `UnityEngine.Input`).

The design docs in `Docs/` are the source of truth for design decisions. Start at `Docs/00-vision-and-scope.md`. If code and docs disagree, ask before changing either.

- 2D isometric sprites: Isometric `Grid`/`Tilemap` for the world, sprite characters.
- Portrait only, iOS 16+, target 60 fps (`GameBootstrap`).
- One-thumb control: a floating stick is the only input. Attacks and skills fire automatically, so there are no skill buttons, no tap-to-move and no dodge button.
- Company Filbus Software, bundle ID `com.filipbusic.rageborn`.

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

- `EnemyManager` owns the pool and everything enemies share: a `NavGrid` baked from the Ground tilemap and the Obstacle layer (`NavGridBaker`), a `FlowField` (one Dijkstra from the player's cell, not A* per enemy) and a `SpatialHash` of enemy positions. It ticks every `EnemyController` once per frame, so enemies have no `Update`. `EnemySpawner` keeps N alive on a ring around the player.
- Enemies have no Rigidbody or collider. They live in ground space and only the transform is projected with `IsoMath`. Find neighbours and targets through the spatial hash. Keep the per-frame path allocation free (measured at 0 bytes and about 0.07 ms for 40 enemies in the editor).
- `EnemyDefinition` (ScriptableObject) holds the tuning. Its values marked as tuning are not in the docs yet. Existing states are Idle and Approach; Attack, Recover and Death come with combat.
- `Tools > ARPG > Add Enemies To Sandbox` builds the swarmer prefab, the definition (`Data/Enemies/Swarmer.asset`) and the scene objects. Rerunning it keeps the tuned definition.
- The pure classes (`NavGrid`, `FlowField`, `SpatialHash`) are covered by EditMode tests.

# Notes

- Unity holds a lock on the project while the editor is open. Do not hand-edit `ProjectSettings/*.asset` then; apply settings through editor scripts instead.
