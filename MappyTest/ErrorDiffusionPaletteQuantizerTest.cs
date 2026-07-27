namespace MappyTest
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;

    using Mappy.Util.ImageSampling;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using TAUtil.Gdi.Bitmap;
    using TAUtil.Gdi.Palette;

    [TestClass]
    public class ErrorDiffusionPaletteQuantizerTest
    {
        [TestMethod]
        public void QuantizationPreservesAnAverageMissingFromThePalette()
        {
            var olive = Color.FromArgb(67, 67, 35);
            var green = Color.FromArgb(43, 103, 19);
            var palette = CreatePalette(olive, green);

            using (var source = CreateSourceImage(olive, green))
            using (var target = CreateSolidImage(64, 64, Color.FromArgb(55, 85, 27)))
            {
                ErrorDiffusionPaletteQuantizer.ToPalette(target, palette, new[] { source });

                var oliveCount = CountPixels(target, olive);
                var greenCount = CountPixels(target, green);

                Assert.IsTrue(oliveCount > 0);
                Assert.IsTrue(greenCount > 0);
                Assert.AreEqual(target.Width * target.Height, oliveCount + greenCount);
                Assert.IsTrue(
                    Math.Abs(oliveCount - greenCount) < (target.Width * target.Height * 0.1));
            }
        }

        [TestMethod]
        public void QuantizationDoesNotUsePaletteColoursMissingFromSources()
        {
            var olive = Color.FromArgb(67, 67, 35);
            var green = Color.FromArgb(43, 103, 19);
            var red = Color.Red;
            var palette = CreatePalette(olive, green, red);

            using (var source = CreateSourceImage(olive, green))
            using (var target = CreateSolidImage(8, 8, red))
            {
                ErrorDiffusionPaletteQuantizer.ToPalette(target, palette, new[] { source });

                Assert.AreEqual(0, CountPixels(target, red));
            }
        }

        [TestMethod]
        public void QuantizationReportsProgressThroughCompletion()
        {
            var olive = Color.FromArgb(67, 67, 35);
            var green = Color.FromArgb(43, 103, 19);
            var palette = CreatePalette(olive, green);
            var progress = new List<int>();

            using (var source = CreateSourceImage(olive, green))
            using (var target = CreateSolidImage(16, 16, Color.FromArgb(55, 85, 27)))
            {
                var completed = ErrorDiffusionPaletteQuantizer.ToPalette(
                    target,
                    palette,
                    new[] { source },
                    () => false,
                    progress.Add);

                Assert.IsTrue(completed);
                Assert.AreEqual(0, progress[0]);
                Assert.AreEqual(100, progress[progress.Count - 1]);

                for (var i = 1; i < progress.Count; i++)
                {
                    Assert.IsTrue(progress[i] >= progress[i - 1]);
                }
            }
        }

        [TestMethod]
        public void QuantizationCanBeCancelled()
        {
            var olive = Color.FromArgb(67, 67, 35);
            var green = Color.FromArgb(43, 103, 19);
            var palette = CreatePalette(olive, green);

            using (var source = CreateSourceImage(olive, green))
            using (var target = CreateSolidImage(16, 16, Color.FromArgb(55, 85, 27)))
            {
                var completed = ErrorDiffusionPaletteQuantizer.ToPalette(
                    target,
                    palette,
                    new[] { source },
                    () => true,
                    _ => { });

                Assert.IsFalse(completed);
            }
        }

        [TestMethod]
        public void PaletteScanReportsProgressWithinAnImage()
        {
            var olive = Color.FromArgb(67, 67, 35);
            var green = Color.FromArgb(43, 103, 19);
            var palette = CreatePalette(olive, green);
            var progress = new List<int>();

            using (var source = CreateSolidImage(32, 32, olive))
            using (var target = CreateSolidImage(1, 1, green))
            {
                ErrorDiffusionPaletteQuantizer.ToPalette(
                    target,
                    palette,
                    new[] { source },
                    () => false,
                    progress.Add);

                Assert.IsTrue(progress.Exists(x => x > 0 && x < 20));
            }
        }

        [TestMethod]
        public void EnhancedColoursSurviveTntPaletteSerialization()
        {
            var palette = PaletteFactory.TAPalette;
            var olive = palette[72];
            var green = palette[168];

            using (var source = CreateSourceImage(olive, green))
            using (var target = CreateSolidImage(64, 64, Color.FromArgb(55, 85, 27)))
            {
                ErrorDiffusionPaletteQuantizer.ToPalette(target, palette, new[] { source });

                var bytes = BitmapConvert.ToBytes(target);
                CollectionAssert.Contains(bytes, (byte)72);
                CollectionAssert.Contains(bytes, (byte)168);

                using (var roundTripped = BitmapConvert.ToBitmap(bytes, target.Width, target.Height))
                {
                    Assert.IsTrue(CountPixels(roundTripped, olive) > 0);
                    Assert.IsTrue(CountPixels(roundTripped, green) > 0);
                }
            }
        }

        private static int CountPixels(Bitmap bitmap, Color color)
        {
            var count = 0;
            for (var y = 0; y < bitmap.Height; y++)
            {
                for (var x = 0; x < bitmap.Width; x++)
                {
                    if (bitmap.GetPixel(x, y).ToArgb() == color.ToArgb())
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        private static ArrayPalette CreatePalette(params Color[] colors)
        {
            var palette = new ArrayPalette(colors.Length);
            for (var i = 0; i < colors.Length; i++)
            {
                palette[i] = colors[i];
            }

            return palette;
        }

        private static Bitmap CreateSolidImage(int width, int height, Color color)
        {
            var bitmap = new Bitmap(width, height);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(color);
            }

            return bitmap;
        }

        private static Bitmap CreateSourceImage(Color first, Color second)
        {
            var bitmap = new Bitmap(2, 1);
            bitmap.SetPixel(0, 0, first);
            bitmap.SetPixel(1, 0, second);
            return bitmap;
        }
    }
}
