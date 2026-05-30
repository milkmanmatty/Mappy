namespace Mappy.Data
{
    using System.Drawing;

    using Mappy.Collections;
    using Mappy.Util;

    /// <summary>
    /// Represents the "blueprint" for a feature.
    /// Contains metadata about the feature.
    /// </summary>
    public class Feature
    {
        public string Name { get; set; }

        public string World { get; set; }

        public string Category { get; set; }

        public Size Footprint { get; set; }

        public Point Offset { get; set; }

        public Bitmap Image { get; set; }

        public string ResourceFileName { get; set; }

        public Maybe<ReclaimInfoStruct> ReclaimInfo { get; set; }

        public int MetalSpotValue { get; set; }

        public bool Permanent { get; set; }

        public Rectangle GetDrawBounds(IGrid<int> heightmap, int posX, int posY)
        {
            var basePoint = this.GetBasePoint(heightmap, posX, posY);
            var pos = new Point(basePoint.X - this.Offset.X, basePoint.Y - this.Offset.Y);
            return new Rectangle(pos, this.Image.Size);
        }

        public Point GetBasePoint(IGrid<int> heightmap, int posX, int posY)
        {
            var height = 0;
            if (posX >= 0 && posX < heightmap.Width - 1 && posY >= 0 && posY < heightmap.Height - 1)
            {
                height = Util.ComputeMidpointHeight(heightmap, posX, posY);
            }

            return new Point(
                (posX * 16) + (this.Footprint.Width * 8),
                (posY * 16) + (this.Footprint.Height * 8) - (height / 2));
        }

        public struct ReclaimInfoStruct
        {
            public int MetalValue { get; set; }

            public int EnergyValue { get; set; }
        }
    }
}
