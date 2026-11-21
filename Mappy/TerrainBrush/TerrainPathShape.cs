namespace Mappy.TerrainBrush
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Works out which sides a river or ridge runs through from the heights
    /// along a cell's edges, so pieces in the archive and cells on the map
    /// are read the same way.
    /// A path cell has four corners on one base, or it is a mouth: two corners
    /// on the path's base and the other two on the sea (for a river) or on a
    /// plateau (for a ridge). A mouth only connects on the side facing away
    /// from the sea or plateau.
    /// </summary>
    public static class TerrainPathShape
    {
        public const int NotAPath = -1;

        /// <summary>
        /// How far the middle of an edge must dip below (or rise above) the
        /// ground for the path to count as running through it.
        /// </summary>
        public const int Threshold = 25;

        public static int Measure(
            IReadOnlyList<TerrainBase> bases,
            TerrainPathKind kind,
            int[] corners,
            TerrainEdges edges)
        {
            if (bases == null || corners == null || corners.Length != TerrainCorners.Count || edges == null)
            {
                return NotAPath;
            }

            int ground;
            var mouth = FindMouth(bases, kind, corners, out ground);
            if (mouth == NotAPath)
            {
                return NotAPath;
            }

            var groundHeight = bases[ground].MeasuredHeight;
            var mask = TerrainSides.None;
            foreach (var side in TerrainSides.All)
            {
                if (mouth != TerrainSides.None && side != TerrainSides.Opposite(mouth))
                {
                    continue;
                }

                if (RunsThrough(kind, edges.Edge(side), groundHeight))
                {
                    mask |= side;
                }
            }

            return mask;
        }

        /// <summary>
        /// The side facing the sea or plateau, None for a cell on one base,
        /// or NotAPath for any other layout.
        /// </summary>
        public static int FindMouth(IReadOnlyList<TerrainBase> bases, TerrainPathKind kind, int[] corners, out int ground)
        {
            ground = corners[0];
            if (corners[0] == corners[1] && corners[0] == corners[2] && corners[0] == corners[3])
            {
                return TerrainSides.None;
            }

            foreach (var side in TerrainSides.All)
            {
                var near = TerrainSides.Corners(side);
                var far = TerrainSides.Corners(TerrainSides.Opposite(side));
                var outside = corners[near[0]];
                var inside = corners[far[0]];
                if (outside == inside || corners[near[1]] != outside || corners[far[1]] != inside)
                {
                    continue;
                }

                var outsideHeight = bases[outside].MeasuredHeight;
                var insideHeight = bases[inside].MeasuredHeight;
                var faces = kind == TerrainPathKind.Channel ? outsideHeight < insideHeight : outsideHeight > insideHeight;
                if (faces)
                {
                    ground = inside;
                    return side;
                }
            }

            return NotAPath;
        }

        private static bool RunsThrough(TerrainPathKind kind, int[] edge, int groundHeight)
        {
            if (edge.Length == 0)
            {
                return false;
            }

            var start = edge.Length / 4;
            var end = Math.Max(start + 1, edge.Length - start);
            var extreme = edge[start];
            for (var index = start; index < end; index++)
            {
                extreme = kind == TerrainPathKind.Channel ? Math.Min(extreme, edge[index]) : Math.Max(extreme, edge[index]);
            }

            return kind == TerrainPathKind.Channel
                ? extreme < groundHeight - Threshold
                : extreme > groundHeight + Threshold;
        }
    }
}
