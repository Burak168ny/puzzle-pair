using System;
using System.Collections.Generic;

namespace PixelParade.Model
{
    public enum ShooterState
    {
        /// <summary>Waiting in a lane under the belt.</summary>
        Queued,
        /// <summary>Tapped, waiting for the entry of the belt to be clear.</summary>
        Pending,
        Riding,
        /// <summary>Finished a lap with ammo left; parked in a slot, can be launched again.</summary>
        Slotted,
        /// <summary>Out of ammo, gone.</summary>
        Spent,
    }

    public sealed class Shooter
    {
        internal Shooter(int id, int color, int ammo, int lane)
        {
            Id = id;
            Color = color;
            Ammo = ammo;
            StartAmmo = ammo;
            Lane = lane;
            Slot = -1;
        }

        public int Id { get; }
        public int Color { get; }
        public int StartAmmo { get; }
        public int Ammo { get; internal set; }
        public ShooterState State { get; internal set; }
        /// <summary>The lane it started in.</summary>
        public int Lane { get; }
        /// <summary>Current belt node while riding.</summary>
        public int Node { get; internal set; }
        /// <summary>Nodes travelled in the current lap.</summary>
        public int Steps { get; internal set; }
        /// <summary>Slot index while slotted, otherwise -1.</summary>
        public int Slot { get; internal set; }
    }

    public enum SimState
    {
        Playing,
        Won,
        Lost,
    }

    public enum LaunchResult
    {
        Ok,
        /// <summary>Not at the front of its lane and not in a slot, or the game is over.</summary>
        NotLaunchable,
        /// <summary>The belt already carries <see cref="PixelSim.BeltCapacity"/> shooters.</summary>
        BeltFull,
    }

    public enum SimEventType
    {
        /// <summary>A shooter left its lane or slot (Slot = the slot it left, -1 for a lane).</summary>
        Launched,
        /// <summary>A pending shooter stepped onto the belt at node 0.</summary>
        Admitted,
        /// <summary>A shooter destroyed pixel (X, Y).</summary>
        Shot,
        /// <summary>A shooter used its last ammo and disappeared.</summary>
        Spent,
        /// <summary>A shooter finished a lap with ammo left and parked in Slot.</summary>
        Slotted,
        /// <summary>A shooter finished a lap but every slot was taken: the level is lost.</summary>
        Overflow,
        Won,
        /// <summary>A booster added slot number Slot.</summary>
        SlotAdded,
    }

    public readonly struct SimEvent
    {
        public readonly SimEventType Type;
        public readonly int ShooterId;
        public readonly int X;
        public readonly int Y;
        public readonly int Slot;

        public SimEvent(SimEventType type, int shooterId, int x = -1, int y = -1, int slot = -1)
        {
            Type = type;
            ShooterId = shooterId;
            X = x;
            Y = y;
            Slot = slot;
        }

        public override string ToString() => $"{Type}(#{ShooterId}, {X},{Y}, slot {Slot})";
    }

    /// <summary>
    /// Deterministic fixed-step simulation of the belt game, engine-free.
    /// Each <see cref="Tick"/>:
    /// <list type="number">
    /// <item>every rider moves one node clockwise;</item>
    /// <item>riders back at the entry finish their lap: into the first free slot, or Overflow (lost);</item>
    /// <item>riders fire in boarding order at the first pixel their node can see; it must match their colour;</item>
    /// <item>one pending shooter boards if no rider is within <see cref="EntrySpacing"/> nodes of the entry.</item>
    /// </list>
    /// The view interpolates between ticks; the rules never depend on frame rate.
    /// </summary>
    public sealed class PixelSim
    {
        public const int DefaultEntrySpacing = 2;

        private readonly List<Shooter> _shooters = new List<Shooter>();
        private readonly List<List<Shooter>> _lanes = new List<List<Shooter>>();
        private readonly List<Shooter> _riders = new List<Shooter>();
        private readonly List<Shooter> _pending = new List<Shooter>();
        private Shooter[] _slots;

