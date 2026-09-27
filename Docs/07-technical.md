# Technical design

## Engine: Unity

Decision: Unity 6000.6.2f1 with C#, the Universal Render Pipeline with the 2D Renderer, targeting iOS 16 and newer. The project was created on 2026-09-21. The reasoning behind the choice follows.

### What the game needs from an engine

- Hundreds of data-defined objects (items, affixes, skills, enemies) with tooling to edit and validate them.
- Many simultaneous entities on screen (40 enemies, projectiles, effects) at 60 frames per second on phones.
- 2D lighting, frame-by-frame sprite animation (pre-rendered from 3D, 2026-09-27; skeletal animation is no longer planned), particle effects, audio mixing with layers.
- Mature iOS build, profiling, iCloud and haptics support.
- A path to Android later if the game earns it.

### Comparison

| Criterion | Unity | Godot 4 | Native Swift with SpriteKit |
|---|---|---|---|
| Data tooling for items and skills | ScriptableObjects, custom editors, Addressables | Resources and custom editor plugins, less mature | Build everything by hand, JSON files |
| 2D lighting and skeletal animation (when engines were compared; the game now uses frame sprites pre-rendered from 3D, 2026-09-27) | Built in 2D Renderer and 2D Animation package | Built in, good for 2D | Limited, SpriteKit lighting is basic, animation by hand |
| Performance with many entities | Good with pooling, DOTS optional | Good for 2D, C# path adds overhead | Good for simple nodes, heavy for large effect counts |
| iOS profiling and debugging | Unity profiler plus Xcode Instruments | Godot profiler, iOS export with Xcode | Best, native Instruments |
| Apple services (CloudKit, haptics) | Plugins or thin native bridge | Plugins or native bridge | Direct |
| Android later | Straightforward | Straightforward | Rewrite |
| Asset store and community | Very large | Growing | Small for games |
| Cost and licensing | Free tier with revenue limits, licensing terms have changed before and must be checked at project start | Free and open source, MIT | Free with Apple developer account |
| Learning curve | Moderate | Low to moderate | Low if already an iOS developer |

Recommendation logic: the itemization and skill systems are the heaviest part of the project, and Unity's data workflow and asset pipeline reduce that cost the most. Godot 4 is a close second and the best choice if licensing risk outweighs everything else or if the developer prefers open source. Native Swift wins only if the developer is already a strong iOS programmer and wants the smallest binary, at the cost of building content tools from scratch.

Prototype gate: build a two week prototype with a floating stick, 40 pooled enemies, one auto-cast skill and item drops. If it does not hold 60 frames per second on an iPhone 12 with a stable frame time under 12 ms, revisit this decision before committing to the vertical slice.

## Project setup

| Item | Value |
|---|---|
| Product name | Rageborn |
| Company | Filbus Software |
| Bundle identifier (iOS) | com.filipbusic.rageborn |
| Unity version | 6000.6.2f1 |
| Render pipeline | URP with the 2D Renderer |
| Input | Unity Input System package only, the legacy Input class is disabled |
| iOS minimum version | 16.0 |
| Orientation | Portrait |
| Frame rate | 60 fps target, set at startup |
| Code layout | `Assets/_Project/`, runtime code in the `ARPG.Runtime` assembly, editor tools in `ARPG.Editor` |

Project settings are applied by the editor menu command Tools > ARPG > Apply Project Setup so they are reproducible and reviewable in code.

## Isometric rendering

- World: an isometric Grid with a cell size of 1 by 0.5 world units and a Tilemap per layer (ground, decor). Tile art is 128 by 64 pixel diamonds at 128 pixels per unit.
- Sorting: the 2D Renderer uses a custom transparency sort axis of (0, 1, 0), so anything lower on screen draws in front. Sprite pivots sit at the character's feet. Characters and props share the Entities sorting layer so they sort against each other.
- Sorting layers, back to front: Ground, Decals, Entities, Effects, WorldUI.
- Camera: fixed orthographic, size 7.5: 15 world units tall, about 7 wide on an iPhone in portrait (size 10 until 2026-09-28).
- Render resolution: the URP render scale is set at startup (`RenderResolution`) to a whole fraction of the screen that brings its long side closest to 870 pixels (1/3 on 3x iPhones, their point resolution; 1/2 on 2x ones), with the point upscaling filter, for Diablo 2's low-resolution look (the owner, 2026-09-28). Screen space overlay canvases draw after the upscale at full resolution. It also cuts the world's pixel work about ninefold on a 3x phone.
- Movement and ranges are computed in ground space and projected for display, see the camera section of 01-core-gameplay.md.
- Pathfinding: Unity's NavMesh is not available for 2D, so enemy navigation uses a grid pathfinder over the tilemap plus steering, with the spatial hash grid for queries.

