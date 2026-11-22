using System.Collections.Generic;
using UnityEngine;

namespace ARPG
{
    // Wild Volley (2026-11-22): the double tap's attack. See WildVolleyRules.
    public partial class PlayerCombat
    {
        static readonly Color VolleyColor = new Color(0.55f, 1f, 0.5f, 1f);

        float volleyCooldown;

        struct RainArrow
        {
            public float Delay;
            public Vector2 Point;
        }

        readonly List<RainArrow> volleyRain = new List<RainArrow>(16);

        /// <summary>Wild Volley's tier for the HUD and tests: 0 not learned, 1 learned, 2 evolved.</summary>
        public int WildVolleyTier => WildVolleyRules.TierOf(session.Company);

        /// <summary>How far the cooldown has come, 0 to 1; 1 when it is ready.</summary>
        public float WildVolleyReady => 1f - Mathf.Clamp01(volleyCooldown / WildVolleyRules.CooldownSeconds);

        /// <summary>A double tap, made by hand in a development build.</summary>
        public void TestWildVolley() => FireWildVolley(IsoMath.WorldToGround(player.transform.position));

        // Once a frame: the cooldown, a double tap, and the evolved volley's rain.
        void UpdateWildVolley(Vector2 origin, float deltaTime)
        {
            volleyCooldown = Mathf.Max(0f, volleyCooldown - deltaTime);
            UpdateVolleyRain(deltaTime);
            if (Time.timeScale <= 0f || !player.ConsumeDoubleTap())
                return;
            if (player.IsDodging || player.Disengaged || !WildVolleyRules.Ready(WildVolleyTier, volleyCooldown))
                return;
            FireWildVolley(origin);
        }

        void FireWildVolley(Vector2 origin)
        {
            var tier = WildVolleyTier;
            if (tier < WildVolleyRules.Learned)
                return;
            volleyCooldown = WildVolleyRules.CooldownSeconds;
            var target = Target != null && Target.IsAlive ? Target : null;
            var aim = target != null ? (target.GroundPosition - origin).normalized : facing;
            if (aim.sqrMagnitude < 1e-6f)
                aim = facing;
            var count = WildVolleyRules.Arrows(tier);
            var spread = WildVolleyRules.SpreadDegrees(tier);
            var share = WildVolleyRules.ArrowShare(tier);
            var glow = new Color(VolleyColor.r, VolleyColor.g, VolleyColor.b, 0.8f);
            // The fan flies true, whatever her stick is doing.
            var stray = strayDegrees;
            strayDegrees = 0f;
            for (var k = 0; k < count; k++)
            {
                var angle = count > 1 ? spread * (k / (float)(count - 1) - 0.5f) : 0f;
                shots.Add(NewArrow(origin, Rotate(aim, angle) * WildVolleyRules.Speed, WildVolleyRules.Range + ArrowOvershoot, share, ShotStyle.Volley, glow));
            }
            strayDegrees = stray;
            Sfx.Play(SoundId.ArrowShot, 1f);
            fx.Burst(Elevation.World2(origin + aim * 0.7f), 0.5f, VolleyColor);
            focus.MarkCombat();
            if (tier >= WildVolleyRules.Evolved)
                CallVolleyRain(origin, aim, target);
        }

        // The evolved volley: arrows fall on the target (or the ground ahead of her) over a moment, and the ground
        // stays thorny for a few seconds, slowing what stands in it.
        void CallVolleyRain(Vector2 origin, Vector2 aim, EnemyController target)
        {
            var centre = target != null ? target.GroundPosition : origin + aim * 4f;
            if (!enemies.Nav.IsOpenToShots(IsoMath.GroundToCell(centre)))
                centre = origin + aim * 2f;
            for (var i = 0; i < WildVolleyRules.RainArrows; i++)
                volleyRain.Add(new RainArrow
                {
                    Delay = 0.25f + WildVolleyRules.RainSeconds * i / WildVolleyRules.RainArrows,
                    Point = centre + Random.insideUnitCircle * WildVolleyRules.RainRadius,
                });
            marks.Add(new GroundMark { Center = centre, Radius = WildVolleyRules.RainRadius, Left = WildVolleyRules.ThornSeconds, Thorny = true });
            fx.Burst(Elevation.World2(centre), WildVolleyRules.RainRadius * 0.5f, VolleyColor);
        }

        void UpdateVolleyRain(float deltaTime)
        {
            for (var i = volleyRain.Count - 1; i >= 0; i--)
            {
                var arrow = volleyRain[i];
                arrow.Delay -= deltaTime;
                if (arrow.Delay > 0f)
                {
                    volleyRain[i] = arrow;
                    continue;
                }
                volleyRain.RemoveAt(i);
                var at = arrow.Point;
                if (!enemies.Nav.IsOpenToShots(IsoMath.GroundToCell(at)))
                    continue;
                var world = Elevation.World2(at);
                fx.Stuck(world, Vector2.down, 0.05f, ArrowArt.Arrow);
                fx.Sparks(world, Vector2.up, 0.1f, VolleyColor, 3);
                enemies.QueryEnemies(at, WildVolleyRules.RainHitRadius + QueryMargin, boonNearby);
                for (var n = 0; n < boonNearby.Count; n++)
                {
                    var enemy = boonNearby[n];
                    if (!enemy.IsAlive || Vector2.Distance(at, enemy.GroundPosition) > WildVolleyRules.RainHitRadius + enemy.Definition.BodyRadius)
                        continue;
                    if (Strike(enemy, WildVolleyRules.RainShare, projectile: true))
                        fx.Blood(Elevation.World2(enemy.GroundPosition), Vector2.down, ArrowHeight, 1.2f);
                }
            }
        }
    }
}
