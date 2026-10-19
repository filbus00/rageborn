using NUnit.Framework;
using UnityEngine;

namespace ARPG.Tests
{
    /// <summary>While she shoots, the view leads a little toward her target (the owner, 2026-10-19).</summary>
    public class CameraLeadTests
    {
        [Test]
        public void TheAimLead_PointsAtTheTarget_ShortAndCapped()
        {
            var lead = CameraRules.AimLead(new Vector2(-6f, 0f));
            Assert.Less(lead.x, 0f);
            Assert.AreEqual(CameraRules.AimLeadDistance, lead.magnitude, 1e-4f);
            Assert.AreEqual(0.5f, CameraRules.AimLead(new Vector2(0f, 1.5f)).magnitude, 1e-4f, "a third of a near target's distance");
        }
    }
}