        public PixelSim(PixelLevel level)
            : this(level.CreatePicture(), level.CreateLanes(), level.Slots, level.BeltCapacity)
        {
        }

        public PixelSim(PixelPicture picture, IReadOnlyList<List<ShooterSpec>> lanes, int slots, int beltCapacity,
            int entrySpacing = DefaultEntrySpacing)
        {
            Picture = picture ?? throw new ArgumentNullException(nameof(picture));
            Belt = new BeltPath(picture.Width, picture.Height);
            if (slots <= 0) throw new ArgumentOutOfRangeException(nameof(slots));
            if (beltCapacity <= 0) throw new ArgumentOutOfRangeException(nameof(beltCapacity));
            _slots = new Shooter[slots];
            BeltCapacity = beltCapacity;
            EntrySpacing = Math.Max(1, entrySpacing);

            for (int lane = 0; lane < lanes.Count; lane++)
            {
                var queue = new List<Shooter>();
                foreach (var spec in lanes[lane])
                {
                    var shooter = new Shooter(_shooters.Count, spec.Color, spec.Ammo, lane);
                    _shooters.Add(shooter);
                    queue.Add(shooter);
                }
                _lanes.Add(queue);
            }

            TotalPixels = picture.Count;
            State = picture.Count == 0 ? SimState.Won : SimState.Playing;
        }

        public PixelPicture Picture { get; }
        public BeltPath Belt { get; }
        public int BeltCapacity { get; }
        public int EntrySpacing { get; }
        public SimState State { get; private set; }
        public int TickCount { get; private set; }
        public int TotalPixels { get; }
        public int Remaining => Picture.Count;

        public IReadOnlyList<Shooter> Shooters => _shooters;
        /// <summary>Riders in boarding order (the order they fire in).</summary>
        public IReadOnlyList<Shooter> Riders => _riders;
        public IReadOnlyList<Shooter> Pending => _pending;
        public int LaneCount => _lanes.Count;
        public IReadOnlyList<Shooter> Lane(int lane) => _lanes[lane];
        public int SlotCount => _slots.Length;
        public Shooter SlotAt(int slot) => _slots[slot];

        public int BeltLoad => _riders.Count + _pending.Count;

        public int FreeSlots
        {
            get
            {
                int free = 0;
                foreach (var s in _slots)
                    if (s == null) free++;
                return free;
            }
        }

        public Shooter LaneFront(int lane) => _lanes[lane].Count > 0 ? _lanes[lane][0] : null;

        public bool IsLaunchable(Shooter shooter)
        {
            if (State != SimState.Playing || shooter == null) return false;
            if (shooter.State == ShooterState.Slotted) return true;
            return shooter.State == ShooterState.Queued && LaneFront(shooter.Lane) == shooter;
        }

        public LaunchResult Launch(int shooterId, List<SimEvent> events)
        {
            var shooter = _shooters[shooterId];
            if (!IsLaunchable(shooter)) return LaunchResult.NotLaunchable;
            if (BeltLoad >= BeltCapacity) return LaunchResult.BeltFull;

            int fromSlot = -1;
            if (shooter.State == ShooterState.Slotted)
            {
                fromSlot = shooter.Slot;
                _slots[fromSlot] = null;
                shooter.Slot = -1;
            }
            else
            {
                _lanes[shooter.Lane].RemoveAt(0);
            }

            shooter.State = ShooterState.Pending;
            _pending.Add(shooter);
            events?.Add(new SimEvent(SimEventType.Launched, shooter.Id, slot: fromSlot));
            return LaunchResult.Ok;
        }

        /// <summary>Booster: one more waiting slot.</summary>
        public void AddSlot(List<SimEvent> events)
        {
            Array.Resize(ref _slots, _slots.Length + 1);
            events?.Add(new SimEvent(SimEventType.SlotAdded, -1, slot: _slots.Length - 1));
        }

