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
        // Worn over the body and under the helm (2026-10-04): boots, the belt, the gloves.
        Boots,
        Belt,
        Gloves,
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
        public readonly string Boots;
        public readonly string Belt;
        public readonly string Gloves;

        public CharacterAppearance(string body, string helm, string offHand, string weapon, CharacterGrip grip,
            string boots = null, string belt = null, string gloves = null)
        {
            Body = body;
            Helm = helm;
            OffHand = offHand;
            Weapon = weapon;
            Grip = grip;
            Boots = boots;
            Belt = belt;
            Gloves = gloves;
        }

        public string LookOf(AppearanceLayer layer) => layer switch
        {
            AppearanceLayer.Body => Body,
            AppearanceLayer.Boots => Boots,
            AppearanceLayer.Belt => Belt,
            AppearanceLayer.Gloves => Gloves,
            AppearanceLayer.Helm => Helm,
            AppearanceLayer.OffHand => OffHand,
            _ => Weapon,
        };

        public bool Equals(CharacterAppearance other) =>
            Body == other.Body && Helm == other.Helm && OffHand == other.OffHand && Weapon == other.Weapon && Grip == other.Grip &&
            Boots == other.Boots && Belt == other.Belt && Gloves == other.Gloves;
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

        // The worn looks, remade on 2026-10-05 (the owner: they "look bad"; "full face cover helms that look cool, armor
        // that looks cool"): five tiers each, by ArmourTier. ArtSource/tools/props/wild_arrow_gear.py builds them.
        public static readonly string[] ChestLooks = { "ranger", "brigand", "scale", "plate", "knight" };
        public static readonly string[] HelmLooks = { "hood", "mask", "barbute", "visored", "horned" };
        public static readonly string[] BootLooks = { "wrapped", "leather_boots", "strapped", "sabatons", "spiked" };
        public static readonly string[] BeltLooks = { "rope", "pouch_belt", "studded", "tassets", "war_girdle" };
        public static readonly string[] GloveLooks = { "wraps", "gloves", "bracers", "gauntlets", "claws" };
        // Bows only since 2026-09-30 (Docs/03, act 1's looks): short bows, longbows and quivers.
        public static readonly string[] OneHandWeaponLooks = { "hunting_bow", "recurve_bow", "horn_bow" };
        public static readonly string[] TwoHandWeaponLooks = { "yew_longbow", "war_bow", "great_bow" };
        public static readonly string[] ShieldLooks = { "hide_quiver", "studded_quiver", "bone_quiver" };

        /// <summary>
        /// Every named legendary that shows has its own look (Docs/03; built 2026-10-05): its slot's layer shows it in
        /// place of the tier's. Rings and amulets do not show. Sheets named as the look (wild_arrow_helm_falconer...).
        /// </summary>
        public static readonly (LegendaryId id, AppearanceLayer layer, string look)[] LegendaryLooks =
        {
            (LegendaryId.FalconersHood, AppearanceLayer.Helm, "falconer"),
            (LegendaryId.CrownOfTheUnblinkingEye, AppearanceLayer.Helm, "unblinking_crown"),
            (LegendaryId.HideOfTheRunningStag, AppearanceLayer.Body, "stag_hide"),
            (LegendaryId.CinderStitchedJerkin, AppearanceLayer.Body, "cinder_jerkin"),
            (LegendaryId.FletchersFingers, AppearanceLayer.Gloves, "fletcher"),
            (LegendaryId.BloodlettersGrips, AppearanceLayer.Gloves, "bloodletter"),
            (LegendaryId.WindrunnerTreads, AppearanceLayer.Boots, "windrunner"),
            (LegendaryId.StalkersTreads, AppearanceLayer.Boots, "stalker"),
            (LegendaryId.BandolierOfManyHeads, AppearanceLayer.Belt, "bandolier"),
            (LegendaryId.Splinterbough, AppearanceLayer.Weapon, "splinterbough"),
            (LegendaryId.EmberTongue, AppearanceLayer.Weapon, "ember_tongue"),
            (LegendaryId.HuntersPromise, AppearanceLayer.Weapon, "hunters_promise"),
            (LegendaryId.Galeheart, AppearanceLayer.Weapon, "galeheart"),
            (LegendaryId.WidowsDraw, AppearanceLayer.Weapon, "widows_draw"),
            (LegendaryId.Gallowsreach, AppearanceLayer.Weapon, "gallowsreach"),
            (LegendaryId.StillwaterYew, AppearanceLayer.Weapon, "stillwater_yew"),
            (LegendaryId.TheLongSilence, AppearanceLayer.Weapon, "the_long_silence"),
            (LegendaryId.QuiverOfEndlessSplinters, AppearanceLayer.OffHand, "quiver_of_endless_splinters"),
            (LegendaryId.AshfallQuiver, AppearanceLayer.OffHand, "ashfall_quiver"),
            (LegendaryId.WindSwornQuiver, AppearanceLayer.OffHand, "wind_sworn_quiver"),
            (LegendaryId.MagpiesNest, AppearanceLayer.OffHand, "magpies_nest"),
            (LegendaryId.QuiverOfTheHollowHound, AppearanceLayer.OffHand, "quiver_of_the_hollow_hound"),
        };

        /// <summary>A legendary's own look, or null.</summary>
        public static string LegendaryLook(LegendaryId id)
        {
            foreach (var entry in LegendaryLooks)
                if (entry.id == id)
                    return entry.look;
            return null;
        }

        /// <summary>The legendary looks a layer can show (for the bake).</summary>
        public static System.Collections.Generic.IEnumerable<string> LegendaryLooksOf(AppearanceLayer layer)
        {
            foreach (var entry in LegendaryLooks)
                if (entry.layer == layer)
                    yield return entry.look;
        }

        /// <summary>Act 1's item level bands for the bows and quivers (the brief, 10.2): 1 to 3, 4 to 6, 7 and up.</summary>
        public static int Tier(int itemLevel) => itemLevel <= 3 ? 0 : itemLevel <= 6 ? 1 : 2;

        /// <summary>The worn armour's five bands (2026-10-05, over the 24 levels' item levels): 1 to 3, 4 to 7, 8 to
        /// 11, 12 to 15, 16 and up.</summary>
        public static int ArmourTier(int itemLevel) =>
            itemLevel <= 3 ? 0 : itemLevel <= 7 ? 1 : itemLevel <= 11 ? 2 : itemLevel <= 15 ? 3 : 4;

        public static string LookOf(string[] looks, Item item)
        {
            if (item == null)
                return null;
            var own = LegendaryLook(item.Legendary);
            if (own != null)
                return own;
            var tier = looks.Length == 5 ? ArmourTier(item.ItemLevel) : Tier(item.ItemLevel);
            return looks[System.Math.Min(tier, looks.Length - 1)];
        }

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
            CharacterGrip.OneHand,
            LookOf(BootLooks, equipment.Get(ItemSlot.Boots)),
            LookOf(BeltLooks, equipment.Get(ItemSlot.Belt)),
            LookOf(GloveLooks, equipment.Get(ItemSlot.Gloves)));

        /// <summary>
        /// The look shown when a layer's own look has no sheets yet: the leather body, the hunting bow and the hide quiver
        /// (the first models, 2026-10-03; every bow and quiver look is modelled since 2026-10-04, ArtSource/tools/props/
        /// gear.py, so this only covers a missing bake); null for the helm.
        /// </summary>
        public static string FallbackLook(AppearanceLayer layer, CharacterGrip grip = CharacterGrip.OneHand) => layer switch
        {
            AppearanceLayer.Body => BareBody,
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
            AppearanceLayer.Boots => "boots",
            AppearanceLayer.Belt => "belt",
            AppearanceLayer.Gloves => "gloves",
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
