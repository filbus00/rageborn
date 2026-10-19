using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace ARPG.Tests
{
    /// <summary>The camera keeps the fight in sight (the owner, 2026-10-19: kiting hid the enemies behind her).</summary>
    public class CameraLeadTests
    {
        [Test]
        public void InAFight_TheViewLeansTowardTheEnemies_NotAwayWithHerRun()
        {
            var fromTheLeft = new List<Vector2> { new Vector2(-4f, 0f), new Vector2(-5f, 1f), new Vector2(-4f, -1f) };
            var lead = CameraRules.ThreatLead(fromTheLeft);
            Assert.Less(lead.x, -1.5f);
            Assert.LessOrEqual(lead.magnitude, CameraRules.MaxThreatLead + 1e-4f);
        }

        [Test]
        public void EnemiesAllRound_KeepTheViewOnHer_AndNoneMeansNoLean()
        {
            var round = new List<Vector2> { new Vector2(4f, 0f), new Vector2(-4f, 0f), new Vector2(0f, 4f), new Vector2(0f, -4f) };
            Assert.Less(CameraRules.ThreatLead(round).magnitude, 0.01f);
            Assert.AreEqual(Vector2.zero, CameraRules.ThreatLead(new List<Vector2>()));
        }
    }
}
