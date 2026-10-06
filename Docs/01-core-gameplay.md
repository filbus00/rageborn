# Core gameplay

## Input model

The only combat input is the floating thumb stick.

- Touch zone: the lower 62 percent of the screen. A touch that begins in the zone spawns the stick base under the thumb. The base stays where it was placed until release.
- Dead zone: 8 percent of stick radius. Stick radius is 64 points by default, adjustable from 48 to 96 in settings.
- Analog output: direction is continuous 360 degrees. Speed scales from 0 to 100 percent between dead zone and full radius. Full deflection equals the character move speed. Base move speed is 4 ground units per second (a starting value for tuning).
- Release: the character stops with 80 ms of deceleration. Releasing counts as standing still for Stillness.
- Left-hand and right-hand mode: the touch zone is symmetric. A setting moves the Portal button to the matching corner (the inventory button sits under the portrait since 2026-09-28).
- Stick drift: if the thumb slides more than 1.6 times the radius from the base, the base follows the thumb so the player never runs out of pad.
- Disengage (the owner, 2026-10-03): while the thumb stays inside the stick's ring, the character fights: she keeps shooting and backs away from what she fights. When the thumb leaves the ring, she holds her fire, turns and runs at full speed the way the thumb points; back inside the ring, she fights again. Claude's numbers, to review: she leaves the fight at 1.2 radii from the base and returns inside 1.05, so a thumb on the edge does not flip between the two; the knob follows the thumb out of the ring so leaving is seen.
- Interruptions: a system gesture, call or notification pauses the game. Returning shows a 3 second resume countdown.

No tap, swipe, long press or double tap has a combat function. The top of the screen holds read-only status. The bottom 38 percent outside the touch zone holds nothing interactive during combat except the inventory button, positioned inside the thumb arc.

## Camera and perspective

