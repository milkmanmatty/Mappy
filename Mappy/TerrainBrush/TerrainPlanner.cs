namespace Mappy.TerrainBrush
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Decides which cells a stroke replaces. The painted block becomes one base.
    /// A cell is kept when its four corners are the same base, or the world has a
    /// piece for that exact layout. Any cell with a changed corner that has no
    /// piece is repaired by moving its unlocked corners toward the painted base,
    /// or sideways once, choosing the repair with the fewest corner changes over
    /// the whole stroke so a fix does not ripple along a coast. Corners further
    /// from the block than the world has bases are left alone, and the stroke is
    /// refused if that cannot make every changed cell valid.
    /// </summary>
    public static class TerrainPlanner
    {
        public const int CellTiles = 16;

        private const int SearchBudget = 4000;

        /// <summary>
        /// The brush size that paints a single grid corner.
        /// </summary>
        public const int CornerBrush = 0;

        public static readonly int[] BrushSizes = { CornerBrush, 16, 32, 64, 128 };

        public static TerrainPlan Plan(
            TerrainWorld world,
            int cellsX,
            int cellsY,
            int blockX,
            int blockY,
            int blockCells,
            int level,
            Func<int, int, int> readCorner,
            int spread = -1)
        {
            if (world == null || world.Bases.Count == 0)
            {
                return TerrainPlan.Refuse("No brush world is loaded");
            }

            if (blockCells <= 0)
            {
                return TerrainPlan.Refuse("The brush is outside the map");
            }

            return Run(world, cellsX, cellsY, blockX, blockY, blockCells, level, readCorner, spread);
        }

        /// <summary>
        /// Paints a square of grid corners rather than cells. One corner gives
        /// the four cells around it a transition piece each and no flat cell.
        /// </summary>
        public static TerrainPlan PlanCorners(
            TerrainWorld world,
            int cellsX,
            int cellsY,
            int cornerX,
            int cornerY,
            int cornerCount,
            int level,
            Func<int, int, int> readCorner,
            int spread = -1)
        {
            if (world == null || world.Bases.Count == 0)
            {
                return TerrainPlan.Refuse("No brush world is loaded");
            }

            if (cornerCount <= 0)
            {
                return TerrainPlan.Refuse("The brush is outside the map");
            }

            return Run(world, cellsX, cellsY, cornerX, cornerY, cornerCount - 1, level, readCorner, spread);
        }

        private static TerrainPlan Run(
            TerrainWorld world,
            int cellsX,
            int cellsY,
            int blockX,
            int blockY,
            int blockCells,
            int level,
            Func<int, int, int> readCorner,
            int spread)
        {
            if (readCorner == null || cellsX <= 0 || cellsY <= 0 || level < 0 || level >= world.Bases.Count)
            {
                return TerrainPlan.Refuse("The brush is outside the map");
            }

            var limit = spread < 0 ? world.Bases.Count : spread;
            var latticeX = cellsX + 1;
            var original = new Dictionary<long, int>();
            var current = new Dictionary<long, int>();
            var locked = new HashSet<long>();

            var painted = false;
            for (var y = blockY; y <= blockY + blockCells; y++)
            {
                for (var x = blockX; x <= blockX + blockCells; x++)
                {
                    if (x < 0 || y < 0 || x > cellsX || y > cellsY)
                    {
                        continue;
                    }

                    painted = true;
                    var key = Key(x, y, latticeX);
                    current[key] = level;
                    locked.Add(key);
                }
            }

            if (!painted)
            {
                return TerrainPlan.Refuse("The brush is outside the map");
            }

            var searched = Search(world, level, limit, blockX, blockY, blockCells, latticeX, cellsX, cellsY, original, current, locked, readCorner);
            if (searched != null)
            {
                current = searched;
            }
            else
            {
                Settle(world, level, limit, blockX, blockY, blockCells, latticeX, cellsX, cellsY, original, current, locked, readCorner);
            }

            var invalid = FirstInvalid(world, cellsX, cellsY, latticeX, original, current, readCorner);
            if (invalid != null)
            {
                return TerrainPlan.Refuse(invalid);
            }

            var cells = new List<TerrainCell>();
            for (var y = 0; y < cellsY; y++)
            {
                for (var x = 0; x < cellsX; x++)
                {
                    var inBlock = x >= blockX && x < blockX + blockCells && y >= blockY && y < blockY + blockCells;
                    var corners = new int[TerrainCorners.Count];
                    var changed = false;
                    for (var corner = 0; corner < corners.Length; corner++)
                    {
                        var cornerX = x + (corner % 2);
                        var cornerY = y + (corner / 2);
                        corners[corner] = Level(current, original, cornerX, cornerY, latticeX, readCorner);
                        changed = changed || corners[corner] != Original(original, cornerX, cornerY, latticeX, readCorner);
                    }

                    if (inBlock || changed)
                    {
                        cells.Add(new TerrainCell(x, y, corners[0], corners[1], corners[2], corners[3]));
                    }
                }
            }

            return TerrainPlan.Accept(cells);
        }

        /// <summary>
        /// The greedy repair used when the search runs out of budget: fixes each
        /// broken cell near the stroke in turn, re-checking its neighbours.
        /// </summary>
        private static void Settle(
            TerrainWorld world,
            int level,
            int limit,
            int blockX,
            int blockY,
            int blockCells,
            int latticeX,
            int cellsX,
            int cellsY,
            Dictionary<long, int> original,
            Dictionary<long, int> current,
            HashSet<long> locked,
            Func<int, int, int> readCorner)
        {
            var queue = new Queue<long>();
            var queued = new HashSet<long>();
            for (var y = blockY - 1; y <= blockY + blockCells; y++)
            {
                for (var x = blockX - 1; x <= blockX + blockCells; x++)
                {
                    Enqueue(queue, queued, x, y, cellsX, cellsY);
                }
            }

            var guard = 0;
            var guardLimit = (cellsX + 1) * (cellsY + 1) * world.Bases.Count * 4;
            while (queue.Count > 0 && guard++ < guardLimit)
            {
                var cellKey = queue.Dequeue();
                queued.Remove(cellKey);
                var cellX = (int)(cellKey % latticeX);
                var cellY = (int)(cellKey / latticeX);
                if (Repair(world, level, limit, blockX, blockY, blockCells, cellX, cellY, latticeX, cellsX, cellsY, original, current, locked, readCorner))
                {
                    for (var corner = 0; corner < TerrainCorners.Count; corner++)
                    {
                        var cornerX = cellX + (corner % 2);
                        var cornerY = cellY + (corner / 2);
                        Enqueue(queue, queued, cornerX - 1, cornerY - 1, cellsX, cellsY);
                        Enqueue(queue, queued, cornerX - 1, cornerY, cellsX, cellsY);
                        Enqueue(queue, queued, cornerX, cornerY - 1, cellsX, cellsY);
                        Enqueue(queue, queued, cornerX, cornerY, cellsX, cellsY);
                    }
                }
            }
        }

        private static bool Repair(
            TerrainWorld world,
            int level,
            int limit,
            int blockX,
            int blockY,
            int blockCells,
            int cellX,
            int cellY,
            int latticeX,
            int cellsX,
            int cellsY,
            Dictionary<long, int> original,
            Dictionary<long, int> current,
            HashSet<long> locked,
            Func<int, int, int> readCorner)
        {
            var before = new int[TerrainCorners.Count];
            var cornerX = new int[TerrainCorners.Count];
            var cornerY = new int[TerrainCorners.Count];
            for (var corner = 0; corner < before.Length; corner++)
            {
                cornerX[corner] = cellX + (corner % 2);
                cornerY[corner] = cellY + (corner / 2);
                before[corner] = Level(current, original, cornerX[corner], cornerY[corner], latticeX, readCorner);
            }

            if (Valid(world, before))
            {
                return false;
            }

            int[] best = null;
            var lowestCost = int.MaxValue;
            var smallestMove = int.MaxValue;
            foreach (var trial in Trials(world, level, limit, blockX, blockY, blockCells, cellX, cellY, before, latticeX, current, locked))
            {
                var changes = Changes(trial, before);
                var move = Move(world, level, trial, before);
                var cost = changes + (2 * NeighbourDamage(world, cellX, cellY, trial, before, latticeX, cellsX, cellsY, original, current, readCorner));
                if (cost < lowestCost || (cost == lowestCost && move < smallestMove))
                {
                    lowestCost = cost;
                    smallestMove = move;
                    best = trial;
                }
            }

            if (best == null)
            {
                return false;
            }

            var different = false;
            for (var corner = 0; corner < best.Length; corner++)
            {
                if (best[corner] == before[corner])
                {
                    continue;
                }

                current[Key(cornerX[corner], cornerY[corner], latticeX)] = best[corner];
                different = true;
            }

            return different;
        }

        /// <summary>
        /// Searches for the repair with the fewest corner changes over the whole
        /// stroke, fixing one broken cell at a time in every valid way. Returns
        /// null when the budget runs out first.
        /// </summary>
        private static Dictionary<long, int> Search(
            TerrainWorld world,
            int level,
            int limit,
            int blockX,
            int blockY,
            int blockCells,
            int latticeX,
            int cellsX,
            int cellsY,
            Dictionary<long, int> original,
            Dictionary<long, int> start,
            HashSet<long> locked,
            Func<int, int, int> readCorner)
        {
            var open = new SortedSet<SearchNode>(SearchNode.Comparer);
            var seen = new HashSet<string>();
            var order = 0L;
            var first = Node(world, start, 0, 0, latticeX, cellsX, cellsY, original, readCorner, order++);
            open.Add(first);
            seen.Add(first.Signature);
            while (open.Count > 0 && order < SearchBudget)
            {
                var node = open.Min;
                open.Remove(node);
                if (node.Broken.Count == 0)
                {
                    return node.Current;
                }

                var cellKey = node.Broken[0];
                var cellX = (int)(cellKey % latticeX);
                var cellY = (int)(cellKey / latticeX);
                var before = new int[TerrainCorners.Count];
                for (var corner = 0; corner < before.Length; corner++)
                {
                    before[corner] = Level(node.Current, original, cellX + (corner % 2), cellY + (corner / 2), latticeX, readCorner);
                }

                foreach (var trial in Trials(world, level, limit, blockX, blockY, blockCells, cellX, cellY, before, latticeX, node.Current, locked))
                {
                    var next = new Dictionary<long, int>(node.Current);
                    for (var corner = 0; corner < trial.Length; corner++)
                    {
                        if (trial[corner] != before[corner])
                        {
                            next[Key(cellX + (corner % 2), cellY + (corner / 2), latticeX)] = trial[corner];
                        }
                    }

                    var child = Node(
                        world,
                        next,
                        node.Changes + Changes(trial, before),
                        node.Move + Move(world, level, trial, before),
                        latticeX,
                        cellsX,
                        cellsY,
                        original,
                        readCorner,
                        order++);
                    if (seen.Add(child.Signature))
                    {
                        open.Add(child);
                    }
                }
            }

            return null;
        }

        private static SearchNode Node(
            TerrainWorld world,
            Dictionary<long, int> current,
            int changes,
            int move,
            int latticeX,
            int cellsX,
            int cellsY,
            Dictionary<long, int> original,
            Func<int, int, int> readCorner,
            long order)
        {
            var cells = new SortedSet<long>();
            var signature = new List<long>();
            foreach (var pair in current)
            {
                if (pair.Value == Original(original, (int)(pair.Key % latticeX), (int)(pair.Key / latticeX), latticeX, readCorner))
                {
                    continue;
                }

                signature.Add((pair.Key * 64) + pair.Value);
                var x = (int)(pair.Key % latticeX);
                var y = (int)(pair.Key / latticeX);
                for (var dy = -1; dy <= 0; dy++)
                {
                    for (var dx = -1; dx <= 0; dx++)
                    {
                        if (OnMap(x + dx, y + dy, cellsX, cellsY))
                        {
                            cells.Add(Key(x + dx, y + dy, latticeX));
                        }
                    }
                }
            }

            var broken = new List<long>();
            var corners = new int[TerrainCorners.Count];
            foreach (var cell in cells)
            {
                var cellX = (int)(cell % latticeX);
                var cellY = (int)(cell / latticeX);
                for (var corner = 0; corner < corners.Length; corner++)
                {
                    corners[corner] = Level(current, original, cellX + (corner % 2), cellY + (corner / 2), latticeX, readCorner);
                }

                if (!Valid(world, corners))
                {
                    broken.Add(cell);
                }
            }

            signature.Sort();
            return new SearchNode
            {
                Current = current,
                Changes = changes,
                Move = move,
                Broken = broken,
                Order = order,
                Signature = string.Join(",", signature),
            };
        }

        /// <summary>
        /// Every way of moving a cell's unlocked corners that leaves the cell
        /// with a piece. A corner may move toward the painted base, or sideways
        /// to a base no further from it while it still has its original base.
        /// </summary>
        private static IEnumerable<int[]> Trials(
            TerrainWorld world,
            int level,
            int limit,
            int blockX,
            int blockY,
            int blockCells,
            int cellX,
            int cellY,
            int[] before,
            int latticeX,
            Dictionary<long, int> current,
            HashSet<long> locked)
        {
            var options = new List<int>[TerrainCorners.Count];
            for (var corner = 0; corner < before.Length; corner++)
            {
                options[corner] = new List<int> { before[corner] };
                var cornerX = cellX + (corner % 2);
                var cornerY = cellY + (corner / 2);
                var key = Key(cornerX, cornerY, latticeX);
                if (locked.Contains(key) || Spread(cornerX, cornerY, blockX, blockY, blockCells) > limit)
                {
                    continue;
                }

                var distance = world.Distance(level, before[corner]);
                if (distance >= TerrainWorld.Far)
                {
                    continue;
                }

                var untouched = !current.ContainsKey(key);
                for (var candidate = 0; candidate < world.Bases.Count; candidate++)
                {
                    if (candidate == before[corner])
                    {
                        continue;
                    }

                    var candidateDistance = world.Distance(level, candidate);
                    if (candidateDistance < distance || (untouched && candidateDistance == distance))
                    {
                        options[corner].Add(candidate);
                    }
                }
            }

            foreach (var topLeft in options[0])
            {
                foreach (var topRight in options[1])
                {
                    foreach (var bottomLeft in options[2])
                    {
                        foreach (var bottomRight in options[3])
                        {
                            var trial = new[] { topLeft, topRight, bottomLeft, bottomRight };
                            if (Valid(world, trial))
                            {
                                yield return trial;
                            }
                        }
                    }
                }
            }
        }

        private static int Changes(int[] trial, int[] before)
        {
            var changes = 0;
            for (var corner = 0; corner < trial.Length; corner++)
            {
                if (trial[corner] != before[corner])
                {
                    changes++;
                }
            }

            return changes;
        }

        private static int Move(TerrainWorld world, int level, int[] trial, int[] before)
        {
            var move = 0;
            for (var corner = 0; corner < trial.Length; corner++)
            {
                move += world.Distance(level, before[corner]) - world.Distance(level, trial[corner]);
            }

            return move;
        }

        /// <summary>
        /// How many neighbouring cells a trial repair would break, less the
        /// number it would fix, so a repair that ripples outward loses to one
        /// that stays put.
        /// </summary>
        private static int NeighbourDamage(
            TerrainWorld world,
            int cellX,
            int cellY,
            int[] trial,
            int[] before,
            int latticeX,
            int cellsX,
            int cellsY,
            Dictionary<long, int> original,
            Dictionary<long, int> current,
            Func<int, int, int> readCorner)
        {
            var overrides = new Dictionary<long, int>();
            var neighbours = new HashSet<long>();
            for (var corner = 0; corner < trial.Length; corner++)
            {
                if (trial[corner] == before[corner])
                {
                    continue;
                }

                var cornerX = cellX + (corner % 2);
                var cornerY = cellY + (corner / 2);
                overrides[Key(cornerX, cornerY, latticeX)] = trial[corner];
                for (var dy = -1; dy <= 0; dy++)
                {
                    for (var dx = -1; dx <= 0; dx++)
                    {
                        var x = cornerX + dx;
                        var y = cornerY + dy;
                        if ((x != cellX || y != cellY) && OnMap(x, y, cellsX, cellsY))
                        {
                            neighbours.Add(Key(x, y, latticeX));
                        }
                    }
                }
            }

            var damage = 0;
            var now = new int[TerrainCorners.Count];
            var after = new int[TerrainCorners.Count];
            foreach (var neighbour in neighbours)
            {
                var x = (int)(neighbour % latticeX);
                var y = (int)(neighbour / latticeX);
                for (var corner = 0; corner < now.Length; corner++)
                {
                    var key = Key(x + (corner % 2), y + (corner / 2), latticeX);
                    now[corner] = Level(current, original, x + (corner % 2), y + (corner / 2), latticeX, readCorner);
                    int value;
                    after[corner] = overrides.TryGetValue(key, out value) ? value : now[corner];
                }

                var validNow = Valid(world, now);
                var validAfter = Valid(world, after);
                if (validNow && !validAfter)
                {
                    damage++;
                }
                else if (!validNow && validAfter)
                {
                    damage--;
                }
            }

            return damage;
        }

        private static string FirstInvalid(
            TerrainWorld world,
            int cellsX,
            int cellsY,
            int latticeX,
            Dictionary<long, int> original,
            Dictionary<long, int> current,
            Func<int, int, int> readCorner)
        {
            for (var y = 0; y < cellsY; y++)
            {
                for (var x = 0; x < cellsX; x++)
                {
                    var corners = new int[TerrainCorners.Count];
                    var touched = false;
                    for (var corner = 0; corner < corners.Length; corner++)
                    {
                        var cornerX = x + (corner % 2);
                        var cornerY = y + (corner / 2);
                        corners[corner] = Level(current, original, cornerX, cornerY, latticeX, readCorner);
                        touched = touched || corners[corner] != Original(original, cornerX, cornerY, latticeX, readCorner);
                    }

                    if (touched && !Valid(world, corners))
                    {
                        return Describe(world, corners);
                    }
                }
            }

            return null;
        }

        private static string Describe(TerrainWorld world, int[] corners)
        {
            var names = new List<string>();
            foreach (var corner in corners)
            {
                var name = world.Bases[corner].Name;
                if (!names.Contains(name))
                {
                    names.Add(name);
                }
            }

            return "The tileset has no piece for " + string.Join(", ", names) + " in one cell";
        }

        private static bool Valid(TerrainWorld world, int[] corners)
        {
            return world.HasLayout(corners[0], corners[1], corners[2], corners[3]);
        }

        private static int Spread(int x, int y, int blockX, int blockY, int blockCells)
        {
            var dx = x < blockX ? blockX - x : x > blockX + blockCells ? x - (blockX + blockCells) : 0;
            var dy = y < blockY ? blockY - y : y > blockY + blockCells ? y - (blockY + blockCells) : 0;
            return dx > dy ? dx : dy;
        }

        private static int Level(
            Dictionary<long, int> current,
            Dictionary<long, int> original,
            int x,
            int y,
            int latticeX,
            Func<int, int, int> readCorner)
        {
            int value;
            if (current.TryGetValue(Key(x, y, latticeX), out value))
            {
                return value;
            }

            return Original(original, x, y, latticeX, readCorner);
        }

        private static int Original(
            Dictionary<long, int> original,
            int x,
            int y,
            int latticeX,
            Func<int, int, int> readCorner)
        {
            var key = Key(x, y, latticeX);
            int value;
            if (!original.TryGetValue(key, out value))
            {
                value = readCorner(x, y);
                if (value < 0)
                {
                    value = 0;
                }

                original[key] = value;
            }

            return value;
        }

        private static void Enqueue(Queue<long> queue, HashSet<long> queued, int x, int y, int cellsX, int cellsY)
        {
            if (!OnMap(x, y, cellsX, cellsY))
            {
                return;
            }

            var key = Key(x, y, cellsX + 1);
            if (queued.Add(key))
            {
                queue.Enqueue(key);
            }
        }

        private static bool OnMap(int x, int y, int cellsX, int cellsY)
        {
            return x >= 0 && y >= 0 && x < cellsX && y < cellsY;
        }

        private static long Key(int x, int y, int latticeX)
        {
            return ((long)y * latticeX) + x;
        }

        private sealed class SearchNode
        {
            public static readonly IComparer<SearchNode> Comparer = new NodeComparer();

            public Dictionary<long, int> Current { get; set; }

            public int Changes { get; set; }

            public int Move { get; set; }

            public List<long> Broken { get; set; }

            public long Order { get; set; }

            public string Signature { get; set; }

            /// <summary>
            /// One change fixes at most the four cells around a corner, so this
            /// never overestimates the changes still needed.
            /// </summary>
            public int Estimate => this.Changes + ((this.Broken.Count + 3) / 4);

            private sealed class NodeComparer : IComparer<SearchNode>
            {
                public int Compare(SearchNode left, SearchNode right)
                {
                    var result = left.Estimate.CompareTo(right.Estimate);
                    if (result == 0)
                    {
                        result = left.Broken.Count.CompareTo(right.Broken.Count);
                    }

                    if (result == 0)
                    {
                        result = left.Move.CompareTo(right.Move);
                    }

                    return result != 0 ? result : left.Order.CompareTo(right.Order);
                }
            }
        }
    }
}
