using System.Collections.Generic;
using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// The pure parts of the Wrathborn's auto-cast (Docs/02-classes-and-skills.md): which skills a level has unlocked,
    /// which ones a level up just unlocked, and the geometry its triggers use. Pure.
    /// </summary>
    public static class SkillRules
    {
        /// <summary>Bull Rush looks for its target within this angle either side of the movement direction.</summary>
        public const float ChargeConeHalfDegrees = 30f;

        /// <summary>A charge ends this far past the enemy that triggered it, so it runs through the line.</summary>
        public const float ChargeOvershoot = 1.5f;

        /// <summary>Whether a skill is usable at a character level.</summary>
        public static bool IsUnlocked(int unlockLevel, int characterLevel) => characterLevel >= unlockLevel;

        /// <summary>The indices of skills that unlock above <paramref name="fromLevel"/> and at or below
        /// <paramref name="toLevel"/>: what a level up (possibly several at once) just unlocked.</summary>
        public static List<int> NewlyUnlocked(IReadOnlyList<int> unlockLevels, int fromLevel, int toLevel)
        {
            var result = new List<int>();
            for (var i = 0; i < unlockLevels.Count; i++)
                if (unlockLevels[i] > fromLevel && unlockLevels[i] <= toLevel)
                    result.Add(i);
            return result;
        }

        /// <summary>Whether a point is ahead of a moving character: between the two distances and within the charge cone
        /// of the movement direction.</summary>
        public static bool IsAhead(Vector2 origin, Vector2 moveDirection, Vector2 point, float minRange, float maxRange)
        {
            var offset = point - origin;
            var distance = offset.magnitude;
            if (distance < minRange || distance > maxRange || moveDirection.sqrMagnitude < 1e-6f)
                return false;
            return Vector2.Angle(moveDirection, offset) <= ChargeConeHalfDegrees;
        }

        /// <summary>Where a charge toward a target ends: past it by <see cref="ChargeOvershoot"/>, but never further than
        /// <paramref name="maxDistance"/>.</summary>
        public static float ChargeDistance(float targetDistance, float maxDistance) =>
            Mathf.Min(targetDistance + ChargeOvershoot, maxDistance);
    }
}
