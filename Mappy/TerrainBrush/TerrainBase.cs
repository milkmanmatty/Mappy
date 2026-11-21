namespace Mappy.TerrainBrush
{
    using System.Collections.Generic;

    public sealed class TerrainBase
    {
        public TerrainBase(string name, int nominal, int measuredHeight, IReadOnlyList<TerrainSectionRef> sections)
        {
            this.Name = name;
            this.Nominal = nominal;
            this.MeasuredHeight = measuredHeight;
            this.Sections = sections;
        }

        public string Name { get; }

        public int Nominal { get; }

        public int MeasuredHeight { get; }

        public IReadOnlyList<TerrainSectionRef> Sections { get; }
    }
}
