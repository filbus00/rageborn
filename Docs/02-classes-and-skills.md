# Classes and skills

> 1.0 ships one class, the **Wild Arrow**, a bow-only ranged hunter (decided 2026-09-30, below: the game uses only bows). It replaces the Wrathborn, the barbarian launch class designed on 2026-09-26, which stays below as reference with the Warden draft before it. The Ranger draft is folded into the Wild Arrow; the Hexer stays a draft for a later class.

## Design rules

- The player never presses a skill. Every skill must be useful when fired by the auto-cast system.
- Skill identity comes from area, trigger condition and modifiers, not from timing.
- Each class has 8 active skills, 4 equipped at a time, plus 1 keystone that changes a rule of the class.
- Skills level from 1 to 20 by spending skill points and gain modifier slots at levels 5, 10 and 15.
- Class base stats differ. Gear can push any class toward any playstyle.

## Attributes (the retired classes'; the Wild Arrow's are in its section)

| Attribute | Effect per point |
|---|---|
| Might | +0.5 percent melee and area damage, +1 armor |
| Agility | +0.4 percent attack speed, +0.2 percent crit chance, +0.15 percent dodge |
| Will | +0.5 percent spell damage, +0.5 percent Focus regeneration, +0.3 percent cooldown reduction |
| Vitality | +8 life (no life regeneration: decided 2026-09-28, 08 open question 10) |

Diminishing returns apply to attack speed above 3.0 per second, cooldown reduction above 40 percent and dodge above 35 percent (hard cap 50 percent).

## Class 0: Wild Arrow (ranged, bows only, Momentum leaning) — the launch class

Decided 2026-09-30 (08-production.md, decision log): the game becomes an action RPG that only uses bows. The launch class is a new hero and class, the **Wild Arrow**; the Wrathborn is retired (kept below as reference, like the Warden). The owner went through the design part by part the same day; what is marked **Decided** below is the owner's, the rest (numbers above all) is Claude's proposal, built as tuning until the owner changes it.

**Decided (2026-09-30):** a new hero and class, not the Wrathborn rethemed; Focus as the resource; short bows and longbows with a quiver in the off-hand; a clean start for saves; the Wrathborn's body with the bandit archer's Mixamo moveset as the stand-in art (09, 4.6); how a fight plays and Focus as below; four skills for now (Split, Pierce, Homing and Explosive Arrow), the rest held; **no passive tree and no keystones for now**: stat points spent on five attributes instead (Attributes, below); a **pet** bought from a vendor in town (Pets, below).

### How a fight plays (decided)

Basic attack: a bow shot, one arrow at the target, 14 units a second, that stops at the first enemy it meets or a wall. Reach 7.5 with a short bow, 9 with a longbow (03). The target must be in sight (no wall on the line from the character to it). Attacks per second start at 1.4 and continue at the full rate while moving (01). Off-hand: a quiver, or nothing (03).

Enemy aggro range is 7 and the short bow reaches 7.5, so the character can open on a pack from just outside it; the pack then comes (a hit wakes a pack, as built). Intended: the fight starts on the player's terms, and swarmers close 7 units in under 2 s. Lean: Momentum, by the rules in 01: a bow character kites, and Momentum's speed and dodge are its defence. Stillness still works for a player who plants and shoots.

### Resource: Focus (decided)

01's rules: 0 to 100, **full on every level load**, regenerates 6 a second, and a basic arrow that hits gives 4. Skills cost Focus. A fight opens on skills and the basic shots keep it going, the opposite rhythm of the Wrathborn's Rage. Shown as the arc under the character's feet.

### Skills (decided: these four for now)

