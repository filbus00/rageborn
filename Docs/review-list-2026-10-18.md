# Decisions to review (2026-10-18)

Every row of Docs/08-production.md still marked "Claude, to review" (or "numbers Claude, to review"), one line each, grouped by topic, newest first. The date is the row's date in Docs/08. Answer yes or no per line; Docs/08 has the full text. Where a row no longer matches the game, it says so.

## Combat and boons

- 2026-10-18: The aim as a partial circle: a light arc 90 degrees wide round her feet, radius 1.35, facing her target, shown from her first target until 1.5 s after the last; the stray arc on it white standing (6 degrees), amber walking (about 17), red running (about 23). (Look.)
- 2026-10-17: Six unique boons, offered from depth 1: Thunder Arrow (every 8th shot, lightning on every enemy within 2.5, 80 percent of the arrow each); Marked Prey (every 8 s the toughest enemy on screen takes +50 percent from her arrows, its death gives back a dive); Dive Volley (each dive looses an arrow at the 4 nearest enemies); Vengeful Quiver (when hurt, an arrow at each of up to 5 enemies near her, once a second); Wildfire (needs Fire Arrow: a burning enemy that dies sets everything within 2.5 burning); Shatter (needs Freezing Arrow: a frozen enemy that dies strikes and chills everything within 2.5).
- 2026-10-17: Arrow-multiplying boons rarer: Extra Arrow and Extra Shot offered a quarter as often as others, Splitting Arrows and Ricochet half as often; ranks unchanged; the card marks them Rare.
- 2026-10-17: Momentum shots: each Momentum stack (one per 0.6 s moving, 5 at most, lost after 1.2 s still) adds 6 percent attack speed and 4 percent arrow speed and reach (+30 and +20 percent at five). The cover half of this row was removed on 2026-10-17 by the owner ("remove the cover mechanism", Docs/08 row 477), so only Momentum is left to review. Note: CLAUDE.md still describes cover under Combat; it is out of date.
- 2026-10-15: Eight boons that change the arrows (plain stat nodes no longer boons): Piercing Arrows (3 ranks, +1 pierce), Splitting Arrows (fork on first hit), Seeking Arrows, Bleeding Arrows (30 percent of weapon damage a second for 3 s), Ricochet (3 ranks, leaps to the nearest enemy within 6), Arrow Nova (every 6th shot, 8 arrows at 60 percent), Corpse Burst (60 percent of the arrow within 2.2), Chain Lightning (3 ranks, 15 percent a rank, arcs to 3 enemies within 4.5 for half the hit).
- 2026-10-13: Four boons: Fire Arrow (1 rank, every arrow burns); Freezing Arrow (1 rank, every arrow chills twice, two hits within 2 s freeze for 1 s, never a boss); Extra Arrow (3 ranks, +1 arrow a rank, fanned 7 degrees); Extra Shot (3 ranks, the bow fires again a rank, 0.12 s apart). All in the first two rows, offered from depth 1.
- 2026-10-10: Dodge on leaving the ring: "a fight" is a target within the last 1.5 s; charge diamonds at the ends of the Focus arc.
- 2026-10-10: Dodge numbers: 0.4 s dive, untouchable the first 0.35 s against everything. (The flick trigger in this row was replaced the same day by leaving the ring.)
- 2026-10-10: Walk below 2.2 units a second, with a 0.3 margin.
- 2026-10-10: Aim marker as a faint wedge. Replaced by the partial circle on 2026-10-18; can be closed.
- 2026-10-08: Aim by stick push: bands at a third and two thirds of the push; stray up to 6.8 degrees divided by the aim; one roll per shot.
- 2026-10-07: Talent trees: the nodes and numbers, 5 points a row, Improved nodes +15 percent skill damage a rank, the Trainer resets talents. (In the roguelite these are the boons.)
- 2026-10-04: Balance pass: enemy life level^1.6 (Docs/03 had 1.9), the Cinder Warden 60 times a normal enemy's life, the skill numbers in Docs/02.
- 2026-10-04: All nine skills unlock at level 1; the skill numbers in Docs/02.
- 2026-10-03: Hit stop only on champion, elite and boss kills, at most once a second.
- 2026-10-03: She keeps her target until another is 0.75 units closer.
- 2026-10-03: Disengage: out at 1.2 stick radii, back in at 1.05; no arrows or skills outside; a draw in progress is cut.
- 2026-10-03: Walk while firing: half speed for up to 0.6 s after each shot or skill.
- 2026-09-30: The Wild Arrow's numbers: skills, unlock levels, cast order, attribute effects, 5 points a level, pets, bow and quiver numbers. Several parts are retired by the roguelite (no levels, no attribute points).
- 2026-09-30: Bows-only choices: enum names kept, passive tree hidden, Vitality on top of class life, the pet draws only melee blows, pet rules as one target choice plus four toggles.

