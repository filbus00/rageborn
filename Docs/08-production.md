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

*Answered 2026-09-27 (A):* No vendor; salvage gives a little gold. *Changed 2026-10-02 (B):* the owner asked for a merchant who buys gear (decision log).

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
| 2026-09-30 | Bows only, as built (Claude's choices while building, to review): the item kinds keep their names in code and saves (weapon = short bow, two-handed = longbow, shield = quiver); the passive tree is hidden rather than deleted until the code reading it is cleared; Vitality points add on top of the class's life by level; the quiver's extra arrow is built, its pierce affix is not; a pet draws only melee blows (not the ghoul's slam or arrows) and fetches loot to the character's feet for auto-loot; the Pet Vendor stands at town cell (5, 1); pet rules are one targeting choice plus four on/off behaviours rather than an ordered list; the skills' alternative triggers and numbers are Docs/02's proposals; the arrow effects (trails, sparks, stuck arrows, blood sprays, the burst, the pierce arrow's blood trail) are Claude's look | Claude, to review |
| 2026-10-01 | Starting kit: a Common short bow and a Common quiver, both item level 1 (Claude's suggestion, "Do it") | User |
| 2026-10-01 | The Wild Arrow's loot depth (03, new section): five build layers, arrow traits as a shared language for powers, burn and chill beside bleed, 12 new or changed affixes, 24 legendaries replacing the Wrathborn's, six build archetypes, legendary homes and the Codex for targeted farming, Imprint from act 3 | Claude, proposed for the owner's review |
| 2026-10-01 | Loot depth, asked one by one: keep the six build archetypes (Splinterstorm, Blood Hunter, Pyre, Seeker, Sniper, Windrunner) plus Beastmaster; add burn and chill beside bleed; legendaries drop anywhere and twice as often at their home; Imprint endgame only | User |
| 2026-10-01 | First legendaries built (11 of 24, one or more per archetype) with arrow traits, burn and chill; fixed affixes from the built pool, item level 3 minimum, homes as drop sources, Stillwater Yew once per Stillness fill | Claude, to review |
| 2026-10-01 | The other 13 legendaries built (all 24) and the Codex screen. Choices: homes (Widow's Draw, Falconer's Hood, Bandolier: elites; The Long Silence, Crown, Cinder-Stitched Jerkin: the boss; Hollow Hound, Bloodletter's Grips: champions; Hide of the Running Stag, Stalker's Treads: chests; Fletcher's Fingers, Windrunner Treads, Eye of the Storm: any enemy); The Widow's Draw passes on the bleed's own strength for 3 s and a crit opens one at 30 percent a second; Cinder-Stitched Jerkin's ground also follows Ashfall Quiver's bursts; Windrunner Treads' free skill needs another 3 s on the move once spent; Stalker's Treads checks Stillness when the arrow is loosed; the Codex hides a legendary's name, power and home until found but shows its kind and archetype, found ones first | Claude, to review |
| 2026-10-01 | Affixes 90 to 100, smart drops and a DEV give button in the Codex built ("Do all 4"). Choices: the fixed affixes (plus 1 pierce, plus 1 skill arrow) keep their value at every tier and Temper does not raise them; fork chance is rolled when the arrow is loosed; far and point blank damage count as increased damage, measured from where the character stands when the arrow lands; the power score leaves the new affixes out (they depend on the fight); where Docs/03 gives a legendary a fixed affix its own slot list forbids, the legendary keeps a stand-in | Claude, to review |
| 2026-10-02 | The smith and all special crafting are on hold: "Lets focus on the core. Just killing mobs, looting gear for now." The Forge is switched off in the game (`Features.Forge`: no smith in town, no materials from elites or the boss, no Forge hint, no materials row in the Bag), its code and saved materials kept. A Fletcher (arrows built from heads, shafts and fletchings) and elite essences were proposed first and are not built | User |
| 2026-10-02 | Gear takes realistic space: the backpack is a 10 x 6 grid, auto-packed (no dragging), replacing "40 slots, no grid tetris" (03). Sizes (Claude's, from Diablo 2): bow and longbow 2 x 4, quiver and chest 2 x 3, helm, gloves, boots 2 x 2, belt 2 x 1, amulet and ring 1 x 1. Claude's choices: first fit row by row from the top left; a full repack, tallest first, only when a new item fits nowhere; an item with no room stays on the ground with a NO ROOM callout at most every 4 s; positions are not saved | User (grid, packing), Claude (details) |
| 2026-10-02 | Pick-up rules (a simple loot filter, before the full one of 03): a Settings row picks what auto-loot takes, Everything, Magic and better (default), Rare and better or Upgrades only; Legendaries and upgrades by the power score are always taken; an item lies on the ground 1.5 s before it is picked up ("a slight delay, maybe a second or two"); every drop shows its name in its rarity colour, and the name pops over the character on pickup. Claude's choices: the default, the 1.5 s, the dimmed name of a drop the rule leaves, the pet fetching only wanted drops | User (rules, delay, names), Claude (details) |
| 2026-10-02 | A merchant in town buys gear for gold ("A use for gold: a merchant to sell gear"), which reverses Q26 (no vendor). It buys only. Claude's choices, to review: it stands at town cell (-4, 1), across the start from the Pet Vendor; walking up opens the Bag at the merchant, where an item's sheet offers Sell for its price in place of Discard, and a row sells all Commons, all Magic or all Rares at once after a second tap, keeping upgrades and named legendaries; prices are 2, 6, 15 and 40 gold by rarity, plus 15 percent of that for each item level, a named legendary half as much again (a Magic item at level 5 sells for about two to three kills' gold) | User (merchant), Claude, to review (details, prices) |
| 2026-10-02 | Item icons: until the UI art (09), each kind of item is drawn in code as a small pixel picture the size of its backpack block (12 art pixels a cell, the palette and outline), with a gem or band in the rarity's colour from Magic up and an ember outline on a Legendary; the item level sits in the tile's corner. Loot on the ground: an item hops out of the kill to about 0.6 units away (never through a wall), its beam rises as it lands, a Rare or Legendary marker flares; names that would overlap are lifted line by line, wanted and better items keeping their spot | Claude, to review |
| 2026-10-03 | The Wild Arrow walks while she fires: "make it so that when firing at enemies, wild arrow walks", and she slows to a walk rather than only looking like it (asked: slow to a walk, walk look at full speed, or slow a little). Claude's choices, to review: half speed (`FiringPace.MoveMultiplier` 0.5) for up to 0.6 s after each shot or skill (a slower bow breaks into a run between shots), dashes excepted; the moving-shot sheets will use the longbow pack's walk legs once her own model is baked | User (walk, slow), Claude, to review (numbers) |
| 2026-10-03 | Disengaging from a fight: "as long as the thumbtracker is inside the thumb circle, the character stays in the fight and fires her bow while walking backwards. But when the tracker leaves the thumb circle she turns and starts running away". Claude's choices, to review: out at 1.2 stick radii, back in at 1.05; no basic arrows and no skills while outside (the pet keeps fighting); a draw in progress is cut at once; the knob is drawn following the thumb out of the ring | User (mechanic), Claude, to review (numbers) |
| 2026-10-03 | The basic arrow's target is the nearest enemy in sight within reach, whichever way she moves, replacing the forward-cone preference (the owner: "while engaging an enemy and having it the enemy being really close, the character just fires off into the distance"). Claude's choice, to review: she keeps her target until another is 0.75 units closer | User (bug), Claude, to review (margin) |
| 2026-10-03 | Hit stop only on champion, elite and boss kills, at most once a second (the owner: "stuttering when enemies are hit ... the fps stays consistent ... but the stuttering feels very heavy"; every kill had frozen the game 50 ms at 2 percent speed, which with a bow killing packs arrow by arrow read as stutter). Claude's choice, to review | User (bug), Claude, to review (which kills, the gap) |
| 2026-10-04 | Props modelled in Blender from code, no plugins ("just fire up blender and make the models ... the town screen needs house assets, tents, camp fires etc. Also fix the dungeon assets, torches, barrels, buckets, blood, rituals, candles"). Claude's choices, to review: every dungeon prop, wall, decal, the stairs, chests and waypoints remade as simple 3D models rendered from the game's camera and brought to the palette; two new dungeon props (a bucket, a standing torch, lit) and a ritual sigil decal; one combat or elite room in four gets a summoning circle painted on its floor, lit dim red (the dungeon generator's version goes to 5, so a seed builds a different level than before); the town gets three houses, two tents, a campfire, a well, the merchant's stall and stores, a pen beside the Pet Vendor, lamp posts, banners by the stairs, graves and trees, all placed in `TownLayout` and blocking her like walls | User (assets), Claude, to review (looks, placement) |
| 2026-10-04 | Floors, wall details, the bows and quivers, and the boss, all modelled in Blender ("Just do all 4"). Claude's choices, to review: six variants each of flagstone, brick, earth and mossy stone floors; about one in nine walls facing a room carries a torch (lit, at most 14 a level), chains with a skull, or a banner on its face, or candles, skulls or a cobweb on its top; the five bows and two quivers the game already named (recurve, horn, yew longbow, war bow, great bow; studded and bone quivers) each modelled and baked onto her; the Cinder Warden as charred armour with ember seams, a horned helm and spiked gauntlets on the ghoul's rig and animations; the husk's champion frost-blue and its elite ash-black and ember-red | User (all four), Claude, to review (looks) |
| 2026-10-04 | The held skill slots: all five candidates (Knockback Shot, Barrage, Kill Shot, Hunter's Breath, Wild Frenzy), nine skills in all, and every skill unlocks at level 1 ("All at once"), so the loadout opens at level 1 (replacing Q4's level 9). Claude's choices, to review: the numbers in Docs/02, and that an unchosen loadout keeps the first four | User (skills, unlocks), Claude, to review (numbers) |
| 2026-10-04 | New act 1 enemies ("do all 5", the option "new enemy types"). Claude's choices, to review: a skeleton (a slower, armoured husk with a rusted blade) and a cultist (a robed archer with a slower, heavier fire bolt), from depth 4, turning 35 percent of packs' husks into skeletons and half of the archers into cultists; ranks and the boss baked at their drawn size so their pixels stay whole | User (enemies), Claude, to review (types, shares, tuning) |
| 2026-10-04 | The owner's five asks: menus revamped, the bow's sound remade, real ground textures instead of tiles, the town spread out, worn gear modelled. Claude's choices, to review: continuous grounds repeating every 4 x 4 cells; the town about 1.6 times wider with three more houses; the gear looks as named by tier (padded, leather, mail; cap, nasal, great), with no chest worn showing her own outfit ("bare") | User (asks), Claude, to review (looks) |
| 2026-10-04 | The ground ("dirt with some grassy spots and some paths" in town, "mostly dirt and some cobble stones showing through" below, no per-room floors, after Diablo 2's act 1). Claude's choices, to review: the grey-olive earth palette, how much cobble and grass shows, and where the town's paths run (the square to the stairs, the merchant, the Pet Vendor, the Waystone, the portal, the campfire, the well, two roads out) | User (look), Claude, to review (amounts, paths) |
| 2026-10-04 | Walls, music ("do 1 through 4"). Claude's choices, to review: rough fieldstone walls with dark tops in four variants (plain, timber, broken, mossy); three tracks (town guitar, dungeon ambience, boss drums), made in code, mixed by numbers and not heard by Claude | User (asks), Claude, to review (look, music) |
| 2026-10-04 | Balance pass ("do 1 through 4"). Claude's choices, to review: enemy life as level to the 1.6 (Docs/03 had 1.9), the Cinder Warden at 60 times a normal enemy's life (was 30), and the skill numbers in Docs/02 (Split Arrow cheaper, Barrage, Kill Shot and Wild Frenzy stronger, Explosive Arrow a little weaker) | User (ask), Claude, to review (numbers) |
| 2026-10-05 | The rest of act 1's roster ("do 1 and 2"): Ash Wolf, Bandit Cutthroat, Ember Acolyte, Pyre Keeper, Carrion Bloat with Docs/05's numbers. Claude's choices, to review: how often they appear (wolf packs one in five from depth 3, cutthroats in 30 percent of packs, acolytes in 35, keepers in 30 of packs of 4 or more, bloats in 40 from depth 5); the keeper's distance; the bloat dying with its burst; their looks. Also: an idle enemy wakes only with sight or a short walkable route (1.75 times its aggro range) | User (ask), Claude, to review (shares, looks) |
| 2026-10-05 | **No acts: one town above a 24-level dungeon.** The owner: "Skip the acts, lets just progress deeper into the dungeon and then have things happen in town depending on how deep you go". Answers: about 24 levels, a boss every 6 ("Fixed, about 24 levels"); the dungeon changes gradually, no hard sections ("Gradual, no hard sections"); in town, new NPCs arrive ("New NPCs arrive"): stash keeper at depth 2, healer at 4, gambler at 8, trainer at 12 (all four, at the proposed depths). Claude's choices, to review: the enemy level curve past depth 6, how the look darkens, the services' prices and details | User (structure, newcomers), Claude, to review (numbers) |
| 2026-10-05 | Bosses at 12, 18, 24 and the deep levels' enemies ("do it"). Claude's choices, to review: the Tidewife at 12, Saint Marrow at 18, the First Watchman at 24, from Docs/05 (the flood as a large circle, Marrow's ring without gaps); eight enemies from Docs/05's later acts (Drowned, Harpooner, Drowned Watchman, Skeleton Knight, Grave Priest, Hollowed, Void Wraith, Rift Caller), their first depths and how fast they spread; the Void Wraith without its blink; their looks | User (ask), Claude, to review (bosses, roster, numbers) |
| 2026-10-05 | Gear remodelled (the owner: helms, chest armour, belts and gloves "look bad"; "full face cover helms that look cool, armor that looks cool, legendaries what have unique looks. Change the art direction somewhat if it is needed"). Claude's choices, to review: the art direction (pieces built over her that change her outline, darker iron and leather, deep red and green cloth, gold and ember accents); five looks per worn slot and their names and bands; a unique look for each of the 22 legendaries that show (rings and the amulet do not) | User (ask), Claude, to review (looks) |
| 2026-10-04 | The camera zooms out one whole step (pixels 2 x 2 on the iPhone 11 instead of 3 x 3, the view half again as wide) and she targets only enemies on screen ("Zoom one step + on-screen only", over zooming to her full reach, which would have made her about 6 percent of the screen with uneven pixels) | User |
| 2026-10-06 | **The Vigil's road replaces the descent.** The owner: the game is "hard balance and make challenging in a fun way. There is no sense of progress or overcoming hard stuff"; the dungeon becomes rifts streaming demons "in a never ending stream until you close the rift"; "closed rifts cannot be reopened ... all rifts have an internal rising dificulty"; "no timers"; "a long corridor leading forwards (up on the phone)", about 24 rifts, each closing opening the next further up. Answers: harder by closeness to the rift; loot better by closeness; closed by killing a guardian (every sixth a boss); back at the last closed rift; a wide hall with side rooms; death sends her to the last beacon and costs some gold (replacing the corpse run); "when a stream is closed, the waystone lights up and the demons from the stream above fear the light and do not pass further"; about 5 minutes a push; an endless rift after the last; the story "The Watch's beacons"; packs in surges. Reverses "packs placed in rooms, idle until aggro" and "nothing spawns around the player". Claude's choices, to review: the numbers and rules under "Proposed" in 05, "The Vigil's road" (hall shape and length, the closeness curve, stream rate and cap, packs joining a screen ahead, the guardian's distance, the light's reach, 10 percent gold) | User (design), Claude, to review (numbers) |
| 2026-10-06 | The road's stretches ten times longer: "the levels are way too short. Make the coridor at least 10 times longer" (about 690 units of hall, was about 70). Claude's choices, to review: 90 legs; the endless stretch as long (its levels keep rising up it); side rooms at one turn in eight (about ten a stretch); wall torches one a leg | User (length), Claude, to review (details) |
| 2026-10-07 | **The Vigil's road is reverted**: "lets restore the latest changes, go back to regular dungeon structure, no streaming enemies". The dungeon is again 24 levels by stairs with placed packs, death the corpse run. Kept from those days: the Bag's crisp figure, the minimap, flow field and spatial hash touching only what changed, the ground painted on worker threads, the cap on drops lying on the ground, the device benchmark. Found on the way: the drop to 30 fps after 2 to 3 minutes was iOS's thermal state through the player setting "Adjust iOS FPS based on thermal state" (on, 30 fps when serious, 15 when critical); left as it is until the owner decides | User |
| 2026-10-07 | **Talent trees replace the skill loadout.** The owner: "rebuild the skill system. Character starts with just regular bow attack"; answers: skill tree with points per level, three trees, Hunter-style (Marksmanship, Beast Mastery, Survival), mostly passives with some skills, 1 point a level from level 2, every learned skill cast automatically. Claude's choices, to review: the nodes and numbers (Docs/02 "Talent trees"), 5 points a row, Improved nodes +15% skill damage a rank, the Trainer resets talents, save format 15 (older saves start with no talents and their points to spend) | User (system), Claude, to review (contents) |
| 2026-10-08 | **Dungeon rebuilt: bigger, rugged, lived in.** The owner: no feel of travelling underground, walls too straight, "double their size", "more varied", monsters "truly living in them"; built halls all the way, "more rugged and broken down and more demonic the further down". Claude's choices, to review (Docs/05 Level generation): 10 to 15 rooms, rock 3 to 7 cells between rooms with rough doglegged passages, rooms with wings and noise-slumped walls, collapses, tall wall faces in three depth themes (crypt, ruin, demonic), a lair for every pack by who lives in it, demonic clutter deeper. Generator version 6 (old saves forget their dungeon kills and chests) | User (direction), Claude, to review (details) |
| 2026-10-08 | **Quests.** The owner: story, quests, quest givers and dungeon quests, "nothing lame like kill x amount". Answers: main story + side quests; rescues, named foes, recovering objects, events in the dungeon; dialogue panels; rewards: the town grows; 4 main + 8 side. Claude's choices, to review (Docs/05 Quests): Mother Aldis as main giver, the twelve quests and their characters, step kinds (rescue, named foe, recover, hold the fire, break, boss, return), the town rewards per quest, no gold, items or XP from quests, ! and ? markers, Quests tab and tracker line | User (shape), Claude, to review (content) |
| 2026-10-08 | **Elite affixes with attacks to dodge.** The owner: "make them have interesting attacks that need to be dodged"; answers: rolled affixes (Diablo 3 style), all four pairs (Molten and Desecrator, Mortar and Plagued, Fire Chains and Arcane Beam, Frost Nova and Lightning Lances). Claude's choices, to review (Docs/05 Elite modifiers): the depth each opens, one affix to depth 6, two from 7, three from 19, at least one attack always, one roll shared by the pack, every number in the table | User (shape), Claude, to review (numbers) |
| 2026-10-08 | **Aim by stick push.** The owner: the further the thumb from the stick's middle, the worse the aim: 100, 80 and 60 percent. Answers: arrows stray off target (they can miss for real); every shot but Homing Arrow. Claude's choices, to review (Docs/02 Basic attack): the bands at a third and two thirds of the push, the stray of up to 6.8 degrees divided by the aim (hitting aim percent of the time against an ordinary enemy 5 away), one roll per shot so a fan turns as one | User (rule), Claude, to review (numbers) |
| 2026-10-10 | **Aim marker.** The owner: "add a small marker"; answer: a cone toward the target. Claude's choices, to review: a wedge as wide as the stray, white, amber and red by band, faint (alpha about a quarter), hidden with no target, when disengaged or within 0.5 | User (marker), Claude, to review (look) |
| 2026-10-10 | **App icon and name.** The owner's new icon (`wildarrowicon.png`, kept in ArtSource/icon) and the name "Wild Arrow" under it on the home screen. Claude's choices, to review: the icon cropped 40 px inside its painted rounded frame and made 1024; only the display name changes (`CFBundleDisplayName`), while the product name, the bundle ID com.filipbusic.rageborn and the save stay Rageborn's | User |
| 2026-10-10 | **Walk when moving slowly.** The owner: a walk animation when moving slowly; answer: download Mixamo's plain walk. Claude's choices, to review: walking below 2.2 units a second with a 0.3 margin | User (ask), Claude, to review (threshold) |
| 2026-10-10 | **Dodge.** The owner: a dodge with Mixamo's Standing Dive when the stick is quickly pulled one way. Answers: two charges, one back every 3 s; about 3 units; she holds fire mid-dive. Claude's choices, to review (Docs/01, the dodge): the flick (0.35 radius to the edge in 0.15 s), 0.4 s dive, untouchable for the first 0.35 s against everything, no charge display yet | User (shape), Claude, to review (numbers) |
| 2026-10-10 | **Dodge on leaving the ring; props walk-through.** The owner: the flick was hard to trigger with no feedback; the dive only in combat, triggered by moving the thumb outside the circle; props without collision ("it is annoying"). Claude's choices, to review: a fight is a target within the last 1.5 s; charge diamonds at the ends of the Focus arc; props on their own tilemap with no collider, enemies walk through them too | User |
| 2026-10-10 | **Dungeon rebuilt: crypt, then caves.** The owner: "a full rebuild of how you generate the dungeons, it still looks really bad. Remake the walls and make it so that the walls are not only right shapes", from his concept images. Answers: crypt to depth 12, caves from 13; drawn terraces with stairs in the caves. Claude's choices, to review (Docs/05 Level generation): crypt room shapes (octagon, round, cross, apse, cut corners) and diagonal wall pieces, cave shapes, terrace rules (one in four caves without; ledges block; steps three wide), boulder and stalagmite art, props and floors per look | User (look), Claude, to review (details) |

## Post-launch plan (draft)

- Weeks 1 to 4: bug fixes, crash review, balance patch based on player reports and simulation.
- Month 2 to 3: quality of life update (loot filter presets, more loadouts, extra stash tabs).
- Month 4 onward: new classes and hardcore mode as post-launch content if sales justify it. How later classes are sold is an open question, see below.

## Immediate next steps

1. Run the M0 prototype: stick, pooled enemies placed as packs in a walled test room (40 in view), one auto-cast skill, item drop and pickup, performance test on a real device.
2. Build the balance simulator skeleton from the formulas in 03-itemization.md and 04-progression-and-economy.md.
3. Write the item and affix data sheets (CSV) for the first 30 affixes and 10 legendaries.
4. Produce a paper prototype of the loadout and results screens and test one-hand reach.
