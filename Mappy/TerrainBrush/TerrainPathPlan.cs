namespace Mappy.TerrainBrush
{
    using System.Collections.Generic;

    public sealed class TerrainPathPlan
    {
        private static readonly IReadOnlyList<TerrainPathStep> NoSteps = new TerrainPathStep[0];

        private TerrainPathPlan(string refusal, IReadOnlyList<TerrainPathStep> steps)
        {
            this.Refusal = refusal;
            this.Steps = steps;
        }

        public bool Accepted => this.Refusal == null;

        public string Refusal { get; }

        public IReadOnlyList<TerrainPathStep> Steps { get; }

        public static TerrainPathPlan Refuse(string reason)
        {
            return new TerrainPathPlan(reason ?? string.Empty, NoSteps);
        }

        public static TerrainPathPlan Accept(IReadOnlyList<TerrainPathStep> steps)
        {
            return new TerrainPathPlan(null, steps ?? NoSteps);
        }
    }
}
