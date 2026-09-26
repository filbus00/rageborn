using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace ARPG.Editor
{
    /// <summary>
    /// A first balance simulator (Docs/08-production.md: a balance simulator from M1). It generates the act's six levels
    /// for many dungeon seeds exactly as the game does (the real room templates, <see cref="DungeonGenerator"/>,
    /// <see cref="PackComposition"/>, the enemy assets), walks a character from level 1 through every level killing
    /// everything, and puts the game's own formulas side by side: XP and levels, the character's damage against enemy
    /// life, and enemy hits against the character's life. It is a model, not a playtest: it assumes gear (below), counts
    /// every pack slot as spawned, and estimates how many enemies a swing catches. Writes Logs/BalanceReport.md.
    ///
    /// Gear models, both at the item level of the depth before (what the last level dropped), with no gear on depth 1
    /// but the starting weapon: "floor" is Common pieces (base stats only); "typical" is Magic pieces with average T5
    /// affixes (act 1 item levels only unlock T5): the weapon with flat damage and attack speed, chest and helm with life.
    /// </summary>
    public static class BalanceReport
    {
        const int Seeds = 40;
        const string OutputPath = "Logs/BalanceReport.md";
        const string EnemiesFolder = "Assets/_Project/Data/Enemies";

        // How many enemies a swing catches in a crowd: the basic attack is a 120 degree sweep, Cleave 200. Assumptions.
        const float CrowdTargetsBasic = 2f;
        const float CrowdTargetsCleave = 3f;

        // Cleave (Data/Skills/Cleave.asset): 180 percent, every 2.5 s. Focus (15 a cast, 6 a second plus 4 a hitting
        // swing) never runs out at that rate, so the cooldown sets it.
        const float CleaveMultiplier = 1.8f;
        const float CleaveCooldown = 2.5f;

        // How many husks can reach the character at once: a ring around it, pushed apart. An assumption.
        const int HusksInReach = 5;

        sealed class DepthStats
        {
            public float Husks, Champions, Elites, Ghouls, Archers;
            public float Xp, Life, EntryLevel, ExitLevel, Gold;
        }

        [MenuItem("Tools/ARPG/Balance Report")]
        public static void RunFromMenu()
        {
            var report = Run();
            Debug.Log($"[ARPG] Balance report written to {OutputPath}.\n{report}");
        }

        public static string Run()
        {
            var shapes = new List<RoomShape>();
            foreach (var guid in AssetDatabase.FindAssets("t:RoomTemplate", new[] { "Assets/_Project/Data/Rooms" }))
            {
                var shape = AssetDatabase.LoadAssetAtPath<RoomTemplate>(AssetDatabase.GUIDToAssetPath(guid)).TryGetShape();
                if (shape != null)
                    shapes.Add(shape);
            }

            var husk = Load("Swarmer");
            var champion = Load("SwarmerChampion");
            var elite = Load("SwarmerElite");
            var ghoul = Load("Ghoul");
            var archer = Load("BanditArcher");
            var boss = Load("CinderWarden");

            var depths = new DepthStats[DungeonRules.LevelsPerAct + 1];
            for (var d = 1; d <= DungeonRules.LevelsPerAct; d++)
                depths[d] = new DepthStats();

            var settings = new DungeonSettings { NormalAggroRange = husk.AggroRange, EliteAggroRange = elite.AggroRange };
            var loot = new LootRoller(1);
            for (var s = 0; s < Seeds; s++)
            {
                var dungeonSeed = DungeonRules.LevelSeed(9000 + s, 0);
                var progress = new CharacterProgress();
                for (var depth = 1; depth <= DungeonRules.LevelsPerAct; depth++)
                {
                    var stats = depths[depth];
                    var levelSeed = DungeonRules.LevelSeed(dungeonSeed, depth);
                    var layout = DungeonGenerator.Generate(levelSeed, depth, shapes, settings);
                    var level = layout.EnemyLevel;
                    stats.EntryLevel += progress.Level;

                    var kills = new List<EnemyDefinition>();
                    for (var i = 0; i < layout.Packs.Count; i++)
                    {
                        var pack = layout.Packs[i];
                        if (pack.Kind == PackKind.Elite)
                        {
                            for (var k = 0; k < pack.Count; k++)
                                kills.Add(elite);
                            continue;
                        }
                        var members = PackComposition.Roll(depth, pack.Kind, pack.Count, levelSeed, i);
                        for (var k = 0; k < members.Length; k++)
                        {
                            if (k == 0 && pack.Kind == PackKind.WithChampion)
                                kills.Add(champion);
                            else if (members[k] == PackMember.Ghoul)
                                kills.Add(ghoul);
                            else if (members[k] == PackMember.Archer)
                                kills.Add(archer);
                            else if (members[k] == PackMember.Husk)
                                kills.Add(husk);
                        }
                    }
                    if (layout.HasBossArena)
                        kills.Add(boss);

                    foreach (var enemy in kills)
                    {
                        var xp = Experience.KillXp(progress.Level, level, enemy.Rank);
                        stats.Xp += xp;
                        progress.Add(xp);
                        stats.Life += enemy.MaxLifeAt(level);
                        stats.Gold += loot.RollGold(Source(enemy.Rank), level);
                        if (enemy == husk) stats.Husks++;
                        else if (enemy == champion) stats.Champions++;
                        else if (enemy == elite) stats.Elites++;
                        else if (enemy == ghoul) stats.Ghouls++;
                        else if (enemy == archer) stats.Archers++;
                    }
                    stats.ExitLevel += progress.Level;
                }
            }

            var text = new StringBuilder();
            text.AppendLine("# Balance report (act 1)");
            text.AppendLine();
            text.AppendLine($"{Seeds} generated dungeons, every enemy killed, depth by depth. Averages. Made by Tools > ARPG > Balance Report ({nameof(BalanceReport)}.cs); see its summary for what the model assumes.");
            text.AppendLine();
            text.AppendLine("## Enemies and progression");
            text.AppendLine();
            text.AppendLine("| Depth | Enemy level | Husks | Champions | Elites | Ghouls | Archers | Enemy life total | XP | Character level in → out | Gold |");
            text.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|");
            for (var d = 1; d <= DungeonRules.LevelsPerAct; d++)
            {
                var x = depths[d];
                text.AppendLine($"| {d} | {DungeonRules.EnemyLevel(d)} | {x.Husks / Seeds:0} | {x.Champions / Seeds:0.0} | {x.Elites / Seeds:0.0} | {x.Ghouls / Seeds:0.0} | {x.Archers / Seeds:0.0} | {x.Life / Seeds:0} | {x.Xp / Seeds:0} | {x.EntryLevel / Seeds:0.0} → {x.ExitLevel / Seeds:0.0} | {x.Gold / Seeds:0} |");
            }

            foreach (var typical in new[] { false, true })
            {
                text.AppendLine();
                text.AppendLine(typical ? "## Combat, typical gear (Magic, average T5 affixes)" : "## Combat, floor gear (Common, base stats only)");
                text.AppendLine();
                text.AppendLine("| Depth | Char level | Hit | Single-target DPS | Crowd DPS | Husk dies in | Ghoul dies in | Life (effective) | Husk hit | Ghoul slam | Arrow | Dies to 5 husks in | Kills 5 husks in | Fight time for the level | Boss dies in |");
                text.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|");
                for (var d = 1; d <= DungeonRules.LevelsPerAct; d++)
                {
                    var x = depths[d];
                    var charLevel = Mathf.RoundToInt(x.EntryLevel / Seeds);
                    var level = DungeonRules.EnemyLevel(d);
                    var gearLevel = d == 1 ? 0 : DungeonRules.EnemyLevel(d - 1);
                    var gear = Gear(gearLevel, typical);
                    var power = PowerScore.Evaluate(gear, charLevel);
                    var hit = power.DamagePerSecond / PowerScore.BaseAttacksPerSecond / (1f + gear.AttackSpeedPercent / 100f);
                    var swings = PowerScore.BaseAttacksPerSecond * (1f + gear.AttackSpeedPercent / 100f);
                    var single = hit * (swings + CleaveMultiplier / CleaveCooldown);
                    var crowd = hit * (swings * CrowdTargetsBasic + CleaveMultiplier / CleaveCooldown * CrowdTargetsCleave);

                    var armor = gear.TotalArmor;
                    var maxLife = CombatFormulas.CharacterBaseLife(charLevel) + gear.TotalLifeBonus;
                    var huskHit = CombatFormulas.EnemyHitOnPlayer(level, husk.DamageMultiplier, armor);
                    var ghoulHit = CombatFormulas.EnemyHitOnPlayer(level, ghoul.DamageMultiplier, armor);
                    var arrowHit = CombatFormulas.EnemyHitOnPlayer(level, archer.DamageMultiplier, armor);
                    var huskCycle = husk.AttackWindupSeconds + husk.AttackRecoverSeconds;
                    var diesTo5 = maxLife / (HusksInReach * huskHit / huskCycle);
                    var kills5 = HusksInReach * husk.MaxLifeAt(level) / crowd;
                    var fight = x.Life / Seeds / crowd;
                    var bossTime = d == DungeonRules.LevelsPerAct ? $"{boss.MaxLifeAt(level) / single:0} s" : "";

                    text.AppendLine($"| {d} | {charLevel} | {hit:0} | {single:0} | {crowd:0} | {husk.MaxLifeAt(level) / single:0.0} s | {ghoul.MaxLifeAt(level) / single:0.0} s | {maxLife:0} ({power.EffectiveLife:0}) | {huskHit:0} | {ghoulHit:0} | {arrowHit:0} | {diesTo5:0.0} s | {kills5:0.0} s | {fight:0} s | {bossTime} |");
                }
            }

            text.AppendLine();
            text.AppendLine($"Assumptions: a crowd swing catches {CrowdTargetsBasic} enemies with the basic attack and {CrowdTargetsCleave} with Cleave; {HusksInReach} husks can reach the character at once; fight time is the level's total enemy life over crowd DPS, with no walking; the potion (3 charges of 40 percent) and dodging telegraphs are left out.");

            Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
            File.WriteAllText(OutputPath, text.ToString());
            return text.ToString();
        }

        static EquipmentState Gear(int itemLevel, bool typical)
        {
            if (itemLevel <= 0)
                return EquipmentState.Starting;

            var rarity = typical ? ItemRarity.Magic : ItemRarity.Common;
            var weapon = new Item(ItemSlot.Weapon, rarity, itemLevel, typical ? new[] { Average(AffixId.FlatWeaponDamage), Average(AffixId.AttackSpeed) } : null);
            var chest = new Item(ItemSlot.Chest, rarity, itemLevel, typical ? new[] { Average(AffixId.Life) } : null);
            var helm = new Item(ItemSlot.Helm, rarity, itemLevel, typical ? new[] { Average(AffixId.Life) } : null);
            return EquipmentState.Empty.With(ItemSlot.Weapon, weapon).With(ItemSlot.Chest, chest).With(ItemSlot.Helm, helm);
        }

        static AffixRoll Average(AffixId id)
        {
            var (min, max) = AffixRoller.ValueRange(id, AffixTable.WorstTier);
            return new AffixRoll(id, AffixTable.WorstTier, (min + max) / 2f);
        }

        static LootSource Source(EnemyRank rank)
        {
            switch (rank)
            {
                case EnemyRank.Champion: return LootSource.Champion;
                case EnemyRank.Elite: return LootSource.Elite;
                case EnemyRank.Boss: return LootSource.Boss;
                default: return LootSource.NormalEnemy;
            }
        }

        static EnemyDefinition Load(string name) => AssetDatabase.LoadAssetAtPath<EnemyDefinition>($"{EnemiesFolder}/{name}.asset");
    }
}
