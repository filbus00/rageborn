using System.Collections.Generic;

namespace ARPG
{
    /// <summary>The wolf's skills (2026-10-15; boons since 2026-11-07).</summary>
    public enum PetSkillId
    {
        Rend,
        Pounce,
        PackHowl,
        BloodFeast,
        PackHunt,
    }

    public sealed class PetSkill
    {
        public PetSkillId Id;
        public string Name;

        /// <summary>The boon (a talent node id) whose ranks are the skill's.</summary>
        public string Boon;

        /// <summary>What it does, with {0} for its value at a rank.</summary>
        public string Text;

        /// <summary>The value a rank adds (a fraction unless the text says otherwise).</summary>
        public float PerRank;
    }

    /// <summary>
    /// The wolf's skills. Taught by Lyriel for coins from 2026-10-15; since 2026-11-07 (the owner: "Remove all other pets
    /// and rework the beastmaster", then "Wolf-centred boons") they are boons of the Beast Mastery tree, taken in a run like
    /// any other, three ranks each (<see cref="TalentRules"/>, the nodes named by <see cref="PetSkill.Boon"/>). The numbers
    /// are Claude's, to review (Docs/08). Pure.
    /// </summary>
    public static class PetSkillRules
    {
        public const int MaxRanks = 3;

        // The timed skills: how often they come round.
        public const float PounceSeconds = 8f;
        public const float PounceReach = 6f;
        public const float HowlSeconds = 12f;
        public const float HowlLasts = 5f;
        public const float BleedSeconds = 3f;
        public const float FeastRadius = 4f;

        public static readonly IReadOnlyList<PetSkill> All = new List<PetSkill>
        {
            new PetSkill { Id = PetSkillId.Rend, Name = "Rend", Boon = "wolf_rend", PerRank = 0.1f,
                Text = "The wolf's bites open a bleed, and her arrows deal {0} more to bleeding enemies." },
            new PetSkill { Id = PetSkillId.Pounce, Name = "Pounce", Boon = "wolf_pounce", PerRank = 1f,
                Text = "Every 8 seconds the wolf leaps at an enemy up to 6 away, its bite chilling it {0}." },
            new PetSkill { Id = PetSkillId.PackHowl, Name = "Pack Howl", Boon = "wolf_howl", PerRank = 0.1f,
                Text = "Every 12 seconds in a fight the wolf howls: {0} attack speed for 5 seconds." },
            new PetSkill { Id = PetSkillId.BloodFeast, Name = "Blood Feast", Boon = "wolf_feast", PerRank = 0.02f,
                Text = "A bleeding enemy dying near the wolf heals her and the wolf {0} of their life." },
            new PetSkill { Id = PetSkillId.PackHunt, Name = "Pack Hunt", Boon = "wolf_pack_hunt", PerRank = 0.15f,
                Text = "Her arrows deal {0} more to the enemy the wolf is fighting." },
        };

        public static PetSkill Get(PetSkillId id)
        {
            foreach (var skill in All)
                if (skill.Id == id)
                    return skill;
            return null;
        }

        /// <summary>A skill's text at a rank (the first when none).</summary>
        public static string Describe(PetSkill skill, int rank)
        {
            var shown = System.Math.Max(1, rank);
            var text = skill.Id == PetSkillId.Pounce
                ? (shown == 1 ? "once" : shown + " times")
                : $"+{skill.PerRank * shown * 100f:0.#} percent";
            return string.Format(skill.Text, text);
        }
    }
}
