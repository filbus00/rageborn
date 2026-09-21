using System;
using System.Collections.Generic;
using UnityEngine;

namespace ARPG
{
    /// <summary>What the character has equipped. Only a weapon slot exists so far; no weapon means unarmed.</summary>
    public readonly struct EquipmentState
    {
        public EquipmentState(Item weapon) => Weapon = weapon;

        /// <summary>The equipped weapon, or null when unarmed.</summary>
        public Item Weapon { get; }

        public bool IsEmpty => Weapon == null;

        /// <summary>
        /// Average damage of what the character fights with. Unarmed is the weapon curve at item level 0, a little under
        /// the level 1 starting weapon.
        /// </summary>
        public float WeaponDamage => Weapon != null ? Weapon.WeaponAverageDamage : CombatFormulas.WeaponAverageDamage(0);

        public static EquipmentState Empty => new EquipmentState(null);

        /// <summary>The gear a new character starts with: a common level 1 weapon. A tuning value.</summary>
        public static EquipmentState Starting => new EquipmentState(new Item(ItemSlot.Weapon, ItemRarity.Common, 1));
    }

    /// <summary>The equipment a dead character left behind, waiting where it died.</summary>
    public sealed class Corpse
    {
        public Corpse(string levelId, Vector2 groundPosition, EquipmentState gear)
        {
            LevelId = levelId;
            GroundPosition = groundPosition;
            Gear = gear;
        }

        /// <summary>The level the character died on, by scene name.</summary>
        public string LevelId { get; }

        public Vector2 GroundPosition { get; }

        public EquipmentState Gear { get; }
    }

    /// <summary>
    /// Everything that has to survive changing scene within one game session: equipment, the backpack, gold, corpses,
    /// which enemies were killed, life and the loot roller. Pure, so it can be tested; <see cref="Current"/> holds
    /// the live one. A future sleep mechanic resets the session (Docs/08-production.md, open question 5), and saving
    /// to disk builds on this.
    /// </summary>
    public sealed class GameSession
    {
        readonly List<Corpse> corpses = new List<Corpse>();
        readonly Dictionary<string, HashSet<int>> killed = new Dictionary<string, HashSet<int>>();

        static GameSession current = new GameSession(Environment.TickCount);

        public GameSession() : this(0)
        {
        }

        /// <param name="lootSeed">Seeds the loot rolls, so a session can be replayed.</param>
        public GameSession(int lootSeed)
        {
            Loot = new LootRoller(lootSeed);
            Equipment = EquipmentState.Starting;
        }

        /// <summary>The live session. Replaced at the start of every play, so nothing leaks between editor plays.</summary>
        public static GameSession Current => current;

        /// <summary>Raised when equipment, the backpack or gold changes. The HUD listens to it.</summary>
        public event Action Changed;

        public EquipmentState Equipment { get; private set; }

        public Inventory Inventory { get; } = new Inventory();

        public int Gold { get; private set; }

        public LootRoller Loot { get; }

        public IReadOnlyList<Corpse> Corpses => corpses;

        /// <summary>The character's life as a fraction of its maximum, carried between scenes.</summary>
        public float LifeFraction { get; set; } = 1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetForNewPlay() => current = new GameSession(Environment.TickCount);

        /// <summary>Equips gear, replacing what the character wears. What was worn is not kept; see <see cref="PickUp"/>.</summary>
        public void Equip(EquipmentState equipment)
        {
            Equipment = equipment;
            Changed?.Invoke();
        }

        public void AddGold(int amount)
        {
            if (amount <= 0)
                return;

            Gold += amount;
            Changed?.Invoke();
        }

        /// <summary>
        /// Puts a found item in the backpack. Returns false, leaving the item where it is, when the backpack is full.
        /// A weapon that beats the equipped one is equipped at once and the old one goes to the backpack. That
        /// automatic rule stands in for the equip screen, which does not exist yet.
        /// </summary>
        public bool PickUp(Item item)
        {
            if (!Inventory.TryAdd(item))
                return false;

            if (item.Slot == ItemSlot.Weapon && (Equipment.IsEmpty || item.WeaponAverageDamage > Equipment.Weapon.WeaponAverageDamage))
            {
                // Swap: the new weapon leaves the backpack and the old one takes its slot, so the count is unchanged.
                Inventory.Remove(item);
                if (!Equipment.IsEmpty)
                    Inventory.TryAdd(Equipment.Weapon);
                Equipment = new EquipmentState(item);
            }

            Changed?.Invoke();
            return true;
        }

        /// <summary>Records that a pack's member was killed. It stays dead while the session lasts.</summary>
        public void RecordKill(string packKey, int slot)
        {
            if (!killed.TryGetValue(packKey, out var slots))
            {
                slots = new HashSet<int>();
                killed[packKey] = slots;
            }
            slots.Add(slot);
        }

        public bool IsKilled(string packKey, int slot) => killed.TryGetValue(packKey, out var slots) && slots.Contains(slot);

        /// <summary>
        /// The character dies: its equipped gear stays behind as a corpse where it fell (Docs/01-core-gameplay.md).
        /// Returns the new corpse, or null when nothing was equipped, since a corpse would hold nothing.
        /// The backpack and gold are kept. An earlier corpse is never touched, and corpses never expire.
        /// </summary>
        public Corpse Die(string levelId, Vector2 groundPosition)
        {
            LifeFraction = 1f;
            if (Equipment.IsEmpty)
                return null;

            var corpse = new Corpse(levelId, groundPosition, Equipment);
            corpses.Add(corpse);
            Equipment = EquipmentState.Empty;
            Changed?.Invoke();
            return corpse;
        }

        /// <summary>
        /// Picks up a corpse's gear. It is equipped when the character wears nothing or something worse; otherwise it
        /// goes to the backpack. Nothing is ever destroyed: when the backpack has no room for what has to move
        /// there, the corpse stays where it is and this returns false.
        /// </summary>
        public bool Retrieve(Corpse corpse)
        {
            if (!corpses.Contains(corpse))
                return false;

            var weapon = corpse.Gear.Weapon;
            if (weapon != null)
            {
                if (Equipment.IsEmpty)
                {
                    Equipment = corpse.Gear;
                }
                else if (weapon.WeaponAverageDamage > Equipment.Weapon.WeaponAverageDamage)
                {
                    // The corpse's weapon is better: it is equipped and the one being worn moves to the backpack.
                    if (!Inventory.TryAdd(Equipment.Weapon))
                        return false;
                    Equipment = corpse.Gear;
                }
                else if (!Inventory.TryAdd(weapon))
                {
                    return false;
                }
            }

            corpses.Remove(corpse);
            Changed?.Invoke();
            return true;
        }
    }
}
