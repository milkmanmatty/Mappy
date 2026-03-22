namespace Mappy.Util
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.IO;
    using System.Numerics;
    using System.Reflection;
    using Mappy.Collections;
    using Mappy.Models.Enums;

    /// <summary>
    /// Class specifically for housing utility methods for working with images.
    /// In particular, helper methods for exporting and importing heightmaps and graphics.
    /// This includes methods for ensuring the correct format is output/input.
    /// </summary>
    public static class ImgUtil
    {
        public static readonly string TempDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "Mappy",
            "Temp");

        public static void ValidateTemps()
        {
            if (!Directory.Exists(TempDir))
            {
                Directory.CreateDirectory(TempDir);
            }
        }

        public static void ClearTemps()
        {
            foreach (var file in Directory.GetFiles(TempDir))
            {
                if (file.EndsWith(".png"))
                {
                    File.Delete(file);
                }
            }
        }

        public static Vector3 GetLightDirectionFromEnum(LightDirection dir)
        {
            switch (dir)
            {
                case LightDirection.TopLeft:
                    return new Vector3(-1, -1, 1);
                case LightDirection.TopRight:
                    return new Vector3(1, -1, 1);
                case LightDirection.BottomLeft:
                    return new Vector3(-1, 1, 1);
                case LightDirection.BottomRight:
                    return new Vector3(1, 1, 1);
                case LightDirection.Overhead:
                    return new Vector3(0, 0, 1);
                default:
                    throw new ArgumentException("Invalid light direction");
            }
        }

        public static Grid<int> ReadHeightmap(Bitmap bmp)
        {
            var data = bmp.LockBits(
                new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);

            var grid = new Grid<int>(bmp.Width, bmp.Height);
            var len = bmp.Width * bmp.Height;

            unsafe
            {
                var ptr = (int*)data.Scan0;
                for (var i = 0; i < len; i++)
                {
                    var c = System.Drawing.Color.FromArgb(ptr[i]);
                    grid[i] = c.R;
                }
            }

            bmp.UnlockBits(data);

            return grid;
        }

        public static Bitmap GetBitmapFromHeightmapGrid(IGrid<int> heights)
        {
            var bmp = new Bitmap(heights.Width, heights.Height, PixelFormat.Format32bppArgb);
            var data = bmp.LockBits(
                new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height),
                ImageLockMode.WriteOnly,
                PixelFormat.Format32bppArgb);

            unsafe
            {
                var pointer = (int*)data.Scan0;
                var i = 0;
                foreach (var h in heights)
                {
                    pointer[i++] = System.Drawing.Color.FromArgb(h, h, h).ToArgb();
                }
            }

            bmp.UnlockBits(data);

            return bmp;
        }

        public static Bitmap GetBitmapFromTilegrid(IGrid<Bitmap> grid)
        {
            int width = grid.Width * 32;
            int height = grid.Height * 32;

            Bitmap bmp = new Bitmap(width, height, PixelFormat.Format8bppIndexed);
            bmp = ForceTaPalette(bmp);

            BitmapData targetData = bmp.LockBits(
                new System.Drawing.Rectangle(0, 0, width, height),
                ImageLockMode.WriteOnly,
                PixelFormat.Format32bppArgb);

            try
            {
                for (var tileY = 0; tileY < grid.Height; tileY++)
                {
                    for (var tileX = 0; tileX < grid.Width; tileX++)
                    {
                        Bitmap tile = grid.Get(tileX, tileY);
                        BitmapData tileData = tile.LockBits(
                            new System.Drawing.Rectangle(0, 0, 32, 32),
                            ImageLockMode.ReadOnly,
                            PixelFormat.Format32bppArgb);

                        try
                        {
                            unsafe
                            {
                                var src = (byte*)tileData.Scan0;
                                byte* dst = (byte*)targetData.Scan0
                                    + (targetData.Stride * (tileY * 32))
                                    + (tileX * 32 * 4);
                                for (var row = 0; row < 32; row++)
                                {
                                    Buffer.MemoryCopy(
                                        src + (row * tileData.Stride),
                                        dst + (row * targetData.Stride),
                                        32 * 4,
                                        32 * 4);
                                }
                            }
                        }
                        finally
                        {
                            tile.UnlockBits(tileData);
                        }
                    }
                }
            }
            finally
            {
                bmp.UnlockBits(targetData);
            }

            return bmp;
        }

        // This should probably be in TAUtil.GDI/Palette
        public static List<System.Drawing.Color> GetTaPalette()
        {
            string exePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;
            return LoadJascPal(Path.Combine(exePath, @"Assets\TA Palette.pal"));
        }

        public static Bitmap ForceTaPalette(Bitmap bmp)
        {
            ColorPalette taPalette = bmp.Palette;
            var taColours = GetTaPalette();
            for (int i = 0; i < 256; i++)
            {
                taPalette.Entries[i] = taColours[i];
            }

            bmp.Palette = taPalette;
            return bmp;
        }

        public static Bitmap Convert32bppTo8bppIndexed(Bitmap source32Img)
        {
            Bitmap bmp = new Bitmap(source32Img.Width, source32Img.Height, PixelFormat.Format8bppIndexed);
            bmp = ForceTaPalette(bmp);

            BitmapData bmpData = bmp.LockBits(
                new Rectangle(0, 0, source32Img.Width, source32Img.Height),
                ImageLockMode.WriteOnly,
                PixelFormat.Format32bppArgb);

            BitmapData sourceData = source32Img.LockBits(
                new Rectangle(0, 0, source32Img.Width, source32Img.Height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);

            unsafe
            {
                try
                {
                    var sourcePtr = (byte*)sourceData.Scan0;
                    var destPtr = (byte*)bmpData.Scan0;
                    int totalBytes = sourceData.Stride * source32Img.Height;
                    Buffer.MemoryCopy(sourcePtr, destPtr, totalBytes, totalBytes);
                }
                finally
                {
                    source32Img.UnlockBits(sourceData);
                    bmp.UnlockBits(bmpData);
                }
            }

            return bmp;
        }

        // Could be public
        private static List<Color> LoadJascPal(string filename)
        {
            var colors = new List<System.Drawing.Color>();
            string[] lines = File.ReadAllLines(filename);

            // Skip first 3 lines (Header, Version, Count)
            for (int i = 3; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i]))
                {
                    continue;
                }

                string[] parts = lines[i].Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 3)
                {
                    colors.Add(System.Drawing.Color.FromArgb(int.Parse(parts[0]), int.Parse(parts[1]), int.Parse(parts[2])));
                }
            }

            return colors;
        }
    }
}