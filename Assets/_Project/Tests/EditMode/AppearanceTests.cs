using ARPG.Editor;
using NUnit.Framework;
using UnityEngine;

namespace ARPG.Tests
{
    public class AppearanceTests
    {
        [Test]
        public void Tiers_FollowActOnesItemLevelBands()
        {
            Assert.AreEqual(0, AppearanceRules.Tier(1));
            Assert.AreEqual(0, AppearanceRules.Tier(3));
            Assert.AreEqual(1, AppearanceRules.Tier(4));
            Assert.AreEqual(1, AppearanceRules.Tier(6));
            Assert.AreEqual(2, AppearanceRules.Tier(7));
            Assert.AreEqual(2, AppearanceRules.Tier(60), "the top act 1 look until later acts add tiers");
        }

        [Test]
        public void NothingEquipped_ShowsTheBareBody_BareHead_EmptyHands()
        {
            var look = AppearanceRules.For(new EquipmentState(new Item[3]));
            Assert.AreEqual(AppearanceRules.BareBody, look.Body);
            Assert.IsNull(look.Helm);
            Assert.IsNull(look.Weapon);
            Assert.IsNull(look.OffHand);
            Assert.AreEqual(CharacterGrip.OneHand, look.Grip);
        }

        [Test]
        public void EquippedGear_ShowsItsTiersLook()
        {
            var equipment = new EquipmentState(new Item[3])
                .With(ItemSlot.Chest, new Item(ItemSlot.Chest, ItemRarity.Common, 8))
                .With(ItemSlot.Helm, new Item(ItemSlot.Helm, ItemRarity.Magic, 2))
                .With(ItemSlot.Weapon, new Item(ItemSlot.Weapon, ItemRarity.Rare, 5));
            var look = AppearanceRules.For(equipment);
            Assert.AreEqual("mail", look.Body);
            Assert.AreEqual("cap", look.Helm);
            Assert.AreEqual("recurve_bow", look.Weapon);
        }

        [Test]
        public void Grip_TwoHandedWins_ThenShield_ThenOffHandWeapon()
        {
            Assert.AreEqual(CharacterGrip.TwoHand, AppearanceRules.GripFor(true, true, true));
            Assert.AreEqual(CharacterGrip.Shield, AppearanceRules.GripFor(false, false, true));
            Assert.AreEqual(CharacterGrip.DualWield, AppearanceRules.GripFor(false, true, false));
            Assert.AreEqual(CharacterGrip.OneHand, AppearanceRules.GripFor(false, false, false));
        }

        [Test]
        public void SheetNames_MatchWhatTheBakeWrites()
        {
            Assert.AreEqual("wrathborn_body_leather_2h_run",
                AppearanceRules.SheetName("wrathborn", AppearanceLayer.Body, "leather", CharacterGrip.TwoHand, "run"));

            var job = ScriptableObject.CreateInstance<SpriteBakeJob>();
            job.characterName = "wrathborn";
            job.bodies.Add(new SpriteBakeJob.Body { look = "leather" });
            job.pieces.Add(new SpriteBakeJob.Piece { layer = AppearanceLayer.OffHand, look = "buckler" });
            foreach (CharacterGrip grip in System.Enum.GetValues(typeof(CharacterGrip)))
                foreach (AppearanceLayer layer in System.Enum.GetValues(typeof(AppearanceLayer)))
                    Assert.AreEqual(
                        AppearanceRules.SheetName("wrathborn", layer, "x", grip, "attack"),
                        job.SheetName(layer, "x", AppearanceRules.GripCode(grip), "attack"));

            // A plain enemy: no layer, look or grip in its names.
            var enemy = ScriptableObject.CreateInstance<SpriteBakeJob>();
            enemy.characterName = "husk";
            enemy.bodies.Add(new SpriteBakeJob.Body());
            Assert.AreEqual("husk_run", enemy.SheetName(AppearanceLayer.Body, "", "", "run"));
            Object.DestroyImmediate(job);
            Object.DestroyImmediate(enemy);
        }

        [Test]
        public void DirectionRow_PicksTheSheetRowTheBakeRenderedThatWay()
        {
            for (var row = 0; row < SpriteBakeMath.DirectionCount; row++)
            {
                var facing = SpriteBakeMath.Facing(row);
                Assert.AreEqual(row, AppearanceRules.DirectionRow(new Vector2(facing.x, facing.z)), SpriteBakeMath.DirectionCodes[row]);
            }
            Assert.AreEqual(0, AppearanceRules.DirectionRow(new Vector2(0.2f, -1f)), "nearly south is south");
            Assert.AreEqual(0, AppearanceRules.DirectionRow(Vector2.zero), "standing still defaults to south");
        }
    }
}