## Architecture

### Layers

1. Data layer: ScriptableObject definitions for items, affixes, skills, enemies, zones, and loot tables. Exported to JSON at build time for tests and simulation.
2. Simulation layer: pure C# classes with no Unity dependency for stats, damage, loot rolls and progression. Unit testable and used by the balance simulator.
3. Presentation layer: MonoBehaviours for rendering, animation, VFX, audio and UI.
4. Services: save, settings, analytics (optional), iCloud.

### Key systems

| System | Responsibility | Notes |
|---|---|---|
| Input | Floating stick, dead zone, analog output | Uses the Unity Input System, touch only |
| Stat engine | Stat modifiers with add, increased and more layers, tags | Recomputed on change, cached |
| Combat | Hit resolution, damage formula, status effects, telegraphs | Fixed timestep for logic |
| Auto-cast | Skill priority, conditions, cooldowns, Focus | Deterministic given seed |
| Enemy AI | State machine per archetype, packs, aggro | Uses a spatial grid for queries |
| Spawner and generator | Seeded room assembly, packs, chests | Layout from seed and zone id |
| Loot | Tables, rarity, affix rolls, bad luck counter | Pure functions with seeded RNG |
| Inventory and Forge | Item store, stash, actions | Transactional |
| Save | Autosave after events, versioned schema | See below |
| UI | Screens as prefabs with a navigation stack | UI Toolkit or uGUI, decision at prototype |

### Determinism

The simulation layer uses a seeded random generator per system (loot, combat, generation). Given a seed, a zone can be replayed for debugging. Rendering and physics are non-deterministic and do not influence logic.

### Performance budget

| Item | Budget |
|---|---|
| Target device | iPhone 12 at 60 fps, iPhone SE 2 and iPhone X at 30 fps |
| Frame time | 12 ms on target, 28 ms on low |
| Draw calls | 150 |
| On screen enemies | 40 on target, 25 on low |
| Projectiles and effects | 150 on target |
| Memory | 700 MB peak on target, 450 MB on low |
| App size | Under 400 MB installed, initial download under 200 MB |
| Load time | Zone load under 2.5 seconds, cold start to Continue under 6 seconds |
| Battery | Under 12 percent per 30 minutes with default settings |

Techniques: object pooling for all entities, texture atlases per act, sprite batching, no per-frame allocations in gameplay (verified by the profiler), spatial hash grid for targeting, limited dynamic lights, and a quality setting that lowers effect counts. A thermal state monitor lowers effects when the device reports serious thermal pressure.

### Save system

- Storage: a JSON document per character plus one shared file for account data (stash, settings, achievements), written to app storage.
- Write rules: atomic write to a temporary file, then rename. Keep the last 3 versions as backups.
- Triggers: on item pickup batch (2 second debounce), on Forge action, on level up, on zone exit, on death, on app background.
- Level state and corpse: for each level visited in the current game session, the seed, killed packs and opened chests, plus any corpse with its gear, are saved with the character so quitting the app never loses a corpse run.
- Resume: loading a saved game restarts in town. The character's position in a level is not saved.
- Schema version: an integer in each file, with migration functions from each prior version.
- iCloud: CloudKit private database syncs save documents. Conflicts resolve by the latest modified time per character, with the losing version stored as a backup and a visible restore option.
- Corruption: on read failure, the game tries backups in order and reports which one it loaded.
- Size: a character with 400 items is under 300 KB.

### Data pipeline

- Source of truth: spreadsheets or CSV files for stat tables, importable into ScriptableObjects by an editor tool.
- Validation: an editor script checks for missing affix ranges, unreachable tiers, unbalanced drop tables and localization keys.
- Balance simulator: a command line tool using the simulation layer to run thousands of clears, output CSV, reviewed on each balance pass.

### Analytics and privacy

- The premium model needs no analytics. If added, use privacy preserving, opt-in event counts only (session length, level, crashes), and follow Apple App Tracking Transparency rules by not tracking across apps.
- Crash reporting through Xcode Organizer and MetricKit, with no third party SDK required.
- 1.0 has no online features. iCloud only copies the save between the player's own devices through Apple, and the game never requires a connection.

### Testing

| Type | Scope |
|---|---|
| Unit tests | Stat engine, damage formula, loot rolls, save migrations |
| Simulation tests | Balance targets in 04-progression-and-economy.md |
| Playmode tests | Input, auto-cast conditions, boss phases |
| Device tests | Performance on 4 target devices per milestone, thermal soak test of 30 minutes |
| Save stress | Kill the app at random points during a run, verify the loot log |
| Accessibility | VoiceOver pass, color blind simulator, large text |

### Build and release

