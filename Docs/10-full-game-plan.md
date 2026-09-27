# Full-game plan: scope map

Document status: written 2026-09-27 by Claude from the owner's request "Try and plan out the whole game and update the docs", as a proposal. The owner answered its 26 questions the same day (`08-production.md`, "Open questions from the full-game plan", and the decision log); those answers are decisions and are marked **Decided (Qn)** below and in the docs. Everything else new in this plan is still a proposal. It maps the whole game, 1.0 and the first updates, so the owner can see what exists, what is designed and what still needs a decision.

How to read it:

- **Status** comes from `CLAUDE.md` (what is in the code on 2026-09-27) and from the docs.
  - **Built**: in the game and checked.
  - **Partly built**: some of it is in the game; the note says what is missing.
  - **Designed**: decided or written in the docs before this plan, not built.
  - **Proposed**: new in this plan, not decided. The design is in the doc named in the Where column, under a heading marked "Proposed (not decided)".
  - **Decided (Qn)**: settled by the owner's answer to question n on 2026-09-27.
- **Q** numbers point to "Open questions from the full-game plan" in `08-production.md`, where each answer is written under its question.
- Decided rules are never restated differently here. When the plan and a decision disagree, the decision stands and the disagreement is a question.

## 1. Core play

| Item | Status | Where | Notes |
|---|---|---|---|
| Floating stick, movement only | Built | 01 | Stick size, dead zone and handedness settings not built (Settings screen) |
| Isometric camera, lead, size 10 | Built | 01 | Screen shake and its setting not built |
| Auto-target and basic attack | Built | 01 | Runs in Update; the docs ask for a fixed timestep |
| Auto-cast by slot order and trigger | Partly built | 01, 02 | Fixed trigger per skill kind; the picker is decided (Q15: default plus two alternatives), not built |
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
| Battle Roar, Rending Spin, Blood Frenzy, Skullsplitter | Designed (one line each), expanded as Proposed | 02 | Unlock at 9, 12, 15, 18: Decided (Q4) |
| Keystones Berserker and Juggernaut | Designed | 02 | Both bought in the tree for 3 points each, from level 20, the loadout picks one: Decided (Q4, Q5) |
| Skill levels 1 to 20 | Proposed | 02 | Scaling rule |
| Skill modifiers at 5, 10, 15 (24 for the class) | Proposed | 02 | |
| Skill tags | Proposed | 02 | Needed by gear |
| Passive tree (60 nodes) | Proposed | 02 | Replaces the stand-in Vitality when built |
| Attributes and attribute points | Designed | 02 | Automatic growth per level: Decided (Q6) |
| Loadout screen, presets | Designed | 06 | Opens at level 9 (Q4); presets bought with gold (Q16) |
| Gear on the character (4 layers, 4 grips) | Partly built | 03, 09 | Bake and layered renderer built; off-hand and two-handers are not items; budget: measure, then trim, resolution may drop to Diablo 2's (Q3) |
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
| Elemental damage types and resistances | Designed | 03, 05 | An element per act, resistances minus 15 per tier after Vigil I: Decided (Q9) |

## 4. Bosses

| Item | Status | Where | Notes |
|---|---|---|---|
| Cinder Warden (act 1) | Built | 05 | Art, sounds and reduce flashing not built |
| Tidewife, Saint Marrow, Warlord Kaeth, First Watchman | Designed (one line per phase), expanded as Proposed | 05 | |
| 4 optional bosses | Proposed | 05 | In the boss rotation from Vigil II: Decided (Q25) |
| Stagger meter, boss bar | Built | 05 | |

## 5. World and dungeon

| Item | Status | Where | Notes |
|---|---|---|---|
| Level generator, packs, chests, stairs | Built | 05 | |
| Boss arena on the last level | Built | 05 | |
| Low camera-side walls | Built | 09 | |
| Mini-map | Built | 05, 06 | Docs now follow the build (Q22) |
| Pack mixes per depth | Built for act 1 | 05 | Acts 2 to 5 Proposed |
| Enemy levels by depth | Built for act 1 (1, 2, 3, 4, 5, 7) | 04, 05 | Fixed per depth and tier (Q2); five campaign passes (Q1); acts 2 to 5 numbers Proposed |
| Room templates | 6 placeholders built | 05 | 150 needed (30 per act) |
| Shrine rooms | Designed | 05 | Numbers Proposed |
| Ambush rooms | Designed | 05 | Dormant packs placed at load: Decided (Q12) |
| Acts 2 to 5 (towns, dungeons, themes) | Designed (names), Proposed (content) | 05 | |
| Story beats, short scenes, lore | Designed (outline), Proposed (per act) | 05 | In-engine, text, no voice: Decided (Q20) |
| Sleep mechanic (session reset) | Designed | 05 | A bed at each town's inn: Decided (Q14) |

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
| Vendor (buy and sell) | None in 1.0 | 04 | Decided (Q26) |

## 7. Items and crafting

