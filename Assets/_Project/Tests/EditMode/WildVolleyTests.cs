using System;
using NUnit.Framework;
using UnityEngine;

namespace ARPG.Tests
{
    public class WildVolleyTests
    {
        const float Points = 1f;

        [Test]
        public void TwoQuickTaps_AreADoubleTap_OnTheSecondLift()
        {
            var detector = new DoubleTapDetector();
            detector.Began(0f, new Vector2(300f, 300f));
            Assert.IsFalse(detector.Ended(0.1f, new Vector2(302f, 300f), Points), "one tap is not two");
            detector.Began(0.3f, new Vector2(310f, 305f));
            Assert.IsTrue(detector.Ended(0.38f, new Vector2(310f, 305f), Points));
        }

        [Test]
        public void ASlowTouch_OrAMovedOne_IsNoTap()
        {
            var detector = new DoubleTapDetector();
            detector.Began(0f, Vector2.zero);
            Assert.IsFalse(detector.Ended(0.5f, Vector2.zero, Points), "held too long");
            detector.Began(0.6f, Vector2.zero);
            Assert.IsFalse(detector.Ended(0.68f, Vector2.zero, Points), "the first was no tap, so this is the first");
            detector.Began(0.8f, Vector2.zero);
            Assert.IsFalse(detector.Ended(0.9f, new Vector2(100f, 0f), Points), "dragged away");
        }

        [Test]
        public void TapsTooFarApartInTime_OrSpace_AreNotADouble()
        {
            var detector = new DoubleTapDetector();
            detector.Began(0f, Vector2.zero);
            detector.Ended(0.1f, Vector2.zero, Points);
            detector.Began(0.9f, Vector2.zero);
            Assert.IsFalse(detector.Ended(0.95f, Vector2.zero, Points), "a second late");
            detector.Began(1.1f, new Vector2(500f, 0f));
            Assert.IsFalse(detector.Ended(1.15f, new Vector2(500f, 0f), Points), "far away");
        }

        [Test]
        public void ADoubleTap_ResetsItself_SoAThirdTapStartsOver()
        {
            var detector = new DoubleTapDetector();
            detector.Began(0f, Vector2.zero);
            detector.Ended(0.05f, Vector2.zero, Points);
            detector.Began(0.15f, Vector2.zero);
            Assert.IsTrue(detector.Ended(0.2f, Vector2.zero, Points));
            detector.Began(0.3f, Vector2.zero);
            Assert.IsFalse(detector.Ended(0.35f, Vector2.zero, Points), "the third is a first tap");
        }

        [Test]
        public void TheVolley_IsLearnedFromItsBook_AndEvolvedWithAContractsLastBoss()
        {
            var company = new CompanyRecord();
            Assert.AreEqual(WildVolleyRules.NotLearned, WildVolleyRules.TierOf(company));
            company.BossFell(6);
            Assert.AreEqual(WildVolleyRules.NotLearned, WildVolleyRules.TierOf(company), "the boss drops the book; killing it teaches nothing");
            Assert.AreEqual(TapSkillRules.WildVolley, TapSkillRules.BookFor(company), "the book it drops");
            Assert.IsTrue(company.LearnTapSkill(TapSkillRules.WildVolley));
            Assert.AreEqual(WildVolleyRules.Learned, WildVolleyRules.TierOf(company));
            Assert.AreEqual(TapSkillRules.WildVolley, company.ChosenTapSkill, "the first skill learned is chosen");
            Assert.IsNull(TapSkillRules.BookFor(company), "no more books once every skill is known");
            Assert.IsFalse(company.LearnTapSkill(TapSkillRules.WildVolley), "a second book teaches nothing");
            company.BossFell(12);
            Assert.AreEqual(WildVolleyRules.Evolved, WildVolleyRules.TierOf(company), "the Brood Mother, the Thornwood's last");
        }

        [Test]
        public void TheEvolution_IsShownOnlyWhenALearnedVolleyEvolves()
        {
            Assert.IsTrue(WildVolleyRules.Evolves(WildVolleyRules.Learned, WildVolleyRules.Evolved));
            Assert.IsFalse(WildVolleyRules.Evolves(WildVolleyRules.NotLearned, WildVolleyRules.NotLearned), "a final boss before the book");
            Assert.IsFalse(WildVolleyRules.Evolves(WildVolleyRules.Evolved, WildVolleyRules.Evolved), "a second contract's last boss");
            Assert.IsFalse(WildVolleyRules.Evolves(WildVolleyRules.Learned, WildVolleyRules.Learned), "an ordinary boss");
        }

        [Test]
        public void OnlyALearnedSkill_CanBeChosen()
        {
            var company = new CompanyRecord();
            Assert.IsFalse(company.ChooseTapSkill(TapSkillRules.WildVolley));
            Assert.IsFalse(company.LearnTapSkill("no_such_skill"));
            company.LearnTapSkill(TapSkillRules.WildVolley);
            Assert.IsTrue(company.ChooseTapSkill(TapSkillRules.WildVolley));
        }

        [Test]
        public void TheEvolvedVolley_LoosesMoreAndHitsHarder_AndTheCooldownGatesIt()
        {
            Assert.Greater(WildVolleyRules.Arrows(WildVolleyRules.Evolved), WildVolleyRules.Arrows(WildVolleyRules.Learned));
            Assert.Greater(WildVolleyRules.ArrowShare(WildVolleyRules.Evolved), WildVolleyRules.ArrowShare(WildVolleyRules.Learned));
            Assert.IsFalse(WildVolleyRules.Ready(WildVolleyRules.NotLearned, 0f));
            Assert.IsFalse(WildVolleyRules.Ready(WildVolleyRules.Learned, 1f));
            Assert.IsTrue(WildVolleyRules.Ready(WildVolleyRules.Learned, 0f));
        }

        [Test]
        public void WhatTheCompanyHasLearned_SurvivesASave_AndAFormat22SaveStartsUnlearned()
        {
            var session = new GameSession(1);
            session.Company.LearnTapSkill(TapSkillRules.WildVolley);
            var data = SaveCodec.Capture(session, 1);
            Assert.IsTrue(SaveCodec.TryParse(SaveCodec.ToJson(data), out var parsed, out var error), error);
            var restored = SaveCodec.Restore(parsed, 2);
            Assert.AreEqual(WildVolleyRules.Learned, WildVolleyRules.TierOf(restored.Company));
            Assert.AreEqual(TapSkillRules.WildVolley, restored.Company.ChosenTapSkill);

            var old = "{\"version\":22,\"savedAtUnixMs\":1,\"gold\":10,\"lifeFraction\":1.0,\"level\":1,\"equipped\":[],\"backpack\":[]}";
            Assert.IsTrue(SaveCodec.TryParse(old, out var oldData, out error), error);
            var fromOld = SaveCodec.Restore(oldData, 3);
            Assert.AreEqual(WildVolleyRules.NotLearned, WildVolleyRules.TierOf(fromOld.Company));

            // A format 23 save from a company that had felled a boss knew the volley then, and keeps it.
            var learned = "{\"version\":23,\"savedAtUnixMs\":1,\"gold\":10,\"lifeFraction\":1.0,\"level\":1,\"equipped\":[],\"backpack\":[],\"bossesFelled\":1}";
            Assert.IsTrue(SaveCodec.TryParse(learned, out var learnedData, out error), error);
            var kept = SaveCodec.Restore(learnedData, 4);
            Assert.AreEqual(WildVolleyRules.Learned, WildVolleyRules.TierOf(kept.Company));
            Assert.AreEqual(TapSkillRules.WildVolley, kept.Company.ChosenTapSkill);
        }
    }
}
