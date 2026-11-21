namespace Mappy.TerrainBrush
{
    /// <summary>
    /// The four sides of a brush cell as bits, so a set of sides is a mask.
    /// </summary>
    public static class TerrainSides
    {
        public const int None = 0;

        public const int Top = 1;

        public const int Right = 2;

        public const int Bottom = 4;

        public const int Left = 8;

        public static readonly int[] All = { Top, Right, Bottom, Left };

        public static int Opposite(int side)
        {
            switch (side)
            {
                case Top:
                    return Bottom;
                case Right:
                    return Left;
                case Bottom:
                    return Top;
                default:
                    return Right;
            }
        }

        public static int OffsetX(int side)
        {
            return side == Right ? 1 : side == Left ? -1 : 0;
        }

        public static int OffsetY(int side)
        {
            return side == Bottom ? 1 : side == Top ? -1 : 0;
        }

        /// <summary>
        /// The two corners on a side, as TerrainCorners indices.
        /// </summary>
        public static int[] Corners(int side)
        {
            switch (side)
            {
                case Top:
                    return new[] { TerrainCorners.TopLeft, TerrainCorners.TopRight };
                case Right:
                    return new[] { TerrainCorners.TopRight, TerrainCorners.BottomRight };
                case Bottom:
                    return new[] { TerrainCorners.BottomLeft, TerrainCorners.BottomRight };
                default:
                    return new[] { TerrainCorners.TopLeft, TerrainCorners.BottomLeft };
            }
        }

        public static int Count(int mask)
        {
            var count = 0;
            foreach (var side in All)
            {
                if ((mask & side) != 0)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
