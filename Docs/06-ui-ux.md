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

The safe area insets are respected on all notched devices. On iPad, the layout centers a phone-width column with the world visible outside it, and the stick zone is anywhere in the lower half.

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
| Pause | Resume, Filter, Settings, Leave zone | Thumb reachable |
| Settings | Controls, audio, haptics, accessibility | Grouped, searchable |
| Leaderboard | Abyss depth, weekly seed | Game Center |

## Navigation

- Menus in town: the Forge, Stash, Trainer and Waystone open by walking up to their NPCs. Where Character, Loadout and Inventory open from is an open question, see 08-production.md.
- A persistent back gesture on every screen using the system edge swipe. Where an edge swipe conflicts, a back button sits at the lower left or lower right by handedness setting.
- Sheets dismiss with a downward swipe on the handle.

## Loadout screen

The most important build screen.

- Four large slots in a vertical stack, each showing icon, name, level, trigger condition and a modifier summary.
- Tap a slot to open the skill list for the class. Locked skills show the level needed.
- Drag handles reorder the priority. A tooltip explains the priority rule: higher slots are considered first.
- The trigger condition is a chip on each slot. A tap opens the list of conditions in the bottom sheet.
- A simulator strip at the bottom shows a 10 second auto-play of the loadout on a training dummy pack so the player sees how the order behaves.
- Saved loadouts: 3 presets per character, switchable from the pause menu.

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
