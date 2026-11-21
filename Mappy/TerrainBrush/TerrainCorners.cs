namespace Mappy.TerrainBrush
{
    /// <summary>
    /// The four corners of a 16 by 16 brush cell, in the order top left,
    /// top right, bottom left, bottom right. Bit 0 marks the top left.
    /// </summary>
    public static class TerrainCorners
    {
        public const int TopLeft = 0;

        public const int TopRight = 1;

        public const int BottomLeft = 2;

        public const int BottomRight = 3;

        public const int Count = 4;

        public const int FlatMask = 0;

        public const int FullMask = 15;

        /// <summary>
        /// The two layouts with opposite corners raised. No section is drawn for them.
        /// </summary>
        public const int DiagonalMaskA = TopLeftBit | BottomRightBit;

        public const int DiagonalMaskB = TopRightBit | BottomLeftBit;

        private const int TopLeftBit = 1;

        private const int TopRightBit = 2;

        private const int BottomLeftBit = 4;

        private const int BottomRightBit = 8;

        private static readonly int[] CornerBits = { TopLeftBit, TopRightBit, BottomLeftBit, BottomRightBit };

        /// <summary>
        /// Every two-base layout that has a section: all masks except flat, full and the two diagonals.
        /// </summary>
        public static readonly int[] TransitionMasks =
        {
            TopLeftBit,
            TopRightBit,
            TopLeftBit | TopRightBit,
            BottomLeftBit,
            TopLeftBit | BottomLeftBit,
            TopLeftBit | TopRightBit | BottomLeftBit,
            BottomRightBit,
            TopRightBit | BottomRightBit,
            TopLeftBit | TopRightBit | BottomRightBit,
            BottomLeftBit | BottomRightBit,
            TopRightBit | BottomLeftBit | BottomRightBit,
            TopLeftBit | BottomLeftBit | BottomRightBit,
        };

        public static int Bit(int corner)
        {
            return CornerBits[corner];
        }

        public static bool IsTransitionMask(int mask)
        {
            return mask > FlatMask && mask < FullMask && mask != DiagonalMaskA && mask != DiagonalMaskB;
        }
    }
}
