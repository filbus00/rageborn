using System.Collections.Generic;
using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// The arrows in flight, owned and ticked by <see cref="EnemyManager"/> after its enemies. Each is a pooled sprite
    /// moved by <see cref="EnemyProjectileFlight"/>; its damage was worked out when it was fired, so a shooter that dies
    /// mid-flight still hits. Sprites are made on first need and reused, so a fight does not allocate.
    /// </summary>
    public sealed class EnemyProjectiles
    {
        // The player's hurt radius plus the arrow's own. Docs/01: hurtboxes are 70 percent of visual size, so this is
        // a little under the body's width. Tuning value.
        const float HitRadius = 0.4f;

        static readonly Color ArrowColor = new Color(0.75f, 0.85f, 1f, 1f);

        struct Shot
        {
            public Transform Transform;
            public Vector2 Position;
            public Vector2 Velocity;
            public float Travelled;
            public float MaxDistance;
            public float Damage;
            public int Level;
            public float ArmorIgnore;
            public EnemyController Shooter;
            // The kind (2026-10-17): how it looks and what it does when it stops; bounces left, the colour of its burst.
            public ProjectileKind Kind;
            public int Bounces;
            public Color Color;
            public float Spin;
        }

        readonly Transform parent;
        readonly List<Shot> flying = new List<Shot>(16);
        readonly Stack<Transform> spare = new Stack<Transform>(16);

        public EnemyProjectiles(Transform parent) => this.parent = parent;

        // Made once, so the bounce check allocates nothing.
        System.Func<Vector2Int, bool> walkable;

        public int Count => flying.Count;

        public void Fire(EnemyController shooter, Vector2 origin, Vector2 velocity, float maxDistance, float damage, int level, float armorIgnore,
            Color color = default, ProjectileKind kind = ProjectileKind.Plain)
        {
            var transform = spare.Count > 0 ? spare.Pop() : Create();
            transform.gameObject.SetActive(true);
            var renderer = transform.GetComponent<SpriteRenderer>();
            var art = ProjectileArt.For(kind);
            if (art != null)
            {
                // A drawn thing (a knife, an axe, a bomb, a blade) in its own colours.
                renderer.sprite = art;
                renderer.color = Color.white;
                transform.localScale = kind == ProjectileKind.Axe || kind == ProjectileKind.Bomb ? new Vector3(1.9f, 1.9f, 1f) : new Vector3(1.5f, 1.5f, 1f);
            }
            else
            {
                // A shooter's own colour (a cultist's fire bolt, drawn larger), else the arrow streak; a fireball is a big
                // round glow.
                var bolt = color.a > 0f;
                renderer.sprite = TelegraphArt.Ember;
                renderer.color = bolt ? color : ArrowColor;
                transform.localScale = kind == ProjectileKind.Fireball ? new Vector3(0.8f, 0.8f, 1f)
                    : bolt ? new Vector3(0.6f, 0.35f, 1f) : new Vector3(0.7f, 0.16f, 1f);
            }
            transform.position = IsoMath.GroundToWorld(origin);
            var world = IsoMath.GroundToWorld(velocity);
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(world.y, world.x) * Mathf.Rad2Deg);
            flying.Add(new Shot
            {
                Transform = transform,
                Position = origin,
                Velocity = velocity,
                MaxDistance = maxDistance,
                Damage = damage,
                Level = level,
                ArmorIgnore = armorIgnore,
                Shooter = shooter,
                Kind = kind,
                Bounces = kind == ProjectileKind.Bouncing ? ProjectileKinds.Bounces : 0,
                Color = color.a > 0f ? color : ArrowColor,
                Spin = kind == ProjectileKind.Bouncing ? ProjectileKinds.BladeSpinDegrees : ProjectileKinds.Spins(kind) ? ProjectileKinds.AxeSpinDegrees : 0f,
            });
        }

        public void Tick(float deltaTime, EnemyManager world)
        {
            for (var i = flying.Count - 1; i >= 0; i--)
            {
                var shot = flying[i];
                var outcome = EnemyProjectileFlight.Step(ref shot.Position, ref shot.Travelled, shot.Velocity, deltaTime,
                    shot.MaxDistance, world.Nav, world.PlayerGround, HitRadius);

                // A bouncing blade turns off the wall and flies on.
                if (outcome == EnemyProjectileFlight.Outcome.Blocked && shot.Bounces > 0)
                {
                    shot.Bounces--;
                    walkable ??= c => world.Nav.IsWalkable(c);
                    shot.Velocity = ProjectileKinds.Bounce(shot.Position, shot.Velocity, walkable);
                    outcome = EnemyProjectileFlight.Outcome.Flying;
                }

                if (outcome == EnemyProjectileFlight.Outcome.Flying)
                {
                    shot.Transform.position = IsoMath.GroundToWorld(shot.Position);
                    if (shot.Spin != 0f)
                        shot.Transform.Rotate(0f, 0f, -shot.Spin * deltaTime);
                    else
                    {
                        var heading = IsoMath.GroundToWorld(shot.Velocity);
                        shot.Transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(heading.y, heading.x) * Mathf.Rad2Deg);
                    }
                    flying[i] = shot;
                    continue;
                }

                if (shot.Kind == ProjectileKind.Bomb)
                {
                    // It lands and blows up after its fuse, shown on the ground; a direct hit blows up at once.
                    var fuse = outcome == EnemyProjectileFlight.Outcome.HitPlayer ? 0.05f : EnemyRules.Telegraph(ProjectileKinds.BombFuse);
                    world.Hazards.Strike(shot.Position, ProjectileKinds.BombRadius, fuse, shot.Damage, 0f, 0f, shot.Level, shot.Color);
                }
                else
                {
                    if (outcome == EnemyProjectileFlight.Outcome.HitPlayer && world.Player != null && world.Player.IsAlive)
                    {
                        var lifeBefore = world.Player.Life;
                        world.Player.TakeHit(shot.Damage, shot.Level, shot.ArmorIgnore);
                        if (world.Player.Life < lifeBefore && shot.Shooter != null && shot.Shooter.IsAlive)
                            shot.Shooter.ApplyOnHitEffects(world, shot.Damage);
                    }
                    // A fireball that missed bursts where it stopped.
                    else if (shot.Kind == ProjectileKind.Fireball)
                        world.Hazards.Strike(shot.Position, ProjectileKinds.FireballBurstRadius, EnemyRules.Telegraph(ProjectileKinds.FireballBurstFill),
                            shot.Damage * ProjectileKinds.FireballBurstShare, 0f, 0f, shot.Level, shot.Color);
                }

                shot.Transform.gameObject.SetActive(false);
                spare.Push(shot.Transform);
                flying[i] = flying[flying.Count - 1];
                flying.RemoveAt(flying.Count - 1);
            }
        }

        Transform Create()
        {
            // A short bright streak: the ember sprite stretched along the flight.
            var go = GroundMarker.NewSprite("Arrow", TelegraphArt.Ember, ArrowColor, parent, 0);
            go.GetComponent<SpriteRenderer>().sortingLayerName = GameSortingLayers.Effects;
            go.transform.localScale = new Vector3(0.7f, 0.16f, 1f);
            return go.transform;
        }
    }
}
