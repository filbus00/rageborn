using UnityEngine;
using UnityEngine.UI;

namespace ARPG
{
    /// <summary>
    /// A small text readout of gold, the backpack and the equipped weapon. It stands in for the inventory screen,
    /// which does not exist yet, so found loot can be seen.
    /// Building the text allocates (about 220 bytes measured), and pickups arrive in bursts during a fight, so the
    /// readout refreshes at most a few times a second. The real HUD must format numbers without allocating.
    /// </summary>
    public class LootHud : MonoBehaviour
    {
        // At most this often, in seconds, however many pickups arrive.
        const float RefreshInterval = 0.25f;

        [SerializeField] Text label;

        GameSession session;
        bool dirty;
        float nextRefresh;

        void Start()
        {
            session = GameSession.Current;
            session.Changed += MarkDirty;
            Refresh();
        }

        void OnDestroy()
        {
            if (session != null)
                session.Changed -= MarkDirty;
        }

        void Update()
        {
            if (!dirty || Time.unscaledTime < nextRefresh)
                return;

            Refresh();
        }

        void MarkDirty() => dirty = true;

        void Refresh()
        {
            dirty = false;
            nextRefresh = Time.unscaledTime + RefreshInterval;
            if (label == null || session == null)
                return;

            var weapon = session.Equipment.Weapon;
            var wearing = weapon == null ? "none (unarmed)" : weapon.Rarity + " item level " + weapon.ItemLevel;
            label.text = "Gold " + session.Gold + "    Bag " + session.Inventory.Count + "/" + session.Inventory.Capacity +
                         "\nWeapon: " + wearing + "    damage " + session.Equipment.WeaponDamage.ToString("F1");
        }
    }
}
