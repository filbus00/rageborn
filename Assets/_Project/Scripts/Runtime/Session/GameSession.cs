using System.Collections.Generic;
using UnityEngine;

namespace ARPG
{
    /// <summary>What the character has equipped. Only a weapon exists so far; item level 0 means unarmed.</summary>
    public readonly struct EquipmentState
    {
        public EquipmentState(int weaponItemLevel) => WeaponItemLevel = Mathf.Max(0, weaponItemLevel);

        /// <summary>Item level of the weapon, or 0 with no weapon.</summary>
        public int WeaponItemLevel { get; }

        public bool IsEmpty => WeaponItemLevel <= 0;

        /// <summary>The gear a new character starts with. A tuning value: one level 1 weapon.</summary>
        public static EquipmentState Starting => new EquipmentState(1);

        public static EquipmentState Empty => new EquipmentState(0);
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
    /// Everything that has to survive changing scene within one game session: equipment, corpses, which enemies were
    /// killed and current life. Pure, so it can be tested; <see cref="Current"/> holds the live one. A future sleep
    /// mechanic resets the session (Docs/08-production.md, open question 5), and saving to disk builds on this.
    /// </summary>
    public sealed class GameSession
    {
        readonly List<Corpse> corpses = new List<Corpse>();
        readonly Dictionary<string, HashSet<int>> killed = new Dictionary<string, HashSet<int>>();

        static GameSession current = new GameSession();

        /// <summary>The live session. Replaced at the start of every play, so nothing leaks between editor plays.</summary>
        public static GameSession Current => current;

        public EquipmentState Equipment { get; private set; } = EquipmentState.Starting;

        public IReadOnlyList<Corpse> Corpses => corpses;

        /// <summary>The character's life as a fraction of its maximum, carried between scenes.</summary>
        public float LifeFraction { get; set; } = 1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetForNewPlay() => current = new GameSession();

        /// <summary>Equips gear, replacing what the character wears.</summary>
        public void Equip(EquipmentState equipment) => Equipment = equipment;

        /// <summary>Records that a pack's member was killed. It stays dead while the session lasts.</summary>
        public void RecordKill(string packKey, int slot)
        {
            if (!killed.TryGetValue(packKey, out var slots))
            {
                slots = new HashSet<int>();
                killed[packKey] = slots;
            }
            slots.Add(slot);
        }

        public bool IsKilled(string packKey, int slot) => killed.TryGetValue(packKey, out var slots) && slots.Contains(slot);

        /// <summary>
        /// The character dies: its equipped gear stays behind as a corpse where it fell (Docs/01-core-gameplay.md).
        /// Returns the new corpse, or null when nothing was equipped, since a corpse would hold nothing.
        /// An earlier corpse is never touched, and corpses never expire.
        /// </summary>
        public Corpse Die(string levelId, Vector2 groundPosition)
        {
            LifeFraction = 1f;
            if (Equipment.IsEmpty)
                return null;

            var corpse = new Corpse(levelId, groundPosition, Equipment);
            corpses.Add(corpse);
            Equipment = EquipmentState.Empty;
            return corpse;
        }

        /// <summary>Picks up a corpse's gear. It is equipped unless the character already wears a better weapon.</summary>
        public bool Retrieve(Corpse corpse)
        {
            if (!corpses.Remove(corpse))
                return false;

            if (corpse.Gear.WeaponItemLevel > Equipment.WeaponItemLevel)
                Equipment = corpse.Gear;
            return true;
        }
    }
}
