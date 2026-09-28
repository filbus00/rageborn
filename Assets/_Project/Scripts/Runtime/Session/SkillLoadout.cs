using System;
using System.Collections.Generic;

namespace ARPG
{
    /// <summary>
    /// The four skill slots the character carries, in cast priority order, and each slot's trigger (Docs/01, auto-cast;
    /// Docs/02; the owner's decisions of 2026-09-27: all four slots open from level 1, the loadout screen from level 9, a
    /// trigger picker of each skill's own trigger and two alternatives). Skills are named by their asset names. Until the
    /// player first changes it, the loadout fills itself with the unlocked skills in the class's order, as the M1 slice
    /// always did; after that a newly unlocked skill only fills an empty slot. Pure, saved with the character.
    /// </summary>
    public sealed class SkillLoadout
    {
        public const int SlotCount = 4;

        readonly string[] slots = new string[SlotCount];
        readonly SkillTrigger[] triggers = new SkillTrigger[SlotCount];

        /// <summary>Raised whenever a slot or trigger changes.</summary>
        public event Action Changed;

        /// <summary>Whether the player has set the loadout; until then it fills itself.</summary>
        public bool Chosen { get; private set; }

        public string SkillAt(int slot) => slot >= 0 && slot < SlotCount ? slots[slot] : null;

        public SkillTrigger TriggerAt(int slot) => slot >= 0 && slot < SlotCount ? triggers[slot] : SkillTrigger.Default;

        /// <summary>The slot holding a skill, or -1.</summary>
        public int SlotOf(string skill)
        {
            if (string.IsNullOrEmpty(skill))
                return -1;
            for (var i = 0; i < SlotCount; i++)
                if (slots[i] == skill)
                    return i;
            return -1;
        }

        /// <summary>
        /// Brings the loadout up to date with the character's level: not yet chosen, the first four unlocked skills in the
        /// class's order; chosen, each skill unlocked after <paramref name="sinceLevel"/> into the first empty slot (so a
        /// slot the player emptied stays empty). Returns whether anything changed.
        /// </summary>
        public bool Fill(IReadOnlyList<(string id, int unlockLevel)> classSkills, int level, int sinceLevel = 0)
        {
            var changed = false;
            if (!Chosen)
            {
                var next = 0;
                foreach (var (id, unlockLevel) in classSkills)
                {
                    if (next >= SlotCount)
                        break;
                    if (level < unlockLevel)
                        continue;
                    if (slots[next] != id)
                    {
                        slots[next] = id;
                        triggers[next] = SkillTrigger.Default;
                        changed = true;
                    }
                    next++;
                }
                for (; next < SlotCount; next++)
                    if (slots[next] != null)
                    {
                        slots[next] = null;
                        changed = true;
                    }
            }
            else
            {
                foreach (var (id, unlockLevel) in classSkills)
                {
                    if (level < unlockLevel || unlockLevel <= sinceLevel || SlotOf(id) >= 0)
                        continue;
                    var empty = -1;
                    for (var i = 0; i < SlotCount && empty < 0; i++)
                        if (slots[i] == null)
                            empty = i;
                    if (empty < 0)
                        break;
                    slots[empty] = id;
                    triggers[empty] = SkillTrigger.Default;
                    changed = true;
                }
            }
            if (changed)
                Changed?.Invoke();
            return changed;
        }

        /// <summary>Puts a skill in a slot with its own trigger. If it was in another slot, the two slots swap.</summary>
        public void Equip(int slot, string skill)
        {
            if (slot < 0 || slot >= SlotCount)
                return;
            var from = SlotOf(skill);
            if (from == slot)
                return;
            if (from >= 0)
            {
                Swap(from, slot);
                return;
            }
            slots[slot] = skill;
            triggers[slot] = SkillTrigger.Default;
            Chosen = true;
            Changed?.Invoke();
        }

        /// <summary>Empties a slot.</summary>
        public void Clear(int slot)
        {
            if (slot < 0 || slot >= SlotCount || slots[slot] == null)
                return;
            slots[slot] = null;
            triggers[slot] = SkillTrigger.Default;
            Chosen = true;
            Changed?.Invoke();
        }

        /// <summary>Swaps two slots, each skill keeping its trigger.</summary>
        public void Swap(int a, int b)
        {
            if (a < 0 || b < 0 || a >= SlotCount || b >= SlotCount || a == b)
                return;
            (slots[a], slots[b]) = (slots[b], slots[a]);
            (triggers[a], triggers[b]) = (triggers[b], triggers[a]);
            Chosen = true;
            Changed?.Invoke();
        }

        public void SetTrigger(int slot, SkillTrigger trigger)
        {
            if (slot < 0 || slot >= SlotCount || slots[slot] == null || triggers[slot] == trigger)
                return;
            triggers[slot] = trigger;
            Chosen = true;
            Changed?.Invoke();
        }

        /// <summary>Sets everything at once, from a save. Missing entries stay empty.</summary>
        public void Restore(IReadOnlyList<string> skills, IReadOnlyList<SkillTrigger> slotTriggers, bool chosen)
        {
            for (var i = 0; i < SlotCount; i++)
            {
                slots[i] = skills != null && i < skills.Count && !string.IsNullOrEmpty(skills[i]) ? skills[i] : null;
                triggers[i] = slotTriggers != null && i < slotTriggers.Count ? slotTriggers[i] : SkillTrigger.Default;
            }
            Chosen = chosen;
        }
    }
}
