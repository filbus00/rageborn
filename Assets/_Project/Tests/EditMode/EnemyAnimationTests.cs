using NUnit.Framework;

namespace ARPG.Tests
{
    public class EnemyAnimationTests
    {
        [Test]
        public void TheWindupEndsOnTheStrike_AndTheRecoveryPlaysTheRest()
        {
            Assert.AreEqual(0f, EnemyAnimationRules.AttackProgress(false, 0f, 0.35f), 1e-5f);
            Assert.AreEqual(EnemyAnimationRules.HitFraction, EnemyAnimationRules.AttackProgress(false, 0.35f, 0.35f), 1e-5f,
                "the blow lands on the animation's strike");
            Assert.AreEqual(EnemyAnimationRules.HitFraction, EnemyAnimationRules.AttackProgress(true, 0f, 0.65f), 1e-5f);
            Assert.AreEqual(1f, EnemyAnimationRules.AttackProgress(true, 0.65f, 0.65f), 1e-5f);
            Assert.AreEqual(1f, EnemyAnimationRules.AttackProgress(true, 0.3f, 0f), 1e-5f, "no recovery time: at the end");
        }

        [Test]
        public void Frames_LoopAndStopAtTheEnd()
        {
            Assert.AreEqual(0, EnemyAnimationRules.LoopFrame(0f, 1f, 10));
            Assert.AreEqual(5, EnemyAnimationRules.LoopFrame(0.55f, 1f, 10));
            Assert.AreEqual(2, EnemyAnimationRules.LoopFrame(1.25f, 1f, 10), "wraps");
            Assert.AreEqual(0, EnemyAnimationRules.LoopFrame(3f, 0f, 10), "no length: first frame");
            Assert.AreEqual(9, EnemyAnimationRules.OneShotFrame(1f, 10), "the last frame is held");
            Assert.AreEqual(0, EnemyAnimationRules.OneShotFrame(-0.2f, 10));
        }
    }
}
