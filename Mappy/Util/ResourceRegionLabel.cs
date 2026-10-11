namespace Mappy.Util
{
    using System.Drawing;

    public sealed class ResourceRegionLabel
    {
        public ResourceRegionLabel(Point location, double patchMetal, int wreckMetal, int energy)
        {
            this.Location = location;
            this.PatchMetal = patchMetal;
            this.WreckMetal = wreckMetal;
            this.Energy = energy;
        }

        public Point Location { get; }

        public double PatchMetal { get; }

        public int WreckMetal { get; }

        public int Energy { get; }
    }
}
