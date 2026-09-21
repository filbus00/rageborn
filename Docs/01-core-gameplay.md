# Core gameplay

## Input model

The only combat input is the floating thumb stick.

- Touch zone: the lower 62 percent of the screen. A touch that begins in the zone spawns the stick base under the thumb. The base stays where it was placed until release.
- Dead zone: 8 percent of stick radius. Stick radius is 64 points by default, adjustable from 48 to 96 in settings.
- Analog output: direction is continuous 360 degrees. Speed scales from 0 to 100 percent between dead zone and full radius. Full deflection equals the character move speed. Base move speed is 4 ground units per second (a starting value for tuning).
- Release: the character stops with 80 ms of deceleration. Releasing counts as standing still for Stillness.
- Left-hand and right-hand mode: the touch zone is symmetric. A setting moves the inventory button to the matching corner.
- Stick drift: if the thumb slides more than 1.6 times the radius from the base, the base follows the thumb so the player never runs out of pad.
- Interruptions: a system gesture, call or notification pauses the game. Returning shows a 3 second resume countdown.

No tap, swipe, long press or double tap has a combat function. The top of the screen holds read-only status. The bottom 38 percent outside the touch zone holds nothing interactive during combat except the inventory button, positioned inside the thumb arc.

## Camera and perspective

- Isometric view: a fixed orthographic camera over a 2:1 dimetric grid (a tile is twice as wide as it is tall on screen). No rotation and no perspective. Characters are 2D sprites.
- Units: 1 unit is one tile width on screen (128 pixels of art). Every range and radius in these docs is measured on the ground plane in these units, so a circle on the ground appears on screen as an ellipse twice as wide as it is tall.
- Stick input is converted from screen space to ground space (the screen Y component is doubled before normalizing) so the character moves at the same speed in every direction on the ground.
- Portrait framing shows roughly 9 by 16 screen units around the player. The player sits at 45 percent of screen height so more space is visible ahead in the movement direction.
- The camera leads the movement direction by up to 1.5 units with smoothing of 0.25 seconds.
- Screen shake is capped and can be disabled.

## Combat model

Combat is real time. The character is always eligible to attack. The auto-attack system runs every frame and follows four rules.

1. Target selection. The character targets the nearest enemy in attack range that is inside a 200 degree forward cone. If none, the nearest enemy in range regardless of facing. Elites and bosses get a 30 percent range weight bonus so they are preferred when near.
2. Attack while moving. Basic attacks continue during movement. Melee classes attack at full rate when the target is in reach. Ranged classes attack at full rate always.
3. Facing. The body faces the movement direction. The weapon arm turns to the target. This keeps kiting readable.
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

- Focus range is 0 to 100. It regenerates 6 per second and gains 4 on each basic attack hit.
- Skills cost Focus or use cooldowns only. Every skill has a cooldown between 1.5 and 20 seconds.
- Most skills cost 15 to 40 Focus. Cooldown-only skills cost nothing but have a longer cooldown.
- Focus is shown as an arc around the character so the player does not look away from the action.

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

- On death the character falls and is sent to the town.
- The character loses all its gear at the death spot, where a corpse marks it. The gear is regained by walking to the corpse and picking it up.
- To get back to the corpse the player can use a portal that is still open, or the waypoints across the dungeon levels, see 05-world-and-content.md.
- This replaces the earlier free checkpoint revive, the Ember Shard instant revive and the no item loss rule. Which items and how much gold count as gear, what happens on a second death before the corpse is reached, and what the Ember Shard is for now are open questions, see 08-production.md.
- Hardcore mode is not in 1.0. It is planned as a post-launch update.

## Auto features that protect one-handed play

- Auto-loot: items within 2.5 units are picked up when the player is not under attack in the last 1.5 seconds, or when the room clears. Gold and materials are always auto-picked.
- Loot filter: see 03-itemization.md.
- Auto-potion: one potion type, heals 40 percent life over 3 seconds. Charges refill from kills. Fires at 35 percent life, configurable from 20 to 60.
- Inventory button: sits at the lower edge in the thumb arc and opens one screen with character stats, inventory, equipped gear and loadout. There is no separate pause menu. Whether opening it pauses the game is an open question.

## Session structure

A typical session:

1. Open game, tap Continue. The character appears where they left off, in the town or on a dungeon level, within 5 seconds. A new character starts in the town.
2. Walk down the stairs and play a level or rift for 3 to 8 minutes.
3. On the results panel, review drops that beat current gear. Equip with one tap.
4. Optionally walk to the Forge in town and spend gold and materials.
5. Leave. The state is saved.

## Feedback

- Hit feedback: 40 ms hit stop on heavy hits, sprite flash, damage numbers with a grouping option that merges numbers within 0.3 seconds.
- Haptics: light tap on a legendary drop, medium on a level up, rhythmic on boss phase change. All optional.
- Audio: layered combat music that adds a stem per pack size. Distinct drop sounds per rarity, audible when the phone is on ring.
- Loot beams: colored light columns by rarity so drops are visible in a crowd.
