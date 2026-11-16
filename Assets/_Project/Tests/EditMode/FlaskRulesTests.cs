using NUnit.Framework;

namespace ARPG.Tests
{
    public class FlaskRulesTests
    {
        [Test]
        public void Arc_StartsAndLandsOnTheGround_HighestHalfway()
        {
            var peak = FlaskRules.Peak(6f);
            Assert.AreEqual(0f, FlaskRules.Height(0f, peak), 1e-5f);
            Assert.AreEqual(0f, FlaskRules.Height(1f, peak), 1e-5f);
            Assert.AreEqual(peak, FlaskRules.Height(0.5f, peak), 1e-5f);
            Assert.Greater(FlaskRules.Peak(8f), FlaskRules.Peak(2f), "a longer throw arcs higher");
        }

        [Test]
        public void Pictures_ByKind()
        {
            Assert.AreEqual("flask_molotov", FlaskRules.PictureOf(FlaskKind.Molotov));
            Assert.AreEqual("flask_poison", FlaskRules.PictureOf(FlaskKind.Poison));
            Assert.AreEqual("projectile_web", FlaskRules.PictureOf(FlaskKind.Web));
        }

        [Test]
        public void MortarShells_Lob_HigherThanBottles_AndTurnSlowly()
        {
            Assert.AreEqual("projectile_bomb", FlaskRules.PictureOf(FlaskKind.Mortar));
            Assert.Greater(FlaskRules.PeakOf(FlaskKind.Mortar, 6f), FlaskRules.PeakOf(FlaskKind.Molotov, 6f) * 2f);
            Assert.AreEqual(FlaskRules.Peak(6f), FlaskRules.PeakOf(FlaskKind.Poison, 6f), 1e-5f);
            Assert.Less(FlaskRules.SpinOf(FlaskKind.Mortar), FlaskRules.SpinOf(FlaskKind.Molotov));
        }

        [Test]
        public void EnemyShots_Fly30PercentFurther_ButBombsLandWhereSheStood()
        {
            Assert.AreEqual(13f, ProjectileKinds.Reach(ProjectileKind.Knife, 10f), 1e-4f);
            Assert.AreEqual(9.1f, ProjectileKinds.Reach(ProjectileKind.Returning, 7f), 1e-4f);
            Assert.AreEqual(5f, ProjectileKinds.Reach(ProjectileKind.Bomb, 5f), 1e-4f);
        }
    }
}