- Isometric view: a fixed orthographic camera over a 2:1 dimetric grid (a tile is twice as wide as it is tall on screen). No rotation and no perspective. Characters are 2D sprites.
- Resolution, Diablo 2 style on everything (the owner, 2026-09-28): the world renders at about 870 pixels on the long side (a phone's point resolution on a 3x iPhone) and is enlarged in whole, hard-edged pixels; the HUD and menus stay at full resolution. See 07.
- Units: 1 unit is one tile width on screen (128 pixels of art). Every range and radius in these docs is measured on the ground plane in these units, so a circle on the ground appears on screen as an ellipse twice as wide as it is tall.
- Stick input is converted from screen space to ground space (the screen Y component is doubled before normalizing) so the character moves at the same speed in every direction on the ground.
- Portrait framing shows 15 world units top to bottom (orthographic size 7.5, 25 percent closer than the earlier 10 at the owner's request of 2026-09-28), about 7 across on an iPhone, around the player. The player sits at 45 percent of screen height so more space is visible ahead in the movement direction.
- The camera leads the movement direction by up to 1.5 units with smoothing of 0.25 seconds.
- Screen shake is capped and can be disabled.

## Combat model

Combat is real time. The character is always eligible to attack. The auto-attack system runs every frame and follows four rules.

1. Target selection. The character targets the nearest enemy in attack range, whichever way she moves. With bows (the game is bows only since 2026-09-30) the range is the bow's reach, 7.5 or 9 units, and a target must be in sight: an enemy behind a wall is never picked. Elites and bosses get a 30 percent range weight bonus so they are preferred when near. She keeps her target until another is closer by more than 0.75 units, so two enemies at about the same distance do not take turns. (Changed 2026-10-03, the owner: she fired at a far enemy while one stood right by her. The earlier rule preferred a 200 degree cone ahead of the movement, which put a close enemy behind her as she backed away; Claude's margin, to review.)
2. Attack while moving. Basic attacks continue during movement. Melee classes attack at full rate when the target is in reach. Ranged classes attack at full rate always.
3. Facing. The body faces the movement direction. The weapon arm turns to the target. This keeps kiting readable. With sprite characters the arm cannot turn on its own, so retreating from a fight (moving more than 112.5 degrees away from the target within 1.2 s of an attack, skill or hit) shows the character facing the target and running backward (decision of 2026-09-27). Targeting still uses the movement direction.
4. Attack rate. One basic attack per 1 / attacks per second. Attacks per second starts at 1.4 and is modified by gear and skills.

## Auto-cast skill system

Each character carries four active skills in ordered slots. The system evaluates slots from 1 to 4 whenever the global cast timer is free. A skill fires when all of these are true:

- It is off cooldown.
- The resource cost is paid (see resources below).
- Its trigger condition passes.
- A valid target or cast area exists.

Trigger conditions, chosen per slot in the loadout screen:

| Condition | Meaning |
|---|---|
| Always | Fire when off cooldown |
| Enemies 3+ | At least three enemies within the skill area |
| Elite present | An elite or boss is in range |
| Life below 50 percent | Defensive skills |
| Life above 50 percent | Sustain-costing skills |
| Standing | Player has been still for 0.4 s |
| Moving | Player is moving |

The global cast timer is 0.35 seconds. Skills never cancel each other. A skill with a channel time locks lower priority skills until it ends.

### Resource: Focus

The launch class, the Wild Arrow, uses Focus (decided 2026-09-30, 02-classes-and-skills.md), full on every level load. The retired Wrathborn used Rage instead.

- Focus range is 0 to 100. It regenerates 6 per second and gains 4 on each basic attack hit.
- Skills cost Focus or use cooldowns only. Every skill has a cooldown between 1.5 and 20 seconds.
- Most skills cost 15 to 40 Focus. Cooldown-only skills cost nothing but have a longer cooldown.
- Focus (or Rage) is shown as an arc around the character so the player does not look away from the action.

## Stillness and Momentum

Movement is the only decision, so movement must carry a tradeoff. Two states exist and swap automatically.

| State | Trigger | Effect at base | Purpose |
|---|---|---|---|
| Stillness | Standing for 0.4 s | Stacks up to 5, one per 0.4 s. Each stack gives 6 percent increased damage and 4 percent damage reduction. Lost on movement | Reward planting and tanking |
| Momentum | Moving for 0.6 s | Stacks up to 5, one per 0.6 s. Each stack gives 5 percent movement speed and 5 percent chance to dodge. Lost after 1.2 s of standing | Reward kiting and repositioning |

Skills, gear and passives can push a build toward either state. A fortress build stands and stacks Stillness. A skirmisher build circles enemies and stacks Momentum. Both are expressions of the same stick.

## Kiting and telegraphs

Enemies with ranged or area attacks show telegraphs.

- Ground telegraphs: a filled shape, drawn in ground space and projected to the isometric view, fills over 0.6 to 1.2 seconds before damage lands. Hard red outline, soft fill. Color and shape are distinct for damage type.
- Projectile telegraphs: a thin line in the aim direction for 0.4 seconds before launch.
- Charge telegraphs: a line to the target point and a 0.5 second windup.
- Player hit forgiveness: hurtboxes are 70 percent of visual size. Hits that land within 60 ms of leaving a telegraph shape are ignored.
- Dodge chance (from Momentum and gear) applies to projectiles and melee, not to ground telegraph shapes. Ground shapes are avoided only by moving.

## Enemy behavior

**Decided (the owner, 2026-10-06): on the Vigil's road (05) the enemies are a stream of packs from the open rift, coming at her awake, stronger the closer she is to the rift; no timers.** What follows describes each enemy's behaviour and still holds; the placed, idle packs are replaced.


Enemies use one of six archetypes. Each has a state machine of Idle, Aggro, Approach, Attack, Recover and Death.

| Archetype | Behavior | Counterplay |
|---|---|---|
| Swarmer | Fast, low health, chases in a loose cluster | Kite into a line, area skills |
| Brute | Slow, high health, telegraphed slam | Move out of the shape, then return |
| Archer | Keeps distance, fires a line-telegraphed shot | Close in or break line of sight |
| Caster | Places ground shapes near the player, summons | Move constantly, kill priority |
| Charger | Winds up then rushes in a straight line | Sidestep during the windup |
| Support | Buffs or heals nearby enemies, stays behind the pack | Reach it, or use area skills |

Aggro range is 7 units for normal enemies and 10 for elites. Leash range is 20 units. Enemies stop tracking when the player is beyond leash range for 4 seconds, then walk back to where they started and return to Idle. Pack sizes range from 3 to 12 for normal packs.

Placement: the level generator puts each pack in a room, and it idles at its home spot until the player comes within aggro range. Nothing spawns around the player and there are no waves. Killed enemies stay dead while the level is loaded. The town is a safe zone with no enemies.

### Elite modifiers

Each elite carries one or two of these, chosen at spawn: Molten (leaves fire pools), Frozen (slows on hit), Vampiric (heals on hit), Shielded (regenerating barrier), Teleporting (blinks to the player), Splitting (spawns adds at half health), Hasted (plus 30 percent move and attack speed), Cursing (reduces player resistance stacks).

### Bosses

Bosses use phases. Each boss has three phases with a distinct attack set and an arena hazard. The stick is the only counter. Boss design rule: a player at gear parity dies to a boss only through repeated telegraph mistakes, never through unavoidable damage.

## Death and recovery

**Decided (the owner, 2026-10-06): on the Vigil's road (05), she wakes at the last lit beacon and loses some gold (proposed: 10 percent of carried gold). Her gear stays on her; no corpse.** The rules below are the earlier corpse run, kept as history.


- On death the character falls and is sent to the town.
- The character loses its equipped gear at the death spot, where a corpse marks it. The gear is regained by walking to the corpse and picking it up. The backpack, gold and the Stash are kept.
- The corpse never expires. If the character dies again before reaching it, the first corpse stays where it is and the second holds nothing, because nothing is equipped. Whether a corpse survives the sleep reset is decided with the sleep design, see 08-production.md.
- To get back to the corpse the player can use a portal that is still open (made with the Portal Tome, found around level 3 of the dungeon) or the waypoints across the dungeon levels, see 05-world-and-content.md. Before the Tome the player walks.
- This replaces the earlier free checkpoint revive, the instant revive and the no item loss rule.
- Hardcore mode is not in 1.0. It is planned as a post-launch update.

## Auto features that protect one-handed play

- Auto-loot: items within 2.5 units are picked up when the player has not been hit in the last 1.5 seconds, or when nothing is engaged with the character (as built; Q22). Gold and materials are always auto-picked.
- Loot filter: see 03-itemization.md.
- Auto-potion: one potion type, heals 40 percent life over 3 seconds. Charges refill from kills. Fires at 35 percent life, configurable from 20 to 60.
- Inventory button: sits under the portrait in the top-left corner (the owner, 2026-09-28; before, at the lower edge in the thumb arc) and opens one screen with character stats, inventory, equipped gear and loadout. There is no separate pause menu. The screen also holds Settings and the loot filter, and the game is paused while it is open.

## Session structure

A typical session:

1. Open game, tap Continue. The character appears in the town within 5 seconds: a loaded game always restarts in town (decision of 2026-09-23; Q22). A new character starts in the town.
2. Walk down the stairs and play a level or rift for 3 to 8 minutes.
3. On the results panel, review drops that beat current gear. Equip with one tap.
4. Optionally walk to the Forge in town and spend gold and materials.
5. Leave. The state is saved.

## Feedback

- Hit feedback: 40 ms hit stop on heavy hits, sprite flash, damage numbers with a grouping option that merges numbers within 0.3 seconds.
- Haptics: light tap on a legendary drop, medium on a level up, rhythmic on boss phase change. All optional.
- Audio: layered combat music that adds a stem per pack size. Distinct drop sounds per rarity, audible when the phone is on ring.
- Loot beams: colored light columns by rarity so drops are visible in a crowd.
