using System.Collections.Generic;

namespace PixelParade.Model
{
    /// <summary>
    /// The campaign: five pictures drawn as text, with hand-ordered shooter lanes (front first).
    /// The EditMode tests check every level (ammo matches pixels, the greedy bot can win).
    /// </summary>
    public static class PixelLevels
    {
        public static readonly IReadOnlyList<PixelLevel> All = new[]
        {
            new PixelLevel("Lime", "Tap a shooter: it rides around and pops its colour",
                new[]
                {
                    "....GGGGGG....",
                    "...GGGGGGGG...",
                    "..GGGLLLLGGG..",
                    ".GGLLLLLLLLGG.",
                    "GGGLLLLLLLLGGG",
                    "GGLLLLLLLLLLGG",
                    "GGLLLLLLLLLLGG",
                    "GGLLLLLLLLLLGG",
                    "GGLLLLLLLLLLGG",
                    "GGGLLLLLLLLGGG",
                    ".GGLLLLLLLLGG.",
                    "..GGGLLLLGGG..",
                    "...GGGGGGGG...",
                    "....GGGGGG....",
                },
                new[] { "G40 L40", "G36 L40" }),

            new PixelLevel("Strawberry", "Shooters only hit the first pixel they can see",
                new[]
                {
                    ".......GG.......",
                    ".......GG.......",
                    ".......GG.......",
                    "......GGGG......",
                    ".GGGGGGGGGGGGGG.",
                    ".GGGGGGGGGGGGGG.",
                    ".RRRRRRRRRRRRRR.",
                    "RRRRYRRRYRRRYRRR",
                    "RRRRRRRRRRRRRRRR",
                    ".RRRRRYRRRYRRRR.",
                    ".RYRRRRRRRRRRYR.",
                    "..RRRRRRYRRRRR..",
                    "...RYRRRRRRYR...",
                    "...RRRRRRRRRR...",
                    "....RRYRRYRR....",
                    "....RRRRRRRR....",
                    ".....RRRYRR.....",
                    "......RRRR......",
                },
                new[] { "R40 Y13 R39", "G38 R40" }),

            new PixelLevel("Sailboat", null,
                new[]
                {
                    "..................",
                    ".........NRRR.....",
                    "........WNRRR.....",
                    ".......WWNW.......",
                    "......WWWNWW......",
                    "......WWWNWW......",
                    ".....WWWWNWWW.....",
                    "....WWWWWNWWWW....",
                    "...WWWWWWNWWWWW...",
                    "..WWWWWWWNWWWWW...",
                    ".NNNNNNNNNNNNNNNN.",
                    "..NNNNNNNNNNNNNN..",
                    "...NNNNNNNNNNNN...",
                    "BBBBBBBBBBBBBBBBBB",
                    "CCCBBCCCBBCCCBBCCC",
                    "BBBBBBBBBBBBBBBBBB",
                    "BBBBBBBBBBBBBBBBBB",
                },
                new[] { "B30 W30 N25", "R6 N26 C12", "W23 B30" }),

            new PixelLevel("Cat", null,
                new[]
                {
                    "..................",
                    ".O..............O.",
                    ".OOO..........OOO.",
                    ".OOOOO.OOOO.OOOOO.",
                    ".OOKOOOOOOOOOOKOO.",
                    ".OOOOOOOOOOOOOOOO.",
                    ".OOOOOOOOOOOOOOOO.",
                    ".OOOOOOOOOOOOOOOO.",
                    ".OOOGGGOOOOGGGOOO.",
                    "OOOOGDGOOOOGDGOOOO",
                    "OOOOGDGOOOOGDGOOOO",
                    "OOOOOOWWWWWWOOOOOO",
                    ".OOOOWWWKKWWWOOOO.",
                    ".OOOWWWWDWWWWWOOO.",
                    "..OOWWWWWWWWWWOO..",
                    "...OOWWWWWWWWOO...",
                    "....OOWWWWWWOO....",
                    ".......OOOO.......",
                },
                new[] { "O40 W25 D5 O40", "K4 O40 G14", "O40 W20" }),

            new PixelLevel("Parrot", null,
                new[]
                {
                    "........RRR.........",
                    "......RRRRRRR.......",
                    ".....RRRRRRRRR......",
                    "....RRRRRRWWWWR.....",
                    "....RRRRRRWWDWRYY...",
                    "....RRRRRRWWWWRYYYY.",
                    "....RRRRRRRWWRRYYYY.",
                    "...GGRRRRRRRRRGYY...",
                    "...GGRRRRRRRRRGGG...",
                    "...BBBGRRRRRLLGGG...",
                    "...BBBBBLLLLLLGGG...",
                    "...GBBBBLLLLLLLGG...",
                    "...GBBBLLLLLLLLGG...",
                    "...GBBBLLLLLLLLGG...",
                    "....BBBLLLLLLLGG....",
                    "....GBGLLLLLLLGG....",
                    ".....BGGLLLLLGG.....",
                    ".....BGGGLLLGG......",
                    "........YGGGY.......",
                    "NNNNNNNNNNNNNNNNNNNN",
                    ".........GG.........",
                    "....................",
                },
                new[] { "R40 L30 D1", "N20 G24 W13", "Y14 R32 L23", "G20 B24" }, slots: 4),
        };
    }
}
