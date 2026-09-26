using System;
using System.Collections.Generic;
using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// Turns a <see cref="GameSession"/> into <see cref="SaveData"/> and JSON and back. Pure, no file access (that is
    /// <see cref="SaveStore"/>). What is saved: equipment, the backpack, gold, life, level and XP, potion charges,
    /// corpses, killed pack members, opened chests, the dungeon seed, salvage materials and the bad luck counter. The loot generator's own state is not: a loaded session gets a fresh seed.
    /// </summary>
    public static class SaveCodec
    {
        // One step per old version: Migrations[v] upgrades a version v save to v + 1 (the loop then bumps the version
        // number). Index 0 is unused. Each step comes with a test that loads a file written in the old version.
        static readonly Func<SaveData, SaveData>[] Migrations =
        {
            null,
            MigrateFrom1, // 1 to 2: level and experience did not exist, so the character was level 1.
            MigrateFrom2, // 2 to 3: potions did not exist; start with full charges.
            MigrateFrom3, // 3 to 4: no dungeon existed; give the session a dungeon seed.
            MigrateFrom4, // 4 to 5: the generator version was not saved; 0 means unknown, so dungeon records are dropped.
            MigrateFrom5, // 5 to 6: the Forge did not exist; no materials, and no item was ever reforged or tempered.
            MigrateFrom6, // 6 to 7: onboarding did not exist; an existing character needs no lessons (see the step).
        };

        static SaveData MigrateFrom1(SaveData data)
        {
            data.level = 1;
            data.experience = 0;
            return data;
        }

        static SaveData MigrateFrom2(SaveData data)
        {
            data.potionCharges = AutoPotion.MaxCharges;
            data.potionKillProgress = 0;
            return data;
        }

        static SaveData MigrateFrom3(SaveData data)
        {
            // Any number will do, since no dungeon level was ever visited; take it from the save time so it differs
            // between saves.
            data.dungeonSeed = DungeonRules.LevelSeed((int)(data.savedAtUnixMs ^ (data.savedAtUnixMs >> 32)), 0);
            data.openedChests = new List<string>();
            return data;
        }

        static SaveData MigrateFrom4(SaveData data)
        {
            data.dungeonVersion = 0;
            return data;
        }

        static SaveData MigrateFrom5(SaveData data)
        {
            data.materials = new List<MaterialData>();
            foreach (var item in AllItems(data))
                item.reforges = item.tempers = 0;
            return data;
        }

        // A character from before onboarding has played already: it knows the stick and has met the smith. Its first
        // Legendary is only guaranteed if it has never had one, counting play time from now.
        static SaveData MigrateFrom6(SaveData data)
        {
            data.stickTaught = true;
            data.forgeIntroduced = true;
            data.seenLegendary = false;
            foreach (var item in AllItems(data))
                if (item.rarity == nameof(ItemRarity.Legendary))
                    data.seenLegendary = true;
            data.legendaryHintShown = data.seenLegendary;
            data.playSeconds = 0f;
            return data;
        }

        static IEnumerable<ItemData> AllItems(SaveData data)
        {
            foreach (var item in data.equipped ?? new List<ItemData>())
                yield return item;
            foreach (var item in data.backpack ?? new List<ItemData>())
                yield return item;
            foreach (var corpse in data.corpses ?? new List<CorpseData>())
                foreach (var item in corpse.gear ?? new List<ItemData>())
                    yield return item;
        }

        public static SaveData Capture(GameSession session, long savedAtUnixMs)
        {
            var data = new SaveData
            {
                version = SaveData.CurrentVersion,
                savedAtUnixMs = savedAtUnixMs,
                gold = session.Gold,
                lifeFraction = session.LifeFraction,
                killsSinceLegendary = session.Loot.KillsSinceLegendary,
                level = session.Progress.Level,
                experience = session.Progress.Xp,
                potionCharges = session.Potion.Charges,
                potionKillProgress = session.Potion.KillProgress,
                dungeonSeed = session.DungeonSeed,
                dungeonVersion = session.DungeonVersion,
                stickTaught = session.Onboarding.StickTaught,
                forgeIntroduced = session.Onboarding.ForgeIntroduced,
                seenLegendary = session.Onboarding.SeenLegendary,
                legendaryHintShown = session.Onboarding.LegendaryHintShown,
                playSeconds = session.Onboarding.PlaySeconds,
                equipped = CaptureEquipment(session.Equipment),
            };

            foreach (var item in session.Inventory.Items)
                data.backpack.Add(CaptureItem(item));

            foreach (var corpse in session.Corpses)
                data.corpses.Add(new CorpseData
                {
                    levelId = corpse.LevelId,
                    x = corpse.GroundPosition.x,
                    y = corpse.GroundPosition.y,
                    gear = CaptureEquipment(corpse.Gear),
                });

            foreach (CraftingMaterial material in Enum.GetValues(typeof(CraftingMaterial)))
                if (session.Materials(material) > 0)
                    data.materials.Add(new MaterialData { name = material.ToString(), amount = session.Materials(material) });

            data.openedChests.AddRange(session.OpenedChests);
            data.openedChests.Sort(StringComparer.Ordinal);

            foreach (var key in session.KilledPackKeys)
            {
                var pack = new KilledPackData { packKey = key };
                pack.slots.AddRange(session.KilledSlots(key));
                pack.slots.Sort();
                data.killed.Add(pack);
            }

            return data;
        }

        /// <summary>
        /// Builds a live session from saved data. Anything that no longer makes sense (an item slot, rarity or affix
        /// this build does not know) is left out and described in <paramref name="warnings"/> rather than failing the
        /// whole load.
        /// </summary>
        public static GameSession Restore(SaveData data, int lootSeed, List<string> warnings = null)
        {
            var session = new GameSession(lootSeed, data.killsSinceLegendary);
            session.RestoreProgress(data.level, data.experience);
            session.RestorePotion(data.potionCharges, data.potionKillProgress);
            session.DungeonSeed = data.dungeonSeed;
            session.RestoreOnboarding(new Onboarding(data.stickTaught, data.forgeIntroduced, data.seenLegendary, data.legendaryHintShown, data.playSeconds));
            foreach (var chest in data.openedChests)
                session.RecordOpened(chest);

            session.Equip(RestoreEquipment(data.equipped, warnings));

            foreach (var saved in data.backpack)
            {
                var item = RestoreItem(saved, warnings);
                if (item != null && !session.Inventory.TryAdd(item))
                    warnings?.Add($"The backpack is full, dropped {item}.");
            }

            session.AddGold(data.gold);

            foreach (var material in data.materials)
            {
                if (TryParseEnum(material.name, out CraftingMaterial parsed))
                    session.RestoreMaterial(parsed, material.amount);
                else
                    warnings?.Add($"Left out an unknown material ({material.name}).");
            }

            // A saved character is alive. A zero would come back as a character that can never die again.
            session.LifeFraction = data.lifeFraction > 0f ? Mathf.Min(data.lifeFraction, 1f) : 1f;

            foreach (var corpse in data.corpses)
            {
                var gear = RestoreEquipment(corpse.gear, warnings);
                if (!gear.IsEmpty)
                    session.RestoreCorpse(new Corpse(corpse.levelId, new Vector2(corpse.x, corpse.y), gear));
            }

            foreach (var pack in data.killed)
                foreach (var slot in pack.slots)
                    session.RecordKill(pack.packKey, slot);

            // Records made on levels an older generator built would land on the wrong packs and chests now.
            if (data.dungeonVersion != DungeonGenerator.Version && session.ForgetDungeonLevels() > 0)
                warnings?.Add($"The dungeon generator changed (version {data.dungeonVersion} to {DungeonGenerator.Version}); dungeon kills and chests were reset.");
            session.DungeonVersion = DungeonGenerator.Version;

            return session;
        }

        public static string ToJson(SaveData data) => JsonUtility.ToJson(data, prettyPrint: false);

        /// <summary>
        /// Parses and upgrades a save. Fails, with the reason in <paramref name="error"/>, for anything that is not a
        /// save (empty, truncated, not JSON) or was written by a newer version of the game, which this one cannot
        /// read without losing data.
        /// </summary>
        public static bool TryParse(string json, out SaveData data, out string error)
        {
            data = null;
            if (string.IsNullOrWhiteSpace(json))
            {
                error = "the file is empty";
                return false;
            }

            SaveData parsed;
            try
            {
                parsed = JsonUtility.FromJson<SaveData>(json);
            }
            catch (Exception e)
            {
                error = $"it is not valid JSON ({e.Message})";
                return false;
            }

            if (parsed == null || parsed.version < 1)
            {
                error = "it has no save version";
                return false;
            }
            if (parsed.version > SaveData.CurrentVersion)
            {
                error = $"it was written by a newer version of the game (save version {parsed.version}, this build reads up to {SaveData.CurrentVersion})";
                return false;
            }

            while (parsed.version < SaveData.CurrentVersion)
            {
                parsed = Migrations[parsed.version](parsed);
                parsed.version++;
            }

            // JsonUtility leaves a missing list null.
            parsed.equipped ??= new List<ItemData>();
            parsed.backpack ??= new List<ItemData>();
            parsed.corpses ??= new List<CorpseData>();
            parsed.killed ??= new List<KilledPackData>();
            parsed.openedChests ??= new List<string>();
            parsed.materials ??= new List<MaterialData>();

            data = parsed;
            error = null;
            return true;
        }

        static List<ItemData> CaptureEquipment(EquipmentState equipment)
        {
            var list = new List<ItemData>();
            foreach (ItemSlot slot in Enum.GetValues(typeof(ItemSlot)))
            {
                var item = equipment.Get(slot);
                if (item != null)
                    list.Add(CaptureItem(item));
            }
            return list;
        }

        static ItemData CaptureItem(Item item)
        {
            var data = new ItemData
            {
                slot = item.Slot.ToString(),
                rarity = item.Rarity.ToString(),
                itemLevel = item.ItemLevel,
                reforges = item.Reforges,
                tempers = item.Tempers,
            };
            foreach (var affix in item.Affixes)
                data.affixes.Add(new AffixData { id = affix.Id.ToString(), tier = affix.Tier, value = affix.Value });
            return data;
        }

        static EquipmentState RestoreEquipment(List<ItemData> saved, List<string> warnings)
        {
            var equipment = EquipmentState.Empty;
            if (saved == null)
                return equipment;

            foreach (var data in saved)
            {
                var item = RestoreItem(data, warnings);
                if (item == null)
                    continue;
                if (equipment.Get(item.Slot) != null)
                {
                    warnings?.Add($"Two items saved in the {item.Slot} slot, kept the first.");
                    continue;
                }
                equipment = equipment.With(item.Slot, item);
            }
            return equipment;
        }

        static Item RestoreItem(ItemData data, List<string> warnings)
        {
            if (data == null)
                return null;
            if (!TryParseEnum(data.slot, out ItemSlot slot) || !TryParseEnum(data.rarity, out ItemRarity rarity))
            {
                warnings?.Add($"Left out an item with an unknown slot or rarity ({data.slot}, {data.rarity}).");
                return null;
            }

            var affixes = new List<AffixRoll>();
            if (data.affixes != null)
            {
                foreach (var affix in data.affixes)
                {
                    if (TryParseEnum(affix.id, out AffixId id))
                        affixes.Add(new AffixRoll(id, affix.tier, affix.value));
                    else
                        warnings?.Add($"Left out an unknown affix ({affix.id}) on a {rarity} {slot}.");
                }
            }

            return new Item(slot, rarity, data.itemLevel, affixes.ToArray(), data.reforges, data.tempers);
        }

        // Enum.TryParse also accepts numbers and any defined value's number, so "7" would parse as a slot that does not
        // exist; only accept a declared name.
        static bool TryParseEnum<T>(string name, out T value) where T : struct, Enum
        {
            value = default;
            return !string.IsNullOrEmpty(name) && Enum.IsDefined(typeof(T), name) && Enum.TryParse(name, out value);
        }
    }
}
