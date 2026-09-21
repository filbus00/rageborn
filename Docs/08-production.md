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
| Premium price limits reach | Medium | Medium | Strong store page, free demo variant, decided after beta |
| Save corruption or loot loss | Low | High | Atomic writes, backups, kill tests |
| App Review rejection | Low | Medium | Follow guidelines, no third party tracking, clear age rating |

## Open questions

1. Price point: decide at launch. Compare against similar premium mobile action RPGs then.
2. Skill loadout depth: four slots with trigger conditions is the plan. Decide after the M0 test whether trigger conditions confuse new players.
3. Demo: decide after beta whether to offer one. It helps a premium title but costs extra scope.
4. Selling later classes: decide after launch. A paid class add-on would be an in-app purchase on iOS, which conflicts with the premium, no in-app purchases decision.
5. The sleep mechanic: how a game session resets, what it resets (level layouts, enemies, chests), and whether a corpse survives it.
6. One-time unlocks such as extra stash tabs or loadout presets were tied to the Ember Shard, which is removed. What gates them now?
7. Achievements: the Game Center achievements are cut with the other online features. Are achievements kept as local ones?
8. The Wrathborn rework: how the Warden becomes a rage-themed barbarian (skill list, keystones, and whether it leans Stillness or Momentum).

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

## Post-launch plan (draft)

- Weeks 1 to 4: bug fixes, crash review, balance patch based on player reports and simulation.
- Month 2 to 3: quality of life update (loot filter presets, more loadouts, extra stash tabs).
- Month 4 onward: new classes and hardcore mode as post-launch content if sales justify it. How later classes are sold is an open question, see below.

## Immediate next steps

1. Run the M0 prototype: stick, pooled enemies placed as packs in a walled test room (40 in view), one auto-cast skill, item drop and pickup, performance test on a real device.
2. Build the balance simulator skeleton from the formulas in 03-itemization.md and 04-progression-and-economy.md.
3. Write the item and affix data sheets (CSV) for the first 30 affixes and 10 legendaries.
4. Produce a paper prototype of the loadout and results screens and test one-hand reach.