## Enemies

- 2026-10-18: Better telegraphs, pools and projectiles: notched double rims, a fill that closes in, a pulse in the last quarter, shock rings, chevron lanes with a fill, pools as uneven blobs with bubbles, glows and trails on projectiles. (Look.)
- 2026-10-17: More thrown attacks, fewer pools: pools kept for the Bile Fiend, Arachnid, Retcher and Dark Cultist; knives (Swarmer, Quill Rat, Wight, Harpy), axes (Fallen, Ogre), fireballs (Zombie, Dark Cultist, Stygian Demon, Balrog), bombs (Imp, Giant), bouncing blades, 3 bounces (Ghoul, Wraith, Gargoyle); elite pools: Molten trail 2.5 s every 2 s, Desecrator every 8 s for 1.5 s, Plagued every 9 s for 3.5 s. Bomb radius 1.5, fuse 0.7 s; fireball burst 1.1 at 60 percent.
- 2026-10-17: Steadier pressure: normal packs 8 to 15 (were 6 to 12), elite packs 4 to 6 (were 3 to 4); a ranged back row in 85 percent of packs (was 60) of 3 or 4 (was 2 or 3); ranged attacks twice as often as Docs/03; their projectiles at Docs/03's damage (were 1.4 times).
- 2026-10-17: The Retcher: depths 4 to 20, weight 5, at the back of packs; a slowing bile glob; every 5 s three bile lanes (slowing) and a poison puddle for 4 s; 1.3 times normal life.
- 2026-10-17: Enemy health bars: thin red bar 0.6 wide, shown only when hurt; none on bosses.
- 2026-10-15: Bosses play their flinch at most once every 5 s.
- 2026-10-13: The new roster from the concept sheet: 25 monsters by band, each fighting as an existing kind, with the ranged move its card names as its special; pack makeup. (Mapping, numbers, models.)
- 2026-10-11: Projectiles: ranged enemies fire half again as often, projectiles 1.5 times as fast and 1.4 times the damage; which specials throw. Note: the 1.4 damage and 1.5 rate were changed on 2026-10-17 (damage 1, rate 2), so only the speed and the throw list are left from this row.
- 2026-10-11: Every enemy type's special: timing, range, size, damage (Docs/05); bosses left out.
- 2026-10-08: Elite affixes: the depth each opens, 1 affix to depth 6, 2 from 7, 3 from 19, at least one attack always, one roll per pack, every number in the table.
- 2026-10-05: Bosses at 12, 18, 24 and the deep roster (Drowned, Harpooner, Drowned Watchman, Skeleton Knight, Grave Priest, Hollowed, Void Wraith, Rift Caller): first depths and spread. (The bosses have since moved: 48 depths.)
- 2026-10-05: Act 1 roster shares: wolf packs 1 in 5 from depth 3, cutthroats in 30 percent of packs, acolytes 35, keepers 30 of packs of 4 or more, bloats 40 from depth 5; enemies wake only with sight or a route 1.75 times aggro range. (The roster was replaced on 2026-10-13; the wake rule still applies.)
- 2026-10-05: The enemy level curve past depth 6 (0.6 a level rounded up), how the light darkens, the newcomers' prices.
- 2026-10-04: Skeleton and cultist from depth 4 (35 percent of husks, half the archers). (Roster replaced 2026-10-13.)

## Loot and economy

- 2026-10-17: Early gear weaker: flat weapon damage, life and armour affixes scale by (item level + 5) / 65, full at 60 (about a tenth at level 1).
- 2026-10-17: Selling to Garrett: prices three times the old ones (`SellRules.RogueliteFactor`); blueprints not sold.
- 2026-10-15: Blueprints: drop 0.2 percent from normals, 1.2 champions, 4 elites and chests, 35 bosses; Thorin forges a Rare for 700 to 10,300 coins; it becomes that kind's kit piece after a death.
- 2026-10-15: Portal home takes 2.5 s and any hit breaks it. Stash chest 10 slots, +10 a rank, 5 ranks, 500 coins doubling.
- 2026-10-13: Potion charges only from Miranda's Deeper Flasks, filled at a run's start, not on reopening the game mid-run.
- 2026-10-13: The roguelite: boons are talent ranks from rows 1 and 2 until Hidden Lore; her level follows the depth's enemy level; the shops and their contents; every price a first guess; the stash prices in this row (5,000 coins, doubling) were replaced on 2026-10-15 by the chest above.
- 2026-10-02: Merchant prices 2, 6, 15, 40 gold by rarity, +15 percent per item level, named legendary half as much again. (Now times 3, see above.)
- 2026-10-01: Affixes 90 to 100: fixed affixes keep their value at every tier; fork rolled when loosed; far and point blank count as increased damage; power score leaves them out.
- 2026-10-01: The other 13 legendaries: homes and details of each power.
- 2026-10-01: First 11 legendaries: item level 3 minimum, homes as drop sources.
- 2026-09-28: Six more slots; drops pick the slot evenly; ring placement rule.
- 2026-09-29: Axe off-hand and grips (the retired Wrathborn); can be closed.

