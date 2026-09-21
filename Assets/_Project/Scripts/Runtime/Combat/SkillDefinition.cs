using UnityEngine;

namespace ARPG
{
    /// <summary>When an auto-cast skill is allowed to fire. Docs/01-core-gameplay.md lists more, added as skills need them.</summary>
    public enum SkillTrigger
    {
        /// <summary>Fire whenever off cooldown, affordable and a target is in reach.</summary>
        Always,
    }

    /// <summary>
    /// Data for one melee sweep skill. Ranges are in ground units. The placeholder skill is Cleave from the Warden
    /// draft in Docs/02-classes-and-skills.md, until the Wrathborn's own skills are designed.
    /// </summary>
    [CreateAssetMenu(menuName = "ARPG/Skill Definition", fileName = "Skill")]
    public class SkillDefinition : ScriptableObject
    {
        [SerializeField] string displayName = "Cleave";

        [Tooltip("Focus spent on each cast. Docs: most skills cost 15 to 40, Cleave 15.")]
        [SerializeField, Min(0f)] float focusCost = 15f;

        [Tooltip("Seconds before the skill can fire again. Docs: Cleave 2.5.")]
        [SerializeField, Min(0f)] float cooldownSeconds = 2.5f;

        [Tooltip("Damage as a multiple of weapon damage. Docs: Cleave 180 percent, so 1.8.")]
        [SerializeField, Min(0f)] float damageMultiplier = 1.8f;

        [Tooltip("Width of the sweep in degrees. Docs: Cleave 200.")]
        [SerializeField, Range(10f, 360f)] float arcDegrees = 200f;

        [Tooltip("How far the sweep reaches from the character, in ground units. The docs give the class reach of 2.0 and no separate skill radius, so Cleave uses it.")]
        [SerializeField, Min(0.1f)] float range = 2f;

        [SerializeField] SkillTrigger trigger = SkillTrigger.Always;

        [Tooltip("Placeholder slash art. A wedge that points along +x, sized to a 1 unit diameter.")]
        [SerializeField] Sprite effectSprite;

        [Tooltip("Tint of the slash. Kept translucent so the character stays readable under it.")]
        [SerializeField] Color effectColor = new Color(1f, 0.75f, 0.35f, 0.55f);

        public string DisplayName => displayName;
        public float FocusCost => focusCost;
        public float CooldownSeconds => cooldownSeconds;
        public float DamageMultiplier => damageMultiplier;
        public float ArcDegrees => arcDegrees;
        public float Range => range;
        public SkillTrigger Trigger => trigger;
        public Sprite EffectSprite => effectSprite;
        public Color EffectColor => effectColor;
    }
}
