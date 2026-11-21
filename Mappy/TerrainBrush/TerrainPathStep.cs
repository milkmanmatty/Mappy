namespace Mappy.TerrainBrush
{
    using System.Collections.Generic;

    /// <summary>
    /// One cell the path brush restamps, with the sides the path will run
    /// through and the pieces that fit.
    /// </summary>
    public sealed class TerrainPathStep
    {
        public TerrainPathStep(int x, int y, int mask, IReadOnlyList<TerrainSectionRef> sections)
        {
            this.X = x;
            this.Y = y;
            this.Mask = mask;
            this.Sections = sections;
        }

        public int X { get; }

        public int Y { get; }

        public int Mask { get; }

        public IReadOnlyList<TerrainSectionRef> Sections { get; }
    }
}
