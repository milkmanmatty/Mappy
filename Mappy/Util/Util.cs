namespace Mappy.Util
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.IO;
    using System.Linq;
    using Geometry;
    using Mappy.Collections;
    using Mappy.Data;
    using Mappy.Models;
    using Mappy.Models.Enums;
    using Mappy.Properties;
    using Mappy.Services;
    using Mappy.Util.ImageSampling;
    using TAUtil.Gdi.Palette;

    public static class Util
    {
        public static HashSet<Bitmap> GetUsedTiles(IMapTile tile)
        {
            var set = new HashSet<Bitmap>();
            foreach (var b in tile.TileGrid)
            {
                set.Add(b);
            }

            return set;
        }

        public static Point? ScreenToHeightIndex(IGrid<int> heightmap, Point p)
        {
            var col = p.X / 16;

            if (col < 0 || col >= heightmap.Width)
            {
                return null;
            }

            var ray = new Ray3D(new Vector3D(p.X, p.Y + 128, 255.0), new Vector3D(0.0, -0.5, -1.0));

            for (var row = heightmap.Height - 1; row >= 0; row--)
            {
                var height = heightmap.Get(col, row);
                var rect = new AxisRectangle3D(
                    new Vector3D((col * 16) + 0.5, (row * 16) + 0.5, height),
                    16.0,
                    16.0);
                if (rect.Intersect(ray, out _))
                {
                    return new Point(col, row);
                }
            }

            return null;
        }

        public static Point? ScreenToNearestHeightPointIndex(IGrid<int> heightmap, Point p)
        {
            var cell = ScreenToHeightIndex(heightmap, p);
            if (!cell.HasValue)
            {
                return null;
            }

            Point? best = null;
            var bestDist = int.MaxValue;
            const int SearchRadius = 2;
            for (var y = cell.Value.Y - SearchRadius; y <= cell.Value.Y + SearchRadius + 1; y++)
            {
                for (var x = cell.Value.X - SearchRadius; x <= cell.Value.X + SearchRadius + 1; x++)
                {
                    if (x < 0 || y < 0 || x >= heightmap.Width || y >= heightmap.Height)
                    {
                        continue;
                    }

                    var projected = new Point(
                        x * 16,
                        (y * 16) - (heightmap.Get(x, y) / 2));

                    var dx = projected.X - p.X;
                    var dy = projected.Y - p.Y;
                    var dist = (dx * dx) + (dy * dy);
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        best = new Point(x, y);
                    }
                }
            }

            return best;
        }

        public static double Mod(double a, double p)
        {
            return ((a % p) + p) % p;
        }

        public static int Mod(int a, int p)
        {
            return ((a % p) + p) % p;
        }

        public static int Clamp(int val, int min, int max)
        {
            return Math.Min(max, Math.Max(min, val));
        }

        public static float Clamp(float val, float min, float max)
        {
            return Math.Min(max, Math.Max(min, val));
        }

        public static double Clamp(double val, double min, double max)
        {
            return Math.Min(max, Math.Max(min, val));
        }

        public static Bitmap GetStartImage(int index)
        {
            switch (index)
            {
                case 1:
                    return Resources.number_1;
                case 2:
                    return Resources.number_2;
                case 3:
                    return Resources.number_3;
                case 4:
                    return Resources.number_4;
                case 5:
                    return Resources.number_5;
                case 6:
                    return Resources.number_6;
                case 7:
                    return Resources.number_7;
                case 8:
                    return Resources.number_8;
                case 9:
                    return Resources.number_9;
                case 10:
                    return Resources.number_10;
                default:
                    throw new ArgumentException("invalid index: " + index);
            }
        }

        public static IDictionary<T, int> ReverseMapping<T>(T[] array)
        {
            var mapping = new Dictionary<T, int>();
            for (var i = 0; i < array.Length; i++)
            {
                mapping[array[i]] = i;
            }

            return mapping;
        }

        public static BackgroundWorker RenderMinimapWorker()
        {
            var worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.WorkerSupportsCancellation = true;
            worker.DoWork += (sender, args) =>
                {
                    var w = (BackgroundWorker)sender;
                    RenderHighQualityMinimap(w, args);
                };
            return worker;
        }

        public static BackgroundWorker RenderEnhancedColoursMinimapWorker()
        {
            var worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.WorkerSupportsCancellation = true;
            worker.DoWork += (sender, args) =>
                {
                    var w = (BackgroundWorker)sender;
                    RenderEnhancedColoursMinimap(w, args);
                };
            return worker;
        }

        public static IEnumerable<TU> Choose<T, TU>(this IEnumerable<T> coll, Func<T, Maybe<TU>> f)
        {
            return coll.SelectMany(item => f(item).Match(x => new[] { x }, () => new TU[] { }));
        }

        public static IEnumerable<Color> EnumerateBigMinimapImage(RenderMinimapArgs args)
        {
            var featuresList = BuildSortedMinimapFeatures(args);
            var tileGrid = args.MapModel.Tile.TileGrid;
            var mapWidth = (tileGrid.Width * 32) - 32;
            var mapHeight = (tileGrid.Height * 32) - 128;
            var rowBuffer = new int[Math.Max(0, mapWidth)];

            var tileCache = new Dictionary<Bitmap, BitmapData>();
            try
            {
                var inProgressFeatures = new List<FeatureInfo>();
                var nextInProgressFeatures = new List<FeatureInfo>();
                var currentFeatureIndex = 0;

                for (var sourcePixelY = 0; sourcePixelY < mapHeight; ++sourcePixelY)
                {
                    CompositeMinimapRow(
                        tileGrid,
                        tileCache,
                        featuresList,
                        inProgressFeatures,
                        nextInProgressFeatures,
                        ref currentFeatureIndex,
                        sourcePixelY,
                        mapWidth,
                        mapHeight,
                        rowBuffer);

                    var swap = inProgressFeatures;
                    inProgressFeatures = nextInProgressFeatures;
                    nextInProgressFeatures = swap;
                    nextInProgressFeatures.Clear();

                    for (var x = 0; x < mapWidth; ++x)
                    {
                        yield return Color.FromArgb(rowBuffer[x]);
                    }
                }
            }
            finally
            {
                UnlockBitmapCache(tileCache);
            }
        }

        public static void RenderHighQualityMinimap(BackgroundWorker w, DoWorkEventArgs workArgs)
        {
            RenderMinimap(w, workArgs, false);
        }

        public static void RenderEnhancedColoursMinimap(BackgroundWorker w, DoWorkEventArgs workArgs)
        {
            RenderMinimap(w, workArgs, true);
        }

        public static Bitmap ToBitmap(IPixelImage map)
        {
            var b = new Bitmap(map.Width, map.Height);

            for (var y = 0; y < map.Height; y++)
            {
                for (var x = 0; x < map.Width; x++)
                {
                    b.SetPixel(x, y, map.GetPixel(x, y));
                }
            }

            return b;
        }

        public static Bitmap GenerateMinimap(IPixelImage map)
        {
            int width, height;

            if (map.Width > map.Height)
            {
                width = 252;
                height = (int)Math.Round(252 * (map.Height / (float)map.Width));
            }
            else
            {
                height = 252;
                width = (int)Math.Round(252 * (map.Width / (float)map.Height));
            }

            var wrapper = new NearestNeighbourWrapper(map, width, height);
            return ToBitmap(wrapper);
        }

        public static bool WriteMapImage(Stream s, IGrid<Bitmap> map, Action<int> reportProgress, Func<bool> shouldCancel)
        {
            return WriteMapImage(s, map, null, null, null, reportProgress, shouldCancel);
        }

        public static bool WriteMapImage(
            Stream s,
            IGrid<Bitmap> map,
            IList<Positioned<IMapTile>> floatingTiles,
            IList<FeatureOverlay> featureOverlays,
            IList<UnitOverlay> unitOverlays,
            Action<int> reportProgress,
            Func<bool> shouldCancel)
        {
            // Exclude 1 tile on the right, and 4 tiles at the bottom.
            var width = Math.Max(1, (map.Width * 32) - 32);
            var height = Math.Max(1, (map.Height * 32) - 128);
            var totalTiles = map.Width * map.Height;

            using (var full = new Bitmap(width, height, PixelFormat.Format32bppArgb))
            {
                var targetData = full.LockBits(
                    new Rectangle(0, 0, width, height),
                    ImageLockMode.WriteOnly,
                    PixelFormat.Format32bppArgb);

                try
                {
                    var tilesProcessed = 0;
                    for (var tileY = 0; tileY < map.Height; tileY++)
                    {
                        for (var tileX = 0; tileX < map.Width; tileX++)
                        {
                            if (shouldCancel())
                            {
                                return false;
                            }

                            var destX = tileX * 32;
                            var destY = tileY * 32;
                            if (destX >= width || destY >= height)
                            {
                                tilesProcessed++;
                                reportProgress((tilesProcessed * 100) / totalTiles);
                                continue;
                            }

                            var copyWidth = Math.Min(32, width - destX);
                            var copyHeight = Math.Min(32, height - destY);

                            var tile = map.Get(tileX, tileY);
                            var tileData = tile.LockBits(
                                new Rectangle(0, 0, 32, 32),
                                ImageLockMode.ReadOnly,
                                PixelFormat.Format32bppArgb);

                            try
                            {
                                unsafe
                                {
                                    var src = (byte*)tileData.Scan0;
                                    var dst = (byte*)targetData.Scan0
                                        + (targetData.Stride * destY)
                                        + (destX * 4);
                                    for (var row = 0; row < copyHeight; row++)
                                    {
                                        Buffer.MemoryCopy(
                                            src + (row * tileData.Stride),
                                            dst + (row * targetData.Stride),
                                            copyWidth * 4,
                                            copyWidth * 4);
                                    }
                                }
                            }
                            finally
                            {
                                tile.UnlockBits(tileData);
                            }

                            tilesProcessed++;
                            reportProgress((tilesProcessed * 100) / totalTiles);
                        }
                    }
                }
                finally
                {
                    full.UnlockBits(targetData);
                }

                using (var g = Graphics.FromImage(full))
                {
                    if (floatingTiles != null)
                    {
                        foreach (var ft in floatingTiles)
                        {
                            var tileGrid = ft.Item.TileGrid;
                            for (var ty = 0; ty < tileGrid.Height; ty++)
                            {
                                for (var tx = 0; tx < tileGrid.Width; tx++)
                                {
                                    var px = (ft.Location.X + tx) * 32;
                                    var py = (ft.Location.Y + ty) * 32;
                                    g.DrawImageUnscaled(tileGrid.Get(tx, ty), px, py);
                                }
                            }
                        }
                    }

                    if (featureOverlays != null)
                    {
                        foreach (var fo in featureOverlays)
                        {
                            g.DrawImageUnscaled(fo.Image, fo.DrawBounds.Location);
                        }
                    }

                    if (unitOverlays != null)
                    {
                        foreach (var uo in unitOverlays)
                        {
                            g.DrawImageUnscaled(uo.Bitmap, uo.X, uo.Y);
                        }
                    }
                }

                full.Save(s, ImageFormat.Png);
            }

            return true;
        }

        public struct FeatureOverlay
        {
            public Bitmap Image { get; set; }

            public Rectangle DrawBounds { get; set; }
        }

        public struct UnitOverlay
        {
            public Bitmap Bitmap { get; set; }

            public int X { get; set; }

            public int Y { get; set; }
        }

        public static GUITab MapTabNameToGUIType(string tabName)
        {
            switch (tabName)
            {
                case "sectionsTab":
                    return GUITab.Sections;
                case "featuresTab":
                    return GUITab.Features;
                case "attributesTab":
                    return GUITab.Attributes;
                case "startPositionsTab":
                    return GUITab.Starts;
                case "heightTab":
                    return GUITab.Height;
                case "voidTab":
                    return GUITab.Void;
                case "missionTab":
                case "otaMissionTab":
                    return GUITab.Mission;
                default:
                    return GUITab.Other;
            }
        }

        public static TValue GetOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, TValue defaultValue)
        {
            TValue result;
            if (!dict.TryGetValue(key, out result))
            {
                return defaultValue;
            }

            return result;
        }

        public static OffsetBitmap RenderWireframe(IEnumerable<Line3D> edges)
        {
            var projectedLines = edges.Select(ProjectLine).ToList();
            var boundingBox = ComputeBoundingBox(projectedLines);

            var w = (int)Math.Ceiling(boundingBox.Width) + 1;
            var h = (int)Math.Ceiling(boundingBox.Height) + 1;

            var b = new Bitmap(w, h, PixelFormat.Format32bppArgb);

            var offset = boundingBox.MinXY;

            using (var g = Graphics.FromImage(b))
            {
                g.Clear(Color.Transparent);
                g.TranslateTransform((float)(-offset.X), (float)(-offset.Y));

                foreach (var l in projectedLines)
                {
                    g.DrawLine(
                        Pens.Magenta,
                        (float)l.Start.X,
                        (float)l.Start.Y,
                        (float)l.End.X,
                        (float)l.End.Y);
                }
            }

            return new OffsetBitmap(
                (int)Math.Round(offset.X),
                (int)Math.Round(offset.Y),
                b);
        }

        public static int ComputeMidpointHeight(IGrid<int> grid, int x, int y)
        {
            var topLeft = grid.Get(x, y);
            var topRight = grid.Get(x + 1, y);
            var bottomLeft = grid.Get(x, y + 1);
            var bottomRight = grid.Get(x + 1, y + 1);

            return (topLeft + topRight + bottomLeft + bottomRight) / 4;
        }

        public static Bitmap BitmapFromFile(string filename)
        {
            // It was discovered that when loading 8bpp PNG images
            // via Image.FromStream, we crash with an OOM exception
            // when trying to display them.
            // Further to that, when we try to quantize Bitmap instances
            // that use the 8bpp format internally, using say Quantization.ToTAPalette,
            // these images can end up being made darker even if our code
            // does not actually change the pixel values.
            //
            // These appear to be .NET Framework or GDI+ bugs.
            //
            // Passing the Bitmap straight into another Bitmap constructor
            // causes it to get converted to the 32 bit ARGB format
            // which does not have these issues.
            using (var bmp = new Bitmap(filename))
            {
                return new Bitmap(bmp);
            }
        }

        public static IEnumerable<Color3f> Resize(IEnumerable<Color3f> input, int width, int height, int newWidth, int newHeight)
        {
            using (var enumerator = input.GetEnumerator())
            {
                var nBuffer = new int[newWidth];
                var rowBuffer = new Color3f[newWidth];
                var currentResizedY = 0;

                for (var y = 0; y < height; ++y)
                {
                    var resizedY = (int)(y * (newHeight / (float)height));
                    if (resizedY != currentResizedY)
                    {
                        foreach (var c in rowBuffer)
                        {
                            yield return c;
                        }

                        Array.Clear(rowBuffer, 0, nBuffer.Length);
                        Array.Clear(nBuffer, 0, nBuffer.Length);
                        currentResizedY = resizedY;
                    }

                    for (var x = 0; x < width; ++x)
                    {
                        var resizedX = (int)(x * (newWidth / (float)width));
                        if (!enumerator.MoveNext())
                        {
                            throw new Exception("Enumerator contained too few elements");
                        }

                        rowBuffer[resizedX] = CombineAverage(rowBuffer[resizedX], enumerator.Current, ++nBuffer[resizedX]);
                    }
                }

                foreach (var c in rowBuffer)
                {
                    yield return c;
                }
            }
        }

        private static void RenderMinimap(
            BackgroundWorker w,
            DoWorkEventArgs workArgs,
            bool enhancedColours)
        {
            var args = (RenderMinimapArgs)workArgs.Argument;

            var tileGrid = args.MapModel.Tile.TileGrid;
            var mapWidth = (tileGrid.Width * 32) - 32;
            var mapHeight = (tileGrid.Height * 32) - 128;

            int width, height;

            if (mapWidth > mapHeight)
            {
                width = 252;
                height = (int)Math.Round(252 * (mapHeight / (float)mapWidth));
            }
            else
            {
                height = 252;
                width = (int)Math.Round(252 * (mapWidth / (float)mapHeight));
            }

            var minimapBitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            if (!CompositeAndResizeMinimap(
                args,
                minimapBitmap,
                mapWidth,
                mapHeight,
                width,
                height,
                () => w.CancellationPending,
                x => w.ReportProgress(enhancedColours ? (x * 75) / 100 : x)))
            {
                minimapBitmap.Dispose();
                workArgs.Cancel = true;
                return;
            }

            if (enhancedColours)
            {
                var paletteSources = GetUsedTiles(args.MapModel.Tile)
                    .Concat(
                        args.MapModel.EnumerateFeatureInstances()
                            .Choose(f => args.FeatureService.TryGetFeature(f.FeatureName))
                            .Where(x => x.Permanent)
                            .Select(x => x.Image));

                var completed = ErrorDiffusionPaletteQuantizer.ToPalette(
                    minimapBitmap,
                    PaletteFactory.TAPalette,
                    paletteSources,
                    () => w.CancellationPending,
                    x => w.ReportProgress(75 + ((x * 25) / 100)));
                if (!completed)
                {
                    minimapBitmap.Dispose();
                    workArgs.Cancel = true;
                    return;
                }
            }
            else
            {
                Quantization.ToTAPalette(minimapBitmap);
            }

            workArgs.Result = minimapBitmap;
        }

        private static bool CompositeAndResizeMinimap(
            RenderMinimapArgs args,
            Bitmap dest,
            int mapWidth,
            int mapHeight,
            int destWidth,
            int destHeight,
            Func<bool> shouldCancel,
            Action<int> reportProgress)
        {
            var featuresList = BuildSortedMinimapFeatures(args);
            var tileGrid = args.MapModel.Tile.TileGrid;
            var rowBuffer = new int[Math.Max(0, mapWidth)];
            var destXMap = new int[Math.Max(0, mapWidth)];
            var scaleX = destWidth / (float)mapWidth;
            for (var x = 0; x < mapWidth; ++x)
            {
                destXMap[x] = (int)(x * scaleX);
            }

            var sumR = new int[destWidth];
            var sumG = new int[destWidth];
            var sumB = new int[destWidth];
            var counts = new int[destWidth];
            var currentResizedY = 0;
            var destPixelsWritten = 0;
            var lastProgress = -1;
            var totalDestPixels = destWidth * destHeight;
            var scaleY = destHeight / (float)mapHeight;

            var tileCache = new Dictionary<Bitmap, BitmapData>();
            var destData = dest.LockBits(
                new Rectangle(0, 0, dest.Width, dest.Height),
                ImageLockMode.WriteOnly,
                PixelFormat.Format32bppArgb);

            try
            {
                var inProgressFeatures = new List<FeatureInfo>();
                var nextInProgressFeatures = new List<FeatureInfo>();
                var currentFeatureIndex = 0;

                unsafe
                {
                    var destPtr = (byte*)destData.Scan0;

                    for (var sourcePixelY = 0; sourcePixelY < mapHeight; ++sourcePixelY)
                    {
                        if (shouldCancel())
                        {
                            return false;
                        }

                        var resizedY = (int)(sourcePixelY * scaleY);
                        if (resizedY != currentResizedY)
                        {
                            WriteMinimapDestRow(
                                destPtr,
                                destData.Stride,
                                destPixelsWritten,
                                sumR,
                                sumG,
                                sumB,
                                counts,
                                destWidth);
                            destPixelsWritten += destWidth;
                            ReportMinimapProgress(
                                destPixelsWritten,
                                totalDestPixels,
                                ref lastProgress,
                                reportProgress);

                            Array.Clear(sumR, 0, destWidth);
                            Array.Clear(sumG, 0, destWidth);
                            Array.Clear(sumB, 0, destWidth);
                            Array.Clear(counts, 0, destWidth);
                            currentResizedY = resizedY;
                        }

                        CompositeMinimapRow(
                            tileGrid,
                            tileCache,
                            featuresList,
                            inProgressFeatures,
                            nextInProgressFeatures,
                            ref currentFeatureIndex,
                            sourcePixelY,
                            mapWidth,
                            mapHeight,
                            rowBuffer);

                        var swap = inProgressFeatures;
                        inProgressFeatures = nextInProgressFeatures;
                        nextInProgressFeatures = swap;
                        nextInProgressFeatures.Clear();

                        for (var x = 0; x < mapWidth; ++x)
                        {
                            var argb = rowBuffer[x];
                            var dx = destXMap[x];
                            sumR[dx] += (argb >> 16) & 0xFF;
                            sumG[dx] += (argb >> 8) & 0xFF;
                            sumB[dx] += argb & 0xFF;
                            counts[dx]++;
                        }
                    }

                    if (mapHeight > 0 && destWidth > 0)
                    {
                        WriteMinimapDestRow(
                            destPtr,
                            destData.Stride,
                            destPixelsWritten,
                            sumR,
                            sumG,
                            sumB,
                            counts,
                            destWidth);
                        destPixelsWritten += destWidth;
                        ReportMinimapProgress(
                            destPixelsWritten,
                            totalDestPixels,
                            ref lastProgress,
                            reportProgress);
                    }
                }
            }
            finally
            {
                dest.UnlockBits(destData);
                UnlockBitmapCache(tileCache);
            }

            return true;
        }

        private static List<FeatureInfo> BuildSortedMinimapFeatures(RenderMinimapArgs args)
        {
            var featuresList = args.MapModel.EnumerateFeatureInstances()
                .Choose(f =>
                    args.FeatureService.TryGetFeature(f.FeatureName)
                        .Where(rec => rec.Permanent)
                        .Select(rec => new FeatureInfo
                        {
                            Image = rec.Image,
                            Location = rec.GetDrawBounds(args.MapModel.Tile.HeightGrid, f.X, f.Y).Location,
                        }))
                .ToList();
            featuresList.Sort((a, b) =>
            {
                if (a.Location.Y - b.Location.Y != 0)
                {
                    return a.Location.Y - b.Location.Y;
                }

                return a.Location.X - b.Location.X;
            });

            return featuresList;
        }

        private static void CompositeMinimapRow(
            IGrid<Bitmap> tileGrid,
            Dictionary<Bitmap, BitmapData> tileCache,
            List<FeatureInfo> featuresList,
            List<FeatureInfo> inProgressFeatures,
            List<FeatureInfo> nextInProgressFeatures,
            ref int currentFeatureIndex,
            int sourcePixelY,
            int mapWidth,
            int mapHeight,
            int[] rowBuffer)
        {
            var tileY = sourcePixelY / 32;
            for (var tileX = 0; tileX < tileGrid.Width - 1; ++tileX)
            {
                var tile = tileGrid.Get(tileX, tileY);
                var tileData = GetOrLockBits(tileCache, tile);
                CopyOpaqueRow(tileData, sourcePixelY % 32, rowBuffer, tileX * 32);
            }

            var inProgressFeatureIndex = 0;
            while (true)
            {
                FeatureInfo nextFeature;
                if (currentFeatureIndex < featuresList.Count && inProgressFeatureIndex < inProgressFeatures.Count && featuresList[currentFeatureIndex].Location.Y <= sourcePixelY)
                {
                    nextFeature = featuresList[currentFeatureIndex].Location.X < inProgressFeatures[inProgressFeatureIndex].Location.X
                        ? featuresList[currentFeatureIndex++]
                        : inProgressFeatures[inProgressFeatureIndex++];
                }
                else if (currentFeatureIndex < featuresList.Count && featuresList[currentFeatureIndex].Location.Y <= sourcePixelY)
                {
                    nextFeature = featuresList[currentFeatureIndex++];
                }
                else if (inProgressFeatureIndex < inProgressFeatures.Count)
                {
                    nextFeature = inProgressFeatures[inProgressFeatureIndex++];
                }
                else
                {
                    break;
                }

                var featureImageData = GetOrLockBits(tileCache, nextFeature.Image);
                var imageRect = new Rectangle(nextFeature.Location, nextFeature.Image.Size);

                var coveringRect = new Rectangle(0, 0, mapWidth, mapHeight);
                coveringRect.Intersect(imageRect);

                if (coveringRect.Width > 0)
                {
                    CopyRowSkipTransparent(
                        featureImageData,
                        sourcePixelY - imageRect.Y,
                        coveringRect.X - imageRect.X,
                        coveringRect.Width,
                        rowBuffer,
                        coveringRect.X);
                }

                if (sourcePixelY + 1 < imageRect.Y + imageRect.Height)
                {
                    nextInProgressFeatures.Add(nextFeature);
                }
            }
        }

        private static BitmapData GetOrLockBits(Dictionary<Bitmap, BitmapData> cache, Bitmap bitmap)
        {
            if (!cache.TryGetValue(bitmap, out var data))
            {
                data = bitmap.LockBits(
                    new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                    ImageLockMode.ReadOnly,
                    PixelFormat.Format32bppArgb);
                cache[bitmap] = data;
            }

            return data;
        }

        private static void UnlockBitmapCache(Dictionary<Bitmap, BitmapData> cache)
        {
            foreach (var entry in cache)
            {
                entry.Key.UnlockBits(entry.Value);
            }
        }

        private static unsafe void WriteMinimapDestRow(
            byte* destPtr,
            int stride,
            int destPixelsWritten,
            int[] sumR,
            int[] sumG,
            int[] sumB,
            int[] counts,
            int destWidth)
        {
            var destRow = destPixelsWritten / destWidth;
            var destOffset = destPixelsWritten % destWidth;
            var row = (int*)(destPtr + (destRow * stride));
            for (var x = 0; x < destWidth; ++x)
            {
                var n = counts[x];
                var r = 0;
                var g = 0;
                var b = 0;
                if (n > 0)
                {
                    r = sumR[x] / n;
                    g = sumG[x] / n;
                    b = sumB[x] / n;
                }

                row[destOffset + x] = unchecked((int)0xFF000000) | (r << 16) | (g << 8) | b;
            }
        }

        private static void ReportMinimapProgress(
            int destPixelsWritten,
            int totalDestPixels,
            ref int lastProgress,
            Action<int> reportProgress)
        {
            if (totalDestPixels <= 0)
            {
                return;
            }

            var progress = (destPixelsWritten * 100) / totalDestPixels;
            if (progress > lastProgress)
            {
                reportProgress(progress);
                lastProgress = progress;
            }
        }

        private static unsafe void CopyOpaqueRow(BitmapData data, int rowNumber, int[] output, int startIndex)
        {
            var src = (byte*)data.Scan0 + (rowNumber * data.Stride);
            var byteCount = data.Width * 4;
            fixed (int* dest = &output[startIndex])
            {
                Buffer.MemoryCopy(src, dest, byteCount, byteCount);
            }
        }

        private static unsafe void CopyRowSkipTransparent(
            BitmapData data,
            int rowNumber,
            int startX,
            int length,
            int[] output,
            int startIndex)
        {
            var src = (int*)((byte*)data.Scan0 + (rowNumber * data.Stride) + (startX * 4));
            for (var i = 0; i < length; ++i)
            {
                var argb = src[i];
                if ((uint)argb >= 0x01000000)
                {
                    output[startIndex + i] = argb;
                }
            }
        }

        private static Color3f CombineAverage(Color3f acc, Color3f val, int n)
        {
            return new Color3f
            {
                R = acc.R + ((val.R - acc.R) / n),
                G = acc.G + ((val.G - acc.G) / n),
                B = acc.B + ((val.B - acc.B) / n),
            };
        }

        private static Rectangle2D ComputeBoundingBox(IEnumerable<Line2D> lines)
        {
            return ComputeBoundingBox(ToPoints(lines));
        }

        private static IEnumerable<Vector2D> ToPoints(IEnumerable<Line2D> lines)
        {
            foreach (var l in lines)
            {
                yield return l.Start;
                yield return l.End;
            }
        }

        private static Rectangle2D ComputeBoundingBox(IEnumerable<Vector2D> points)
        {
            var minX = double.PositiveInfinity;
            var maxX = double.NegativeInfinity;
            var minY = double.PositiveInfinity;
            var maxY = double.NegativeInfinity;

            foreach (var p in points)
            {
                if (p.X < minX)
                {
                    minX = p.X;
                }

                if (p.X > maxX)
                {
                    maxX = p.X;
                }

                if (p.Y < minY)
                {
                    minY = p.Y;
                }

                if (p.Y > maxY)
                {
                    maxY = p.Y;
                }
            }

            return Rectangle2D.FromMinMax(minX, minY, maxX, maxY);
        }

        private static Vector2D ProjectPoint(Vector3D point)
        {
            return ProjectThreeDoVertex(point);
        }

        public static Vector2D ProjectThreeDoVertex(Vector3D point)
        {
            point /= Math.Pow(2, 16);

            point.X *= -1;

            return new Vector2D(
                point.X,
                point.Z - (point.Y / 2.0));
        }

        private static Line2D ProjectLine(Line3D line)
        {
            return new Line2D(
                ProjectPoint(line.Start),
                ProjectPoint(line.End));
        }

        public struct RenderMinimapArgs
        {
            public IReadOnlyMapModel MapModel { get; set; }

            public FeatureService FeatureService { get; set; }
        }

        public struct FeatureInfo
        {
            public Bitmap Image { get; set; }

            public Point Location { get; set; }
        }

        public struct Color3f
        {
            public static Color3f Zero = new Color3f { R = 0.0f, G = 0.0f, B = 0.0f };

            public float R { get; set; }

            public float G { get; set; }

            public float B { get; set; }
        }
    }
}
