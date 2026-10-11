namespace Mappy.Util
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;

    using Mappy.Data;
    using Mappy.Models;

    public static class ResourceMarkerRegions
    {
        private const int CellSize = 16;

        private const int MinimumRegionCells = 6;

        public static IReadOnlyList<ResourceRegionLabel> Build(IMainModel map)
        {
            if (map == null || map.MarkerLines == null || map.MarkerLines.Count == 0)
            {
                return Array.Empty<ResourceRegionLabel>();
            }

            var width = map.FeatureGridWidth;
            var height = map.FeatureGridHeight;
            if (width <= 0 || height <= 0)
            {
                return Array.Empty<ResourceRegionLabel>();
            }

            var barrier = new bool[width, height];
            foreach (var line in map.MarkerLines)
            {
                var points = line.Points;
                for (var i = 1; i < points.Count; i++)
                {
                    Rasterize(
                        barrier,
                        ToCell(points[i - 1].X, width),
                        ToCell(points[i - 1].Y, height),
                        ToCell(points[i].X, width),
                        ToCell(points[i].Y, height));
                }
            }

            var region = new int[width, height];
            var nextId = 1;
            var queue = new Queue<Point>();
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    if (barrier[x, y] || region[x, y] != 0)
                    {
                        continue;
                    }

                    Flood(region, barrier, width, height, x, y, nextId, queue);
                    nextId++;
                }
            }

            if (nextId == 1)
            {
                return Array.Empty<ResourceRegionLabel>();
            }

            var totals = new RegionTotals[nextId];
            for (var i = 0; i < totals.Length; i++)
            {
                totals[i] = new RegionTotals();
            }

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var id = region[x, y];
                    if (id <= 0)
                    {
                        continue;
                    }

                    totals[id].Count++;
                    totals[id].SumX += x;
                    totals[id].SumY += y;
                }
            }

            foreach (var instance in map.EnumerateFeatureInstances())
            {
                var id = RegionForFeature(region, barrier, width, height, instance.X, instance.Y);
                if (id <= 0)
                {
                    continue;
                }

                AddFeature(totals[id], instance.BaseFeature);
            }

            var labels = new List<ResourceRegionLabel>();
            for (var id = 1; id < totals.Length; id++)
            {
                var total = totals[id];
                if (total.Count < MinimumRegionCells)
                {
                    continue;
                }

                var cx = (int)(total.SumX / total.Count);
                var cy = (int)(total.SumY / total.Count);
                labels.Add(new ResourceRegionLabel(
                    new Point((cx * CellSize) + (CellSize / 2), (cy * CellSize) + (CellSize / 2)),
                    total.PatchMetal,
                    total.WreckMetal,
                    total.Energy));
            }

            return labels;
        }

        private static void AddFeature(RegionTotals total, Feature feature)
        {
            if (feature == null)
            {
                return;
            }

            if (feature.MetalSpotValue > 0)
            {
                total.PatchMetal += MetalSpotDisplayFormat.GetCalculatedValue(feature);
            }

            feature.ReclaimInfo.Do(
                rec =>
                {
                    total.WreckMetal += rec.MetalValue;
                    total.Energy += rec.EnergyValue;
                },
                () => { });
        }

        private static int RegionForFeature(int[,] region, bool[,] barrier, int width, int height, int x, int y)
        {
            if (x < 0 || y < 0 || x >= width || y >= height)
            {
                return 0;
            }

            if (!barrier[x, y] && region[x, y] > 0)
            {
                return region[x, y];
            }

            for (var radius = 1; radius <= 3; radius++)
            {
                for (var dy = -radius; dy <= radius; dy++)
                {
                    for (var dx = -radius; dx <= radius; dx++)
                    {
                        if (Math.Abs(dx) != radius && Math.Abs(dy) != radius)
                        {
                            continue;
                        }

                        var nx = x + dx;
                        var ny = y + dy;
                        if (nx < 0 || ny < 0 || nx >= width || ny >= height)
                        {
                            continue;
                        }

                        if (region[nx, ny] > 0)
                        {
                            return region[nx, ny];
                        }
                    }
                }
            }

            return 0;
        }

        private static void Flood(
            int[,] region,
            bool[,] barrier,
            int width,
            int height,
            int startX,
            int startY,
            int id,
            Queue<Point> queue)
        {
            queue.Clear();
            region[startX, startY] = id;
            queue.Enqueue(new Point(startX, startY));
            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                TryEnqueue(region, barrier, width, height, cell.X - 1, cell.Y, id, queue);
                TryEnqueue(region, barrier, width, height, cell.X + 1, cell.Y, id, queue);
                TryEnqueue(region, barrier, width, height, cell.X, cell.Y - 1, id, queue);
                TryEnqueue(region, barrier, width, height, cell.X, cell.Y + 1, id, queue);
            }
        }

        private static void TryEnqueue(
            int[,] region,
            bool[,] barrier,
            int width,
            int height,
            int x,
            int y,
            int id,
            Queue<Point> queue)
        {
            if (x < 0 || y < 0 || x >= width || y >= height || barrier[x, y] || region[x, y] != 0)
            {
                return;
            }

            region[x, y] = id;
            queue.Enqueue(new Point(x, y));
        }

        private static void Rasterize(bool[,] barrier, int x0, int y0, int x1, int y1)
        {
            var width = barrier.GetLength(0);
            var height = barrier.GetLength(1);
            var dx = Math.Abs(x1 - x0);
            var dy = Math.Abs(y1 - y0);
            var sx = x0 < x1 ? 1 : -1;
            var sy = y0 < y1 ? 1 : -1;
            var err = dx - dy;

            while (true)
            {
                Mark(barrier, x0, y0, width, height);
                if (x0 == x1 && y0 == y1)
                {
                    break;
                }

                var e2 = 2 * err;
                var stepX = e2 > -dy;
                var stepY = e2 < dx;
                if (stepX && stepY)
                {
                    Mark(barrier, x0 + sx, y0, width, height);
                    Mark(barrier, x0, y0 + sy, width, height);
                }

                if (stepX)
                {
                    err -= dy;
                    x0 += sx;
                }

                if (stepY)
                {
                    err += dx;
                    y0 += sy;
                }
            }
        }

        private static void Mark(bool[,] barrier, int x, int y, int width, int height)
        {
            if (x >= 0 && y >= 0 && x < width && y < height)
            {
                barrier[x, y] = true;
            }
        }

        private static int ToCell(int pixel, int count)
        {
            var cell = pixel / CellSize;
            if (cell < 0)
            {
                return 0;
            }

            if (cell >= count)
            {
                return count - 1;
            }

            return cell;
        }

        private sealed class RegionTotals
        {
            public double PatchMetal { get; set; }

            public int WreckMetal { get; set; }

            public int Energy { get; set; }

            public int Count { get; set; }

            public long SumX { get; set; }

            public long SumY { get; set; }
        }
    }
}
