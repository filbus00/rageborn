namespace ARPG
{
    /// <summary>
    /// Wild Volley (2026-11-22, the owner: "An attack that is fired when double tapping in quick succession ... learned
    /// after killing the first boss and then maybe evolved after killing the final boss for a stage"; "Wild Volley, build
    /// it"): a double tap looses a fan of arrows at the nearest enemies. The company learns it when its first boss falls
    /// and it evolves when the last boss of a contract falls (the Brood Mother, for the Thornwood): a second wave of
    /// arrows then rains on the target and leaves thorny ground that slows. Kept for good with the company's record, so
    /// every later archer, and a contract that starts deep, has it. Claude's numbers, to review (Docs/08). Pure.
    /// </summary>
    public static class WildVolleyRules
    {
        public const int NotLearned = 0, Learned = 1, Evolved = 2;

        public static int TierOf(int bossesFelled, int finalBossesFelled) =>
            finalBossesFelled > 0 ? Evolved : bossesFelled > 0 ? Learned : NotLearned;

        public static int TierOf(CompanyRecord company) => company == null ? NotLearned : TierOf(company.BossesFelled, company.FinalBosses.Count);

        public const float CooldownSeconds = 8f;

        public static int Arrows(int tier) => tier >= Evolved ? 9 : 7;

        public static float SpreadDegrees(int tier) => tier >= Evolved ? 64f : 56f;

        /// <summary>Each arrow's damage, as a multiple of a basic arrow's.</summary>
        public static float ArrowShare(int tier) => tier >= Evolved ? 1.1f : 0.9f;

        public const float Speed = 20f;
        public const float Range = 10f;

        // The evolved volley's second wave: arrows fall on the target over a moment, and the ground stays thorny.
        public const int RainArrows = 10;
        public const float RainSeconds = 0.7f;
        public const float RainRadius = 2f;
        public const float RainHitRadius = 0.8f;
        public const float RainShare = 0.7f;
        public const float ThornSeconds = 4f;
        public const float ThornSlow = 0.5f;

        /// <summary>The volley is ready: learned and its cooldown spent.</summary>
        public static bool Ready(int tier, float cooldownLeft) => tier >= Learned && cooldownLeft <= 0f;
    }
}
