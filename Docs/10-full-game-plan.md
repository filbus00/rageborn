# Full-game plan: scope map

Document status: proposal, written 2026-09-27 by Claude from the owner's request "Try and plan out the whole game and update the docs". Nothing in this file is a decision. It maps the whole game, 1.0 and the first updates, so the owner can see what exists, what is designed and what still needs a decision.

How to read it:

- **Status** comes from `CLAUDE.md` (what is in the code on 2026-09-27) and from the docs.
  - **Built**: in the game and checked.
  - **Partly built**: some of it is in the game; the note says what is missing.
  - **Designed**: decided or written in the docs before this plan, not built.
  - **Proposed**: new in this plan, not decided. The design is in the doc named in the Where column, under a heading marked "Proposed (not decided)".
- **Q** numbers point to "Open questions from the full-game plan" in `08-production.md`. A line with a Q number cannot be finished until the owner answers it.
- Decided rules are never restated differently here. When the plan and a decision disagree, the decision stands and the disagreement is a question.

## 1. Core play

| Item | Status | Where | Notes |
|---|---|---|---|
| Floating stick, movement only | Built | 01 | Stick size, dead zone and handedness settings not built (Settings screen) |
| Isometric camera, lead, size 10 | Built | 01 | Screen shake and its setting not built |
| Auto-target and basic attack | Built | 01 | Runs in Update; the docs ask for a fixed timestep |
| Auto-cast by slot order and trigger | Partly built | 01, 02 | Fixed trigger per skill kind; no per-slot trigger picker (Q15) |
| Stillness and Momentum | Built | 01 | |
| Rage | Built | 02 | |
| Telegraphs (ground, line) | Built | 01 | Charge telegraph built for the boss only |
| Dodge | Built | 01 | |
| Hit stop, damage numbers, flashes, dissolve | Built | 01 | Number grouping option not built |
| Auto-loot | Built | 01, 03 | |
| Auto-potion | Built | 01 | Threshold setting not built (Settings screen) |
| Death, corpse and corpse run | Built | 01 | |
| Haptics | Not built | 01 | Proposed list in 07, Audio and haptics plan |
| Interruption pause and 3 s resume countdown | Not built | 01 | Save on background is built |

## 2. The Wrathborn

| Item | Status | Where | Notes |
|---|---|---|---|
| Hew, Hurl Axe, Bull Rush, Ground Breaker | Built | 02 | Unlock at 1, 2, 4, 6 and equip themselves |
| Battle Roar, Rending Spin, Blood Frenzy, Skullsplitter | Designed (one line each), expanded as Proposed | 02 | Unlock levels Q4 |
| Keystones Berserker and Juggernaut | Designed | 02 | Where they are bought: Q5 |
| Skill levels 1 to 20 | Proposed | 02 | Scaling rule |
| Skill modifiers at 5, 10, 15 (24 for the class) | Proposed | 02 | |
| Skill tags | Proposed | 02 | Needed by gear |
| Passive tree (60 nodes) | Proposed | 02 | Replaces the stand-in Vitality when built |
| Attributes and attribute points | Designed (effects), Proposed (source) | 02 | Q6 |
| Loadout screen, presets | Designed | 06 | Q15, Q16 |
| Gear on the character (4 layers, 4 grips) | Partly built | 03, 09 | Bake and layered renderer built; off-hand and two-handers are not items; memory and size budget Q3 |
| 16 directions for the player | Built (bake and playback) | 09 | |
| Ranger, Hexer | Designed as drafts | 02 | Post-launch, not expanded |

## 3. Enemies

| Item | Status | Where | Notes |
|---|---|---|---|
| Swarmer, Brute, Archer archetypes | Built | 01 | Husk, Ghoul, Bandit Archer |
| Caster, Charger, Support archetypes | Designed | 01 | |
| 40 normal enemies, 8 per act | 3 built, 37 Proposed | 05 | |
| Champion and Elite ranks, pack rules | Built | 01, 03 | |
| Elite modifiers: Hasted, Vampiric, Frozen | Built | 01 | |
| The other 5 designed modifiers | Designed | 01 | Numbers Proposed in 05 |
| 7 more modifiers to reach 15 | Proposed | 05 | |
| Elemental damage types and resistances | Proposed | 03, 05 | Q9 |

## 4. Bosses

