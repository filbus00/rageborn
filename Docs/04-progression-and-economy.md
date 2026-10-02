# Progression and economy

## Progression layers

| Layer | Range | Speed | Role |
|---|---|---|---|
| Character level | 1 to 60 | Hours 0 to 18 | Unlocks skills, passive points, zone access |
| Gear | Item level 1 to 160 | Entire game | Main power axis |
| Skill level and modifiers | 1 to 20 per skill | Hours 0 to 30 | Build shaping |
| Paragon | 1 to 200 after level 60 | Hours 18 onward | Slow stat growth, keeps XP relevant |
| Difficulty tier | Vigil I to Vigil V | Five passes through the campaign | Raises enemy level and drop quality |
| Abyss depth | 1 to unbounded | Endgame | Personal best and top item level |

## XP curve

XP needed to advance from level n to n plus 1: 400 times n to the power 2.2, rounded. Enemy XP at level L: 8 times L to the power 1.5. XP is scaled by the level difference between player and enemy: 100 percent within plus or minus 3 levels, dropping 12 percent per level below, and 8 percent per level above (capped at 150 percent). The steps count from the edge of that band: an enemy 4 levels below gives 88 percent, 5 below 76 percent.

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

Total normal-enemy kills from level 1 to 60 at even level: about 31,400. At an average of 30 kills per minute this is about 17 hours. Elites give 8 times normal XP, Champions 3 times, bosses 60 times, zone completion bonus 20 percent of a level at that tier. The target for a first play through is 15 to 20 hours to level 60.

