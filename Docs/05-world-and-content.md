# World and content

## Setting

The Vigil was a chain of watch fires that kept the Hollow at bay for four hundred years. The fires are out. The lands under them are cursed and full of the dead, the corrupted and the things that fed on both. The player character is a survivor who takes up an ember from the last fire and walks the old road toward the source.

Tone: grim, weathered, quiet. Color palette is desaturated stone, ash and rust, with warm ember orange for loot, player effects and the Vigil fires, cold blue and sickly green for enemies. Text is short and low on exposition. Lore comes from item descriptions, zone landmarks and short cutscenes.

## Story outline

1. Act 1, Ashfields: the first fire dies. The player gathers survivors at a ruined chapel.
2. Act 2, Drowned Reach: a flooded province whose watchmen turned. The player finds the second ember.
3. Act 3, Bone Orchard: catacombs and burial fields. A saint who bargained with the Hollow.
4. Act 4, Iron Spire: a fortress of sworn knights who serve the Hollow willingly.
5. Act 5, The Hollow: the source. The final boss is the first watchman, whose failure began the fall.

Story is delivered in short scenes across the campaign, about 20 minutes in all (25 scenes, below). Skippable at all times. Scenes are made in-engine from sprites, portraits and text lines, with no voice (decided 2026-09-27, Q20).

## Structure

| Act | Levels | Player level (Vigil I, the first of five passes, decided 2026-09-27, Q1) | Boss | Signature enemies |
|---|---|---|---|---|
| 1 Ashfields | 6 | 1 to about 7 (enemy levels 1 to 7; flattened from 1 to 12 on 2026-09-26, see 08-production.md) | Cinder Warden | Husks, ghouls, wolves, bandit archers |
| 2 Drowned Reach | 6 | 7 to about 12 (enemy levels 9 to 14, proposed) | The Tidewife | Drowned, leech swarms, marsh casters |
| 3 Bone Orchard | 6 | 12 to about 16 (enemy levels 14 to 18, proposed) | Saint Marrow | Skeleton knights, bone chargers, grave priests |
| 4 Iron Spire | 6 | 16 to about 19 (enemy levels 18 to 21, proposed) | Warlord Kaeth | Armored knights, siege brutes, banner bearers |
| 5 The Hollow | 6 | 19 to about 21 (enemy levels 21 to 23, proposed); level 60 is reached over the later Vigil tiers (04) | The First Watchman | Void wraiths, corrupted allies, elite mixed packs |

Each act has one town above one dungeon. The dungeon is descended level by level: the exit room of each level holds stairs down to the next, and its start room holds stairs up (on the first level they lead back to the town).

## Town

Each act has one town, a small safe scene the player walks through with the stick. There are no enemies and no combat. NPCs stand at fixed spots for the Forge, the Stash, the Class Trainer and the Waystone. Walking up to an NPC opens its bottom sheet panel, so nothing in the world needs a tap. The dungeon entrance is a stairway the player walks into. A new character begins in the act 1 town, at the ruined chapel. The Waystone jumps to any activated waypoint, see Getting around below.

### Getting around

- Stairs: the exit room of each level holds stairs down and the start room holds stairs up.
- Waypoints: each level has a waypoint, in its start room (decision of 2026-09-26), that the player activates by stepping on it. From any activated waypoint, or from the Waystone in town, the player can jump to any other activated waypoint. A list screen opens from the waypoint or the Waystone. Rifts and the Abyss are entered from an NPC in town.
- Portal Tome: a permanent item, given by an NPC, the Wanderer, on dungeon depth 3 (decision of 2026-09-26). It stays with the character, is free to use and is used from the UI. It opens a portal to town, and the portal stays open so the player can come back through it. Before the Tome the player walks back to town by the stairs. There are no consumable portal scrolls.
- Persistence: within a game session a level keeps its seeded layout, its dead enemies and its opened chests when the player leaves and comes back. A sleep mechanic resets the session: a bed in each town's inn (decided 2026-09-27, Q14; rules under "Sleep" below).
- Death: the character is sent to town, see 01-core-gameplay.md.

## Level generation

Levels are connected halls, as in Diablo 1's cathedral (the owner, 2026-09-30: the rooms-and-corridors levels were "more like corridors with open rooms"; "make the levels more open spaces"). A seeded generator packs rooms of different sizes wall to wall and joins them without corridors.

- Rooms: rectangles about 13 to 23 units a side (18 to 32 cells), packed wall to wall on a grid whose columns and rows each have their own width. Neighbours along the level's room tree open into each other through a wide arch (5 to 11 cells) or, a third of the time, through the whole shared wall bar a pier at each end, making one hall; other neighbours often get an arch too, so there are several ways around. No corridors.
- Dressing: rooms stand with pillar rows along their length, pillar grids, stubs of wall reaching in from their sides, or a hand-authored pillar layout from the room library (30 per act, three sizes, mostly open halls); the start and exit rooms stay open. Each room has a floor style (flagstone, dark brick, packed earth, mossy stone), props against its walls (barrels, crates, urns, bone piles, rubble, broken columns, sarcophagi, braziers and candles, at most one lit per room) and decals on its floor (cracks, bones, blood, rubble, moss, a skull, puddles). Placeholder art drawn in code until Docs/09's tiles and props exist.
- Layout: a start room with the stairs up, 5 to 8 rooms of mixed type, an exit room with the stairs down. A level takes 4 to 8 minutes to clear.
- Room types: combat (60 percent), elite (15), treasure (10), shrine (10), ambush (5).
- Every level has one guaranteed elite pack and one guaranteed chest. The act boss waits on the last level of each act.
- Packs are placed by the generator and idle until the player comes within aggro range, see 01-core-gameplay.md.
- Mini-map: a small overlay at the top corner showing explored rooms, chests and the exit arrow. It is read-only.

