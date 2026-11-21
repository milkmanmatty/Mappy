namespace Mappy.TerrainBrush
{
    using System;

    using Mappy.Collections;

    /// <summary>
    /// The height samples along the four edges of a section or a map cell,
    /// read left to right and top to bottom.
    /// </summary>
    public sealed class TerrainEdges
    {
        public TerrainEdges(int[] top, int[] right, int[] bottom, int[] left)
        {
            this.Top = top ?? throw new ArgumentNullException(nameof(top));
            this.Right = right ?? throw new ArgumentNullException(nameof(right));
            this.Bottom = bottom ?? throw new ArgumentNullException(nameof(bottom));
            this.Left = left ?? throw new ArgumentNullException(nameof(left));
        }

        public int[] Top { get; }

        public int[] Right { get; }

        public int[] Bottom { get; }

        public int[] Left { get; }

        public int[] Edge(int side)
        {
            switch (side)
            {
                case TerrainSides.Top:
                    return this.Top;
                case TerrainSides.Right:
                    return this.Right;
                case TerrainSides.Bottom:
                    return this.Bottom;
                default:
                    return this.Left;
            }
        }

        /// <summary>
        /// Reads the edges of a width by height block of height samples.
        /// </summary>
        public static TerrainEdges Read(Func<int, int, int> sample, int width, int height)
        {
            if (width <= 0 || height <= 0)
            {
                throw new ArgumentException("The block has no height samples.");
            }

            var top = new int[width];
            var bottom = new int[width];
            for (var x = 0; x < width; x++)
            {
                top[x] = sample(x, 0);
                bottom[x] = sample(x, height - 1);
            }

            var left = new int[height];
            var right = new int[height];
            for (var y = 0; y < height; y++)
            {
                left[y] = sample(0, y);
                right[y] = sample(width - 1, y);
            }

            return new TerrainEdges(top, right, bottom, left);
        }

        /// <summary>
        /// Reads the edges of one brush cell from the map's height grid.
        /// Samples past the map edge read the nearest sample on the map.
        /// Returns null when the cell is wholly off the map.
        /// </summary>
        public static TerrainEdges ReadCell(IGrid<int> heights, int cellX, int cellY)
        {
            var size = TerrainPlanner.CellTiles * 2;
            var originX = cellX * size;
            var originY = cellY * size;
            if (cellX < 0 || cellY < 0 || originX >= heights.Width || originY >= heights.Height)
            {
                return null;
            }

            return Read(
                (x, y) => heights.Get(
                    Math.Min(heights.Width - 1, originX + x),
                    Math.Min(heights.Height - 1, originY + y)),
                size,
                size);
        }
    }
}