        public void Tick(List<SimEvent> events)
        {
            if (State != SimState.Playing) return;
            TickCount++;

            // 1. Move.
            foreach (var rider in _riders)
            {
                rider.Steps++;
                rider.Node = rider.Steps % Belt.Count;
            }

            // 2. Finish laps.
            for (int i = 0; i < _riders.Count; i++)
            {
                var rider = _riders[i];
                if (rider.Steps < Belt.Count) continue;

                int slot = Array.IndexOf(_slots, null);
                if (slot < 0)
                {
                    State = SimState.Lost;
                    events?.Add(new SimEvent(SimEventType.Overflow, rider.Id));
                    return;
                }

                _riders.RemoveAt(i--);
                rider.State = ShooterState.Slotted;
                rider.Slot = slot;
                rider.Steps = 0;
                _slots[slot] = rider;
                events?.Add(new SimEvent(SimEventType.Slotted, rider.Id, slot: slot));
            }

            // 3. Fire, in boarding order.
            for (int i = 0; i < _riders.Count; i++)
            {
                var rider = _riders[i];
                int target = FirstVisible(Belt[rider.Node]);
                if (target < 0 || Picture.GetAt(target) != rider.Color) continue;

                Picture.ClearAt(target);
                rider.Ammo--;
                events?.Add(new SimEvent(SimEventType.Shot, rider.Id, Picture.XOf(target), Picture.YOf(target)));

                if (rider.Ammo == 0)
                {
                    rider.State = ShooterState.Spent;
                    _riders.RemoveAt(i--);
                    events?.Add(new SimEvent(SimEventType.Spent, rider.Id));
                }

                if (Picture.Count == 0)
                {
                    State = SimState.Won;
                    events?.Add(new SimEvent(SimEventType.Won, -1));
                    return;
                }
            }

            // 4. Board one pending shooter.
            if (_pending.Count > 0 && EntryClear())
            {
                var next = _pending[0];
                _pending.RemoveAt(0);
                next.State = ShooterState.Riding;
                next.Node = 0;
                next.Steps = 0;
                _riders.Add(next);
                events?.Add(new SimEvent(SimEventType.Admitted, next.Id));
            }
        }

        /// <summary>Index of the first pixel a node can see along its row/column, or -1.</summary>
        public int FirstVisible(BeltNode node)
        {
            int w = Picture.Width, h = Picture.Height;
            switch (node.Side)
            {
                case BeltSide.Left:
                    for (int x = 0; x < w; x++)
                        if (Picture.Get(x, node.Line) >= 0) return Picture.Index(x, node.Line);
                    return -1;
                case BeltSide.Right:
                    for (int x = w - 1; x >= 0; x--)
                        if (Picture.Get(x, node.Line) >= 0) return Picture.Index(x, node.Line);
                    return -1;
                case BeltSide.Top:
                    for (int y = h - 1; y >= 0; y--)
                        if (Picture.Get(node.Line, y) >= 0) return Picture.Index(node.Line, y);
                    return -1;
                case BeltSide.Bottom:
                    for (int y = 0; y < h; y++)
                        if (Picture.Get(node.Line, y) >= 0) return Picture.Index(node.Line, y);
                    return -1;
                default:
                    return -1;
            }
        }

        /// <summary>How many distinct pixels of <paramref name="color"/> are exposed to the belt right now.</summary>
        public int VisibleCount(int color)
        {
            var seen = new HashSet<int>();
            for (int i = 0; i < Belt.Count; i++)
            {
                int target = FirstVisible(Belt[i]);
                if (target >= 0 && Picture.GetAt(target) == color) seen.Add(target);
            }
            return seen.Count;
        }

        private bool EntryClear()
        {
            foreach (var rider in _riders)
                if (rider.Steps < EntrySpacing) return false;
            return true;
        }
    }
}
