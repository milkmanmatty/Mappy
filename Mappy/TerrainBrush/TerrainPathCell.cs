namespace Mappy.TerrainBrush
{
    /// <summary>
    /// What the path brush reads from one cell: its four corner bases and the
    /// sides a path already runs through, or TerrainPathShape.NotAPath when the
    /// corners cannot hold a path.
    /// </summary>
    public struct TerrainPathCell
    {
        public TerrainPathCell(int[] corners, int mask)
        {
            this.Corners = corners;
            this.Mask = mask;
        }

        public int[] Corners { get; }

        public int Mask { get; }
    }
}
