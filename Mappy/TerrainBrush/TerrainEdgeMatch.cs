namespace Mappy.TerrainBrush
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Chooses among pieces for one cell by how well their edge heights meet
    /// the neighbouring cells, so a slope next to a cliff takes the piece made
    /// to meet a cliff on that side.
    /// </summary>
    public static class TerrainEdgeMatch
    {
        /// <summary>
        /// Pieces within this much average height difference per compared sample
        /// of the best are treated as equally good.
        /// </summary>
        public const int Tolerance = 2;

        /// <summary>
        /// The sum of height differences between a piece's edge on one side and
        /// the neighbour's edge that touches it. Edges of different lengths are
        /// compared at matching positions.
        /// </summary>
        public static int Cost(int[] edge, int[] facing, out int samples)
        {
            samples = 0;
            if (edge == null || facing == null || edge.Length == 0 || facing.Length == 0)
            {
                return 0;
            }

            var count = Math.Max(edge.Length, facing.Length);
            var cost = 0;
            for (var index = 0; index < count; index++)
            {
                var a = edge[(index * edge.Length) / count];
                var b = facing[(index * facing.Length) / count];
                cost += Math.Abs(a - b);
            }

            samples = count;
            return cost;
        }

        /// <summary>
        /// The index of the chosen candidate. facing holds, for each side in
        /// TerrainSides.All order, the neighbour's edge touching that side, or
        /// null when the neighbour is unknown. The seed picks among the
        /// candidates that match equally well.
        /// </summary>
        public static int Pick(IReadOnlyList<TerrainSectionRef> candidates, int[][] facing, int seed)
        {
            if (candidates == null || candidates.Count == 0)
            {
                throw new ArgumentException("There is nothing to pick from.", nameof(candidates));
            }

            var costs = new long[candidates.Count];
            var best = long.MaxValue;
            var samples = 0;
            for (var index = 0; index < candidates.Count; index++)
            {
                var edges = candidates[index].Edges;
                long cost = 0;
                var compared = 0;
                for (var sideIndex = 0; sideIndex < TerrainSides.All.Length; sideIndex++)
                {
                    if (facing == null || facing[sideIndex] == null)
                    {
                        continue;
                    }

                    if (edges == null)
                    {
                        cost = long.MaxValue / 2;
                        break;
                    }

                    int sideSamples;
                    cost += Cost(edges.Edge(TerrainSides.All[sideIndex]), facing[sideIndex], out sideSamples);
                    compared += sideSamples;
                }

                costs[index] = cost;
                if (cost < best)
                {
                    best = cost;
                    samples = compared;
                }
            }

            var limit = best + ((long)Tolerance * samples);
            var near = new List<int>();
            for (var index = 0; index < costs.Length; index++)
            {
                if (costs[index] <= limit)
                {
                    near.Add(index);
                }
            }

            return near[(seed & 0x7fffffff) % near.Count];
        }
    }
}
