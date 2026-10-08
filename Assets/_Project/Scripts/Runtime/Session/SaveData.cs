using System;
using System.Collections.Generic;

namespace ARPG
{
    /// <summary>
    /// The on-disk shape of one character's save (Docs/07-technical.md, save system): plain fields for
    /// <see cref="UnityEngine.JsonUtility"/>. Enums are stored by name, not number, so adding or reordering an
    /// <see cref="ItemSlot"/> or <see cref="AffixId"/> cannot silently turn a saved helm into something else.
    /// Bump <see cref="CurrentVersion"/> and add a step to <see cref="SaveCodec"/>'s migrations whenever the shape or
    /// the meaning of a field changes.
    /// </summary>
    [Serializable]
    public sealed class SaveData
    {
        /// <summary>1: the first. 2: adds level and experience. 3: adds potion charges. 4: adds the dungeon seed and
        /// opened chests. 5: adds the dungeon generator version. 6: adds salvage materials and each item's reforge and
        /// temper counts. 7: adds what the character has been taught (onboarding) and play time. 8: adds activated
        /// waypoints, the Portal Tome and an open portal. 9: adds the skill loadout. 10: adds skill levels. 11: adds the passive tree.
        /// 12: bows only, the Wild Arrow (2026-09-30): adds spent stat points and pets. A save older than 12 is the retired
        /// Wrathborn's and is set aside for a clean start (the owner's decision), though it still parses. 13: adds each
        /// item's named legendary and the legendaries seen (the Codex). 14: the deepest depth and the stash. 15: the talent
        /// trees' ranks (2026-10-07; skill levels and the loadout are kept in the file but no longer used).</summary>
        public const int CurrentVersion = 16;

        public int version;

        /// <summary>The version the file was written in before migration, set when it is read; not saved.</summary>
        [NonSerialized] public int readFromVersion;

        /// <summary>When it was written, in Unix milliseconds. iCloud conflict resolution will compare these.</summary>
        public long savedAtUnixMs;

        public int gold;
        public float lifeFraction = 1f;
        public int killsSinceLegendary;
        public int level = 1;

        /// <summary>XP toward the next level, not the lifetime total.</summary>
        public int experience;

        public int potionCharges = AutoPotion.MaxCharges;
        public int potionKillProgress;

        /// <summary>Seeds every dungeon level of the game session, so each keeps its layout.</summary>
        public int dungeonSeed;

        /// <summary>The <see cref="DungeonGenerator.Version"/> that built the levels the kills and chests refer to.</summary>
        public int dungeonVersion;
        public List<ItemData> equipped = new List<ItemData>();
        public List<ItemData> backpack = new List<ItemData>();
        public List<CorpseData> corpses = new List<CorpseData>();
        public List<KilledPackData> killed = new List<KilledPackData>();

        /// <summary>Opened chests, by level and chest ("Dungeon 2/Chest 0").</summary>
        public List<string> openedChests = new List<string>();

        /// <summary>Salvage materials held, by name. A material with none is left out.</summary>
        public List<MaterialData> materials = new List<MaterialData>();

        /// <summary>Onboarding (Docs/06): what has been taught, and play time for the guaranteed first Legendary.</summary>
        public bool stickTaught;
        public bool forgeIntroduced;
        public bool seenLegendary;
        public bool legendaryHintShown;
        public float playSeconds;

        /// <summary>Depths whose waypoint is activated.</summary>
        public List<int> waypoints = new List<int>();
        public bool hasPortalTome;

        /// <summary>The open portal's depth (0 for none) and ground position.</summary>
        public int portalDepth;
        public float portalX;
        public float portalY;

        /// <summary>The four skill slots by skill asset name (empty for none), each slot's trigger by name, and whether
        /// the player has set them (until then the loadout fills itself).</summary>
        public List<string> loadoutSkills = new List<string>();
        public List<string> loadoutTriggers = new List<string>();
        public bool loadoutChosen;

        /// <summary>Skills raised above level 1, by asset name. Points are not stored: they follow from the level.</summary>
        public List<SkillLevelData> skillLevels = new List<SkillLevelData>();

        /// <summary>Passive nodes bought, by id, and the keystone in effect (empty for none).</summary>
        public List<string> passiveNodes = new List<string>();
        public string activeKeystone = "";

        /// <summary>Stat points spent on each attribute, in <see cref="Attribute"/> order (Strength, Agility, Vitality,
        /// Speed, Focus). Unspent points are not stored: they follow from the level.</summary>
        public List<int> attributePoints = new List<int>();

        /// <summary>Pets bought, by kind name, the one following the character (empty for none), and its rules in
        /// order, by name.</summary>
        public List<string> pets = new List<string>();
        public string activePet = "";
        public List<string> petRules = new List<string>();

        /// <summary>The named legendaries the character has found, by name (the Codex, Docs/03).</summary>
        public List<string> codex = new List<string>();

        // Version 14 (2026-10-05, one town above 24 levels): the deepest depth reached, by which newcomers arrive in
        // town, and the Stash Keeper's chest.
        public int deepestDepth;
        public List<ItemData> stash = new List<ItemData>();

        // Version 15 (2026-10-07): the ranks bought in the talent trees, by talent id; she starts with only her bow.
        public List<TalentData> talents = new List<TalentData>();

        // Version 16 (2026-10-08): the quests taken, each with its current step and whether it is done.
        public List<QuestData> quests = new List<QuestData>();
    }

    [Serializable]
    public sealed class QuestData
    {
        public string id;
        public int step;
        public bool done;
    }

    [Serializable]
    public sealed class TalentData
    {
        public string id;
        public int rank;
    }

    [Serializable]
    public sealed class SkillLevelData
    {
        public string skill;
        public int level;
    }

    [Serializable]
    public sealed class MaterialData
    {
        public string name;
        public int amount;
    }

    [Serializable]
    public sealed class ItemData
    {
        public string slot;
        public string rarity;
        public int itemLevel;
        public List<AffixData> affixes = new List<AffixData>();

        /// <summary>Times the Forge reforged or tempered the item; both raise or limit later Forge actions.</summary>
        public int reforges;
        public int tempers;

        /// <summary>The named legendary it is, by name (empty for none).</summary>
        public string legendary = "";
    }

    [Serializable]
    public sealed class AffixData
    {
        public string id;
        public int tier;
        public float value;
    }

    [Serializable]
    public sealed class CorpseData
    {
        public string levelId;
        public float x;
        public float y;
        public List<ItemData> gear = new List<ItemData>();
    }

    [Serializable]
    public sealed class KilledPackData
    {
        public string packKey;
        public List<int> slots = new List<int>();
    }
}
