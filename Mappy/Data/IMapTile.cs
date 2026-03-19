namespace Mappy.Data
{
    using System.Drawing;

    using Collections;

    /// <summary>
    /// Interface for a map tile.
    /// </summary>
    public interface IMapTile
    {
        IGrid<Bitmap> TileGrid { get; }

        IGrid<int> HeightGrid { get; }
    }
}
