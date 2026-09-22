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
        // There is no progression yet, so the character is level 1.
        const int CharacterLevel = 1;

        LifePool life;
        GameSession session;

        // Far in the past, so a fresh character counts as not recently hit.
        float lastHitTime = -1000f;

        /// <summary>Raised once, when a hit kills the character.</summary>
        public event Action Died;

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
        public float Armor => GameSession.Current.Equipment.TotalArmor;

        void Awake()
        {
            session = GameSession.Current;
            life = new LifePool(ComputeMaxLife());
            life.SetFraction(session.LifeFraction);
            session.Changed += HandleEquipmentChanged;
        }

        void OnDestroy()
        {
            if (session != null)
                session.Changed -= HandleEquipmentChanged;
        }

        /// <summary>Takes a hit from an attacker of the given level. The raw damage is reduced by armor here.</summary>
        public void TakeHit(float rawDamage, int attackerLevel)
        {
            if (!IsAlive)
                return;

            var damage = rawDamage * (1f - CombatFormulas.ArmorReduction(Armor, attackerLevel));
            DamageTaken += damage;
            HitsTaken++;
            lastHitTime = Time.time;

            var killed = life.TakeDamage(damage);
            session.LifeFraction = life.Fraction;
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

        static float ComputeMaxLife() => CombatFormulas.CharacterBaseLife(CharacterLevel) + GameSession.Current.Equipment.TotalLifeBonus;
    }
}
