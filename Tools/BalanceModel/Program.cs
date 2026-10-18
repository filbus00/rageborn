using System;
using System.Text;
using ARPG;

// An offline balance model (2026-10-18), not shipped: the game's pure formulas at a few depths, printed as a Markdown
// table. It changes no numbers. Assumptions are printed with the table.
static class Program
{
    static readonly int[] Depths = { 1, 3, 6, 12, 18, 24, 36, 48 };
    const int Samples = 3000;

    static void Main()
    {
        System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
        var text = new StringBuilder();
        text.AppendLine("| Depth | Enemy level | Enemy life | Enemy hit | Her level | Her life (kit / Magic) | Arrow, kit | Arrow, Magic | Arrows to kill (kit / Magic) | Basic attacks a second (kit / Magic) | Enemy hits to kill her (kit / Magic) |");
        text.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|");
        foreach (var depth in Depths)
        {
            var enemyLevel = DungeonRules.EnemyLevel(depth);
            var herLevel = BoonRules.LevelAt(depth);
            var enemyLife = CombatFormulas.EnemyLife(enemyLevel);
            var enemyHit = CombatFormulas.EnemyHitDamage(enemyLevel);

            var kit = Stats.Of(EquipmentState.Starting);
            var magic = Stats.AverageMagic(enemyLevel, new Random(depth));

            var kitArrow = kit.ArrowDamage(enemyLevel);
            var magicArrow = magic.ArrowDamage(enemyLevel);
            var kitLife = CombatFormulas.CharacterLife(herLevel) + kit.Life;
            var magicLife = CombatFormulas.CharacterLife(herLevel) + magic.Life;
            var kitTaken = CombatFormulas.EnemyHitOnPlayer(enemyLevel, 1f, kit.Armor);
            var magicTaken = CombatFormulas.EnemyHitOnPlayer(enemyLevel, 1f, magic.Armor);

            text.AppendLine($"| {depth} | {enemyLevel} | {enemyLife:0} | {enemyHit:0.0} | {herLevel} | {kitLife:0} / {magicLife:0} | {kitArrow:0.0} | {magicArrow:0.0} | " +
                            $"{Math.Ceiling(enemyLife / kitArrow):0} / {Math.Ceiling(enemyLife / magicArrow):0} | {kit.AttacksPerSecond:0.00} / {magic.AttacksPerSecond:0.00} | " +
                            $"{Math.Ceiling(kitLife / kitTaken):0} / {Math.Ceiling(magicLife / magicTaken):0} |");
        }

        text.AppendLine();
        text.AppendLine("Boon multipliers on her basic attack's damage (every arrow finding a target, as against a pack):");
        text.AppendLine();
        text.AppendLine("| Boon | Rank 1 | Rank 2 | Rank 3 |");
        text.AppendLine("|---|---|---|---|");
        text.AppendLine($"| Extra Arrow (+1 arrow a rank, fanned {7} degrees) | {1 + 1}x | {1 + 2}x | {1 + 3}x |");
        text.AppendLine($"| Extra Shot (the bow looses again a rank) | {1 + 1}x | {1 + 2}x | {1 + 3}x |");
        text.AppendLine($"| Extra Arrow and Extra Shot together | {(1 + 1) * (1 + 1)}x | {(1 + 2) * (1 + 2)}x | {(1 + 3) * (1 + 3)}x |");
        text.AppendLine();
        text.AppendLine($"Thunder Arrow (every {UniqueBoonRules.ThunderEvery}th basic attack, {UniqueBoonRules.ThunderShare:0.0} of the arrow to every other enemy within {UniqueBoonRules.ThunderRadius}):");
        text.AppendLine();
        text.AppendLine("| Other enemies within reach | Multiplier |");
        text.AppendLine("|---|---|");
        foreach (var others in new[] { 0, 3, 6, 10 })
        {
            // Extra Arrow's copies carry the thunder too (PlayerCombat.AddExtraArrows copies the first arrow), so the
            // multiplier stays the same on top of Extra Arrow's.
            var alone = 1f + UniqueBoonRules.ThunderShare * others / UniqueBoonRules.ThunderEvery;
            text.AppendLine($"| {others} | {alone:0.00}x |");
        }
        Console.Write(text.ToString());
    }

