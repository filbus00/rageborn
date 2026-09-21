# Itemization

Loot is the reason to play. This file defines what drops, how it is rolled, how the player decides quickly, and how the player can improve items.

## Equipment slots

Ten slots: weapon, off-hand, helm, chest, gloves, boots, belt, amulet, ring 1, ring 2.

Weapons have a base damage range and base attack speed. The off-hand slot accepts shields (Warden), quivers (Ranger) or focus orbs (Hexer). Class restrictions apply to weapon and off-hand only. All other slots are shared across classes.

## Item level

Item level equals the zone level where the item drops, capped at 60 in the campaign and rising in Abyss to a cap of 160. Item level gates affix tiers and base item names.

## Rarity

| Rarity | Color | Affixes | Notes |
|---|---|---|---|
| Common | White | 0, base stat only | Salvage material, rare use as sockets carriers |
| Magic | Blue | 1 prefix, 1 suffix | Early game fill |
| Rare | Yellow | 2 to 3 prefixes, 2 to 3 suffixes | Main gearing path |
| Legendary | Orange | 4 fixed and 2 random affixes plus one unique power | Build defining |
| Cursed (variant) | Purple | Legendary with a curse drawback and a stronger power | Endgame only, from Abyss depth 30 onward |

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

Design rule: a legendary power must change what the skill loadout does, not only add a number. About 60 legendaries ship in 1.0, 20 per class plus 20 shared.

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

- Filtered items are still picked up and auto-salvaged for materials. They never take an inventory slot.
- The filter can be changed from the inventory screen in two taps.
- A custom filter allows rules by rarity, slot, affix tag and minimum item level.
- The filter never hides Legendary items by default.

Power score is a single number computed from an item's contribution to damage per second, effective life and the class's main tags. It is used for the upgrade arrows and the Smart preset. It is a comparison aid, not a source of truth. Tooltips always show raw affixes.

## Inventory and stash

- Inventory: 40 slots. Items stack vertically in a scrolling list, no grid tetris.
- Stash: 120 slots at start, expandable to 300 with gold.
- Shared stash across characters on the same device.
- Salvage: tap and hold to salvage, or bulk salvage by rarity and below a chosen level. Salvage returns materials by rarity: Ash (common), Cinders (magic), Bloodstone (rare), Soulglass (legendary).

## Forge (crafting)

The Forge is a menu, not a world location. Every action has a gold cost and a material cost.

| Action | Effect | Cost basis |
|---|---|---|
| Reforge affix | Reroll one chosen affix on a Rare item, same tier band | Bloodstone plus gold that rises with each reroll on the same item |
| Reroll values | Reroll numeric values of all affixes on a Legendary item within their ranges | Soulglass plus gold |
| Add socket | Adds one socket to an item with fewer than its maximum | Bloodstone |
| Temper | Raises the tier of one affix by one step, up to T1 | Soulglass, limited to 3 uses per item |
| Imprint | Copies a legendary power onto a Rare item of the same slot, destroying the legendary | Soulglass, endgame only |
| Transmog | Changes appearance only | Gold |

Cost escalation: each reforge on the same item raises its material cost by 25 percent. This creates a natural stopping point and keeps drops relevant.

## Item tooltips (one-hand layout)

- Tooltips open as a bottom sheet, reachable with the thumb.
- Layout order: name and rarity, power score with an arrow, base stat, affixes with tier dots, sockets, unique power text, comparison strip.
- The comparison strip shows the equipped item side by side and highlights gains in green and losses in red.
- Actions sit in one row at the bottom: Equip, Salvage, Lock. Lock prevents salvage and stash cleaning.
- Swipe left and right on the sheet moves between drops in the results list.