- CI builds a signed TestFlight build on every merge to the main branch.
- Internal test group of 10, external group of 100 for beta.
- Release channels: TestFlight, then App Store. Price and region setup in App Store Connect.
- App Store items to prepare early: age rating (violence, fantasy), privacy nutrition label (data not collected if no analytics), screenshots in portrait, preview video. English only for 1.0.

## Art pipeline

- Concept: paintover sheets per enemy, silhouette tests at the size they appear on a phone.
- Characters and enemies: 3D models rendered frame by frame into sprite sheets by the editor's sprite bake (decision of 2026-09-27; 09, Route A and 4.5), 8 directions for enemies and 16 for the player, gear as separate layers stacked at runtime (Q22 replaced the earlier layered PSD and 2D Animation plan).
- Environments: isometric tile sets per act, 128 by 64 pixel diamond tiles at 128 pixels per unit (one tile is 1 by 0.5 world units), a set of decor props, baked shadows.
- VFX: sprite sheet particles and a small library of shader effects (dissolve, hit flash, outline for rarity).
- Item icons: 128 by 128 icons, 60 legendary unique icons, base type icons shared across rarity with a color frame.
- UI: 9-slice panels and an icon set, all vector where possible.

## Audio pipeline

- Middleware not required. Use Unity's audio mixer with snapshots for combat intensity.
- Music stems: 4 to 6 layers per act, crossfaded by pack size.
- SFX: 300 sounds estimated, priority system that drops low priority sounds when more than 16 voices play.

## Full-game plan: technical needs (proposed, with the owner's decisions of 2026-09-27)

Written 2026-09-27 for the full-game plan (`10-full-game-plan.md`). Proposals and estimates, except where marked **Decided**. Questions are numbered as in `08-production.md`.

### Sprite memory and app size (decided, Q3)

**Decided (Q3, 2026-09-27):** measure act 1 on a phone first, then trim with the levers below. The owner's note: "measure, then trim, also note that the game is quite high res, it can drop more in resolution. It can be the same as d2". So the character's height in pixels is one of the levers: the Wrathborn is baked 185 px tall today, while Diablo 2's heroes stood roughly 80 to 100 px on an 800 by 600 screen. Halving the height cuts texture memory and disk about four times.

Measured on 2026-09-27, after the estimates below were written: the Wrathborn's leather body in the one-handed grip, 12 animations in 16 directions at 185 px, is 153 million pixels: 584 MB uncompressed, about 64 MB as ASTC 6x6, 41 MB of PNG on disk. That is one body look in one grip, before the helm, weapon and off-hand layers.

