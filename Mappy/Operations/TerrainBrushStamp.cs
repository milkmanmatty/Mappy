namespace Mappy.Operations
{
    using System.Drawing;

    using Mappy.Collections;
    using Mappy.Data;
    using Mappy.TerrainBrush;

    /// <summary>
    /// The tiles and heights one cell writes, placed at a tile coordinate.
    /// A section smaller than a cell is repeated so the cell is filled.
    /// </summary>
    public sealed class TerrainBrushStamp
    {
        public TerrainBrushStamp(IGrid<Bitmap> tiles, IGrid<int> heights, int tileX, int tileY)
        {
            this.Tiles = tiles;
            this.Heights = heights;
            this.TileX = tileX;
            this.TileY = tileY;
        }

        public IGrid<Bitmap> Tiles { get; }

        public IGrid<int> Heights { get; }

        public int TileX { get; }

        public int TileY { get; }

        /// <summary>
        /// True when the map already holds exactly these tiles and heights,
        /// clipped to the map edge, so stamping would change nothing.
        /// </summary>
        public bool Matches(IGrid<Bitmap> mapTiles, IGrid<int> mapHeights)
        {
            return Covers(this.Tiles, mapTiles, this.TileX, this.TileY, (a, b) => ReferenceEquals(a, b))
                && Covers(this.Heights, mapHeights, this.TileX * 2, this.TileY * 2, (a, b) => a == b);
        }

        public static TerrainBrushStamp FromSection(MapTile section, int tileX, int tileY)
        {
            const int Cell = TerrainPlanner.CellTiles;
            var tiles = section.TileGrid;
            var heights = section.HeightGrid;
            if (tiles.Width <= 0 || tiles.Height <= 0)
            {
                return new TerrainBrushStamp(tiles, heights, tileX, tileY);
            }

            if (tiles.Width <= Cell && tiles.Height <= Cell
                && Cell % tiles.Width == 0 && Cell % tiles.Height == 0
                && heights.Width >= tiles.Width * 2 && heights.Height >= tiles.Height * 2
                && (tiles.Width != Cell || tiles.Height != Cell))
            {
                var tiledTiles = new Grid<Bitmap>(Cell, Cell);
                var tiledHeights = new Grid<int>(Cell * 2, Cell * 2);
                for (var y = 0; y < Cell; y += tiles.Height)
                {
                    for (var x = 0; x < Cell; x += tiles.Width)
                    {
                        GridMethods.Copy(tiles, tiledTiles, x, y);
                        GridMethods.Copy(heights, tiledHeights, 0, 0, x * 2, y * 2, tiles.Width * 2, tiles.Height * 2);
                    }
                }

                return new TerrainBrushStamp(tiledTiles, tiledHeights, tileX, tileY);
            }

            if (tiles.Width > Cell || tiles.Height > Cell)
            {
                var clipWidth = tiles.Width > Cell ? Cell : tiles.Width;
                var clipHeight = tiles.Height > Cell ? Cell : tiles.Height;
                var clippedTiles = new Grid<Bitmap>(clipWidth, clipHeight);
                GridMethods.Copy(tiles, clippedTiles, 0, 0, 0, 0, clipWidth, clipHeight);
                var clippedHeights = new Grid<int>(clipWidth * 2, clipHeight * 2);
                var heightWidth = heights.Width < clipWidth * 2 ? heights.Width : clipWidth * 2;
                var heightHeight = heights.Height < clipHeight * 2 ? heights.Height : clipHeight * 2;
                if (heightWidth > 0 && heightHeight > 0)
                {
                    GridMethods.Copy(heights, clippedHeights, 0, 0, 0, 0, heightWidth, heightHeight);
                }

                return new TerrainBrushStamp(clippedTiles, clippedHeights, tileX, tileY);
            }

            return new TerrainBrushStamp(tiles, heights, tileX, tileY);
        }

        private static bool Covers<T>(IGrid<T> source, IGrid<T> map, int originX, int originY, System.Func<T, T, bool> same)
        {
            for (var y = 0; y < source.Height; y++)
            {
                var mapY = originY + y;
                if (mapY < 0 || mapY >= map.Height)
                {
                    continue;
                }

                for (var x = 0; x < source.Width; x++)
                {
                    var mapX = originX + x;
                    if (mapX < 0 || mapX >= map.Width)
                    {
                        continue;
                    }

                    if (!same(source.Get(x, y), map.Get(mapX, mapY)))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
