# Classes and skills

> 1.0 ships one class, the Wratborn, a barbarian-style warrior. Which of the three drafts below it reworks or replaces is an open question, see 08-production.md. The other classes are content for after launch.

## Design rules

- The player never presses a skill. Every skill must be useful when fired by the auto-cast system.
- Skill identity comes from area, trigger condition and modifiers, not from timing.
- Each class has 8 active skills, 4 equipped at a time, plus 1 keystone that changes a rule of the class.
- Skills level from 1 to 20 by spending skill points and gain modifier slots at levels 5, 10 and 15.
- Class base stats differ. Gear can push any class toward any playstyle.

## Attributes

| Attribute | Effect per point |
|---|---|
| Might | +0.5 percent melee and area damage, +1 armor |
| Agility | +0.4 percent attack speed, +0.2 percent crit chance, +0.15 percent dodge |
| Will | +0.5 percent spell damage, +0.5 percent Focus regeneration, +0.3 percent cooldown reduction |
| Vitality | +8 life, +0.1 percent life regeneration per second |

Diminishing returns apply to attack speed above 3.0 per second, cooldown reduction above 40 percent and dodge above 35 percent (hard cap 50 percent).

## Class 1: Warden (melee, Stillness leaning)

Base: high life, medium speed, short range (2.0 units), strong armor.
Basic attack: sword or mace sweep in a 120 degree arc.
Keystone options: Bastion (Stillness stacks to 8, movement halves stacks lost), Vengeful Ward (damage taken is stored and released as an area pulse every 4 s).

| Skill | Type | Cost | Cooldown | Description |
|---|---|---|---|---|
| Cleave | Sweep | 15 Focus | 2.5 s | 200 degree sweep for 180 percent weapon damage |
| Shield Bash | Single | 20 Focus | 5 s | Stuns the target for 1 s, 250 percent damage |
| Earthshatter | Area | 35 Focus | 8 s | Ground slam, 4 unit radius, 320 percent, slows 30 percent |
| Iron Vow | Buff | none | 18 s | 6 s of plus 40 percent armor and 15 percent damage reduction |
| Chain Hook | Pull | 25 Focus | 10 s | Pulls up to 5 enemies within 8 units to the player |
| Banner of Ash | Ground | 30 Focus | 14 s | Plants a banner, 5 unit radius, allies and player gain 20 percent damage for 8 s |
| Whirlwind Vigil | Channel | 40 Focus | 12 s | 3 s spin, moves at 60 percent speed, 100 percent per 0.3 s |
| Judgment | Execute | 30 Focus | 6 s | Strikes target for 400 percent, 900 percent if the target is below 25 percent life |

## Class 2: Ranger (ranged, Momentum leaning)

Base: medium life, high speed, long range (7 units), high dodge.
Basic attack: a bow shot, single projectile, pierces 0 by default.
Keystone options: Windrunner (Momentum stacks to 8, each also gives 3 percent damage), Deadeye (critical hits refund 5 Focus, first attack after standing still crits).

| Skill | Type | Cost | Cooldown | Description |
|---|---|---|---|---|
| Multishot | Cone | 20 Focus | 3 s | Fires 5 arrows in a 45 degree cone, 90 percent each |
| Piercing Bolt | Line | 25 Focus | 5 s | One arrow pierces all enemies, 300 percent |
| Caltrops | Ground | 15 Focus | 7 s | Field of spikes, 3 unit radius, 6 s, slows 40 percent, deals 60 percent per second |
| Rolling Volley | Movement | none | 9 s | While moving, fires arrows backward in a fan for 4 s |
| Hunter's Mark | Debuff | 10 Focus | 12 s | Marks an elite or boss, marked target takes 25 percent more damage for 8 s |
| Explosive Trap | Ground | 30 Focus | 10 s | Trap detonates when an enemy nears, 5 unit radius, 350 percent |
| Falcon | Summon | none | 20 s | A falcon attacks on its own for 12 s at 80 percent weapon damage |
| Storm of Arrows | Area | 40 Focus | 15 s | 2 s delay, then 3 s of arrows at a marked area, 7 unit radius |

## Class 3: Hexer (caster, mixed)