Estimates from the brief's frame counts (09), for ASTC 4x4 (1 byte per pixel, 64 KB per 256 px cell) before trimming empty space. The brief's own number (09, section 14: the player's 9 sheets at 8 directions, about 50 MB compressed) matches this method. Real numbers must be measured, as decided for act 1 ("measured on a phone before cutting").

| Item | Cells | Memory, untrimmed |
|---|---|---|
| The Wrathborn, one body look, one grip, 16 directions, all 16 animations the full game needs (about 172 frames: the brief's 9 plus the 4 later skills, the two run turns and the backward run) | 2,752 | about 172 MB |
| The same for helm, weapon and off-hand layers (mostly empty; trimmed they shrink the most) | 3 x 2,752 | up to 516 MB, estimated 60 to 90 MB trimmed |
| One enemy, one rank, 8 directions, 5 animations (about 48 frames) | 384 | 24 MB |
| One enemy with its champion and elite sheet sets (09, 5.1) | 1,152 | 72 MB |
| One act's 8 enemies with ranks | | about 580 MB |
| One boss (512 px cells, 8 animations, about 116 frames, 8 directions) | 928 | about 232 MB |

Against the budget above (700 MB peak memory on target, under 400 MB installed):

- In memory, the player alone (one grip, equipped looks only, as built) is about 250 MB untrimmed. A level with its act's enemies and ranks as separate sheets adds about 580 MB. Together they are over the 700 MB budget before tiles, effects and the boss.
- On disk, act 1's shown looks alone (4 bodies, 3 helms, 3 one-handed and 3 two-handed weapons, 3 shields, 4 grips) come to several gigabytes untrimmed. The full game's 15 looks per slot (03) are far past 400 MB installed.

Proposed ways to fit, all compatible with the decisions (Q3 lists them as options):

1. Trim and pack every sheet into atlases (the bake writes full cells today). Expected saving 40 to 60 percent for bodies, more for pieces.
2. Rank variants by shader instead of separate sheets: the built tint cannot reach gold or crimson (`CLAUDE.md`, Rank), but a gradient-map shader that replaces the sprite's colours by brightness can. One sheet set per enemy instead of three: act enemies drop from about 580 to about 190 MB before trimming.
3. 16 directions only where the eye needs them (idle, run, the turns, the backward run), 8 for attacks, skills, hit and death, whose rows the game already picks by angle.
4. Stream looks by act: Unity Addressables in per-act groups, with acts 2 to 5 and their looks as Apple on-demand resources or background assets, so the installed app holds act 1 and the town only.
5. Fewer frames for short actions (skills at 8 frames instead of 10 to 14).

6. A lower character resolution, down to Diablo 2's (allowed by the owner, Q3).

If none of these is enough, the remaining levers change a decision (fewer looks per act, fewer grips, or drawing the player's 3D model at runtime instead of baked frames) and are for the owner.

### Content data

- 40 enemies, 88 affixes, 40 legendaries, 72 modifiers and 60 passive nodes are too many to set up by hand in the inspector. Proposed: CSV tables in `Assets/_Project/Data/Tables` imported into ScriptableObjects by an editor tool, as planned above (Data pipeline), with the validation script checking every reference.
- Enemy behaviours are code per archetype (built for three); each new enemy is a definition plus, where the table in 05 says so, one behaviour hook (split on death, aura, shield, summon from placed piles).

### Save format additions (proposed, in order)

Each is a version bump with a migration and a test, as built: skills (levels, modifiers, loadout order, triggers, presets), the passive tree, the Vigil tier and per-tier waypoints, the stash and settings in the account file (above), gems and sockets, rift keys and Boss Sigils, the Codex, lore read, achievements, the Abyss best, Paragon.

### Balance tools

The balance report and the autopilot cover act 1. Proposed: both take an act and a tier as input, so every act on every tier can be checked against 04's targets before it ships, and the report adds the four later skills and the passive tree when they exist.

## Audio and haptics plan (proposed, with the owner's decision of 2026-09-27)

Built: synthesized placeholder effects with 16 voices and priorities, and a placeholder synth music loop (`CLAUDE.md`, Sound). The plan below is for the real audio. **Decided (Q21, 2026-09-27):** the music and sounds are generated with AI tools, like the art, not commissioned or licensed. The structure below is a proposal.

### Music

Kept from 01 and 05: sparse low strings, choir and drums, layered stems that follow pack size and elite presence. Proposed structure per act:

| Piece | Where | Stems |
|---|---|---|
| Town theme | Each act's town, 2 to 3 minute loop | Pad, a solo instrument (act 1 cello, act 2 low flute, act 3 choir, act 4 brass, act 5 a detuned version of act 1's cello) |
| Dungeon theme | Every level of the act | 4 stems: drone (always), low percussion (a pack aggroed), high percussion and strings (6 or more enemies engaged), choir (an elite pack engaged). Stems fade in over 2 s and out over 6 s after the fight |
| Boss theme | The boss arena | 3 sections, one per phase, changing on the phase flash; silence on the kill, then the town theme's solo instrument |
| Endgame | Rifts (the act theme at a faster tempo), the Abyss (its own drone that darkens every 10 floors) | |
| Title | The title screen | A short piece on the act 1 motif |
| Stingers | Level up, Legendary drop, act complete, death | 2 to 5 seconds each, ducking the music |

Mixer (07 above: snapshots for combat intensity): groups Music, Effects, Ambience, UI, with snapshots Explore, Fight, Boss and Paused (music at 40 percent under the Bag screen).

### Sound list per system

The 300 sounds estimated above, by system (counts proposed):

| System | Sounds | Count |
|---|---|---|
| Player | Footsteps per floor type (stone, mud, wood, bone, iron, void), the basic swing per grip, hits taken, death, level up, the ember lantern's crackle (ambient, near) | 30 |
| Skills | Each of the 8 skills: cast, impact, and its modifiers' extra parts where they add a new event (fissures, returning axe) | 35 |
| Enemies | Per enemy (40): aggro, attack wind-up, attack, hurt, death; shared per archetype where they sound alike | 120 |
| Bosses | Per boss (9): each attack's telegraph and impact, phase roar, stagger, death | 60 |
| Loot | A drop per rarity (5, with Cursed), gold, gems, materials, pickups, equip per slot type | 15 |
| Forge and town | Each Forge action, salvage, stash, trainer, waystone, portal, waypoint, NPC greetings (no voice lines, a short sound each) | 15 |
| UI | Tap, sheet open and close, tab, error, upgrade badge, hint chime | 10 |
| Ambience | One bed per tileset (10) and per town (5): wind, water, fire, distant bells | 15 |

### Haptics (01: all optional)

Light tap on a Legendary drop, medium on a level up, rhythmic on a boss phase change (01). Proposed additions: a short heavy tick on a player death, a light tick on a block, and one on a stagger. Uses the iOS impact generators; off when the Settings toggle is off.
