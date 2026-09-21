using System;
using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// The player's life. Enemies call <see cref="TakeHit"/>; armor is applied here. Life is carried between scenes
    /// through <see cref="GameSession"/>, so taking stairs does not heal. Death is announced through <see cref="Died"/>
    /// and handled by <see cref="DeathFlow"/>.
    /// </summary>
    public class PlayerHealth : MonoBehaviour
    {
        // There is no progression yet, so the character is level 1.
        const int CharacterLevel = 1;

        LifePool life;

        /// <summary>Raised once, when a hit kills the character.</summary>
        public event Action Died;

        public bool IsAlive => life != null && !life.IsDead;

        public float Life => life != null ? life.Current : 0f;

        public float MaxLife => life != null ? life.Max : CombatFormulas.CharacterBaseLife(CharacterLevel);

        public float Fraction => life != null ? life.Fraction : 1f;

        /// <summary>Total damage taken since the scene started, after armor. For tests and tuning.</summary>
        public float DamageTaken { get; private set; }

        public int HitsTaken { get; private set; }

        /// <summary>Armor of the character. Only a weapon exists so far, and weapons give none.</summary>
        public float Armor => 0f;

        void Awake()
        {
            life = new LifePool(CombatFormulas.CharacterBaseLife(CharacterLevel));
            life.SetFraction(GameSession.Current.LifeFraction);
        }

        /// <summary>Takes a hit from an attacker of the given level. The raw damage is reduced by armor here.</summary>
        public void TakeHit(float rawDamage, int attackerLevel)
        {
            if (!IsAlive)
                return;

            var damage = rawDamage * (1f - CombatFormulas.ArmorReduction(Armor, attackerLevel));
            DamageTaken += damage;
            HitsTaken++;

            var killed = life.TakeDamage(damage);
            GameSession.Current.LifeFraction = life.Fraction;
            if (killed)
                Died?.Invoke();
        }
    }
}
