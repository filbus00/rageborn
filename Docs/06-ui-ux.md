# UI and UX

## Principles

1. Everything interactive sits in the lower 60 percent of the screen, inside the thumb arc of a right or left hand holding a phone.
2. The top of the screen is for reading. Nothing in the top 25 percent responds to touch except the mini-map expand tap.
3. Every menu opens as a bottom sheet with large targets. Minimum tap target 48 by 48 points, preferred 56.
4. Comparison beats reading. Show the change (arrows, colors) before the raw numbers.
5. One decision per screen. If a screen needs two, it becomes two steps.

## Reach model

Assume a 6.1 inch phone held in one hand. Comfortable thumb reach covers the lower 55 to 60 percent of the screen and the far-side upper area is not reachable.

| Zone | Content |
|---|---|
| Top 0 to 25 percent | Life bar, Focus indicator, level, buff icons, mini-map. Read only |
| Middle 25 to 60 percent | Play field. Dialogs and drop popups appear here |
| Bottom 60 to 100 percent | Stick zone in combat, menu controls out of combat |

The safe area insets are respected on all notched devices. The game is iPhone only for 1.0.

## HUD in combat

- Life: a slim bar at the top and a ring around the character. The ring pulses under 35 percent.
- Focus: an arc on the character's feet.
- Skill icons: four small icons in a row at the top showing cooldown state. They are not buttons. Cooldown sweeps are drawn on top.
- Potion charges: three pips beside the life bar.
- Stillness and Momentum stacks: pips above the character's head, colored blue and amber.
- Boss bar: a wide bar just below the top row with phase markers.
- Damage numbers: small, grouped and pooled. Player damage taken is red and centered on the character.
- Loot drop labels: rarity color, name for Rare and above only. Common and Magic show as a small colored diamond.
- Mini-map: a corner overlay, 88 by 88 points, collapsed by default in combat.

## Screen inventory

| Screen | Purpose | Notes |
|---|---|---|
| Title | Continue, New Character, Settings | Continue is the largest button |
| Character select | Up to 12 characters, sort by last played | Shows class, level, power score |
| Town panels | Forge, Stash, Trainer, Waystone, each opened by walking up to its NPC in the town | Bottom sheet |
| Waystone map | Choose a level already reached, tier, rift, Abyss | Opened at the Waystone. Vertical scroll, act headers, recommended power per level |
| Loadout | Choose four skills, order, triggers, keystone | Drag to reorder, tap to open trigger picker |
| Skill detail | Level, modifiers, tags | Bottom sheet |
| Passive tree | Spend points | Pan by drag, tap to select, pinch to zoom with a fallback zoom slider |
| Equipment | Ten slots on a paper doll | Tap a slot, opens a filtered list |
| Inventory | Scrolling list, filter and sort chips | Long press for multi select |
| Stash | Same as inventory | Search field, filter chips |
| Forge | Reforge, Socket, Temper, Imprint, Transmog | One action per tab |
| Results | End of zone summary and drops | Swipe through upgrades, tap Equip |
| Character | Character stats, inventory, equipped gear and loadout, plus Settings and the loot filter | Opened by the inventory button. There is no separate pause menu, and the game is paused while it is open |
| Settings | Controls, audio, haptics, accessibility | Grouped, searchable |

## Navigation

- Menus in town: the Forge, Stash, Trainer and Waystone open by walking up to their NPCs. Character, Loadout and Inventory open from the inventory button, in town and in the dungeon. There is no separate pause menu. Settings and the loot filter are on the same screen, and the game pauses while it is open.
- A persistent back gesture on every screen using the system edge swipe. Where an edge swipe conflicts, a back button sits at the lower left or lower right by handedness setting.
- Sheets dismiss with a downward swipe on the handle.

## Loadout screen

The most important build screen.

- Four large slots in a vertical stack, each showing icon, name, level, trigger condition and a modifier summary.
- Tap a slot to open the skill list for the class. Locked skills show the level needed.
- Drag handles reorder the priority. A tooltip explains the priority rule: higher slots are considered first.
- The trigger condition is a chip on each slot. A tap opens the list of conditions in the bottom sheet.
- A simulator strip at the bottom shows a 10 second auto-play of the loadout on a training dummy pack so the player sees how the order behaves.
- Saved loadouts: 3 presets per character, switchable from this screen.

## Results screen

- A vertical list of drops, best first, with upgrade arrows and power score deltas.
- One tap on an upgrade shows the comparison sheet.
- Big buttons at the bottom: Equip all upgrades, Salvage the rest, Continue.
- Equip all upgrades is undoable for 10 seconds.

## Onboarding

- First 10 minutes: a short scripted scene in the town, then a scripted first level. The stick is taught by a ghost thumb overlay for 5 seconds only.
- Auto-attack and auto-skills are not taught with text. Enemies die, so the player learns by seeing.
- The first legendary drop is guaranteed at minute 20 of play, from a scripted elite. A short tooltip walks through comparing and equipping it.
- The Forge is introduced after the first Rare drop.
- Loot filter is introduced after 200 items dropped.