### Shrines

A shrine is stepped on to activate. Effects last 60 seconds: Speed, Fury (damage), Warding (damage reduction), Fortune (Magic Find), Wrath (the Wrathborn's Rage; the Focus shrine of the drafts' Focus classes, renamed for a Rage class, Q22). One shrine per room at most.

## Enemy roster (launch)

Count: 40 normal enemy types, 15 elite modifiers used across them, 5 act bosses, 4 optional bosses. Each enemy has a sprite set with 8 direction facing and the art brief's 5 animations, plus champion and elite sheet sets (09, 5.1; this replaces "4 states and 2 palette swaps per act", Q22).

Per act examples (archetypes from 01-core-gameplay.md):

| Act | Enemy | Archetype | Notes |
|---|---|---|---|
| 1 | Husk | Swarmer | Groups of 8 to 12, low health |
| 1 | Ghoul | Brute | Slow slam, 2 unit radius |
| 1 | Bandit Archer | Archer | Line telegraph |
| 2 | Leech Swarm | Swarmer | Poison on hit, splits when killed |
| 2 | Marsh Witch | Caster | Ground pools, summons husks |
| 3 | Bone Charger | Charger | Charge with 0.5 s windup |
| 3 | Grave Priest | Support | Heals and shields nearby |
| 4 | Siege Brute | Brute | Large radius slam, knock back |
| 4 | Banner Bearer | Support | Grants attack speed to pack |
| 5 | Void Wraith | Charger | Teleports short distances |
| 5 | Corrupted Watchman | Archer plus melee | Mixed behavior, two attack patterns |

## Bosses

Each act boss has three phases in a circular arena of radius 12 units. Bosses have telegraphed attacks only. Bosses have a stagger meter that only fills through repeated hits and briefly stops the boss when full, so weaker builds can still finish fights.

| Boss | Phase 1 | Phase 2 | Phase 3 |
|---|---|---|---|
| Cinder Warden | Slams and ember volley | Spawns burning husks, arena edge ignites | Rapid charges, ember rain |
| The Tidewife | Water pools and tentacles | Floods half the arena, pools spread | Sweeping wave, drowned adds |
| Saint Marrow | Bone spikes, grave bolts | Raises skeletons, teleports | Ring of bones closes inward |
| Warlord Kaeth | Cleave and shield charge | Calls archers on ramparts | Berserk, attacks chain with no pause |
| The First Watchman | Copies of each earlier boss pattern in rotation | Void fields plus mixed adds | Screen edges close, needs constant movement |

Optional bosses appear in the boss rotation from Vigil II. They drop legendaries with a higher chance and are tuned around specific build tags to encourage build variety.

## Endgame

### Rifts

- Timed zones of 5 minutes with a kill goal.
- Rift keys drop from elites and bosses. Keys set difficulty tier and modifiers.
- A completed rift gives a chest with 3 items, at least one Rare.
- A death ends the run and returns the character to town with its gear, no corpse (decided 2026-09-27, Q13).
- Rift modifiers, two per key: Bloodlust, Hexed (enemies curse), Frozen Ground, Swarm, Champion Horde.

### Abyss

- An endless dungeon. Each floor is a compact room with a 90 second timer, a guardian on every fifth floor.
- Floor enemy level rises 2 per floor. Every 10 floors the player picks a boon from three, active for the rest of the run.
- Rewards: materials on each floor, item drops from guardians.
- Death ends the run and returns the character to town with its gear, no corpse (decided 2026-09-27, Q13). The deepest floor reached is kept as the player's personal best.

### Boss rotation

A list of all bosses, each usable at any unlocked difficulty tier. A boss can be fought repeatedly for loot. Boss keys drop from rifts and Abyss guardians.

### Achievements

60 achievements, local only (no Game Center in 1.0), recorded in the Journal, with no rewards (decided 2026-09-27, Q17). Examples: clear each act without using a potion, reach Abyss depth 50 with a Fortress build, collect all class legendaries, win with a build that has no active skill of a certain tag.

## Audio and art direction

- Art: in the style of the original Diablo 2 but not a copy (2026-09-27): sprites pre-rendered from 3D models, frame by frame in 8 directions at 12 fps, 256 by 256 cells for standard enemies, 512 by 512 for bosses, isometric perspective. Environments are tile-based with lit layers. Walls on a room's camera-facing sides are drawn cut down, so they never hide the character. The full brief is 09-art-brief.md.
- Lighting: dark scenes lit by the player's ember and enemy effects. Dynamic 2D lights on the player and a few props only, for performance.
- Music: sparse, low strings, choir, drum layers. Layered stems respond to pack size and elite presence. Music and sounds are made with AI generation tools, like the art (decided 2026-09-27, Q21).
- Sound: heavy, tactile, low frequency emphasis. Rarity-specific pickup sounds.

## Content counts for 1.0

One class at launch (decided 2026-09-21); these counts replace the earlier three-class table (decided 2026-09-27, Q23; 10-full-game-plan.md, section 14).

