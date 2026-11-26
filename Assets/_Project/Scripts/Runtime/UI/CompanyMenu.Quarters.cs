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
        // The camp spots open these by number (CampSpot.Kind), so new places go at the end.
        enum Place { Yard, Armoury, Kennel, Fletcher, Jeweller }

        Place place = Place.Yard;

        static readonly (Place place, string name, string keeper, string role, string line, string emblem, string portrait)[] Places =
        {
            (Place.Yard, "THE YARD", "Brunhilde", "Captain of the company",
                "Drills, flasks and old lore. What coins buy here, every archer after her keeps.", "emblem_sword", "portrait_brunhilde"),
            (Place.Armoury, "ARMOURY", "Thorin", "Blacksmith and keeper of the vault",
                "Furnishes the company's armour, takes found armour apart for iron, keeps the vault.", "emblem_anvil", "portrait_thorin"),
            (Place.Fletcher, "FLETCHER", "Oswin", "Fletcher and bowyer",
                "Makes the company's bows and quivers, and strips found ones for heartwood.", "emblem_feather", "portrait_oswin"),
            (Place.Jeweller, "JEWELLER", "Mirela", "Jeweller and charm maker",
                "Cuts the company's rings and amulet, and found jewellery into rough gems.", "emblem_goblet", "portrait_mirela"),
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
                var n = (float)Places.Length;
                rect.anchorMin = new Vector2(i / n, 0f);
                rect.anchorMax = new Vector2((i + 1) / n, 1f);
                rect.offsetMin = new Vector2(i == 0 ? 0f : 1f, 0f);
                rect.offsetMax = new Vector2(i == Places.Length - 1 ? 0f : -1f, 0f);
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
            var here = Places[0];
            foreach (var entry in Places)
                if (entry.place == place)
                    here = entry;
            var card = Block(here.keeper, 78f);
            var picture = PixelUi.Frame(card, "Picture", "frame_portrait_b3").rectTransform;
            PixelUi.At(picture, 4f, 4f, 77f, 70f);
            var face = PixelUi.Picture(picture, "Portrait", PixelUi.Sprite(here.portrait) != null ? here.portrait : "portrait_archer").rectTransform;
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
                case Place.Fletcher:
                    DrawFletcher();
                    break;
                case Place.Jeweller:
                    DrawJeweller();
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

        void DrawArmoury()
        {
            Heading("COMPANY ARMOUR");
            CompanyGear(GearKeeper.Thorin);

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
                if (!item.IsBlueprint && !item.IsCompanyIssue)
                    loot++;
            var sell = Block("Sell", 42f);
            var title = PixelUi.Label(sell, "SELL LOOT", PixelUi.TitleFont, PixelUi.Gold);
            PixelUi.Across(title.rectTransform, 7f, 12f, 8f, 90f);
            var count = PixelUi.Label(sell, loot > 0 ? $"{loot} item{(loot == 1 ? "" : "s")} in the bag. One at a time, or a whole rarity." : "Nothing to sell.",
                PixelUi.BodyFont, PixelUi.Text);
            PixelUi.Across(count.rectTransform, 23f, 12f, 8f, 90f);
            TextButton(sell, "SELL", loot > 0, 78f, () => InventoryScreen.Current?.OpenAtMerchant());
        }

        // --- The fletcher and the jeweller (2026-11-26) --------------------------------------------------------------

        void DrawFletcher()
        {
            Heading("THE BOW SHE IS ISSUED");
            var choice = Block("Bow choice", 34f);
            var label = PixelUi.Label(choice, "Every archer leaves camp with", PixelUi.BodyFont, PixelUi.Text, TextAnchor.MiddleLeft);
            UiStyle.Stretch(label.rectTransform);
            label.rectTransform.offsetMin = new Vector2(8f, 0f);
            var longbow = session.Meta.IssuedBow == ItemSlot.TwoHandWeapon;
            TextButton(choice, "LONGBOW", true, 76f, () => session.ChooseIssuedBow(ItemSlot.TwoHandWeapon), longbow);
            var shortBow = Pressable(choice, "SHORT BOW", true, 76f, () => session.ChooseIssuedBow(ItemSlot.Weapon), !longbow);
            shortBow.anchoredPosition = new Vector2(-86f, 0f);
            var text = PixelUi.Label(shortBow, "SHORT BOW", PixelUi.BodyFont, PixelUi.Gold, TextAnchor.MiddleCenter);
            UiStyle.Stretch(text.rectTransform);

            Heading("COMPANY BOWS AND QUIVERS");
            CompanyGear(GearKeeper.Fletcher);
        }

        void DrawJeweller()
        {
            Heading("COMPANY RINGS AND AMULET");
            CompanyGear(GearKeeper.Jeweller);
        }

        // What the company's piece of each of a keeper's kinds is now, and raising it a rank with his material and coins.
        void CompanyGear(GearKeeper keeper)
        {
            var material = CompanyGearRules.MaterialOf(keeper);
            var stock = Block("Material", 26f);
            var picture = ItemIcons.Picture(ItemArtNames.ForMaterial(material));
            if (picture != null)
            {
                var icon = UiStyle.Image(stock, "Material", Color.white);
                icon.sprite = picture;
                PixelUi.At(icon.rectTransform, 6f, 5f, 16f, 16f);
            }
            var have = PixelUi.Label(stock, $"{CompanyGearRules.MaterialName(material).ToUpperInvariant()}  {session.Meta.Material(material)}",
                PixelUi.TitleFont, PixelUi.Gold, TextAnchor.MiddleLeft);
            UiStyle.Stretch(have.rectTransform);
            have.rectTransform.offsetMin = new Vector2(28f, 0f);
            var where = PixelUi.Label(stock, "Found in the levels, kept at once", PixelUi.BodyFont, PixelUi.TextDim, TextAnchor.MiddleRight);
            UiStyle.Stretch(where.rectTransform);
            where.rectTransform.offsetMax = new Vector2(-8f, 0f);

            foreach (var kind in CompanyGearRules.KindsOf(keeper))
                GearCard(kind);
            Note("Every archer starts a contract in the company's gear; a rank raised is hers at once. Tap a piece to wear it again.");
            Recycling(keeper, material);
        }

        // Found gear of the keeper's kinds in the bag, taken apart for his material (2026-11-26): a tap takes one, a
        // button every Magic piece (never a named legendary, a trinket or an upgrade).
        void Recycling(GearKeeper keeper, CompanyMaterial material)
        {
            var mine = new List<Item>();
            foreach (var item in session.Inventory.Items)
                if (CompanyGearRules.KeeperOf(item.Slot) == keeper && CompanyGearRules.RecycleYield(item) > 0)
                    mine.Add(item);
            var stuff = CompanyGearRules.MaterialName(material).ToLowerInvariant();
            Heading("RECYCLE <color=#CC342C>·</color> TAP A PIECE IN THE BAG");
            if (mine.Count == 0)
            {
                Note(keeper == GearKeeper.Jeweller
                    ? $"No found rings, amulets or trinkets in the bag. Bring them home and she cuts them into {stuff}."
                    : $"Nothing of his in the bag. Bring found gear home and he takes it apart for {stuff}.");
                return;
            }
            Tiles("Recycle", mine, mine.Count, item =>
            {
                var gained = session.Recycle(item);
                if (gained <= 0)
                    return;
                Sfx.Play(SoundId.Pickup);
                (HintBanner.Current != null ? HintBanner.Current : HintBanner.Create()).Show($"{ItemComparison.Name(item)} gave {gained} {stuff}.");
            });
            var magic = 0;
            foreach (var item in mine)
                if (item.Rarity == ItemRarity.Magic && item.Legendary == LegendaryId.None && !PowerScore.IsUpgrade(session.Equipment, item, session.Level, session.Talents.Bonuses))
                    magic += CompanyGearRules.RecycleYield(item);
            var row = Block("Recycle all", 34f);
            var label = PixelUi.Label(row, magic > 0 ? $"Every Magic piece but upgrades: {magic} {stuff}" : "No Magic pieces to spare.",
                PixelUi.BodyFont, PixelUi.Text, TextAnchor.MiddleLeft);
            UiStyle.Stretch(label.rectTransform);
            label.rectTransform.offsetMin = new Vector2(8f, 0f);
            TextButton(row, "ALL MAGIC", magic > 0, 84f, () =>
            {
                if (session.RecycleAll(keeper, ItemRarity.Magic) > 0)
                    Sfx.Play(SoundId.Pickup);
            });
        }

        void GearCard(ItemSlot kind)
        {
            var rank = session.Meta.GearRank(kind);
            var piece = CompanyGearRules.Make(kind, rank);
            var maxed = rank >= CompanyGearRules.MaxRank;
            var card = Block(kind.ToString(), 52f);
            var place = kind == ItemSlot.Shield ? ItemSlot.OffHand : kind == ItemSlot.TwoHandWeapon ? ItemSlot.Weapon : kind;
            var bowNotIssued = (kind == ItemSlot.Weapon || kind == ItemSlot.TwoHandWeapon) && session.Meta.IssuedBow != kind;
            Tile(card, 4f, 4f, 44f, piece, bowNotIssued ? null : _ =>
            {
                if (session.WearCompanyPiece(place))
                    Sfx.Play(SoundId.Pickup);
            });
            var name = PixelUi.Label(card, ItemComparison.Name(piece).ToUpperInvariant(), PixelUi.TitleFont, LootColors.Of(piece.Rarity));
            PixelUi.At(name.rectTransform, 54f, 5f, 200f, 12f);
            var x = 54f + Mathf.Min(150f, name.preferredWidth) + 6f;
            for (var i = 0; i < CompanyGearRules.MaxRank; i++)
            {
                var pip = UiStyle.Image(card, "Pip", i < rank ? PixelUi.Gold : PipOff);
                PixelUi.At(pip.rectTransform, x + i * 5f, 9f, 3f, 4f);
            }
            var kindName = CompanyGearRules.KindName(kind) + (bowNotIssued ? " (not issued)" : "");
            var info = PixelUi.Label(card, $"{kindName} <color=#CC342C>·</color> rank {rank} <color=#CC342C>·</color> item level {piece.ItemLevel}",
                PixelUi.BodyFont, PixelUi.TextDim);
            PixelUi.Across(info.rectTransform, 18f, 10f, 54f, 90f);
            var line = PixelUi.Label(card, maxed ? AffixLine(piece) : NextLine(piece, CompanyGearRules.Make(kind, rank + 1)),
                PixelUi.BodyFont, PixelUi.Text);
            PixelUi.Across(line.rectTransform, 30f, 20f, 54f, 90f);
            if (maxed)
            {
                var done = PixelUi.Label(card, "FINEST", PixelUi.BodyFont, PixelUi.GoldDim, TextAnchor.MiddleCenter);
                UiStyle.Place(done.rectTransform, new Vector2(1f, 0.5f), new Vector2(-6f, 0f), new Vector2(78f, 28f));
                return;
            }
            RaiseButton(card, kind, rank);
        }

        // The piece's affixes in a line ("Life 31 · Armor 12").
        static string AffixLine(Item item)
        {
            if (item.Affixes.Count == 0)
                return "No affixes yet";
            var parts = new List<string>();
            foreach (var affix in item.Affixes)
                parts.Add($"{ItemComparison.AffixLabel(affix.Id)} {Number(affix.Id, affix.Value)}");
            return string.Join(" <color=#CC342C>·</color> ", parts);
        }

        // What the next rank brings: a new affix, a better look, more of what it has.
        static string NextLine(Item now, Item next)
        {
            var parts = new List<string>();
            if (ItemComparison.Name(next) != ItemComparison.Name(now) && ItemNames.LookIndex(next.Slot, next.ItemLevel) != ItemNames.LookIndex(now.Slot, now.ItemLevel))
                parts.Add(ItemNames.BaseName(next.Slot, next.ItemLevel));
            foreach (var affix in next.Affixes)
            {
                if (now.AffixSum(affix.Id) <= 0f)
                    parts.Add($"<color=#F0C840>new: {ItemComparison.AffixLabel(affix.Id)} {Number(affix.Id, affix.Value)}</color>");
            }
            if (parts.Count == 0)
                parts.Add("every number up");
            return "Next rank: " + string.Join(", ", parts);
        }

        static string Number(AffixId id, float value) => ItemComparison.IsPercent(id) ? $"+{value:0.#}%" : $"+{value:0.#}";

        // A keeper's raise button: the material over the coins, dim and red where short.
        void RaiseButton(RectTransform card, ItemSlot kind, int rank)
        {
            var material = CompanyGearRules.MaterialOf(kind);
            var need = CompanyGearRules.MaterialCost(rank);
            var cost = CompanyGearRules.CoinCost(rank);
            var enough = session.Meta.Material(material) >= need;
            var afford = session.Gold >= cost;
            var frame = Pressable(card, "RAISE", enough && afford, 78f, () =>
            {
                if (!session.RaiseCompanyGear(kind))
                    return;
                Sfx.Play(SoundId.LevelUp, 0.7f);
                var piece = CompanyGearRules.Make(kind, session.Meta.GearRank(kind));
                (HintBanner.Current != null ? HintBanner.Current : HintBanner.Create()).Show($"The company's {ItemComparison.Name(piece)} is ready.");
            }, false);
            frame.sizeDelta = new Vector2(78f, 40f);
            var top = PixelUi.Label(frame, "RAISE", PixelUi.BodyFont, enough && afford ? PixelUi.Gold : PixelUi.TextDim, TextAnchor.UpperCenter);
            PixelUi.Across(top.rectTransform, 3f, 10f, 0f, 0f);
            var stuff = PixelUi.Label(frame, $"{need} {CompanyGearRules.MaterialName(material).ToLowerInvariant()}", PixelUi.BodyFont,
                enough ? PixelUi.Text : PixelUi.Red, TextAnchor.UpperCenter);
            PixelUi.Across(stuff.rectTransform, 14f, 10f, 0f, 0f);
            var price = PixelUi.Label(frame, cost.ToString("N0"), PixelUi.BodyFont, afford ? PixelUi.Text : PixelUi.Red, TextAnchor.UpperCenter);
            PixelUi.Across(price.rectTransform, 25f, 10f, 0f, 11f);
            var coin = PixelUi.Picture(frame, "Coin", "icon_coin").rectTransform;
            UiStyle.Place(coin, new Vector2(0.5f, 1f), new Vector2(price.preferredWidth / 2f + 1f, -25f), coin.sizeDelta);
            coin.pivot = new Vector2(0.5f, 1f);
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
