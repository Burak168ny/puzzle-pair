using System;
using System.Collections.Generic;
using NUnit.Framework;
using PixelParade.Model;

namespace PixelParade.Tests
{
    public sealed class PixelModelTests
    {
        private static int C(char key) => PixelPalette.IndexOf(key);

        private static PixelSim Sim(string[] art, string[] lanes, int slots = 5, int capacity = 5)
            => new PixelSim(new PixelLevel("test", null, art, lanes, slots, capacity));

        private static void Run(PixelSim sim, int ticks, List<SimEvent> events = null)
        {
            for (int i = 0; i < ticks && sim.State == SimState.Playing; i++) sim.Tick(events);
        }

        [Test]
        public void Picture_ParsesRowsTopToBottom_WithYUp()
        {
            var picture = PixelPicture.Parse(new[] { "R.", "GB" });

            Assert.AreEqual(2, picture.Width);
            Assert.AreEqual(2, picture.Height);
            Assert.AreEqual(3, picture.Count);
            Assert.AreEqual(C('R'), picture.Get(0, 1));
            Assert.AreEqual(-1, picture.Get(1, 1));
            Assert.AreEqual(C('G'), picture.Get(0, 0));
            Assert.AreEqual(1, picture.CountOf(C('B')));
        }

        [Test]
        public void Picture_RejectsUnknownKeysAndRaggedRows()
        {
            Assert.Throws<FormatException>(() => PixelPicture.Parse(new[] { "R?" }));
            Assert.Throws<FormatException>(() => PixelPicture.Parse(new[] { "RR", "R" }));
        }

        [Test]
        public void Belt_HasOneNodePerRowAndColumnPlusCorners_CounterClockwise()
        {
            var belt = new BeltPath(3, 2);

            Assert.AreEqual(2 * 3 + 2 * 2 + 4, belt.Count);
            Assert.AreEqual(BeltSide.Corner, belt[0].Side);
            Assert.AreEqual(BeltSide.Bottom, belt[1].Side);
            Assert.AreEqual(0, belt[1].Line);
            Assert.AreEqual(2, belt[3].Line);
            Assert.AreEqual(BeltSide.Corner, belt[4].Side);
            Assert.AreEqual(BeltSide.Right, belt[5].Side);
            Assert.AreEqual(0, belt[5].Line);
            Assert.AreEqual(BeltSide.Top, belt[8].Side);
            Assert.AreEqual(2, belt[8].Line);
            Assert.AreEqual(BeltSide.Left, belt[12].Side);
            Assert.AreEqual(1, belt[12].Line);
            Assert.AreEqual(0, belt[belt.Count - 1].Line);
        }

        [Test]
        public void FirstVisible_LooksInwardFromEachSide()
        {
            var sim = Sim(new[]
            {
                "RG.",
                ".BY",
            }, new[] { "R1 G1 B1 Y1" });
            var p = sim.Picture;

            Assert.AreEqual(p.Index(0, 1), sim.FirstVisible(new BeltNode(-1, 1, BeltSide.Left, 1)));
            Assert.AreEqual(p.Index(1, 0), sim.FirstVisible(new BeltNode(-1, 0, BeltSide.Left, 0)));
            Assert.AreEqual(p.Index(2, 0), sim.FirstVisible(new BeltNode(3, 0, BeltSide.Right, 0)));
            Assert.AreEqual(p.Index(1, 1), sim.FirstVisible(new BeltNode(1, 2, BeltSide.Top, 1)));
            Assert.AreEqual(p.Index(1, 0), sim.FirstVisible(new BeltNode(1, -1, BeltSide.Bottom, 1)));
            Assert.AreEqual(-1, sim.FirstVisible(new BeltNode(-1, -1, BeltSide.Corner, -1)));
        }

        [Test]
        public void Launch_OnlyTheFrontOfALane()
        {
            var sim = Sim(new[] { "RG" }, new[] { "R1 G1" });
            var back = sim.Lane(0)[1];

            Assert.AreEqual(LaunchResult.NotLaunchable, sim.Launch(back.Id, null));
            Assert.AreEqual(LaunchResult.Ok, sim.Launch(sim.LaneFront(0).Id, null));
            Assert.AreEqual(back, sim.LaneFront(0));
        }

        [Test]
        public void Launch_RespectsBeltCapacity()
        {
            var sim = Sim(new[] { "RG" }, new[] { "R1", "G1" }, capacity: 1);

            Assert.AreEqual(LaunchResult.Ok, sim.Launch(sim.LaneFront(0).Id, null));
            Assert.AreEqual(LaunchResult.BeltFull, sim.Launch(sim.LaneFront(1).Id, null));
            Assert.AreEqual(1, sim.BeltLoad);
        }

        [Test]
        public void Shooter_BoardsAtTheEntry_AndFiresOnlyAtItsColour()
        {
            // One row "RG": the bottom node under column 0 sees R, the one under column 1 sees G.
            var sim = Sim(new[] { "RG" }, new[] { "G1 R1" });
            var events = new List<SimEvent>();
            var green = sim.LaneFront(0);
            sim.Launch(green.Id, events);

            sim.Tick(events); // boards at node 0 (corner)
            Assert.AreEqual(ShooterState.Riding, green.State);
            Assert.AreEqual(0, green.Node);

            sim.Tick(events); // node 1 = bottom of column 0, sees R: no shot
            Assert.AreEqual(1, green.Node);
            Assert.AreEqual(1, green.Ammo);
            Assert.AreEqual(2, sim.Remaining);

            // Node 2 looks up column 1 and sees G.
            Run(sim, 1, events);
            Assert.AreEqual(0, green.Ammo);
            Assert.AreEqual(ShooterState.Spent, green.State);
            Assert.AreEqual(-1, sim.Picture.Get(1, 0));
            Assert.IsTrue(events.Exists(e => e.Type == SimEventType.Shot && e.X == 1 && e.Y == 0));
            Assert.IsTrue(events.Exists(e => e.Type == SimEventType.Spent && e.ShooterId == green.Id));
        }

