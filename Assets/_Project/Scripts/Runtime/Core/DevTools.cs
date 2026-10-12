using System.Collections.Generic;
using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// Development builds only (the owner, 2026-10-13: "give me a button in settings that equips full loadout of
    /// legendaries, give me max level and full skills"): what the Settings tab's Developer rows do. Pure but for the
    /// session they act on.
    /// </summary>
    public static class DevTools
    {
        public static bool Enabled => Debug.isDebugBuild;

        static readonly System.Random Random = new System.Random();

        /// <summary>A legendary in every place that has one (the first of each kind in the table: a bow, a quiver, a
        /// helm, a chest, gloves, boots, a belt, an amulet and a ring), at her level; what she wore goes to the backpack
        /// while it has room. Returns how many were put on.</summary>
        public static int EquipLegendaries(GameSession session)
        {
            var gear = session.Equipment;
            var filled = new HashSet<ItemSlot>();
            var displaced = new List<Item>();
            var level = Mathf.Max(Legendaries.MinItemLevel, session.Level);
            foreach (var legendary in Legendaries.All)
                foreach (var place in EquipmentState.PlacesFor(legendary.Slot))
                {
                    if (!filled.Add(place))
                        continue;
                    gear = gear.Equip(place, Legendaries.Create(legendary.Id, level, Random), displaced);
                    break;
                }
            session.Equip(gear);
            foreach (var item in displaced)
                session.PickUp(item);
            return filled.Count;
        }

        /// <summary>Level 60, every talent at its full rank, and the stat points not yet spent spread evenly (2026-10-13),
        /// each to whichever stat has the fewest.</summary>
        public static void MaxCharacter(GameSession session)
        {
            while (!session.Progress.IsMaxLevel)
                session.GrantExperience(session.Progress.XpToNextLevel - session.Progress.Xp);
            session.Talents.MaxAll();
            var points = session.Attributes;
            var stats = (Attribute[])System.Enum.GetValues(typeof(Attribute));
            while (points.Available(session.Level) > 0)
            {
                var least = stats[0];
                foreach (var stat in stats)
                    if (points.Spent(stat) < points.Spent(least))
                        least = stat;
                if (!points.Spend(least, session.Level))
                    break;
            }
        }
    }
}