## Feedback and juice

- Rarity beams and sounds as described in 01-core-gameplay.md.
- Level up: a burst, a short sound, and a passive point badge on the Character tab.
- Upgrade badge: a small green arrow on the Character tab when a drop is better.
- Screen flash on boss phase change, disabled by the reduce flashing setting.

## Accessibility

- Color blind modes: protan, deutan, tritan, with shape-coded rarity icons (diamond, hex, star, crown).
- Text size: 4 steps, dynamic type respected.
- Reduce motion: disables camera lead, screen shake and hit stop.
- Reduce flashing: replaces flashes with fades.
- Stick size, position lock and dead zone adjustable.
- Auto pause when the game moves to the background or an incoming call arrives.
- Hearing: every important audio cue has a visual counterpart such as a directional indicator for off-screen ranged attackers.
- VoiceOver on all menu screens. Combat is not VoiceOver playable and this is stated in the settings.

## Visual style of UI

- Dark stone panels with thin ember colored strokes, angular corners, minimal gradients.
- Two typefaces: a serif display face for titles and item names, a legible sans for numbers.
- Numbers use tabular figures so stat changes do not shift the layout.
- Rarity colors: Common #9A9A9A, Magic #4A7BD4, Rare #E0C040, Legendary #E07A20, Cursed #9B4FD0. All contrast tested against panel background.

## Full-game plan: screens (proposed, not decided)

Written 2026-09-27 for the full-game plan (`10-full-game-plan.md`). Layouts for every screen in the inventory above that is not built yet, and for the screens the other proposals need. All follow the principles above: controls in the lower 60 percent, reading at the top, bottom sheets, 48 point minimum and 56 preferred tap targets, one decision per screen. Built screens (the Bag inventory and item sheet, the Forge sheet, the waypoint list, the hint banner) keep their built layouts. Questions are numbered as in `08-production.md`.

### Common frame

- A sheet has a drag handle and a title at its top edge, content in the middle, and its action row at the bottom, where the thumb rests. Close is always the right-most button of the action row (the item sheet does this today) and a downward swipe on the handle also closes it.
- Lists scroll vertically only. Rows are 56 points tall; a row's main action button sits at its right end.
- Tabs sit just above the action row, never at the top of the screen.

### The Character screen (the Bag button)

Decided: one inventory button opens stats, inventory, equipped gear, loadout, Settings and the loot filter, and pauses the game. Built today as the Bag screen (equipped slots, stats, backpack). Proposed: the same full-screen panel gains a tab row above its bottom edge: Gear, Skills, Tree, Stats, Filter, Journal, Settings. Gear is the built screen, extended by the paper doll below. A badge on a tab shows unspent points (Skills, Tree) or an upgrade (Gear).

### Equipment paper doll (Gear tab)

- Top 40 percent (read only): the character as the game draws him, idle, facing S, with the equipped looks and the grip (the layered renderer is built), and the power score under him.
- Below it: the ten slots as tiles, two rows of five, 64 points each, in the order weapon, off-hand, helm, chest, gloves / boots, belt, amulet, ring, ring. A tile shows the icon in its rarity frame, or an empty outline. A green arrow marks a slot with an upgrade in the backpack.
- Tapping a tile opens a sheet with that slot's item and, under it, the backpack filtered to that slot, best first, with Equip on each row. Tapping a row opens the built item sheet.
- Under the tiles: the backpack list as built (40 rows, sort chips: Newest, Power, Slot).

### Loadout (Skills tab)

As designed above (Loadout screen), with details:

- Four slot rows (80 points tall) in priority order: icon, name, level, the trigger chip, the three modifier picks as small icons. A drag handle on the right reorders them.
- Under the slots: the keystone row (Berserker or Juggernaut, from level 20 when bought in the tree; 02).
- Tapping a slot opens the skill list sheet: all eight skills, locked ones with their unlock level, each with an Equip button. Tapping the trigger chip opens the trigger sheet (Q15).
- The simulator strip (above) stays a proposal for after launch: it needs a training scene that the one-thumb game does not otherwise have.
- Presets: three chips above the action row; presets 2 and 3 are bought with gold (04, Q16).

### Skill detail

A bottom sheet from any skill icon: name, tags, level with the skill point cost, current and next level numbers, trigger, then the three modifier tiers. Each tier shows its three options as 56 point cards; a locked tier shows its skill level. Action row: Level up (spends a point), Close.

### Passive tree (Tree tab)

