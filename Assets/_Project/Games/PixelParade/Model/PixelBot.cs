using System.Collections.Generic;

namespace PixelParade.Model
{
    public readonly struct BotResult
    {
        public readonly SimState State;
        public readonly int Ticks;
        public readonly int Launches;
        /// <summary>Most slots occupied at once: how close the bot came to overflowing.</summary>
        public readonly int PeakSlots;
        /// <summary>Launches that came back with ammo (wasted laps).</summary>
        public readonly int WastedLaps;

        public BotResult(SimState state, int ticks, int launches, int peakSlots, int wastedLaps)
        {
            State = state;
            Ticks = ticks;
            Launches = launches;
            PeakSlots = peakSlots;
            WastedLaps = wastedLaps;
        }

        public bool Won => State == SimState.Won;

        public override string ToString() =>
            $"{State} in {Ticks} ticks, {Launches} launches, {WastedLaps} wasted laps, peak slots {PeakSlots}";
    }

    /// <summary>
    /// A simple greedy player used by the tests to prove every level can be won, and to measure
    /// how much slack a level leaves. It launches the available shooter with the most exposed
    /// pixels of its colour (minus ammo of that colour already on the belt); when nothing is
    /// exposed and the belt is empty it digs with the lane whose next shooter would be most useful.
    /// </summary>
    public static class PixelBot
    {
        public static BotResult Play(PixelLevel level, int maxTicks = 20000)
        {
            var sim = new PixelSim(level);
            var events = new List<SimEvent>();
            int launches = 0, peak = 0, wasted = 0;

            while (sim.State == SimState.Playing && sim.TickCount < maxTicks)
            {
                while (sim.BeltLoad < sim.BeltCapacity)
                {
                    var choice = Choose(sim);
                    if (choice == null || sim.Launch(choice.Id, events) != LaunchResult.Ok) break;
                    launches++;
                }

                events.Clear();
                sim.Tick(events);
                foreach (var e in events)
                    if (e.Type == SimEventType.Slotted) wasted++;
                peak = System.Math.Max(peak, sim.SlotCount - sim.FreeSlots);
            }
            return new BotResult(sim.State, sim.TickCount, launches, peak, wasted);
        }

        private static Shooter Choose(PixelSim sim)
        {
            var candidates = new List<Shooter>();
            for (int lane = 0; lane < sim.LaneCount; lane++)
            {
                var front = sim.LaneFront(lane);
                if (front != null) candidates.Add(front);
            }
            for (int slot = 0; slot < sim.SlotCount; slot++)
            {
                var parked = sim.SlotAt(slot);
                if (parked != null) candidates.Add(parked);
            }
            if (candidates.Count == 0) return null;

            Shooter best = null;
            int bestScore = 0;
            foreach (var s in candidates)
            {
                int score = Useful(sim, s.Color);
                // Prefer emptying a slot on ties.
                if (score > bestScore || (score == bestScore && score > 0 && s.State == ShooterState.Slotted))
                {
                    best = s;
                    bestScore = score;
                }
            }
            if (best != null) return best;

            // Nothing useful: only dig when the belt is idle, through the lane with the best next shooter.
            if (sim.BeltLoad > 0) return null;
            Shooter dig = null;
            int digScore = -1;
            for (int lane = 0; lane < sim.LaneCount; lane++)
            {
                var queue = sim.Lane(lane);
                if (queue.Count == 0) continue;
                int score = queue.Count > 1 ? Useful(sim, queue[1].Color) : 0;
                if (score > digScore)
                {
                    dig = queue[0];
                    digScore = score;
                }
            }
            return dig ?? candidates[0];
        }

        /// <summary>Exposed pixels of a colour not already claimed by riders of that colour.</summary>
        private static int Useful(PixelSim sim, int color)
        {
            int claimed = 0;
            foreach (var r in sim.Riders)
                if (r.Color == color) claimed += r.Ammo;
            foreach (var p in sim.Pending)
                if (p.Color == color) claimed += p.Ammo;
            return sim.VisibleCount(color) - claimed;
        }
    }
}
