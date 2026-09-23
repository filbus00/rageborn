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
        public const int CurrentVersion = 1;

        public int version;

        /// <summary>When it was written, in Unix milliseconds. iCloud conflict resolution will compare these.</summary>
        public long savedAtUnixMs;

        public int gold;
        public float lifeFraction = 1f;
        public int killsSinceLegendary;
        public List<ItemData> equipped = new List<ItemData>();
        public List<ItemData> backpack = new List<ItemData>();
        public List<CorpseData> corpses = new List<CorpseData>();
        public List<KilledPackData> killed = new List<KilledPackData>();
    }

    [Serializable]
    public sealed class ItemData
    {
        public string slot;
        public string rarity;
        public int itemLevel;
        public List<AffixData> affixes = new List<AffixData>();
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