| Skill | Unlock | Type | Cost | Cooldown | Trigger | Description |
|---|---|---|---|---|---|---|
| Split Arrow | 1 | Cone | 12 Focus | 3 s | 2 or more enemies in reach inside the forward cone | 5 arrows in a 40 degree fan at the target, 90 percent weapon damage each; each stops at the first enemy it meets |
| Pierce Arrow | 2 | Projectile | 15 Focus | 4 s | An enemy 4 to 9 units away in sight | One arrow that passes through every enemy on its line for 200 percent; walls stop it |
| Homing Arrow | 4 | Projectile | 15 Focus | 5 s | An enemy in reach and in sight | 3 arrows that curve after their targets, 120 percent each; each picks a different enemy when it can. The target must be in sight when they are loosed (the owner: no shots around corners); a wall still stops an arrow |
| Explosive Arrow | 6 | Area | 30 Focus | 7 s | 3 or more enemies within 2.5 units of a target in sight | One arrow that bursts on hitting an enemy or a wall: 220 percent to everything within 2.5 units |

Slot order, which is cast priority (proposed): Explosive, Homing, Pierce, Split, so the area skill fires first on a crowd. Numbers, unlock levels and the order are proposals.

Each is one of the skill kinds the Wrathborn's were, so the auto-cast system, triggers, skill levels and loadout carry over: Split Arrow in the sweep's place (a cone in reach), Pierce Arrow in the projectile's (Hurl Axe, now piercing), Homing Arrow a projectile with steering (new: the arrow turns toward its target each step, at most 360 degrees a second, tuning), Explosive Arrow the area's (Ground Breaker, centred on the arrow's impact instead of the character).

Held for later (the owner, 2026-09-30: "hold it with the skills"): the other four skill slots. Proposed during the walkthrough and kept here as candidates: **Knockback Shot** (the owner's pick to replace an auto-leap, Vault: a heavy arrow at the closest enemy, 150 percent, knocks it and everything within 1.5 back 3 units, free, gains 15 Focus, 6 s, when an enemy is within 2.5), Hunter's Breath (a Focus refill), Barrage (a moving multi-shot), Wild Frenzy (attack speed from Momentum), Kill Shot (an execute). The first four take the loadout's four slots, so the loadout screen has nothing to choose until more skills exist.

Alternative triggers (Q15, proposed): Split Arrow Always and Elite present; Pierce Arrow Elite present and Standing; Homing Arrow Elite present and 3+ enemies; Explosive Arrow Always and Elite present.

**Decided (the owner, 2026-10-04):** the held slots are filled with all five candidates, **Knockback Shot, Barrage, Kill Shot, Hunter's Breath and Wild Frenzy**, so the class has nine skills and equips four; **every skill unlocks at level 1** ("All at once"), so the loadout opens at level 1 too (replacing Q4's level 9). Built the same day; the numbers are Claude's, to review:

| Skill | Kind | Cost | Cooldown | Trigger | Description |
|---|---|---|---|---|---|
| Knockback Shot | KnockbackShot | free, gains 15 Focus | 6 s | An enemy within 2.5 | A heavy arrow at the nearest enemy, 200 percent; it and every enemy within 1.5 are thrown back 3 units |
| Barrage | Barrage | 20 | 7 s | 2 or more enemies in 7, while moving | 8 arrows over 1.2 s at the enemies in reach, 80 percent each, while she keeps moving and shooting |
| Kill Shot | KillShot | 15 | 4 s | An enemy below 30 percent life in reach and sight (or an elite or boss) | One fast arrow, 200 percent; 600 percent and a certain crit on a target below 30 percent when it lands |
| Hunter's Breath | Buff | free | 15 s | Focus below 30 with an enemy within 6 | Gains 60 Focus at once |
| Wild Frenzy | Buff | 10 | 12 s | 3 or more Momentum stacks in a fight | 8 s of 25 percent attack speed, plus 5 percent per live Momentum stack |

An unchosen loadout keeps the first four (Explosive, Homing, Pierce, Split); the new five follow them in the class order, so they are equipped from the loadout screen.

Skill levels: the Wrathborn's rules stand (below, "Skill levels 1 to 20"): a skill starts at 1 when it unlocks, a skill point raises it one level, capped at character level minus unlock level plus 1 and at 20; each level adds 7 percent of the level 1 damage multiplier; costs and cooldowns stay. Modifiers are not proposed yet.

Skill tags (proposed): Split Arrow Projectile, Cone; Pierce Arrow Projectile, Pierce; Homing Arrow Projectile, Homing; Explosive Arrow Projectile, Area. 03's skill level affixes follow them.

### Attributes and stat points (decided 2026-09-30: no passive tree)

The owner: "No skill trees, add stat points to spend on attributes", with the attributes "Strength, agility, vitality, speed, and one more", the fifth chosen as Focus. This replaces the passive tree and the automatic attribute growth (Q6) for the Wild Arrow; the Attributes table above is the retired classes'. Effects per point (proposed, the owner approved the split):

| Attribute | Effect per point |
|---|---|
| Strength | +0.5 percent damage on every arrow, +1 armor |
| Agility | +0.2 percent crit chance, +0.2 percent dodge, +1 percent crit damage |
| Vitality | +8 life (no regeneration, 08 open question 10) |
| Speed | +0.4 percent attack speed, +0.2 percent move speed |
| Focus | +0.5 percent Focus regeneration, +0.3 percent cooldown reduction, +1 max Focus |

- Every attribute starts at 10, and the bonuses count from 0 (the starting 10 are part of the class's base, not extra).
- 5 points a level from level 2, so 295 at level 60 (proposed; Diablo 2's number). The points are spent from a stats page in the Bag, a + beside each attribute; unspent points show as a badge on the Bag button, as skill points do.
- No respec for now (proposed; the Trainer could offer it for gold later). The diminishing returns above (attack speed, cooldown reduction, dodge, capped at 50) stand.
- Balance: the class keeps the Wrathborn's life by level (the stand-in 8 Vitality a level, 64 life), and Vitality points add their 8 life each on top, so a character who never raises Vitality has the life the balance was built on. The balance report assumes one point in five on each attribute until measured.

### Pets (decided 2026-09-30)

The owner: "Add a pet vendor in town. Pet is bought and levels with the player", later: a few kinds, one active, and a pet fights, draws aggro, picks up loot, gives a passive bonus and is customizable ("which enemy to attack, what to do depending on context etc"). Details proposed:

- **The Pet Vendor**, a new NPC in every town (05, Towns), walked up to like the others. Its sheet lists the kinds, buys one for gold, switches the active pet and holds the pet's rules. Owned pets are kept; switching is free.
- **Kinds** (proposed, three at launch, each playing differently): **Wolf** (a biter that holds enemies: plus 5 percent life to the character), **Raven** (fast and fragile, fetches loot from far away: plus 10 percent Magic Find), **Boar** (slow, tough, draws aggro: plus 10 percent armor). Prices 500, 1,500 and 3,000 gold (tuning).
- **Levels with the player:** a pet's level is always the character's level; its life and damage follow the enemy curves at that level times its kind's multipliers (tuning). It needs no XP of its own.
- **Fights:** it attacks an enemy by its rules (below), 80 percent weapon damage per hit (tuning).
- **Draws aggro:** an enemy within 2 units of the pet may attack it instead of the player (the Boar most). At 0 life the pet is knocked out and comes back beside the player after 20 s; it never dies for good.
- **Picks up loot:** out of combat it fetches gold and items within a wide radius (Raven 12, others 6) and brings them to the character, on the same rules as auto-loot (01, 03).
- **Passive bonus:** the kind's bonus above, while it is the active pet (knocked out or not).
- **Rules** (customizable, the owner's ask): a short list of if-then rules picked from chips, evaluated in order like the skill slots' triggers (01), no typing and one thumb: targets (what I attack; the nearest; elites first; the weakest; whatever attacks me) and behaviours (guard me when my life is below 50 percent; fetch loot only when no enemy is near; stay close; hold back from bosses). Default: attack what I attack, fetch loot out of combat.
- Enemy AI today targets only the player: choosing the pet is new work for every archetype.

## Retired: Wrathborn (melee, Momentum leaning) — the launch class from 2026-09-26 to 2026-09-30

Retired on 2026-09-30 when the game became bows only (08). Kept as reference: the Wild Arrow reuses his skill kinds, triggers, skill levels, tree layout and keystone rules.

Decided 2026-09-26 (08-production.md, decision log): Rage as the resource, a Momentum lean, an all-new barbarian skill set (none of the Warden's skills), and for the M1 slice the first 4 skills unlocking by level. All numbers are starting tuning.

Base: melee, reach 2.0 units, basic attack a 120 degree sweep, as the Warden's. Off-hand (decision of 2026-09-27): nothing, an off-hand weapon, a shield, or a two-handed weapon instead; each grip has its own animations (03-itemization.md, Appearance).

Resource, Rage (replaces Focus for this class): 0 to 100, starts empty. A basic attack that hits gains 6 (once per swing), each hit taken gains 3. After 3 seconds without dealing or taking a hit it drains 5 per second. The basic attack and Bull Rush are the generators; every other skill but Battle Roar spends Rage, so a fight opens on plain swings or a charge and builds.

Lean: Momentum, by the rules in 01-core-gameplay.md. Bull Rush adds stacks and Blood Frenzy and the Juggernaut keystone feed on them.

| Skill | Type | Cost | Cooldown | Trigger | Description |
|---|---|---|---|---|---|
| Hew | Sweep | 20 Rage | 3 s | 2 or more enemies in reach | 180 degree sweep for 170 percent weapon damage |
| Bull Rush | Charge | none, gains 15 Rage | 6 s | Moving, an enemy 3 to 6 units ahead | Charges through the line, 150 percent to everything hit and knockback, plus 1 Momentum stack per enemy hit. The opener: it builds Rage instead of spending it (decision of 2026-09-26) |
| Hurl Axe | Projectile | 10 Rage | 4 s | An enemy 4 to 9 units away in sight | A thrown axe, 200 percent to the first enemy hit |
| Ground Breaker | Area | 35 Rage | 8 s | 4 or more enemies within 3.5 units | Slam, 3.5 unit radius, 280 percent, slows 30 percent for 2 s |
| Battle Roar | Buff | none | 15 s | 3 or more enemies near and Rage below 30 | Gains 40 Rage and plus 20 percent damage for 6 s |
| Rending Spin | Channel | 40 Rage | 12 s | Moving, 3 or more enemies near | 2.5 s spin at full movement speed, 90 percent every 0.3 s, applies a bleed |
| Blood Frenzy | Buff | 20 Rage | 14 s | In combat with 3 or more Momentum stacks | Plus 30 percent attack speed for 6 s, plus 5 percent more per Momentum stack |
| Skullsplitter | Execute | 25 Rage | 5 s | A target below 25 percent life, or an elite or boss | 350 percent to one target, 800 percent below 25 percent life |

Keystones: Berserker (below 50 percent life, plus 30 percent damage and double Rage gain), Juggernaut (each Momentum stack also gives 3 percent damage reduction, and stopping loses half the stacks instead of all).

M1 vertical slice: the first four unlock and equip themselves as the character levels through act 1: Hew at level 1, Hurl Axe at 2, Bull Rush at 4, Ground Breaker at 6. Slot order, which is cast priority: Ground Breaker, Hurl Axe, Bull Rush, Hew, so a big crowd gets the slam before Hew spends the Rage (decision of 2026-09-26). Battle Roar, Rending Spin, Blood Frenzy, Skullsplitter, the keystones, skill points and levels, modifiers and the loadout screen come after the slice.

## Class 1: Warden (melee, Stillness leaning) — draft, replaced by the Wrathborn

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

## Class 2: Ranger (ranged, Momentum leaning) — draft, folded into the Wild Arrow

The Wild Arrow (above) takes the bow role; the ideas below not used by it (Caltrops, traps, the Falcon) stay here for modifiers or later.

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

## Passive tree (the retired Wrathborn's; the Wild Arrow has none, decided 2026-09-30)

Each class has a tree of 60 nodes over three branches. Nodes cost one passive point. The player gains one passive point per level from level 2, up to 59 points at level 60, so they can nearly complete the tree. Paragon points add stats but not nodes.

Node types:

| Type | Count | Example |
|---|---|---|
| Minor | 36 | Plus 3 percent life, plus 2 percent crit |
| Notable | 18 | Warden: Unmoving, Stillness stacks give 3 percent life regeneration each |
| Keystone | 2 for the Wrathborn (the Wild Arrow has no tree, decided 2026-09-30) | Class keystones listed above, 3 points each, both can be bought, from level 20; the loadout picks the active one (decided 2026-09-27, Q4 and Q5; the drafts for later classes list 2 options each) |
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

## Full-game plan: the Wrathborn in full (retired with the class on 2026-09-30; the rules the Wild Arrow keeps are named in its section)

Written 2026-09-27 for the full-game plan (`10-full-game-plan.md`). The Wrathborn's eight skills and two keystones were accepted as written on 2026-09-26, so their table above is not changed: where this section expands a one-line entry it only fills in what the line leaves open (radius of "near", animation, timing), and every new effect is a modifier, not a change to the skill. All numbers below are proposals and tuning values, except where a paragraph is marked **Decided** (the owner's answers of 2026-09-27 to the questions numbered as in `08-production.md`, "Open questions from the full-game plan").

### Skill tags (proposed)

Gear scales skills by tag (above, "Skill gems and sockets"). The Wrathborn uses eight tags, four of them from the list above (Sweep, Area, Channel, Movement) and four new (Melee, Projectile, Buff, Execute).

| Skill | Tags |
|---|---|
| Hew | Melee, Sweep |
| Bull Rush | Melee, Movement |
| Hurl Axe | Projectile |
| Ground Breaker | Melee, Area |
| Battle Roar | Buff |
| Rending Spin | Melee, Sweep, Channel, Movement |
| Blood Frenzy | Buff |
| Skullsplitter | Melee, Execute |

### The four skills after the slice (details proposed)

**Decided (Q4, 2026-09-27):** they unlock at levels 9, 12, 15 and 18 (Battle Roar, Rending Spin, Blood Frenzy, Skullsplitter); the built four keep 1, 2, 4 and 6, and all four slots are open from level 1.

| Skill | From the table above (decided) | Filled in (proposed) |
|---|---|---|
| Battle Roar | Buff, no cost, 15 s, 3 or more enemies near and Rage below 30; gains 40 Rage and plus 20 percent damage for 6 s | "Near" is within 5 units, the same area the other buffs use. The damage is an increased modifier (03's formula, step 2). Cast time 0.4 s, 12-frame roar animation; the global cast timer applies. A red ring pulses once from the player, no telegraph needed since it hurts no one. It is the only skill that can fire when Rage is too low for everything else, so it sits well in slot 1 or 2 |
| Rending Spin | Channel, 40 Rage, 12 s, moving and 3 or more enemies near; 2.5 s spin at full movement speed, 90 percent every 0.3 s, applies a bleed | "Near" is within 4 units. The spin hits everything within the basic reach (2.0) each tick: 8 ticks. The bleed deals 40 percent weapon damage over 4 s, refreshed by later ticks, not stacked. The basic attack pauses during the spin and, by 01's channel rule, lower slots wait until it ends. Moving during the spin counts as moving for Momentum. A looping 8-frame spin animation in 16 directions is unnecessary: the spin is one direction-free loop (09) |
| Blood Frenzy | Buff, 20 Rage, 14 s, in combat with 3 or more Momentum stacks; plus 30 percent attack speed for 6 s, plus 5 percent more per Momentum stack | "In combat" means a hit dealt or taken within the last 3 s, the same test as the Rage drain. The per-stack part follows the live stack count during the 6 s (a player who keeps moving keeps it). Cast time 0.2 s; a red vapour on the character while it lasts |
| Skullsplitter | Execute, 25 Rage, 5 s, a target below 25 percent life, or an elite or boss; 350 percent to one target, 800 percent below 25 percent life | The target must be within the basic reach plus 0.5. Below 25 percent is read when the blow lands, 0.3 s after the cast, so a target that drops under during the wind-up gets the 800. Against a boss the 800 applies below 25 percent of its life, which also falls in phase 3. 12-frame overhead chop |

### Skill levels 1 to 20 (proposed)

- A skill starts at level 1 when it unlocks. Each skill point raises one skill by one level. A skill's level cannot exceed the character's level minus its unlock level plus 1, so points cannot all go into one skill at level 2.
- Points: 1 per level to 30, then 1 per 2 levels (04), 44 by level 60. Maxing four skills costs 76 points, so a character at 60 has about three skills at 20 or eight at about level 6: a real choice.
- Gear adds levels (03, the skill level affixes) on top, up to level 25.
- Scaling per level above 1: damage skills gain 7 percent of their level 1 multiplier (Hew 170 percent at level 1, 289 at 10, 396 at 20). Battle Roar gains 1 percent damage per level (20 to 39 percent). Blood Frenzy gains 1 percent attack speed per level (30 to 49). Bull Rush's Rage gain, every Rage cost and every cooldown stay fixed, so the loadout's rhythm does not change with levels.
- Modifier slots open at skill levels 5, 10 and 15 (above). Picking one of three is free and can be changed at the Trainer for the respec price (04).

### Skill modifiers (proposed: 72 options, 24 choices)

One pick from three at each of skill levels 5, 10 and 15.

| Skill | Level 5 | Level 10 | Level 15 |
|---|---|---|---|
| Hew | Wide Arc (sweep becomes 270 degrees); Rending Edge (bleed, 40 percent over 4 s); Sunder (hit enemies take 10 percent more damage for 4 s) | Follow-through (a second sweep at 60 percent); Reaping (heal 1 percent life per enemy hit); Rage Feeder (refund 4 Rage per enemy hit, up to 20) | Shockwave (the sweep sends a 5-unit wave, 60 percent); Momentum Cleave (plus 8 percent damage per Momentum stack); Echo (cooldown 1 s shorter when it hits 4 or more) |
| Bull Rush | Trample (reaches targets up to 9 away); Shoulder Guard (20 percent less damage taken during the dash and 2 s after); Stampede (gains 25 Rage instead of 15) | Double Rush (charges back through the line within 1 s, 60 percent); Stunning Impact (the first enemy hit is stunned 1 s); Wake of Fire (burning trail for 3 s, 30 percent per second) | Unstoppable (cooldown 2 s shorter per elite hit); Momentum Surge (sets Momentum to its cap); Breaker Finish (ends in a slam, radius 2, 100 percent) |
| Hurl Axe | Ricochet (bounces to 2 more enemies at 70 percent); Heavy Axe (plus 30 percent damage and knockback); Quick Throw (cooldown 3 s) | Twin Axes (two axes in a 15 degree fan); Returning Axe (flies back, hitting again at 70 percent); Crippling (slows 40 percent for 3 s) | Piercing (passes through every enemy; walls still stop it); Rage Throw (free while Rage is below 30); Marked (the target takes 15 percent more damage for 5 s) |
| Ground Breaker | Wide Crater (radius 4.5); Aftershock (a second slam 1 s later, 50 percent); Quake (stuns 0.8 s instead of slowing) | Fissures (3 cracks run 6 units outward, 80 percent); Hair Trigger (fires with 3 enemies instead of 4); Rage Quake (costs 25 Rage) | Seismic (consumes Momentum: plus 10 percent damage per stack); Tremor Field (the crater slows 50 percent for 4 s); Crusher (plus 40 percent against elites and bosses) |
| Battle Roar | Rallying (plus 15 percent move speed while active); Intimidate (enemies within 5 deal 20 percent less damage for 4 s); Deep Breath (gains 60 Rage) | War Cry (lasts 9 s); Second Wind (heals 10 percent life); Dread (enemies within 4 flee for 1.5 s) | Endless Roar (cooldown 10 s); Battle Trance (basic hits give double Rage while active); Unyielding (20 percent less damage taken while active) |
| Rending Spin | Whirl (a tick every 0.25 s); Deep Wounds (bleed doubled); Wide Spin (reach plus 0.8) | Lasting Spin (3.5 s); Undertow (enemies within 4 drift 1 unit a second toward the player); Rage Spin (each tick that hits an elite refunds 2 Rage) | Blade Storm (ends by throwing 3 axes outward, 100 percent each); Endless Motion (Momentum gains a stack every 0.5 s while spinning); Bloodbath (heals 0.5 percent life per enemy hit per tick, up to 3 percent a tick) |
| Blood Frenzy | Lingering (lasts 8 s); Bloodlust (Life on Hit plus 50 percent while active); Swift Frenzy (plus 10 percent move speed while active) | Frenzied Rage (basic hits give 3 more Rage while active); Thirst (each kill adds 0.5 s, up to 4 s); Low Threshold (fires at 2 Momentum stacks) | Berserk Blood (plus 2 percent damage per 10 percent life missing while active); Frenzy Chain (an elite kill resets the cooldown, once per cast); Overdrive (10 percent per stack instead of 5, costs 30 Rage) |
| Skullsplitter | Executioner (the threshold is 30 percent); Cleaving Split (enemies within 1.5 of the target take 50 percent); Refund (a kill refunds 15 Rage) | Ruthless (a kill sets the cooldown to 2 s); Bleeding Skull (bleed, 100 percent over 4 s); Elite Hunter (plus 30 percent against elites and bosses) | Headsman (a kill heals 5 percent life); Stagger Split (adds 10 to a boss's stagger meter); Double Split (strikes twice, the second at 50 percent) |

Rules the modifiers keep: none adds a button or a timing input; none takes control of the character (the dash of Bull Rush is the one movement a skill makes, and it was decided); crowd control only ever lands on enemies.

### Keystones (decided, Q5)

Berserker and Juggernaut are decided as written above. **Decided (Q5 and Q4, 2026-09-27):** the Wrathborn has 2 keystones (not the 3 per class the passive tree section above describes), and his tree holds them, one at the end of the Wrath branch (Berserker) and one at the end of the Stampede branch (Juggernaut), each costing 3 points and available from level 20 (04); both can be bought, and the loadout screen picks which one is active. Changing it is free out of combat (the last sentence is a proposal).

### Passive tree (proposed)

Kept from above: 60 nodes over three branches, one point per level from level 2 (59 at level 60), node types minor, notable, keystone and gateway, respec at the Trainer. For the Wrathborn: 1 start node, 36 minor, 18 notable, 2 keystones, 3 gateways = 60. Buying everything costs 63 points, so a level 60 character leaves 4 nodes out. The node type table above says 3 keystones; the Wrathborn has 2 (decided, Q5), and the counts still reach 60 because of the start node.

Layout: a start node in the middle with three branches going up, left and right. Each branch has an inner half (6 minor, 3 notable) and an outer half behind a gateway (6 minor, 3 notable, and the keystone for Wrath and Stampede). A gateway opens after 15 points in the whole tree. The tree scrolls in one direction on the phone (06, Passive tree), so the branches are drawn as three columns.

| Branch | Theme | Minor nodes (3 kinds, 4 of each across the branch, 2 of each per half) |
|---|---|---|
| Wrath (up) | Rage, damage, crits | Plus 4 percent damage; plus 1.5 percent critical chance; plus 10 percent critical damage |
| Stampede (left) | Momentum, speed, dodge | Plus 3 percent move speed; plus 3 percent attack speed; plus 1.5 percent dodge |
| Scar (right) | Life, armor, sustain | Plus 4 percent life; plus 8 percent armor; plus 2 Life on Hit |

Notables (proposed numbers):

| Branch | Inner half | Outer half |
|---|---|---|
| Wrath | Red Mist: basic hits give 8 Rage instead of 6. Bloodied Edge: plus 15 percent damage to enemies below 50 percent life. Short Fuse: Rage drains after 5 s without combat instead of 3 | Carnage: kills give 3 Rage. Butcher: critical hits deal plus 30 percent damage to bleeding enemies. Hatred: plus 1 percent damage per 10 Rage held. Keystone: Berserker |
| Stampede | Road Runner: Momentum builds a stack every 0.45 s instead of 0.6. Sure Footed: Momentum lasts 2 s after stopping instead of 1.2. Battering Ram: Movement skills deal plus 25 percent damage | Hit and Run: each Momentum stack also gives 2 percent attack speed. Tailwind: plus 1 Momentum cap. Crashing Wave: Bull Rush's cooldown is 1 s shorter per enemy hit. Keystone: Juggernaut |
| Scar | Thick Hide: plus 20 percent armor. Scar Tissue: 5 percent less damage from elites and bosses. Iron Lungs: the potion heals 50 percent instead of 40 | Unbroken: while below 35 percent life, 15 percent less damage taken. Blood Price: Life on Hit plus 50 percent. Old Wounds: plus 1 Stillness cap and plus 10 percent life (a door to a standing build for a Momentum class) |

The stand-in Vitality (8 points a level, built, 2026-09-26) stays until the tree and the attribute growth (decided, Q6) are built; the Scar branch and the attributes together must give about the same life at each level, which the balance report can check.

### Attributes for the Wrathborn (decided, Q6)

The attribute table above stands. **Decided (Q6, 2026-09-27):** attributes grow automatically with the class, so the one-thumb game gains no extra screen and the built balance carries over: per level from level 2, 8 Vitality (the stand-in, unchanged), 2 Might, 2 Agility and 1 Will. Paragon points (04) are the only free choice. For the Wrathborn, Will gives Rage gained instead of Focus regeneration (Focus means nothing for Rage); the amount, plus 0.5 percent per point, is a proposal.

### Loadout and triggers

The loadout screen (06) and the per-slot trigger picker (01) are designed; the built game uses one fixed trigger per skill kind. **Decided (Q15, 2026-09-27):** each skill keeps its trigger from the table above as its default, and the picker offers two alternatives per skill from 01's list (which two is a proposal, for example Hew: "2 or more in reach", "Always", "Elite present"). The loadout screen opens at level 9 (Q4). The loadout keeps 4 slots and the keystone.

### What the Ranger and Hexer would need (post-launch, not expanded)

They stay drafts (decided: one class at launch). Neither can be built on the launch systems alone. They would need: the Focus resource (a `FocusPool` exists and is unused); a player projectile basic attack (enemies already have projectiles); placed ground effects that deal damage over time (Caltrops, traps, Plague Cloud); summons with their own AI (Falcon, Raise Thrall); their own off-hand items (quiver, orb, 03); their own grips, gear looks and a 16-direction bake of each look (09); a passive tree and 8 skills with modifiers each; their share of the Legendaries (03 plans 20 per class). A second class is roughly the size of M1 again in code and larger in art.
