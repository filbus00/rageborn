# Technical design

## Engine: Unity

Decision: Unity 6000.6.2f1 with C#, the Universal Render Pipeline with the 2D Renderer, targeting iOS 16 and newer. The project was created on 2026-09-21. The reasoning behind the choice follows.

### What the game needs from an engine

- Hundreds of data-defined objects (items, affixes, skills, enemies) with tooling to edit and validate them.
- Many simultaneous entities on screen (40 enemies, projectiles, effects) at 60 frames per second on phones.
- 2D lighting, skeletal animation, particle effects, audio mixing with layers.
- Mature iOS build, profiling, Game Center, iCloud and haptics support.
- A path to Android later if the game earns it.

### Comparison

| Criterion | Unity | Godot 4 | Native Swift with SpriteKit |
|---|---|---|---|
| Data tooling for items and skills | ScriptableObjects, custom editors, Addressables | Resources and custom editor plugins, less mature | Build everything by hand, JSON files |
| 2D lighting and skeletal animation | Built in 2D Renderer and 2D Animation package | Built in, good for 2D | Limited, SpriteKit lighting is basic, animation by hand |
| Performance with many entities | Good with pooling, DOTS optional | Good for 2D, C# path adds overhead | Good for simple nodes, heavy for large effect counts |
| iOS profiling and debugging | Unity profiler plus Xcode Instruments | Godot profiler, iOS export with Xcode | Best, native Instruments |
| Apple services (Game Center, CloudKit, haptics) | Plugins or thin native bridge | Plugins or native bridge | Direct |
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
- Camera: fixed orthographic, size 8 for the 9 by 16 portrait framing.
- Movement and ranges are computed in ground space and projected for display, see the camera section of 01-core-gameplay.md.
- Pathfinding: Unity's NavMesh is not available for 2D, so enemy navigation uses a grid pathfinder over the tilemap plus steering, with the spatial hash grid for queries.

## Architecture

### Layers

1. Data layer: ScriptableObject definitions for items, affixes, skills, enemies, zones, and loot tables. Exported to JSON at build time for tests and simulation.
2. Simulation layer: pure C# classes with no Unity dependency for stats, damage, loot rolls and progression. Unit testable and used by the balance simulator.
3. Presentation layer: MonoBehaviours for rendering, animation, VFX, audio and UI.
4. Services: save, settings, analytics (optional), Game Center, iCloud.

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

The simulation layer uses a seeded random generator per system (loot, combat, generation). Given a seed, a zone can be replayed for debugging and for the weekly Abyss seed. Rendering and physics are non-deterministic and do not influence logic.

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
- Triggers: on item pickup batch (2 second debounce), on Forge action, on level up, on zone exit, on app background.
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
- Game Center is the only online service in 1.0.

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
- App Store items to prepare early: age rating (violence, fantasy), privacy nutrition label (data not collected if no analytics), screenshots in portrait, preview video, localizations.

## Art pipeline

- Concept: paintover sheets per enemy, silhouette tests at the size they appear on a phone.
- Characters and enemies: layered PSD, imported with the Unity 2D Animation package, 8 directions built from a small set of rigged parts, mirrored where possible.
- Environments: isometric tile sets per act, 128 by 64 pixel diamond tiles at 128 pixels per unit (one tile is 1 by 0.5 world units), a set of decor props, baked shadows.
- VFX: sprite sheet particles and a small library of shader effects (dissolve, hit flash, outline for rarity).
- Item icons: 128 by 128 icons, 60 legendary unique icons, base type icons shared across rarity with a color frame.
- UI: 9-slice panels and an icon set, all vector where possible.

## Audio pipeline

- Middleware not required. Use Unity's audio mixer with snapshots for combat intensity.
- Music stems: 4 to 6 layers per act, crossfaded by pack size.
- SFX: 300 sounds estimated, priority system that drops low priority sounds when more than 16 voices play.
