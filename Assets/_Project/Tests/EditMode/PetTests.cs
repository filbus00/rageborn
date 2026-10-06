using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace ARPG.Tests
{
    /// <summary>Pets (Docs/02, decided 2026-09-30): bought at the Pet Vendor, one active, levelling with the character,
    /// with the player's rules.</summary>
    public class PetTests
    {
        [Test]
        public void Buying_TakesTheGold_AndTheNewPetFollows()
        {
            var session = new GameSession();
            Assert.IsFalse(session.BuyPet(PetKind.Wolf), "no gold");
            session.AddGold(600);
            Assert.IsTrue(session.BuyPet(PetKind.Wolf));
            Assert.AreEqual(100, session.Gold);
            Assert.AreEqual(PetKind.Wolf, session.Pets.Active);
            Assert.IsFalse(session.BuyPet(PetKind.Wolf), "owned already");
            Assert.AreEqual(100, session.Gold);
        }

        [Test]
        public void Switching_IsFree_ButOnlyToAnOwnedPet()
        {
            var session = new GameSession();
            session.AddGold(2000);
            session.BuyPet(PetKind.Wolf);
            session.BuyPet(PetKind.Raven);
            Assert.AreEqual(PetKind.Raven, session.Pets.Active);
            Assert.IsTrue(session.Pets.SetActive(PetKind.Wolf));
            Assert.AreEqual(0, session.Gold);
            Assert.IsFalse(session.Pets.SetActive(PetKind.Boar));
            Assert.AreEqual(PetKind.Wolf, session.Pets.Active);
        }

        [Test]
        public void ThePetsLevel_IsTheCharacters()
        {
            Assert.AreEqual(CombatFormulas.CharacterLife(10) * PetRules.Get(PetKind.Boar).LifeFactor, PetRules.MaxLife(PetKind.Boar, 10), 1e-3f);
            Assert.Greater(PetRules.MaxLife(PetKind.Wolf, 20), PetRules.MaxLife(PetKind.Wolf, 5));
        }

        [Test]
        public void EachKind_GivesItsBonus_OnlyWhileActive()
        {
            Assert.AreEqual(0.05f, PetRules.LifeBonus(PetKind.Wolf), 1e-5f);
            Assert.AreEqual(0.1f, PetRules.MagicFindBonus(PetKind.Raven), 1e-5f);
            Assert.AreEqual(0.1f, PetRules.ArmorBonus(PetKind.Boar), 1e-5f);
            Assert.AreEqual(0f, PetRules.LifeBonus(null));
            Assert.AreEqual(0f, PetRules.MagicFindBonus(PetKind.Wolf));
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
        public void PetsAndTheirRules_SurviveASave()
        {
            var session = new GameSession();
            session.AddGold(5000);
            session.BuyPet(PetKind.Boar);
            session.BuyPet(PetKind.Wolf);
            session.Pets.SetActive(PetKind.Boar);
            session.Pets.SetTargeting(PetTargeting.Weakest);
            session.Pets.Toggle(PetBehaviour.StayClose);
            session.Pets.Toggle(PetBehaviour.FetchOnlyWhenClear);

            var restored = SaveCodec.Restore(SaveCodec.Capture(session, 0), 1);
            Assert.IsTrue(restored.Pets.Owns(PetKind.Boar) && restored.Pets.Owns(PetKind.Wolf));
            Assert.IsFalse(restored.Pets.Owns(PetKind.Raven));
            Assert.AreEqual(PetKind.Boar, restored.Pets.Active);
            Assert.AreEqual(PetTargeting.Weakest, restored.Pets.Targeting);
            Assert.IsTrue(restored.Pets.Has(PetBehaviour.StayClose));
            Assert.IsFalse(restored.Pets.Has(PetBehaviour.FetchOnlyWhenClear));
        }

        [Test]
        public void UnknownPetNames_AreLeftOut()
        {
            var state = new PetState();
            var warnings = new List<string>();
            state.Restore(new[] { "Wolf", "Dragon" }, "Dragon", new[] { "Sideways" }, warnings);
            Assert.IsTrue(state.Owns(PetKind.Wolf));
            Assert.IsNull(state.Active);
            Assert.AreEqual(PetState.DefaultTargeting, state.Targeting);
            Assert.AreEqual(1, warnings.Count);
        }

        [Test]
        public void Pets_ShowTheirOwnSheets_AndOnlyTheRavenFlies()
        {
            Assert.AreEqual("pet_wolf", PetRules.SpriteCharacter(PetKind.Wolf));
            Assert.AreEqual("pet_raven", PetRules.SpriteCharacter(PetKind.Raven));
            Assert.AreEqual(0f, PetRules.Hover(PetKind.Boar, 1.3f, 0f));
            var flying = PetRules.Hover(PetKind.Raven, 0f, 0f);
            Assert.AreEqual(PetRules.HoverHeight(PetKind.Raven), flying, 1e-5f);
            Assert.AreEqual(flying / 2f, PetRules.Hover(PetKind.Raven, 0f, 0.5f), 1e-5f, "halfway down while it falls");
            Assert.AreEqual(0f, PetRules.Hover(PetKind.Raven, 0.7f, 1f), "knocked out on the ground");
        }
    }
}
