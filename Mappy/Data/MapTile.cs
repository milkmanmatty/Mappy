namespace Mappy.Data
{
    using System.Diagnostics;
    using System.Drawing;

    using Mappy.Collections;

    public class MapTile : IMapTile
    {
        public const int TileWidth = 32;
        public const int TileHeight = 32;
        public const int AttrWidth = TileWidth / 2;
        public const int AttrHeight = TileHeight / 2;

        public static readonly Size TileSize = new Size(MapTile.TileWidth, MapTile.TileHeight);
        public static readonly Size AttrSize = new Size(MapTile.AttrWidth, MapTile.AttrHeight);

        private static readonly Bitmap DefaultSquare = new Bitmap(MapTile.TileWidth, MapTile.TileHeight);

        public MapTile(int width, int height)
        {
            this.TileGrid = new Grid<Bitmap>(width, height);
            this.HeightGrid = new Grid<int>(width * 2, height * 2);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    this.TileGrid[x, y] = MapTile.DefaultSquare;
                }
            }
        }

        public IGrid<Bitmap> TileGrid { get; private set; }

        public IGrid<int> HeightGrid { get; private set; }

        public void Merge(IMapTile other, int x, int y)
        {
            this.Merge(other, 0, 0, x, y, other.TileGrid.Width, other.TileGrid.Height);
        }

        public void Merge(IMapTile other, int sourceX, int sourceY, int destX, int destY, int width, int height)
        {
            this.TileGrid.Merge(other.TileGrid, sourceX, sourceY, destX, destY, width, height);
            this.HeightGrid.Merge(other.HeightGrid, sourceX * 2, sourceY * 2, destX * 2, destY * 2, width * 2, height * 2);
        }
    }
}
