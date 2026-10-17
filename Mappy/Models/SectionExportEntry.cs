namespace Mappy.Models
{
    using Mappy.Data;

    public class SectionExportEntry
    {
        public SectionExportEntry(IMapTile tile, string fileName)
        {
            this.Tile = tile;
            this.FileName = fileName;
        }

        public IMapTile Tile { get; }

        public string FileName { get; }
    }
}
