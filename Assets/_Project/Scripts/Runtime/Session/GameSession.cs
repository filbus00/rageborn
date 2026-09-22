using System;
using System.Collections.Generic;
using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// What the character has equipped, one item per <see cref="ItemSlot"/>. A slot with nothing in it is null.
    /// Immutable: <see cref="With"/> returns a changed copy, so it is cheap to hand around and to snapshot into a
    /// <see cref="Corpse"/>.
    /// </summary>
    public readonly struct EquipmentState
    {
        static readonly int SlotCount = Enum.GetValues(typeof(ItemSlot)).Length;

        readonly Item[] items;

        public EquipmentState(Item[] items) => this.items = items;

        /// <summary>Equips a single item into its own slot, everything else empty. Convenient for a fresh loadout or a test.</summary>
        public EquipmentState(Item item) : this(item == null ? null : OneSlot(item))
        {
        }

        static Item[] OneSlot(Item item)
        {
            var array = new Item[SlotCount];
            array[(int)item.Slot] = item;
            return array;
        }

        public Item Get(ItemSlot slot) => items != null ? items[(int)slot] : null;

        /// <summary>The equipped weapon, or null when unarmed. Unarmed still fights, at the weapon curve's item level 0.</summary>
        public Item Weapon => Get(ItemSlot.Weapon);

        public Item Chest => Get(ItemSlot.Chest);

        public Item Helm => Get(ItemSlot.Helm);

        /// <summary>Whether every slot is empty.</summary>
        public bool IsEmpty
        {
            get
            {
                if (items == null)
                    return true;
                for (var i = 0; i < items.Length; i++)
                    if (items[i] != null)
                        return false;
                return true;
            }
        }

        /// <summary>Returns a copy with one slot changed. Pass null to empty that slot.</summary>
        public EquipmentState With(ItemSlot slot, Item item)
        {
            var copy = new Item[SlotCount];
            if (items != null)
                Array.Copy(items, copy, SlotCount);
            copy[(int)slot] = item;
            return new EquipmentState(copy);
        }

        /// <summary>
        /// Average damage of what the character fights with. Unarmed is the weapon curve at item level 0, a little under
        /// the level 1 starting weapon.
        /// </summary>
        public float WeaponDamage => Weapon != null ? Weapon.WeaponAverageDamage : CombatFormulas.WeaponAverageDamage(0);

        /// <summary>Sum of a stat across every equipped item. Used for every affix-derived bonus, so a future slot or
        /// affix that also grants the stat just adds in.</summary>
        public float Sum(Func<Item, float> stat)
        {
            if (items == null)
                return 0f;
            var total = 0f;
            for (var i = 0; i < items.Length; i++)
                if (items[i] != null)
                    total += stat(items[i]);
            return total;
        }

        public float TotalArmor => Sum(item => item.ArmorValue + item.ArmorAffixBonus);
        public float TotalLifeBonus => Sum(item => item.LifeBonus);
        public float FlatWeaponDamageBonus => Sum(item => item.FlatWeaponDamageBonus);
        public float IncreasedDamagePercent => Sum(item => item.IncreasedDamagePercent);
        public float AttackSpeedPercent => Sum(item => item.AttackSpeedPercent);
        public float CriticalChancePercent => Sum(item => item.CriticalChancePercent);
        public float CriticalDamagePercent => Sum(item => item.CriticalDamagePercent);
        public float LifeOnHit => Sum(item => item.LifeOnHit);
        public float CooldownReductionPercent => Sum(item => item.CooldownReductionPercent);

        public static EquipmentState Empty => new EquipmentState((Item[])null);

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
        static readonly ItemSlot[] AllSlots = (ItemSlot[])Enum.GetValues(typeof(ItemSlot));

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

        /// <summary>Raised when equipment, the backpack or gold changes. The HUD and the inventory screen listen to it.</summary>
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

        /// <summary>Equips gear wholesale, replacing what the character wears. Used to set up a starting loadout or
        /// in tests; the inventory screen uses <see cref="EquipFromInventory"/> instead, which keeps the backpack honest.</summary>
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
        /// Nothing is auto-equipped any more: the player chooses from the inventory screen with
        /// <see cref="EquipFromInventory"/>.
        /// </summary>
        public bool PickUp(Item item)
        {
            if (!Inventory.TryAdd(item))
                return false;

            Changed?.Invoke();
            return true;
        }

        /// <summary>
        /// Equips an item from the backpack into its slot, moving whatever was worn there back to the backpack.
        /// Returns false, changing nothing, when the backpack has no room for the item being swapped out.
        /// </summary>
        public bool EquipFromInventory(Item item)
        {
            if (item == null || !Inventory.Contains(item))
                return false;

            var current = Equipment.Get(item.Slot);
            Inventory.Remove(item);
            if (current != null && !Inventory.TryAdd(current))
            {
                // No room for the item being swapped out: put the new one back and change nothing.
                Inventory.TryAdd(item);
                return false;
            }

            Equipment = Equipment.With(item.Slot, item);
            Changed?.Invoke();
            return true;
        }

        /// <summary>Unequips a slot back to the backpack. Returns false, changing nothing, when the slot is already
        /// empty or the backpack is full.</summary>
        public bool Unequip(ItemSlot slot)
        {
            var current = Equipment.Get(slot);
            if (current == null || !Inventory.TryAdd(current))
                return false;

            Equipment = Equipment.With(slot, null);
            Changed?.Invoke();
            return true;
        }

        /// <summary>
        /// Removes an item from the backpack for good. Stands in for salvage until crafting materials exist
        /// (Docs/03-itemization.md); it returns nothing yet. Returns false when the item was not in the backpack.
        /// </summary>
        public bool Discard(Item item)
        {
            if (item == null || !Inventory.Remove(item))
                return false;

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
        /// Picks up a corpse's gear, slot by slot. Each piece is equipped when the character has nothing or something
        /// worse there; otherwise it goes to the backpack. This is an all-or-nothing transaction: when the backpack
        /// cannot hold everything that has to move there, nothing happens and the corpse stays where it is.
        /// </summary>
        public bool Retrieve(Corpse corpse)
        {
            if (!corpses.Contains(corpse))
                return false;

            var newEquipment = Equipment;
            var toBag = new List<Item>();

            foreach (var slot in AllSlots)
            {
                var found = corpse.Gear.Get(slot);
                if (found == null)
                    continue;

                var worn = newEquipment.Get(slot);
                if (worn == null || IsUpgrade(found, worn))
                {
                    if (worn != null)
                        toBag.Add(worn);
                    newEquipment = newEquipment.With(slot, found);
                }
                else
                {
                    toBag.Add(found);
                }
            }

            if (Inventory.Count + toBag.Count > Inventory.Capacity)
                return false;

            foreach (var item in toBag)
                Inventory.TryAdd(item);
            Equipment = newEquipment;
            corpses.Remove(corpse);
            Changed?.Invoke();
            return true;
        }

        /// <summary>A rough, slot-agnostic "is this better" used only to decide what to re-equip on retrieval: higher
        /// item level wins, rarity breaks a tie. Not the docs' power score (Docs/03-itemization.md), which needs a
        /// full damage-per-second and effective-life model this project does not have yet.</summary>
        static bool IsUpgrade(Item candidate, Item current) =>
            candidate.ItemLevel != current.ItemLevel ? candidate.ItemLevel > current.ItemLevel : candidate.Rarity > current.Rarity;
    }
}
