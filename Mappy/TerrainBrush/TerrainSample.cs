namespace Mappy.TerrainBrush
{
    public struct TerrainSample
    {
        public int TileWidth;

        public int TileHeight;

        public int TopLeft;

        public int TopRight;

        public int BottomLeft;

        public int BottomRight;

        /// <summary>
        /// The heights along the section's edges, or null when they were not read.
        /// </summary>
        public TerrainEdges Edges;

        public int Corner(int corner)
        {
            switch (corner)
            {
                case TerrainCorners.TopLeft:
                    return this.TopLeft;
                case TerrainCorners.TopRight:
                    return this.TopRight;
                case TerrainCorners.BottomLeft:
                    return this.BottomLeft;
                default:
                    return this.BottomRight;
            }
        }
    }
}
