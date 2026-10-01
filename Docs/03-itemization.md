# Itemization

Loot is the reason to play. This file defines what drops, how it is rolled, how the player decides quickly, and how the player can improve items.

## Equipment slots

Ten slots: weapon, off-hand, helm, chest, gloves, boots, belt, amulet, ring 1, ring 2.

Weapons have a base damage range and base attack speed. **Bows only** (decided 2026-09-30, 08): the weapon slot holds a short bow or a longbow, and the off-hand holds a quiver or nothing (the Wild Arrow, 02). Axes, shields, dual wield and two-handed melee weapons are retired with the Wrathborn; his grips are kept below as reference. Focus orbs belong to the Hexer draft (Q22). Class restrictions apply to weapon and off-hand only. All other slots are shared across classes.

## Appearance

Decided 2026-09-27 (08-production.md): the game is gear oriented like Diablo 2, and equipped gear is displayed on the character.

- The Wild Arrow's grip (decided 2026-09-30): both hands on the bow, the quiver on the back or hip. One grip, one animation set; short bow and longbow share it (proposed, until a longbow's heavier draw is worth its own clips).
- Retired with the Wrathborn: his off-hand (decision of 2026-09-27): "can in offhand hold: nothing, offhand weapon, shield, or use two hand". So he fights in one of four grips: one-handed with an empty off-hand, dual wield, weapon and shield, or two-handed. A two-handed weapon needs both hands, so the off-hand is empty while one is equipped (Claude's reading of "use two hand", confirmed with the grip rules on 2026-09-27, Q10; the equip rule is not built). Each grip has its own animation set, as in Diablo 2.
- Shown slots: weapon, off-hand, helm and chest armour. The chest armour sets the torso, arms and legs. Gloves, boots, belt, amulet and rings do not change the character's look (at about 170 px tall they would barely read).
- Looks come in tiers by item level: 3 per shown slot in act 1 (bow: hunting bow, recurve bow, horn bow; longbow: yew longbow, war bow, great bow; quiver: hide quiver, studded quiver, bone quiver, proposed 2026-09-30; chest: padded, leather, mail; helm: cap, nasal helm, great helm), more tiers added with each act. Every item of a tier looks the same on the character, as Diablo 2's light, medium and heavy armour did. The item's icon matches its tier.
- Every legendary in a shown slot has its own unique model and icon.
- An empty helm slot shows the bare head and an empty weapon slot empty hands, matching the game, where a character with no weapon fights unarmed (Claude's reading, not a separate decision).
- The Forge's Transmog (below) changes an item's look; it comes after launch (decided 2026-09-27, Q24).
- How the art is built (a body per chest look, helms, weapons and off-hands as separate pieces, baked into sprite layers the game stacks) is in 09-art-brief.md, section 4.5.

## Item level

Item level equals the zone level where the item drops, capped at 60 in the campaign and rising in Abyss to a cap of 160. Item level gates affix tiers and base item names.

## Rarity

| Rarity | Color | Affixes | Notes |
|---|---|---|---|
| Common | White | 0, base stat only | Salvage material, rare use as sockets carriers |
| Magic | Blue | 1 prefix, 1 suffix | Early game fill |
| Rare | Yellow | 2 to 3 prefixes, 2 to 3 suffixes | Main gearing path |
| Legendary | Orange | 4 fixed and 2 random affixes plus one unique power | Build defining |
| Cursed (variant) | Purple | Legendary with a curse drawback and a stronger power | Endgame only, from Abyss depth 30 onward (04's tier table now agrees, Q22) |

Base drop weights for a normal kill at zone level equal to player level:

| Rarity | Weight |
|---|---|
| Common | 60 |
| Magic | 30 |
| Rare | 8.5 |
| Legendary | 1.5 |

Magic Find multiplies the weights of Magic, Rare and Legendary by (1 + MF percent). Magic Find on gear is capped at 200 percent for the player and does not affect Cursed items.

## Item drop chance per source

| Source | Drop chance | Items on drop | Rarity floor |
|---|---|---|---|
| Normal enemy | 6 percent | 1 | Common |
| Champion (pack leader) | 25 percent | 1 | Magic |
| Elite | 100 percent | 1 to 2 | Magic, 30 percent chance rare floor |
| Zone chest | 100 percent | 1 to 3 | Magic |
| Boss | 100 percent | 4 to 6 | Rare, one legendary chance of 35 percent |
| Abyss floor guardian | 100 percent | 3 to 5 | Rare, escalating legendary chance |

Bad luck protection: the game tracks kills since the last legendary. After 300 kills the legendary weight doubles, after 600 it triples. The counter resets on any legendary drop. This is tuned so a median player sees a legendary about every 25 minutes in the campaign and every 12 in endgame.

## Affix system

Affixes come in prefixes and suffixes. An item has up to three of each. Each affix has five tiers, T1 to T5. Higher tiers require higher item level.

Ranges in the tables below are T1 values. Lower tiers scale the T1 range by the factor shown. A roll picks a uniform value inside the scaled range.

| Tier | Item level required | Range as share of T1 range |
|---|---|---|
| T5 | 1 | 20 to 35 percent |
| T4 | 15 | 35 to 55 percent |
| T3 | 30 | 55 to 75 percent |
| T2 | 45 | 75 to 90 percent |
| T1 | 60 | 90 to 100 percent |

### Prefix pool (offensive and defensive base)

| Affix | Range at T1 | Slots that can roll it |
|---|---|---|
| Flat weapon damage | 60 to 90 | Weapon, ring, amulet, gloves |
| Increased damage | 18 to 26 percent | Weapon, gloves, amulet |
| Life | 180 to 240 | Chest, helm, belt, boots |
| Armor | 90 to 130 | Chest, helm, gloves, boots, shield |
| Increased Focus regeneration | 10 to 16 percent | Amulet, ring, orb |
| Added area | 8 to 14 percent | Weapon, helm, amulet |
| Skill level (by tag) | 1 to 2 | Weapon, off-hand, amulet |
| Life regeneration | 8 to 14 per second | Chest, belt, ring |

### Suffix pool (utility and scaling)

| Affix | Range at T1 | Slots |
|---|---|---|
| Attack speed | 7 to 11 percent | Weapon, gloves, ring |
| Critical chance | 4 to 7 percent | Weapon, ring, amulet, gloves |
| Critical damage | 20 to 30 percent | Weapon, amulet, ring |
| Movement speed | 6 to 10 percent | Boots only |
| Cooldown reduction | 5 to 9 percent | Helm, amulet, ring |
| Resistance (fire, cold, poison, shadow) | 12 to 18 percent | Any except weapon |
| Life on hit | 4 to 8 | Weapon, ring, gloves |
| Dodge chance | 3 to 5 percent | Boots, belt, ring |
| Magic Find | 8 to 12 percent | Amulet, ring, boots |
| Stillness or Momentum stack cap | 1 | Belt, boots, helm |

The suffix Stillness stack cap and its Momentum counterpart are limited to one per item and appear only from item level 35.

About 90 distinct affixes ship in 1.0. The table above shows the launch core. All values are placeholders subject to simulation.

## Base value formulas

The following curves give a starting point. A simulation script in the tooling repository will refine them.

- Weapon average damage: 6 + 2.6 times (item level to the power 1.35). At item level 30 this is about 262, at level 60 about 660.
- Base armor per piece at item level L: 8 + 3.1 times L.
- Enemy hit damage at level L: 3 times L to the power 1.45. Enemy life at level L: 8 times L to the power 1.9.
- Character base life at level L: 80 plus 20 times L before Vitality and gear.

Damage formula for a hit:

1. Base = weapon damage times skill multiplier plus flat added damage times the skill multiplier.
2. Increased modifiers sum together: increased = 1 plus sum of all increased percentages.
3. More multipliers multiply separately: Stillness stacks, banner, Hunter's Mark, curses, legendary powers.
4. Critical hits multiply by (1.5 plus critical damage bonus).
5. Enemy mitigation by armor: reduction = armor / (armor + 50 times enemy level + 400), capped at 80 percent.
6. Resistance reduces elemental damage by resistance percent, capped at 75 percent for players.

Player armor uses the same formula against the level of the attacker. Elite and boss damage ignores 15 percent of player armor.

## Legendary items

Each legendary has three parts: a fixed base type, four fixed affix slots with rolled values, and a unique power. Powers are designed around the skill tags in 02-classes-and-skills.md. Examples:

| Item | Slot | Unique power |
|---|---|---|
| Ashen Warden's Plate | Chest | While in Stillness, Cleave, Earthshatter and Shield Bash cast twice, second cast at 50 percent damage |
| Wind-Sworn Quiver | Off-hand | Rolling Volley no longer has a cooldown while Momentum is at max stacks |
| Voidglass Circlet | Helm | Curse of Frailty spreads to enemies within 3 units when its target dies |
| The Last Toll | Ring | When you would die, restore 40 percent life and deal 800 percent damage in radius 8. Cooldown 90 s |
| Hollow Saint's Chain | Amulet | Summons are also affected by your Stillness and Momentum stacks |
| Thornroot Belt | Belt | Elites you kill drop a healing shrine that lasts 10 s |

Design rule: a legendary power must change what the skill loadout does, not only add a number. 40 legendaries ship in 1.0: 24 tied to Wrathborn skills and 16 shared (decided 2026-09-27, Q7; the earlier 60 assumed three classes). The examples above for the Warden, Ranger and Hexer wait for those classes.

Cursed variants carry a stronger power and a drawback, for example: The Last Toll (Cursed) triggers every 45 s but reduces max life by 25 percent.

## Sockets and gems

- Sockets appear on Rare and Legendary items. Rare items have 0 to 2 sockets, Legendary 1 to 3.
- Gem types by color: Ruby (damage), Sapphire (Focus and cooldown), Emerald (defense and life), Onyx (Magic Find and utility).
- Gem tiers: Chipped, Flawed, Normal, Flawless, Perfect. Three of one tier fuse into one of the next at the Forge for gold.
- Gems in weapons, armor and jewelry give different bonuses, shown on the gem tooltip.

## Loot filter

The filter is a first-class system because the player cannot afford to read every drop.

Preset filters:

| Preset | Shows |
|---|---|
| Everything | All drops |
| Smart | Magic and above that beat an equipped item in at least one stat, and all Rare and above |
| Upgrades only | Items that raise the character's power score |
| Legendary only | Legendary and Cursed only |

Rules:

- Filtered items are still picked up, into a salvage pouch that takes no backpack slots; the smith offers to salvage them all in one tap (decided 2026-09-27, Q11, since salvage happens only at the Forge). They never take an inventory slot.
- The filter can be changed from the inventory screen in two taps.
- A custom filter allows rules by rarity, slot, affix tag and minimum item level.
- The filter never hides Legendary items by default.

Power score is a single number computed from an item's contribution to damage per second, effective life and the class's main tags. It is used for the upgrade arrows and the Smart preset. It is a comparison aid, not a source of truth. Tooltips always show raw affixes.

## Inventory and stash

- Inventory: 40 slots. Items stack vertically in a scrolling list, no grid tetris.
- Stash: 120 slots at start, expandable to 300 with gold.
- Shared stash across characters on the same device.
- Salvage: only at the Forge in town (decision of 2026-09-26). Salvage one item, or bulk salvage by rarity and below a chosen level. Salvage returns materials by rarity: Ash (common), Cinders (magic), Bloodstone (rare), Soulglass (legendary).

## Forge (crafting)

The Forge is a bottom sheet opened by walking up to the smith in town (05-world-and-content.md), with one action per tab and Salvage as the first tab. Every action has a gold cost and a material cost. The first version builds Salvage, Reforge affix, Reroll values and Temper (decision of 2026-09-26). Reforge turns the chosen affix into a new random affix of the same kind at the same tier, and can land on the same stat.

| Action | Effect | Cost basis |
|---|---|---|
| Reforge affix | Reroll one chosen affix on a Rare item, same tier band | Bloodstone plus gold that rises with each reroll on the same item |
| Reroll values | Reroll numeric values of all affixes on a Legendary item within their ranges | Soulglass plus gold |
| Add socket | Adds one socket to an item with fewer than its maximum | Cinders on a Rare, Bloodstone on a Legendary (04, material economy; a proposal) |
| Temper | Raises the tier of one affix by one step, up to T1 | Soulglass, limited to 3 uses per item |
| Imprint | Copies a legendary power onto a Rare item of the same slot, destroying the legendary | Soulglass, endgame only |
| Transmog | Changes appearance only | After launch (decided, Q24) |

Cost escalation: each reforge on the same item raises its material cost by 25 percent. This creates a natural stopping point and keeps drops relevant.

## Item tooltips (one-hand layout)

- Tooltips open as a bottom sheet, reachable with the thumb.
- Layout order: name and rarity, power score with an arrow, base stat, affixes with tier dots, sockets, unique power text, comparison strip.
- The comparison strip shows the equipped item side by side and highlights gains in green and losses in red.
- Actions sit in one row at the bottom: Equip, Discard, Lock. Lock prevents discarding, salvage and stash cleaning. Salvage is at the Forge in town, not on the tooltip (decision of 2026-09-26).
- Swipe left and right on the sheet moves between drops in the results list.

## Full-game plan: items (proposed, with the owner's decisions of 2026-09-27)

Written 2026-09-27 for the full-game plan (`10-full-game-plan.md`). Everything below is a proposal and numbers are tuning values, except where marked **Decided** (the owner's answers of 2026-09-27; questions are numbered as in `08-production.md`, "Open questions from the full-game plan"). The text above now follows those decisions.

### The ten slots for the Wild Arrow (from 2026-09-30; the Wrathborn's table kept after it)

Proposed 2026-09-30 with the bows-only decision. Numbers are tuning.

| Slot | Base stat | Implicit (every item of the slot) | Shown on the character |
|---|---|---|---|
| Weapon: short bow | Weapon damage (built curve) | Reach 7.5 | Yes |
| Weapon: longbow | Weapon damage times 1.3 | Attack speed times 0.8, reach 9 | Yes |
| Off-hand: quiver | none | Plus 5 percent attack speed, rising to 9 with the tier | Yes |
| The other eight | As in the Wrathborn's table below | | |

**Starting kit (decided 2026-10-01):** a Common item level 1 short bow (hunting bow look) and a Common item level 1 quiver (hide quiver look, +5 percent attack speed). A longbow's damage per second is 1.04 times a short bow's before affixes; it pays in speed for reach. Both bows take two hands, so the off-hand holds only a quiver. A quiver rolls Flat weapon damage and Life (prefixes) and Attack speed, Critical chance and Critical damage (suffixes), and two quiver-only affixes are proposed: **Extra arrow** (S, 8 to 15 percent chance that a basic shot looses a second arrow at another enemy in reach) and **Pierce** (S, fixed: basic arrows pass through 1 more enemy, from item level 20). Block, the shield affixes (28, 85 below) and dual wield are gone.

### The ten slots for the Wrathborn (retired 2026-09-30)

Built: weapon, chest, helm; on 2026-09-28 gloves, boots, belt, amulet and two rings with the base armor below (the belt's potion charge from item level 30 is not built); on 2026-09-29 the off-hand (shield, dual wield) and two-handed weapons with the grips below. Proposed for the other seven:

| Slot | Base stat | Implicit (every item of the slot) | Shown on the character |
|---|---|---|---|
| Weapon, one-handed | Weapon damage (built curve) | none | Yes (decided) |
| Weapon, two-handed | Weapon damage times 1.6 | Attack speed times 0.85, reach plus 0.3 | Yes |
| Off-hand: one-handed weapon | Its own weapon damage, used by its own swings | Dual wield (below) | Yes |
| Off-hand: shield | Armor (the built curve, full value) | Block chance 12 percent, rising to 20 with the tier | Yes |
| Helm | Armor, full value (built) | none | Yes (decided) |
| Chest | Armor, full value (built) | none | Yes (decided) |
| Gloves | Armor, 60 percent | none | No (decided) |
| Boots | Armor, 60 percent | none | No |
| Belt | Armor, 40 percent | From item level 30: plus 1 potion charge | No |
| Amulet | none | none | No |
| Ring (2 slots) | none | none | No |

Grips (decided: nothing, an off-hand weapon, a shield, or a two-handed weapon; the equip rule is not built). **Decided (Q10, 2026-09-27):** the grips play differently, as follows (the numbers are tuning):

- One-handed, off-hand empty: the reference. Nothing extra.
- Dual wield: swings alternate hands, each using its own weapon's damage; plus 15 percent attack speed; both weapons' affixes count. Skills use the main-hand weapon.
- Weapon and shield: block chance from the shield. A block stops a melee hit or a projectile completely (like dodge, not ground shapes, 01). Block and dodge roll separately; block is capped at 50 percent.
- Two-handed: the off-hand slot is emptied when a two-handed weapon is equipped (the item goes to the backpack; if the backpack is full the equip is refused with a message). Equipping an off-hand item while holding a two-hander asks to swap.
- A two-hander's damage per second is 1.36 times a one-hander of the same item level before affixes, which pays for the empty off-hand (a shield's armor and block, or dual wield's speed and second set of affixes).

Weapon family: axes only for 1.0 (Hurl Axe throws the weapon, and the brief's act 1 looks are axes), plus the maul among the two-handers, as the brief already has it. Maces and swords wait for a class that needs them.

### Base types and looks per item level (proposed)

Decided: shown slots have looks in tiers by item level, 3 per slot in act 1, more with each act, and every legendary its own look. Act 1's bands (1 to 3, 4 to 6, 7 and up) are built. Proposed: each act adds 3 looks per shown slot, and the bands spread over the whole item level range of 04's recommended level plan (Q1), so a character keeps finding better-looking gear until level 60 and in the Abyss. With act 2's looks, "7 and up" becomes 7 to 10.

The Wild Arrow's columns (2026-09-30, proposed): **1h weapon** becomes the short bow (act 1: hunting bow, recurve bow, horn bow), **2h weapon** the longbow (yew longbow, war bow, great bow) and **Shield** the quiver (hide quiver, studded quiver, bone quiver). The later bands' axe, maul and shield names below are renamed when those acts get their looks.

| Band | Item level | 1h weapon | 2h weapon | Shield | Helm | Chest |
|---|---|---|---|---|---|---|
| Act 1 a | 1 to 3 | hatchet | great axe | buckler | cap | padded |
| Act 1 b | 4 to 6 | bearded axe | maul | round shield | nasal | leather |
| Act 1 c | 7 to 10 | war axe | bardiche | kite shield | great | mail |
| Act 2 a | 11 to 14 | boarding axe | long axe | targe | kettle hat | scale |
| Act 2 b | 15 to 18 | cleaver | pole cleaver | heater | sallet | brigandine |
| Act 2 c | 19 to 23 | broad axe | harpoon axe | tower shield | barbute | lamellar |
| Act 3 a | 24 to 29 | bone-hafted axe | bone maul | bone-rimmed shield | bone mask helm | bone-plated coat |
| Act 3 b | 30 to 35 | reaver | reaper's scythe-axe | reliquary shield | visored helm | ringmail coat |
| Act 3 c | 36 to 41 | headsman's axe | executioner's axe | saint's aegis | crested helm | splint armour |
| Act 4 a | 42 to 47 | knight's axe | siege maul | spired shield | bascinet | half plate |
| Act 4 b | 48 to 53 | tabar | war cleaver | oath shield | armet | iron plate |
| Act 4 c | 54 to 59 | spire axe | spire halberd | bastion shield | oath helm | oath plate |
| Act 5 a | 60 to 79 | hollow-edge axe | hollow maul | voidglass shield | ember crown | ember plate |
| Act 5 b | 80 to 109 | watcher's axe | watcher's great axe | watcher's shield | watcher's helm | hollow-forged plate |
| Act 5 c | 110 to 160 | vigil axe | vigil great axe | vigil shield | vigil helm | vigil plate |

The first rows of act 1 are the brief's (09, 4.5). An off-hand weapon uses the one-handed weapon's look. Gloves, boots, belts, amulets and rings have no look on the character (decided) and get one icon per act band (5 per slot, 09).

Every shown look is one more set of baked sheets (a body per chest look, a piece per helm, weapon and shield look). How many the phone and the app size can hold is found by measuring act 1 on a phone and then trimming; the character's resolution may drop to Diablo 2's (decided 2026-09-27, Q3; 07).

### Item level above 60 (decided, Q8)

Item levels go to 160 in the Abyss (above), but affix tiers stop at T1 (item level 60). **Decided (Q8, 2026-09-27):** base damage and armor keep following their formulas to 160 (a level 160 weapon averages about 2,450 damage), and an affix rolled on an item above level 60 has its T1 range raised by 1 percent per level above 60, up to plus 100 percent at 160. No new tier names. Tempering (built) still stops at T1.

### Elements and resistances (decided, Q9)

The resistance suffixes and the 75 percent cap exist in the docs above, but nothing deals elemental damage. **Decided (Q9, 2026-09-27):** each act has an element, and each Vigil tier after the first lowers the character's resistances by 15. The details: basic melee hits are physical everywhere; each act's casters, projectiles and boss ground shapes deal the act's element (05: act 1 fire, 2 cold, 3 poison, 4 physical and fire, 5 shadow). Resistance reduces that damage by its percent (above). As in Diablo 2, each Vigil tier after the first lowers the character's resistances: minus 15 on Vigil II, 30 on III, 45 on IV, 60 on V and the Abyss (tuning), so resistance gear matters later and not at the start. Act 1 as built has no elements; the Cinder Warden's fire would become fire damage.

### Affix pool (proposed, 88 affixes)

Built affixes keep their built numbers. Ranges are T1 at item level 60; lower tiers use the tier share table above. "Fixed" affixes do not tier: they appear from the item level named and always give the value shown. P is a prefix, S a suffix. The 8 Wrathborn tags are in 02.

| # | Affix | Kind | T1 range | Slots | Status |
|---|---|---|---|---|---|
| 1 | Flat weapon damage | P | 60 to 90 | Weapon, ring, amulet, gloves | Built (weapon) |
| 2 | Increased damage | P | 18 to 26 percent | Weapon, gloves, amulet | Built (weapon) |
| 3 | Life | P | 180 to 240 | Chest, helm, belt, boots, shield | Built (chest, helm) |
| 4 | Armor | P | 90 to 130 | Chest, helm, gloves, boots, shield | Built (chest, helm) |
| 5 | Increased Focus regeneration (Rage gained for the retired Wrathborn) | P | 10 to 16 percent | Amulet, ring, shield | Designed |
| 6 | Added area | P | 8 to 14 percent | Weapon, helm, amulet | Designed |
| 7 to 14 | Plus levels to skills by tag (one affix per tag; the Wild Arrow's tags so far are Projectile, Cone, Pierce, Homing and Area, 02) | P | 1 to 2 | Weapon, off-hand, amulet | Designed (skill level by tag) |
| 15 | Life regeneration | P | 8 to 14 per second | Chest, belt, ring | Designed |
| 16 | Increased armor | P | 15 to 25 percent | Chest, helm, shield, gloves, boots | Proposed |
| 17 | Maximum life | P | 5 to 9 percent | Chest, belt, amulet | Proposed |
| 18 | Damage to elites and bosses | P | 10 to 16 percent | Weapon, amulet, ring | Proposed |
| 19 | Bleed damage | P | 20 to 35 percent | Weapon, gloves | Proposed |
| 20 to 27 | Increased damage of Melee, Sweep, Area, Movement, Projectile, Buff (the buff's own numbers), Channel or Execute skills (in that order) | P | 12 to 20 percent | Weapon, gloves, helm, amulet | Proposed |
| 28 | Block chance | P | 6 to 10 percent | Shield | Retired 2026-09-30 (no shields) |
| 29 | Damage reduction against projectiles | P | 6 to 10 percent | Chest, shield | Proposed |
| 30 | Damage reduction against melee | P | 6 to 10 percent | Chest, shield | Proposed |
| 31 | Thorns (damage to melee attackers) | P | 40 to 70 | Chest, shield, belt | Proposed |
| 32 | Increased Stillness effect | P | 15 to 25 percent | Chest, belt | Proposed |
| 33 | Increased Momentum effect | P | 15 to 25 percent | Boots, gloves | Proposed |
| 34 | Damage while Momentum is at its cap | P | 10 to 16 percent | Boots, gloves | Proposed |
| 35 | Damage while holding 3 or more Stillness stacks | P | 10 to 16 percent | Chest, belt | Proposed |
| 36 | Damage to bleeding enemies | P | 12 to 20 percent | Weapon, ring | Proposed |
| 37 | Damage to slowed or stunned enemies | P | 12 to 20 percent | Weapon, gloves | Proposed |
| 38 | Armor penetration (ignores enemy armor) | P | 8 to 14 percent | Weapon, gloves | Proposed |
| 39 | Plus 1 to all skills | P | Fixed 1, from item level 60 | Amulet | Proposed |
| 40 | Attack speed | S | 7 to 11 percent | Weapon, gloves, ring | Built (weapon) |
| 41 | Critical chance | S | 4 to 7 percent | Weapon, ring, amulet, gloves | Built (weapon) |
| 42 | Critical damage | S | 20 to 30 percent | Weapon, amulet, ring | Built (weapon) |
| 43 | Movement speed | S | 6 to 10 percent | Boots | Designed |
| 44 | Cooldown reduction | S | 5 to 9 percent | Helm, amulet, ring | Built (helm) |
| 45 to 48 | Fire, cold, poison or shadow resistance (one affix each) | S | 12 to 18 percent | Any except weapon | Designed |
| 49 | All resistances | S | 6 to 10 percent | Amulet, ring, shield | Proposed |
| 50 | Life on hit | S | 4 to 8 | Weapon, ring, gloves | Built (weapon) |
| 51 | Dodge chance | S | 3 to 5 percent | Boots, belt, ring | Designed |
| 52 | Magic Find | S | 8 to 12 percent | Amulet, ring, boots | Designed |
| 53 | Stillness stack cap | S | Fixed 1, from item level 35 | Belt, boots, helm | Designed |
| 54 | Momentum stack cap | S | Fixed 1, from item level 35 | Belt, boots, helm | Designed |
| 55 to 62 | Plus levels to one named skill, one affix per skill: Split Arrow, Pierce Arrow, Homing Arrow, Explosive Arrow (the Wild Arrow's four, 2026-09-30; four more when its held skills are added) | S | 1 to 2 | Helm, gloves, amulet, off-hand | Proposed |
| 63 | Gold find | S | 15 to 25 percent | Amulet, ring, belt | Proposed |
| 64 | Life on kill | S | 20 to 40 | Weapon, ring, belt | Proposed |
| 65 | Rage on kill | S | 2 to 4 | Weapon, ring | Proposed |
| 66 | Reduced Rage costs | S | 6 to 10 percent | Helm, amulet | Proposed |
| 67 | Rage drain delay | S | 1 to 2 s | Belt, amulet | Proposed |
| 68 | Potion healing | S | 15 to 25 percent | Belt | Proposed |
| 69 | Fewer kills to refill a potion charge | S | 2 to 3 (from 10) | Belt | Proposed |
| 70 | Extra potion charge | S | Fixed 1, from item level 35 | Belt | Proposed |
| 71 | Pickup radius | S | 0.5 to 1.0 units | Boots, belt | Proposed |
| 72 | Reach | S | 0.2 to 0.4 units | Weapon | Proposed |
| 73 | Knockback chance | S | 10 to 20 percent | Weapon, shield | Proposed |
| 74 | Hits slow the target 20 percent for 1 s, chance | S | 10 to 20 percent | Weapon, gloves | Proposed |
| 75 | Stun chance (0.5 s) | S | 3 to 6 percent | Two-handed weapon | Proposed |
| 76 | Less damage from elites and bosses | S | 5 to 9 percent | Helm, chest, shield | Proposed |
| 77 | Less damage from ground shapes | S | 6 to 10 percent | Boots, chest | Proposed |
| 78 | Ember light radius | S | 10 to 20 percent | Helm, amulet | Proposed |
| 79 | Stagger build-up against bosses | S | 15 to 25 percent | Weapon, gloves | Proposed |
| 80 | Buff skill duration | S | 10 to 20 percent | Helm, amulet | Proposed |
| 81 | Experience gained | S | 3 to 6 percent | Helm, amulet | Proposed |
| 82 | Damage reduction while moving | S | 4 to 7 percent | Boots, chest | Proposed |
| 83 | Life regeneration while standing | S | 12 to 20 per second | Belt, chest | Proposed |
| 84 | Critical chance against elites | S | 5 to 9 percent | Ring, gloves | Proposed |
| 85 | Rage on block | S | 3 to 5 | Shield | Retired 2026-09-30 (no shields) |
| 86 | Life on elite kill | S | 5 to 8 percent of max life | Ring, amulet | Proposed |
| 87 | Movement speed while Momentum is at its cap | S | 5 to 8 percent | Boots | Proposed |
| 88 | Bleed duration | S | 1 to 2 s | Weapon, gloves | Proposed |

Counts: 39 prefixes, 49 suffixes, 88 in all (above: "about 90"). Every slot has at least 3 of each kind eligible once all ten slots exist, so a Rare's 2 to 3 of each always fits.

### Legendaries for launch (40 decided, Q7; each one proposed)

00 and the section above planned 60 (20 per class plus 20 shared), which assumed three classes. **Bows only (2026-09-30):** the 24 tied to Wrathborn skills below, and every axe, shield or two-hander among them, are retired with him and are to be redesigned for the Wild Arrow's skills and bows; the 16 shared ones stand. **Decided (Q7, 2026-09-27):** 40, of which 24 are tied to Wrathborn skills and 16 are shared rules any later class can use. 20 are in shown slots and need their own model (decided: every legendary in a shown slot has one). The two shared examples above that fit (The Last Toll, Thornroot Belt) are kept; the other four examples belong to the Warden, Ranger and Hexer and wait for them.

Each has a fixed base (its band's look is replaced by its own), four fixed affixes (values rolled in the item level's tier), two random affixes and the power (the section above). Fixed affixes are given by number from the affix table.

| # | Name | Slot | Fixed affixes | Unique power | Tied to |
|---|---|---|---|---|---|
| 1 | Bonebiter | 1h weapon | 1, 40, 41, 21 | Hew makes enemies bleed; a bleeding enemy hit by Hew again takes 40 percent more from it | Hew |
| 2 | The Ferryman's Cleaver | 1h weapon | 1, 2, 24, 50 | Hurl Axe flies back to the character, hitting everything on the way; its cooldown is 2 s shorter when it returns | Hurl Axe |
| 3 | Last Ember of the Warden | 1h weapon | 1, 2, 42, 27 | A Skullsplitter kill bursts in embers: radius 3, 150 percent | Skullsplitter |
| 4 | Stormstep Hatchet | 1h weapon | 1, 40, 23, 43 | Bull Rush leaves a line that strikes 1 s later for 120 percent | Bull Rush |
| 5 | Oathbreaker | 1h weapon | 1, 2, 40, 7 | Every fifth basic attack is a free Hew | Hew |
| 6 | Grave Maul | 2h weapon | 1, 2, 22, 75 | Ground Breaker's crater pulses twice more, 1 s apart, at 50 percent | Ground Breaker |
| 7 | Hollowreaper | 2h weapon | 1, 40, 26, 33 | Rending Spin moves 40 percent faster and its reach grows 0.2 per Momentum stack | Rending Spin |
| 8 | Kaeth's Oath | 2h weapon | 1, 2, 27, 18 | Skullsplitter's threshold is 35 percent, and its kills reset Bull Rush | Skullsplitter, Bull Rush |
| 9 | Tidebreaker | 2h weapon | 1, 2, 25, 73 | Battle Roar also slams: radius 3, 200 percent, knockback | Battle Roar |
| 10 | The Iron Promise | Shield | 4, 28, 30, 85 | A block gives 8 Rage and 1 Stillness stack | Rage, Stillness |
| 11 | Ramward | Shield | 4, 28, 3, 10 | Bull Rush deals double damage, and every hit during the dash is blocked | Bull Rush |
| 12 | Ember Aegis | Shield | 4, 28, 49, 77 | Every 8 s, the next ground shape that hits deals 50 percent less | Shared |
| 13 | Crown of the Unburned | Helm | 3, 4, 44, 12 | Battle Roar gives double Rage and can fire at any Rage | Battle Roar |
| 14 | Ashen Mask | Helm | 3, 16, 44, 80 | Blood Frenzy also gives 20 percent damage reduction | Blood Frenzy |
| 15 | Skull of Marrow | Helm | 3, 4, 62, 76 | Killing an elite fires a free Skullsplitter at the nearest enemy | Skullsplitter |
| 16 | Watchman's Visor | Helm | 3, 4, 49, 78 | Ground telegraphs under the character fill 10 percent slower | Shared |
| 17 | Hide of the Stampede | Chest | 3, 4, 33, 82 | Plus 3 Momentum cap; Bull Rush gives 2 stacks per enemy hit | Bull Rush, Momentum |
| 18 | Scarred Hauberk | Chest | 3, 17, 4, 76 | Berserker's threshold is 65 percent life instead of 50 | Berserker keystone |
| 19 | The Bloodied Coat | Chest | 3, 16, 36, 15 | Rending Spin's bleed heals the character for 25 percent of its damage | Rending Spin |
| 20 | Warden's Plate | Chest | 3, 4, 17, 29 | A hit larger than 20 percent of max life gives 30 percent damage reduction for 3 s (cooldown 10 s) | Shared |
| 21 | Grips of the Hurler | Gloves | 1, 40, 24, 57 | Hurl Axe throws 3 axes in a fan and is free while Rage is below 50 | Hurl Axe |
| 22 | Butcher's Wraps | Gloves | 19, 41, 88, 36 | Critical hits on bleeding enemies add 1 s to the bleed and 50 percent critical damage | Bleed |
| 23 | Fists of Wrath | Gloves | 2, 40, 41, 65 | At full Rage basic attacks hit twice | Rage |
| 24 | Gauntlets of the Oath | Gloves | 4, 50, 84, 38 | Life on Hit is doubled against elites and bosses | Shared |
| 25 | Stampede Treads | Boots | 43, 4, 34, 87 | Moving through enemies knocks them aside for 60 percent, once per enemy every 2 s | Momentum |
| 26 | Ashwalkers | Boots | 43, 3, 45, 33 | At full Momentum, the character leaves a trail of embers: 40 percent per second | Momentum |
| 27 | Striders of the Wanderer | Boots | 43, 51, 49, 71 | After a dodge, plus 30 percent move speed for 2 s | Shared |
| 28 | Leaden Boots | Boots | 3, 4, 32, 53 | Stillness builds twice as fast, and moving keeps half the stacks | Stillness |
| 29 | Thornroot Belt | Belt | 3, 15, 68, 69 | Elites killed leave a healing shrine for 10 s (above) | Shared |
| 30 | Girdle of Rage | Belt | 3, 67, 5, 17 | Rage never drains out of combat | Rage |
| 31 | Sash of the Last Draught | Belt | 3, 68, 70, 63 | Plus 1 potion charge; a potion also gives 20 percent damage while it heals | Shared |
| 32 | Belt of the First Fire | Belt | 3, 17, 22, 31 | Ground Breaker's cooldown resets when it hits 8 or more enemies | Ground Breaker |
| 33 | Heart of the Vigil | Amulet | 2, 78, 49, 52 | Ember light radius plus 50 percent; enemies inside it take 10 percent more damage | Shared |
| 34 | Wrathstone | Amulet | 12, 80, 44, 5 | Battle Roar and Blood Frenzy have 30 percent shorter cooldowns and last 50 percent longer | Buff skills |
| 35 | Tooth of the Hunger | Amulet | 14, 27, 42, 64 | A Skullsplitter kill heals 10 percent life | Skullsplitter |
| 36 | The Drowned Locket | Amulet | 17, 49, 76, 15 | The first hit taken every 6 s deals half damage | Shared |
| 37 | The Last Toll | Ring | 41, 42, 49, 3 | When the character would die: restore 40 percent life and deal 800 percent in radius 8; cooldown 90 s (above) | Shared |
| 38 | Band of the Charging Bull | Ring | 40, 23, 65, 56 | Bull Rush's cooldown is 3 s shorter and it holds 2 charges | Bull Rush |
| 39 | Ring of Endless Spin | Ring | 41, 26, 66, 60 | Rending Spin lasts as long as Rage remains, 12 Rage a second after its first 2.5 s | Rending Spin |
| 40 | Seal of the Gambler | Ring | 52, 63, 41, 42 | Elites drop one more item, and the character takes 10 percent more damage | Shared |

Drop rules: every Legendary can drop anywhere its item level allows, with a minimum item level per item (8 for the first 20, 20 for the rest, tuning), so a new character meets the simple ones first. A Legendary found once is recorded in the Codex (05, Endgame).

### Cursed items (proposed details)

Kept from above: purple, a Legendary with a curse and a stronger power, from Abyss depth 30 (04's tier table said Vigil II; it now follows this, Q22). Proposed: every Legendary has a Cursed form with its power's numbers raised by 50 percent and one drawback rolled from: 20 percent less max life; no auto-potion; Rage drains even in combat at 2 a second; 30 percent less to all resistances; 20 percent more damage from elites; Momentum and Stillness caps minus 2; 10 percent less move speed; no Life on Hit. Salvage gives 2 Soulglass. Magic Find does not affect Cursed items (above).

### Sets (decided, Q18)

No sets are in the docs. **Decided (Q18, 2026-09-27):** none in 1.0. Each set would need its own looks on the character and would compete with the Legendaries for the same slots; the 40 Legendaries already carry the build-changing role.

### Sockets and gems (proposed numbers)

Kept from above: Rare items 0 to 2 sockets, Legendary 1 to 3; four colours in five tiers; three of a tier fuse into one of the next at the Forge. The Sapphire's "Focus and cooldown" becomes Rage and cooldown for the Wrathborn.

| Gem | In a weapon | In armor (helm, chest, shield, gloves, boots, belt) | In jewelry (amulet, ring) |
|---|---|---|---|
| Ruby | Increased damage 4, 7, 10, 14, 20 percent | Critical damage 4, 7, 10, 14, 20 percent | Damage to elites 3, 5, 7, 10, 14 percent |
| Sapphire | Rage per hit 1, 1, 2, 2, 3 | Cooldown reduction 1, 2, 3, 4, 6 percent | Rage gained 3, 5, 7, 10, 14 percent |
| Emerald | Life on hit 2, 4, 6, 9, 14 | Life 3, 5, 7, 10, 14 percent | Armor 5, 8, 12, 16, 22 percent |
| Onyx | Attack speed 1, 2, 3, 4, 6 percent | Magic Find 3, 5, 8, 11, 15 percent | Move speed 1, 2, 3, 4, 5 percent |

Values are Chipped, Flawed, Normal, Flawless, Perfect. Drops: elites 10 percent, chests 20, bosses 1 to 2; the tier drops by item level (Chipped from 1, Flawed from 15, Normal from 30, Flawless from 45); Perfect only by fusion. Gems stack in their own pouch, not in the 40 backpack slots. Removing a gem at the Forge returns it for gold (10 times the curve at the item's level). Fusion costs are in 04.

### Loot filter (decided, Q11)

The design above stands (presets, custom rules, never hides Legendaries by default). Its auto-salvage conflicted with the decision that salvage happens only at the Forge (existing question 9, repeated as Q11). **Decided (Q11, 2026-09-27):** filtered items are picked up into a separate salvage pouch that does not use backpack slots and cannot be opened in the dungeon; walking up to the smith offers "Salvage N filtered items" as one tap in the Salvage tab. Salvage still only happens at the Forge, and the player never walks past loot. The filter's introduction stays at 200 items dropped (06).

### Transmog (after launch, Q24)

Designed as a Forge action for gold. **Decided (Q24, 2026-09-27):** Transmog comes after launch, not in 1.0. The rules below are kept as a proposal for then: the Forge's Transmog tab lists every look the character has ever picked up for that slot (tiers and legendaries alike); choosing one changes only the item's look and icon, for 10 times the gold curve plus 4 Ash (04). Removing a transmog is free. A legendary's look can be put on any item of its slot once that legendary has been found.