## Town and shops

- 2026-10-17: Deep entrances: a run started deeper is owed a boon for every skipped depth (ten choices at depth 10).
- 2026-10-17: Townsfolk at least 5.5 cells apart, each by their own place; an iron-bound stash chest beside Elara.
- 2026-10-15: Pet skills: four a pet, three ranks (600, 2,400, 5,400 coins a rank), on while that pet follows, each pairing with a boon (the full list is in Docs/08).
- 2026-10-12: Town rebuilt after the concept art: church, smithy, stables, the Waystone as a blue dais, gate at the bottom left. (Layout.)
- 2026-10-12: Walled town: 44 cells square with corner towers, a back gate to the graveyard, each person a house or stall. (Layout.)
- 2026-10-11: The town dark: ambient 0.55, 0.6, 0.8 at 0.42 (was 0.62), paved square and roads, mud and puddles, the mausoleum stairs.
- 2026-10-08: Quests (contents). Quests are switched off in the roguelite; can wait.

## UI

- 2026-10-17: Tidier menus: shared sheet parts, scrollbars, short sheets fitted to their lists, the Boons tab listing this run's boons and those still to find, the Codex grouped, the Bag's header showing the depth.
- 2026-10-15: A run summary sheet when a run ends (how it ended, depth, time, kills, coins, items, blueprints, boons).
- 2026-10-15: Boon icons: pixel glyphs framed in the tree's colour, a gold plus on Improved nodes, the rank in the corner, eight to a row under the depth.
- 2026-10-13: "Depth N" (or "Town") in gold under the potion pips.
- 2026-10-02: Item icons drawn in code; loot hop 0.6 units; Rare markers; label lifting.

## Art and sound

- 2026-10-17: Dungeon music recomposed: 75 a minute in D, five sections over 128 s.
- 2026-10-13: Our own models for her body, bow, quiver and hood; the new monsters' models. (Models.)
- 2026-10-12: Town and dungeon music fuller (town 96 s in three passes, dungeon four 32 s sections).
- 2026-10-12: The prison's look: barred-cell walls, free-standing cages in 70 percent of halls, gibbets, benches.
- 2026-10-12: The coin sound: 0.22 s, 4 dB quieter, a five-note climb over pickups less than 1.2 s apart, softening to 60 percent.
- 2026-10-12: The owner's sounds trimmed, mono, levelled.
- 2026-10-12: 36 depths with the burnt library (since moved: 48 depths, library 25 to 36); its questline and boss texts.
- 2026-10-11: More gore: amounts (splats 30 s, last 6 s fading, 260 at most, 4 to 7 chunks).
- 2026-10-11: Walls about twice her height; stairs three wall cells wide; terrace rims half her height.
- 2026-10-11: Loads: phone builds in Xcode's Release configuration (still a Unity development build); her sheets stay loaded across scenes.
- 2026-10-10: Stairs set into a far wall, as far from doorways as can be.
- 2026-10-10: Crypt then caves: room shapes, terrace rules (one cave in four without), boulders and stalagmites.
- 2026-10-10: App icon cropped 40 px inside its frame; only the display name is "Wild Arrow".
- 2026-10-08: Dungeon rebuilt: 10 to 15 rooms, 3 to 7 cells of rock between, rough passages, collapses, lairs.
- 2026-10-06: The Vigil's road and its length. Reverted on 2026-10-07; can be closed.
- 2026-10-05: Gear remodelled: art direction, five looks per slot, legendary looks.
- 2026-10-04: Walls (fieldstone, four variants) and the first music.
- 2026-10-04: Ground: palette, how much cobble and grass, where the paths run.
- 2026-10-04: Menus, bow sound, ground textures, town 1.6 times wider, gear looks by tier.
- 2026-10-04: Floors, wall details, bows and quivers, the Cinder Warden modelled.
- 2026-10-04: Props modelled in Blender; summoning circle in one combat or elite room in four.
