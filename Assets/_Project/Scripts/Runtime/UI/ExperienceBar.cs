using UnityEngine;
using UnityEngine.UI;

namespace ARPG
{
    /// <summary>
    /// The thin XP bar under the life bar and the level beside it (Docs/06-ui-ux.md puts the level in the read-only
    /// top of the screen). Like <see cref="LifeBar"/>, the fill's right anchor tracks progress, so it needs no art.
    /// </summary>
    public class ExperienceBar : MonoBehaviour
    {
        [SerializeField] RectTransform fill;
        [SerializeField] Text levelLabel;

        int shownLevel = -1;

        void Update()
        {
            var progress = GameSession.Current.Progress;
            if (fill != null)
                fill.anchorMax = new Vector2(progress.Fraction, 1f);

            // Only rebuild the string when the level changes, so the bar does not allocate every frame.
            if (levelLabel != null && progress.Level != shownLevel)
            {
                shownLevel = progress.Level;
                levelLabel.text = shownLevel.ToString();
            }
        }
    }
}
