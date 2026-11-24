namespace ARPG
{
    /// <summary>A skill the double tap fires (Wild Volley first; more can join it).</summary>
    public sealed class TapSkill
    {
        public readonly string Id;
        public readonly string Name;
        public readonly string Text;
        public readonly string EvolvedText;

        public TapSkill(string id, string name, string text, string evolvedText)
        {
            Id = id;
            Name = name;
            Text = text;
            EvolvedText = evolvedText;
        }
    }

    /// <summary>
    /// The double tap's skills (2026-11-24, the owner: "make the boss drop a skill book that is picked up and small animation
    /// when the book is picked up and the skill learned. Also show the skill in the bag menu. Prepare the menu so that the
    /// player can select which skill to use when they are learned. They will all be triggered by double tapping"). A boss
    /// drops the book of the first skill the company has not learned; walking over it teaches it for good (every archer of
    /// the company after has it). One is chosen at a time and the double tap fires it. Pure.
    /// </summary>
    public static class TapSkillRules
    {
        public const string WildVolley = "wild_volley";

        public static readonly TapSkill[] All =
        {
            new TapSkill(WildVolley, "Wild Volley",
                "A fan of seven arrows at the nearest enemies. Ready again after 4 seconds.",
                "Evolved: nine arrows, then a rain of arrows on the target and thorny ground that slows."),
        };

        public static TapSkill Get(string id)
        {
            foreach (var skill in All)
                if (skill.Id == id)
                    return skill;
            return null;
        }

        /// <summary>The book a boss drops: the first skill the company has not learned, or null when it knows them all.</summary>
        public static string BookFor(CompanyRecord company)
        {
            if (company == null)
                return null;
            foreach (var skill in All)
                if (!company.TapSkills.Contains(skill.Id))
                    return skill.Id;
            return null;
        }
    }
}
