namespace MappyTest
{
    using System.Drawing;

    using Mappy.UI.Controls;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class MinimapControlTest
    {
        [TestMethod]
        public void RatioLockedImageCoversBottomAndRightEdges()
        {
            var imageColor = Color.FromArgb(12, 96, 48);
            var backgroundColor = Color.White;

            using (var image = new Bitmap(252, 188))
            using (var rendered = new Bitmap(503, 376))
            using (var control = new MinimapControl())
            {
                using (var graphics = Graphics.FromImage(image))
                {
                    graphics.Clear(imageColor);
                }

                control.BackColor = backgroundColor;
                control.BackgroundImage = image;
                control.RectVisible = false;
                control.Size = rendered.Size;
                control.DrawToBitmap(rendered, new Rectangle(Point.Empty, rendered.Size));

                for (var x = 0; x < rendered.Width; x++)
                {
                    Assert.AreEqual(imageColor.ToArgb(), rendered.GetPixel(x, rendered.Height - 1).ToArgb());
                }

                for (var y = 0; y < rendered.Height; y++)
                {
                    Assert.AreEqual(imageColor.ToArgb(), rendered.GetPixel(rendered.Width - 1, y).ToArgb());
                }
            }
        }

        [TestMethod]
        public void PlayerMarkerScalesUpWithTheMinimap()
        {
            var imageColor = Color.FromArgb(12, 96, 48);
            var markerColor = Color.Red;

            using (var image = new Bitmap(252, 188))
            using (var naturalRender = new Bitmap(252, 188))
            using (var doubledRender = new Bitmap(504, 376))
            using (var control = new MinimapControl())
            {
                using (var graphics = Graphics.FromImage(image))
                {
                    graphics.Clear(imageColor);
                }

                control.BackgroundImage = image;
                control.RectVisible = false;
                control.SetMarker(0, new Point(126, 94), markerColor);

                control.Size = naturalRender.Size;
                control.DrawToBitmap(naturalRender, new Rectangle(Point.Empty, naturalRender.Size));

                control.Size = doubledRender.Size;
                control.DrawToBitmap(doubledRender, new Rectangle(Point.Empty, doubledRender.Size));

                Assert.AreEqual(9, CountPixels(naturalRender, markerColor));
                Assert.AreEqual(36, CountPixels(doubledRender, markerColor));
            }
        }

        private static int CountPixels(Bitmap bitmap, Color color)
        {
            var result = 0;
            for (var y = 0; y < bitmap.Height; y++)
            {
                for (var x = 0; x < bitmap.Width; x++)
                {
                    if (bitmap.GetPixel(x, y).ToArgb() == color.ToArgb())
                    {
                        result++;
                    }
                }
            }

            return result;
        }
    }
}
