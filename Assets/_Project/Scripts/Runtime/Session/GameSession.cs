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

        // Tolerates a shorter array (one made before a slot was added).
        public Item Get(ItemSlot slot) => items != null && (int)slot < items.Length ? items[(int)slot] : null;

        /// <summary>The equipped weapon, or null when unarmed. Unarmed still fights, at the weapon curve's item level 0.</summary>
        public Item Weapon => Get(ItemSlot.Weapon);

        public Item Chest => Get(ItemSlot.Chest);

        public Item Helm => Get(ItemSlot.Helm);

        static readonly ItemSlot[] RingPlaces = { ItemSlot.Ring, ItemSlot.Ring2 };
        static readonly ItemSlot[] OffHandPlace = { ItemSlot.OffHand };
        static readonly ItemSlot[] WeaponPlace = { ItemSlot.Weapon };
        static readonly ItemSlot[][] SinglePlaces = BuildSinglePlaces();

        static ItemSlot[][] BuildSinglePlaces()
        {
            var places = new ItemSlot[SlotCount][];
            for (var i = 0; i < SlotCount; i++)
                places[i] = new[] { (ItemSlot)i };
            return places;
        }

        /// <summary>Where an item of this kind can be worn: a ring on either hand, either bow in the weapon place, a
        /// quiver in the off-hand, everything else in its own slot (bows only, Docs/03, 2026-09-30).</summary>
        public static ItemSlot[] PlacesFor(ItemSlot kind) => kind switch
        {
            ItemSlot.Ring or ItemSlot.Ring2 => RingPlaces,
            ItemSlot.Weapon or ItemSlot.TwoHandWeapon => WeaponPlace,
            ItemSlot.Shield or ItemSlot.OffHand => OffHandPlace,
            _ => SinglePlaces[(int)kind],
        };

        public Item OffHand => Get(ItemSlot.OffHand);

        /// <summary>A longbow is worn (the old two-handed weapon kind).</summary>
        public bool IsTwoHanded => Weapon != null && Weapon.Slot == ItemSlot.TwoHandWeapon;

        /// <summary>A longbow is worn.</summary>
        public bool IsLongbow => IsTwoHanded;

        /// <summary>Bows only: nothing is ever dual wielded. Kept false for the code that still asks.</summary>
        public bool IsDualWield => false;

        /// <summary>A quiver is worn in the off-hand (the old shield kind).</summary>
        public bool HasShield => OffHand != null && OffHand.Slot == ItemSlot.Shield;

        public bool HasQuiver => HasShield;

        /// <summary>Nothing blocks with bows only (the shield's block is retired).</summary>
        public float BlockChance => 0f;

        /// <summary>The weapon's damage; there is no off-hand weapon with bows.</summary>
        public float OffHandWeaponDamage => WeaponDamage;

        /// <summary>The attack speed the quiver adds as a fraction, and the factor the bow multiplies by (a longbow).</summary>
        public float GripAttackSpeedBonus => HasQuiver ? OffHand.QuiverAttackSpeedPercent / 100f : 0f;

        public float GripAttackSpeedFactor => IsLongbow ? GripRules.LongbowAttackSpeedFactor : 1f;

        /// <summary>How much further the basic arrow reaches than a short bow's 7.5 (a longbow's 9).</summary>
        public float GripReach => IsLongbow ? GripRules.LongbowReach : 0f;

        /// <summary>
        /// Wears an item in a place. With bows only there are no grip rules left to apply (a bow and a quiver always
        /// go together); what comes off is added to <paramref name="displaced"/>.
        /// </summary>
        public EquipmentState Equip(ItemSlot place, Item item, List<Item> displaced = null)
        {
            var result = this;
            void TakeOff(ItemSlot at)
            {
                var worn = result.Get(at);
                if (worn == null)
                    return;
                displaced?.Add(worn);
                result = result.With(at, null);
            }

            TakeOff(place);
            return result.With(place, item);
        }

        /// <summary>Where this exact item is worn, or null when it is not.</summary>
        public ItemSlot? PlaceOf(Item item)
        {
            if (items == null || item == null)
                return null;
            for (var i = 0; i < items.Length; i++)
                if (items[i] == item)
                    return (ItemSlot)i;
            return null;
        }

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
                Array.Copy(items, copy, Math.Min(items.Length, SlotCount));
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
        public float MovementSpeedPercent => Sum(item => item.MovementSpeedPercent);
        public float DodgePercent => Sum(item => item.DodgePercent);

        /// <summary>The quiver's chance, in percent, that a basic shot looses a second arrow (Docs/03, 2026-09-30).</summary>
        public float ExtraArrowPercent => Sum(item => item.ExtraArrowPercent);

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
    /// which enemies were killed, which chests were opened, life, level and XP, potion charges, the dungeon's seed and
    /// the loot roller. Pure, so it can be tested; <see cref="Current"/> holds
    /// the live one. A future sleep mechanic resets the session (Docs/08-production.md, open question 5).
    /// <see cref="SaveCodec"/> writes it to disk and reads it back, and <see cref="SaveDirector"/> decides when.
    /// </summary>
    public sealed class GameSession
    {
        static readonly ItemSlot[] AllSlots = (ItemSlot[])Enum.GetValues(typeof(ItemSlot));

        readonly List<Corpse> corpses = new List<Corpse>();
        readonly Dictionary<string, HashSet<int>> killed = new Dictionary<string, HashSet<int>>();
        readonly HashSet<string> openedChests = new HashSet<string>();
        readonly Dictionary<string, bool[]> explored = new Dictionary<string, bool[]>();
        readonly int[] materials = new int[Enum.GetValues(typeof(CraftingMaterial)).Length];
        readonly SortedSet<int> waypoints = new SortedSet<int>();
        Onboarding onboarding;

        static GameSession current = new GameSession(Environment.TickCount);

        public GameSession() : this(0)
        {
        }

        /// <param name="lootSeed">Seeds the loot rolls, so a session can be replayed.</param>
        /// <param name="killsSinceLegendary">The bad luck counter to carry on from, when restoring a save.</param>
        public GameSession(int lootSeed, int killsSinceLegendary = 0)
        {
            Loot = new LootRoller(lootSeed, killsSinceLegendary);
            ForgeRandom = new System.Random(DungeonRules.LevelSeed(lootSeed, -1));
            Equipment = EquipmentState.Starting;
            SetPotion(new AutoPotion());
            SetOnboarding(new Onboarding());
            Loadout = new SkillLoadout();
            Loadout.Changed += RaiseModified;
            SkillLevels = new SkillLevels();
            SkillLevels.Changed += RaiseModified;
            PassiveTree = new PassiveTree();
            PassiveTree.Changed += RaiseModified;
            // Stat points change max life, armor and the rest, so they raise Changed (which raises Modified) too.
            Attributes = new AttributePoints();
            Attributes.Changed += NotifyChanged;
            Pets = new PetState();
            Pets.Changed += NotifyChanged;

            // Its own number, not the loot seed itself, so the dungeon and the drops do not move in step.
            DungeonSeed = DungeonRules.LevelSeed(lootSeed, 0);
        }

        /// <summary>Seeds every dungeon level of this game session (<see cref="DungeonRules.LevelSeed"/>), so each keeps
        /// its layout when the player leaves and comes back. Saved. The future sleep mechanic rolls a new one.</summary>
        public int DungeonSeed { get; internal set; }

        /// <summary>Where the next scene load is headed, set by a stairway before it loads: which dungeon depth, and
        /// which stairs the player should arrive by. Not saved: a loaded game starts in town.</summary>
        public LevelTravel Travel { get; set; }

        /// <summary>The <see cref="DungeonGenerator.Version"/> the recorded dungeon kills and chests belong to.</summary>
        public int DungeonVersion { get; internal set; } = DungeonGenerator.Version;

        /// <summary>
        /// Forgets every killed pack member and opened chest on a generated level, for when the generator changed and
        /// those records would land on different packs and chests. Hand-built scenes keep theirs. Corpses are kept;
        /// <see cref="MoveCorpse"/> puts one back on floor if its spot is now a wall. Returns how many packs and chests
        /// it forgot.
        /// </summary>
        public int ForgetDungeonLevels()
        {
            var packs = new List<string>();
            foreach (var key in killed.Keys)
                if (DungeonRules.IsDungeonKey(key))
                    packs.Add(key);
            foreach (var key in packs)
                killed.Remove(key);
            var forgotten = packs.Count + openedChests.RemoveWhere(DungeonRules.IsDungeonKey);
            if (forgotten > 0)
                Modified?.Invoke();
            return forgotten;
        }

        /// <summary>The live session. Replaced at the start of every play, so nothing leaks between editor plays,
        /// and then by the loaded save when there is one (<see cref="Install"/>).</summary>
        public static GameSession Current => current;

        /// <summary>Makes a session the live one. Only for startup, before any scene object has read
        /// <see cref="Current"/>: objects that cached the old one would keep listening to it.</summary>
        public static void Install(GameSession session) => current = session ?? throw new ArgumentNullException(nameof(session));

        /// <summary>Raised when equipment, the backpack or gold changes. The HUD and the inventory screen listen to it.</summary>
        public event Action Changed;

        /// <summary>Raised on every change a save must capture: everything <see cref="Changed"/> covers, plus kills and
        /// deaths. Life is not included, it changes every hit; the save picks it up when it is next written.</summary>
        public event Action Modified;

        public EquipmentState Equipment { get; private set; }

        public Inventory Inventory { get; } = new Inventory();

        public int Gold { get; private set; }

        /// <summary>Rolls the Forge's reforges and rerolls. Like the loot roller, seeded from the session and not saved.</summary>
        public System.Random ForgeRandom { get; }

        /// <summary>How much of a salvage material the character holds (Docs/04: all materials stack without limit).</summary>
        public int Materials(CraftingMaterial material) => materials[(int)material];

        public void AddMaterial(CraftingMaterial material, int amount)
        {
            if (amount <= 0)
                return;

            materials[(int)material] += amount;
            NotifyChanged();
        }

        public LootRoller Loot { get; }

        public CharacterProgress Progress { get; private set; } = new CharacterProgress();

        /// <summary>The depths whose waypoint the character has stepped on (Docs/05), in order. Saved.</summary>
        public IReadOnlyCollection<int> Waypoints => waypoints;

        public bool HasWaypoint(int depth) => waypoints.Contains(depth);

        /// <summary>Activates a depth's waypoint. Returns true the first time.</summary>
        public bool ActivateWaypoint(int depth)
        {
            if (depth < 1 || !waypoints.Add(depth))
                return false;
            Modified?.Invoke();
            return true;
        }

        /// <summary>Whether the character owns the Portal Tome (Docs/05: permanent, free to use from the UI). Saved.</summary>
        public bool HasPortalTome { get; private set; }

        public void GivePortalTome()
        {
            if (HasPortalTome)
                return;
            HasPortalTome = true;
            NotifyChanged();
        }

        /// <summary>The depth an open town portal stands on, 0 when none is open. One portal at a time. Saved.</summary>
        public int PortalDepth { get; private set; }

        /// <summary>Where on its level the open portal stands, in ground units.</summary>
        public Vector2 PortalPosition { get; private set; }

        /// <summary>Opens a portal on a depth, replacing any other: Docs/05, it stays open so the player can come back.</summary>
        public void OpenPortal(int depth, Vector2 groundPosition)
        {
            PortalDepth = depth;
            PortalPosition = groundPosition;
            Modified?.Invoke();
        }

        /// <summary>Closes the portal, when the player has come back through it (as in Diablo 1).</summary>
        public void ClosePortal()
        {
            if (PortalDepth == 0)
                return;
            PortalDepth = 0;
            Modified?.Invoke();
        }

        /// <summary>What the character has been taught (Docs/06, onboarding). Its changes raise <see cref="Modified"/>.</summary>
        public Onboarding Onboarding => onboarding;

        /// <summary>Raised with an item after it went into the backpack. Onboarding hints listen to it.</summary>
        public event Action<Item> PickedUp;

        /// <summary>Puts back the onboarding state read from a save.</summary>
        internal void RestoreOnboarding(Onboarding restored) => SetOnboarding(restored);

        void SetOnboarding(Onboarding value)
        {
            if (onboarding != null)
                onboarding.Changed -= RaiseModified;
            onboarding = value;
            onboarding.Changed += RaiseModified;
        }

        /// <summary>The auto-potion's charges and kill progress. Its changes raise <see cref="Modified"/>.</summary>
        public AutoPotion Potion { get; private set; }

        /// <summary>The four skill slots and their triggers (Docs/01, Docs/02). Its changes raise <see cref="Modified"/>.</summary>
        public SkillLoadout Loadout { get; }

        /// <summary>Skill points spent and each skill's level (Docs/04, Docs/02). Its changes raise <see cref="Modified"/>.</summary>
        public SkillLevels SkillLevels { get; }

        /// <summary>The retired Wrathborn's passive tree (Docs/02). The Wild Arrow has none (decided 2026-09-30), so
        /// nothing can be bought and its bonuses stay zero; it is kept until the code reading it is cleared out.</summary>
        public PassiveTree PassiveTree { get; }

        /// <summary>The stat points spent on the five attributes (Docs/02, decided 2026-09-30). Its changes raise
        /// <see cref="Changed"/>.</summary>
        public AttributePoints Attributes { get; }

        /// <summary>What the attributes give now.</summary>
        public CharacterAttributes AttributeBonuses => CharacterAttributes.Of(Attributes);

        /// <summary>The pets bought at the Pet Vendor, the active one and its rules (Docs/02, decided 2026-09-30). Its
        /// changes raise <see cref="Changed"/>.</summary>
        public PetState Pets { get; }

        public int Level => Progress.Level;

        /// <summary>Raised with the new level when the character levels up, after <see cref="Changed"/>.</summary>
        public event Action<int> LeveledUp;

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
            NotifyChanged();
        }

        /// <summary>Buys a pet at the Pet Vendor (Docs/02, Pets): pays its price in gold, and it follows the character
        /// at once. False, and nothing changes, when it is owned already or the gold is short.</summary>
        public bool BuyPet(PetKind kind)
        {
            var price = PetRules.Get(kind).Price;
            if (Pets.Owns(kind) || Gold < price)
                return false;
            Gold -= price;
            Pets.AddOwned(kind);
            Pets.SetActive(kind);
            NotifyChanged();
            return true;
        }

        public void AddGold(int amount)
        {
            if (amount <= 0)
                return;

            Gold += amount;
            NotifyChanged();
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

            NotifyChanged();
            PickedUp?.Invoke(item);
            return true;
        }

        /// <summary>
        /// Equips an item from the backpack into its slot, moving whatever was worn there back to the backpack. A ring
        /// goes on an empty hand, else replaces whichever ring it beats by more (<see cref="PowerScore.PlaceFor"/>).
        /// Returns false, changing nothing, when the backpack has no room for the item being swapped out.
        /// </summary>
        public bool EquipFromInventory(Item item)
        {
            if (item == null || !Inventory.Contains(item))
                return false;

            // Under the grip rules a two-hander can push out both hands' items (Docs/03), so check room for all of them.
            var place = PowerScore.PlaceFor(Equipment, item, Level, PassiveTree.Bonuses);
            var displaced = new List<Item>();
            var equipped = Equipment.Equip(place, item, displaced);
            if (Inventory.Count - 1 + displaced.Count > Inventory.Capacity)
                return false;

            Inventory.Remove(item);
            foreach (var worn in displaced)
                Inventory.TryAdd(worn);
            Equipment = equipped;
            NotifyChanged();
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
            NotifyChanged();
            return true;
        }

        /// <summary>
        /// Removes an item from the backpack for good, for nothing. Salvaging for materials happens only at the Forge in
        /// town (the user's decision, 2026-09-26), so this is what the dungeon offers. Returns false when the item was
        /// not in the backpack.
        /// </summary>
        public bool Discard(Item item)
        {
            if (item == null || !Inventory.Remove(item))
                return false;

            NotifyChanged();
            return true;
        }

        /// <summary>
        /// Salvages a backpack item at the Forge: it is gone and its materials are added
        /// (<see cref="ForgeRules.SalvageYield"/>). Equipped items are not salvaged. Returns false when the item was not
        /// in the backpack.
        /// </summary>
        public bool Salvage(Item item)
        {
            if (item == null || !Inventory.Remove(item))
                return false;

            var (material, amount) = ForgeRules.SalvageYield(item);
            materials[(int)material] += amount;
            NotifyChanged();
            return true;
        }

        public bool CanAfford(ForgeCost cost) => Gold >= cost.Gold && Materials(cost.Material) >= cost.Amount;

        /// <summary>
        /// Pays for a Forge action and puts its result where the original was, in the backpack or worn. Returns false,
        /// changing nothing, when the original is neither or the cost cannot be paid.
        /// </summary>
        public bool ApplyForge(Item original, Item result, ForgeCost cost)
        {
            if (original == null || result == null || result.Slot != original.Slot || !CanAfford(cost))
                return false;

            if (!Inventory.Replace(original, result))
            {
                var place = Equipment.PlaceOf(original);
                if (place == null)
                    return false;
                Equipment = Equipment.With(place.Value, result);
            }

            Gold -= cost.Gold;
            materials[(int)cost.Material] -= cost.Amount;
            NotifyChanged();
            return true;
        }

        /// <summary>Puts back the materials read from a save.</summary>
        internal void RestoreMaterial(CraftingMaterial material, int amount) => materials[(int)material] = Math.Max(0, amount);

        /// <summary>
        /// Adds XP. A level up refills life (the user's decision, 2026-09-23, as in Diablo 1), raises
        /// <see cref="Changed"/> (maximum life depends on level) and then <see cref="LeveledUp"/>. Returns how many
        /// levels were gained.
        /// </summary>
        public int GrantExperience(int amount)
        {
            var before = Progress.Xp;
            var gained = Progress.Add(amount);
            if (gained > 0)
            {
                LifeFraction = 1f;
                NotifyChanged();
                LeveledUp?.Invoke(Level);
            }
            else if (Progress.Xp != before)
            {
                Modified?.Invoke();
            }
            return gained;
        }

        /// <summary>Puts back the potion charges read from a save.</summary>
        internal void RestorePotion(int charges, int killProgress) => SetPotion(new AutoPotion(charges, killProgress));

        void SetPotion(AutoPotion potion)
        {
            if (Potion != null)
                Potion.Changed -= RaiseModified;
            Potion = potion;
            Potion.Changed += RaiseModified;
        }

        void RaiseModified() => Modified?.Invoke();

        /// <summary>Puts back the level and XP read from a save.</summary>
        internal void RestoreProgress(int level, int xp) => Progress = new CharacterProgress(level, xp);

        /// <summary>Records that a pack's member was killed. It stays dead while the session lasts.</summary>
        public void RecordKill(string packKey, int slot)
        {
            if (!killed.TryGetValue(packKey, out var slots))
            {
                slots = new HashSet<int>();
                killed[packKey] = slots;
            }
            if (slots.Add(slot))
                Modified?.Invoke();
        }

        public bool IsKilled(string packKey, int slot) => killed.TryGetValue(packKey, out var slots) && slots.Contains(slot);

        /// <summary>The keys of every pack with at least one member killed, for saving.</summary>
        public IEnumerable<string> KilledPackKeys => killed.Keys;

        /// <summary>The killed member slots of one pack, for saving. Empty for a pack with no kills.</summary>
        public IEnumerable<int> KilledSlots(string packKey) =>
            killed.TryGetValue(packKey, out var slots) ? slots : (IEnumerable<int>)Array.Empty<int>();

        /// <summary>
        /// The mini-map's explored flags for a level (<see cref="MinimapReveal"/>), kept while the session lasts so a
        /// level's map is still drawn on coming back. Not saved: a loaded game starts with every map blank. A level
        /// whose size changed (a new generator) starts over.
        /// </summary>
        public bool[] ExploredCells(string levelId, int cellCount)
        {
            if (!explored.TryGetValue(levelId, out var cells) || cells.Length != cellCount)
            {
                cells = new bool[cellCount];
                explored[levelId] = cells;
            }
            return cells;
        }

        /// <summary>Records that a chest was opened. It stays open while the session lasts.</summary>
        public void RecordOpened(string chestKey)
        {
            if (openedChests.Add(chestKey))
                Modified?.Invoke();
        }

        public bool IsOpened(string chestKey) => openedChests.Contains(chestKey);

        public IEnumerable<string> OpenedChests => openedChests;

        /// <summary>Moves a corpse to another spot on its level, gear untouched: used when the spot it fell on is no
        /// longer floor, so a corpse run can never become impossible.</summary>
        public Corpse MoveCorpse(Corpse corpse, Vector2 groundPosition)
        {
            var index = corpses.IndexOf(corpse);
            if (index < 0)
                return corpse;

            var moved = new Corpse(corpse.LevelId, groundPosition, corpse.Gear);
            corpses[index] = moved;
            Modified?.Invoke();
            return moved;
        }

        /// <summary>Puts back a corpse read from a save.</summary>
        internal void RestoreCorpse(Corpse corpse) => corpses.Add(corpse);

        /// <summary>
        /// The character dies: its equipped gear stays behind as a corpse where it fell (Docs/01-core-gameplay.md).
        /// Returns the new corpse, or null when nothing was equipped, since a corpse would hold nothing.
        /// The backpack and gold are kept. An earlier corpse is never touched, and corpses never expire.
        /// </summary>
        public Corpse Die(string levelId, Vector2 groundPosition)
        {
            LifeFraction = 1f;
            if (Equipment.IsEmpty)
            {
                Modified?.Invoke();
                return null;
            }

            var corpse = new Corpse(levelId, groundPosition, Equipment);
            corpses.Add(corpse);
            Equipment = EquipmentState.Empty;
            NotifyChanged();
            return corpse;
        }

        /// <summary>
        /// Picks up a corpse's gear, slot by slot. Each piece is equipped when the character has nothing there or it
        /// raises the power score (<see cref="PowerScore"/>); otherwise it goes to the backpack. This is an all-or-nothing transaction: when the backpack
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

                // Compared in the place it was worn, so the corpse's second ring is weighed against the second hand.
                var worn = newEquipment.Get(slot);
                if (worn == null || PowerScore.ChangeAt(newEquipment, slot, found, Level, PassiveTree.Bonuses) > 0f)
                    newEquipment = newEquipment.Equip(slot, found, toBag);
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
            NotifyChanged();
            return true;
        }

        void NotifyChanged()
        {
            Changed?.Invoke();
            Modified?.Invoke();
        }
    }
}
