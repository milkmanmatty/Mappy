namespace Mappy.Util.ImageSampling
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.Linq;

    using TAUtil.Gdi.Palette;

    /// <summary>
    /// Quantizes an image using perceptual palette matching and error diffusion.
    /// </summary>
    public static class ErrorDiffusionPaletteQuantizer
    {
        private static readonly double[] SrgbToLinear = BuildSrgbToLinearTable();

        /// <summary>
        /// Quantizes a bitmap in place.
        /// </summary>
        /// <param name="bitmap">The bitmap to quantize.</param>
        /// <param name="palette">The destination palette.</param>
        /// <param name="sourceImages">
        /// Source images whose used palette colours are valid quantization targets.
        /// If none of their pixels exactly matches the palette, the full palette is used.
        /// </param>
        public static void ToPalette(Bitmap bitmap, IPalette palette, IEnumerable<Bitmap> sourceImages)
        {
            ToPalette(bitmap, palette, sourceImages, () => false, _ => { });
        }

        /// <summary>
        /// Quantizes a bitmap in place with cancellation and progress reporting.
        /// </summary>
        /// <param name="bitmap">The bitmap to quantize.</param>
        /// <param name="palette">The destination palette.</param>
        /// <param name="sourceImages">
        /// Source images whose used palette colours are valid quantization targets.
        /// If none of their pixels exactly matches the palette, the full palette is used.
        /// </param>
        /// <param name="shouldCancel">Returns true when processing should stop.</param>
        /// <param name="reportProgress">Reports progress from zero to one hundred.</param>
        /// <returns>True if quantization completed, or false if it was cancelled.</returns>
        public static bool ToPalette(
            Bitmap bitmap,
            IPalette palette,
            IEnumerable<Bitmap> sourceImages,
            Func<bool> shouldCancel,
            Action<int> reportProgress)
        {
            if (bitmap == null)
            {
                throw new ArgumentNullException(nameof(bitmap));
            }

            if (palette == null)
            {
                throw new ArgumentNullException(nameof(palette));
            }

            if (sourceImages == null)
            {
                throw new ArgumentNullException(nameof(sourceImages));
            }

            if (shouldCancel == null)
            {
                throw new ArgumentNullException(nameof(shouldCancel));
            }

            if (reportProgress == null)
            {
                throw new ArgumentNullException(nameof(reportProgress));
            }

            if (palette.Count == 0)
            {
                throw new ArgumentException("The palette must contain at least one colour.", nameof(palette));
            }

            reportProgress(0);

            var sources = sourceImages.Where(x => x != null).Distinct().ToList();
            var subset = BuildPaletteSubset(palette, sources, shouldCancel, reportProgress);
            if (subset == null || shouldCancel())
            {
                return false;
            }

            var rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            var data = bitmap.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

            try
            {
                var currentErrors = new int[(bitmap.Width + 2) * 3];
                var nextErrors = new int[(bitmap.Width + 2) * 3];
                var scanDirection = 1;
                var lastProgress = 20;

                unsafe
                {
                    for (var y = 0; y < bitmap.Height; y++)
                    {
                        if (shouldCancel())
                        {
                            return false;
                        }

                        var row = (int*)((byte*)data.Scan0 + (y * data.Stride));
                        var firstX = scanDirection > 0 ? 0 : bitmap.Width - 1;

                        for (var step = 0; step < bitmap.Width; step++)
                        {
                            var x = firstX + (scanDirection * step);
                            var here = (x + 1) * 3;
                            var forward = (x + scanDirection + 1) * 3;
                            var backward = (x - scanDirection + 1) * 3;
                            var source = Color.FromArgb(row[x]);

                            var red = ClampToByte(source.R + RoundError(currentErrors[here]));
                            var green = ClampToByte(source.G + RoundError(currentErrors[here + 1]));
                            var blue = ClampToByte(source.B + RoundError(currentErrors[here + 2]));

                            var nearest = FindNearest(subset, red, green, blue);
                            row[x] = nearest.Color.ToArgb();

                            var redError = red - nearest.Color.R;
                            var greenError = green - nearest.Color.G;
                            var blueError = blue - nearest.Color.B;

                            AddError(currentErrors, forward, redError, greenError, blueError, 7);
                            AddError(nextErrors, backward, redError, greenError, blueError, 3);
                            AddError(nextErrors, here, redError, greenError, blueError, 5);
                            AddError(nextErrors, forward, redError, greenError, blueError, 1);
                        }

                        var swap = currentErrors;
                        currentErrors = nextErrors;
                        nextErrors = swap;
                        Array.Clear(nextErrors, 0, nextErrors.Length);
                        scanDirection = -scanDirection;

                        var progress = 20 + (((y + 1) * 80) / bitmap.Height);
                        if (progress > lastProgress)
                        {
                            reportProgress(progress);
                            lastProgress = progress;
                        }
                    }
                }
            }
            finally
            {
                bitmap.UnlockBits(data);
            }

            return true;
        }

        private static void AddError(
            int[] errors,
            int offset,
            int red,
            int green,
            int blue,
            int weight)
        {
            errors[offset] += red * weight;
            errors[offset + 1] += green * weight;
            errors[offset + 2] += blue * weight;
        }

        private static PaletteEntry[] BuildPaletteSubset(
            IPalette palette,
            IList<Bitmap> sourceImages,
            Func<bool> shouldCancel,
            Action<int> reportProgress)
        {
            var paletteArgb = new Dictionary<int, List<int>>();
            for (var i = 0; i < palette.Count; i++)
            {
                var argb = palette[i].ToArgb();
                if (!paletteArgb.TryGetValue(argb, out var indices))
                {
                    indices = new List<int>();
                    paletteArgb.Add(argb, indices);
                }

                indices.Add(i);
            }

            var used = new bool[palette.Count];
            var lastProgress = 0;
            var totalPixels = sourceImages.Sum(x => (long)x.Width * x.Height);
            long processedPixels = 0;
            for (var i = 0; i < sourceImages.Count; i++)
            {
                if (shouldCancel()
                    || !MarkUsedPaletteEntries(
                        sourceImages[i],
                        paletteArgb,
                        used,
                        shouldCancel,
                        processed =>
                        {
                            processedPixels += processed;
                            var progress = totalPixels == 0
                                ? 20
                                : (int)((processedPixels * 20) / totalPixels);
                            if (progress > lastProgress)
                            {
                                reportProgress(progress);
                                lastProgress = progress;
                            }
                        }))
                {
                    return null;
                }
            }

            if (sourceImages.Count == 0)
            {
                reportProgress(20);
            }

            if (!used.Any(x => x))
            {
                for (var i = 0; i < used.Length; i++)
                {
                    used[i] = true;
                }
            }

            var result = new List<PaletteEntry>();
            for (var i = 0; i < palette.Count; i++)
            {
                if (used[i])
                {
                    result.Add(new PaletteEntry(palette[i]));
                }
            }

            return result.ToArray();
        }

        private static double[] BuildSrgbToLinearTable()
        {
            var table = new double[256];
            for (var i = 0; i < table.Length; i++)
            {
                var component = i / 255.0;
                table[i] = component <= 0.04045
                    ? component / 12.92
                    : Math.Pow((component + 0.055) / 1.055, 2.4);
            }

            return table;
        }

        private static int ClampToByte(int value)
        {
            return Math.Max(0, Math.Min(255, value));
        }

        private static PaletteEntry FindNearest(PaletteEntry[] palette, int red, int green, int blue)
        {
            var target = ToOklab(red, green, blue);
            var nearest = palette[0];
            var nearestDistance = double.MaxValue;

            foreach (var entry in palette)
            {
                var deltaL = entry.Oklab.L - target.L;
                var deltaA = entry.Oklab.A - target.A;
                var deltaB = entry.Oklab.B - target.B;
                var distance = (deltaL * deltaL) + (deltaA * deltaA) + (deltaB * deltaB);
                if (distance < nearestDistance)
                {
                    nearest = entry;
                    nearestDistance = distance;
                }
            }

            return nearest;
        }

        private static bool MarkUsedPaletteEntries(
            Bitmap bitmap,
            IDictionary<int, List<int>> paletteArgb,
            bool[] used,
            Func<bool> shouldCancel,
            Action<int> reportPixelsProcessed)
        {
            var rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            var data = bitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            try
            {
                unsafe
                {
                    for (var y = 0; y < bitmap.Height; y++)
                    {
                        if (shouldCancel())
                        {
                            return false;
                        }

                        var row = (int*)((byte*)data.Scan0 + (y * data.Stride));
                        for (var x = 0; x < bitmap.Width; x++)
                        {
                            if (!paletteArgb.TryGetValue(row[x], out var indices))
                            {
                                continue;
                            }

                            foreach (var index in indices)
                            {
                                used[index] = true;
                            }
                        }

                        reportPixelsProcessed(bitmap.Width);
                    }
                }
            }
            finally
            {
                bitmap.UnlockBits(data);
            }

            return true;
        }

        private static int RoundError(int value)
        {
            return value >= 0 ? (value + 8) / 16 : -((-value + 8) / 16);
        }

        private static OklabColor ToOklab(int red, int green, int blue)
        {
            var linearRed = SrgbToLinear[red];
            var linearGreen = SrgbToLinear[green];
            var linearBlue = SrgbToLinear[blue];

            var l = (0.4122214708 * linearRed) + (0.5363325363 * linearGreen) + (0.0514459929 * linearBlue);
            var m = (0.2119034982 * linearRed) + (0.6806995451 * linearGreen) + (0.1073969566 * linearBlue);
            var s = (0.0883024619 * linearRed) + (0.2817188376 * linearGreen) + (0.6299787005 * linearBlue);

            var lRoot = Math.Pow(l, 1.0 / 3.0);
            var mRoot = Math.Pow(m, 1.0 / 3.0);
            var sRoot = Math.Pow(s, 1.0 / 3.0);

            return new OklabColor(
                (0.2104542553 * lRoot) + (0.7936177850 * mRoot) - (0.0040720468 * sRoot),
                (1.9779984951 * lRoot) - (2.4285922050 * mRoot) + (0.4505937099 * sRoot),
                (0.0259040371 * lRoot) + (0.7827717662 * mRoot) - (0.8086757660 * sRoot));
        }

        private struct OklabColor
        {
            public OklabColor(double l, double a, double b)
            {
                this.L = l;
                this.A = a;
                this.B = b;
            }

            public double L { get; }

            public double A { get; }

            public double B { get; }
        }

        private struct PaletteEntry
        {
            public PaletteEntry(Color color)
            {
                this.Color = color;
                this.Oklab = ToOklab(color.R, color.G, color.B);
            }

            public Color Color { get; }

            public OklabColor Oklab { get; }
        }
    }
}