| Item | Status | Where | Notes |
|---|---|---|---|
| Cinder Warden (act 1) | Built | 05 | Art, sounds and reduce flashing not built |
| Tidewife, Saint Marrow, Warlord Kaeth, First Watchman | Designed (one line per phase), expanded as Proposed | 05 | |
| 4 optional bosses | Proposed | 05 | |
| Stagger meter, boss bar | Built | 05 | |

## 5. World and dungeon

| Item | Status | Where | Notes |
|---|---|---|---|
| Level generator, packs, chests, stairs | Built | 05 | |
| Boss arena on the last level | Built | 05 | |
| Low camera-side walls | Built | 09 | |
| Mini-map | Built | 05, 06 | Docs and build differ in size (Q22) |
| Pack mixes per depth | Built for act 1 | 05 | Acts 2 to 5 Proposed |
| Enemy levels by depth | Built for act 1 (1, 2, 3, 4, 5, 7) | 04, 05 | Acts 2 to 5 depend on Q1, Q2 |
| Room templates | 6 placeholders built | 05 | 150 needed (30 per act) |
| Shrine rooms | Designed | 05 | Numbers Proposed |
| Ambush rooms | Designed | 05 | Conflicts with "nothing spawns around the player" (Q12) |
| Acts 2 to 5 (towns, dungeons, themes) | Designed (names), Proposed (content) | 05 | |
| Story beats, short scenes, lore | Designed (outline), Proposed (per act) | 05 | Scene format Q20 |
| Sleep mechanic (session reset) | Not designed | 08 | Q14 |

## 6. Town, NPCs and travel

| Item | Status | Where | Notes |
|---|---|---|---|
| Act 1 town, walkable | Built | 05 | Placeholder ground |
| Forge smith | Built | 03 | |
| Waystone and waypoints | Built | 05 | Tiers, rifts and Abyss entries not built |
| Wanderer and Portal Tome, town portal | Built | 05 | |
| Stash NPC and stash | Designed | 03 | Not built |
| Trainer NPC (respec, skill points) | Designed (name only) | 05 | Duties Proposed in 05 |
| Rift and Abyss NPC | Designed (one line) | 05 | Proposed in 05 |
| Towns for acts 2 to 5 | Proposed | 05 | |
| Vendor (buy and sell) | Not in the docs | 04 | Q26 |

## 7. Items and crafting

| Item | Status | Where | Notes |
|---|---|---|---|
| Weapon, Chest, Helm | Built | 03 | |
| Off-hand, gloves, boots, belt, amulet, 2 rings | Designed (slots), Proposed (bases) | 03 | Off-hand rules Q10 |
| Rarities, drop sources, bad-luck protection | Built | 03 | Cursed not built |
| Affixes | 9 built, about 90 Proposed | 03 | |
| Item level above 60 | Not designed | 03 | Q8 |
| Legendaries with fixed affixes and powers | Proposed (40) | 03 | Count Q7 |
| Sets | Not in the docs | 03 | Q18 |
| Sockets and gems | Designed | 03 | Numbers Proposed |
| Loot filter | Designed | 03 | Q11 (existing question 9) |
| Power score | Built (partly) | 03 | Tags, Life on Hit and cooldown reduction not counted |
| Forge: Salvage, Reforge, Reroll, Temper | Built | 03 | |
| Forge: Socket, Imprint, Transmog | Designed | 03 | Transmog rules Q24 |
| Stash (120 to 300 slots, shared) | Designed | 03 | |
| Appearance tiers per act | Designed for act 1, Proposed for acts 2 to 5 | 03, 09 | |

## 8. Progression and economy

| Item | Status | Where | Notes |
|---|---|---|---|
| XP curve and rewards | Built | 04 | |
| Level 1 to 60 by act | Proposed | 04 | Q1 blocks it: one campaign pass reaches about level 20 |
| Skill points, passive points | Designed | 04 | Not built |
| Paragon | Designed | 04 | Not built |
| Vigil difficulty tiers | Designed, revised as Proposed | 04 | Q1, Q2 |
| Gold and material economy | Partly built | 04 | Sinks Proposed |
| Drop tables per tier | Proposed | 04 | |

## 9. Endgame

| Item | Status | Where | Notes |
|---|---|---|---|
| Rifts | Designed | 05 | Numbers Proposed |
| Abyss | Designed | 05 | Numbers Proposed; death rule Q13 |
| Boss rotation | Designed | 05 | Proposed details |
| Achievements (local) | Designed, list Proposed | 05 | Q17 (existing question 7) |
| Hardcore | Post-launch (decided) | 00 | |