Level pacing targets: the character reaches level 60 over five passes through the campaign, one per Vigil tier (decided 2026-09-27, Q1; this replaces the earlier table, which put level 60 at the first act 5 boss). The milestones are in "Recommended level plan" below (Vigil I's story ends near level 21 after about 3.5 hours, level 60 comes near the end of Vigil V at about 18 hours); those numbers are targets to tune.

## Level rewards

- Every level up refills life.
- Level 2 onward: 5 stat points per level for the Wild Arrow's five attributes (decided 2026-09-30: no passive tree; the amount is proposed, 02). The retired Wrathborn got 1 passive point per level.
- Skill point: 1 per level to level 30, 1 per 2 levels after.
- Skills for the Wild Arrow (2026-09-30): Split Arrow 1, Pierce Arrow 2, Homing Arrow 4, Explosive Arrow 6 (02); the other four are held. The retired Wrathborn's, Skills (decided 2026-09-27, Q4, replacing "1, 2, 3, 6, 10, 14, 18, 22"): Hew 1, Hurl Axe 2, Bull Rush 4, Ground Breaker 6 (built), Battle Roar 9, Rending Spin 12, Blood Frenzy 15, Skullsplitter 18. The loadout screen opens at level 9.
- Slots (decided, Q4, replacing the slot unlocks at 8 and 20 and the keystone slot at 30): all four skill slots are open from level 1; keystones can be bought from level 20.
- Attributes (decided, Q6): automatic growth per level, see 02.

## Paragon

After level 60, XP feeds Paragon levels. Each Paragon level requires 4 million XP at first, rising 1 percent per level. Each level grants one point to Might, Agility, Will or Vitality, worth half of a regular point. A Paragon cap of 200 grants about 100 points worth of stats. This is deliberately small next to gear. Its job is to give feedback for time spent on farming days when no upgrade drops.

## Difficulty tiers

The five tiers are five passes through the campaign (decided 2026-09-27, Q1): Vigil I is the story, and each later tier opens when the act 5 boss of the tier before is dead. Each tier raises enemy level, adds elite modifiers and unlocks higher item level drops. The table below is the original; its offsets and elite chances are replaced by "Proposed Vigil tier table" below, which follows the decisions (its numbers are still proposals).

| Tier | Enemy level offset | Elite chance | Item level cap | Notes |
|---|---|---|---|---|
| Vigil I | 0 | 10 percent | 60 | Campaign |
| Vigil II | plus 10 | 14 percent | 80 | Cursed affix pool unlocked |
| Vigil III | plus 20 | 18 percent | 100 | Boss adds a second phase set |
| Vigil IV | plus 30 | 22 percent | 130 | Champions drop Rare floor |
| Vigil V | plus 40 | 26 percent | 160 | Best drops, boss mechanics changed |

Enemy levels are fixed per depth and tier, never set from the player's level (decided 2026-09-27, Q2, as built for act 1 and as "Difficulty adaptation" below says). The "offset" column above is therefore not an offset on the player's level; enemies use the formulas in 03-itemization.md with L equal to their depth's level on that tier. Vigil I's elite rooms are 15 percent, as the generator builds them (Q22).

## Currencies and materials

| Currency | Source | Use | Sink |
|---|---|---|---|
| Gold | Enemy drops, chests, selling gear to the merchant in town (2026-10-02, replacing Q26's "no vendor"), a little from salvage | Forge actions, respec, stash tabs and loadout presets (Q16) | Forge costs, respec, stash, presets |
| Ash | Salvage common | Low-tier gem fuse | Gem fuse |
| Cinders | Salvage magic | Add a socket to a Rare (see the material economy below) | Socket |
| Bloodstone | Salvage rare, elites | Reforge, a socket on a Legendary | Forge |
| Soulglass | Salvage legendary, bosses | Temper, Reroll values, Imprint | Forge |

Gold drops scale with level: normal enemy drops 0.6 times L to the power 1.3, Champion 3 times that, elite 8 times that. Forge costs use the same curve times an action multiplier. The intended feel: gold is plentiful early and becomes a real limit only when reforging repeatedly at endgame.

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

## Full-game plan (proposed, with the owner's decisions of 2026-09-27)

Written 2026-09-27 for the full-game plan (`10-full-game-plan.md`). Every number in this section is a proposal and a tuning value. The owner answered its questions the same day (`08-production.md`, "Open questions from the full-game plan"): the level plan's shape (Q1), fixed enemy levels (Q2), the level rewards (Q4), gold for one-time unlocks (Q16) and no vendor (Q26) are decisions, and the text above now follows them.

### The gap between the XP curve and the campaign (Q1)

The XP curve above is built for 15 to 20 hours to level 60: about 31,400 normal kills at even level. The campaign is much shorter than that:

- A dungeon level holds about 100 enemies, worth about 140 normal kills of XP once champions and elites count (the act 1 balance report, `Logs/BalanceReport.md`: 1,193 XP on depth 1 at level 1, 12,546 on depth 5 at level 5). It takes 4 to 8 minutes (05).
- 30 dungeon levels and 5 bosses give about 4,300 normal kills of XP, a seventh of what level 60 needs.
- Simulated with the real formulas and that density, one pass through the five acts with enemies at the character's level ends at level 19. With enemies 7 levels above (the 150 percent XP cap) it ends at level 31. Act 1 as built ends at level 7.4, which matches the game.
- So the act ranges in 05 (act 2 at 12 to 24, act 5 at 48 to 60) and the pacing table above ("level 60, act 5 boss, 17 hours") cannot both hold with 6 levels per act and 4 to 8 minutes per level. The campaign is about 3.5 hours; the curve wants about 17.

The two are consistent in one way: at even level the curve needs about 7 full campaigns of kills, or 5 when enemies stay 4 levels above the character. That is Diablo 2's shape: the story is played through on rising difficulties, and the character reaches the top level on the last one.

### Level plan: the campaign on rising Vigil tiers (decided, Q1)

**Decided (Q1 and Q2, 2026-09-27):** the five Vigil tiers become five passes through the same five acts, each with its own fixed enemy level per depth (as act 1 is built today). A tier opens when the act 5 boss of the tier before is dead. Rifts and the Abyss (05) run at the tier the player picks and give the same XP per minute, so a player can farm instead of replaying.

Enemy level per depth (depths 1 to 6 of each act; the sixth is the boss level). Vigil I act 1 is the built table. The rest follows a simulated full clear: Vigil I keeps enemies 2 levels above the character, later tiers 4 above, so the XP band stays at full or better.

| Tier | Act 1 | Act 2 | Act 3 | Act 4 | Act 5 | Character level at the end of the tier |
|---|---|---|---|---|---|---|
| Vigil I | 1, 2, 3, 4, 5, 7 (built) | 9, 10, 11, 12, 12, 14 | 14, 14, 15, 16, 16, 18 | 18, 18, 18, 19, 19, 21 | 21, 21, 21, 22, 22, 23 | 21 to 22 |
| Vigil II | 25 to 28 | 28 to 31 | 31 to 33 | 33 to 36 | 36 to 38 | 34 |
| Vigil III | 37 to 40 | 40 to 42 | 42 to 44 | 44 to 45 | 45 to 47 | 43 |
| Vigil IV | 46 to 49 | 49 to 50 | 50 to 52 | 52 to 53 | 53 to 55 | 51 |
| Vigil V | 54 to 56 | 56 to 58 | 58 to 59 | 59 to 60 | 60 to 62 | 58, then 60 after about an hour of rifts or Abyss |

The enemy levels in the table are proposals from a simulated full clear, to be tuned with the balance report. Character level at the end of each act on Vigil I: 7, 12, 16, 19, 21.5; these replace the "Player level" column of the act table in 05.

Consequences (the second is decided, the rest proposed):

- Item level follows the enemy level, so the tier sets the best items: Vigil I tops out at item level 23, Vigil II at 38, III at 47, IV at 55, V at 62, the Abyss at 160 (03). Affix tiers open along the way: T4 (item level 15) in Vigil I act 3, T3 (30) in Vigil II act 3, T2 (45) in Vigil III act 5, T1 (60) in Vigil V act 4.
- Enemy levels stay fixed per depth and tier, never scaled to the player (decided, Q2). This agrees with "Difficulty adaptation" above and with the build; the Vigil table above now says so.
- A full clear of every level is not required: a player who skips rooms falls behind and uses rifts to catch up. The recommended power score per level (Difficulty adaptation, above) tells them.
- Farming a finished level again needs the sleep reset (decided, Q14: a bed at each town's inn, 05), since killed enemies stay dead for the session.

Proposed pacing (5.5 minutes a dungeon level, 2 minutes a boss, 5 minutes in town per act; the balance report puts act 1 fights at 24 to 123 s a level with typical gear, plus walking):

| Milestone | Level | Play time |
|---|---|---|
| Vigil I, Cinder Warden | 7 | 45 minutes |
| Vigil I, act 5 boss (story ends) | 21 | 3.5 hours |
| Vigil II, act 5 boss | 34 | 7 hours |
| Vigil III, act 5 boss | 43 | 10.5 hours |
| Vigil IV, act 5 boss | 51 | 14 hours |
| Vigil V, act 5 boss | 58 | 17.5 hours |
| Level 60 | 60 | 18 to 19 hours |

This keeps the "15 to 20 hours to level 60" target and the XP formulas unchanged, and replaces the old pacing table above (decided, Q1).

### Proposed Vigil tier table

Replaces the numbers of the tier table above (the five-pass shape is decided, Q1; these numbers are proposals). Elite chance is the share of rooms that are elite rooms (15 percent in the generator today, which the table above now follows, Q22).

| Tier | Opens | Enemy levels | Item level cap | Elite rooms | Extra rules (from the table above, kept) |
|---|---|---|---|---|---|
| Vigil I | New character | 1 to 23 | 23 | 15 percent (built) | Campaign, story scenes play |
| Vigil II | Vigil I act 5 boss | 25 to 38 | 38 | 17 percent | Elites roll two modifiers 75 percent of the time instead of the built 50 (tuning); Vigil IV and V always two |
| Vigil III | Vigil II act 5 boss | 37 to 47 | 47 | 19 percent | Bosses gain their Vigil III attack (05, Bosses, one extra attack per phase) |
| Vigil IV | Vigil III act 5 boss | 46 to 55 | 55 | 21 percent | Champions drop at a Rare floor |
| Vigil V | Vigil IV act 5 boss | 54 to 62 | 62 | 23 percent | Boss attacks faster (fill times 80 percent), best drop table |
| Abyss | Vigil I act 5 boss | 2 per floor from the tier's start | 160 | every floor is a room | 05, Endgame |

Story scenes play only on the first Vigil I pass and can be replayed from Settings (06).

### Proposed drop tables per tier

Base values are the built ones (03). Tiers raise chances, never lower them. All tuning.

| Source and roll | Vigil I (built) | Vigil II | Vigil III | Vigil IV | Vigil V | Abyss guardian |
|---|---|---|---|---|---|---|
| Normal enemy drop chance | 6 percent | 7 | 8 | 9 | 10 | not applicable |
| Normal rarity weights (Common, Magic, Rare, Legendary) | 60, 30, 8.5, 1.5 | 55, 32, 11, 2 | 50, 34, 13.5, 2.5 | 45, 36, 16, 3 | 40, 37, 19, 4 | Rare floor |
| Champion | 25 percent, Magic floor | 30, Magic | 35, Magic | 40, Rare | 45, Rare | |
| Elite Rare floor chance | 30 percent | 35 | 40 | 50 | 60 | |
| Elite items | 1 to 2 | 1 to 2 | 2 | 2 | 2 to 3 | |
| Chest items | 1 to 3, Magic floor | 1 to 3 | 2 to 3 | 2 to 3 | 2 to 4 | |
| Boss items and Legendary chance | 4 to 6 at Rare, 35 percent | 4 to 6, 45 | 5 to 6, 55 | 5 to 7, 65 | 6 to 7, 80 | 3 to 5 at Rare, +5 percent Legendary per guardian in the run, up to 60 |
| Cursed chance (a Legendary rolls Cursed) | 0 | 0 | 0 | 0 | 0 | 10 percent from depth 30 (03) |
| Sockets roll (Rare, Legendary) | 03 | 03 | 03 | 03 | 03 | 03 |

Bad-luck protection (03, built) is unchanged at every tier. At Vigil V a normal kill gives a Legendary about 4 times as often as on Vigil I (10 percent drop chance times a weight of 4 in 100, against 6 times 1.5). The endgame target (03) is one about every 12 minutes, about twice the campaign rate, so these weights are likely too generous; the balance simulation should set them.

### Level rewards, reconciled with the build (decided, Q4)

Built: Hew at level 1, Hurl Axe 2, Bull Rush 4, Ground Breaker 6; all four equip themselves, so there is no slot unlock. **Decided (Q4, 2026-09-27)**, following the build and the Vigil I levels ("Level rewards" above now says the same):

| Level | Reward |
|---|---|
| 1, 2, 4, 6 | Split Arrow, Pierce Arrow, Homing Arrow, Explosive Arrow (the Wild Arrow, 2026-09-30; the Wrathborn's Hew, Hurl Axe, Bull Rush, Ground Breaker were built) |
| 9 | Battle Roar (Vigil I act 2). The loadout screen opens: from here the player chooses which four to carry |
| 12 | Rending Spin (act 2 boss) |
| 15 | Blood Frenzy (act 3) |
| 18 | Skullsplitter (act 4) |
| 20 | Keystone slot (the tree's keystones can be bought from then, 02) |
| Every level from 2 | 5 stat points for the Wild Arrow (1 passive point for the retired Wrathborn) (above), 1 skill point to 30 and 1 per 2 levels after (above) |

All four skill slots are open from level 1 (they fill as skills unlock); the slot unlocks at levels 8 and 20 are dropped.

### Proposed gold economy

Gold per normal kill is 0.6 times L to the power 1.3 (built). A full dungeon level gives about 155 times that (about 100 kills, champions 3 times, elites 8 times, a chest 5 times), and a boss 20 times.

| Enemy level | Gold per normal kill | Gold per dungeon level | Reforge (10 times) | Temper (25 times) |
|---|---|---|---|---|
| 1 | 1 (floor) | about 90 | 6 | 15 |
| 7 | 7.5 | 1,170 | 75 | 190 |
| 25 | 39 | 6,100 | 390 | 990 |
| 45 | 85 | 13,100 | 850 | 2,100 |
| 60 | 123 | 19,100 | 1,230 | 3,070 |

Sources: kills, chests, bosses (built); proposed additions: salvage gives gold equal to 2 normal kills at the item's level (so salvaging is never worthless), rift chests 10 times, Abyss floors 5 times per floor.

Sinks (proposed, tuning):

| Sink | Cost | Why |
|---|---|---|
| Forge actions | Built multipliers of the curve at item level | Main sink, escalating per item |
| Stash tabs | 6 tabs of 30 beyond the first 120 slots: 2,000, 5,000, 12,000, 25,000, 50,000, 100,000 | 03 caps the stash at 300. Paid with gold: decided 2026-09-27 (Q16); the prices are proposals |
| Loadout presets 2 and 3 | 5,000 and 20,000 | Paid with gold: decided (Q16); prices proposed |
| Respec (Trainer) | Passive tree: 50 times the curve at the character's level, capped at 5,000 (02). Skill points: free while under level 20, then the same | 02 says respec cost is capped at 5,000 |
| Socket (Forge) | 20 times the curve plus materials | 03 |
| Gem fusion | 5, 15, 40, 100 times the curve for each step up | 03 |
| Transmog | After launch (decided, Q24) | |
| Vendor | A merchant buys gear, sells nothing (2026-10-02; Q26 was "none"). Price: 2, 6, 15, 40 gold by rarity, times 1 + 0.15 x item level; a named legendary x 1.5 | `SellRules` |

Rule kept from above: no sink punishes a normal session, and a character never needs to farm gold to keep playing.

### Proposed material economy

Estimated per full dungeon level on Vigil I (about 16 items: 6 from normal kills, 7 or 8 from elites, 2 from the chest, 1 from champions): 7 Common, 5 Magic, 4 Rare, a Legendary every 25 minutes (03's target).

| Material | Salvage (built) | Other sources | Per dungeon level (salvage all) | Uses |
|---|---|---|---|---|
| Ash | Common: 2 | none | 14 | Gem fusion Chipped to Flawed (6 Ash) (proposed; Transmog, which would also use Ash, comes after launch, Q24) |
| Cinders | Magic: 2 | none | 10 | Gem fusion Flawed to Normal (6), socket on a Rare (6) (proposed) |
| Bloodstone | Rare: 2 | Elites 1 each (built) | 13 | Reforge (built 2, rising 25 percent), socket on a Legendary (3), gem fusion Normal to Flawless (4) |
| Soulglass | Legendary: 1 | Bosses 3 (built) | about 1 per act from salvage, 3 per boss | Reroll values (built 1), Temper (built 2), Imprint (5, proposed), gem fusion Flawless to Perfect (2) |

Ash and Cinders have no use in the built Forge yet; the proposal above gives them one. The currency table above named Cinders for sockets on Magic and Rare items, while 03 puts sockets on Rare and Legendary only and prices them in Bloodstone; the owner chose to settle such conflicts in favour of the build or a decision (Q22), and since neither covers sockets, both docs now follow this table's reading: sockets on Rare (6 Cinders) and Legendary (3 Bloodstone) items only, a proposal to confirm when sockets are built.

### Paragon (proposed details)

Kept as above: level 60 XP feeds Paragon, 4 million XP for the first level, rising 1 percent a level, to 200. At enemy level 60 to 80 a dungeon level gives 0.5 to 0.8 million XP, so Paragon 50 comes after about 50 hours and 200 after about 250 to 300 hours. Each Paragon level gives one point in an attribute of the player's choice, the only free attribute choice, since levels grow attributes automatically (decided, Q6). The Paragon number shows next to the level on the HUD and in character select.
