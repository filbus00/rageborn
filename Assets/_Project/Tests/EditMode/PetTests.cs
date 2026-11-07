using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace ARPG.Tests
{
    /// <summary>The pet (Docs/02): the wolf alone since 2026-11-07, with every archer, levelling with her, with the
    /// player's rules, its skills boons and its training Lyriel's.</summary>
    public class PetTests
    {
        [Test]
        public void TheWolf_FollowsEveryArcher_WithNothingToBuy()
        {
            var session = new GameSession();
            Assert.AreEqual(PetKind.Wolf, session.Pets.Active);
            Assert.IsTrue(session.Pets.Owns(PetKind.Wolf));
            Assert.AreEqual(PetKind.Wolf, session.NextRun(4).Pets.Active);
        }

        [Test]
        public void ThePetsLevel_IsTheCharacters()
        {
            Assert.AreEqual(CombatFormulas.CharacterLife(10) * PetRules.Get(PetKind.Wolf).LifeFactor, PetRules.MaxLife(PetKind.Wolf, 10), 1e-3f);
            Assert.Greater(PetRules.MaxLife(PetKind.Wolf, 20), PetRules.MaxLife(PetKind.Wolf, 5));
            Assert.AreEqual(0.05f, PetRules.LifeBonus(PetKind.Wolf), 1e-5f);
            Assert.AreEqual(0f, PetRules.LifeBonus(null));
        }

        [Test]
        public void Targeting_FollowsTheRule_AndFallsBackToTheNearest()
        {
            var positions = new List<Vector2> { new Vector2(5f, 0f), new Vector2(1f, 0f), new Vector2(0f, 3f) };
            var fractions = new List<float> { 0.9f, 0.8f, 0.1f };
            var elites = new List<bool> { true, false, false };
            var pet = Vector2.zero;
            var player = new Vector2(0f, 4f);

            Assert.AreEqual(0, PetRules.PickTarget(PetTargeting.WhatIAttack, positions, fractions, elites, 0, pet, player));
            Assert.AreEqual(1, PetRules.PickTarget(PetTargeting.WhatIAttack, positions, fractions, elites, -1, pet, player), "no target of mine: the nearest");
            Assert.AreEqual(1, PetRules.PickTarget(PetTargeting.Nearest, positions, fractions, elites, -1, pet, player));
            Assert.AreEqual(0, PetRules.PickTarget(PetTargeting.ElitesFirst, positions, fractions, elites, -1, pet, player));
            Assert.AreEqual(2, PetRules.PickTarget(PetTargeting.Weakest, positions, fractions, elites, -1, pet, player));
            Assert.AreEqual(2, PetRules.PickTarget(PetTargeting.WhatAttacksMe, positions, fractions, elites, -1, pet, player));
            Assert.AreEqual(-1, PetRules.PickTarget(PetTargeting.Nearest, new List<Vector2>(), fractions, elites, -1, pet, player));
        }

        [Test]
        public void TheRules_SurviveASave()
        {
            var session = new GameSession();
            session.Pets.SetTargeting(PetTargeting.Weakest);
            session.Pets.Toggle(PetBehaviour.StayClose);

            var restored = SaveCodec.Restore(SaveCodec.Capture(session, 0), 1);
            Assert.AreEqual(PetKind.Wolf, restored.Pets.Active);
            Assert.AreEqual(PetTargeting.Weakest, restored.Pets.Targeting);
            Assert.IsTrue(restored.Pets.Has(PetBehaviour.StayClose));
        }

        [Test]
        public void OldRules_LoadWithoutTheFetching()
        {
            var state = new PetState();
            state.Restore(new[] { "Sideways", "FetchOnlyWhenClear", "GuardWhenLow" });
            Assert.AreEqual(PetState.DefaultTargeting, state.Targeting);
            Assert.AreEqual(PetBehaviour.GuardWhenLow, state.Behaviours);
            Assert.AreEqual(PetKind.Wolf, state.Active);
        }

        [Test]
        public void TheWolf_ShowsItsOwnSheets()
        {
            Assert.AreEqual("pet_wolf", PetRules.SpriteCharacter(PetKind.Wolf));
        }
    }
}
