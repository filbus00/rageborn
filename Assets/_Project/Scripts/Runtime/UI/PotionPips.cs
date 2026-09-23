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

        void Update()
        {
            if (fills == null)
                return;

            var potion = GameSession.Current.Potion;
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
