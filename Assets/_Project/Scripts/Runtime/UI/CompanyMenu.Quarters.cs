using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ARPG
{
    /// <summary>
    /// The company's quarters (2026-11-11, the owner: "it feels like there are too many vendors in quarters ... Reimagine
    /// it all. Remove the possibility of buying starting gear. We already have the blueprints"): three places in place
    /// of seven keepers, each drawn in the page itself with no sheet over it. The Yard (Brunhilde: the drills, and with
    /// them Miranda's flasks and Sylthara's lore), the Armoury (Thorin: the forge, the vault that was Elara's, and the
    /// loot he buys, which was Garrett's) and the Kennel (Lyriel: the wolf). Claude's grouping, to review (Docs/08).
    /// </summary>
    public partial class CompanyMenu
    {
        enum Place { Yard, Armoury, Kennel }

        Place place = Place.Yard;

        static readonly (Place place, string name, string keeper, string role, string line, string emblem, string portrait)[] Places =
        {
            (Place.Yard, "THE YARD", "Brunhilde", "Captain of the company",
                "Drills, flasks and old lore. What coins buy here, every archer after her keeps.", "emblem_sword", "portrait_brunhilde"),
            (Place.Armoury, "ARMOURY", "Thorin", "Blacksmith and keeper of the vault",
                "Forges what your blueprints show, keeps what you store, and buys the rest.", "emblem_anvil", "portrait_thorin"),
            (Place.Kennel, "KENNEL", "Lyriel", "Beastmaster",
                "Trains the wolf that runs with every archer.", "emblem_paw", "portrait_lyriel"),
        };

        static readonly Color PipOff = new Color32(44, 54, 44, 255);

        void DrawQuarters()
        {
            // The three places, side by side.
            var row = Block("Places", 28f, null);
            for (var i = 0; i < Places.Length; i++)
            {
                var entry = Places[i];
                var on = entry.place == place;
                var frame = PixelUi.Frame(row, entry.name, on ? "frame_tab_on_b3" : "frame_tab_b3", true);
                var rect = frame.rectTransform;
                rect.anchorMin = new Vector2(i / 3f, 0f);
                rect.anchorMax = new Vector2((i + 1) / 3f, 1f);
                rect.offsetMin = new Vector2(i == 0 ? 0f : 2f, 0f);
                rect.offsetMax = new Vector2(i == 2 ? 0f : -2f, 0f);
                var button = frame.gameObject.AddComponent<Button>();
                button.targetGraphic = frame;
                var target = entry.place;
                button.onClick.AddListener(() =>
                {
                    Sfx.Play(SoundId.Hint);
                    place = target;
                    Redraw();
                    scroll.verticalNormalizedPosition = 1f;
                });
                var text = PixelUi.Label(frame.transform, entry.name, PixelUi.TitleFont, on ? PixelUi.Gold : PixelUi.TextDim, TextAnchor.MiddleCenter);
                UiStyle.Stretch(text.rectTransform);
                text.rectTransform.anchoredPosition = new Vector2(0f, 1f);
            }

            // Who keeps it.
            var here = Places[(int)place];
            var card = Block(here.keeper, 78f);
            var picture = PixelUi.Frame(card, "Picture", "frame_portrait_b3").rectTransform;
            PixelUi.At(picture, 4f, 4f, 77f, 70f);
            var face = PixelUi.Picture(picture, "Portrait", here.portrait).rectTransform;
            UiStyle.Place(face, new Vector2(0.5f, 0.5f), Vector2.zero, face.sizeDelta);
            var name = PixelUi.Label(card, here.keeper.ToUpperInvariant(), PixelUi.TitleFont, PixelUi.Gold);
            PixelUi.At(name.rectTransform, 88f, 8f, 200f, 12f);
            var role = PixelUi.Label(card, here.role.ToUpperInvariant(), PixelUi.BodyFont, PixelUi.GoldDim);
            PixelUi.Across(role.rectTransform, 23f, 10f, 88f, 30f);
            PixelUi.Across(PixelUi.Tiled(card, "Rule", "rule_dim").rectTransform, 35f, 1f, 88f, 30f);
            var line = PixelUi.Label(card, here.line, PixelUi.BodyFont, PixelUi.Text);
            PixelUi.Across(line.rectTransform, 40f, 34f, 88f, 12f);
            var emblem = PixelUi.Picture(card, "Emblem", here.emblem).rectTransform;
            UiStyle.Place(emblem, new Vector2(1f, 1f), new Vector2(-9f, -7f), emblem.sizeDelta);

            switch (place)
            {
                case Place.Yard:
                    DrawYard();
                    break;
                case Place.Armoury:
                    DrawArmoury();
                    break;
                default:
                    DrawKennel();
                    break;
            }
        }

        // --- The Yard ------------------------------------------------------------------------------------------------

        void DrawYard()
        {
            Heading("DRILLS");
            Upgrades(MetaShop.Training, "TRAIN");
            Heading("FLASKS");
            Upgrades(MetaShop.Potions, "BREW");
            Heading("LORE OF THE BOONS");
            Upgrades(MetaShop.Secrets, "LEARN");
        }

        void Upgrades(MetaShop shop, string verb)
        {
            foreach (var upgrade in MetaRules.All)
                if (upgrade.Shop == shop)
                    UpgradeCard(upgrade, verb);
        }

        // --- The Armoury ---------------------------------------------------------------------------------------------

        readonly List<string> blueprintKeys = new List<string>();

        void DrawArmoury()
        {
            Heading("THE FORGE");
            blueprintKeys.Clear();
            blueprintKeys.AddRange(session.Meta.Blueprints);
            blueprintKeys.Sort(StringComparer.Ordinal);
            if (blueprintKeys.Count == 0)
                Note("No blueprints yet. They drop now and then, most often from bosses and elites. Bring one home through the portal and " +
                     "Thorin knows it for good.");
            else
                Note("A forged piece is Rare. It goes into the bag, and every new archer starts in it.");
            foreach (var key in blueprintKeys)
            {
                if (!BlueprintRules.TryParse(key, out var kind, out var look))
                    continue;
                var cost = BlueprintRules.ForgeCost(look);
                var kit = session.Meta.ForgedFor(kind);
                var card = Block(key, 42f);
                Tile(card, 4f, 4f, 34f, new Item(kind, ItemRarity.Rare, ItemNames.LevelOfLook(kind, look)), null);
                var name = PixelUi.Label(card, ItemNames.LookName(kind, look).ToUpperInvariant(), PixelUi.TitleFont, PixelUi.Gold);
                PixelUi.Across(name.rectTransform, 7f, 12f, 44f, 90f);
                var now = kit != null ? $"Starting kit: {ItemComparison.Name(kit)}, level {kit.ItemLevel}" : "Not yet forged";
                var text = PixelUi.Label(card, $"{ItemComparison.KindName(kind)} <color=#CC342C>·</color> {now}", PixelUi.BodyFont, PixelUi.Text);
                PixelUi.Across(text.rectTransform, 23f, 12f, 44f, 90f);
                var captured = key;
                PriceButton(card, "FORGE", cost, session.Inventory.CanFit(new Item(kind, ItemRarity.Rare, 1)), () =>
                {
                    var item = session.Forge(captured);
                    if (item == null)
                        return;
                    Sfx.Play(SoundId.LevelUp, 0.7f);
                    (HintBanner.Current != null ? HintBanner.Current : HintBanner.Create()).Show($"Thorin forged a {ItemComparison.Name(item)}. It is in your bag.");
                });
            }

            // The vault: what is in it is safe from every death. A tap moves an item between it and the bag.
            var slots = session.Meta.StashSlots;
            Heading($"THE VAULT <color=#CC342C>·</color> {session.Stash.Count} OF {slots}");
            Tiles("Vault", session.Stash, slots, item =>
            {
                if (session.TakeFromStash(item))
                    Sfx.Play(SoundId.Pickup);
            });
            UpgradeCard(MetaRules.Get(MetaUpgradeId.StashSlot), "BUILD");

            var carried = session.Inventory.Items;
            Heading("IN THE BAG <color=#CC342C>·</color> TAP TO STORE");
            if (carried.Count == 0)
                Note("The bag is empty. What she brings home through the portal lies here.");
            else
                Tiles("Carried", carried, carried.Count, item =>
                {
                    if (session.StoreInStash(item))
                        Sfx.Play(SoundId.Pickup);
                });

            // Thorin buys what she brings home (Garrett's trade, 2026-10-17): the Bag opens with its prices.
            var loot = 0;
            foreach (var item in carried)
                if (!item.IsBlueprint)
                    loot++;
            var sell = Block("Sell", 42f);
            var title = PixelUi.Label(sell, "SELL LOOT", PixelUi.TitleFont, PixelUi.Gold);
            PixelUi.Across(title.rectTransform, 7f, 12f, 8f, 90f);
            var count = PixelUi.Label(sell, loot > 0 ? $"{loot} item{(loot == 1 ? "" : "s")} in the bag. One at a time, or a whole rarity." : "Nothing to sell.",
                PixelUi.BodyFont, PixelUi.Text);
            PixelUi.Across(count.rectTransform, 23f, 12f, 8f, 90f);
            TextButton(sell, "SELL", loot > 0, 78f, () => InventoryScreen.Current?.OpenAtMerchant());
        }

        // --- The Kennel ----------------------------------------------------------------------------------------------

        void DrawKennel()
        {
            var pets = session.Pets;
            var wolf = PetRules.Get(PetKind.Wolf);
            Heading(wolf.Name.ToUpperInvariant());
            Note($"{wolf.BonusText}. It runs with every archer and levels with her. Its skills are boons, found on contract.");

            Heading("TRAINING");
            Upgrades(MetaShop.Kennel, "TRAIN");

            Heading("HOW IT FIGHTS");
            var target = Block("Target", 34f);
            var label = PixelUi.Label(target, "Its target", PixelUi.BodyFont, PixelUi.Text, TextAnchor.MiddleLeft);
            UiStyle.Stretch(label.rectTransform);
            label.rectTransform.offsetMin = new Vector2(8f, 0f);
            TextButton(target, PetRules.TargetingText(pets.Targeting), true, 170f, pets.CycleTargeting);
            foreach (PetBehaviour behaviour in Enum.GetValues(typeof(PetBehaviour)))
            {
                if (behaviour == PetBehaviour.None)
                    continue;
                var on = pets.Has(behaviour);
                var row = Block(behaviour.ToString(), 34f);
                var text = PixelUi.Label(row, PetRules.BehaviourText(behaviour), PixelUi.BodyFont, on ? PixelUi.Text : PixelUi.TextDim, TextAnchor.MiddleLeft);
                UiStyle.Stretch(text.rectTransform);
                text.rectTransform.offsetMin = new Vector2(8f, 0f);
                text.rectTransform.offsetMax = new Vector2(-66f, 0f);
                var captured = behaviour;
                TextButton(row, on ? "ON" : "OFF", true, 54f, () => pets.Toggle(captured), on);
            }
        }

        // --- The pieces ----------------------------------------------------------------------------------------------

        // A section's name, with a thin rule running on from it.
        void Heading(string text)
        {
            var row = Block("Heading", 16f, null);
            var label = PixelUi.Label(row, text, PixelUi.BodyFont, PixelUi.GoldDim);
            PixelUi.Across(label.rectTransform, 5f, 10f, 2f, 0f);
            PixelUi.Across(PixelUi.Tiled(row, "Rule", "rule_dim").rectTransform, 10f, 1f, label.preferredWidth + 9f, 0f);
        }

        // A line or two of plain words under a heading.
        void Note(string text)
        {
            var stack = Stack(content, "Note", null, 2);
            stack.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(2, 2, 0, 2);
            PixelUi.Label(stack, text, PixelUi.BodyFont, PixelUi.TextDim);
        }

        // An upgrade bought with coins: its name, a pip a rank, what the next rank gives, and its price.
        void UpgradeCard(MetaUpgrade upgrade, string verb)
        {
            var rank = session.Meta.Rank(upgrade.Id);
            var maxed = session.Meta.IsMaxed(upgrade.Id);
            var card = Block(upgrade.Name, 42f);
            var name = PixelUi.Label(card, upgrade.Name.ToUpperInvariant(), PixelUi.TitleFont, maxed ? PixelUi.GoldDim : PixelUi.Gold);
            PixelUi.At(name.rectTransform, 8f, 7f, 260f, 12f);
            var x = 8f + name.preferredWidth + 6f;
            for (var i = 0; i < upgrade.MaxRanks; i++)
            {
                var pip = UiStyle.Image(card, "Pip", i < rank ? PixelUi.Gold : PipOff);
                PixelUi.At(pip.rectTransform, x + i * 6f, 10f, 4f, 4f);
            }
            var text = PixelUi.Label(card, MetaRules.Describe(upgrade, maxed ? rank : rank + 1), PixelUi.BodyFont, PixelUi.Text);
            PixelUi.Across(text.rectTransform, 23f, 12f, 8f, 90f);
            if (maxed)
            {
                var done = PixelUi.Label(card, "MASTERED", PixelUi.BodyFont, PixelUi.GoldDim, TextAnchor.MiddleCenter);
                UiStyle.Place(done.rectTransform, new Vector2(1f, 0.5f), new Vector2(-6f, 0f), new Vector2(78f, 28f));
                return;
            }
            var id = upgrade.Id;
            PriceButton(card, verb, session.Meta.NextCost(id), true, () =>
            {
                if (session.BuyUpgrade(id))
                    Sfx.Play(SoundId.LevelUp, 0.6f);
            });
        }

        // A button at a card's right: what it does over its price in coins. Dim, and the price red, when the coins are short.
        void PriceButton(RectTransform card, string verb, int cost, bool allowed, Action onClick)
        {
            var afford = session.Gold >= cost;
            var frame = Pressable(card, verb, afford && allowed, 78f, onClick, false);
            var top = PixelUi.Label(frame, verb, PixelUi.BodyFont, afford && allowed ? PixelUi.Gold : PixelUi.TextDim, TextAnchor.UpperCenter);
            PixelUi.Across(top.rectTransform, 4f, 10f, 0f, 0f);
            var price = PixelUi.Label(frame, cost.ToString("N0"), PixelUi.BodyFont, afford ? PixelUi.Text : PixelUi.Red, TextAnchor.UpperCenter);
            PixelUi.Across(price.rectTransform, 14f, 10f, 0f, 11f);
            var coin = PixelUi.Picture(frame, "Coin", "icon_coin").rectTransform;
            UiStyle.Place(coin, new Vector2(0.5f, 1f), new Vector2(price.preferredWidth / 2f + 1f, -14f), coin.sizeDelta);
            coin.pivot = new Vector2(0.5f, 1f);
        }

        // A button at a card's right with one word on it.
        void TextButton(RectTransform card, string label, bool enabled, float width, Action onClick, bool lit = false)
        {
            var frame = Pressable(card, label, enabled, width, onClick, lit);
            var text = PixelUi.Label(frame, label, PixelUi.BodyFont, enabled ? PixelUi.Gold : PixelUi.TextDim, TextAnchor.MiddleCenter);
            UiStyle.Stretch(text.rectTransform);
        }

        RectTransform Pressable(RectTransform card, string name, bool enabled, float width, Action onClick, bool lit)
        {
            var frame = PixelUi.Frame(card, name, lit ? "frame_tab_on_b3" : enabled ? "frame_button_b5" : "frame_tab_b3", enabled);
            UiStyle.Place(frame.rectTransform, new Vector2(1f, 0.5f), new Vector2(-6f, 0f), new Vector2(width, 28f));
            if (enabled)
            {
                var button = frame.gameObject.AddComponent<Button>();
                button.targetGraphic = frame;
                button.onClick.AddListener(() =>
                {
                    onClick();
                    Redraw();
                });
            }
            return frame.rectTransform;
        }

        // Items in rows of ten, each a tile; the slots past the last item empty. A tap gives the item to onTap.
        void Tiles(string name, IReadOnlyList<Item> items, int slots, Action<Item> onTap)
        {
            const int perRow = 10;
            const float gap = 2f;
            var width = ((RectTransform)canvas.transform).rect.width - Edge * 2f;
            if (width <= 0f)
                width = PixelUi.DesignWidth - Edge * 2f;
            var size = Mathf.Floor((width - gap * (perRow - 1)) / perRow);
            var rows = Mathf.Max(1, (slots + perRow - 1) / perRow);
            var grid = Block(name, rows * (size + gap) - gap, null);
            var inner = UiStyle.Rect(grid, "Tiles");
            UiStyle.Place(inner, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(perRow * (size + gap) - gap, rows * (size + gap) - gap));
            for (var i = 0; i < slots; i++)
                Tile(inner, i % perRow * (size + gap), i / perRow * (size + gap), size, i < items.Count ? items[i] : null, onTap);
        }

        void Tile(RectTransform parent, float x, float y, float size, Item item, Action<Item> onTap)
        {
            var frame = PixelUi.Frame(parent, item != null ? "Item" : "Empty", "frame_inset_b3", item != null && onTap != null);
            PixelUi.At(frame.rectTransform, x, y, size, size);
            if (item == null)
                return;
            var icon = UiStyle.Image(frame.transform, "Icon", Color.white);
            icon.sprite = ItemIcons.For(item);
            icon.preserveAspect = true;
            UiStyle.Stretch(icon.rectTransform, 4f);
            var rarity = UiStyle.Image(frame.transform, "Rarity", LootColors.Of(item.Rarity));
            PixelUi.At(rarity.rectTransform, 3f, size - 5f, size - 6f, 2f);
            if (onTap == null)
                return;
            var button = frame.gameObject.AddComponent<Button>();
            button.targetGraphic = frame;
            button.onClick.AddListener(() =>
            {
                onTap(item);
                Redraw();
            });
        }
    }
}
