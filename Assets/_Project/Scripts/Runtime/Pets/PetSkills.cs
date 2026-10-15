using System.Collections.Generic;

namespace ARPG
{
    /// <summary>The pets' skills (2026-10-15): four a pet, taught by Lyriel.</summary>
    public enum PetSkillId
    {
        // The wolf: bleeding and frost.
        Rend,
        Pounce,
        PackHowl,
        BloodFeast,

        // The raven: marks and lightning.
        HuntersMark,
        StormWings,
        GuidingCry,
        GildedBeak,

        // The boar: fire and bursts.
        EmberTusks,
        Trample,
        Bristle,
        GoreBurst,
    }

    public sealed class PetSkill
    {
        public PetSkillId Id;
        public PetKind Pet;
        public string Name;

        /// <summary>What it does, with {0} for its value at a rank.</summary>
        public string Text;

        /// <summary>The value a rank adds (a fraction unless the text says otherwise).</summary>
        public float PerRank;

        /// <summary>The boon it pairs with (a talent node id), named on Lyriel's sheet, or null.</summary>
        public string Pairs;
    }

    /// <summary>
    /// The pets' skills (the owner, 2026-10-15: "Make it so that you can teach them skills that persist and help you in the
    /// dungeon, make the skills synergise with builds"; answers: Lyriel teaches them for coins, and every skill a pet has
    /// learned is always on). Four a pet, three ranks each, each made to pair with boons. The numbers are Claude's, to
    /// review (Docs/08). Pure.
    /// </summary>
    public static class PetSkillRules
    {
        public const int MaxRanks = 3;

        // The timed skills: how often they come round.
        public const float PounceSeconds = 8f;
        public const float PounceReach = 6f;
        public const float HowlSeconds = 12f;
        public const float HowlLasts = 5f;
        public const float TrampleSeconds = 8f;
        public const float TrampleLength = 4f;
        public const float BleedSeconds = 3f;
        public const float BurnSeconds = 3f;
        public const float MarkSeconds = 4f;
        public const float FeastRadius = 4f;
        public const float BurstRadius = 2f;

        public static readonly IReadOnlyList<PetSkill> All = new List<PetSkill>
        {
            new PetSkill { Id = PetSkillId.Rend, Pet = PetKind.Wolf, Name = "Rend", PerRank = 0.05f, Pairs = "bleeding_arrows",
                Text = "Bites open a bleed, and her arrows deal {0} more to bleeding enemies." },
            new PetSkill { Id = PetSkillId.Pounce, Pet = PetKind.Wolf, Name = "Pounce", PerRank = 1f, Pairs = "freezing_arrow",
                Text = "Every 8 seconds the wolf leaps at an enemy up to 6 away, its bite chilling it {0}." },
            new PetSkill { Id = PetSkillId.PackHowl, Pet = PetKind.Wolf, Name = "Pack Howl", PerRank = 0.08f, Pairs = "extra_shot",
                Text = "Every 12 seconds in a fight the wolf howls: {0} attack speed for 5 seconds." },
            new PetSkill { Id = PetSkillId.BloodFeast, Pet = PetKind.Wolf, Name = "Blood Feast", PerRank = 0.01f, Pairs = "bleeding_arrows",
                Text = "A bleeding enemy dying near the wolf heals her and the wolf {0} of their life." },

            new PetSkill { Id = PetSkillId.HuntersMark, Pet = PetKind.Raven, Name = "Hunter's Mark", PerRank = 0.06f, Pairs = "piercing_arrows",
                Text = "Pecks mark the enemy for 4 seconds; her arrows deal {0} more to the marked." },
            new PetSkill { Id = PetSkillId.StormWings, Pet = PetKind.Raven, Name = "Storm Wings", PerRank = 1f, Pairs = "chain_lightning",
                Text = "Pecks arc lightning to {0} near the target, one more with Chain Lightning." },
            new PetSkill { Id = PetSkillId.GuidingCry, Pet = PetKind.Raven, Name = "Guiding Cry", PerRank = 1f, Pairs = "ricochet",
                Text = "An arrow that hits the raven's target leaps on {0}." },
            new PetSkill { Id = PetSkillId.GildedBeak, Pet = PetKind.Raven, Name = "Gilded Beak", PerRank = 0.1f, Pairs = null,
                Text = "The raven finds glints: {0} more coins." },

            new PetSkill { Id = PetSkillId.EmberTusks, Pet = PetKind.Boar, Name = "Ember Tusks", PerRank = 0.2f, Pairs = "fire_arrow",
                Text = "Gores set enemies burning, and deal {0} more to the burning." },
            new PetSkill { Id = PetSkillId.Trample, Pet = PetKind.Boar, Name = "Trample", PerRank = 0.4f, Pairs = "knockback_shot",
                Text = "Every 8 seconds the boar charges 4 through the enemies before it, throwing them aside for {0} of a gore." },
            new PetSkill { Id = PetSkillId.Bristle, Pet = PetKind.Boar, Name = "Bristle", PerRank = 0.15f, Pairs = null,
                Text = "Enemies beside the boar are {0} likelier to strike it instead of her, and it takes 10 percent less a rank." },
            new PetSkill { Id = PetSkillId.GoreBurst, Pet = PetKind.Boar, Name = "Gore Burst", PerRank = 0.5f, Pairs = "corpse_burst",
                Text = "An enemy the boar kills bursts for {0} of the gore, wider with Corpse Burst." },
        };

        public static PetSkill Get(PetSkillId id)
        {
            foreach (var skill in All)
                if (skill.Id == id)
                    return skill;
            return null;
        }

        public static IEnumerable<PetSkill> For(PetKind pet)
        {
            foreach (var skill in All)
                if (skill.Pet == pet)
                    yield return skill;
        }

        /// <summary>Lyriel's price for the next rank after <paramref name="rank"/>: 600, 2,400, 5,400.</summary>
        public static int Cost(int rank) => 600 * (rank + 1) * (rank + 1);

        /// <summary>A skill's text at a rank (the first when none).</summary>
        public static string Describe(PetSkill skill, int rank)
        {
            var shown = System.Math.Max(1, rank);
            var value = skill.PerRank * shown;
            string text;
            switch (skill.Id)
            {
                case PetSkillId.Pounce:
                    text = shown == 1 ? "once" : shown + " times";
                    break;
                case PetSkillId.StormWings:
                    text = shown == 1 ? "one enemy" : shown + " enemies";
                    break;
                case PetSkillId.GuidingCry:
                    text = shown == 1 ? "once more" : shown + " more times";
                    break;
                default:
                    text = $"+{value * 100f:0.#} percent";
                    break;
            }
            return string.Format(skill.Text, text);
        }
    }
}