## 10. UI

| Screen | Status | Where |
|---|---|---|
| HUD: life, XP, level, potion pips, Rage arc, stance pips, mini-map, boss bar, hint banner | Built | 06 |
| HUD: skill cooldown icons | Not built | 06 |
| Inventory (Bag) and item sheet | Built | 06 |
| Forge sheet | Built | 06 |
| Waypoint list | Built | 06 |
| Title, character select | Proposed layout | 06 |
| Loadout, skill detail, passive tree | Proposed layout | 06 |
| Equipment paper doll | Proposed layout | 06 |
| Stash, Trainer, Rift and Abyss sheets | Proposed layout | 06 |
| Waystone map (tiers, rifts, Abyss) | Proposed layout | 06 |
| Results | Proposed layout | 06, Q19 |
| Settings | Proposed layout | 06 |
| Loot filter editor | Proposed layout | 06 |
| Story scene player | Proposed layout | 06, Q20 |

## 11. Audio

| Item | Status | Where |
|---|---|---|
| Synthesized placeholder effects, 16 voices, priorities | Built | 07 |
| Placeholder music loop | Built | 07 |
| Layered music stems per act and situation | Proposed plan | 07, Q21 |
| Full sound list per system | Proposed | 07 |
| Mixer, snapshots, volume settings | Not built | 07 |

## 12. Art

| Item | Status | Where |
|---|---|---|
| Brief for act 1 (terrain, player, enemies, boss, NPCs, props, effects, icons, UI, key art) | Designed | 09 |
| Wrathborn leather body, bearded axe, test bake | Built (partly) | 09 |
| Acts 2 to 5 asset lists | Proposed | 09 |
| Item icons for all slots and tiers, legendary models | Proposed | 09 |
| Sprite memory and app size budget | Open | 07, 09, Q3 |

## 13. Technical and release

| Item | Status | Where |
|---|---|---|
| Save (versioned, backups, migrations) | Built | 07 |
| Account file (stash, settings, achievements) | Designed | 07 |
| iCloud sync | Designed | 07 |
| Device performance test on iPhone 12 | Not done | 07, 08 |
| Balance report and autopilot | Built for act 1 | 07 |
| Asset streaming for sprite sheets | Proposed | 07, Q3 |
| TestFlight, store assets | Designed | 07 |

## 14. Content counts

The table in `05-world-and-content.md` was written for three classes. With one class at launch (decision of 2026-09-21) some of its counts no longer fit. Proposed counts (Q23):

| Category | 05 today | Proposed for 1.0 | Built |
|---|---|---|---|
| Acts | 5 | 5 | 1 |
| Dungeon levels | 30 | 30 | 6 (act 1) |
| Hand-authored rooms | 150 | 150 (30 per act) plus 5 boss arenas | 6 placeholders, 1 arena |
| Towns | 5 (one per act) | 5 | 1 |
| Normal enemies | 40 | 40 | 3 |
| Elite modifiers | 15 | 15 | 3 |
| Bosses | 9 | 5 act bosses and 4 optional | 1 |
| Classes | 3 | 1 | 1 |
| Active skills | 24 | 8 | 4 |
| Skill modifiers | 216 | 24 choices at 3 levels, 72 options | 0 |
| Passive nodes | 60 per class | 60 | 0 |
| Legendary items | 60 | 40 (Q7) | 0 |
| Affixes | 90 | about 90 | 9 |
| Gems | 4 types, 5 tiers | 4 types, 5 tiers | 0 |
| Achievements | 60 | 60 | 0 |
| Story scenes | about 30 minutes | 25 short scenes, about 20 minutes | 0 |

## 15. What blocks what

The questions that hold up the most work, in order (full text in `08-production.md`):

1. Q1, how the character reaches level 60: sets enemy levels for acts 2 to 5, item levels, affix tier access, the Vigil tiers and the balance targets.
2. Q2, whether enemy level is fixed per depth (as built) or follows the player.
3. Q3, the sprite memory and app size budget: sets how many looks, grips, directions and frames the art can have.
4. Q4 to Q6, skill unlocks, keystones and attributes: needed before the skill system and the passive tree are built.
5. Q7 to Q10, legendaries, item levels above 60, elements and the off-hand: needed before the other seven slots and the legendaries are built.

The milestone plan that follows from this map is in `08-production.md`, "Milestones to 1.0 (proposed)".
