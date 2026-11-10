using NUnit.Framework;

namespace ARPG.Tests
{
    public class LevelNameTests
    {
        [Test]
        public void Levels_AreNamedByContractAndPlaceInIt()
        {
            Assert.AreEqual("Level 1-1", ContractRules.LevelName(1));
            Assert.AreEqual("Level 1-6", ContractRules.LevelName(6));
            Assert.AreEqual("Level 1-12", ContractRules.LevelName(12));
            Assert.AreEqual("Level 2-1", ContractRules.LevelName(13));
            Assert.AreEqual("4-12", ContractRules.LevelCode(48));
        }
    }
}
