using System.Collections.Generic;
using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// The fun boons' numbers (the owner, 2026-11-15: "lets add some fun boons. One suggestion is dragon fire, just an arrow
    /// that explodes as it leaves the bow and shoots a cone of fire. suggest more", then "add all of them as rare boons"):
    /// Dragon Fire, Storm Quiver, Frost Trail, Boomerang Arrow, Rain of Arrows, Ghost Archer, Bone Arrows, Dread Howl and
    /// Last Arrow. Claude's numbers, to review (Docs/08). Pure.
    /// </summary>
    public static class FunBoonRules
    {
        // Dragon Fire: every fourth basic shot, a cone of fire from the bow.
        public const int DragonEvery = 4;
        public const float DragonLength = 4.5f;
        public const float DragonHalfAngle = 30f;
        public const float DragonShare = 0.9f;

        // Storm Quiver: every fifth basic arrow calls a bolt on what it hits, which jumps on.
        public const int StormEvery = 5;
        public const float StormShare = 0.8f;
        public const int StormJumps = 2;
        public const float StormJumpRadius = 3.5f;

        // Frost Trail: ice dropped along a dive.
        public const float IceRadius = 0.9f;
        public const float IceSeconds = 4f;
        public const float IceSlow = 0.4f;
        public const float IceEvery = 0.06f;

        // Boomerang Arrow: the way back.
        public const float ReturnShare = 0.5f;
        public const float ReturnMinDistance = 0.75f;

        // Rain of Arrows: standing still in a fight.
        public const float RainAfterSeconds = 2f;
        public const float RainEvery = 0.2f;
        public const float RainRadius = 1.8f;
        public const float RainHitRadius = 0.7f;
        public const float RainShare = 0.5f;

        // Ghost Archer: the copy left where she dived from.
        public const float GhostSeconds = 4f;
        public const float GhostEvery = 0.5f;
        public const float GhostShare = 0.6f;

        // Bone Arrows: shards from kills.
        public const int BoneMax = 5;
        public const int ShardsOnGround = 12;
        public const float ShardSeconds = 10f;
        public const float ShardReach = 0.9f;
        public const float BoneFanDegrees = 6f;

        // Dread Howl: the wolf's bite frightens those round it.
        public const float DreadRadius = 2.5f;
        public const float DreadPush = 1.2f;
        public const float DreadSlow = 0.5f;
        public const float DreadSeconds = 3f;
        public const float DreadBonus = 0.3f;
        public const float DreadGap = 4f;

        // Last Arrow: once a level, when she is badly hurt.
        public const float LastArrowBelow = 1f / 3f;
        public const float LastArrowMultiplier = 6f;

        /// <summary>Whether the basic shot of this count (from 1) breathes Dragon Fire.</summary>
        public static bool IsDragonShot(int basicShotCount) => basicShotCount > 0 && basicShotCount % DragonEvery == 0;

        /// <summary>Whether the basic shot of this count (from 1) is a storm arrow.</summary>
        public static bool IsStormShot(int basicShotCount) => basicShotCount > 0 && basicShotCount % StormEvery == 0;

        /// <summary>Whether a point lies in the cone of fire from her bow: within its length (the body's radius allowed)
        /// and its half angle of the aim. A point on her is in it.</summary>
        public static bool InCone(Vector2 origin, Vector2 aim, Vector2 point, float bodyRadius)
        {
            var to = point - origin;
            var distance = to.magnitude;
            if (distance > DragonLength + bodyRadius)
                return false;
            return distance < 0.01f || Vector2.Angle(aim, to) <= DragonHalfAngle;
        }

        /// <summary>Whether the Last Arrow flies: not yet spent this level, alive, and below a third of her life.</summary>
        public static bool LastArrowReady(bool spent, float lifeFraction) => !spent && lifeFraction > 0f && lifeFraction < LastArrowBelow;

        /// <summary>The bone arrows she holds after picking up a shard.</summary>
        public static int Loaded(int held) => Mathf.Min(BoneMax, held + 1);

        /// <summary>The angle of the k-th bone arrow (from 1) off her aim: to either side in turn, fanning out.</summary>
        public static float BoneAngle(int k) => (k % 2 == 1 ? 1f : -1f) * BoneFanDegrees * ((k + 1) / 2);
    }

    // The fun boons' effects (2026-11-15).
    public partial class PlayerCombat
    {
        static readonly Color GhostColor = new Color(0.62f, 0.85f, 1f, 0.6f);
        static readonly Color BoneColor = new Color(0.94f, 0.9f, 0.8f, 0.8f);
        static readonly Color DreadColor = new Color(0.8f, 0.25f, 0.25f, 1f);
        static readonly Color LastArrowColor = new Color(1f, 0.85f, 0.3f, 1f);

        bool lastArrowSpent;
        int funSeenDodges;
        float iceTimer;
        float rainTimer;
        int rainCount;
        int boneArrows;
        float dreadReady;

        readonly HashSet<EnemyController> stormStruck = new HashSet<EnemyController>();
        readonly Dictionary<EnemyController, float> frightened = new Dictionary<EnemyController, float>();
        readonly List<EnemyController> frightEnded = new List<EnemyController>(8);

        struct Shard
        {
            public Vector2 At;
            public float Left;
            public Transform Transform;
        }

        readonly List<Shard> shards = new List<Shard>(FunBoonRules.ShardsOnGround);
        readonly Stack<Transform> spareShards = new Stack<Transform>();
        static Sprite shardSprite;

        // The ghost: a still copy of her picture where she dived from.
        Transform ghostRoot;
        readonly List<SpriteRenderer> ghostRenderers = new List<SpriteRenderer>(8);
        Vector2 ghostAt;
        float ghostLeft;
        float ghostShotTimer;
        LayeredCharacterSprite herSprite;

        /// <summary>Her arrows' "more" against the frightened (Dread Howl).</summary>
        float FrightMore(EnemyController enemy) =>
            frightened.Count > 0 && frightened.TryGetValue(enemy, out var until) && Time.time < until ? 1f + FunBoonRules.DreadBonus : 1f;

        // A basic attack's first arrow: Boomerang Arrow's turn, Storm Quiver's bolt, the Last Arrow, and Dragon Fire's cone.
        void FunBoonsOnShot(ref Shot first, Vector2 origin, Vector2 aim)
        {
            var boons = session.Talents.Bonuses;
            first.Returns = boons.BoomerangArrow;
            if (boons.StormQuiver && FunBoonRules.IsStormShot(basicShotCount))
            {
                first.Storm = true;
                first.Charged = true;
                first.Glow = LightningColor;
            }
            if (boons.LastArrow && health != null && FunBoonRules.LastArrowReady(lastArrowSpent, health.Fraction))
            {
                lastArrowSpent = true;
                first.Multiplier = FunBoonRules.LastArrowMultiplier;
                first.CertainCrit = true;
                first.PierceLeft = int.MaxValue;
                EnsurePierceSet(ref first);
                first.Returns = false;
                first.Fiery = true;
                first.Glow = LastArrowColor;
                DamageNumbers.Current?.ShowText(Elevation.Lifted(player.transform.position) + new Vector3(0f, 2.1f, 0f), "LAST ARROW", LastArrowColor, 40);
                fx.Burst(Elevation.World2(origin), 0.8f, LastArrowColor);
                Sfx.Play(SoundId.Explosion, 0.6f);
            }
            if (boons.DragonFire && FunBoonRules.IsDragonShot(basicShotCount))
                DragonFire(origin, aim);
        }

        // Dragon Fire: the cone strikes and sets burning every enemy in it she has a line to.
        void DragonFire(Vector2 origin, Vector2 aim)
        {
            enemies.QueryEnemies(origin, FunBoonRules.DragonLength + QueryMargin, boonNearby);
            for (var n = 0; n < boonNearby.Count; n++)
            {
                var enemy = boonNearby[n];
                if (!enemy.IsAlive || !FunBoonRules.InCone(origin, aim, enemy.GroundPosition, enemy.Definition.BodyRadius) ||
                    !enemies.Nav.HasLineOfSight(origin, enemy.GroundPosition))
                    continue;
                if (!Strike(enemy, FunBoonRules.DragonShare, projectile: true) && enemy.IsAlive)
                    enemy.ApplyBurn(Damage(enemy, BurnShare / BurnStacks.Seconds, false) * BurnFactor);
                fx.Flames(Elevation.World2(enemy.GroundPosition), ArrowHeight * 0.8f);
            }
            // The fire: three rows of flames fanning out from the bow.
            for (var row = 1; row <= 3; row++)
            {
                var distance = FunBoonRules.DragonLength * row / 3f;
                var across = row + 1;
                for (var k = 0; k < across; k++)
                {
                    var angle = Mathf.Lerp(-FunBoonRules.DragonHalfAngle, FunBoonRules.DragonHalfAngle, across == 1 ? 0.5f : k / (float)(across - 1));
                    var at = origin + Rotate(aim, angle) * distance;
                    if (enemies.Nav.HasLineOfSight(origin, at))
                        fx.Flames(Elevation.World2(at), ArrowHeight);
                }
            }
            fx.Burst(Elevation.World2(origin + aim * 0.7f), 0.6f, FireGlow);
            Sfx.Play(SoundId.FireHit, 0.8f);
            focus.MarkCombat();
        }

        // Storm Quiver: a bolt down on what the arrow hit, jumping on to the nearest others.
        void Storm(EnemyController struck, float multiplier)
        {
            stormStruck.Clear();
            stormStruck.Add(struck);
            Thunderbolt(struck.GroundPosition);
            Sfx.Play(SoundId.Thunder, 0.5f);
            if (struck.IsAlive)
                Strike(struck, multiplier * FunBoonRules.StormShare, projectile: true);
            var from = struck;
            for (var jump = 0; jump < FunBoonRules.StormJumps; jump++)
            {
                var next = NearestUnstruck(from, FunBoonRules.StormJumpRadius, stormStruck);
                if (next == null)
                    break;
                stormStruck.Add(next);
                Bolt(from.GroundPosition, next.GroundPosition);
                Strike(next, multiplier * FunBoonRules.StormShare, projectile: true);
                from = next;
            }
            stormStruck.Clear();
        }

        // Boomerang Arrow: an arrow at its end (or on its last hit) turns once and flies back to where she stands,
        // through everything, at half damage. Returns whether it flies on.
        bool TurnBack(ref Shot shot, EnemyController struck)
        {
            if (!shot.Returns || player == null)
                return false;
            shot.Returns = false;
            var home = IsoMath.WorldToGround(player.transform.position) - shot.Position;
            var distance = home.magnitude;
            if (distance < FunBoonRules.ReturnMinDistance)
                return false;
            var speed = shot.Velocity.magnitude;
            shot.Velocity = home / distance * speed;
            shot.Travelled = 0f;
            shot.MaxDistance = distance;
            shot.Multiplier *= FunBoonRules.ReturnShare;
            shot.PierceLeft = int.MaxValue;
            EnsurePierceSet(ref shot);
            shot.Pierced.Clear();
            if (struck != null)
                shot.Pierced.Add(struck);
            shot.CanFork = false;
            shot.Bounces = 0;
            shot.Ricochets = 0;
            shot.Thunder = false;
            shot.Storm = false;
            shot.Seeking = false;
            shot.Chasing = null;
            shot.ExecuteBelow = 0f;
            fx.Sparks(Elevation.World2(shot.Position), IsoMath.GroundToWorld(shot.Velocity.normalized), ArrowHeight, shot.Glow, 3);
            return true;
        }

        // Bone Arrows: as many free arrows as she holds, fanned round her aim, with the attack.
        void LooseBoneArrows(Vector2 origin, Vector2 aim)
        {
            if (boneArrows <= 0)
                return;
            for (var k = 1; k <= boneArrows; k++)
                shots.Add(NewArrow(origin, Rotate(aim, FunBoonRules.BoneAngle(k)) * ArrowSpeed, BasicRange + ArrowOvershoot, 1f, ShotStyle.Basic, BoneColor));
            boneArrows = 0;
        }

        void DropShard(Vector2 at)
        {
            if (shards.Count >= FunBoonRules.ShardsOnGround)
                return;
            Transform shard;
            if (spareShards.Count > 0)
                shard = spareShards.Pop();
            else
            {
                var go = GroundMarker.NewSprite("Bone Shard", ShardSprite, Color.white, transform, 0);
                go.GetComponent<SpriteRenderer>().sortingLayerName = GameSortingLayers.Effects;
                shard = go.transform;
            }
            shard.gameObject.SetActive(true);
            shard.position = Elevation.World2(at) + new Vector2(0f, 0.12f);
            shards.Add(new Shard { At = at, Left = FunBoonRules.ShardSeconds, Transform = shard });
        }

        static Sprite ShardSprite
        {
            get
            {
                if (shardSprite != null)
                    return shardSprite;
                // A little bone, 2 screen pixels a pixel like the rest: 5 x 3 drawn.
                string[] rows = { "#...#", ".###.", "#...#" };
                const int scale = 2;
                var texture = new Texture2D(5 * scale, 3 * scale, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
                var pixels = new Color32[texture.width * texture.height];
                for (var y = 0; y < texture.height; y++)
                    for (var x = 0; x < texture.width; x++)
                        pixels[x + y * texture.width] = rows[2 - y / scale][x / scale] == '#' ? new Color32(240, 232, 216, 255) : new Color32(0, 0, 0, 0);
                texture.SetPixels32(pixels);
                texture.Apply();
                shardSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 27f, 0, SpriteMeshType.FullRect);
                return shardSprite;
            }
        }

        /// <summary>Dread Howl: the wolf bit an enemy (from <see cref="PetController"/>). Those round it, not bosses, are
        /// thrown back from her, slowed, and take more from her arrows for a while; at most once every few seconds.</summary>
        public void OnWolfBite(EnemyController bitten)
        {
            if (session == null || !session.Talents.Bonuses.DreadHowl || player == null || enemies == null || bitten == null || Time.time < dreadReady)
                return;
            dreadReady = Time.time + FunBoonRules.DreadGap;
            var at = bitten.GroundPosition;
            var her = IsoMath.WorldToGround(player.transform.position);
            enemies.QueryEnemies(at, FunBoonRules.DreadRadius + QueryMargin, boonNearby);
            for (var n = 0; n < boonNearby.Count; n++)
            {
                var enemy = boonNearby[n];
                if (!enemy.IsAlive || enemy.Definition.Rank == EnemyRank.Boss ||
                    Vector2.Distance(at, enemy.GroundPosition) > FunBoonRules.DreadRadius + enemy.Definition.BodyRadius)
                    continue;
                var away = enemy.GroundPosition - her;
                if (away.sqrMagnitude > 1e-6f)
                    enemy.Push(away.normalized * FunBoonRules.DreadPush);
                enemy.ApplySlow(1f - FunBoonRules.DreadSlow, FunBoonRules.DreadSeconds);
                frightened[enemy] = Time.time + FunBoonRules.DreadSeconds;
                fx.Sparks(Elevation.World2(enemy.GroundPosition) + new Vector2(0f, 1.0f), Vector2.up, 0f, DreadColor, 2);
            }
            fx.Burst(Elevation.World2(at), FunBoonRules.DreadRadius * 0.5f, DreadColor);
            DamageNumbers.Current?.ShowText(Elevation.World2(at) + new Vector2(0f, 1.6f), "DREAD", DreadColor, 30);
            Sfx.Play(SoundId.Crit, 0.4f);
        }

        /// <summary>Once a frame: the dive's ice and ghost, the rain while she stands, the shards, the frightened.</summary>
        void UpdateFunBoons(Vector2 origin, float deltaTime, PassiveBonuses boons)
        {
            // A dive began: the ghost stays where she left.
            if (player.DodgeCount != funSeenDodges)
            {
                funSeenDodges = player.DodgeCount;
                if (boons.GhostArcher)
                    RaiseGhost(origin);
            }
            if (boons.FrostTrail && player.IsDodging)
            {
                iceTimer -= deltaTime;
                if (iceTimer <= 0f)
                {
                    iceTimer = FunBoonRules.IceEvery;
                    marks.Add(new GroundMark { Center = origin, Radius = FunBoonRules.IceRadius, Left = FunBoonRules.IceSeconds, Icy = true });
                    fx.FrostShards(Elevation.World2(origin), Vector2.up, 0.1f, 1.2f);
                }
            }
            else
                iceTimer = 0f;

            UpdateGhost(deltaTime);
            if (boons.RainOfArrows)
                UpdateRain(deltaTime);
            UpdateShards(origin, deltaTime);

            if (frightened.Count > 0)
            {
                frightEnded.Clear();
                foreach (var pair in frightened)
                    if (Time.time >= pair.Value || pair.Key == null || !pair.Key.IsAlive)
                        frightEnded.Add(pair.Key);
                for (var i = 0; i < frightEnded.Count; i++)
                    frightened.Remove(frightEnded[i]);
            }
        }

        // Rain of Arrows: while she has stood still long enough with a target, arrows fall round it.
        void UpdateRain(float deltaTime)
        {
            if (stillSeconds < FunBoonRules.RainAfterSeconds || Target == null || !Target.IsAlive || player.Disengaged)
            {
                rainTimer = 0f;
                return;
            }
            rainTimer -= deltaTime;
            if (rainTimer > 0f)
                return;
            rainTimer = FunBoonRules.RainEvery;
            var at = Target.GroundPosition + Random.insideUnitCircle * FunBoonRules.RainRadius;
            if (!enemies.Nav.IsOpenToShots(IsoMath.GroundToCell(at)))
                return;
            var world = Elevation.World2(at);
            fx.Stuck(world, Vector2.down, 0.05f, ArrowArt.Arrow);
            fx.Sparks(world, Vector2.up, 0.1f, BasicTrailColor, 2);
            if (rainCount++ % 3 == 0)
                Sfx.Play(SoundId.ArrowShot, 0.25f);
            enemies.QueryEnemies(at, FunBoonRules.RainHitRadius + QueryMargin, boonNearby);
            for (var n = 0; n < boonNearby.Count; n++)
            {
                var enemy = boonNearby[n];
                if (!enemy.IsAlive || Vector2.Distance(at, enemy.GroundPosition) > FunBoonRules.RainHitRadius + enemy.Definition.BodyRadius)
                    continue;
                if (Strike(enemy, FunBoonRules.RainShare, projectile: true))
                    fx.Blood(Elevation.World2(enemy.GroundPosition), Vector2.down, ArrowHeight, 1.2f);
            }
            focus.MarkCombat();
        }

        void UpdateShards(Vector2 origin, float deltaTime)
        {
            for (var i = shards.Count - 1; i >= 0; i--)
            {
                var shard = shards[i];
                shard.Left -= deltaTime;
                var taken = Vector2.Distance(origin, shard.At) <= FunBoonRules.ShardReach;
                if (taken && boneArrows < FunBoonRules.BoneMax)
                {
                    boneArrows = FunBoonRules.Loaded(boneArrows);
                    DamageNumbers.Current?.ShowText(Elevation.Lifted(player.transform.position) + new Vector3(0f, 1.9f, 0f), "BONE " + boneArrows, BoneColor, 28);
                }
                else
                    taken = false;
                if (taken || shard.Left <= 0f)
                {
                    shard.Transform.gameObject.SetActive(false);
                    spareShards.Push(shard.Transform);
                    shards[i] = shards[shards.Count - 1];
                    shards.RemoveAt(shards.Count - 1);
                    continue;
                }
                // The last two seconds it blinks.
                if (shard.Left < 2f)
                    shard.Transform.gameObject.SetActive(Mathf.FloorToInt(shard.Left * 8f) % 2 == 0);
                shards[i] = shard;
            }
        }

        // Ghost Archer: her picture as it is now, pale, left standing at this spot.
        void RaiseGhost(Vector2 at)
        {
            if (herSprite == null)
                herSprite = player.GetComponentInChildren<LayeredCharacterSprite>();
            if (herSprite == null)
                return;
            if (ghostRoot == null)
            {
                ghostRoot = new GameObject("Ghost Archer").transform;
                ghostRoot.SetParent(transform, false);
            }
            var visible = herSprite.VisibleRenderers;
            while (ghostRenderers.Count < visible.Count)
            {
                var go = new GameObject("Layer", typeof(SpriteRenderer));
                go.transform.SetParent(ghostRoot, false);
                ghostRenderers.Add(go.GetComponent<SpriteRenderer>());
            }
            for (var i = 0; i < ghostRenderers.Count; i++)
            {
                var copy = ghostRenderers[i];
                if (i >= visible.Count)
                {
                    copy.enabled = false;
                    continue;
                }
                var source = visible[i];
                copy.enabled = true;
                copy.sprite = source.sprite;
                copy.flipX = source.flipX;
                copy.sharedMaterial = source.sharedMaterial;
                copy.sortingLayerID = source.sortingLayerID;
                copy.sortingOrder = source.sortingOrder;
                copy.color = GhostColor;
                copy.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
                copy.transform.localScale = source.transform.lossyScale;
            }
            ghostRoot.gameObject.SetActive(true);
            ghostAt = at;
            ghostLeft = FunBoonRules.GhostSeconds;
            ghostShotTimer = FunBoonRules.GhostEvery * 0.5f;
            fx.Burst(Elevation.World2(at), 0.5f, GhostColor);
        }

        void UpdateGhost(float deltaTime)
        {
            if (ghostLeft <= 0f)
                return;
            ghostLeft -= deltaTime;
            if (ghostLeft <= 0f)
            {
                if (ghostRoot != null)
                    ghostRoot.gameObject.SetActive(false);
                return;
            }
            // It fades over its last second.
            var color = GhostColor;
            color.a *= Mathf.Clamp01(ghostLeft);
            for (var i = 0; i < ghostRenderers.Count; i++)
                ghostRenderers[i].color = color;
            ghostShotTimer -= deltaTime;
            if (ghostShotTimer > 0f)
                return;
            ghostShotTimer = FunBoonRules.GhostEvery;
            LooseAtNearest(ghostAt, 1, FunBoonRules.GhostShare);
        }
    }
}
