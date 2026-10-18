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
        public float IgniteChancePercent => Sum(item => item.IgniteChancePercent);
        public float ChillChancePercent => Sum(item => item.ChillChancePercent);

        /// <summary>Any affix summed over everything worn, in its own unit (the Wild Arrow's affixes 90 to 100 are read
        /// this way where they land).</summary>
        public float AffixTotal(AffixId id) => Sum(item => item.AffixSum(id));

        /// <summary>Whether a named legendary is worn, for its power (Docs/03, 2026-10-01).</summary>
        public bool Wears(LegendaryId legendary)
        {
            if (items == null || legendary == LegendaryId.None)
                return false;
            for (var i = 0; i < items.Length; i++)
                if (items[i] != null && items[i].Legendary == legendary)
                    return true;
            return false;
        }

        public static EquipmentState Empty => new EquipmentState((Item[])null);

        /// <summary>The gear a new character starts with (the owner, 2026-10-01): a Common level 1 short bow (the hunting
        /// bow's look) and a Common level 1 quiver (the hide quiver's, +5 percent attack speed), so the first Magic drop is
        /// already an upgrade and the off-hand is seen to hold a quiver from the start.</summary>
        public static EquipmentState Starting => new EquipmentState(new Item(ItemSlot.Weapon, ItemRarity.Common, 1))
            .With(ItemSlot.OffHand, new Item(ItemSlot.Shield, ItemRarity.Common, 1));
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
            // Smart drops: legendaries lean toward places with no legendary worn yet.
            Loot.Worn = () => Equipment;
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
            Talents = new TalentState();
            Talents.Changed += RaiseModified;
            Quests = new QuestState();
            Quests.Changed += RaiseModified;
            // The roguelite (2026-10-13): extra potion charges are bought in town.
            Meta = new MetaProgress();
            Talents.Meta = Meta;
            Meta.Changed += OnMetaChanged;
            // Stat points change max life, armor and the rest, so they raise Changed (which raises Modified) too.
            Attributes = new AttributePoints();
            Attributes.Changed += NotifyChanged;
            Pets = new PetState();
            Pets.Changed += NotifyChanged;
            Potion.ExtraCharges = Meta.ExtraPotionCharges;
            if (Features.Roguelite)
            {
                Loot.DropScale = RogueliteDropScale;
                Potion.UseRogueliteRules();
            }

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

        /// <summary>The Wild Arrow's three talent trees (2026-10-07): the skills she has learned and the passive bonuses
        /// every system reads (the retired Wrathborn's <see cref="PassiveTree"/> stays, hidden and empty).</summary>
        public TalentState Talents { get; }

        /// <summary>What the merchant pays for an item: its price, and a quarter more once Tobin minds the stall.</summary>
        public int SellPrice(Item item) =>
            Mathf.RoundToInt(SellRules.Price(item) * Quests.Boons.SellPrice * (Features.Roguelite ? SellRules.RogueliteFactor : 1f));

        /// <summary>The quests taken and their steps (2026-10-08, Docs/05 Quests).</summary>
        public QuestState Quests { get; }

        /// <summary>The stat points spent on the five attributes (Docs/02, decided 2026-09-30). Its changes raise
        /// <see cref="Changed"/>.</summary>
        public AttributePoints Attributes { get; }

        /// <summary>What the attributes give now.</summary>
        public CharacterAttributes AttributeBonuses => CharacterAttributes.Of(Attributes);

        /// <summary>The pets bought at the Pet Vendor, the active one and its rules (Docs/02, decided 2026-09-30). Its
        /// changes raise <see cref="Changed"/>.</summary>
        public PetState Pets { get; private set; }

        // --- The roguelite (2026-10-13) -------------------------------------------------------------------------------

        /// <summary>What survives every run (the town's upgrades). Saved.</summary>
        public MetaProgress Meta { get; private set; }

        /// <summary>Boon choices owed: one for each new depth reached this run, taken in the dungeon. Saved.</summary>
        public int PendingBoons { get; internal set; }

        /// <summary>Rerolls of the boon choice left this run. Saved.</summary>
        public int RerollsLeft { get; internal set; }

        /// <summary>Whether this run's one item has gone to the stash. Saved.</summary>
        public bool SentToStash { get; internal set; }

        /// <summary>Whether this run has begun (she has gone down the stairs). Saved.</summary>
        public bool RunStarted { get; internal set; }

        void OnMetaChanged()
        {
            Talents.Refresh();
            Potion.ExtraCharges = Meta.ExtraPotionCharges;
            NotifyChanged();
        }

        /// <summary>Puts back the meta read from a save.</summary>
        internal void RestoreMeta(MetaProgress meta)
        {
            Meta.Changed -= OnMetaChanged;
            Meta = meta;
            Talents.Meta = meta;
            Meta.Changed += OnMetaChanged;
            Potion.ExtraCharges = Meta.ExtraPotionCharges;
        }

        /// <summary>Buys the next rank of a town upgrade with the coins. False when it is maxed or the coins are short.</summary>
        public bool BuyUpgrade(MetaUpgradeId id)
        {
            if (Meta.IsMaxed(id))
                return false;
            var cost = Meta.NextCost(id);
            if (Gold < cost)
                return false;
            Gold -= cost;
            Meta.Raise(id);
            // A better kit piece goes on now in town, but only over nothing or a plain kit piece, never over what she found.
            if (!RunStarted && MetaRules.Get(id)?.Shop == MetaShop.Outfitting)
                PutOnKitWhereBare();
            return true;
        }

        /// <summary>Takes a boon: a rank of a talent node, paying one pending choice.</summary>
        public bool TakeBoon(string id)
        {
            if (PendingBoons <= 0 || !Talents.Grant(id))
                return false;
            PendingBoons--;
            NotifyChanged();
            return true;
        }

        /// <summary>Spends a reroll of the boon choice. False when none are left.</summary>
        public bool UseReroll()
        {
            if (RerollsLeft <= 0)
                return false;
            RerollsLeft--;
            RaiseModified();
            return true;
        }

        /// <summary>Puts the kit's piece on in each place that is empty or holds a Common item (in town, after buying).</summary>
        void PutOnKitWhereBare()
        {
            var kit = StartingKit();
            var equipment = Equipment;
            foreach (ItemSlot place in System.Enum.GetValues(typeof(ItemSlot)))
            {
                var piece = kit.Get(place);
                var worn = equipment.Get(place);
                if (piece != null && (worn == null || (worn.Rarity == ItemRarity.Common && worn.Legendary == LegendaryId.None)))
                    equipment = equipment.With(place, piece);
            }
            Equip(equipment);
        }

        /// <summary>
        /// Thorin forges the item of a blueprint he knows (2026-10-15): Rare, at the look's level, for coins. It goes into
        /// the backpack and becomes that kind's kit piece, worn at the start of every run after a death. Null when he does
        /// not know it, the coins are short or the backpack has no room.
        /// </summary>
        public Item Forge(string key)
        {
            if (!Meta.Knows(key) || !BlueprintRules.TryParse(key, out var kind, out var look))
                return null;
            var cost = BlueprintRules.ForgeCost(look);
            if (Gold < cost)
                return null;
            var level = BlueprintRules.ForgeLevel(kind, look, DungeonRules.EnemyLevel(Mathf.Max(1, Meta.DeepestEver)));
            var item = Loot.RollItemOf(kind, ItemRarity.Rare, level);
            if (!Inventory.TryAdd(item))
                return null;
            Gold -= cost;
            Meta.SetForged(item);
            NotifyChanged();
            return item;
        }

        /// <summary>Teaches Thorin every blueprint in the backpack and takes them out (a run come home, 2026-10-15). Returns
        /// how many were new to him.</summary>
        public int LearnCarriedBlueprints()
        {
            var learned = 0;
            var carried = new List<Item>();
            foreach (var item in Inventory.Items)
                if (item.IsBlueprint)
                    carried.Add(item);
            foreach (var blueprint in carried)
            {
                if (Meta.Learn(BlueprintRules.Key(blueprint)))
                    learned++;
                Inventory.Remove(blueprint);
            }
            return learned;
        }

        /// <summary>The run's one item to the stash, from the backpack or worn. False when it was sent already, the stash
        /// has no free slot, or the item is not hers.</summary>
        public bool SendToStash(Item item)
        {
            if (SentToStash || item == null || stash.Count >= Meta.StashSlots)
                return false;
            if (!Inventory.Remove(item))
            {
                var place = Equipment.PlaceOf(item);
                if (place == null)
                    return false;
                Equipment = Equipment.With(place.Value, null);
            }
            stash.Add(item);
            SentToStash = true;
            NotifyChanged();
            return true;
        }

        /// <summary>The kit she starts a run with: a bow and a quiver, better as they are bought, and armour once bought.</summary>
        public EquipmentState StartingKit()
        {
            var bow = MetaRules.KitRarity(Meta.Rank(MetaUpgradeId.StartBow));
            var quiver = MetaRules.KitRarity(Meta.Rank(MetaUpgradeId.StartQuiver));
            var kit = EquipmentState.Starting;
            if (bow != ItemRarity.Common)
                kit = kit.With(ItemSlot.Weapon, Loot.RollItemOf(ItemSlot.Weapon, bow, 1));
            if (quiver != ItemRarity.Common)
                kit = kit.With(ItemSlot.OffHand, Loot.RollItemOf(ItemSlot.Shield, quiver, 1));
            var armour = Meta.Rank(MetaUpgradeId.StartArmour);
            if (armour > 0)
                kit = kit.With(ItemSlot.Chest, Loot.RollItemOf(ItemSlot.Chest, MetaRules.KitRarity(armour), 1));
            // Thorin's forged pieces in their places (2026-10-15).
            foreach (var pair in Meta.Forged)
                kit = kit.Equip(EquipmentState.PlacesFor(pair.Key)[0], pair.Value);
            return kit;
        }

        /// <summary>
        /// Ends this run and makes the next (the roguelite: at death or after the last boss): a fresh session with the
        /// coins, the town's upgrades, the pets, the stash, the Codex and the hints carried over, and the starting kit on.
        /// </summary>
        public GameSession NextRun(int lootSeed) => NextRun(lootSeed, false);

        /// <param name="keepCarried">Through the portal (2026-10-15): what she wears and carries comes home with her.
        /// Otherwise (a death) it is lost and she starts again in the kit.</param>
        public GameSession NextRun(int lootSeed, bool keepCarried)
        {
            var next = new GameSession(lootSeed);
            next.Gold = Gold;
            Meta.Runs++;
            next.RestoreMeta(Meta);
            Pets.Changed -= NotifyChanged;
            next.Pets = Pets;
            next.Pets.Changed += next.NotifyChanged;
            next.stash.AddRange(stash);
            foreach (var id in codex)
                next.codex.Add(id);
            next.SetOnboarding(onboarding);
            if (keepCarried)
            {
                next.Equipment = Equipment;
                foreach (var item in Inventory.Items)
                    next.Inventory.TryAdd(item);
            }
            else
                next.Equipment = next.StartingKit();
            next.LifeFraction = 1f;
            next.Potion.Refill();
            return next;
        }

        public int Level => Progress.Level;

        /// <summary>What this run has done (the run summary, 2026-10-15). Saved.</summary>
        public RunStats Run { get; } = new RunStats();

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

        readonly HashSet<LegendaryId> codex = new HashSet<LegendaryId>();

        /// <summary>The named legendaries this character has found (the Codex, Docs/03): once seen, a legendary's home
        /// is shown. Saved.</summary>
        public bool HasSeen(LegendaryId legendary) => codex.Contains(legendary);

        public IReadOnlyCollection<LegendaryId> Codex => codex;

        public void RecordLegendary(LegendaryId legendary)
        {
            if (legendary != LegendaryId.None)
                codex.Add(legendary);
        }

        internal List<string> CodexNames()
        {
            var names = new List<string>(codex.Count);
            foreach (var id in codex)
                names.Add(id.ToString());
            return names;
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

        /// <summary>Lyriel teaches the next rank of a pet's skill (2026-10-15) for coins. False when the pet is not hers, the
        /// skill is at its last rank or the coins are short.</summary>
        public bool TeachPetSkill(PetSkillId id)
        {
            var skill = PetSkillRules.Get(id);
            if (skill == null || !Pets.Owns(skill.Pet) || Pets.Rank(id) >= PetSkillRules.MaxRanks)
                return false;
            var cost = PetSkillRules.Cost(Pets.Rank(id));
            if (Gold < cost)
                return false;
            Gold -= cost;
            Pets.RaiseSkill(id);
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

        // --- The town's newcomers (2026-10-05) ----------------------------------------------------------------------

        /// <summary>The deepest dungeon level the character has stood on, 0 before the first. Newcomers arrive in town by
        /// it (<see cref="Newcomers"/>). Saved.</summary>
        public int DeepestDepth { get; private set; }

        /// <summary>Records standing on a depth. True when it is deeper than any before.</summary>
        public bool ReachDepth(int depth)
        {
            if (depth <= DeepestDepth)
                return false;
            if (DeepestDepth == 0)
            {
                RerollsLeft = Meta.RerollsPerRun;
                Run.RecordBefore = Meta.DeepestEver;
                // A run begun deeper (a deep entrance, 2026-10-17) is owed the boons of the depths it skipped.
                PendingBoons += depth - 1;
            }
            DeepestDepth = depth;
            RunStarted = true;
            // The roguelite (2026-10-13): a boon for every new depth, and she fights at the depth's level.
            PendingBoons++;
            Meta.RecordDepth(depth);
            var level = BoonRules.LevelAt(depth);
            if (level > Progress.Level)
                Progress = new CharacterProgress(level);
            NotifyChanged();
            return true;
        }

        // The roguelite (2026-10-13): everyone is in town from the start; they sell the town's upgrades.
        public bool HasArrived(Newcomer who) => Features.Roguelite || Newcomers.HasArrived(who, DeepestDepth);

        /// <summary>How many items the stash holds (a list, not a grid). Tuning.</summary>
        public const int StashCapacity = 60;

        readonly List<Item> stash = new List<Item>();

        /// <summary>The Stash Keeper's chest: gear kept in town, out of the backpack. Saved.</summary>
        public IReadOnlyList<Item> Stash => stash;

        /// <summary>Moves an item from the backpack into the stash. False when the stash is full or the item is not in the
        /// backpack.</summary>
        public bool StoreInStash(Item item)
        {
            if (stash.Count >= Meta.StashSlots || !Inventory.Remove(item))
                return false;
            stash.Add(item);
            NotifyChanged();
            return true;
        }

        /// <summary>Moves an item from the stash into the backpack. False when there is no room for it.</summary>
        public bool TakeFromStash(Item item)
        {
            if (!stash.Contains(item) || !Inventory.TryAdd(item))
                return false;
            stash.Remove(item);
            NotifyChanged();
            return true;
        }

        internal void RestoreStash(Item item) => stash.Add(item);

        /// <summary>The Gambler: pays the price and puts the rolled item of that kind in the backpack. False, with nothing
        /// paid, when the gold or the backpack's room is short.</summary>
        public bool Gamble(ItemSlot kind, System.Random random, out Item bought)
        {
            bought = null;
            var itemLevel = GambleRules.ItemLevel(DeepestDepth);
            var price = GambleRules.Price(kind, itemLevel);
            if (Gold < price)
                return false;
            var item = Loot.RollItemOf(kind, GambleRules.Rarity(random.NextDouble(), Quests.Boons.LegendaryGamble), itemLevel);
            if (!Inventory.TryAdd(item))
                return false;
            Gold -= price;
            RecordLegendary(item.Legendary);
            bought = item;
            NotifyChanged();
            PickedUp?.Invoke(item);
            return true;
        }

        /// <summary>The Trainer: every skill point back for the fee. False when there is nothing to reset or not the gold.</summary>
        public bool RespecSkills()
        {
            var price = RespecRules.SkillsPrice(Level);
            if (Gold < price || Talents.Spent == 0)
                return false;
            Gold -= price;
            Talents.ResetAll();
            NotifyChanged();
            return true;
        }

        /// <summary>The Trainer: every stat point back for the fee. False when there is nothing to reset or not the gold.</summary>
        public bool RespecStats()
        {
            var price = RespecRules.StatsPrice(Level);
            if (Gold < price || Attributes.TotalSpent == 0)
                return false;
            Gold -= price;
            Attributes.ResetAll();
            NotifyChanged();
            return true;
        }

        /// <summary>
        /// Puts a found item in the backpack. Returns false, leaving the item where it is, when there is no room for its
        /// size (<see cref="ItemSize"/>).
        /// Nothing is auto-equipped any more: the player chooses from the inventory screen with
        /// <see cref="EquipFromInventory"/>.
        /// </summary>
        public bool PickUp(Item item)
        {
            if (!Inventory.TryAdd(item))
                return false;

            RecordLegendary(item.Legendary);

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
            if (item == null || item.IsBlueprint || !Inventory.Contains(item))
                return false;
            return EquipFromInventory(item, PowerScore.PlaceFor(Equipment, item, Level, Talents.Bonuses));
        }

        /// <summary>Wears a backpack item in a chosen place, as when it is dragged onto a slot (the owner, 2026-10-03): a
        /// ring on either hand. False, changing nothing, when the item cannot be worn there
        /// (<see cref="EquipmentState.PlacesFor"/>), is not in the backpack, or what comes off has no room.</summary>
        public bool EquipFromInventory(Item item, ItemSlot place)
        {
            if (item == null || item.IsBlueprint || !Inventory.Contains(item) || System.Array.IndexOf(EquipmentState.PlacesFor(item.Slot), place) < 0)
                return false;

            // Under the grip rules a two-hander can push out both hands' items (Docs/03), so check room for all of them.
            var displaced = new List<Item>();
            var equipped = Equipment.Equip(place, item, displaced);
            // The backpack is a grid (Inventory): the item leaves and whatever comes off must fit in its place, all or nothing.
            if (!Inventory.TrySwap(item, displaced))
                return false;

            Equipment = equipped;
            NotifyChanged();
            return true;
        }

        /// <summary>Unequips a slot back to the backpack. Returns false, changing nothing, when the slot is already
        /// empty or the backpack has no room for it.</summary>
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

        /// <summary>Sells a backpack item to the merchant in town for <see cref="SellRules.Price"/>. Returns the gold
        /// paid, or 0 when the item was not in the backpack.</summary>
        public int Sell(Item item)
        {
            if (item == null || item.IsBlueprint || !Inventory.Remove(item))
                return 0;

            var price = SellPrice(item);
            Gold += price;
            NotifyChanged();
            return price;
        }

        /// <summary>The backpack items a sale of all of one rarity would take (<see cref="SellRules.InBulkSale"/>), into
        /// <paramref name="into"/>, which is cleared first. Returns the gold they would bring.</summary>
        public int BulkSale(ItemRarity rarity, List<Item> into)
        {
            into.Clear();
            var gold = 0;
            foreach (var item in Inventory.Items)
                if (SellRules.InBulkSale(item, rarity, PowerScore.IsUpgrade(Equipment, item, Level, Talents.Bonuses)))
                {
                    into.Add(item);
                    gold += SellPrice(item);
                }
            return gold;
        }

        /// <summary>Sells every backpack item of a rarity except upgrades and named legendaries, with one change event.
        /// Returns the gold paid.</summary>
        public int SellAll(ItemRarity rarity)
        {
            var sold = new List<Item>();
            BulkSale(rarity, sold);
            var gold = 0;
            foreach (var item in sold)
                if (Inventory.Remove(item))
                    gold += SellPrice(item);
            if (gold == 0)
                return 0;
            Gold += gold;
            NotifyChanged();
            return gold;
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
            // The roguelite (2026-10-13): no experience; her level follows the depth (ReachDepth).
            return 0;
#pragma warning disable CS0162

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
#pragma warning restore CS0162

        /// <summary>Puts back the potion charges read from a save.</summary>
        internal void RestorePotion(int charges, int killProgress)
        {
            SetPotion(new AutoPotion(charges, killProgress, Meta.ExtraPotionCharges));
            if (Features.Roguelite)
                Potion.UseRogueliteRules();
        }

        /// <summary>The roguelite's item drop chance, a tenth of Docs/03's (the owner, 2026-10-13).</summary>
        public const float RogueliteDropScale = 0.1f;

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
                if (worn == null || PowerScore.ChangeAt(newEquipment, slot, found, Level, Talents.Bonuses) > 0f)
                    newEquipment = newEquipment.Equip(slot, found, toBag);
                else
                {
                    toBag.Add(found);
                }
            }

            if (!Inventory.TrySwap(null, toBag))
                return false;

            Equipment = newEquipment;
            corpses.Remove(corpse);
            NotifyChanged();
            return true;
        }

        /// <summary>Moves a backpack item by hand (2026-10-18). False when those cells are not free.</summary>
        public bool MoveInBackpack(Item item, Vector2Int corner)
        {
            if (!Inventory.TryMove(item, corner))
                return false;
            NotifyChanged();
            return true;
        }

        /// <summary>The backpack's auto sort (2026-10-18).</summary>
        public void SortBackpack()
        {
            Inventory.Sort();
            NotifyChanged();
        }

        void NotifyChanged()
        {
            Changed?.Invoke();
            Modified?.Invoke();
        }
    }
}
