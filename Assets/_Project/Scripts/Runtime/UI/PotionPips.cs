using UnityEngine;
using UnityEngine.UI;

namespace ARPG
{
    /// <summary>
    /// The auto-potion's charges right of the life bar, one pip per charge: lit when full, dim when spent. The pip
    /// that is refilling shows its kill progress as a partial fill. Read-only, like the rest of the top of the screen
    /// (Docs/06-ui-ux.md).
    /// </summary>
    public class PotionPips : MonoBehaviour
    {
        [SerializeField] Image[] fills;

        void AddPip()
        {
            var last = fills[fills.Length - 1].transform.parent as RectTransform;
            var copy = (RectTransform)Instantiate(last, last.parent);
            copy.name = $"Pip {fills.Length + 1}";
            var step = fills.Length > 1 ? last.anchoredPosition - ((RectTransform)fills[fills.Length - 2].transform.parent).anchoredPosition : new Vector2(last.sizeDelta.x + 8f, 0f);
            copy.anchoredPosition = last.anchoredPosition + step;
            System.Array.Resize(ref fills, fills.Length + 1);
            // Each pip is a dark square with its fill as the only child.
            fills[fills.Length - 1] = copy.GetChild(0).GetComponent<Image>();
        }

        void Update()
        {
            if (fills == null)
                return;

            var potion = GameSession.Current.Potion;
            // A charge beyond the scene's pips (Sister Ivy's blessing, 2026-10-08): another pip, copied from the last.
            if (potion.Capacity > fills.Length && fills.Length > 0 && fills[fills.Length - 1] != null)
                AddPip();
            for (var i = 0; i < fills.Length; i++)
            {
                if (fills[i] == null)
                    continue;

                float amount;
                if (i < potion.Charges)
                    amount = 1f;
                else if (i == potion.Charges)
                    amount = (float)potion.KillProgress / AutoPotion.KillsPerCharge;
                else
                    amount = 0f;

                // Filled images draw bottom-up, so a refilling pip visibly fills like a flask.
                if (!Mathf.Approximately(fills[i].fillAmount, amount))
                    fills[i].fillAmount = amount;
            }
        }
    }
}
