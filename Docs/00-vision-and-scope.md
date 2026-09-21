# Rageborn: vision and scope

Document status: draft 0.2, 2026-09-21. This file is the entry point to the design set. Numbers in every file are starting values for tuning, not final.

## Document set

| File | Contents |
|---|---|
| 00-vision-and-scope.md | Pitch, pillars, audience, decisions made, scope limits, glossary |
| 01-core-gameplay.md | Controls, combat, auto-skill system, enemy behavior, death and recovery |
| 02-classes-and-skills.md | Three classes, skill lists, skill modifiers, passive tree |
| 03-itemization.md | Slots, rarities, affixes, drops, loot filter, crafting, formulas |
| 04-progression-and-economy.md | XP curve, difficulty tiers, gold and material sinks, session pacing |
| 05-world-and-content.md | Setting, zones, enemy roster, bosses, endgame modes |
| 06-ui-ux.md | Screen inventory, one-hand layout, menus, feedback, accessibility |
| 07-technical.md | Engine recommendation, architecture, data, save, performance, tooling |
| 08-production.md | Milestones, team assumptions, risks, open questions, decision log |

## Pitch

Rageborn is a portrait, one-thumb isometric action RPG for iPhone in the Diablo tradition. The player walks through cursed ruins with a floating thumb stick. The character fights on its own. Everything else the player cares about happens in gear: reading item drops, comparing affixes, socketing, reforging and choosing which four skills the character carries. A session is three to ten minutes of movement and looting, followed by a short menu pass to improve the build.

The game is a premium purchase. There are no in-app purchases, no energy timers and no ads. Every drop is tuned for the fun of finding it.

## Pillars

1. Movement is the only input in combat. No attack button, no skill buttons, no dodge button. Positioning, pulling and kiting carry all of the moment-to-moment skill.
2. Loot drives everything. A drop should change what the player does next within one minute of finding it. Gear is the main progression axis, character level is secondary.
3. One hand, always. Every screen, including menus and item comparison, is usable with one thumb on a phone held in one hand.
4. Short and dense. A full loop of enter, fight, loot, upgrade fits in five minutes. The player can stop at any point without losing progress.
5. Readable chaos. Packs of thirty enemies and heavy effects must never hide the player character, enemy telegraphs or item drops.

## Target audience

Players aged 18 to 45 who know Diablo, Path of Exile or Vampire Survivors style games and play on the phone in short windows: commutes, waiting rooms, one hand on a rail. They accept a premium price. They want build theorycrafting and item hunting without a controller or two hands.

## Decisions taken

| Topic | Decision | Source |
|---|---|---|
| Orientation | Portrait | User |
| Control | Floating thumb stick, movement only. Attacks and skills fire automatically | User |
| Setting | Dark gothic fantasy | User |
| Game name | Rageborn | User |
| Business model | Premium, one-time price, no in-app purchases | User |
| Engine | Unity 6000.6.2f1 with the Universal Render Pipeline in 2D mode, see 07-technical.md | User |
| Camera and art | Isometric view (2:1 dimetric), 2D sprites with skeletal animation, isometric Tilemap for the world | User |
| Game structure | Diablo 1 style: a safe town, then a dungeon descended level by level. Not a survivor or horde game: enemies are packs placed in rooms, idle until aggro, and killed enemies stay dead | User |
| Town | A walkable scene with no enemies. NPCs open their panels when the player walks up to them. The way into the dungeon is a stairway the player walks into | User |
| Dungeon | Diablo 1 style descent by stairs. Each act is a dungeon of about 6 seeded levels below its own town | User |
| Players | Single player, offline first | Recommendation |
| Platform | iPhone first, iPad supported by layout scaling, Android not in scope for 1.0 | Recommendation |

## Scope for version 1.0

In scope:

- Three classes, eight active skills each, one passive tree each
- Five acts, each a town above a dungeon of about 6 levels, seeded procedural layouts
- Character level cap 60, then a Paragon track to 200 for endgame
- Ten equipment slots, four rarities, about 90 affixes, 60 legendary items
- Abyss endless dungeon and five bosses in a repeatable boss rotation
- Local save with iCloud sync, Game Center leaderboards for Abyss depth
- English UI at launch, string tables ready for localization

Out of scope for 1.0:

- Multiplayer, trading, guilds, live events
- Manual skill activation, dodge rolls, any second input
- Controller support and Android
- Offline idle gains

## Success criteria

- New player reaches first legendary drop within the first 60 minutes.
- Median session length between 4 and 12 minutes.
- Day 7 return above 20 percent among buyers, measured with privacy-preserving analytics if any are used.
- 60 frames per second on iPhone 12 and newer with 40 enemies on screen, 30 on older supported devices.
- No crash-loss of loot: every item drop is saved within 2 seconds of pickup.

## Glossary

| Term | Meaning |
|---|---|
| Auto-cast | Skills fire without player input when their conditions are met |
| Loadout | The four active skills and one keystone the character carries into a run |
| Priority | The order in which off-cooldown skills are considered by the auto-cast system |
| Affix | A random modifier on an item, prefix or suffix |
| Tier (affix) | Strength band of an affix, T1 strongest, gated by item level |
| Item level (ilvl) | Power budget of a dropped item, equal to the zone level at drop time |
| Pack | A group of enemies placed together in a room by the level generator |
| Town | The safe, walkable scene above each act's dungeon: no enemies, with NPCs for the Forge, Stash, Trainer and Waystone |
| Level | One seeded room layout in a dungeon, entered by stairs. Earlier drafts and other files call it a zone; the two words mean the same thing |
| Elite | Stronger enemy with one or two modifiers, guaranteed drops |
| Rift | A time-limited zone run, used in the campaign as a repeatable farm |
| Abyss | Endless dungeon mode with rising floor depth |
| Paragon | Post-60 progression track that adds small stat points |
| Stillness | Bonus state gained by standing still, see 01-core-gameplay.md |
| Momentum | Bonus state gained by moving, see 01-core-gameplay.md |
