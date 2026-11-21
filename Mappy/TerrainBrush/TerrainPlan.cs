namespace Mappy.TerrainBrush
{
    using System.Collections.Generic;

    /// <summary>
    /// One 16 by 16 cell the brush has to draw again, with the base at each corner.
    /// </summary>
    public sealed class TerrainCell
    {
        public TerrainCell(int x, int y, int topLeft, int topRight, int bottomLeft, int bottomRight)
        {
            this.X = x;
            this.Y = y;
            this.TopLeft = topLeft;
            this.TopRight = topRight;
            this.BottomLeft = bottomLeft;
            this.BottomRight = bottomRight;
        }

        public int X { get; }

        public int Y { get; }

        public int TopLeft { get; }

        public int TopRight { get; }

        public int BottomLeft { get; }

        public int BottomRight { get; }

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

    public sealed class TerrainPlan
    {
        private TerrainPlan(bool accepted, string refusal, IReadOnlyList<TerrainCell> cells)
        {
            this.Accepted = accepted;
            this.Refusal = refusal ?? string.Empty;
            this.Cells = cells ?? new TerrainCell[0];
        }

        public bool Accepted { get; }

        public string Refusal { get; }

        public IReadOnlyList<TerrainCell> Cells { get; }

        public static TerrainPlan Accept(IReadOnlyList<TerrainCell> cells)
        {
            return new TerrainPlan(true, string.Empty, cells);
        }

        public static TerrainPlan Refuse(string reason)
        {
            return new TerrainPlan(false, reason, new TerrainCell[0]);
        }
    }
}
