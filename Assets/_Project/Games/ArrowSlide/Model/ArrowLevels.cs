using System.Collections.Generic;

namespace ArrowSlide.Model
{
    /// <summary>A hand-authored level: the board as text plus presentation data.</summary>
    public sealed class ArrowLevel
    {
        public ArrowLevel(string name, string tip, params string[] rows)
        {
            Name = name;
            Tip = tip;
            Rows = rows;
        }

        public string Name { get; }
        /// <summary>One-line hint shown at the start of the level (tutorial), or null.</summary>
        public string Tip { get; }
        /// <summary>Board rows from top to bottom, see <see cref="ArrowLevelParser"/>.</summary>
        public IReadOnlyList<string> Rows { get; }

        public ArrowBoard CreateBoard() => ArrowLevelParser.Parse(Rows);
    }

    /// <summary>
    /// The campaign, easy to hard. Boards 1-2 were drawn by hand; the dense boards 3-5 were drafted
    /// with an offline helper (reverse placement, solvable by construction) and picked by hand.
    /// Every board is checked by the EditMode tests (parses, solvable, gets harder).
    /// </summary>
    public static class ArrowLevels
    {
        public static readonly IReadOnlyList<ArrowLevel> All = new[]
        {
            new ArrowLevel("First Steps", "Tap an arrow to slide it off the board",
                "...U",
                ">>R^",
                "...^",
                "L<<."),

            new ArrowLevel("Columns", "Blocked arrows bounce back and cost a heart",
                "L<<>R",
                "UvL<<",
                "^vUUv",
                "^v^^v",
                "UD^^D",
                "^>>>R"),

            new ArrowLevel("Thicket", null,
                "L<L<L<<<",
                "UL<^L<<<",
                "^L<Uv<U^",
                "UU^^<^^^",
                "^^<L<<^^",
                "Uv^<>^vU",
                "^vUv<<D^",
                "^v^>>>vv",
                "vD^<<vDv",
                "D>>>^D.D"),

            new ArrowLevel("Heart", null,
                "..L<<.>RU..",
                "vUv>^Uv<^Uv",
                "v^>>>^vv^^v",
                "v^<>Rvv>^^v",
                ">Rvv<<>>vUv",
                ">vvvvUvL<^<",
                ".DDv>^>>Rv.",
                ".>vDvUv<vD.",
                "..DL<^>RD..",
                "....L<<...."),

            new ArrowLevel("Full Moon", null,
                "......L<vv.....",
                "...>>>>Rv>RU...",
                "...^<<>vvU>^v..",
                ".UL<<<<vD^UU>R.",
                ".^L<<<<<.>^^L<.",
                "v^<v<<<<.^v>>^.",
                "vU^<U>>v>^>>R>R",
                "v^>v^<<<^>>Rv^v",
                "D^<>R>R>^^<<>^v",
                ".L<<v^>>R>R>R.D",
                ".L<^>Rv<>^>>>v.",
                ".>^^L<<>>v>>vD.",
                "..>>>>>Rvv>RD..",
                "...>>vvL<D>R...",
                ".....DDL<<....."),
        };
    }
}
