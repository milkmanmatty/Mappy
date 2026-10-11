namespace Mappy.Collections
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    using Mappy.Data;

    /// <summary>
    /// Read-only height grid that paints base heights with floating tiles overlaid.
    /// Later floating tiles win where they overlap. Lifted selections zero the base
    /// heightmap, so contour and grid overlays must sample the floating tile instead.
    /// </summary>
    public class CompositeHeightGrid : IGrid<int>
    {
        private readonly IGrid<int> baseHeights;

        private readonly IList<Positioned<IMapTile>> floatingTiles;

        public CompositeHeightGrid(IGrid<int> baseHeights, IList<Positioned<IMapTile>> floatingTiles)
        {
            this.baseHeights = baseHeights ?? throw new ArgumentNullException(nameof(baseHeights));
            this.floatingTiles = floatingTiles ?? throw new ArgumentNullException(nameof(floatingTiles));
        }

        public int Width => this.baseHeights.Width;

        public int Height => this.baseHeights.Height;

        public int this[int index]
        {
            get
            {
                var coords = this.baseHeights.ToCoords(index);
                return this.Sample(coords.X, coords.Y);
            }

            set
            {
                throw new NotSupportedException("Composite height grid is read-only.");
            }
        }

        public IEnumerator<int> GetEnumerator()
        {
            var count = this.Width * this.Height;
            for (var i = 0; i < count; i++)
            {
                yield return this[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        private int Sample(int x, int y)
        {
            for (var i = this.floatingTiles.Count - 1; i >= 0; i--)
            {
                var tile = this.floatingTiles[i];
                if (tile?.Item == null)
                {
                    continue;
                }

                var originX = tile.Location.X * 2;
                var originY = tile.Location.Y * 2;
                var localX = x - originX;
                var localY = y - originY;
                var heights = tile.Item.HeightGrid;
                if (localX >= 0 && localY >= 0 && localX < heights.Width && localY < heights.Height)
                {
                    return heights.Get(localX, localY);
                }
            }

            return this.baseHeights.Get(x, y);
        }
    }
}
