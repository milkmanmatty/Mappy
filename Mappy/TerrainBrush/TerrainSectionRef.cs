namespace Mappy.TerrainBrush
{
    public sealed class TerrainSectionRef
    {
        public TerrainSectionRef(string hpiFileName, string sctFileName, int tileWidth, int tileHeight)
            : this(hpiFileName, sctFileName, tileWidth, tileHeight, null)
        {
        }

        public TerrainSectionRef(string hpiFileName, string sctFileName, int tileWidth, int tileHeight, TerrainEdges edges)
        {
            this.HpiFileName = hpiFileName;
            this.SctFileName = sctFileName;
            this.TileWidth = tileWidth;
            this.TileHeight = tileHeight;
            this.Edges = edges;
        }

        public string HpiFileName { get; }

        public string SctFileName { get; }

        public int TileWidth { get; }

        public int TileHeight { get; }

        /// <summary>
        /// The heights along the section's edges, or null when they were not read.
        /// </summary>
        public TerrainEdges Edges { get; }
    }
}
