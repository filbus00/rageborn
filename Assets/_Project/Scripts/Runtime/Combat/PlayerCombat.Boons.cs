using System.Collections.Generic;
using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// The unique boons' numbers (the owner, 2026-10-17: "Add more unique boons"): Thunder Arrow, Marked Prey, Dive Volley,
    /// Vengeful Quiver, Wildfire and Shatter. Claude's numbers, to review (Docs/08). Pure.
    /// </summary>
    public static class UniqueBoonRules
    {
        public const int ThunderEvery = 8;
        public const float ThunderRadius = 2.5f;
        public const float ThunderShare = 0.8f;

        public const float PreySeconds = 8f;
        public const float PreyBonus = 0.5f;

        public const int DiveVolleyArrows = 4;

        public const int VengefulArrows = 5;
        public const float VengefulGap = 1f;

        public const float DeathRadius = 2.5f;
        public const float ShatterShare = 1f;

        /// <summary>Whether the basic shot of this count (from 1) is a thunder arrow.</summary>
        public static bool IsThunderShot(int basicShotCount) => basicShotCount > 0 && basicShotCount % ThunderEvery == 0;

        /// <summary>The prey among enemies by their life: the one with the most maximum life (the first on a tie), or -1.</summary>
        public static int Toughest(IReadOnlyList<float> maxLives)
        {
            var best = -1;
            for (var i = 0; i < maxLives.Count; i++)
                if (best < 0 || maxLives[i] > maxLives[best])
                    best = i;
            return best;
        }
    }

    // The unique boons' effects (2026-10-17).
    public partial class PlayerCombat
    {
        static readonly Color PreyColor = new Color(1f, 0.25f, 0.2f, 1f);
        static readonly Color ThunderColor = new Color(0.75f, 0.85f, 1f, 1f);

        EnemyController prey;
        float preyTimer;
        float preySparkTimer;
        int seenDodges;
        float vengefulReady;
        bool killedHooked;

        struct BoonDeath
        {
            public EnemyController Enemy;
            public Vector2 At;
            public bool Burning, Frozen;
        }

        // Deaths are handled in Update, not inside the kill (a shatter can kill another, and the lists are shared).
        readonly List<BoonDeath> boonDeaths = new List<BoonDeath>(8);
        readonly List<EnemyController> boonNearby = new List<EnemyController>(16);
        readonly List<float> preyLives = new List<float>(32);

        /// <summary>Her arrows' "more" against her prey (Marked Prey).</summary>
        float PreyMore(EnemyController enemy) => enemy == prey && prey != null && prey.IsAlive ? 1f + UniqueBoonRules.PreyBonus : 1f;

        void HookBoonKills()
        {
            if (killedHooked || enemies == null)
                return;
            enemies.Killed += OnBoonKill;
            killedHooked = true;
        }

        void UnhookBoonKills()
        {
            if (killedHooked && enemies != null)
                enemies.Killed -= OnBoonKill;
            killedHooked = false;
        }

        void OnBoonKill(EnemyController enemy)
        {
            var boons = session.Talents.Bonuses;
            if (enemy == prey)
            {
                prey = null;
                preyTimer = 0f;
                player?.DodgeCharges.Refund();
            }
            var burning = boons.Wildfire && enemy.IsBurning;
            var frozen = boons.Shatter && enemy.IsFrozen;
            if (burning || frozen)
                boonDeaths.Add(new BoonDeath { Enemy = enemy, At = enemy.GroundPosition, Burning = burning, Frozen = frozen });
        }

        /// <summary>Once a frame, after the shots: the deaths that spread, the prey, and a dive's volley.</summary>
        void UpdateBoons(Vector2 origin, float deltaTime)
        {
            HookBoonKills();
            var boons = session.Talents.Bonuses;
            for (var d = 0; d < boonDeaths.Count; d++)
                SpreadDeath(boonDeaths[d]);
            boonDeaths.Clear();

            if (boons.MarkedPrey)
                UpdatePrey(deltaTime);
            else
                prey = null;

            if (player.DodgeCount != seenDodges)
            {
                seenDodges = player.DodgeCount;
                if (boons.DiveVolley)
                    LooseAtNearest(origin, UniqueBoonRules.DiveVolleyArrows);
            }
        }

        // Wildfire: the dead's fire takes to those around it. Shatter: the frozen dead bursts into ice.
        void SpreadDeath(BoonDeath death)
        {
            enemies.QueryEnemies(death.At, UniqueBoonRules.DeathRadius + QueryMargin, boonNearby);
            var world = IsoMath.GroundToWorld(death.At);
            if (death.Frozen)
            {
                fx.Burst(world, UniqueBoonRules.DeathRadius * 0.5f, ChillColor);
                fx.FrostShards(world, Vector2.up, ArrowHeight, 2.2f);
                Sfx.Play(SoundId.Freeze, 0.6f);
            }
            if (death.Burning)
            {
                fx.Flames(world, ArrowHeight);
                Sfx.Play(SoundId.FireHit, 0.5f);
            }
            for (var n = 0; n < boonNearby.Count; n++)
            {
                var enemy = boonNearby[n];
                if (enemy == death.Enemy || !enemy.IsAlive ||
                    Vector2.Distance(death.At, enemy.GroundPosition) > UniqueBoonRules.DeathRadius + enemy.Definition.BodyRadius)
                    continue;
                if (death.Burning)
                {
                    enemy.ApplyBurn(Damage(enemy, BurnShare / BurnStacks.Seconds, false) * BurnFactor);
                    fx.Flames(IsoMath.GroundToWorld(enemy.GroundPosition), ArrowHeight * 0.8f);
                }
                if (death.Frozen && enemy.IsAlive)
                {
                    enemy.ApplyChill();
                    enemy.ApplyChill();
                    Strike(enemy, UniqueBoonRules.ShatterShare, projectile: true);
                }
            }
        }

        // Marked Prey: every 8 s (or when the prey dies or leaves the screen) the toughest enemy on screen is marked.
        void UpdatePrey(float deltaTime)
        {
            preyTimer -= deltaTime;
            var lost = prey == null || !prey.IsAlive || !candidates.Contains(prey);
            if (preyTimer <= 0f || lost && preyTimer < UniqueBoonRules.PreySeconds - 1f)
            {
                preyLives.Clear();
                for (var i = 0; i < candidates.Count; i++)
                    preyLives.Add(candidates[i].IsAlive ? candidates[i].MaxLife : -1f);
                var best = UniqueBoonRules.Toughest(preyLives);
                var next = best >= 0 && preyLives[best] > 0f ? candidates[best] : null;
                if (next != null)
                {
                    preyTimer = UniqueBoonRules.PreySeconds;
                    if (next != prey)
                        DamageNumbers.Current?.ShowText(IsoMath.GroundToWorld(next.GroundPosition) + new Vector2(0f, 1.6f), "PREY", PreyColor, 30);
                    prey = next;
                }
            }
            if (prey == null || !prey.IsAlive)
                return;
            preySparkTimer -= deltaTime;
            if (preySparkTimer > 0f)
                return;
            preySparkTimer = 0.2f;
            fx.Sparks(IsoMath.GroundToWorld(prey.GroundPosition) + new Vector2(0f, 1.1f), Vector2.up, 0f, PreyColor, 1);
        }

        // Vengeful Quiver: hurt, she shoots back at those around her.
        void Retaliate()
        {
            if (!session.Talents.Bonuses.VengefulQuiver || player == null || Time.time < vengefulReady)
                return;
            vengefulReady = Time.time + UniqueBoonRules.VengefulGap;
            LooseAtNearest(IsoMath.WorldToGround(player.transform.position), UniqueBoonRules.VengefulArrows);
        }

        // A basic arrow at each of the nearest enemies she can see within her reach (Dive Volley, Vengeful Quiver).
        void LooseAtNearest(Vector2 origin, int count)
        {
            if (enemies == null)
                return;
            var reach = BasicRange;
            enemies.QueryEnemies(origin, reach + QueryMargin, boonNearby);
            KeepOnScreen(boonNearby);
            for (var k = 0; k < count && boonNearby.Count > 0; k++)
            {
                var best = -1;
                var bestDistance = reach;
                for (var n = 0; n < boonNearby.Count; n++)
                {
                    var enemy = boonNearby[n];
                    if (!enemy.IsAlive)
                        continue;
                    var distance = Vector2.Distance(origin, enemy.GroundPosition);
                    if (distance <= bestDistance && enemies.Nav.HasLineOfSight(origin, enemy.GroundPosition))
                    {
                        best = n;
                        bestDistance = distance;
                    }
                }
                if (best < 0)
                    break;
                var aim = (boonNearby[best].GroundPosition - origin).normalized;
                boonNearby.RemoveAt(best);
                if (aim.sqrMagnitude < 1e-6f)
                    continue;
                shots.Add(NewArrow(origin, aim * ArrowSpeed, reach + ArrowOvershoot, 1f, ShotStyle.Basic, BasicTrailColor));
            }
            Sfx.Play(SoundId.ArrowShot, 0.5f);
        }

        // Thunder Arrow: lightning down on every enemy around where it struck.
        void Thunder(EnemyController struck, float multiplier)
        {
            var at = struck.GroundPosition;
            enemies.QueryEnemies(at, UniqueBoonRules.ThunderRadius + QueryMargin, boonNearby);
            Thunderbolt(at);
            fx.Burst(IsoMath.GroundToWorld(at), UniqueBoonRules.ThunderRadius * 0.4f, ThunderColor);
            Sfx.Play(SoundId.Thunder, 0.6f);
            for (var n = 0; n < boonNearby.Count; n++)
            {
                var enemy = boonNearby[n];
                if (enemy == struck || !enemy.IsAlive ||
                    Vector2.Distance(at, enemy.GroundPosition) > UniqueBoonRules.ThunderRadius + enemy.Definition.BodyRadius)
                    continue;
                Thunderbolt(enemy.GroundPosition);
                Strike(enemy, multiplier * UniqueBoonRules.ThunderShare, projectile: true);
            }
        }
    }
}
