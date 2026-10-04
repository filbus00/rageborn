namespace ARPG
{
    /// <summary>
    /// How the character holds its gear (decision of 2026-09-27, Docs/03-itemization.md, Appearance): the off-hand holds
    /// nothing, an off-hand weapon or a shield, or a two-handed weapon takes both hands. Each grip has its own animation set.
    /// </summary>
    public enum CharacterGrip
    {
        OneHand,
        DualWield,
        Shield,
        TwoHand,
    }

    /// <summary>The sprite layers of a character, in the order they are stacked (the first is drawn first).</summary>
    public enum AppearanceLayer
    {
        Body,
        Helm,
        OffHand,
        Weapon,
    }

    /// <summary>What a character looks like right now: a look id per layer (null for an empty slot) and the grip.</summary>
    public readonly struct CharacterAppearance
    {
        public readonly string Body;
        public readonly string Helm;
        public readonly string OffHand;
        public readonly string Weapon;
        public readonly CharacterGrip Grip;

        public CharacterAppearance(string body, string helm, string offHand, string weapon, CharacterGrip grip)
        {
            Body = body;
            Helm = helm;
            OffHand = offHand;
            Weapon = weapon;
            Grip = grip;
        }

        public string LookOf(AppearanceLayer layer) => layer switch
        {
            AppearanceLayer.Body => Body,
            AppearanceLayer.Helm => Helm,
            AppearanceLayer.OffHand => OffHand,
            _ => Weapon,
        };

        public bool Equals(CharacterAppearance other) =>
            Body == other.Body && Helm == other.Helm && OffHand == other.OffHand && Weapon == other.Weapon && Grip == other.Grip;
    }

    /// <summary>
    /// Which look the equipped gear shows (Docs/03-itemization.md, Appearance; the looks are Docs/09-art-brief.md, 4.5):
    /// weapon, off-hand, helm and chest armour show, in tiers by item level (act 1: 1 to 3, 4 to 6, 7 and up). The names
    /// here are the sprite sheets' look ids. Legendaries are to have their own looks; until named legendaries exist they
    /// show their tier's look. The off-hand slot and two-handed weapons are not items yet, so the game's grip is always
    /// one-handed; <see cref="GripFor"/> is the rule for when they are.
    /// </summary>
    public static class AppearanceRules
    {
        public const string BareBody = "bare";

        public static readonly string[] ChestLooks = { "padded", "leather", "mail" };
        public static readonly string[] HelmLooks = { "cap", "nasal", "great" };
        // Bows only since 2026-09-30 (Docs/03, act 1's looks): short bows, longbows and quivers.
        public static readonly string[] OneHandWeaponLooks = { "hunting_bow", "recurve_bow", "horn_bow" };
        public static readonly string[] TwoHandWeaponLooks = { "yew_longbow", "war_bow", "great_bow" };
        public static readonly string[] ShieldLooks = { "hide_quiver", "studded_quiver", "bone_quiver" };

        /// <summary>Act 1's item level bands (the brief, 10.2): 1 to 3 is tier 0, 4 to 6 tier 1, 7 and up tier 2.</summary>
        public static int Tier(int itemLevel) => itemLevel <= 3 ? 0 : itemLevel <= 6 ? 1 : 2;

        public static string LookOf(string[] looks, Item item) =>
            item == null ? null : looks[System.Math.Min(Tier(item.ItemLevel), looks.Length - 1)];

        /// <summary>The grip from what the hands hold: a two-handed weapon wins, then the off-hand item.</summary>
        public static CharacterGrip GripFor(bool twoHandedWeapon, bool offHandWeapon, bool shield)
        {
            if (twoHandedWeapon)
                return CharacterGrip.TwoHand;
            if (shield)
                return CharacterGrip.Shield;
            return offHandWeapon ? CharacterGrip.DualWield : CharacterGrip.OneHand;
        }

        /// <summary>The look of what is equipped now. Empty slots show the bare body, the bare head and empty hands.
        /// Bows only (2026-09-30): one grip for every bow (the stand-in bakes it as "1h", Docs/09 4.6), the bow in the
        /// weapon layer and the quiver in the off-hand layer.</summary>
        public static CharacterAppearance For(EquipmentState equipment) => new CharacterAppearance(
            LookOf(ChestLooks, equipment.Chest) ?? BareBody,
            LookOf(HelmLooks, equipment.Helm),
            equipment.HasQuiver ? LookOf(ShieldLooks, equipment.OffHand) : null,
            LookOf(equipment.IsLongbow ? TwoHandWeaponLooks : OneHandWeaponLooks, equipment.Weapon),
            CharacterGrip.OneHand);

        /// <summary>
        /// The look shown when a layer's own look has no sheets yet: the leather body, the hunting bow and the hide quiver
        /// (the first models, 2026-10-03; every bow and quiver look is modelled since 2026-10-04, ArtSource/tools/props/
        /// gear.py, so this only covers a missing bake); null for the helm.
        /// </summary>
        public static string FallbackLook(AppearanceLayer layer, CharacterGrip grip = CharacterGrip.OneHand) => layer switch
        {
            AppearanceLayer.Body => "leather",
            AppearanceLayer.Weapon => OneHandWeaponLooks[0],
            AppearanceLayer.OffHand => ShieldLooks[0],
            _ => null,
        };

        /// <summary>The sprite sheets' short names (the brief's file names).</summary>
        public static string GripCode(CharacterGrip grip) => grip switch
        {
            CharacterGrip.DualWield => "dual",
            CharacterGrip.Shield => "shield",
            CharacterGrip.TwoHand => "2h",
            _ => "1h",
        };

        public static string LayerCode(AppearanceLayer layer) => layer switch
        {
            AppearanceLayer.Helm => "helm",
            AppearanceLayer.OffHand => "offhand",
            AppearanceLayer.Weapon => "weapon",
            _ => "body",
        };

        /// <summary>
        /// A layer sheet's name, as the sprite bake writes it: character, layer, look, grip, animation. The Resources path
        /// of a character's sheets is Characters/&lt;character&gt;/&lt;name&gt;.
        /// </summary>
        public static string SheetName(string character, AppearanceLayer layer, string look, CharacterGrip grip, string animation) =>
            $"{character}_{LayerCode(layer)}_{look}_{GripCode(grip)}_{animation}";

        static readonly string[] EightDirections = { "s", "sw", "w", "nw", "n", "ne", "e", "se" };

        static readonly string[] SixteenDirections =
            { "s", "ssw", "sw", "wsw", "w", "wnw", "nw", "nnw", "n", "nne", "ne", "ene", "e", "ese", "se", "sse" };

        /// <summary>
        /// The sheet rows' names, in row order from the top: south first, then clockwise seen from above. 8 for enemies,
        /// 16 for the player (as Diablo 2 gave its heroes; the user's request of 2026-09-27). Every second name of the
        /// 16 is the 8's.
        /// </summary>
        public static string[] DirectionCodes(int count) => count == 16 ? SixteenDirections : EightDirections;

        /// <summary>
        /// Which sheet row faces a ground direction: the nearest of <paramref name="count"/> directions spread evenly
        /// on the ground from south, clockwise seen from above (the sprite bake's convention, x along the screen, y up it).
        /// </summary>
        public static int DirectionRow(UnityEngine.Vector2 groundDirection, int count = 8)
        {
            if (groundDirection.sqrMagnitude < 1e-8f)
                return 0;
            var angle = UnityEngine.Mathf.Atan2(groundDirection.y, groundDirection.x) * UnityEngine.Mathf.Rad2Deg;
            var steps = UnityEngine.Mathf.RoundToInt((270f - angle) / (360f / count));
            return ((steps % count) + count) % count;
        }
    }
}
