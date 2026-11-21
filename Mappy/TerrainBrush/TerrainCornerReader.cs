namespace Mappy.TerrainBrush
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;

    using Mappy.Collections;

    /// <summary>
    /// Reads the base at a cell corner from the height samples that touch it.
    /// </summary>
    public static class TerrainCornerReader
    {
        public static int Read(TerrainWorld world, IGrid<int> heights, int cornerX, int cornerY)
        {
            return Read(world, heights, null, cornerX, cornerY);
        }

        /// <summary>
        /// Reads a corner, ignoring samples on unpainted (default) tiles unless
        /// every sample is unpainted, so a blank area does not outvote painted
        /// terrain. A corner past the map edge, where the last cell is only
        /// partly on the map, reads the nearest samples on the map.
        /// </summary>
        public static int Read(TerrainWorld world, IGrid<int> heights, IGrid<Bitmap> tiles, int cornerX, int cornerY)
        {
            if (heights.Width <= 0 || heights.Height <= 0)
            {
                return world.VoteSamples(new int[0]);
            }

            var originX = cornerX * TerrainPlanner.CellTiles * 2;
            var originY = cornerY * TerrainPlanner.CellTiles * 2;
            var painted = new List<int>(4);
            var all = new List<int>(4);
            for (var dy = -1; dy <= 0; dy++)
            {
                for (var dx = -1; dx <= 0; dx++)
                {
                    var x = Clamp(originX + dx, heights.Width);
                    var y = Clamp(originY + dy, heights.Height);
                    var height = heights.Get(x, y);
                    all.Add(height);
                    if (tiles == null || !IsDefault(tiles, x / 2, y / 2))
                    {
                        painted.Add(height);
                    }
                }
            }

            return world.VoteSamples(painted.Count > 0 ? painted : all);
        }

        private static bool IsDefault(IGrid<Bitmap> tiles, int x, int y)
        {
            if (x >= tiles.Width || y >= tiles.Height)
            {
                return true;
            }

            var tile = tiles.Get(x, y);
            return tile == null || ReferenceEquals(tile, Globals.DefaultTile);
        }

        private static int Clamp(int value, int size)
        {
            return Math.Max(0, Math.Min(size - 1, value));
        }
    }
}
