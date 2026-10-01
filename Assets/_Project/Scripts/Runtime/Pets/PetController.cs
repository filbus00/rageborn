using System.Collections.Generic;
using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// The pet following the character (Docs/02, Pets; the owner's decisions of 2026-09-30): it fights an enemy chosen
    /// by its targeting rule, draws hits (an enemy beside it may strike it instead of the character), fetches gold and
    /// items to the character when it has nothing to fight, and follows otherwise. Its level is the character's. At no
    /// life it is knocked out and comes back after <see cref="PetRules.KnockedOutSeconds"/>. Like the enemies it lives in
    /// ground space with no physics body, walking around walls with the enemies' nav grid where there is one (none in
    /// town). Placeholder art: a small figure in its kind's colour with its name, until pets have art (Docs/09).
    /// Spawned by <see cref="PetDirector"/>.
    /// </summary>
    public class PetController : MonoBehaviour
    {
        // Past this far from the character, or stuck this long, it jumps back to the character's side.
        const float LeashDistance = 14f;
        const float StuckSeconds = 2f;
        const float FollowGap = 1.3f;
        const float StepPiece = 0.25f;
        const float RegenPerSecond = 0.05f;
        const float CarryHandOver = 1.5f;
        const float PickUpReach = 0.5f;
        const float GuardRadius = 2.5f;
        const float BodyRadius = 0.35f;

        public static PetController Current { get; private set; }

        PetKind kind;
        PetDefinition definition;
        PlayerController player;
        PlayerHealth health;
        PlayerCombat combat;
        EnemyManager enemies;
        LootDirector loot;

        Vector2 ground;
        float life;
        float attackTimer;
        float knockedOut;
        float stuckTimer;
        Vector2 lastGround;
        LootDrop carrying;
        Transform model;

        readonly List<EnemyController> near = new List<EnemyController>(16);
        readonly List<EnemyController> candidates = new List<EnemyController>(16);
        readonly List<Vector2> positions = new List<Vector2>(16);
        readonly List<float> fractions = new List<float>(16);
        readonly List<bool> elites = new List<bool>(16);

        public PetKind Kind => kind;
        public Vector2 GroundPosition => ground;
        public bool IsKnockedOut => knockedOut > 0f;
        public float Life => life;

        /// <summary>Builds the pet beside the character.</summary>
        public static PetController Spawn(PetKind kind, PlayerController player)
        {
            var go = new GameObject("Pet " + kind);
            var pet = go.AddComponent<PetController>();
            pet.kind = kind;
            pet.definition = PetRules.Get(kind);
            pet.player = player;
            pet.health = FindAnyObjectByType<PlayerHealth>();
            pet.combat = FindAnyObjectByType<PlayerCombat>();
            pet.enemies = FindAnyObjectByType<EnemyManager>();
            pet.loot = FindAnyObjectByType<LootDirector>();

            // A small figure: the NPC placeholder scaled down, lower and longer for the wolf and boar.
            var modelObject = new GameObject("Model");
            modelObject.transform.SetParent(go.transform, false);
            modelObject.transform.localScale = kind == PetKind.Raven ? new Vector3(0.45f, 0.45f, 1f) : new Vector3(0.75f, 0.55f, 1f);
            TravelArt.Figure(modelObject.transform, pet.definition.Color);
            TravelArt.Label(go.transform, pet.definition.Name, new Color(0.85f, 0.85f, 0.75f), kind == PetKind.Raven ? 0.9f : 0.8f);
            pet.model = modelObject.transform;

            pet.PlaceBeside(IsoMath.WorldToGround(player.transform.position));
            pet.life = PetRules.MaxLife(kind, GameSession.Current.Level);
            return pet;
        }

        void OnEnable() => Current = this;

        void OnDisable()
        {
            if (Current == this)
                Current = null;
        }

        /// <summary>
        /// Called by an enemy whose blow is about to land on the character: an enemy that stands beside the pet may
        /// strike the pet instead (its kind's aggro chance). Returns whether the pet took the hit.
        /// </summary>
        public bool TryTakeHit(EnemyController attacker, float damage)
        {
            if (knockedOut > 0f || attacker == null)
                return false;
            if (Vector2.Distance(attacker.GroundPosition, ground) > attacker.Definition.AttackRange + attacker.Definition.BodyRadius + BodyRadius)
                return false;
            if (Random.value >= definition.AggroChance)
                return false;

            life -= damage;
            var world = IsoMath.GroundToWorld(ground);
            DamageNumbers.Current?.Show(new Vector3(world.x, world.y + 0.6f, 0f), damage, false, isDamageToPlayer: true);
            if (life <= 0f)
                KnockOut();
            return true;
        }

        void KnockOut()
        {
            life = 0f;
            knockedOut = PetRules.KnockedOutSeconds;
            DropCarried();
            var world = IsoMath.GroundToWorld(ground);
            DamageNumbers.Current?.ShowText(new Vector3(world.x, world.y + 1f, 0f), definition.Name.ToUpperInvariant() + " IS DOWN", definition.Color, 32);
            SetVisible(false);
        }

        void Update()
        {
            if (player == null)
                return;
            var deltaTime = Time.deltaTime;
            var session = GameSession.Current;
            var maxLife = PetRules.MaxLife(kind, session.Level);
            var playerGround = IsoMath.WorldToGround(player.transform.position);

            if (knockedOut > 0f)
            {
                knockedOut -= deltaTime;
                if (knockedOut > 0f)
                    return;
                PlaceBeside(playerGround);
                life = maxLife;
                SetVisible(true);
            }

            attackTimer = Mathf.Max(0f, attackTimer - deltaTime);
            var rules = session.Pets;
            var target = ChooseTarget(rules, playerGround);
            if (target == null)
                life = Mathf.Min(maxLife, life + maxLife * RegenPerSecond * deltaTime);

            Vector2 goal;
            var stopAt = 0.2f;
            if (target != null)
            {
                DropCarried();
                goal = target.GroundPosition;
                stopAt = PetRules.BiteReach + target.Definition.BodyRadius;
                if (Vector2.Distance(ground, goal) <= stopAt + 0.1f && attackTimer <= 0f)
                    Bite(target);
            }
            else if (Fetch(rules, playerGround, out var fetchGoal))
            {
                goal = fetchGoal;
            }
            else
            {
                goal = FollowPoint(playerGround);
            }

            Walk(goal, stopAt, deltaTime);

            // Carried loot rides along at the pet's feet.
            if (carrying != null)
                carrying.MoveTo(ground);

            // Too far, or stuck on a wall: back to the character's side.
            stuckTimer = Vector2.Distance(ground, lastGround) < 0.01f && Vector2.Distance(ground, goal) > stopAt + 0.5f
                ? stuckTimer + deltaTime : 0f;
            lastGround = ground;
            if (Vector2.Distance(ground, playerGround) > LeashDistance || stuckTimer > StuckSeconds)
            {
                DropCarried();
                PlaceBeside(playerGround);
            }
            Draw(goal);
        }

        // The enemies the pet may consider, by its behaviours, then one of them by its targeting rule.
        EnemyController ChooseTarget(PetState rules, Vector2 playerGround)
        {
            if (enemies == null || !enemies.IsReady)
                return null;
            var guarding = rules.Has(PetBehaviour.GuardWhenLow) && health != null && health.Fraction < 0.5f;
            var radius = guarding ? GuardRadius : rules.Has(PetBehaviour.StayClose) ? PetRules.StayCloseRadius + 1f : PetRules.HuntRadius;
            enemies.QueryEnemies(playerGround, radius, near);
            candidates.Clear();
            positions.Clear();
            fractions.Clear();
            elites.Clear();
            var playerTarget = -1;
            var chosenByPlayer = combat != null ? combat.Target : null;
            for (var i = 0; i < near.Count; i++)
            {
                var enemy = near[i];
                if (!enemy.IsAlive || Vector2.Distance(enemy.GroundPosition, playerGround) > radius + enemy.Definition.BodyRadius)
                    continue;
                var rank = enemy.Definition.Rank;
                if (rank == EnemyRank.Boss && rules.Has(PetBehaviour.HoldBackFromBosses))
                    continue;
                if (enemy == chosenByPlayer)
                    playerTarget = candidates.Count;
                candidates.Add(enemy);
                positions.Add(enemy.GroundPosition);
                fractions.Add(enemy.MaxLife > 0f ? enemy.Life / enemy.MaxLife : 1f);
                elites.Add(rank == EnemyRank.Elite || rank == EnemyRank.Boss);
            }
            var index = PetRules.PickTarget(rules.Targeting, positions, fractions, elites, playerTarget, ground, playerGround);
            return index >= 0 ? candidates[index] : null;
        }

        void Bite(EnemyController target)
        {
            var session = GameSession.Current;
            var equipment = session.Equipment;
            // Pack Leader's Signet (Docs/03): the pet bites with the character's attack speed and crits, and its kills
            // give Focus.
            var signet = equipment.Wears(LegendaryId.PackLeadersSignet);
            var speed = signet ? 1f + equipment.AttackSpeedPercent / 100f + session.AttributeBonuses.AttackSpeed : 1f;
            attackTimer = definition.AttackSeconds / Mathf.Max(0.1f, speed);
            var critical = signet && Random.value < (equipment.CriticalChancePercent + session.AttributeBonuses.CriticalChance) / 100f;
            var criticalDamage = (equipment.CriticalDamagePercent + session.AttributeBonuses.CriticalDamage) / 100f;
            var damage = CombatFormulas.HitDamage(equipment.WeaponDamage, definition.DamageFactor, 0f, 0f, 1f,
                critical, criticalDamage, target.Definition.Armor, target.Level);
            var world = IsoMath.GroundToWorld(target.GroundPosition);
            DamageNumbers.Current?.Show(new Vector3(world.x, world.y, 0f), damage, critical, isDamageToPlayer: false);
            var killed = target.TakeDamage(damage);
            Sfx.Play(killed ? SoundId.Kill : SoundId.Hit, 0.5f);
            if (killed && signet && combat != null)
                combat.Focus.Gain(Legendaries.PackLeaderFocusOnKill);
        }

        // Loot within the kind's reach of the character and outside the character's own pick-up reach, fetched when the
        // rules allow; the drop is carried to the character and left at its feet for auto-loot. Returns where to walk.
        bool Fetch(PetState rules, Vector2 playerGround, out Vector2 goal)
        {
            goal = default;
            if (loot == null)
                return false;
            if (rules.Has(PetBehaviour.FetchOnlyWhenClear) && enemies != null && enemies.EngagedCount > 0)
            {
                DropCarried();
                return false;
            }
            if (carrying != null)
            {
                if (!Contains(loot.Active, carrying))
                {
                    carrying = null;
                    return false;
                }
                if (Vector2.Distance(ground, playerGround) <= CarryHandOver)
                {
                    carrying = null;
                    return false;
                }
                goal = playerGround;
                return true;
            }

            LootDrop best = null;
            var bestDistance = float.MaxValue;
            var drops = loot.Active;
            for (var i = 0; i < drops.Count; i++)
            {
                var drop = drops[i];
                var fromPlayer = Vector2.Distance(drop.GroundPosition, playerGround);
                if (fromPlayer > definition.FetchRadius || AutoLootRules.InRange(playerGround, drop.GroundPosition))
                    continue;
                var distance = Vector2.Distance(ground, drop.GroundPosition);
                if (distance < bestDistance)
                {
                    best = drop;
                    bestDistance = distance;
                }
            }
            if (best == null)
                return false;
            if (bestDistance <= PickUpReach)
                carrying = best;
            goal = best.GroundPosition;
            return true;
        }

        static bool Contains(IReadOnlyList<LootDrop> drops, LootDrop drop)
        {
            for (var i = 0; i < drops.Count; i++)
                if (drops[i] == drop)
                    return true;
            return false;
        }

        void DropCarried() => carrying = null;

        // A little behind and to the side of the character, the way it is moving.
        Vector2 FollowPoint(Vector2 playerGround)
        {
            var velocity = player.GroundVelocity;
            var back = velocity.sqrMagnitude > 0.04f ? -velocity.normalized : (ground - playerGround).normalized;
            if (back.sqrMagnitude < 1e-4f)
                back = Vector2.down;
            var side = new Vector2(-back.y, back.x) * 0.5f;
            return playerGround + (back + side).normalized * FollowGap;
        }

        // Straight toward the goal in short pieces; a wall blocks a piece, and the pet slides along it on one axis.
        void Walk(Vector2 goal, float stopAt, float deltaTime)
        {
            var toGoal = goal - ground;
            var distance = toGoal.magnitude;
            if (distance <= stopAt)
                return;
            var travel = Mathf.Min(distance - stopAt, definition.Speed * deltaTime);
            var direction = toGoal / distance;
            var nav = enemies != null && enemies.IsReady ? enemies.Nav : null;
            while (travel > 1e-4f)
            {
                var piece = Mathf.Min(StepPiece, travel);
                travel -= piece;
                var step = direction * piece;
                if (Walkable(nav, ground + step))
                    ground += step;
                else if (Walkable(nav, ground + new Vector2(step.x, 0f)))
                    ground += new Vector2(step.x, 0f);
                else if (Walkable(nav, ground + new Vector2(0f, step.y)))
                    ground += new Vector2(0f, step.y);
                else
                    break;
            }
        }

        static bool Walkable(NavGrid nav, Vector2 at) => nav == null || nav.IsWalkable(IsoMath.GroundToCell(at));

        void PlaceBeside(Vector2 playerGround)
        {
            ground = playerGround + new Vector2(-0.8f, -0.6f);
            var nav = enemies != null && enemies.IsReady ? enemies.Nav : null;
            if (!Walkable(nav, ground))
                ground = playerGround;
            lastGround = ground;
            stuckTimer = 0f;
            transform.position = IsoMath.GroundToWorld(ground);
        }

        void Draw(Vector2 goal)
        {
            transform.position = IsoMath.GroundToWorld(ground);
            // Faces where it is going, by flipping the figure.
            var dx = goal.x - ground.x;
            if (Mathf.Abs(dx) > 0.05f && model != null)
            {
                var scale = model.localScale;
                scale.x = Mathf.Abs(scale.x) * (dx < 0f ? -1f : 1f);
                model.localScale = scale;
            }
        }

        void SetVisible(bool visible)
        {
            foreach (var renderer in GetComponentsInChildren<Renderer>(true))
                renderer.enabled = visible;
        }
    }
}
