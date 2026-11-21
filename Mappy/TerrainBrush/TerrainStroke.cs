namespace Mappy.TerrainBrush
{
    using System.Collections.Generic;
    using System.Drawing;

    using Mappy.Operations;

    /// <summary>
    /// What one brush click would write: the cells to stamp and the
    /// height-grid areas whose features it clears, or the reason it refuses.
    /// </summary>
    public sealed class TerrainStroke
    {
        private static readonly IReadOnlyList<TerrainBrushStamp> NoStamps = new TerrainBrushStamp[0];

        private static readonly IReadOnlyList<Rectangle> NoAreas = new Rectangle[0];

        private TerrainStroke(string refusal, IReadOnlyList<TerrainBrushStamp> stamps, IReadOnlyList<Rectangle> clearedAreas)
        {
            this.Refusal = refusal;
            this.Stamps = stamps;
            this.ClearedAreas = clearedAreas;
        }

        public bool Accepted => this.Refusal == null;

        public string Refusal { get; }

        public IReadOnlyList<TerrainBrushStamp> Stamps { get; }

        public IReadOnlyList<Rectangle> ClearedAreas { get; }

        public static TerrainStroke Refuse(string reason)
        {
            return new TerrainStroke(reason ?? string.Empty, NoStamps, NoAreas);
        }

        public static TerrainStroke Accept(IReadOnlyList<TerrainBrushStamp> stamps, IReadOnlyList<Rectangle> clearedAreas)
        {
            return new TerrainStroke(null, stamps ?? NoStamps, clearedAreas ?? NoAreas);
        }
    }
}
