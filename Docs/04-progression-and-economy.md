# Progression and economy

## Progression layers

| Layer | Range | Speed | Role |
|---|---|---|---|
| Character level | 1 to 60 | Hours 0 to 18 | Unlocks skills, passive points, zone access |
| Gear | Item level 1 to 160 | Entire game | Main power axis |
| Skill level and modifiers | 1 to 20 per skill | Hours 0 to 30 | Build shaping |
| Paragon | 1 to 200 after level 60 | Hours 18 onward | Slow stat growth, keeps XP relevant |
| Difficulty tier | Vigil I to Vigil V | After act 5 | Raises enemy level and drop quality |
| Abyss depth | 1 to unbounded | Endgame | Leaderboard content and top item level |

## XP curve

XP needed to advance from level n to n plus 1: 400 times n to the power 2.2, rounded. Enemy XP at level L: 8 times L to the power 1.5. XP is scaled by the level difference between player and enemy: 100 percent within plus or minus 3 levels, dropping 12 percent per level below, and 8 percent per level above (capped at 150 percent).

| Level | XP to next | Cumulative XP | Normal enemy XP | Kills per level |
|---|---|---|---|---|
| 1 | 400 | 400 | 8 | 50 |
| 5 | 13,797 | 28,965 | 89 | 154 |
| 10 | 63,396 | 230,974 | 253 | 251 |
| 15 | 154,689 | 804,344 | 465 | 333 |
| 20 | 291,290 | 1,968,881 | 716 | 407 |
| 30 | 710,766 | 7,023,161 | 1,315 | 541 |
| 40 | 1,338,419 | 17,405,577 | 2,024 | 661 |
| 50 | 2,186,724 | 35,268,944 | 2,828 | 773 |
| 60 | 3,265,824 | 62,877,084 | 3,718 | 878 |

Total normal-enemy kills from level 1 to 60 at even level: about 31,400. At an average of 30 kills per minute this is about 17 hours. Elites give 8 times normal XP, bosses 60 times, zone completion bonus 20 percent of a level at that tier. The target for a first play through is 15 to 20 hours to level 60.

Level pacing targets:

| Milestone | Target play time |
|---|---|
| Level 10, act 1 boss | 1 hour |
| Level 20, act 2 boss | 3 hours |
| Level 35, act 3 boss | 7 hours |
| Level 50, act 4 boss | 12 hours |
| Level 60, act 5 boss | 17 hours |

## Level rewards

- Level 2 onward: 1 passive point per level.
- Skill point: 1 per level to level 30, 1 per 2 levels after.
- Level 1: Class base skills 1 and 2. Additional skills unlock at levels 3, 6, 10, 14, 18, 22.
- Slot unlocks: skill slots 3 and 4 at levels 8 and 20. Keystone slot at level 30.

## Paragon

After level 60, XP feeds Paragon levels. Each Paragon level requires 4 million XP at first, rising 1 percent per level. Each level grants one point to Might, Agility, Will or Vitality, worth half of a regular point. A Paragon cap of 200 grants about 100 points worth of stats. This is deliberately small next to gear. Its job is to give feedback for time spent on farming days when no upgrade drops.

## Difficulty tiers

After clearing act 5, the world can be replayed at higher tiers. Each tier raises enemy level, adds elite modifiers and unlocks higher item level drops.

| Tier | Enemy level offset | Elite chance | Item level cap | Notes |
|---|---|---|---|---|
| Vigil I | 0 | 10 percent | 60 | Campaign |
| Vigil II | plus 10 | 14 percent | 80 | Cursed affix pool unlocked |
| Vigil III | plus 20 | 18 percent | 100 | Boss adds a second phase set |
| Vigil IV | plus 30 | 22 percent | 130 | Champions drop Rare floor |
| Vigil V | plus 40 | 26 percent | 160 | Best drops, boss mechanics changed |

Enemy level offsets apply on top of the player's level for scaling formulas. Enemies use the formulas in 03-itemization.md with L equal to player level plus offset.

## Currencies and materials

| Currency | Source | Use | Sink |
|---|---|---|---|
| Gold | Enemy drops, chests, salvage sales | Forge actions, respec, stash slots | Forge costs, respec, stash |
| Ash | Salvage common | Low-tier gem fuse | Gem fuse |
| Cinders | Salvage magic | Add socket to magic and rare | Socket |
| Bloodstone | Salvage rare, elites | Reforge, sockets | Forge |
| Soulglass | Salvage legendary, bosses | Temper, Reroll values, Imprint | Forge |
| Ember Shard | Rare drop from bosses, Abyss milestones | One-time unlocks. Its former use as an instant revive is under review, see 08-production.md | Rare use |

Gold drops scale with level: normal enemy drops 0.6 times L to the power 1.3, elite 8 times that. Forge costs use the same curve times an action multiplier. The intended feel: gold is plentiful early and becomes a real limit only when reforging repeatedly at endgame.

Economy rules:

- No premium currency and no gold sink that punishes a normal play session.
- All materials stack without limit.
- The stash, inventory and currencies are saved on every gain.

## Difficulty adaptation

The game does not silently scale enemies to the player. Difficulty is set by zone and tier. A visible recommended power score is shown per zone. If the player fails a boss three times the game offers a hint pointing to the weakest defensive stat, and never lowers boss health.

## Offline and session rules

- No offline progression. The game only moves when the player moves.
- Rest bonus: none.
- The daily login reward does not exist.
- Pausing at any time is safe. The save is written when the app moves to the background.

## Session pacing targets

| Session type | Length | Loop |
|---|---|---|
| Quick | 3 to 5 min | One Rift, review drops |
| Standard | 8 to 12 min | A zone or an Abyss run with Forge visit |
| Long | 20 to 40 min | Boss chain plus build changes |

Meaningful upgrades per 10 minutes: at least 1 in the first 5 hours, at least 0.5 up to level 60, at least 0.2 in endgame.

## Balance workflow

1. Build a simulation in the tooling repository (Python or a Unity editor tool) that models a character with item level, affixes and skills against enemy stats at each zone level.
2. Target time to kill: normal enemy 0.6 to 1.2 seconds, elite 5 to 9 seconds, zone boss 60 to 120 seconds, at a gear score equal to the zone recommendation.
3. Target damage taken: a well-played clear ends with the character at 55 to 75 percent life on average, boss fights end with 1 to 2 potions used.
4. Run 10,000 simulated clears per archetype per act. Flag any archetype that clears more than 20 percent faster or slower than the median.
5. Playtest with real players at each milestone and compare the loot rate to the targets above.
