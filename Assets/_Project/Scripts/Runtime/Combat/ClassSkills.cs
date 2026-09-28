using System.Collections.Generic;
using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// The class's skills in fill order (the same list as <see cref="PlayerCombat"/>'s), kept in Resources so a scene
    /// without combat (the town) can show the loadout. Written by Tools > ARPG > Create Wrathborn Skills. Found when the
    /// Skills page would not open in town: it looked for the combat, which the town does not have.
    /// </summary>
    [CreateAssetMenu(menuName = "ARPG/Class Skills", fileName = "ClassSkills")]
    public class ClassSkills : ScriptableObject
    {
        public const string ResourcePath = "ClassSkills";

        [SerializeField] SkillDefinition[] skills = new SkillDefinition[0];

        public IReadOnlyList<SkillDefinition> Skills => skills;

        static ClassSkills loaded;

        /// <summary>The list, or null when the asset has not been built.</summary>
        public static ClassSkills Load() => loaded != null ? loaded : loaded = Resources.Load<ClassSkills>(ResourcePath);

        public SkillDefinition Find(string id)
        {
            foreach (var skill in skills)
                if (skill != null && skill.name == id)
                    return skill;
            return null;
        }

        public List<(string id, int unlockLevel)> FillList()
        {
            var list = new List<(string, int)>(skills.Length);
            foreach (var skill in skills)
                if (skill != null)
                    list.Add((skill.name, skill.UnlockLevel));
            return list;
        }

#if UNITY_EDITOR
        public void Set(SkillDefinition[] value) => skills = value;
#endif
    }
}
