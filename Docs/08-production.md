# Production plan

Assumptions: a small team of one to three people, part time or full time, with the developer handling design and code. Art and audio may be commissioned. Durations below assume one full time developer and outsourced art. Halve or double the calendar as team size changes.

## Milestones

| Milestone | Duration | Goal | Exit criteria |
|---|---|---|---|
| M0 Prototype | 2 to 3 weeks | Prove input, feel and performance | Stick, 40 enemies, one auto-cast skill, drops with beams, holds 60 fps on iPhone 12 |
| M1 Vertical slice | 8 to 10 weeks | One class, the walkable town, one zone, boss, loot and Forge | 20 minutes of play a tester can finish without help, item comparison works, save works |
| M2 Alpha | 16 to 20 weeks | All systems, act 1 to 3 content, three classes rough | Full loop through level 36, balance simulator running |
| M3 Beta | 12 to 16 weeks | Acts 4 and 5, endgame, polish, accessibility | Feature complete, TestFlight beta, performance targets met on all devices |
| M4 Release candidate | 4 to 6 weeks | Bug fixing, localization pass, store assets | Zero known crashes, App Review submission |
| M5 Launch and support | Ongoing | Patch cadence, balance updates | Post-launch plan in this file |

Total: about 12 to 14 months for a solo developer with commissioned art. A reduced version 1.0 with three acts, three classes and half the legendaries could ship in about 9 months.

## Scope tiers

| Tier | Contents | Use |
|---|---|---|
| Core | Stick, auto-combat, 1 class, act 1, loot, Forge, save | Must ship |
| Full | 3 classes, 5 acts, endgame, all legendaries | Target for 1.0 |
| Stretch | Extra classes, Android, iPad-specific UI, more Abyss features | After 1.0 |

If the schedule slips, cut in this order: optional bosses, fourth and fifth Vigil tiers, Imprint, achievements beyond 30, story cutscenes, then a class or an act.

## Team roles

| Role | Work | Source |
|---|---|---|
| Designer and programmer | Systems, data, tools, UI code | Developer |
| Artist (2D) | Characters, enemies, tiles, item icons, UI | Contract |
| VFX and animation | Effects, rigs | Contract or the same artist |
| Composer and sound | Music stems, SFX | Contract or licensed library |
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
| Premium price limits reach | Medium | Medium | Strong store page, free demo variant considered for post-launch (not decided) |
| Save corruption or loot loss | Low | High | Atomic writes, backups, kill tests |
| App Review rejection | Low | Medium | Follow guidelines, no third party tracking, clear age rating |

## Open questions

1. Price point: premium price not set. Compare against similar premium mobile action RPGs at launch time before deciding.
2. Class count at launch: three is the plan, two is the fallback.
3. Hardcore mode: keep or move to a post-launch update.
4. Skill loadout depth: four slots with trigger conditions is the plan. Decide after the M0 test whether trigger conditions confuse new players.
5. Weekly Abyss seed: whether to include in 1.0 or ship in an update.
6. iPad: scaled phone layout only, or a separate layout.
7. Localization: launch languages beyond English.
8. Whether to include a demo. It helps a premium title but costs extra scope.
9. Returning to town from deep in the dungeon: a free Return to town in the pause menu, a portal scroll, or only by walking up the stairs and the Waystone.
10. Death and checkpoints: revive at the last checkpoint is the current rule. Is the checkpoint the stairs the player last arrived by, or the town?
11. Level persistence: does leaving a level and coming back keep its layout and its dead enemies?
12. Menu access: the hub tab bar is gone, so where do Character, Loadout and Inventory open from, in town and in the dungeon?
13. Whether the Waystone map fully replaces the world map screen, as assumed in 06-ui-ux.md.

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

## Post-launch plan (draft)

- Weeks 1 to 4: bug fixes, crash review, balance patch based on player reports and simulation.
- Month 2 to 3: quality of life update (loot filter presets, more loadouts, extra stash tabs).
- Month 4 onward: a free content update with a fourth class or a new Abyss ruleset if sales justify it. Paid expansions are possible without changing the premium principle.

## Immediate next steps

1. Run the M0 prototype: stick, pooled enemies placed as packs in a walled test room (40 in view), one auto-cast skill, item drop and pickup, performance test on a real device.
2. Build the balance simulator skeleton from the formulas in 03-itemization.md and 04-progression-and-economy.md.
3. Write the item and affix data sheets (CSV) for the first 30 affixes and 10 legendaries.
4. Produce a paper prototype of the loadout and results screens and test one-hand reach.