| Category | Count |
|---|---|
| Acts | 5 |
| Dungeon levels | 30 |
| Hand-authored rooms | 150 (30 per act) plus 5 boss arenas |
| Towns | 5 (one per act) |
| Normal enemies | 40 |
| Elite modifiers | 15 |
| Bosses | 9: 5 act bosses and 4 optional |
| Classes | 1 |
| Active skills | 8 |
| Skill modifiers | 24 choices at 3 levels, 72 options |
| Passive nodes | 60 |
| Legendary items | 40 (Q7) |
| Item sets | none (Q18) |
| Affixes | about 90 |
| Gems | 4 types, 5 tiers |
| Achievements | 60 |
| Story scenes | 25 short scenes, about 20 minutes |

## Full-game plan: world and content (proposed, with the owner's decisions of 2026-09-27)

Written 2026-09-27 for the full-game plan (`10-full-game-plan.md`). Everything in this section is a proposal: names, numbers, behaviors and story, except where marked **Decided** (the owner's answers of 2026-09-27). Numbers are tuning values. The text above now follows those decisions. Questions are numbered as in `08-production.md`, "Open questions from the full-game plan".

Rules every proposal here keeps (decided above or in 00 and 01): enemies are packs placed in rooms, idle until aggro, and never spawn around the player; killed enemies stay dead for the session; the stick is the only input; every dangerous attack is telegraphed; ground shapes cannot be dodged by chance, only by moving; the town is safe.

### Acts at a glance

The player level column above used to say act 2 at 12 to 24 and so on, which cannot be reached with 6 levels per act (04, "The gap between the XP curve and the campaign"). **Decided (Q1, 2026-09-27):** the campaign is played five times, once per Vigil tier, and the column now shows Vigil I. The enemy levels below are 04's Vigil I proposal. **Decided (Q9):** each act has a damage element, as the column below lists.

| Act | Town | Dungeon | Depths 1 to 3 | Depths 4 to 6 | Damage element (decided, Q9) | Vigil I enemy levels | Boss |
|---|---|---|---|---|---|---|---|
| 1 Ashfields | Emberwatch, the ruined chapel village | The chapel's catacombs | Crypts: burial niches, bone piles, soot | Burned barrows: collapsed tombs opening into ash-choked earth | Fire | 1 to 7 (built) | Cinder Warden (built) |
| 2 Drowned Reach | Saltmere, a stilt village on the last dry hill | The sunken watch-keep | Flooded cellars: shallow water, rotting timber | Drowned halls: deeper water channels, coral-grown stone | Cold | 9 to 14 | The Tidewife |
| 3 Bone Orchard | Gravesend, a gravediggers' camp in an orchard of dead trees | The ossuary under the orchard | Ossuary galleries: stacked skulls, root-broken walls | Saint's crypts: marble, candle shrines, grave dirt | Poison | 14 to 18 | Saint Marrow |
| 4 Iron Spire | The Last Camp, a refugee siege camp at the foot of the Spire | The Iron Spire itself, descended from the gate down | Barracks and forges: iron floors, braziers, banners | Oath halls: black iron, chains, trophy walls | Physical and fire | 18 to 21 | Warlord Kaeth |
| 5 The Hollow | The Ember Refuge, the survivors' final camp at the rim | The Hollow, a wound in the world | Broken Vigil: ruins of the first watch fire, half dissolved | The void: floating stone, violet light, no walls on some sides (walls stay walls in play) | Shadow | 21 to 23 | The First Watchman |

Each act uses two tilesets (depths 1 to 3 and 4 to 6) plus its boss arena. Act 1 has one placeholder set today.

### Towns (proposed)

Every town has the same five services, placed so every NPC is within 12 units of the arrival point and the stairs down are the farthest thing from it, so a player coming back from the dungeon passes the smith and the stash on the way. NPCs are walked up to, never tapped (decided).

| NPC | Every town | Opens |
|---|---|---|
| Smith | yes (act 1 built) | The Forge (03) |
| Stash keeper | yes | The Stash (03, 06) |
| Trainer | yes | Respec of the passive tree and of skill points (02, 04); a short reminder of the loadout rules |
| Waystone | yes (act 1 built) | The Waystone map (06): waypoints, tiers, boss rotation |
| The Watcher | yes, from the first act 5 boss kill | Rifts and the Abyss (Endgame, below), the "NPC in town" of 05 above; 06's Waystone map now points here (Q22) |
| Story NPC | one per town | Plays the act's town scenes; otherwise one line of idle talk |

The Wanderer (built, act 1 depth 3) stays the only NPC in a dungeon.

Layout notes:

| Town | Landmark | Story NPC | Notes |
|---|---|---|---|
| Emberwatch | The roofless chapel with its cold fire bowl (09 has `chapel_ruin`) | Mother Aldis, the chapel's last keeper | Built as a flat square today. The chapel sits north, stairs down in its crypt entrance |
| Saltmere | A beached watch-tower used as the stash house | Bram, a ferryman | Boardwalks over mud; water tiles are walls |
| Gravesend | A dead orchard in rows; the stash is a mausoleum | Sister Ivy, a gravedigger who serves Saint Marrow until act 3's end | Lantern posts light the paths |
| The Last Camp | Siege wagons and tents under the black Spire | Captain Hale, a deserter from Kaeth's order | Most crowded town; keep NPCs spread out so walk-up panels never overlap |
| The Ember Refuge | A new watch fire the survivors light with the player's ember at act 5's start | Mother Aldis again, come from act 1 | The fire grows brighter after each act 5 tier is cleared |

### Dungeon rooms per act

05 above asks for 30 rooms per act in three sizes. Proposed split per act: 10 small (20 to 22 cells), 12 medium (26 to 30), 8 large (32 to 36), all in the built text format (`RoomShape`). Each act also gets its boss arena (built for act 1: a 36-cell disc).

| Act | Room features (all built from floor and pillar cells, no new tile rules) |
|---|---|
| 1 | Pillared crypt halls, ring-shaped rooms around a central tomb, narrow burial galleries |
| 2 | Rooms split by water channels (walls) with 5-cell bridges, flooded pits (walls) |
| 3 | Long ossuary corridors turned into rooms, crypts with stacked sarcophagus rows as pillars |
| 4 | Square halls with pillar colonnades, forges as 3 by 3 pillar blocks, trophy rooms |
| 5 | Broken, irregular rooms with many small voids (walls), few long sight lines for archers |

Rooms needed for the room types (below): each act's 30 rooms must include 3 shrine rooms and 2 ambush rooms (any combat room can be used for those too, but these read best).

### Enemy roster (proposed: 40 normal enemies)

All use the built rules: level-driven life and damage from 03's curves, a life and hit multiplier against the husk, packs from 3 to 12, champion and elite variants (09: a champion and an elite sheet set each). Archetypes and counterplay are 01's. Telegraph times follow 01 (ground 0.6 to 1.2 s, projectile line 0.4 s, charge line 0.5 s).

Act 1 (fire): the first three are built.

| Enemy | Archetype | Life, hit (times a husk's) | Behavior | Telegraph | From depth |
|---|---|---|---|---|---|
| Husk | Swarmer | 1, 1 (built) | Bites at reach | 0.35 s swell (built) | 1 |
| Ghoul | Brute | 3, 2.2 (built) | Slam within 1.6 | 2-unit circle, 0.9 s (built) | 2 |
| Bandit Archer | Archer | 0.7, 1.2 (built) | Shoots within 7.5 with sight, keeps 4.25 to 5 away | 0.4 s line (built) | 2 |
| Ash Wolf | Charger | 0.8, 1.0 | Packs of 4 to 6 without husks. Lunges 4 units at a target 3 to 5 away, then bites normally | 0.5 s line, 4 long | 3 |
| Bandit Cutthroat | Swarmer | 0.9, 1.1 | Fast (speed 4.2), strikes twice quickly, then backs off 2 units for 1 s | 0.3 s swell | 3 |
| Ember Acolyte | Caster | 0.8, 1.3 | Keeps 6 away; every 5 s places a fire circle (radius 1.5) on the player's spot; the fire burns 3 s after landing (0.5 hit per second) | 1.0 s circle fill | 4 |
| Pyre Keeper | Support | 1.5, 0.8 | Stays behind its pack; allies within 5 deal 20 percent more damage (a visible ember aura); flees to the pack's far side when the player is within 3 | Aura ring on the ground, always shown | 4 |
| Carrion Bloat | Brute | 2, 1.8 | Slow (speed 1.6); walks at the player and bursts when killed or after reaching it: a 2-unit circle | 0.8 s circle fill on death or arrival | 5 |

Act 2 (cold):

| Enemy | Archetype | Life, hit | Behavior | Telegraph |
|---|---|---|---|---|
| Drowned | Swarmer | 1.1, 1 | Husk-like; hits slow the player 10 percent for 1 s | 0.35 s swell |
| Leech Swarm | Swarmer | 0.5, 0.6 | Poison on hit (a bleed of 60 percent over 3 s); splits into 2 half-life leeches when killed, once (05 above) | none, small hits |
| Marsh Witch | Caster | 1, 1.4 | Places 2 water circles (radius 1.8) that slow 40 percent for 4 s; every 15 s calls 3 Drowned from her own pack's dormant spares (see Ambush rooms: they lie in the water near her, visible, from the start) | 1.1 s circle fill |
| Drowned Watchman | Brute | 3.5, 2.4 | Slams a 3-unit cone forward | 1.0 s cone fill |
| Harpooner | Archer | 0.9, 1.3 | Throws a harpoon that slows 50 percent for 1.5 s on hit | 0.4 s line |
| Bog Lurker | Charger | 1.6, 1.8 | Idles submerged (a ripple); surfaces with a 5-unit lunge | 0.6 s line |
| Tide Priest | Support | 1.4, 0.7 | Shields the pack member nearest the player for 30 percent of its life every 8 s | A blue ring on the shielded enemy, 0.5 s before it lands |
| Mire Hound | Charger | 0.9, 1.1 | Like the Ash Wolf, with a frost bite (slow 20 percent, 2 s) | 0.5 s line |

Act 3 (poison):

| Enemy | Archetype | Life, hit | Behavior | Telegraph |
|---|---|---|---|---|
| Grave Rat Swarm | Swarmer | 0.4, 0.5 | Packs of 12; fast; poison bite | none |
| Skeleton Knight | Brute | 3, 2 | Shield up: takes 50 percent less from the front while it winds up; a 2-unit cleave | 0.8 s cone fill |
| Bone Charger | Charger | 1.8, 2 | Charges 7 units, stops at walls (05 above) | 0.5 s line (05) |
| Grave Priest | Support | 1.5, 0.8 | Heals the most hurt ally within 6 for 25 percent every 6 s; shields when no one is hurt (05 above) | Green ring on the target, 0.6 s |
| Ossuary Archer | Archer | 0.8, 1 | Fires 3 arrows in a 20 degree fan | 0.4 s lines, 3 of them |
| Mourner | Caster | 1, 1.4 | Grave bolt: a circle (radius 1.2) that leaves a poison pool for 4 s (0.4 hit per second) | 0.9 s circle fill |
| Bone Weaver | Caster | 1.2, 1 | Stands still and raises 2 skeleton husks from bone piles already lying in the room (placed at level load, visible), every 12 s, up to 4 | 1.0 s glow on the bone pile |
| Corpse Hulk | Brute | 4, 2.6 | Slow; ground pound in a ring (inner 1, outer 3) | 1.1 s ring fill |

Act 4 (physical and fire):

| Enemy | Archetype | Life, hit | Behavior | Telegraph |
|---|---|---|---|---|
| Sworn Squire | Swarmer | 1.2, 1.1 | Armored husk equivalent (plus 50 percent armor) | 0.35 s swell |
| Armored Knight | Brute | 3.5, 2.2 | A 2-hit combo: cone, then a 1.5-unit circle | 0.7 s cone, then 0.6 s circle |
| Siege Brute | Brute | 5, 3 | Large slam (radius 3) that knocks the player back 2 units (05 above) | 1.2 s circle fill |
| Banner Bearer | Support | 2, 0.8 | Allies within 6 attack 25 percent faster (05 above); the banner falls when it dies | Banner aura ring, always shown |
| Crossbowman | Archer | 1, 1.6 | Slow, heavy bolt that pierces through other enemies | 0.5 s line |
| Warhound | Charger | 1, 1.2 | Like the Ash Wolf, faster lunge (6 units) | 0.5 s line |
| Iron Chaplain | Caster | 1.3, 1.5 | Consecrates a 6-long line of burning ground toward the player, 3 s | 1.0 s line fill |
| Oathbound Lancer | Charger | 2.5, 2.4 | Long charge (9 units) that does not stop at the first hit | 0.6 s line |

Act 5 (shadow):

| Enemy | Archetype | Life, hit | Behavior | Telegraph |
|---|---|---|---|---|
| Hollowed | Swarmer | 1.1, 1.2 | The Hollow's husk: faster (speed 3.8) | 0.3 s swell |
| Void Spawn | Swarmer | 0.6, 0.8 | Splits into 2 when killed, once | none |
| Void Wraith | Charger | 1.4, 1.8 | Blinks up to 4 units to reposition, then charges (05 above) | Blink marker 0.4 s, then 0.5 s line |
| Corrupted Watchman | Archer | 1.6, 1.6 | Shoots at range; below 3 units switches to a sword sweep (05 above) | 0.4 s line; 0.6 s cone |
| Rift Caller | Caster | 1.2, 1.4 | Opens a void circle (radius 2) that pulses 3 times, 1 s apart | 0.9 s fill per pulse |
| Hollow Colossus | Brute | 6, 3.2 | Two slams: a line (8 long, 2 wide), then a circle (radius 2.5) at its end | 1.0 s line, 0.8 s circle |
| Whisperer | Support | 1.5, 0.8 | Curses the player while within 7: 15 percent less damage dealt; the curse ends 2 s after it dies or the player leaves | A violet tether line, always shown |
| Fallen Vigil Knight | Brute | 4, 2.6 | A corrupted ally from earlier acts: shield charge (5 units) then cleave | 0.5 s line, 0.7 s cone |

Pack mixes (the built rule for act 1, `PackComposition`, extended): in each act, depth 1 uses the act's swarmer only; deeper, a pack is swarmers only (40 percent), swarmers with 2 or 3 of one other type (45, the type drawn from those unlocked at that depth), or a band of 5 non-swarmers (15). A Support or Caster is never alone in a pack. Elite packs are made of one type, any of the act's eight.

### Elite modifiers (15, proposed)

01 lists eight; three are built (Hasted, Vampiric, Frozen). Proposed numbers for the other five and seven more. The built roller gives an elite one or two modifiers with equal chance; proposed: the chance of two rises with the tier (04, tier table: 75 percent on Vigil II and III, always two on IV and V). Modifiers open by act on Vigil I so the list grows as the player learns.

| Modifier | Effect (proposed numbers) | Icon colour (09) | Opens |
|---|---|---|---|
| Hasted | Plus 30 percent move and attack speed (built) | Yellow | Act 1 |
| Vampiric | Heals on a landed hit (built) | Green | Act 1 |
| Frozen | Slows the player on a landed hit (built) | Cyan | Act 1 |
| Molten | Leaves a fire pool (radius 1.2, 4 s, 0.5 hit per second) every 3 s where it walks; explodes on death (radius 2, 0.8 s fill) | Orange | Act 2 |
| Shielded | A barrier of 25 percent of its life that regenerates after 6 s without taking a hit | White | Act 2 |
| Teleporting | Blinks next to the player every 7 s when more than 5 away | Violet | Act 3 |
| Splitting | At half life splits off 2 normal copies with 30 percent life each | Grey | Act 3 |
| Cursing | Each hit adds a stack of minus 5 percent to all resistances for 6 s, up to 5 stacks | Dark violet | Act 3 |
| Plagued | Poison clouds (radius 2, 5 s, 0.4 hit per second) appear under the player every 6 s | Sick green | Act 4 |
| Mortar | Lobs a circle (radius 1.5) at the player every 4 s when more than 4 away | Rust | Act 4 |
| Fire Chains | Elites of the pack are joined by burning lines; crossing one deals 0.8 hit (lines drawn, never hidden) | Ember | Act 4 |
| Juggernaut | Immune to slows and knockback; plus 40 percent life | Iron grey | Act 5 |
| Enraged | Below 30 percent life: plus 50 percent attack speed | Red | Act 5 |
| Linked | Damage to one elite of the pack is shared equally across all of them | Blue | Act 5 |
| Desecrator | A shadow circle (radius 1.8) under the player every 5 s | Black-violet | Act 5 |

Left out on purpose: anything that pulls, stuns or roots the player (the stick is the only input, so losing control is worse than on a PC), reflect damage (it punishes the melee launch class for fighting at all), and invisible or unmarked hazards.

### Bosses (proposed)

Every boss follows the built Cinder Warden pattern (05, Act boss in `CLAUDE.md`): 30 times a normal enemy's life at its level, phases below 2/3 and 1/3 with a screen flash and the current attack cancelled, the stagger meter (3.5 per hit, 3 s stagger), attacks as ground telegraphs that fill before they land, damage as the enemy hit curve times a multiplier, an arena of radius 18 cells (12.7 units) replacing the last level's exit room, reset (not heal) when the player leaves by 3 units or dies, and Stairs To Town after the kill (acts 1 to 4 also open the stairs to the next act's town, see Story). Vigil III adds the one extra attack listed per phase (04, tier table).

The Cinder Warden (act 1) is built as written above in this file and in `CLAUDE.md`; its Vigil III additions: phase 1 a second slam right after the first (offset 3 units toward the player), phase 2 fire ring 1 unit wider, phase 3 a fourth charge.

The Tidewife (act 2, cold):

| Phase | Attacks (radius or size, fill time, damage multiplier) | Vigil III addition |
|---|---|---|
| 1 | Tide Pools: 3 circles (radius 2) near the player, 1.0 s, 1.0, leaving slowing water (40 percent) for 4 s. Tentacle Lash: a line 7 long, 1.2 wide, 0.8 s, 1.4 | A fourth pool |
| 2 | Flood: half the arena (a half disc) fills over 2.0 s and stays 8 s: slows 40 percent, 0.5 hit per second; it switches sides every 10 s. Pools grow to radius 3. 3 Drowned rise from the water edge every 15 s, up to 9 (they lie in the arena from the start, visible under the water) | Flood ticks 0.7 |
| 3 | Sweeping Wave: a band 3 wide crossing the whole arena from one side with a 3-unit gap, 1.2 s, 1.6; every 6 s. Tentacle Lash twice in a row | The gap narrows to 2.5 |

Saint Marrow (act 3, poison):

| Phase | Attacks | Vigil III addition |
|---|---|---|
| 1 | Bone Spikes: 5 circles (radius 1.2) in a line toward the player, each 0.7 s, landing 0.15 s apart, 1.2. Grave Bolts: 3 aimed shots, 0.4 s lines, 0.6 each | A second line of spikes at 30 degrees |
| 2 | Raises 4 skeleton husks from the bone piles at the arena rim every 12 s, up to 8. Teleports every 8 s: a 0.6 s marker at the destination, never within 4 of the player | Teleport leaves a poison pool (radius 2, 4 s) |
| 3 | Ring of Bones: a band from the arena rim closes to radius 5 over 20 s with 2 gaps 3 units wide that turn slowly; 1.0 hit per second inside the band; then it resets. Spikes continue | The gaps are 2.5 wide |

Warlord Kaeth (act 4, physical and fire):

| Phase | Attacks | Vigil III addition |
|---|---|---|
| 1 | Cleave: a 120 degree cone, radius 4, 0.8 s, 1.8. Shield Charge: a line to the player, 0.5 s, 1.4, knockback 2 | A second cleave after the charge |
| 2 | Calls archers: 4 Crossbowmen take posts on the arena rim every 20 s (normal enemies, killable, Hurl Axe reaches them). Kaeth keeps phase 1 attacks | 5 archers |
| 3 | Berserk: chains of 3 attacks (cleave, charge, cleave) with no recovery between them, then a 2 s pause with his back turned (the fairness rule in 01); plus 30 percent move speed | Chains of 4 |

The First Watchman (act 5, shadow):

| Phase | Attacks | Vigil III addition |
|---|---|---|
| 1 | Echoes: one earlier boss pattern every 3 s in turn: the Warden's slam (radius 3.2, 1.1 s), the Tidewife's pools, Marrow's spike line, Kaeth's charge. Same numbers as their owners at this level | Two echoes overlap every fourth turn |
| 2 | Void Fields: 3 circles (radius 3) that fill in 1.2 s and stay 12 s, 0.8 hit per second. Mixed adds: 2 enemies from each earlier act's roster every 15 s, up to 8, walking in from the arena's four gates | Void Fields last 16 s |
| 3 | The edges close: a band at the rim shrinks the arena from radius 12.7 to 6 over 30 s and stays (1.2 hit per second inside it). Void rain: a circle (radius 1.5) on the player's spot every second, 0.9 s, 0.9 | The arena shrinks to 5 |

Optional bosses (in the boss rotation from Vigil II, 05 above, decided 2026-09-27, Q25; each rewards a different build, with a higher Legendary chance: 60 percent on Vigil II rising to 100 on Vigil V, proposed):

| Boss | Where it lives | Rewards | Phase 1 | Phase 2 | Phase 3 |
|---|---|---|---|---|---|
| The Hunger Below | A worm the size of a room, under act 2 | Momentum and moving builds | Bursts from the ground under wherever the player stood still for 1 s (circle radius 2.5, 0.8 s) | Two bursts at once, then a tail sweep (half-arena cone, 1.2 s) | Constant bursts every 2 s; standing is never safe |
| The Ashen Twins | Two bound knights with shared life, act 1 | Area and Sweep skills (hitting both at once) | They fight apart; the one farther from the player casts fire circles | If more than 6 apart, both heal 2 percent per second (a tether line shows it) | They merge fire rings around themselves (radius 3 each) that move with them |
| The Sexton of Tolls | A bell-ringer in the Bone Orchard's bell tower, act 3 | Stillness and standing builds | Rings the bell: a ring wave from its position every 5 s; 4 sigils on the floor halve damage taken while stood on | Sigils move every 10 s | Ring waves every 3 s, two sigils left |
| The Mother of Hounds | A giant hound bitch with her litter, act 4 | Execute and Charge skills (killing adds fast) | 6 Warhounds in her pack; she lunges (0.5 s line) | Howls every 20 s: 4 more hounds walk in from the gates | Frenzy: every living hound adds 10 percent to her attack speed |

### Shrines (proposed numbers)

05 above: a shrine is stepped on, one per shrine room, effects last 60 seconds. Numbers:

| Shrine | Effect for 60 s |
|---|---|
| Speed | Plus 25 percent move speed and 15 percent attack speed |
| Fury | Plus 30 percent damage |
| Warding | 25 percent less damage taken |
| Fortune | Plus 100 percent Magic Find (above the 200 percent gear cap, shrines are separate) |
| Focus | For the Wrathborn: Rage never drains and gains 50 percent more. The name comes from the Focus resource; renamed Wrath shrine (Q22) |

A shrine room holds one shrine on a plinth and 1 or 2 normal packs; shrines are marked on the mini-map once seen.

### Ambush rooms (decided, Q12)

05 above lists ambush rooms (5 percent). 01 and the structure decision say nothing spawns around the player and there are no waves. **Decided (Q12, 2026-09-27):** an ambush room's packs are placed at level load like every other pack, but lie dormant and visible (mounds of ash, bodies in the water, bone piles) around the room's edges; crossing the room's middle wakes them all at once from where they lie. Nothing is created during play.

### Story and scenes (proposed)

The outline above stands. Scenes are short (20 to 60 seconds), skippable at all times, and do not take control away in the dungeon: they play in town, at a boss's first sight (5 seconds, the boss name and one line), and after a boss kill. Format, **decided (Q20, 2026-09-27):** in-engine, the characters as sprites, text lines with a portrait, no voice acting. 25 scenes, about 20 minutes in total (proposed).

| Act | Opening (town) | Middle | Boss | Closing | Lore landmarks in the dungeon |
|---|---|---|---|---|---|
| 1 | The Wrathborn arrives at Emberwatch carrying the ember; Mother Aldis says the chapel's fire died in the night and the dead walk the crypts | The Wanderer (depth 3) gives the Portal Tome and speaks of the Vigil's other fires | The Cinder Warden: the keeper of the first fire, burned hollow | The Warden falls; the Wrathborn's ember takes its dying flame. Aldis sends him west to the Drowned Reach, where the second fire stood | 3 fallen watch-stones with carved names |
| 2 | Saltmere's ferryman Bram tells of the watchmen who walked into the sea | A drowned watch-log in the keep: the watchmen chose the water over the Hollow | The Tidewife: the watch-captain's widow, who called them into the water | The second ember. Bram ferries him to the Bone Orchard | Water-logged journals |
| 3 | Gravesend: Sister Ivy praises Saint Marrow, who "keeps the dead quiet" | Ivy learns the saint feeds the dead to the Hollow; she turns against him | Saint Marrow, who bargained with the Hollow for eternal life | The third ember, and Ivy's warning: the knights of the Spire serve the Hollow willingly | Saint's reliquaries with his sermons |
| 4 | Captain Hale explains the order's oath to the Hollow | Hale's old banner in the oath hall, his name struck out | Warlord Kaeth, who swore the oath first | The fourth ember. The way to the Hollow opens | Oath tablets |
| 5 | The survivors light the Ember Refuge's fire with the four embers | The broken first Vigil fire; the Wanderer is revealed as the First Watchman's shadow | The First Watchman, whose failure began the fall | The fire is relit; the Wrathborn stays as its keeper. Credits. Then Vigil II opens: "The Hollow remembers" | Memories: short text visions when a landmark is walked over |

Lore delivery: short item flavor text on every Legendary (one sentence), lore landmarks (walk-on, a hint banner with 1 or 2 lines, collected in a journal on the Character screen), NPC idle lines, and the scenes above. No text is required to play.

### Getting from act to act (proposed)

The act 1 boss already adds Stairs To Town (built). Proposed: from act 1 to 4, the boss kill also unlocks the next act's town on the Waystone map, and the stairs lead to that town the first time. Each act's town has its own Waystone; every waypoint of every act is on one map (06, Waystone map).

### Sleep (decided, Q14)

05 above says a sleep mechanic resets the session. **Decided (Q14, 2026-09-27):** a bed in each town's inn (a walk-up spot). Sleeping asks one confirmation, then gives every dungeon level of every act new seeds, restores killed enemies and closed chests, and keeps waypoints, the Portal Tome and the portal. Corpses stay, moved to the arrival point of their level. This is the only way to farm a finished level without rifts.

## Full-game plan: endgame (proposed, with the owner's decisions of 2026-09-27)

Written 2026-09-27. Rules kept: no online features, no weekly seeds or recurring live content (decided); everything is offline and local.

### After act 5

What keeps a player going, in the order they meet it: the next Vigil tier (04), the Watcher's rifts and Abyss (from the first act 5 kill), the boss rotation (from Vigil II), Legendary collection (a Codex on the Character screen listing every Legendary found, with the power text of those not yet found hidden), Paragon after level 60, achievements, and new builds from Legendaries that change skills.

### Rifts

Kept from above: 5 minute timed levels with a kill goal, keys from elites and bosses, a chest of 3 items with at least one Rare, two modifiers per key. Proposed details:

- Entry: the Watcher in any town. A key has a tier (the tier it dropped on) and 2 modifiers.
- Keys: 5 percent per elite, 100 percent per boss, 1 per completed rift (a chance at a higher tier's key: 20 percent).
- Layout: a generated level of 4 to 5 rooms with packs 1.5 times as dense, no chests or shrines; enemy level is the tier's act 5 depth 5 level (Vigil I: 22).
- Goal: a bar filled by kills (normal 1, champion 3, elite 8) to 150. At the goal the Rift Guardian, placed in the last room from the start and dormant until then, wakes (a 20 times life champion of the act's brute). Killing it in time gives the chest; failing keeps the drops and gives nothing more.
- Modifiers (two per key, from above plus proposed): Bloodlust (enemies plus 30 percent damage, plus 30 percent XP), Hexed (enemies curse: Cursing on every pack's leader), Frozen Ground (slowing ice patches), Swarm (packs 50 percent bigger), Champion Horde (every pack has a champion), Fortified (plus 50 percent enemy life, plus 50 percent gold), Volatile (normal enemies burst on death, radius 1.2, 0.8 s), Echoing (elites have 2 modifiers).

### The Abyss

Kept from above: an endless dungeon, one compact room per floor, 90 second timer per floor, a guardian every fifth floor, a boon from three every 10 floors, materials each floor, death ends the run, the deepest floor is kept as a personal best (local only, no leaderboard, decided). Proposed details:

- Entry: the Watcher. The player may start at floor 1 or at any multiple of 10 up to their best minus 10.
- Floor enemy level: the chosen tier's lowest level plus 2 per floor, up to 160 (03's item level cap). A floor is one room with 3 to 5 packs; the stairs down open when all are dead. The timer running out ends the run with what was collected.
- Guardians: an elite pack with 2 modifiers plus a champion leader on floors 5, 15, 25; an optional boss from the rotation on floors 10, 20, 30 and so on.
- Boons (pick 1 of 3 every 10 floors, for the rest of the run): plus 20 percent damage; plus 20 percent life; potion charges refill on every floor; plus 1 Momentum and Stillness cap; skills cost 25 percent less Rage; plus 50 percent Magic Find; the first death this run is prevented (once); elites drop an extra item; plus 15 percent attack speed; plus 10 percent cooldown reduction; ground telegraphs fill 15 percent slower; plus 2 levels to all skills.
- Death, **decided (Q13, 2026-09-27):** in the Abyss and rifts, death ends the run and returns the character to town with its gear, no corpse, since the level no longer exists.
- Cursed items drop from depth 30 onward (03).

### Boss rotation

Kept from above. Proposed: a page of the Waystone map (06) listing every boss killed at least once, at any tier unlocked. A fight costs one Boss Sigil (dropped by rift completions, 50 percent, and Abyss guardians, 100 percent); act bosses can also be fought for free the campaign way. The fight is the boss arena alone, with Stairs To Town after the kill. Optional bosses appear in the rotation from Vigil II (above; decided 2026-09-27, Q25).

### Achievements (60, local, no rewards: decided, Q17; the list is proposed)

| Group | Achievements |
|---|---|
| Story (10) | Kill each act boss (5); finish each Vigil tier (5) |
| Combat (10) | Clear a level without potions; clear an act without potions (05 above); kill 1,000 / 10,000 enemies; kill 100 elites; break a boss's stagger 3 times in one fight; kill a pack of 12 with one Ground Breaker; dodge 100 hits; win a boss fight without being hit by a ground shape; kill every optional boss |
| Build (10) | Buy both keystones; reach skill level 20; unlock every modifier of one skill; fill the passive tree; clear a level with no Sweep skill equipped (05 above's example, adapted); 5 Momentum stacks for 60 s; 5 Stillness stacks through a boss phase; reach 200 percent Magic Find; reach 50 percent cooldown reduction; clear Abyss floor 50 with a Momentum-leaning build (05 above's Fortress example, adapted to the Wrathborn) |
| Loot (12) | Find a Legendary; find 10 / all 40 Legendaries (Codex); find a Cursed item; equip a full set of 10 Legendaries or Rares; temper an affix to T1; reforge 50 times; socket a Perfect gem; salvage 1,000 items; fill the stash; wear a Legendary in every shown slot (replaces "transmog every shown slot": Transmog comes after launch, Q24); find a T1 affix |
| Endgame (10) | Abyss floors 10, 25, 50, 75, 100; complete 10 / 100 rifts; complete a rift in under 3 minutes; kill a boss from the rotation on Vigil V; Paragon 50 |
| Exploration (8) | Activate every waypoint in an act (5 achievements, one per act); read every lore landmark; walk up to every NPC in every town; recover your corpse 10 times |

## Audio and music plan

Proposed, see `07-technical.md`, "Audio and haptics plan (proposed)".