| Item | Status | Where | Notes |
|---|---|---|---|
| Weapon, Chest, Helm | Built | 03 | |
| Off-hand, gloves, boots, belt, amulet, 2 rings | Designed (slots), Proposed (bases) | 03 | Grip rules Decided (Q10) |
| Rarities, drop sources, bad-luck protection | Built | 03 | Cursed not built |
| Affixes | 9 built, about 90 Proposed | 03 | |
| Item level above 60 | Designed | 03 | Affix ranges grow 1 percent a level: Decided (Q8) |
| Legendaries with fixed affixes and powers | 40, Decided (Q7); each one Proposed | 03 | |
| Sets | None in 1.0 | 03 | Decided (Q18) |
| Sockets and gems | Designed | 03 | Numbers Proposed |
| Loot filter | Designed | 03 | Filtered items to a salvage pouch: Decided (Q11) |
| Power score | Built (partly) | 03 | Tags, Life on Hit and cooldown reduction not counted |
| Forge: Salvage, Reforge, Reroll, Temper | Built | 03 | |
| Forge: Socket, Imprint | Designed | 03 | |
| Transmog | After launch | 03 | Decided (Q24) |
| Stash (120 to 300 slots, shared) | Designed | 03 | |
| Appearance tiers per act | Designed for act 1, Proposed for acts 2 to 5 | 03, 09 | |

## 8. Progression and economy

| Item | Status | Where | Notes |
|---|---|---|---|
| XP curve and rewards | Built | 04 | |
| Level 1 to 60 | Designed | 04 | Five passes through the campaign (Vigil I to V): Decided (Q1) |
| Skill points, passive points | Designed | 04 | Not built |
| Paragon | Designed | 04 | Not built |
| Vigil difficulty tiers | Designed | 04 | Five campaign passes, fixed enemy levels: Decided (Q1, Q2); tier numbers Proposed |
| Gold and material economy | Partly built | 04 | Sinks Proposed |
| Drop tables per tier | Proposed | 04 | |

## 9. Endgame

| Item | Status | Where | Notes |
|---|---|---|---|
| Rifts | Designed | 05 | Numbers Proposed; a death ends the run, gear kept: Decided (Q13) |
| Abyss | Designed | 05 | Numbers Proposed; a death ends the run, gear kept: Decided (Q13) |
| Boss rotation | Designed | 05 | Proposed details |
| Achievements (local) | Designed, list Proposed | 05 | 60 local, no rewards: Decided (Q17) |
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
| Results | Proposed layout; on arriving in town, Decided (Q19) | 06 |
| Settings | Proposed layout | 06 |
| Loot filter editor | Proposed layout | 06 |
| Story scene player | Proposed layout; in-engine text scenes, Decided (Q20) | 06 |

## 11. Audio

| Item | Status | Where |
|---|---|---|
| Synthesized placeholder effects, 16 voices, priorities | Built | 07 |
| Placeholder music loop | Built | 07 |
| Layered music stems per act and situation | Proposed plan; AI-generated, Decided (Q21) | 07 |
| Full sound list per system | Proposed | 07 |
| Mixer, snapshots, volume settings | Not built | 07 |

## 12. Art

| Item | Status | Where |
|---|---|---|
| Brief for act 1 (terrain, player, enemies, boss, NPCs, props, effects, icons, UI, key art) | Designed | 09 |
| Wrathborn leather body, bearded axe, test bake | Built (partly) | 09 |
| Acts 2 to 5 asset lists | Proposed | 09 |
| Item icons for all slots and tiers, legendary models | Proposed | 09 |
| Sprite memory and app size budget | Measure, then trim; resolution may drop to Diablo 2's: Decided (Q3) | 07, 09 |

## 13. Technical and release

| Item | Status | Where |
|---|---|---|
| Save (versioned, backups, migrations) | Built | 07 |
| Account file (stash, settings, achievements) | Designed | 07 |
| iCloud sync | Designed | 07 |
| Device performance test on iPhone 12 | Not done | 07, 08 |
| Balance report and autopilot | Built for act 1 | 07 |
| Asset streaming for sprite sheets | Proposed (one of Q3's trimming levers) | 07 |
| TestFlight, store assets | Designed | 07 |

## 14. Content counts

The table in `05-world-and-content.md` was written for three classes. With one class at launch (decision of 2026-09-21) some of its counts no longer fit. The counts below replace it (Decided, Q23; 05 now shows them):

| Category | 05 before | 1.0 | Built |
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
| Item sets | none | none (Q18) | 0 |
| Affixes | 90 | about 90 | 9 |
| Gems | 4 types, 5 tiers | 4 types, 5 tiers | 0 |
| Achievements | 60 | 60 | 0 |
| Story scenes | about 30 minutes | 25 short scenes, about 20 minutes | 0 |

## 15. What blocks what

The 26 questions were answered on 2026-09-27. What the answers unblock, in the order the plan listed them:

1. Q1 and Q2: level 60 comes from five passes through the campaign (Vigil I to V) with enemy levels fixed per depth and tier, so the enemy levels for acts 2 to 5, item levels, affix tier access and the balance targets can now be set per act and tier.
2. Q3: the art budget is found by measuring act 1 on a phone and then trimming, and the owner allows the character resolution to drop to Diablo 2's, so the bake's target height is one of the levers.
3. Q4 to Q6: skill unlocks, keystones and attributes are settled, so the skill system and the passive tree can be built.
4. Q7 to Q10: 40 Legendaries, affixes above item level 60, elements per act and the grip rules are settled, so the other seven slots and the Legendaries can be built.

The milestone plan that follows from this map is in `08-production.md`, "Milestones to 1.0 (proposed)".