        [Test]
        public void Shooter_WithAmmoLeftAfterALap_ParksInASlot_AndCanRelaunch()
        {
            // Y is buried under R everywhere... except there is no way to see it: R surrounds it.
            var sim = Sim(new[] { "RRR", "RYR", "RRR" }, new[] { "Y1 R8" });
            var events = new List<SimEvent>();
            var yellow = sim.LaneFront(0);
            sim.Launch(yellow.Id, events);
            Run(sim, 1 + sim.Belt.Count, events);

            Assert.AreEqual(ShooterState.Slotted, yellow.State);
            Assert.AreEqual(0, yellow.Slot);
            Assert.AreEqual(sim.SlotCount - 1, sim.FreeSlots);
            Assert.IsTrue(sim.IsLaunchable(yellow));
            Assert.AreEqual(LaunchResult.Ok, sim.Launch(yellow.Id, events));
            Assert.AreEqual(sim.SlotCount, sim.FreeSlots);
        }

        // Two yellow pixels hidden inside a red frame: yellow shooters can never see them at first.
        private static readonly string[] HiddenYellow = { "RRRR", "RYYR", "RRRR" };

        [Test]
        public void Overflow_WhenALapEndsWithEverySlotTaken_LosesTheLevel()
        {
            var sim = Sim(HiddenYellow, new[] { "Y1", "Y1", "R10" }, slots: 1);
            var events = new List<SimEvent>();
            sim.Launch(sim.LaneFront(0).Id, events);
            sim.Launch(sim.LaneFront(1).Id, events);
            Run(sim, 200, events);

            Assert.AreEqual(SimState.Lost, sim.State);
            Assert.IsTrue(events.Exists(e => e.Type == SimEventType.Slotted));
            Assert.IsTrue(events.Exists(e => e.Type == SimEventType.Overflow));
        }

        [Test]
        public void AddSlot_MakesRoomForOneMore()
        {
            var sim = Sim(HiddenYellow, new[] { "Y1", "Y1", "R10" }, slots: 1);
            var events = new List<SimEvent>();
            sim.AddSlot(events);
            sim.Launch(sim.LaneFront(0).Id, events);
            sim.Launch(sim.LaneFront(1).Id, events);
            Run(sim, 200, events);

            Assert.AreEqual(SimState.Playing, sim.State);
            Assert.AreEqual(2, sim.SlotCount);
            Assert.AreEqual(0, sim.FreeSlots);
            Assert.IsTrue(events.Exists(e => e.Type == SimEventType.SlotAdded && e.Slot == 1));

            // Now the red shooter opens the frame and both yellows finish the job from their slots.
            sim.Launch(sim.LaneFront(2).Id, events);
            Run(sim, 200, events);
            sim.Launch(sim.SlotAt(0).Id, events);
            sim.Launch(sim.SlotAt(1).Id, events);
            Run(sim, 200, events);
            Assert.AreEqual(SimState.Won, sim.State);
        }

        [Test]
        public void EntrySpacing_KeepsRidersApart()
        {
            // G hides at the top right, so the green shooter rides on for a while without shooting.
            var sim = Sim(new[] { "RG", "RR" }, new[] { "G1", "R3" });
            var events = new List<SimEvent>();
            var first = sim.LaneFront(0);
            var second = sim.LaneFront(1);
            sim.Launch(first.Id, events);
            sim.Launch(second.Id, events);

            sim.Tick(events);
            Assert.AreEqual(ShooterState.Riding, first.State);
            Assert.AreEqual(ShooterState.Pending, second.State);

            sim.Tick(events); // first is at node 1, still inside the spacing
            Assert.AreEqual(ShooterState.Pending, second.State);

            sim.Tick(events);
            Assert.AreEqual(ShooterState.Riding, second.State);
            Assert.AreEqual(PixelSim.DefaultEntrySpacing, first.Steps);
        }

        [Test]
        public void ClearingThePicture_Wins()
        {
            var sim = Sim(new[] { "RG" }, new[] { "R1", "G1" });
            var events = new List<SimEvent>();
            sim.Launch(sim.LaneFront(0).Id, events);
            sim.Launch(sim.LaneFront(1).Id, events);
            Run(sim, 100, events);

            Assert.AreEqual(SimState.Won, sim.State);
            Assert.AreEqual(0, sim.Remaining);
            Assert.IsTrue(events.Exists(e => e.Type == SimEventType.Won));
        }

        [Test]
        public void SameInputs_GiveTheSameEvents()
        {
            string Play()
            {
                var level = PixelLevels.All[2];
                var sim = new PixelSim(level);
                var events = new List<SimEvent>();
                var log = new System.Text.StringBuilder();
                for (int t = 0; t < 400 && sim.State == SimState.Playing; t++)
                {
                    if (t % 7 == 0)
                        for (int lane = 0; lane < sim.LaneCount; lane++)
                            if (sim.LaneFront(lane) != null) sim.Launch(sim.LaneFront(lane).Id, events);
                    sim.Tick(events);
                }
                foreach (var e in events) log.Append(e).Append(';');
                return log.ToString();
            }

            Assert.AreEqual(Play(), Play());
        }
    }
}
