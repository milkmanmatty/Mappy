namespace Mappy.TerrainBrush
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;

    /// <summary>
    /// Plans one move of the river or ridge brush. A click on an empty cell
    /// drops the style's lone piece (a pond or a peak). Moving from one cell to
    /// the next joins them: each gains the side facing the other, and both are
    /// restamped with the piece for their new sides. A move to a cell that cannot
    /// hold the path (open water, say) just ends the run, and moving onto a
    /// coast or cliff cell uses a mouth piece where the style has one.
    /// </summary>
    public static class TerrainPathPlanner
    {
        public static TerrainPathPlan Plan(
            TerrainPathStyle style,
            int cellsX,
            int cellsY,
            Point? from,
            Point to,
            Func<int, int, TerrainPathCell> readCell)
        {
            if (style == null)
            {
                throw new ArgumentNullException(nameof(style));
            }

            if (readCell == null)
            {
                throw new ArgumentNullException(nameof(readCell));
            }

            if (!Inside(to, cellsX, cellsY))
            {
                return TerrainPathPlan.Accept(null);
            }

            var steps = new List<TerrainPathStep>();
            var stepIndex = new Dictionary<Point, int>();
            Func<Point, TerrainPathCell> read = point =>
            {
                var cell = readCell(point.X, point.Y);
                int index;
                return stepIndex.TryGetValue(point, out index)
                    ? new TerrainPathCell(cell.Corners, steps[index].Mask)
                    : cell;
            };
            Action<Point, int, IReadOnlyList<TerrainSectionRef>> record = (point, mask, sections) =>
            {
                var step = new TerrainPathStep(point.X, point.Y, mask, sections);
                int index;
                if (stepIndex.TryGetValue(point, out index))
                {
                    steps[index] = step;
                }
                else
                {
                    stepIndex.Add(point, steps.Count);
                    steps.Add(step);
                }
            };

            if (!from.HasValue || from.Value == to)
            {
                var cell = read(to);
                if (cell.Mask == TerrainSides.None && IsFlat(cell.Corners))
                {
                    var lone = style.Sections(cell.Corners, TerrainSides.None);
                    if (lone != null)
                    {
                        record(to, TerrainSides.None, lone);
                    }
                }

                return TerrainPathPlan.Accept(steps);
            }

            var current = from.Value;
            while (current != to)
            {
                var dx = to.X - current.X;
                var dy = to.Y - current.Y;
                var side = Math.Abs(dx) >= Math.Abs(dy)
                    ? (dx > 0 ? TerrainSides.Right : TerrainSides.Left)
                    : (dy > 0 ? TerrainSides.Bottom : TerrainSides.Top);
                var next = new Point(current.X + TerrainSides.OffsetX(side), current.Y + TerrainSides.OffsetY(side));
                if (Inside(current, cellsX, cellsY))
                {
                    var refusal = Join(style, read, record, current, next, side);
                    if (refusal != null)
                    {
                        return TerrainPathPlan.Refuse(refusal);
                    }
                }

                current = next;
            }

            return TerrainPathPlan.Accept(steps);
        }

        private static string Join(
            TerrainPathStyle style,
            Func<Point, TerrainPathCell> read,
            Action<Point, int, IReadOnlyList<TerrainSectionRef>> record,
            Point current,
            Point next,
            int side)
        {
            var a = read(current);
            var b = read(next);
            if (a.Mask == TerrainPathShape.NotAPath || b.Mask == TerrainPathShape.NotAPath
                || !style.Accepts(a.Corners) || !style.Accepts(b.Corners))
            {
                return null;
            }

            var opposite = TerrainSides.Opposite(side);
            if ((a.Mask & side) != 0 && (b.Mask & opposite) != 0)
            {
                return null;
            }

            var maskA = a.Mask | side;
            var maskB = b.Mask | opposite;
            var sectionsA = style.Sections(a.Corners, maskA);
            var sectionsB = style.Sections(b.Corners, maskB);
            if (sectionsA == null || sectionsB == null)
            {
                return "The " + style.Name + " pieces cannot join these cells.";
            }

            record(current, maskA, sectionsA);
            record(next, maskB, sectionsB);
            return null;
        }

        private static bool Inside(Point point, int cellsX, int cellsY)
        {
            return point.X >= 0 && point.Y >= 0 && point.X < cellsX && point.Y < cellsY;
        }

        private static bool IsFlat(int[] corners)
        {
            return corners[0] == corners[1] && corners[0] == corners[2] && corners[0] == corners[3];
        }
    }
}
