# Production plan

Assumptions: a small team of one to three people, part time or full time, with the developer handling design and code. Art and audio may be commissioned. Durations below assume one full time developer and outsourced art. Halve or double the calendar as team size changes.

## Milestones

| Milestone | Duration | Goal | Exit criteria |
|---|---|---|---|
| M0 Prototype | 2 to 3 weeks | Prove input, feel and performance | Stick, 40 enemies, one auto-cast skill, drops with beams, holds 60 fps on iPhone 12 |
| M1 Vertical slice | 8 to 10 weeks | One class, the walkable town, one zone, boss, loot and Forge | 20 minutes of play a tester can finish without help, item comparison works, save works |
| M2 Alpha | 16 to 20 weeks | All systems, act 1 to 3 content, the launch class (Wrathborn) | Full loop through level 36, balance simulator running |
| M3 Beta | 12 to 16 weeks | Acts 4 and 5, endgame, polish, accessibility | Feature complete, TestFlight beta, performance targets met on all devices |
| M4 Release candidate | 4 to 6 weeks | Bug fixing, store assets | Zero known crashes, App Review submission |
| M5 Launch and support | Ongoing | Patch cadence, balance updates | Post-launch plan in this file |

Total: about 12 to 14 months for a solo developer with commissioned art. A reduced version 1.0 with three acts, three classes and half the legendaries could ship in about 9 months. These estimates assumed three classes and have not been rerun since the decision on 2026-09-21 to launch with one class.

## Scope tiers

| Tier | Contents | Use |
|---|---|---|
| Core | Stick, auto-combat, 1 class, act 1, loot, Forge, save | Must ship |
| Full | 1 class (Wrathborn), 5 acts, endgame, all legendaries | Target for 1.0 |
| Stretch | Extra classes as later content, hardcore mode, iPad, Android, more Abyss features | After 1.0 |

If the schedule slips, cut in this order: optional bosses, fourth and fifth Vigil tiers, Imprint, achievements beyond 30, story cutscenes, then an act.

## Milestones to 1.0 (proposed, not decided)

