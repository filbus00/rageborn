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

        // A projectile's pooled look (2026-10-18): a root that moves, the body that turns or spins, a glow under it and
        // a tapered trail behind it along the flight.
        sealed class Visual
        {
            public Transform Root;
            public SpriteRenderer Body;
            public SpriteRenderer Glow;
            public SpriteRenderer Trail;
        }

        struct Shot
        {
            public Visual Look;
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
        readonly Stack<Visual> spare = new Stack<Visual>(16);

        public EnemyProjectiles(Transform parent) => this.parent = parent;

        // Made once, so the bounce check allocates nothing.
        System.Func<Vector2Int, bool> walkable;

        public int Count => flying.Count;

        public void Fire(EnemyController shooter, Vector2 origin, Vector2 velocity, float maxDistance, float damage, int level, float armorIgnore,
            Color color = default, ProjectileKind kind = ProjectileKind.Plain)
        {
            var look = spare.Count > 0 ? spare.Pop() : Create();
            look.Root.gameObject.SetActive(true);
            var tint = color.a > 0f ? color : ArrowColor;
            var renderer = look.Body;
            var art = ProjectileArt.For(kind);
            float glowSize, trailLength, trailWidth;
            if (art != null)
            {
                // A drawn thing (a knife, an axe, a bomb, a blade) in its own colours.
                renderer.sprite = art;
                renderer.color = Color.white;
                // About the size of the coloured bolts (2026-10-25, the owner: "make sure that all enemie projectiles are
                // about the size as the green attacks of the first enemies"; were 1.5 and 1.9, a knife hardly seen).
                look.Body.transform.localScale = kind == ProjectileKind.Axe || kind == ProjectileKind.Bomb ? new Vector3(2.6f, 2.6f, 1f) : new Vector3(2.4f, 2.4f, 1f);
                glowSize = 0.9f;
                trailLength = kind == ProjectileKind.Knife ? 1.0f : 0.8f;
                trailWidth = kind == ProjectileKind.Knife ? 0.16f : 0.22f;
            }
            else if (kind == ProjectileKind.Fireball)
            {
                // A white-hot heart in a big glow of its colour, a long burning tail.
                renderer.sprite = TelegraphArt.Glow;
                renderer.color = Color.Lerp(tint, Color.white, 0.65f);
                look.Body.transform.localScale = new Vector3(0.55f, 0.55f, 1f);
                glowSize = 1.5f;
                trailLength = 1.2f;
                trailWidth = 0.45f;
            }
            else
            {
                // A bolt: a bright core stretched along the flight (an arrow is thinner and paler).
                var bolt = color.a > 0f;
                renderer.sprite = TelegraphArt.Ember;
                renderer.color = Color.Lerp(tint, Color.white, 0.4f);
                // An arrow as big as a bolt (2026-10-25: arrows were thin and hard to see).
                look.Body.transform.localScale = bolt ? new Vector3(0.6f, 0.3f, 1f) : new Vector3(0.75f, 0.26f, 1f);
                glowSize = 0.9f;
                trailLength = bolt ? 1.0f : 1.1f;
                trailWidth = bolt ? 0.22f : 0.2f;
            }
            look.Glow.color = new Color(tint.r, tint.g, tint.b, art != null ? 0.4f : 0.55f);
            look.Glow.transform.localScale = new Vector3(glowSize, glowSize * 0.75f, 1f);
            look.Trail.color = new Color(tint.r, tint.g, tint.b, art != null ? 0.45f : 0.8f);
            look.Trail.transform.localScale = new Vector3(trailLength, trailWidth, 1f);
            look.Root.position = Elevation.World2(origin);
            var world = IsoMath.GroundToWorld(velocity);
            var heading = Quaternion.Euler(0f, 0f, Mathf.Atan2(world.y, world.x) * Mathf.Rad2Deg);
            look.Body.transform.rotation = heading;
            look.Trail.transform.rotation = heading;
            flying.Add(new Shot
            {
                Look = look,
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
                    walkable ??= c => world.Nav.IsOpenToShots(c);
                    shot.Velocity = ProjectileKinds.Bounce(shot.Position, shot.Velocity, walkable);
                    outcome = EnemyProjectileFlight.Outcome.Flying;
                }

                if (outcome == EnemyProjectileFlight.Outcome.Flying)
                {
                    shot.Look.Root.position = Elevation.World2(shot.Position);
                    var heading = IsoMath.GroundToWorld(shot.Velocity);
                    var turn = Quaternion.Euler(0f, 0f, Mathf.Atan2(heading.y, heading.x) * Mathf.Rad2Deg);
                    shot.Look.Trail.transform.rotation = turn;
                    if (shot.Spin != 0f)
                        shot.Look.Body.transform.Rotate(0f, 0f, -shot.Spin * deltaTime);
                    else
                        shot.Look.Body.transform.rotation = turn;
                    // A fireball's heart and glow flicker.
                    if (shot.Kind == ProjectileKind.Fireball)
                    {
                        var flicker = 1f + 0.12f * Mathf.Sin((shot.Travelled + Time.time) * 23f);
                        shot.Look.Glow.transform.localScale = new Vector3(1.5f * flicker, 1.1f * flicker, 1f);
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

                shot.Look.Root.gameObject.SetActive(false);
                spare.Push(shot.Look);
                flying[i] = flying[flying.Count - 1];
                flying.RemoveAt(flying.Count - 1);
            }
        }

        Visual Create()
        {
            var root = new GameObject("Projectile").transform;
            root.SetParent(parent, false);
            SpriteRenderer Part(string name, Sprite sprite, int order)
            {
                var renderer = GroundMarker.NewSprite(name, sprite, ArrowColor, root, order).GetComponent<SpriteRenderer>();
                renderer.sortingLayerName = GameSortingLayers.Effects;
                return renderer;
            }
            return new Visual
            {
                Root = root,
                Glow = Part("Glow", TelegraphArt.Glow, 0),
                Trail = Part("Trail", TelegraphArt.Streak, 1),
                Body = Part("Body", TelegraphArt.Ember, 2),
            };
        }
    }
}
