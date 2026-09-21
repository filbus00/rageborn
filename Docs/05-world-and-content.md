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

Story is delivered in about 30 minutes of short scenes across the campaign. Skippable at all times.

## Structure

| Act | Levels | Player level | Boss | Signature enemies |
|---|---|---|---|---|
| 1 Ashfields | 6 | 1 to 12 | Cinder Warden | Husks, ghouls, wolves, bandit archers |
| 2 Drowned Reach | 6 | 12 to 24 | The Tidewife | Drowned, leech swarms, marsh casters |
| 3 Bone Orchard | 6 | 24 to 36 | Saint Marrow | Skeleton knights, bone chargers, grave priests |
| 4 Iron Spire | 6 | 36 to 48 | Warlord Kaeth | Armored knights, siege brutes, banner bearers |
| 5 The Hollow | 6 | 48 to 60 | The First Watchman | Void wraiths, corrupted allies, elite mixed packs |

Each act has one town above one dungeon. The dungeon is descended level by level: the exit room of each level holds stairs down to the next, and its start room holds stairs up (on the first level they lead back to the town).

## Town

Each act has one town, a small safe scene the player walks through with the stick. There are no enemies and no combat. NPCs stand at fixed spots for the Forge, the Stash, the Class Trainer and the Waystone. Walking up to an NPC opens its bottom sheet panel, so nothing in the world needs a tap. The dungeon entrance is a stairway the player walks into. A new character begins in the act 1 town, at the ruined chapel. The Waystone jumps to any activated waypoint, see Getting around below.

### Getting around

- Stairs: the exit room of each level holds stairs down and the start room holds stairs up.
- Waypoints: each level has a waypoint the player activates by stepping on it. From any activated waypoint, or from the Waystone in town, the player can jump to any other activated waypoint. A list screen opens from the waypoint or the Waystone. Rifts and the Abyss are entered from an NPC in town.
- Portal scroll: a consumable the player uses from the UI. It opens a portal to town, and the portal stays open so the player can come back through it. The first scrolls are found on the early levels of the dungeon.
- Persistence: within a game session a level keeps its seeded layout, its dead enemies and its opened chests when the player leaves and comes back. A sleep mechanic resets the session. Its rules are not designed yet.
- Death: the character is sent to town, see 01-core-gameplay.md.

## Level generation

Levels are built from hand-authored rooms joined by a seeded generator.

- Room library: 30 rooms per act, each 20 by 20 units, three sizes.
- Layout: a start room with the stairs up, 5 to 8 rooms of mixed type, an exit room with the stairs down. A level takes 4 to 8 minutes to clear.
- Room types: combat (60 percent), elite (15), treasure (10), shrine (10), ambush (5).
- Every level has one guaranteed elite pack and one guaranteed chest. The act boss waits on the last level of each act.
- Packs are placed by the generator and idle until the player comes within aggro range, see 01-core-gameplay.md.
- Mini-map: a small overlay at the top corner showing explored rooms, chests and the exit arrow. It is read-only.

### Shrines

A shrine is stepped on to activate. Effects last 60 seconds: Speed, Fury (damage), Warding (damage reduction), Fortune (Magic Find), Focus (regeneration). One shrine per room at most.

## Enemy roster (launch)

Count: 40 normal enemy types, 15 elite modifiers used across them, 5 act bosses, 4 optional bosses. Each enemy has a sprite set with 8 direction facing, 4 states, and 2 palette swaps per act.

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
- Rift modifiers, two per key: Bloodlust, Hexed (enemies curse), Frozen Ground, Swarm, Champion Horde.

### Abyss

- An endless dungeon. Each floor is a compact room with a 90 second timer, a guardian on every fifth floor.
- Floor enemy level rises 2 per floor. Every 10 floors the player picks a boon from three, active for the rest of the run.
- Rewards: materials on each floor, item drops from guardians, Ember Shard at depth milestones.
- Death ends the run. Depth is submitted to a Game Center leaderboard.

### Boss rotation

A list of all bosses, each usable at any unlocked difficulty tier. A boss can be fought repeatedly for loot. Boss keys drop from rifts and Abyss guardians.

### Achievements

Around 60 Game Center achievements. Examples: clear each act without using a potion, reach Abyss depth 50 with a Fortress build, collect all class legendaries, win with a build that has no active skill of a certain tag.

## Audio and art direction

- Art: 2D hand-painted look, 256 by 256 sprites for standard enemies, 512 by 512 for bosses, isometric perspective. Environments are tile-based with lit layers.
- Lighting: dark scenes lit by the player's ember and enemy effects. Dynamic 2D lights on the player and a few props only, for performance.
- Music: sparse, low strings, choir, drum layers. Layered stems respond to pack size and elite presence.
- Sound: heavy, tactile, low frequency emphasis. Rarity-specific pickup sounds.

## Content counts for 1.0

| Category | Count |
|---|---|
| Acts | 5 |
| Zones | 30 |
| Hand-authored rooms | 150 |
| Normal enemies | 40 |
| Bosses | 9 |
| Classes | 3 |
| Active skills | 24 |
| Skill modifiers | 216 |
| Legendary items | 60 |
| Affixes | 90 |
| Gems | 4 types, 5 tiers |
| Achievements | 60 |