Written 2026-09-27 for the full-game plan (`10-full-game-plan.md`). The milestones above stay as they are; this is a proposal for how to get from where the project is today to 1.0 and the first updates. Sizes assume one developer writing the code with Claude and the owner producing the art with AI tools (09's pipeline: images, image-to-3D, Mixamo, the sprite bake), and are rough.

Where the project is on 2026-09-27 (`CLAUDE.md`): M0 is done except the device test on an iPhone 12. M1's slice is mostly built: the town, act 1's six levels and its boss, three enemies, three item slots, loot, the Forge, saving, onboarding, placeholder audio and the first real art (the Wrathborn's leather body and axe). Missing for M1's exit: real act 1 art, and a tester finishing 20 minutes without help.

| Milestone | Size | Content | Exit criteria | The owner produces | Code |
|---|---|---|---|---|---|
| M1 close | 3 to 5 weeks | Act 1 art (09, steps 1 to 11), the device test, the sprite budget measured and trimmed (Q3, answered 2026-09-27 with Q1 to Q6) | 60 fps on an iPhone 12 in the act 1 boss fight; memory measured with the real Wrathborn looks and act 1 enemies; 3 testers finish act 1 without help | Style frames, act 1 terrain, 4 bodies, 3 helms, 9 weapons and shields, 3 enemies, the Cinder Warden, 2 NPCs, props, effects, icons, UI, key art | Trimmed atlases, streaming groups (07), the off-hand slot and grips as items, fixes from testing |
| M2a Systems | 8 to 10 weeks | The full Wrathborn (8 skills, levels, modifiers, loadout, triggers per Q15, keystones, passive tree, attributes), the other 7 slots, the affix pool, gems and sockets, the stash and trainer, the Settings screen, the loot filter, results, CSV data tables | Act 1 playable with every system; the balance report models skills, tree and gear; save migrations tested | The 4 later skill animations per grip and look, skill and modifier icons, tree art, slot icons for the 7 new slots (act 1 band) | Everything listed; about 60 percent of the remaining code |
| M2b Acts 2 and 3 | 10 to 12 weeks | Towns, 16 enemies, the Tidewife and Saint Marrow, 60 rooms, the elite modifiers of acts 2 and 3, shrines and ambush rooms, the story scenes of acts 1 to 3, elements and resistances | Vigil I playable through act 3 in about 2 hours; balance report and autopilot green for acts 1 to 3 | Two acts of terrain, enemies, bosses, NPCs, portraits, props, looks for 6 bands | Caster, Charger and Support archetypes, the new behaviours, the scene player |
| M3 Beta | 14 to 18 weeks | Acts 4 and 5, Warlord Kaeth and the First Watchman, Vigil II to V, rifts, the Abyss, the boss rotation and 4 optional bosses, 40 legendaries with powers, Cursed items, Imprint (Transmog after launch, Q24), achievements and the Codex, real music and sounds, accessibility, iCloud sync | Feature complete; level 60 reachable in 15 to 20 hours by the autopilot; TestFlight beta of 100; performance targets on all target devices | Two acts of art, 20 legendary models, 40 legendary icons, the optional bosses, music and sounds (Q21) | Tier plumbing, endgame modes, legendary powers, audio mixer |
| M4 Release candidate | 4 to 6 weeks | Bugs, store page, screenshots, preview video, price (open question 1) | Zero known crashes; App Review passed | Store art | Fixes |
| 1.1 Quality of life | 4 weeks after launch | The post-launch plan above: more filter presets, loadouts, stash tabs; balance from player reports | | | |
| 1.2 Hardcore | 4 to 6 weeks | Hardcore mode (decided as post-launch) | | | |
| 2.0 A second class | about the size of M2a plus its art | The Ranger or the Hexer (02, what they would need); how it is sold is open question 4 | | A full character with looks per band and grip | Focus, projectiles, placed effects or summons |

Total to 1.0 from today: about 40 to 50 weeks for one developer, most of it limited by art production for acts 2 to 5. If it slips, the cut order above stands, with two additions before cutting an act: fewer looks for acts 2 to 5 (2 per act instead of the proposed 3; act 1 keeps its decided 3), and Vigil IV and V as rift-only tiers instead of full campaign passes.

## Team roles

| Role | Work | Source |
|---|---|---|
| Designer and programmer | Systems, data, tools, UI code | Developer |
| Artist (2D) | Characters, enemies, tiles, item icons, UI | Contract |
| VFX and animation | Effects, rigs | Contract or the same artist |
| Composer and sound | Music stems, SFX | Generated with AI tools, like the art (decided 2026-09-27, Q21) |
| QA | Device testing, balance runs | Community testers in beta |

## Risks

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| Auto-combat feels passive and boring | Medium | High | Stillness and Momentum tradeoff, elite modifiers, telegraphs that demand movement. Test in M0 with players who did not build it |
| Loot rarely feels exciting | Medium | High | Bad luck protection, guaranteed early legendary, loot beams and sound, simulation of drop rates |
| Balance across archetypes | High | Medium | Simulator from M1, target 20 percent clear speed band |
| Performance on older devices | Medium | High | Budgets set in M0, device tests every milestone, quality setting |
| Content volume too large | High | High | Scope tiers, reuse enemy rigs with palette swaps, procedural rooms |
| One-hand menus become cramped | Low | Medium | Bottom sheet pattern from the start, test on a 6.1 inch phone with one hand |
| Engine licensing or platform rule changes | Low | Medium | Keep simulation layer engine free, pin the Unity version (currently 6000.6.2f1, check before production whether a long-term-support release is preferable) |
| Premium price limits reach | Medium | Medium | Strong store page, free demo variant, decided after beta |
| Save corruption or loot loss | Low | High | Atomic writes, backups, kill tests |
| App Review rejection | Low | Medium | Follow guidelines, no third party tracking, clear age rating |

## Open questions

1. Price point: decide at launch. Compare against similar premium mobile action RPGs then.
2. Skill loadout depth: four slots with trigger conditions is the plan. Answered 2026-09-27 (Q15): each skill keeps a default trigger with two alternatives in a picker.
3. Demo: decide after beta whether to offer one. It helps a premium title but costs extra scope.
4. Selling later classes: decide after launch. A paid class add-on would be an in-app purchase on iOS, which conflicts with the premium, no in-app purchases decision.
5. The sleep mechanic: answered 2026-09-27 (Q14): a bed in each town's inn; new seeds for every dungeon level, enemies and chests back; waypoints, the Tome and the portal stay; corpses stay, moved to their level's arrival point.
6. One-time unlocks such as extra stash tabs or loadout presets were tied to the Ember Shard, which is removed. Answered 2026-09-27 (Q16): gold.
7. Achievements: the Game Center achievements are cut with the other online features. Answered 2026-09-27 (Q17): 60 local achievements in the Journal, no rewards.
8. The Wrathborn rework: decided 2026-09-26, see 02-classes-and-skills.md and the decision log.
9. Salvaging happens only at the Forge (decision of 2026-09-26), but the loot filter in 03-itemization.md auto-salvages filtered items as they are picked up, anywhere. Answered 2026-09-27 (Q11): filtered items go to a salvage pouch that takes no backpack slots, and the smith salvages them in one tap.
10. Vitality's life regeneration (02, Attributes: +0.1 percent a second per point) would heal 4.8 percent of max life a second at level 7 and 47 percent at level 60 with 8 Vitality a level (Q6). Is it meant per 10 points, as a flat amount, or left out? Not built until answered (2026-09-28). **Answered 2026-09-28: leave it out.** Vitality gives life only; healing stays with the auto-potion, Life on Hit and the tree.

## Open questions from the full-game plan

Written 2026-09-27 for the full-game plan (`10-full-game-plan.md`). All 26 were answered by the user the same day; each answer is under its question and in the decision log. Every question here comes from a proposal in the docs or a conflict found while planning. The questions above stay open as they are; five of them (2, 5, 6, 7 and 9) are repeated here with options, so all answers can be given in one pass. Ordered by how much other work waits on each answer. The first option in each list is the recommendation.

**Q1. How does the character reach level 60?** With 6 levels per act, about 100 enemies a level and the built XP formulas, one pass through the five acts ends near level 19 (level 31 even with enemies 7 levels above), while 04 and 05 plan level 60 at the act 5 boss in about 17 hours. Everything about acts 2 to 5, item levels and balance depends on this (04, "The gap between the XP curve and the campaign").
- A. Diablo 2's shape: the five Vigil tiers are five passes through the campaign with fixed enemy levels per depth; Vigil I's story ends near level 21 after about 3.5 hours, level 60 comes near the end of Vigil V at about 18 hours; the XP formulas stay. (recommended)
- B. Keep one campaign pass to 60 by multiplying XP per kill by act (the formulas change; 60 comes in about 4 hours instead of 17).
- C. Keep one pass by making dungeon levels 5 to 7 times bigger (against the 4 to 8 minute levels in 05).
- D. Keep 05's act ranges and expect farming (rifts, repeated levels after a sleep reset) between acts.

*Answered 2026-09-27 (A):* Five passes: the five Vigil tiers are five passes through the campaign with fixed enemy levels per depth; the XP formulas stay.

**Q2. Is an enemy's level fixed per depth and tier, or set from the player's level?** 04's tier table says enemies use the player's level plus an offset; the same file says the game never scales enemies to the player; the build fixes levels per depth (act 1: 1, 2, 3, 4, 5, 7).
- A. Fixed per depth and tier, as built; the recommended power score per level warns a player who is behind. (recommended)
- B. The player's level plus the tier's offset, as the tier table says.
- C. Fixed, but never more than 5 below the player (a floor so old areas stay worth something).

*Answered 2026-09-27 (A):* Enemy levels fixed per depth and tier, as built.

**Q3. What sprite budget can gear on the character have?** Decided: gear shows on the character, 4 layers, 4 grips, 16 directions for the player, 3 looks per shown slot in act 1 and more per act. Estimated untrimmed, one body look in one grip is about 172 MB of texture memory and act 1's shown looks alone are several gigabytes on disk, against 700 MB peak memory and 400 MB installed (07, "Sprite memory and app size").
- A. Measure act 1 on a phone first (as decided), then fit with the non-decision levers: trimmed atlases, rank variants by a colour shader, 16 directions only for movement animations, per-act streaming (on-demand resources), fewer frames for short actions. (recommended)
- B. As A, and also raise the installed-size target (for example to 1 GB with streaming for later acts).
- C. Fewer looks for acts 2 to 5 (2 per act) or fewer grips.
- D. Draw the player's 3D model at runtime instead of baked frames (would reverse the decision of 2026-09-27 on frame sprites).

*Answered 2026-09-27 (A, with a note):* "measure, then trim, also note that the game is quite high res, it can drop more in resolution. It can be the same as d2".

**Q4. When do the later skills unlock, and are skill slots gated?** Built: Hew 1, Hurl Axe 2, Bull Rush 4, Ground Breaker 6, all four slots in use. 04 says skills at 1, 2, 3, 6, 10, 14, 18, 22 and slots 3 and 4 at levels 8 and 20, keystone slot at 30.
- A. Keep the built four, then Battle Roar 9, Rending Spin 12, Blood Frenzy 15, Skullsplitter 18; all four slots open from level 1; keystones from level 20; the loadout screen opens at level 9. (recommended)
- B. Follow 04's list and move the built unlocks to match it.
- C. As A, but keystones at 30 as 04 says (after Vigil I's story on the recommended level plan).

*Answered 2026-09-27 (A):* Keep the built four, then Battle Roar 9, Rending Spin 12, Blood Frenzy 15, Skullsplitter 18; all four slots open from level 1; keystones from level 20; the loadout screen opens at level 9.

**Q5. How many keystones does the Wrathborn's tree have, and where are they bought?** 02's tree has 3 keystones per class, "choose one, cost 3 points"; the Wrathborn has 2 (decided); 00's glossary puts the keystone in the loadout.
- A. 2 keystones, bought in the tree for 3 points each at the ends of two branches; the loadout picks the active one. (recommended)
- B. 2 keystones, choosing one locks the other (only one can ever be bought until a respec).
- C. Design a third keystone for the Scar branch.

*Answered 2026-09-27 (A):* 2 keystones, both can be bought in the tree for 3 points each; the loadout picks the active one.

**Q6. Where do attribute points come from?** Only a stand-in exists (8 Vitality a level, decided 2026-09-26 until the tree exists). Nothing says how Might, Agility and Will are gained, and Will's Focus regeneration means nothing for Rage.
- A. Automatic class growth per level (8 Vitality, 2 Might, 2 Agility, 1 Will), Paragon points free; Will gives Rage gained for the Wrathborn. No extra screen. (recommended)
- B. 5 free points a level as in Diablo 2, with a suggested spread button.
- C. No attribute points; attributes only from the passive tree, gear and Paragon.

*Answered 2026-09-27 (A):* Automatic attribute growth per level (8 Vitality, 2 Might, 2 Agility, 1 Will), Paragon points free; Will gives Rage gained for the Wrathborn.

**Q7. How many Legendaries ship in 1.0?** 00 and 03 say 60 (20 per class plus 20 shared), written for three classes. Every Legendary in a shown slot needs its own model (decided).
- A. 40: 24 tied to Wrathborn skills, 16 shared (03's list; 20 unique models). (recommended)
- B. 60 as written, 40 of them for the Wrathborn (about 30 models).
- C. 30, the rest in updates.

*Answered 2026-09-27 (A):* 40 Legendaries in 1.0: 24 tied to Wrathborn skills, 16 shared.

**Q8. What does an item level above 60 give?** Items go to 160 in the Abyss, but affix tiers stop at T1 (60).
- A. Base damage and armor keep their curves; affix ranges grow 1 percent per level above 60, to plus 100 percent at 160. (recommended)
- B. A new tier T0 from item level 100.
- C. Only the base stat grows; affixes stop at T1.

*Answered 2026-09-27 (A):* Above item level 60 base stats keep their curves and affix ranges grow 1 percent per level, to plus 100 percent at 160.

**Q9. Is there elemental damage?** Resistance affixes and a 75 percent cap are designed, but no enemy deals an element.
- A. Each act has an element for its casters, projectiles and boss ground shapes (fire, cold, poison, physical and fire, shadow); tiers after Vigil I lower the character's resistances by 15 per tier, as Diablo 2 did. (recommended)
- B. Elements without the tier penalty.
- C. No elements; drop the resistance affixes.

*Answered 2026-09-27 (A):* An element per act, and resistances lowered 15 per tier after Vigil I.

**Q10. How do the four grips play?** Decided: the off-hand holds nothing, an off-hand weapon or a shield, or a two-handed weapon is used, each grip with its own animations. The rules are not decided, and the equip rule for two-handers is Claude's reading of "use two hand".
- A. Dual wield alternates hands with plus 15 percent attack speed; a shield blocks 12 to 20 percent of melee hits and projectiles; a two-hander deals 1.6 times the damage at 0.85 times the speed with plus 0.3 reach and empties the off-hand. (recommended)
- B. As A, but block reduces a hit by half instead of stopping it.
- C. Grips are looks only, with no rule differences.

*Answered 2026-09-27 (A):* Distinct grip rules: dual wield plus 15 percent attack speed alternating hands; shield blocks 12 to 20 percent of melee hits and projectiles; two-hander 1.6 times damage, 0.85 times speed, plus 0.3 reach, empties the off-hand.

**Q11. Where do filtered items go, given salvage only at the Forge?** (Question 9 above.)
- A. Into a salvage pouch that takes no backpack slots; the smith offers to salvage them in one tap. (recommended)
- B. They stay on the ground.
- C. They are salvaged where they drop (an exception to the Forge-only decision).

*Answered 2026-09-27 (A):* Filtered items go to a salvage pouch that takes no backpack slots; the smith salvages them in one tap.

**Q12. What is an ambush room?** 05 lists ambush rooms; 01 and the structure decision say nothing spawns around the player and there are no waves.
- A. Packs placed at level load but lying dormant and visible at the room's edges, all waking when the player crosses the middle. Nothing is created in play. (recommended)
- B. Drop ambush rooms.
- C. Enemies appear from the room's doorways (an exception to the decision).

*Answered 2026-09-27 (A):* Ambush rooms are dormant packs placed at level load, visible at the room's edges, waking when the player crosses the middle.

**Q13. What happens on death in a rift or the Abyss?** The death rule sends the character to town without its gear until it reaches its corpse; a rift or Abyss floor no longer exists after the run.
- A. The run ends and the character returns to town with its gear, no corpse. (recommended)
- B. The corpse is placed in town by the Watcher.
- C. The run ends and the gear is lost for good (a harder endgame).

*Answered 2026-09-27 (A):* A death in a rift or the Abyss ends the run; the character returns to town with its gear, no corpse.

**Q14. How does the sleep reset work?** (Question 5 above.)
- A. A bed in each town's inn; after one confirmation every dungeon level gets new seeds, enemies and chests come back; waypoints, the Tome and the portal stay; corpses stay, moved to their level's arrival point. (recommended)
- B. As A, but corpses are returned to the town.
- C. The session resets automatically when the game is started fresh (as Diablo 1's new game).

*Answered 2026-09-27 (A):* Sleep reset: a bed in each town's inn, new seeds for every dungeon level after one confirmation; waypoints, Tome and portal stay; corpses stay, moved to their level's arrival point.

**Q15. Can the player choose a skill's trigger?** (Question 2 above, deferred until the M0 test.) Built: one fixed trigger per skill kind.
- A. Each skill keeps its trigger by default, with two alternatives in a picker. (recommended)
- B. Any of 01's seven conditions for any skill.
- C. Fixed triggers only; the loadout is choice and order.

*Answered 2026-09-27 (A):* Each skill keeps its default trigger, with two alternatives in a picker.

**Q16. What gates the one-time unlocks that were tied to the Ember Shard?** (Question 6 above.)
- A. Gold: stash tabs 2,000 to 100,000, loadout presets 5,000 and 20,000 (04). (recommended)
- B. Progress: a tab per act cleared, a preset per Vigil tier.
- C. Everything open from the start.

*Answered 2026-09-27 (A):* One-time unlocks (stash tabs, loadout presets) are paid with gold.

**Q17. Are achievements kept as local ones?** (Question 7 above.)
- A. Yes, 60 local achievements in the Journal (05's list), no rewards. (recommended)
- B. Yes, with small cosmetic rewards (Transmog looks).
- C. No achievements in 1.0.

*Answered 2026-09-27 (A):* 60 local achievements in the Journal, no rewards.

**Q18. Are there item sets?**
- A. None in 1.0. (recommended)
- B. A few sets in the unshown slots only (no extra looks).
- C. Sets in every slot.

*Answered 2026-09-27 (A):* No item sets in 1.0.

**Q19. When does the Results screen appear, and what replaces "Salvage the rest"?** 01 and 06 place it after a level; a stair-descent game has no level end, and salvage is Forge-only.
- A. On arriving in town with new items; "Mark the rest for salvage" moves them to the salvage pouch. (recommended)
- B. Drop the Results screen; the Bag's upgrade arrows are enough.
- C. A results sheet on each stairway down.

*Answered 2026-09-27 (A):* The Results screen appears on arriving in town with new items; "Mark the rest for salvage" moves them to the salvage pouch.

**Q20. What are story scenes made of?**
- A. In-engine: sprites, portraits and text lines, no voice, skippable. (recommended)
- B. Painted stills with text, like a storybook.
- C. Voiced scenes (English only).

*Answered 2026-09-27 (A):* Story scenes in-engine: sprites, portraits and text, no voice, skippable.

**Q21. Where do the real music and sounds come from?**
- A. Commissioned stems and a licensed sound library, as 08's team roles say. (recommended)
- B. Generated with AI tools like the art.
- C. Keep the synthesized placeholders and improve them.

*Answered 2026-09-27 (B):* Real music and sounds generated with AI tools, like the art (not the recommendation).

**Q22. Docs that disagree with the build or with each other.** Each is small; the recommendation for all is to change the doc to match the build or the decision.
- 01, session structure: "the character appears where they left off" against the decision of 2026-09-23 (a loaded game restarts in town).
- 01, auto-loot "or when the room clears" against the built rule (nothing engaged with the character).
- 03, off-hand: "shields (Warden), quivers (Ranger) or focus orbs (Hexer)" against the Wrathborn's off-hand decision.
- 03 and 04: sockets on Rare and Legendary for Bloodstone (03) against Cinders for sockets on Magic and Rare (04).
- 03 and 04: Cursed items from Abyss depth 30 (03) against Vigil II (04's tier table).
- 04, Vigil I elite chance 10 percent against the generator's 15 percent elite rooms.
- 05, "Each enemy has ... 4 states and 2 palette swaps per act" against the brief's 5 animations and champion and elite sheet sets (09, 5.1).
- 05, the Focus shrine for a Rage class (proposed name: Wrath shrine).
- 05 and 06: rifts and the Abyss entered from an NPC (05) or from the Waystone map (06).
- 06, stance pips "above the character's head, blue and amber" against the built pips under the Rage arc (Momentum cyan, Stillness amber).
- 06, the mini-map "88 by 88 points, collapsed by default in combat" against the built small map, always shown.
- 06, onboarding: the first Legendary "from a scripted elite" against the built "next elite killed" after minute 20.
- 07, art pipeline ("layered PSD, imported with the Unity 2D Animation package") and the comparison row on skeletal animation, against the decision of 2026-09-27 (frame sprites pre-rendered from 3D).
- A. Update each doc to match the build or the decision. (recommended)
- B. Review them one by one.

*Answered 2026-09-27 (A):* Update each conflicting doc to match the build or the decision.

**Q23. Update the content counts for one class?** 05's table still counts 3 classes, 24 skills, 216 modifiers and 60 Legendaries.
- A. Replace them with the proposed counts in `10-full-game-plan.md`, section 14. (recommended)
- B. Keep them as the long-term target including later classes.

*Answered 2026-09-27 (A):* Replace Docs/05's content counts with the plan's one-class counts (10-full-game-plan.md, section 14).

**Q24. How does Transmog work?**
- A. Any look the character has picked up for that slot, for gold and 4 Ash; removing it is free. (recommended)
- B. Only looks of the same band or lower.
- C. Transmog after launch.

*Answered 2026-09-27 (C):* Transmog after launch (not the recommendation).

**Q25. When do optional bosses appear?** 05 says in the boss rotation from Vigil II.
- A. In the boss rotation only, from Vigil II, as 05 says. (recommended)
- B. As A, and each also placed once on its act's last level of that tier behind a sealed door that the act boss kill opens.
- C. As rare encounters in rifts.

*Answered 2026-09-27 (A):* Optional bosses in the boss rotation only, from Vigil II.

**Q26. Is there a vendor to buy and sell items?** None is in the docs, but 04's currency table lists "salvage sales" as a gold source.
- A. No vendor; salvage gives a little gold (04's proposal), so every source is a drop. (recommended)
- B. A vendor who buys items for gold.
- C. A vendor who also sells Common and Magic items and gems, as in Diablo 2.

*Answered 2026-09-27 (A):* No vendor; salvage gives a little gold.

## Decision log

| Date | Decision | By |
|---|---|---|
| 2026-09-20 | Portrait, thumb stick, movement is the only input | User |
| 2026-09-20 | Dark gothic fantasy setting | User |
| 2026-09-20 | Premium, no in-app purchases | User |
| 2026-09-20 | Engine: recommendation Unity, awaiting confirmation | Claude, superseded on 2026-09-21 |
| 2026-09-21 | Game name: Rageborn (replaces the working title Ashen Vigil) | User |
| 2026-09-21 | Engine: Unity 6000.6.2f1, URP 2D Renderer, iOS | User |
| 2026-09-21 | Camera and art: isometric, 2D sprites (not 3D) | User |
| 2026-09-21 | Bundle identifier com.filipbusic.rageborn, company Filbus Software | User |
| 2026-09-21 | Structure: Diablo 1 style, not a survivor game. A safe town, then a dungeon descended level by level | User |
| 2026-09-21 | Town: a walkable scene with no enemies, NPCs open their panels when walked up to (replaces the non-walkable hub panel) | User |
| 2026-09-21 | Dungeon: Diablo 1 style descent by stairs, about 6 levels per act (replaces picking zones from a world map) | User |
| 2026-09-21 | Classes: 1.0 launches with one class, the Wrathborn, a barbarian-style warrior, a rework of the Warden. More classes come later as new content. How they are sold is decided after launch | User |
| 2026-09-21 | Hardcore mode moves to a post-launch update | User |
| 2026-09-21 | Weekly Abyss seed cut, and no weekly challenges or similar recurring content | User |
| 2026-09-21 | Platform: iPhone only for 1.0, no iPad support | User |
| 2026-09-21 | Localization: English only for 1.0 | User |
| 2026-09-21 | Deferred: price (at launch), skill trigger depth (after the M0 test), demo (after beta) | User |
| 2026-09-21 | Returning to town: a permanent Portal Tome found around level 3, free to use from the UI. No consumable scrolls. Before the Tome the player walks back | User |
| 2026-09-21 | Death: the character is sent to town and loses its equipped gear (backpack, gold and Stash are kept) until it reaches its corpse. The corpse never expires, and a second death leaves the first corpse in place. Return by an open portal or by waypoints. Replaces the free checkpoint revive and the no item loss rule | User |
| 2026-09-21 | Persistence: Diablo 1 style within a game session (layout, dead enemies, chests). A sleep mechanic resets the session, design pending | User |
| 2026-09-21 | Menus: no separate pause menu. One inventory button opens stats, inventory, equipped gear, loadout, Settings and the loot filter, and pauses the game | User |
| 2026-09-21 | Fast travel: waypoints in levels and the Waystone in town. The world map screen is dropped | User |
| 2026-09-21 | No online features in 1.0: the Abyss leaderboard and Game Center are cut. iCloud save sync stays | User |
| 2026-09-21 | The Ember Shard is removed from the game | User |
| 2026-09-23 | Loading a saved game restarts in town | User |
| 2026-09-23 | XP level-difference steps count from the edge of the plus or minus 3 band (4 below gives 88 percent) | User |
| 2026-09-23 | Champions give more XP and gold than normal enemies. The multiplier, 3 times, is a tuning value picked by Claude | User |
| 2026-09-23 | A level up refills life | User |
| 2026-09-23 | Camera zoomed out: orthographic size 10 instead of 8, which felt too close | User |
| 2026-09-24 | Dungeon levels bigger and more open: rooms up to about 25 units across instead of 20, mostly open, with 5 cell doorways and corridors | User |
| 2026-09-26 | Forge, first version: Salvage, Reforge affix, Reroll values and Temper. Socket, Imprint and Transmog wait until gems, legendary powers and appearances exist | User |
| 2026-09-26 | Reforge turns the chosen affix into a new random affix of the same kind (prefix or suffix) at the same tier; it can land on the same stat | User |
| 2026-09-26 | Salvage and every Forge action happen only at the smith in town. The dungeon keeps Discard, which gives nothing back. Replaces Salvage on the item tooltip (03-itemization.md) | User |
| 2026-09-26 | The Wrathborn's resource is Rage (built by hitting and being hit, drains out of combat), not Focus | User |
| 2026-09-26 | The Wrathborn leans Momentum | User |
| 2026-09-26 | The Wrathborn gets an all-new barbarian skill set, none of the Warden's; Claude's draft of 8 skills and 2 keystones accepted as written (02-classes-and-skills.md) | User |
| 2026-09-26 | M1 slice: the first 4 skills unlock by level and equip themselves; the rest of the skill system comes after the slice | User |
| 2026-09-26 | Balance left as it is for now, after the first balance report | User |
| 2026-09-26 | Bull Rush builds Rage: it costs nothing and gains 15, so it can open a fight | User |
| 2026-09-26 | Slot order Ground Breaker, Hurl Axe, Bull Rush, Hew, so the slam is not starved of Rage by Hew | User |
| 2026-09-26 | Balance fix after the second balance report: act 1's enemy levels by depth flattened to follow the character (1, 2, 3, 4, 5, 7 instead of 1 to 12 spread evenly), so act 1 now ends near character level 7, not 12; and a stand-in Vitality of 8 points per level from level 2 (64 life a level) until the passive tree exists. Both values are Claude's, picked with the balance report | User |
| 2026-09-26 | The Portal Tome is given by an NPC, the Wanderer, on dungeon depth 3 | User |
| 2026-09-26 | Each level's waypoint stands in its start room | User |
| 2026-09-26 | The Cinder Warden's life raised from 15 to 30 times a normal enemy's, after the flatter enemy levels put the fight at 35 s; now about 70 s with typical gear (Docs/04 target 60 to 120 s) | User |
| 2026-09-27 | Art: generated with AI, "in the style of the original diablo 2 but not a copy". The brief is 09-art-brief.md | User |
| 2026-09-27 | Characters are frame-by-frame sprites pre-rendered from 3D models in 8 directions, not skeletal animation | User |
| 2026-09-27 | Act 1 art gets the full frame set (8 directions, 12 fps), measured on a phone before cutting | User |
| 2026-09-27 | Walls on a room's camera-facing sides are drawn cut down, as in Diablo 2, so they never hide the character | User |
| 2026-09-27 | "The game will be gear oriented just like diablo 2. It is very important that new gear equipped is displayed on the model." | User |
| 2026-09-27 | Shown slots, like Diablo 2: weapon, off-hand, helm and chest armour; the chest armour also sets the arms and legs. Gloves, boots, belt and jewellery do not show | User |
| 2026-09-27 | Looks per shown slot: tiers by item level (3 per slot in act 1, more with each act), plus a unique model for every legendary | User |
| 2026-09-27 | The Wrathborn's off-hand: "can in offhand hold: nothing, offhand weapon, shield, or use two hand" | User |
| 2026-09-27 | Each grip (one-handed with an empty off-hand, dual wield, weapon and shield, two-handed) has its own animation set, as in Diablo 2 | User |
| 2026-09-27 | Retreating from a fight shows the character facing the target and running backward, the one exception to "the body faces the movement direction" (Docs/01, facing) | User |
| 2026-09-27 | "bake 16 directions": the player character is baked in 16 directions, as Diablo 2 did for its heroes; enemies stay at 8 | User |
| 2026-09-27 | Placeholder background music until real stems exist: "dark and gothic, just some basic synth sounds on a simple loop" | User |
| 2026-09-27 | Full-game plan Q1: Five passes: the five Vigil tiers are five passes through the campaign with fixed enemy levels per depth; the XP formulas stay | User |
| 2026-09-27 | Full-game plan Q2: Enemy levels fixed per depth and tier, as built | User |
| 2026-09-27 | Full-game plan Q3: "measure, then trim, also note that the game is quite high res, it can drop more in resolution. It can be the same as d2" | User |
| 2026-09-27 | Full-game plan Q4: Keep the built four, then Battle Roar 9, Rending Spin 12, Blood Frenzy 15, Skullsplitter 18; all four slots open from level 1; keystones from level 20; the loadout screen opens at level 9 | User |
| 2026-09-27 | Full-game plan Q5: 2 keystones, both can be bought in the tree for 3 points each; the loadout picks the active one | User |
| 2026-09-27 | Full-game plan Q6: Automatic attribute growth per level (8 Vitality, 2 Might, 2 Agility, 1 Will), Paragon points free; Will gives Rage gained for the Wrathborn | User |
| 2026-09-27 | Full-game plan Q7: 40 Legendaries in 1.0: 24 tied to Wrathborn skills, 16 shared | User |
| 2026-09-27 | Full-game plan Q8: Above item level 60 base stats keep their curves and affix ranges grow 1 percent per level, to plus 100 percent at 160 | User |
| 2026-09-27 | Full-game plan Q9: An element per act, and resistances lowered 15 per tier after Vigil I | User |
| 2026-09-27 | Full-game plan Q10: Distinct grip rules: dual wield plus 15 percent attack speed alternating hands; shield blocks 12 to 20 percent of melee hits and projectiles; two-hander 1.6 times damage, 0.85 times speed, plus 0.3 reach, empties the off-hand | User |
| 2026-09-27 | Full-game plan Q11: Filtered items go to a salvage pouch that takes no backpack slots; the smith salvages them in one tap | User |
| 2026-09-27 | Full-game plan Q12: Ambush rooms are dormant packs placed at level load, visible at the room's edges, waking when the player crosses the middle | User |
| 2026-09-27 | Full-game plan Q13: A death in a rift or the Abyss ends the run; the character returns to town with its gear, no corpse | User |
| 2026-09-27 | Full-game plan Q14: Sleep reset: a bed in each town's inn, new seeds for every dungeon level after one confirmation; waypoints, Tome and portal stay; corpses stay, moved to their level's arrival point | User |
| 2026-09-27 | Full-game plan Q15: Each skill keeps its default trigger, with two alternatives in a picker | User |
| 2026-09-27 | Full-game plan Q16: One-time unlocks (stash tabs, loadout presets) are paid with gold | User |
| 2026-09-27 | Full-game plan Q17: 60 local achievements in the Journal, no rewards | User |
| 2026-09-27 | Full-game plan Q18: No item sets in 1.0 | User |
| 2026-09-27 | Full-game plan Q19: The Results screen appears on arriving in town with new items; "Mark the rest for salvage" moves them to the salvage pouch | User |
| 2026-09-27 | Full-game plan Q20: Story scenes in-engine: sprites, portraits and text, no voice, skippable | User |
| 2026-09-27 | Full-game plan Q21: Real music and sounds generated with AI tools, like the art (not the recommendation) | User |
| 2026-09-27 | Full-game plan Q22: Update each conflicting doc to match the build or the decision | User |
| 2026-09-27 | Full-game plan Q23: Replace Docs/05's content counts with the plan's one-class counts (10-full-game-plan.md, section 14) | User |
| 2026-09-27 | Full-game plan Q24: Transmog after launch (not the recommendation) | User |
| 2026-09-27 | Full-game plan Q25: Optional bosses in the boss rotation only, from Vigil II | User |
| 2026-09-27 | Full-game plan Q26: No vendor; salvage gives a little gold | User |
| 2026-09-28 | "Do it, diablo 2 style on all and move the camera 25% closer. The player really small and far away now": the whole world renders at about Diablo 2's resolution with hard pixels (HUD sharp), and the camera is 25 percent closer (orthographic size 7.5 instead of 10). Earlier the same day: the Wrathborn's sprites at half resolution ("the game is quite high res, it can drop more in resolution. It can be the same as d2", said while answering Q3) | User |
| 2026-09-28 | Art brief at the new resolution: all in-world art made at 64 pixels a unit (every size in 09 sections 3 to 9 and 15 halved), style frames made at 390 x 844 and enlarged 3x; UI, icons and key art unchanged. The render aims at 870 px on the long side (a 3x iPhone's point resolution) | Claude, approved by the owner 2026-09-28 |
| 2026-09-28 | Enemy art pipeline: the bake heights (husk 70, ghoul 88, bandit archer 80 px at 64 a unit, asked 9 percent over for the A-pose box), the attack's strike at 55 percent of its animation, a 0.25 s hit reaction outside attacks, the placeholder swell tells switched off for baked enemies, and the suggested Mixamo animations in 09 section 5.4 | Claude, approved by the owner 2026-09-28 |
| 2026-09-28 | Each skill's two alternative triggers (Q15): Hew Always and Elite present; Hurl Axe Elite present and Standing; Bull Rush Elite present and 3+ enemies; Ground Breaker 3+ enemies and Elite present; Battle Roar Always and Life below 50%; Rending Spin 3+ enemies (without moving) and Standing; Blood Frenzy Always and Elite present; Skullsplitter Always and Elite present. An alternative replaces the skill's own condition; a valid target or area is still needed | Claude, approved by the owner 2026-09-28 |
| 2026-09-28 | Loadout rules: until the player first changes it, the loadout fills itself with the unlocked skills in the class order (so characters below level 9 play as before); after that a newly unlocked skill only fills an empty slot. A slot can be left empty. Cooldowns belong to the skill, not the slot | Claude, approved by the owner 2026-09-28 |
| 2026-09-28 | The Skills page shows before level 9 whenever skill points are unspent, but only raising skill levels works there until the loadout opens at 9 (Q4); otherwise points earned from level 2 could not be spent until 9 | Claude, approved by the owner 2026-09-28 |
| 2026-09-28 | Built with Docs/02's proposed numbers as tuning: the four later skills' filled-in details, skill levels (7 percent of level 1 damage a level, buffs 1 percent), the passive tree's nodes. Rending Spin's bleed replaces rather than stacks and is dealt in 0.5 s pulses; buffs show their name and a ground ring until they have art | Claude, approved by the owner 2026-09-28 |
| 2026-09-28 | Passive tree layout: each half branch is a chain in the order minor, minor, notable, minor, minor, notable, minor, minor, notable; the screen is a tab per branch with a scrolling list rather than drawn columns. Scar Tissue (less damage from elites and bosses) applies to attacks that ignore armor, which only elites and the boss make | Claude, approved by the owner 2026-09-28 |
| 2026-09-28 | Settings, first version: only the settings with a decided source and a built system behind them (stick size, dead zone, handedness, auto-potion threshold, music and effects volume, reduce flashing, reduce motion), with Docs/06's proposed ranges and defaults; steps of 8 points, 2, 5 and 10 percent; − and + buttons rather than sliders (one thumb, 56 point targets); a Settings button in the Bag's top-left corner rather than the proposed tab row; kept in the account file `account.json` beside the character save. Reduce flashing makes the boss phase flash a quarter as bright over 1.2 s; reduce motion turns off the camera lead and hit stop (there is no screen shake). A music setting of 80 plays at the level the music had before | Claude, approved by the owner 2026-09-28 |
| 2026-09-29 | Off-hand and grips built with 03's numbers. Choices: only the great axe exists as a two-hander (the bearded axe enlarged 1.5 times until a model exists); one shield model (the owner's Viking_shield.fbx) stands in for all three shield looks; each grip bakes only the clips it has its own of (the owner's downloads: the shield's run, the two-hander's run and swing, dual wield's combo) and borrows the one-handed body for the rest, with the shield or second axe baked over those; a one-handed axe goes where it scores higher (replacing the main axe or joining it for dual wield) | Claude, to review |
| 2026-09-28 | HUD: the player's portrait top left, the life and experience bars to its right, the Bag button under the portrait, the mini-map top right with no background; the fps counter faint at the very top. The Bag restyled after the owner's reference image (paper doll, stats, item grid, tabs) | User |
| 2026-09-28 | Six more slots built from 03's proposed table (gloves, boots, belt, amulet, two rings); the off-hand waits for its grips and their animations. Only built affixes roll on them, on the slots 03 lists, plus movement speed (boots) and dodge (boots, belt, ring), both designed; drops pick the slot evenly from eight kinds, so a weapon is 1 drop in 8; a ring goes on an empty hand, else replaces the ring it beats by more | Claude, to review |
| 2026-09-28 | Vitality gives life only, no life regeneration (open question 10) | User |
| 2026-09-28 | Attributes built as decided (Q6) with Docs/02's per-point effects; Might counts for every hit but the thrown axe ("melee and area"); dodge capped at 50 percent. Vitality's life regeneration is not built: as written (0.1 percent of life a second per point, 8 points a level) it would heal 4.8 percent of max life a second at level 7 and 47 percent at 60, which cannot be meant; question 10 below | Claude, approved by the owner 2026-09-28 |
| 2026-09-30 | The bandit archer is dead and demonic, not a living bandit: "make the archer dead/demonic". Name, role and numbers unchanged; the look is in 09 5.2 and its prompt in 5.4 | User |
| 2026-09-30 | Levels are connected halls, not rooms joined by corridors: "make the levels more open spaces. Just like the diablo 1 dungeons. Add some placeholder assets also to make it more interesting. Like different floor textures, brushes, and whatever fits inside a dungeon"; of three proposals (connected halls, open caverns, a few big halls) the owner chose connected halls. The numbers (room spans, arch widths, hall and loop chances, prop and decal densities) are Claude's, tuning. 05's level generation says how | User; numbers Claude's |
| 2026-09-30 | Pixel art: "I am not liking the art style. I want it pixelated"; asked whether that meant bigger pixels, a pixel-art look or both: "Both". The world renders at about 600 pixels on the long side (from Diablo 2's 870) at 40 pixels a unit, and every world sprite is drawn at 40 a unit in one dark-fantasy palette with hard alpha, characters and props with a dark one-pixel outline. The palette and the numbers are Claude's | User; palette and numbers Claude's |
| 2026-09-30 | Bows only: "I wish it to become an action rpg that only uses bows". Asked one question at a time: a new hero and class, not the Wrathborn rethemed; its name **Wild Arrow** (hero and class); Focus as the resource; short bows and longbows with a quiver in the off-hand; a clean start for saves (old saves set aside, not deleted); until it has its own model, the Wrathborn's body baked with the bandit archer's Mixamo moveset. The Wrathborn is retired (kept in 02 as reference) | User |
| 2026-09-30 | The Wild Arrow, gone through part by part with the owner: how a fight plays (arrows, reach 7.5 and 9, line of sight, kiting) and Focus (full on each level, 6 a second, 4 per hit) kept as proposed; four skills for now, the owner's list: Split Arrow, Pierce Arrow, Homing Arrow (no shots around corners) and Explosive Arrow, the rest held; no passive tree and no keystones for now: "No skill trees, add stat points to spend on attributes", the attributes Strength, Agility, Vitality, Speed and Focus; pets: "Add a pet vendor in town. Pet is bought and levels with the player", a few kinds with one active, a pet that fights, draws aggro, picks up loot, gives a passive bonus and has customizable rules; gear (short bow, longbow, quiver) kept as proposed; the build order approved | User |
| 2026-09-30 | The Wild Arrow's numbers and details (02, 03): skill numbers, unlock levels 1, 2, 4, 6 and cast order; each attribute's effects (the owner approved the split), 5 points a level from 2, a start of 10, no respec; the pet kinds (wolf, raven, boar), prices, rules chips and knock-out; bow and quiver numbers and act 1 looks | Claude, to review |

## Post-launch plan (draft)

- Weeks 1 to 4: bug fixes, crash review, balance patch based on player reports and simulation.
- Month 2 to 3: quality of life update (loot filter presets, more loadouts, extra stash tabs).
- Month 4 onward: new classes and hardcore mode as post-launch content if sales justify it. How later classes are sold is an open question, see below.

## Immediate next steps

1. Run the M0 prototype: stick, pooled enemies placed as packs in a walled test room (40 in view), one auto-cast skill, item drop and pickup, performance test on a real device.
2. Build the balance simulator skeleton from the formulas in 03-itemization.md and 04-progression-and-economy.md.
3. Write the item and affix data sheets (CSV) for the first 30 affixes and 10 legendaries.
4. Produce a paper prototype of the loadout and results screens and test one-hand reach.
