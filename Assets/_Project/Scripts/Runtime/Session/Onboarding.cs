using System;

namespace ARPG
{
    /// <summary>The one-time hints Docs/06-ui-ux.md's onboarding calls for, as the game shows them.</summary>
    public enum OnboardingHint
    {
        None,

        /// <summary>After the first Rare drop: the Forge is introduced.</summary>
        Forge,

        /// <summary>The first Legendary: a short walk through comparing and equipping it.</summary>
        Legendary,
    }

    /// <summary>
    /// What a character has been taught, from Docs/06-ui-ux.md (onboarding): the stick by a ghost thumb for 5 seconds on
    /// a new character's first arrival in town, the Forge after the first Rare, and the first Legendary guaranteed at
    /// minute 20 of play, from the next elite killed, with a short hint on comparing and equipping it. A Legendary found
    /// before then makes the guarantee moot. Play time counts only in the dungeon and town scenes while the game is not
    /// paused. Saved with the character. Pure.
    /// </summary>
    public sealed class Onboarding
    {
        /// <summary>Docs: the stick is taught for 5 seconds only.</summary>
        public const float StickLessonSeconds = 5f;

        /// <summary>Docs: the first legendary drop is guaranteed at minute 20 of play.</summary>
        public const float GuaranteedLegendarySeconds = 20f * 60f;

        public bool StickTaught { get; private set; }
        public bool ForgeIntroduced { get; private set; }
        public bool SeenLegendary { get; private set; }
        public bool LegendaryHintShown { get; private set; }
        public float PlaySeconds { get; private set; }

        /// <summary>Raised on every change a save must capture (play time is left out: it changes every frame).</summary>
        public event Action Changed;

        public Onboarding()
        {
        }

        /// <summary>Everything as a save held it.</summary>
        public Onboarding(bool stickTaught, bool forgeIntroduced, bool seenLegendary, bool legendaryHintShown, float playSeconds)
        {
            StickTaught = stickTaught;
            ForgeIntroduced = forgeIntroduced;
            SeenLegendary = seenLegendary;
            LegendaryHintShown = legendaryHintShown;
            PlaySeconds = Math.Max(0f, playSeconds);
        }

        /// <summary>Set when the guaranteed Legendary has dropped, so a pack of elites does not all drop one before it is
        /// picked up. Not saved: if it is left on the ground and lost, a later session tries again.</summary>
        public bool GuaranteeDropped { get; private set; }

        /// <summary>Whether the guaranteed Legendary should drop from the next elite: 20 minutes played and none seen.</summary>
        public bool LegendaryDue => !SeenLegendary && !GuaranteeDropped && PlaySeconds >= GuaranteedLegendarySeconds;

        public void MarkGuaranteeDropped() => GuaranteeDropped = true;

        public void Tick(float deltaSeconds)
        {
            if (deltaSeconds > 0f)
                PlaySeconds += deltaSeconds;
        }

        public void MarkStickTaught()
        {
            if (StickTaught)
                return;
            StickTaught = true;
            Changed?.Invoke();
        }

        /// <summary>
        /// Notes an item that dropped or was picked up, and returns the hint to show now, if any: the Forge on the
        /// first Rare or better, the Legendary walk-through on the first Legendary. Each shows once.
        /// </summary>
        public OnboardingHint OnItemFound(Item item)
        {
            if (item == null)
                return OnboardingHint.None;

            if (item.Rarity == ItemRarity.Legendary)
            {
                var first = !LegendaryHintShown;
                SeenLegendary = true;
                LegendaryHintShown = true;
                // A first Legendary is also the first Rare or better: the Forge can wait for the next one.
                if (first)
                {
                    Changed?.Invoke();
                    return OnboardingHint.Legendary;
                }
            }

            if (item.Rarity >= ItemRarity.Rare && !ForgeIntroduced)
            {
                ForgeIntroduced = true;
                Changed?.Invoke();
                return OnboardingHint.Forge;
            }
            return OnboardingHint.None;
        }
    }
}
