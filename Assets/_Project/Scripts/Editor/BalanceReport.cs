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

        // How many enemies a hit catches in a crowd, by skill kind. Assumptions. The basic attack is one arrow (bows only,
        // 2026-09-30), which stops at the first enemy it meets.
        const float CrowdTargetsBasic = 1f;
        const float CrowdTargetsSweep = 3f;
        const float CrowdTargetsSlam = 5f;
        const float CrowdTargetsCharge = 3f;

        // The Wild Arrow's skills in a crowd (assumptions): Split Arrow's fan catches up to 3, a pierce arrow goes through
        // 2.5 on its line, every homing arrow finds an enemy, and the explosive burst catches as many as a slam.
        const float CrowdTargetsVolley = 3f;
        const float CrowdTargetsPierce = 2.5f;
        // The Wild Arrow kites (Docs/02): it fights moving, with Momentum's speed and dodge rather than Stillness's damage
        // and reduction. Momentum's dodge is left out, like every dodge in this model.
        const bool AssumeStillness = false;

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

        public static string Run() => Run(DungeonRules.EnemyLevel, CombatFormulas.VitalityPerLevel);

        /// <summary>The report with other enemy levels by depth or another stand-in Vitality per level, to try tuning
        /// before changing the game.</summary>
        public static string Run(System.Func<int, int> enemyLevel, float vitalityPerLevel)
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
            // From depth 4; an older project without them counts husks and archers in their place.
            var skeleton = AssetDatabase.LoadAssetAtPath<EnemyDefinition>("Assets/_Project/Data/Enemies/Skeleton.asset") ?? husk;
            var cultist = AssetDatabase.LoadAssetAtPath<EnemyDefinition>("Assets/_Project/Data/Enemies/Cultist.asset") ?? archer;
            // The rest of act 1 (2026-10-05); an older project counts its stand-ins.
            var ashWolf = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(Act1RosterBuilder.AshWolfPath) ?? husk;
            var cutthroat = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(Act1RosterBuilder.CutthroatPath) ?? husk;
            var acolyte = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(Act1RosterBuilder.EmberAcolytePath) ?? archer;
            var keeper = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(Act1RosterBuilder.PyreKeeperPath) ?? husk;
            var bloat = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(Act1RosterBuilder.CarrionBloatPath) ?? ghoul;
            // The class's own skills (the Wild Arrow's, from Resources), not every asset in Data/Skills, which still holds
            // the retired Wrathborn's.
            var skills = new List<SkillDefinition>();
            var classSkills = ClassSkills.Load();
            if (classSkills != null)
                foreach (var skill in classSkills.Skills)
                    if (skill != null)
                        skills.Add(skill);

            var depths = new DepthStats[DungeonRules.Depths + 1];
            for (var d = 1; d <= DungeonRules.Depths; d++)
                depths[d] = new DepthStats();

            var settings = new DungeonSettings { NormalAggroRange = husk.AggroRange, EliteAggroRange = elite.AggroRange };
            var loot = new LootRoller(1);
            for (var s = 0; s < Seeds; s++)
            {
                var dungeonSeed = DungeonRules.LevelSeed(9000 + s, 0);
                var progress = new CharacterProgress();
                for (var depth = 1; depth <= DungeonRules.Depths; depth++)
                {
                    var stats = depths[depth];
                    var levelSeed = DungeonRules.LevelSeed(dungeonSeed, depth);
                    var layout = DungeonGenerator.Generate(levelSeed, depth, shapes, settings);
                    var level = enemyLevel(depth);
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
                            else if (members[k] == PackMember.Skeleton)
                                kills.Add(skeleton);
                            else if (members[k] == PackMember.Cultist)
                                kills.Add(cultist);
                            else if (members[k] == PackMember.AshWolf)
                                kills.Add(ashWolf);
                            else if (members[k] == PackMember.Cutthroat)
                                kills.Add(cutthroat);
                            else if (members[k] == PackMember.EmberAcolyte)
                                kills.Add(acolyte);
                            else if (members[k] == PackMember.PyreKeeper)
                                kills.Add(keeper);
                            else if (members[k] == PackMember.CarrionBloat)
                                kills.Add(bloat);
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
            text.AppendLine($"# Balance report ({DungeonRules.Depths} levels)");
            text.AppendLine();
            text.AppendLine($"{Seeds} generated dungeons, every enemy killed, depth by depth. Averages. Made by Tools > ARPG > Balance Report ({nameof(BalanceReport)}.cs); see its summary for what the model assumes.");
            text.AppendLine();
            text.AppendLine($"Stand-in Vitality: {vitalityPerLevel} points per level from level 2, {vitalityPerLevel * CombatFormulas.LifePerVitality} life a level.");
            text.AppendLine();
            text.AppendLine("## Enemies and progression");
            text.AppendLine();
            text.AppendLine("| Depth | Enemy level | Husks | Champions | Elites | Ghouls | Archers | Enemy life total | XP | Character level in → out | Gold |");
            text.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|");
            for (var d = 1; d <= DungeonRules.Depths; d++)
            {
                var x = depths[d];
                text.AppendLine($"| {d} | {enemyLevel(d)} | {x.Husks / Seeds:0} | {x.Champions / Seeds:0.0} | {x.Elites / Seeds:0.0} | {x.Ghouls / Seeds:0.0} | {x.Archers / Seeds:0.0} | {x.Life / Seeds:0} | {x.Xp / Seeds:0} | {x.EntryLevel / Seeds:0.0} → {x.ExitLevel / Seeds:0.0} | {x.Gold / Seeds:0} |");
            }

            var defaultLoadout = skills.Take(SkillLoadout.SlotCount).ToList();
            float compareHit = 0f, compareSwings = 0f, compareHuskLife = 0f, compareBossLife = 0f;
            var compareLevel = 1;
            foreach (var typical in new[] { false, true })
            {
                text.AppendLine();
                text.AppendLine(typical ? "## Combat, typical gear (Magic, average T5 affixes)" : "## Combat, floor gear (Common, base stats only)");
                text.AppendLine();
                text.AppendLine("| Depth | Char level | Hit | Single-target DPS | Crowd DPS | Husk dies in | Ghoul dies in | Life (effective) | Husk hit | Ghoul slam | Arrow | Dies to 5 husks in | Kills 5 husks in | Fight time for the level | Boss dies in |");
                text.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|");
                for (var d = 1; d <= DungeonRules.Depths; d++)
                {
                    var x = depths[d];
                    var charLevel = Mathf.RoundToInt(x.EntryLevel / Seeds);
                    var level = enemyLevel(d);
                    var gearLevel = d == 1 ? 0 : enemyLevel(d - 1);
                    var gear = Gear(gearLevel, typical);
                    var power = PowerScore.Evaluate(gear, charLevel);
                    // Attributes (by level) are counted; the passive tree is not, since how the points are spent is the player's.
                    var swings = PowerScore.AttacksPerSecond(gear, charLevel);
                    var hit = power.DamagePerSecond / swings;
                    // The default loadout (the class's first four skills, which an unchosen character starts with) at
                    // its cooldowns, slowed together when Focus cannot pay (SkillValue).
                    var stillness = AssumeStillness ? StanceStacks.MaxStacks * StanceStacks.StillnessDamagePerStack : 0f;
                    hit *= 1f + stillness;
                    var value = SkillValue(defaultLoadout, hit, swings, charLevel, husk.MaxLifeAt(level));
                    var single = value.Single;
                    var crowd = value.Crowd;
                    if (typical && d == DungeonRules.BossEvery)
                    {
                        compareHit = hit;
                        compareSwings = swings;
                        compareLevel = charLevel;
                        compareHuskLife = husk.MaxLifeAt(level);
                        compareBossLife = boss.MaxLifeAt(level);
                    }

                    var armor = gear.TotalArmor;
                    // The class's life by level, the typical build's Vitality points (one in five) and the gear's.
                    var maxLife = CombatFormulas.CharacterBaseLife(charLevel) + vitalityPerLevel * Mathf.Max(0, charLevel - 1) * CombatFormulas.LifePerVitality +
                                  CharacterAttributes.At(charLevel).Life + gear.TotalLifeBonus;
                    var effectiveLife = maxLife / (1f - CombatFormulas.ArmorReduction(gear.TotalArmor, charLevel));
                    var huskHit = CombatFormulas.EnemyHitOnPlayer(level, husk.DamageMultiplier, armor);
                    var ghoulHit = CombatFormulas.EnemyHitOnPlayer(level, ghoul.DamageMultiplier, armor);
                    var arrowHit = CombatFormulas.EnemyHitOnPlayer(level, archer.DamageMultiplier, armor);
                    var huskCycle = husk.AttackWindupSeconds + husk.AttackRecoverSeconds;
                    var reduction = AssumeStillness ? StanceStacks.MaxStacks * StanceStacks.StillnessReductionPerStack : 0f;
                    huskHit *= 1f - reduction;
                    ghoulHit *= 1f - reduction;
                    arrowHit *= 1f - reduction;
                    var diesTo5 = maxLife / (HusksInReach * huskHit / huskCycle);
                    var kills5 = HusksInReach * husk.MaxLifeAt(level) / crowd;
                    var fight = x.Life / Seeds / crowd;
                    var bossTime = DungeonRules.IsBossDepth(d) ? $"{boss.MaxLifeAt(level) / single:0} s" : "";

                    text.AppendLine($"| {d} | {charLevel} | {hit:0} | {single:0} | {crowd:0} | {husk.MaxLifeAt(level) / single:0.0} s | {ghoul.MaxLifeAt(level) / single:0.0} s | {maxLife:0} ({effectiveLife:0}) | {huskHit:0} | {ghoulHit:0} | {arrowHit:0} | {diesTo5:0.0} s | {kills5:0.0} s | {fight:0} s | {bossTime} |");
                }
            }

            // Every skill on its own beside the basic arrow, and some loadouts, for the typical character at depth 6.
            text.AppendLine();
            text.AppendLine($"## Skills compared (typical gear, depth {DungeonRules.BossEvery}, level {compareLevel})");
            text.AppendLine();
            text.AppendLine("Each skill alone beside the basic arrow: what it adds a second against one target and in a crowd, the Focus it spends a second, and its damage per Focus point (crowd).");
            text.AppendLine();
            text.AppendLine("| Skill | Kind | Cost | Cooldown | Adds, single | Adds, crowd | Focus a second | Crowd damage per Focus |");
            text.AppendLine("|---|---|---|---|---|---|---|---|");
            var basic = SkillValue(new List<SkillDefinition>(), compareHit, compareSwings, compareLevel, compareHuskLife);
            foreach (var skill in skills)
            {
                var alone = SkillValue(new List<SkillDefinition> { skill }, compareHit, compareSwings, compareLevel, compareHuskLife);
                var perFocus = skill.RageCost > 0f ? $"{(alone.Crowd - basic.Crowd) * skill.CooldownSeconds / skill.RageCost:0.0}" : "free";
                text.AppendLine($"| {skill.DisplayName} | {skill.Kind} | {skill.RageCost:0} | {skill.CooldownSeconds:0.#} s | {alone.Single - basic.Single:0} | {alone.Crowd - basic.Crowd:0} | {skill.RageCost / skill.CooldownSeconds:0.0} | {perFocus} |");
            }
            text.AppendLine();
            text.AppendLine("| Loadout | Single DPS | Crowd DPS | Focus share | Boss dies in |");
            text.AppendLine("|---|---|---|---|---|");
            foreach (var (name, picks) in Loadouts)
            {
                var loadout = new List<SkillDefinition>();
                foreach (var pick in picks)
                {
                    var found = skills.FirstOrDefault(x => x.DisplayName == pick);
                    if (found != null)
                        loadout.Add(found);
                }
                var v = SkillValue(loadout, compareHit, compareSwings, compareLevel, compareHuskLife);
                text.AppendLine($"| {name}: {string.Join(", ", picks)} | {v.Single:0} | {v.Crowd:0} | {v.FocusShare:P0} | {compareBossLife / v.Single:0} s |");
            }

            text.AppendLine();
            text.AppendLine($"Assumptions (the Wild Arrow, bows only): the depth tables use the default loadout (the class's first four skills); each skill in a loadout fires at its cooldown, slowed together when Focus (6 a second, 4 per basic arrow, Knockback Shot's and Hunter's Breath's gains) cannot pay; skill points are spread evenly over the loadout; in a crowd the basic arrow hits {CrowdTargetsBasic} enemy, Split Arrow up to {CrowdTargetsVolley}, Pierce Arrow {CrowdTargetsPierce}, each homing arrow 1, Explosive Arrow {CrowdTargetsSlam}, Knockback Shot {CrowdTargetsKnockback} and every Barrage arrow 1; against one target Split Arrow, Explosive Arrow and Barrage do not fire; Kill Shot fires only on a target under its threshold (the last part of a long fight, and in a crowd on a husk it overkills); Wild Frenzy runs with 3 Momentum; the character kites (no Stillness), with the typical build's stat points (one in five on each attribute); {HusksInReach} husks can reach the character at once, which a kiter mostly avoids, so 'dies to 5 husks' is the worst case; fight time is the level's total enemy life over crowd DPS, with no walking; the potion, the pet and dodging are left out.");

            Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
            File.WriteAllText(OutputPath, text.ToString());
            return text.ToString();
        }

        const float CrowdTargetsKnockback = 2f;

        // Loadouts compared in the report (display names).
        static readonly (string name, string[] picks)[] Loadouts =
        {
            ("Default", new[] { "Explosive Arrow", "Homing Arrow", "Pierce Arrow", "Split Arrow" }),
            ("Boss", new[] { "Kill Shot", "Pierce Arrow", "Homing Arrow", "Hunter's Breath" }),
            ("Crowd", new[] { "Barrage", "Explosive Arrow", "Split Arrow", "Knockback Shot" }),
            ("Frenzy", new[] { "Wild Frenzy", "Pierce Arrow", "Homing Arrow", "Split Arrow" }),
            ("New five", new[] { "Kill Shot", "Barrage", "Knockback Shot", "Wild Frenzy" }),
        };

        struct Value
        {
            public float Single, Crowd, FocusShare;
        }

        /// <summary>
        /// What a loadout adds to the basic arrow (hit is one basic arrow's damage, swings its rate): every skill at its
        /// cooldown, the spenders slowed together when Focus cannot pay, skill points spread evenly over the loadout.
        /// Hits a cast lands, by kind, against one target and in a crowd (the constants above); buffs add attack speed
        /// or Focus, not damage.
        /// </summary>
        static Value SkillValue(List<SkillDefinition> loadout, float hit, float swings, int charLevel, float huskLife)
        {
            var points = SkillLevels.EarnedPoints(charLevel) / (float)Mathf.Max(1, loadout.Count);
            var levelFactor = 1f + SkillLevels.DamagePerLevel * points;
            var speed = 0f;
            var focusIn = FocusPool.DefaultRegenPerSecond;
            var focusOut = 0f;
            foreach (var skill in loadout)
            {
                focusIn += skill.RageGain / skill.CooldownSeconds;
                focusOut += skill.RageCost / skill.CooldownSeconds;
                if (skill.Kind == SkillKind.Buff && skill.BuffAttackSpeed > 0f)
                    speed += (skill.BuffAttackSpeed + skill.BuffAttackSpeedPerMomentum * Mathf.Max(3, skill.MinMomentum)) *
                             Mathf.Min(1f, skill.DurationSeconds / skill.CooldownSeconds);
            }
            var rate = swings * (1f + speed);
            focusIn += rate * FocusPool.PerBasicHit;
            var share = focusOut > focusIn ? focusIn / focusOut : 1f;
            var value = new Value { Single = hit * rate, Crowd = hit * rate * CrowdTargetsBasic, FocusShare = share };
            foreach (var skill in loadout)
            {
                var casts = (skill.RageCost > 0f ? share : 1f) / skill.CooldownSeconds;
                var each = hit * levelFactor * skill.DamageMultiplier;
                float single, crowd;
                switch (skill.Kind)
                {
                    case SkillKind.Volley: single = 0f; crowd = each * Mathf.Min(CrowdTargetsVolley, skill.ProjectileCount); break;
                    case SkillKind.PierceShot: single = each; crowd = each * CrowdTargetsPierce; break;
                    case SkillKind.HomingShot: single = each * skill.ProjectileCount; crowd = single; break;
                    case SkillKind.ExplosiveShot: single = 0f; crowd = each * CrowdTargetsSlam; break;
                    case SkillKind.KnockbackShot: single = each; crowd = each * CrowdTargetsKnockback; break;
                    case SkillKind.Barrage: single = 0f; crowd = each * skill.ProjectileCount; break;
                    case SkillKind.KillShot:
                        // A certain crit (1.5 times, crit damage bonuses left out).
                        var execute = hit * levelFactor * skill.ExecuteMultiplier * 1.5f;
                        single = execute * skill.ExecuteThreshold;
                        crowd = Mathf.Min(execute, huskLife * skill.ExecuteThreshold);
                        break;
                    default: single = 0f; crowd = 0f; break;
                }
                value.Single += single * casts;
                value.Crowd += crowd * casts;
            }
            return value;
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