Base: low life, high Focus regeneration, medium range (5 units), strong cooldown reduction.
Basic attack: a dark bolt that seeks the nearest target.
Keystone options: Blood Pact (skills cost life instead of Focus, plus 50 percent damage), Void Conduit (each skill cast on a target adds 1 stack of Void, at 10 stacks the target takes a burst).

| Skill | Type | Cost | Cooldown | Description |
|---|---|---|---|---|
| Soul Lance | Line | 20 Focus | 3 s | Spear of shadow, 280 percent, pierces |
| Plague Cloud | Ground | 25 Focus | 8 s | Poison cloud, 4 unit radius, 6 s, 80 percent per second |
| Bone Cage | Control | 30 Focus | 12 s | Traps enemies in radius 3 for 2 s |
| Raise Thrall | Summon | 30 Focus | 10 s | Summons 2 skeletons for 15 s |
| Curse of Frailty | Debuff | 15 Focus | 9 s | Enemies in radius 5 deal 20 percent less damage, take 15 percent more |
| Blood Nova | Area | 10 percent life | 6 s | Burst around the player, 5 unit radius, 300 percent, heals 3 percent of life per enemy hit |
| Phantom Step | Movement | none | 10 s | On trigger, teleports 4 units away from the nearest enemy. Uses the Life below 50 percent condition by default |
| Soul Harvest | Passive-active | none | 20 s | Consumes corpses in range for 2 s to restore 30 Focus and 10 percent life |

## Skill modifiers

Each skill has three modifier slots unlocked at levels 5, 10 and 15. The player picks one modifier from a group of three at each unlock. Examples for Cleave:

| Level | Options |
|---|---|
| 5 | Wide Arc (sweep becomes 270 degrees), Bleeding Edge (applies bleed, 60 percent over 4 s), Rending (reduces enemy armor 10 percent for 4 s) |
| 10 | Follow-through (second sweep at 60 percent), Momentum Strike (in Stillness, plus 10 percent damage per stack), Reaping (heals 1 percent life per enemy hit) |
| 15 | Shockwave (sweep launches a projectile), Sundering (crits reduce cooldown by 0.5 s), Bastion Cut (grants 5 percent damage reduction on cast for 3 s) |

All 24 skills follow this pattern, giving 72 modifiers per class, 216 across the game.

## Skill gems and sockets (interaction with gear)

Skills are not socketed into gear. Gear affixes can target a skill by name (for example, plus 2 levels to Cleave) or by tag (Sweep, Area, Channel, Summon, Movement, Ground, Debuff, Control). A skill can have several tags. Tag scaling is the main way a legendary item reshapes a build.

## Passive tree

Each class has a tree of 60 nodes over three branches. Nodes cost one passive point. The player gains one passive point per level from level 2, up to 59 points at level 60, so they can nearly complete the tree. Paragon points add stats but not nodes.

Node types:

| Type | Count | Example |
|---|---|---|
| Minor | 36 | Plus 3 percent life, plus 2 percent crit |
| Notable | 18 | Warden: Unmoving, Stillness stacks give 3 percent life regeneration each |
| Keystone | 3 | Class keystones listed above, choose one, cost 3 points |
| Gateway | 3 | Locks the next branch until a condition is met, for example 30 points spent |

Respec costs gold that scales with level, capped at 5,000. Free respec is available once per act clear during the campaign.

## Build archetypes (for balance targets)

| Archetype | Class | Gear tags | Play |
|---|---|---|---|
| Fortress | Warden | Stillness, armor, Earthshatter | Plants, stacks Stillness, lets packs come |
| Whirl kiter | Warden | Channel, Momentum, movement speed | Runs in circles, channels while moving |
| Glass cannon | Ranger | Crit, Piercing Bolt, Deadeye | Stops to fire, hits hard |
| Trapper | Ranger | Ground, Explosive Trap, Caltrops | Lays a field, kites into it |
| Necromancer | Hexer | Summon, Curse, corpse gain | Stays back, lets thralls fight |
| Blood mage | Hexer | Life cost, Blood Nova, leech | Wades in, sustains through leech |

Balance target: at equal gear level, the median time to clear a standard zone should stay within 20 percent across archetypes.
