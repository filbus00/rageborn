using System;
using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// The player's life. Enemies call <see cref="TakeHit"/>; armor is applied here. Life is carried between scenes
    /// through <see cref="GameSession"/>, so taking stairs does not heal. Death is announced through <see cref="Died"/>
    /// and handled by <see cref="DeathFlow"/>. Max life and armor read live from the equipped gear, so the inventory
    /// screen changing equipment updates them without a scene reload.
    /// </summary>
    public class PlayerHealth : MonoBehaviour
    {
        static readonly Color DodgeColor = new Color(0.6f, 0.9f, 1f);
        static readonly Color HurtFlashColor = new Color(1f, 0.25f, 0.2f);

        LifePool life;
        GameSession session;
        PlayerController player;

        // Far in the past, so a fresh character counts as not recently hit.
        float lastHitTime = -1000f;

        /// <summary>Raised once, when a hit kills the character.</summary>
        public event Action Died;

        /// <summary>Raised with the damage of every hit that lands (not a dodged one). The Wrathborn gains Rage from it.</summary>
        public event Action<float> HitTaken;

        // Flavor randomness like the crit roll, not economy: not seeded.
        readonly System.Random dodgeRandom = new System.Random();

        public bool IsAlive => life != null && !life.IsDead;

        public float Life => life != null ? life.Current : 0f;

        public float MaxLife => life != null ? life.Max : ComputeMaxLife();

        public float Fraction => life != null ? life.Fraction : 1f;

        /// <summary>Total damage taken since the scene started, after armor. For tests and tuning.</summary>
        public float DamageTaken { get; private set; }

        public int HitsTaken { get; private set; }

        /// <summary>Seconds since the character last took a hit. Auto-loot waits for this to pass 1.5.</summary>
        public float SecondsSinceLastHit => Time.time - lastHitTime;

        /// <summary>Armor from equipped Chest and Helm pieces: their base value by item level plus any Armor affix.</summary>
        public float Armor =>
            (GameSession.Current.Equipment.TotalArmor + CharacterAttributes.At(GameSession.Current.Level).Armor) *
            (1f + GameSession.Current.PassiveTree.Bonuses.ArmorPercent);

        void Awake()
        {
            // This object is a bare data holder, not positioned on the player, so the player's own transform is
            // needed to place a damage number correctly.
            player = FindAnyObjectByType<PlayerController>();
            session = GameSession.Current;
            life = new LifePool(ComputeMaxLife());
            life.SetFraction(session.LifeFraction);
            session.Changed += HandleEquipmentChanged;
            session.LeveledUp += HandleLeveledUp;
        }

        void OnDestroy()
        {
            if (session != null)
            {
                session.Changed -= HandleEquipmentChanged;
                session.LeveledUp -= HandleLeveledUp;
            }
        }

        /// <summary>Takes a hit from an attacker of the given level. The raw damage is reduced by armor here.
        /// <paramref name="armorIgnorePercent"/> is the share of armor the attack ignores, 0.15 for an elite's 15
        /// percent (Docs/03-itemization.md). Docs/01: dodge (from Momentum) applies to projectiles and melee, not to
        /// ground telegraph shapes, which are avoided only by moving; those pass <paramref name="dodgeable"/> false.
        /// Stillness reduces the damage.</summary>
        public void TakeHit(float rawDamage, int attackerLevel, float armorIgnorePercent = 0f, bool dodgeable = true)
        {
            if (!IsAlive)
                return;

            var stance = player != null ? player.Stance : null;
            var tree = session.PassiveTree.Bonuses;
            var dodge = Mathf.Min(0.5f, stance != null
                ? stance.DodgeChance + tree.Dodge + CharacterAttributes.At(session.Level).Dodge + session.Equipment.DodgePercent / 100f
                : 0f);
            if (dodgeable && stance != null && dodgeRandom.NextDouble() < dodge)
            {
                if (player != null)
                    DamageNumbers.Current?.ShowText(player.transform.position + Vector3.up * 1.2f, "DODGE", DodgeColor, 34);
                return;
            }
            // A shield blocks a melee hit or a projectile completely, rolled apart from dodge (Docs/03, Q10).
            var block = session.Equipment.BlockChance;
            if (dodgeable && block > 0f && dodgeRandom.NextDouble() < block)
            {
                if (player != null)
                    DamageNumbers.Current?.ShowText(player.transform.position + Vector3.up * 1.2f, "BLOCK", DodgeColor, 34);
                Sfx.Play(SoundId.Hit, 0.5f);
                return;
            }

            var effectiveArmor = Armor * (1f - Mathf.Clamp01(armorIgnorePercent));
            var damage = rawDamage * (1f - CombatFormulas.ArmorReduction(effectiveArmor, attackerLevel));
            if (stance != null)
                damage *= 1f - stance.DamageReduction;
            // The passive tree (Docs/02): Scar Tissue against elites and bosses (the attacks that ignore armor),
            // Unbroken below 35 percent life, Juggernaut per Momentum stack.
            if (armorIgnorePercent > 0f)
                damage *= 1f - tree.LessDamageFromElites;
            if (Fraction < 0.35f)
                damage *= 1f - tree.LowLifeReduction;
            if (tree.Juggernaut && stance != null)
                damage *= 1f - Mathf.Min(0.5f, 0.03f * stance.Momentum);
            DamageTaken += damage;
            HitsTaken++;
            lastHitTime = Time.time;

            if (player != null)
            {
                DamageNumbers.Current?.Show(player.transform.position, damage, critical: false, isDamageToPlayer: true);
                player.Flash(HurtFlashColor, 0.14f);
            }

            var killed = life.TakeDamage(damage);
            session.LifeFraction = life.Fraction;
            HitTaken?.Invoke(damage);
            Sfx.Play(SoundId.Hurt);
            if (killed)
                Died?.Invoke();
        }

        /// <summary>Heals the character, for example from a Life on Hit affix. No effect once dead.</summary>
        public void Heal(float amount)
        {
            if (!IsAlive || amount <= 0f)
                return;

            life.Heal(amount);
            session.LifeFraction = life.Fraction;
        }

        void HandleEquipmentChanged() => life.SetMax(ComputeMaxLife());

        /// <summary>Follows a change to the passive tree's life bonus (the session raises Modified for it, not Changed).</summary>
        void Update()
        {
            var max = ComputeMaxLife();
            if (!Mathf.Approximately(max, life.Max))
                life.SetMax(max);
        }

        // A level up refills life. The session already holds the full fraction; the pool follows it.
        void HandleLeveledUp(int level) => life.SetFraction(session.LifeFraction);

        static float ComputeMaxLife() =>
            (CombatFormulas.CharacterLife(GameSession.Current.Level) + GameSession.Current.Equipment.TotalLifeBonus) *
            (1f + GameSession.Current.PassiveTree.Bonuses.LifePercent);
    }
}