The design above says pan by drag and pinch to zoom. Proposed for one thumb: the tree is drawn as three vertical columns (Wrath, Stampede, Scar, 02) that scroll up and down together, no zoom needed. Nodes are 56 point circles for notables and keystones and 44 point for minor nodes (a minor node's hit area is still 48). Tapping a node opens a sheet with its effect and Buy (disabled with the reason when it cannot be bought). Points left show at the top. Respec happens at the Trainer, not here.

### Stats tab

A read-only list, grouped: Offense (hit, damage per second, attack speed, crit), Defense (life, armor and its reduction against an enemy of the character's level, resistances, dodge, block), Rage and stances, Utility (Magic Find, gold find, move speed). Each value explains itself in a sheet when tapped.

### Loot filter (Filter tab)

Presets as 56 point cards (Everything, Smart, Upgrades only, Legendary only) with the active one marked, two taps from the combat screen as designed. "Custom" opens a rule list: each rule is a row "Hide Magic below item level 20 in Gloves"; Add rule opens three pickers one after another (rarity, slot, minimum item level), one decision per step. Filtered items go where Q11 decides.

### Journal tab

The Codex of Legendaries found (05, Endgame), lore landmarks read, achievements (Q17), and the personal bests (deepest Abyss floor, fastest rift). Read only.

### Settings tab

Grouped, with a search field at the bottom (above: "grouped, searchable"). Options the other docs wait for are marked with their source.

| Group | Setting | Range, default |
|---|---|---|
| Controls | Stick size (01) | 48 to 96 points, 64 |
| Controls | Dead zone (01) | 4 to 20 percent, 8 |
| Controls | Stick position lock (above) | Floating, or fixed at the last spot; floating |
| Controls | Handedness (01): moves the Bag and Portal buttons | Right, left; right |
| Combat | Auto-potion threshold (01) | 20 to 60 percent life, 35 |
| Combat | Damage numbers (01) | On, grouped, off; grouped |
| Combat | Loot labels | Rare and above, all, off; Rare and above |
| Audio | Music, effects, ambience volumes (07) | 0 to 100, 80, 100, 80 |
| Audio | Play sound on silent (drop sounds follow the ring switch today, 01) | On, off; off |
| Haptics | Haptics (01) | On, off; on |
| Accessibility | Reduce flashing (above): the boss phase flash becomes a fade | On, off; off |
| Accessibility | Reduce motion (above): no camera lead, shake or hit stop | On, off; off |
| Accessibility | Screen shake (01) | 0 to 100, 60 |
| Accessibility | Colour blind mode (above) | Off, protan, deutan, tritan |
| Accessibility | Text size (above) | 4 steps |
| Game | Replay story scenes (05) | A list of the scenes seen |
| Game | iCloud sync (07) | On, off; on; last sync time |
| Game | Restore a backup (07) | Lists the 3 backups with their dates |
| Game | Credits, licences, version | |

### Title screen

- The title background (09) with the RAGEBORN title in the display face in the top 30 percent.
- Buttons in the bottom 35 percent: Continue (largest, 72 points tall, showing the character's name, level and act), Characters, Settings.
- Cold start to Continue under 6 seconds (07).

### Character select

A list of up to 12 characters (above), sorted by last played: a row shows the class icon, name, level (and Paragon), the Vigil tier and act, power score. Action row: Play, New, Delete (with a typed confirmation, the only text entry in the game besides naming). New asks for a name (the system keyboard) and, since one class ships, goes straight into the act 1 town.

### Town NPC sheets

| NPC | Sheet |
|---|---|
| Stash keeper | Like the backpack list, with tab chips (one per 30 slots bought), a search field at the bottom and Move on each row (backpack to stash or back). A second list shows the backpack under it on a toggle, so every move is one tap |
| Trainer | Two buttons: Reset passive tree, Reset skill points, each with its gold cost and a confirmation; a third changes one modifier pick. It shows the free respec when one is available (02: once per act clear) |
| The Watcher | Tabs Rifts and Abyss. Rifts lists the keys owned (tier and two modifiers per key), best first, with Open on each. Abyss shows the personal best, a start floor picker (multiples of 10), and Enter |

### Waystone map

Built today: a list of Town and the activated waypoints. Proposed:

- A tier chip row (Vigil I to V, locked ones greyed) above the list. Waypoints are kept per tier, as Diablo 2 did per difficulty.
- The list: act headers (with the town first), then the depths with enemy level and the recommended power score (04, Difficulty adaptation). The current spot is shown but not a button (built).
- A Bosses page (from Vigil II): every boss killed, with Fight (costs a Boss Sigil, 05) at the chosen tier.
- 05 says rifts and the Abyss are entered from an NPC in town; the screen table above puts them on the Waystone map. Proposed: the Watcher holds them (Q22).

### Results

Designed above (a list of drops, best first, Equip all upgrades, Salvage the rest, Continue, undo for 10 s). Proposed (Q19): it opens on arriving in town when anything was picked up since the last visit, never in the dungeon (a level has no end screen in a stair-descent game). "Salvage the rest" would break the Forge-only salvage decision; proposed instead: "Mark the rest for salvage", which moves them to the salvage pouch the smith empties (03, Loot filter).

### Story scenes

A bottom panel over the paused game: the speaker's portrait on the left, 2 to 3 lines of text, a tap anywhere advances, Skip at the lower right. Scenes in town let the player keep walking once they end. Format Q20.

### HUD additions

- Skill cooldown icons (above, not built): four 40 point icons in a row under the XP bar, read only.
- The Portal and Bag buttons move to the left with left-hand mode.
- A small tier badge (I to V) beside the level number when not on Vigil I.
