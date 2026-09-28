namespace ARPG
{
    /// <summary>What a trigger condition looks at, gathered once per skill check.</summary>
    public struct TriggerContext
    {
        /// <summary>Enemies inside the skill's own area (its range).</summary>
        public int EnemiesInArea;

        /// <summary>Whether an Elite or a boss is inside the skill's area.</summary>
        public bool ElitePresent;

        public float LifeFraction;

        /// <summary>How long the character has stood still.</summary>
        public float StillSeconds;

        public bool Moving;
    }

    /// <summary>
    /// The trigger conditions a slot can use instead of its skill's own (Docs/01-core-gameplay.md, the table of
    /// conditions; Q15: each skill offers two besides its own). Pure so it is tested. The skill's own condition and the
    /// need for a valid target or area are checked by <see cref="PlayerCombat"/>.
    /// </summary>
    public static class SkillTriggerRules
    {
        /// <summary>Docs/01: "Standing" means still for 0.4 s.</summary>
        public const float StandingSeconds = 0.4f;

        public static bool Passes(SkillTrigger trigger, TriggerContext context)
        {
            switch (trigger)
            {
                case SkillTrigger.Always: return true;
                case SkillTrigger.EnemiesThreePlus: return context.EnemiesInArea >= 3;
                case SkillTrigger.ElitePresent: return context.ElitePresent;
                case SkillTrigger.LifeBelowHalf: return context.LifeFraction < 0.5f;
                case SkillTrigger.LifeAboveHalf: return context.LifeFraction > 0.5f;
                case SkillTrigger.Standing: return context.StillSeconds >= StandingSeconds;
                case SkillTrigger.Moving: return context.Moving;
                default: return false;
            }
        }

        /// <summary>The words the loadout shows for a trigger; the default one shows the skill's own condition.</summary>
        public static string Label(SkillTrigger trigger, string ownCondition)
        {
            switch (trigger)
            {
                case SkillTrigger.Always: return "Always";
                case SkillTrigger.EnemiesThreePlus: return "3+ enemies";
                case SkillTrigger.ElitePresent: return "Elite present";
                case SkillTrigger.LifeBelowHalf: return "Life below 50%";
                case SkillTrigger.LifeAboveHalf: return "Life above 50%";
                case SkillTrigger.Standing: return "Standing";
                case SkillTrigger.Moving: return "Moving";
                default: return ownCondition;
            }
        }
    }
}
