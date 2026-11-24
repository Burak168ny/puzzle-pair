using NUnit.Framework;
using PixelParade.Model;

namespace PixelParade.Tests
{
    public sealed class PixelLevelTests
    {
        [Test]
        public void Campaign_HasFiveLevels()
        {
            Assert.AreEqual(5, PixelLevels.All.Count);
        }

        [Test]
        public void EveryLevel_AmmoMatchesPixels()
        {
            foreach (var level in PixelLevels.All)
                Assert.IsNull(level.Validate(), level.Name);
        }

        [Test]
        public void EveryLevel_IsWonByTheGreedyBot()
        {
            foreach (var level in PixelLevels.All)
            {
                var result = PixelBot.Play(level);
                Assert.IsTrue(result.Won, $"{level.Name}: {result}");
                Assert.Less(result.PeakSlots, level.Slots, $"{level.Name} leaves no slack: {result}");
            }
        }

        [Test]
        public void Pictures_GrowInPixelCount()
        {
            int previous = 0;
            foreach (var level in PixelLevels.All)
            {
                int count = level.CreatePicture().Count;
                Assert.Greater(count, previous, level.Name);
                previous = count;
            }
        }

        [Test]
        public void FirstLevel_HasATutorialTip()
        {
            Assert.IsFalse(string.IsNullOrEmpty(PixelLevels.All[0].Tip));
        }

        [Test]
        public void Validate_ReportsAmmoMismatch()
        {
            var level = new PixelLevel("bad", null, new[] { "RR" }, new[] { "R1" });
            StringAssert.Contains("R: 2 pixels but 1 ammo", level.Validate());
        }

        [Test]
        public void Validate_ReportsBadShooterTokens()
        {
            var level = new PixelLevel("bad", null, new[] { "RR" }, new[] { "Rx" });
            Assert.IsNotNull(level.Validate());
        }
    }
}
