using System;
using System.Collections.Generic;
using System.Text;

namespace PixelParade.Model
{
    /// <summary>A shooter as written in a level: colour and ammo.</summary>
    public readonly struct ShooterSpec
    {
        public readonly int Color;
        public readonly int Ammo;

        public ShooterSpec(int color, int ammo)
        {
            if (ammo <= 0) throw new ArgumentOutOfRangeException(nameof(ammo), "A shooter needs ammo.");
            Color = color;
            Ammo = ammo;
        }

        public override string ToString() => $"{PixelPalette.KeyOf(Color)}{Ammo}";
    }

    /// <summary>
    /// A hand-authored level: the picture as text and the shooter lanes, front first, e.g. "R10 D10 R8".
    /// The ammo of each colour must add up to the number of pixels of that colour.
    /// </summary>
    public sealed class PixelLevel
    {
        public const int DefaultSlots = 5;
        public const int DefaultBeltCapacity = 5;

        public PixelLevel(string name, string tip, string[] art, string[] lanes, int slots = DefaultSlots, int beltCapacity = DefaultBeltCapacity)
        {
            Name = name;
            Tip = tip;
            Art = art;
            Lanes = lanes;
            Slots = slots;
            BeltCapacity = beltCapacity;
        }

        public string Name { get; }
        public string Tip { get; }
        public IReadOnlyList<string> Art { get; }
        public IReadOnlyList<string> Lanes { get; }
        /// <summary>Waiting places for shooters that finish a lap with ammo left.</summary>
        public int Slots { get; }
        /// <summary>How many shooters may be on the belt (or waiting to board) at once.</summary>
        public int BeltCapacity { get; }

        public PixelPicture CreatePicture() => PixelPicture.Parse(Art);

        public List<List<ShooterSpec>> CreateLanes()
        {
            var lanes = new List<List<ShooterSpec>>(Lanes.Count);
            foreach (var lane in Lanes)
            {
                var specs = new List<ShooterSpec>();
                foreach (var token in lane.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (token.Length < 2 || !int.TryParse(token.Substring(1), out int ammo))
                        throw new FormatException($"Bad shooter '{token}' in lane \"{lane}\" (expected e.g. R10).");
                    specs.Add(new ShooterSpec(PixelPalette.IndexOf(token[0]), ammo));
                }
                lanes.Add(specs);
            }
            return lanes;
        }

        /// <summary>Null when the level is consistent, otherwise a readable list of problems.</summary>
        public string Validate()
        {
            var errors = new StringBuilder();
            try
            {
                var picture = CreatePicture();
                var lanes = CreateLanes();
                var ammo = new int[PixelPalette.Count];
                foreach (var lane in lanes)
                foreach (var spec in lane)
                    ammo[spec.Color] += spec.Ammo;

                for (int color = 0; color < PixelPalette.Count; color++)
                {
                    int pixels = picture.CountOf(color);
                    if (pixels != ammo[color])
                        errors.AppendLine($"{PixelPalette.KeyOf(color)}: {pixels} pixels but {ammo[color]} ammo");
                }
                if (Slots <= 0) errors.AppendLine("Slots must be positive.");
                if (BeltCapacity <= 0) errors.AppendLine("Belt capacity must be positive.");
            }
            catch (FormatException e)
            {
                errors.AppendLine(e.Message);
            }
            return errors.Length == 0 ? null : errors.ToString();
        }
    }
}
