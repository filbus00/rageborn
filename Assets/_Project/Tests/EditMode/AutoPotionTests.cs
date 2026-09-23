using NUnit.Framework;

namespace ARPG.Tests
{
    public class AutoPotionTests
    {
        const float Frame = 1f / 60f;

        [Test]
        public void ANewPotion_IsFull_AndIdle()
        {
            var potion = new AutoPotion();

            Assert.AreEqual(AutoPotion.MaxCharges, potion.Charges);
            Assert.AreEqual(0, potion.KillProgress);
            Assert.IsFalse(potion.IsHealing);
        }

        [Test]
        public void ItDoesNotFire_AboveTheTrigger()
        {
            var potion = new AutoPotion();

            var heal = potion.Tick(Frame, 0.36f, true, out var fired);

            Assert.IsFalse(fired);
            Assert.AreEqual(0f, heal);
            Assert.AreEqual(AutoPotion.MaxCharges, potion.Charges);
        }

        [Test]
        public void ItFires_AtTheTrigger_UsingOneCharge()
        {
            var potion = new AutoPotion();

            potion.Tick(Frame, 0.35f, true, out var fired);

            Assert.IsTrue(fired);
            Assert.IsTrue(potion.IsHealing);
            Assert.AreEqual(AutoPotion.MaxCharges - 1, potion.Charges);
        }

        [Test]
        public void ItHealsFortyPercent_SpreadOverThreeSeconds()
        {
            var potion = new AutoPotion();
            var total = 0f;
            var firstSecond = 0f;

            // 3.5 seconds of frames: the heal ends on its own after 3.
            for (var t = 0; t < 210; t++)
            {
                var heal = potion.Tick(Frame, 0.2f, true, out _);
                total += heal;
                if (t < 60)
                    firstSecond += heal;
                if (t == 0)
                    Assert.AreEqual(AutoPotion.MaxCharges - 1, potion.Charges);
            }

            Assert.AreEqual(0.4f, firstSecond * 3f, 1e-3f, "evenly spread");
            // Still at 0.2 life the whole time (the caller would normally apply the heal), so a second potion fires
            // once the first ends: the cooldown is the heal itself.
            Assert.AreEqual(AutoPotion.MaxCharges - 2, potion.Charges);
            Assert.Greater(total, 0.4f);
        }

        [Test]
        public void ASecondPotion_DoesNotFire_WhileTheFirstIsHealing()
        {
            var potion = new AutoPotion();
            potion.Tick(Frame, 0.1f, true, out _);

            potion.Tick(1f, 0.1f, true, out var firedAgain);

            Assert.IsFalse(firedAgain);
            Assert.AreEqual(AutoPotion.MaxCharges - 1, potion.Charges);
        }

        [Test]
        public void OneHealTotalsExactlyFortyPercent()
        {
            var potion = new AutoPotion(charges: 1);
            var total = 0f;
            for (var t = 0; t < 400; t++)
                total += potion.Tick(Frame, 0.1f, true, out _);

            Assert.AreEqual(0.4f, total, 1e-4f);
            Assert.AreEqual(0, potion.Charges);
        }

        [Test]
        public void WithNoCharges_NothingFires()
        {
            var potion = new AutoPotion(charges: 0);

            var heal = potion.Tick(Frame, 0.05f, true, out var fired);

            Assert.IsFalse(fired);
            Assert.AreEqual(0f, heal);
        }

        [Test]
        public void ADeadCharacter_NeitherFiresNorHeals_AndDeathStopsAHeal()
        {
            var potion = new AutoPotion();
            potion.Tick(Frame, 0.2f, true, out _);

            var heal = potion.Tick(Frame, 0f, false, out var fired);

            Assert.IsFalse(fired);
            Assert.AreEqual(0f, heal);
            Assert.IsFalse(potion.IsHealing);
        }

        [Test]
        public void KillsRefillACharge_AtTen()
        {
            var potion = new AutoPotion(charges: 1);

            for (var i = 0; i < AutoPotion.KillsPerCharge - 1; i++)
                potion.RegisterKill();
            Assert.AreEqual(1, potion.Charges);
            Assert.AreEqual(AutoPotion.KillsPerCharge - 1, potion.KillProgress);

            potion.RegisterKill();
            Assert.AreEqual(2, potion.Charges);
            Assert.AreEqual(0, potion.KillProgress);
        }

        [Test]
        public void KillsWhileFull_AreNotBanked()
        {
            var potion = new AutoPotion();

            for (var i = 0; i < 25; i++)
                potion.RegisterKill();

            Assert.AreEqual(AutoPotion.MaxCharges, potion.Charges);
            Assert.AreEqual(0, potion.KillProgress);
        }

        [Test]
        public void Refill_RestoresEveryCharge()
        {
            var potion = new AutoPotion(charges: 0, killProgress: 4);

            potion.Refill();

            Assert.AreEqual(AutoPotion.MaxCharges, potion.Charges);
            Assert.AreEqual(0, potion.KillProgress);
        }

        [Test]
        public void TheTrigger_IsClampedToTwentyToSixtyPercent()
        {
            var potion = new AutoPotion();

            potion.TriggerFraction = 0.05f;
            Assert.AreEqual(0.2f, potion.TriggerFraction);
            potion.TriggerFraction = 0.9f;
            Assert.AreEqual(0.6f, potion.TriggerFraction);
        }

        [Test]
        public void ValuesFromASave_AreClampedIntoRange()
        {
            Assert.AreEqual(AutoPotion.MaxCharges, new AutoPotion(99, 0).Charges);
            Assert.AreEqual(0, new AutoPotion(-1, 0).Charges);
            Assert.AreEqual(AutoPotion.KillsPerCharge - 1, new AutoPotion(0, 99).KillProgress);
            Assert.AreEqual(0, new AutoPotion(AutoPotion.MaxCharges, 5).KillProgress, "no progress is kept while full");
        }

        [Test]
        public void PotionChanges_RaiseTheSessionsModified_SoTheyAreSaved()
        {
            var session = new GameSession();
            var modified = 0;
            session.Modified += () => modified++;

            session.Potion.Tick(Frame, 0.2f, true, out _);
            Assert.AreEqual(1, modified, "a fired potion");

            session.Potion.RegisterKill();
            Assert.AreEqual(2, modified, "kill progress");
        }
    }
}