    // The gear numbers the hit formula reads.
    sealed class Stats
    {
        public float Weapon, Flat, Increased, CritChance, CritDamage, AttackSpeed, Life, Armor, QuiverSpeed;

        public float AttacksPerSecond => 1.4f * (1f + AttackSpeed + QuiverSpeed);

        // The expected damage of one basic arrow on a normal enemy of a level (armour 0, as every enemy definition
        // has until tuned), with crits averaged in.
        public float ArrowDamage(int enemyLevel)
        {
            var normal = CombatFormulas.HitDamage(Weapon, 1f, Flat, Increased, 1f, false, CritDamage, 0f, enemyLevel);
            var crit = CombatFormulas.HitDamage(Weapon, 1f, Flat, Increased, 1f, true, CritDamage, 0f, enemyLevel);
            var chance = Math.Min(1f, CritChance);
            return normal * (1f - chance) + crit * chance;
        }

        public static Stats Of(EquipmentState gear) => new Stats
        {
            Weapon = gear.WeaponDamage,
            Flat = gear.FlatWeaponDamageBonus,
            Increased = gear.IncreasedDamagePercent / 100f,
            CritChance = gear.CriticalChancePercent / 100f,
            CritDamage = gear.CriticalDamagePercent / 100f,
            AttackSpeed = gear.AttackSpeedPercent / 100f,
            QuiverSpeed = gear.GripAttackSpeedBonus,
            Life = gear.TotalLifeBonus,
            Armor = gear.TotalArmor,
        };

        // A short bow, a quiver and the eight worn pieces all Magic at the depth's enemy level, averaged over many rolls.
        public static Stats AverageMagic(int itemLevel, Random random)
        {
            var sum = new Stats();
            for (var i = 0; i < Samples; i++)
            {
                Item Roll(ItemSlot slot) => new Item(slot, ItemRarity.Magic, itemLevel, AffixRoller.Roll(slot, ItemRarity.Magic, itemLevel, random));
                var gear = new EquipmentState(Roll(ItemSlot.Weapon))
                    .With(ItemSlot.OffHand, Roll(ItemSlot.Shield))
                    .With(ItemSlot.Chest, Roll(ItemSlot.Chest))
                    .With(ItemSlot.Helm, Roll(ItemSlot.Helm))
                    .With(ItemSlot.Gloves, Roll(ItemSlot.Gloves))
                    .With(ItemSlot.Boots, Roll(ItemSlot.Boots))
                    .With(ItemSlot.Belt, Roll(ItemSlot.Belt))
                    .With(ItemSlot.Amulet, Roll(ItemSlot.Amulet))
                    .With(ItemSlot.Ring, Roll(ItemSlot.Ring))
                    .With(ItemSlot.Ring2, Roll(ItemSlot.Ring));
                var s = Of(gear);
                sum.Weapon += s.Weapon; sum.Flat += s.Flat; sum.Increased += s.Increased; sum.CritChance += s.CritChance;
                sum.CritDamage += s.CritDamage; sum.AttackSpeed += s.AttackSpeed; sum.QuiverSpeed += s.QuiverSpeed;
                sum.Life += s.Life; sum.Armor += s.Armor;
            }
            return new Stats
            {
                Weapon = sum.Weapon / Samples, Flat = sum.Flat / Samples, Increased = sum.Increased / Samples,
                CritChance = sum.CritChance / Samples, CritDamage = sum.CritDamage / Samples, AttackSpeed = sum.AttackSpeed / Samples,
                QuiverSpeed = sum.QuiverSpeed / Samples, Life = sum.Life / Samples, Armor = sum.Armor / Samples,
            };
        }
    }
}
