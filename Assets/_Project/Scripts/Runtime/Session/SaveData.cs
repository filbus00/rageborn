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
        /// temper counts.</summary>
        public const int CurrentVersion = 6;

        public int version;

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
