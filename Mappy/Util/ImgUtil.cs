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

        public static readonly string ExportDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "Mappy",
            "Exports");

        private static Dictionary<int, byte> colorToTAPaletteIndex;

        public static Dictionary<int, byte> ColorToTAPaletteIndex
        {
            get
            {
                if (colorToTAPaletteIndex == null)
                {
                    colorToTAPaletteIndex = SetupPaletteDict();
                }

                return colorToTAPaletteIndex;
            }
            private set => colorToTAPaletteIndex = value;
        }

        public static void ValidateDir(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
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

            Bitmap output = new Bitmap(width, height, PixelFormat.Format8bppIndexed);
            output = ForceTaPalette(output);

            BitmapData outData = output.LockBits(
                new System.Drawing.Rectangle(0, 0, width, height),
                ImageLockMode.WriteOnly,
                PixelFormat.Format8bppIndexed);

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
                                byte* tilePtr = (byte*)tileData.Scan0;
                                byte* outPtr = (byte*)outData.Scan0 + (tileY * 32 * outData.Stride) + (tileX * 32);

                                for (var row = 0; row < 32; row++)
                                {
                                    byte* origRow = tilePtr + (row * tileData.Stride);
                                    byte* outRow = outPtr + (row * outData.Stride);

                                    for (var col = 0; col < 32; col++)
                                    {
                                        // 32bpp is stored as BGRA in memory
                                        int idx = col * 4;
                                        byte b = origRow[idx];
                                        byte g = origRow[idx + 1];
                                        byte r = origRow[idx + 2];
                                        byte a = origRow[idx + 3];

                                        // Reconstruct the integer ARGB value
                                        int argb = (a << 24) | (r << 16) | (g << 8) | b;

                                        // Lookup the palette index and write it to the destination
                                        if (ColorToTAPaletteIndex.TryGetValue(argb, out byte index))
                                        {
                                            outRow[col] = index;
                                        }
                                        else
                                        {
                                            // Fallback (should never happen)
                                            outRow[col] = 0;
                                        }
                                    }
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
                output.UnlockBits(outData);
            }

            return output;
        }

        // This should probably be in TAUtil.GDI/Palette
        public static List<Color> GetTaPalette()
        {
            string exePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;
            return LoadJascPal(Path.Combine(exePath, @"Assets\TA Palette.pal"));
        }

        /// <summary>
        /// Inject the Bitmap with the correct TA Palette. This is needed for 8bpp indexed images.
        /// </summary>
        /// <param name="bmp">The Bitmap to inject the TA Palette into.</param>
        /// <returns>The Bitmap with the TA Palette applied.</returns>
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
            Bitmap output = new Bitmap(source32Img.Width, source32Img.Height, PixelFormat.Format8bppIndexed);
            output = ForceTaPalette(output);

            BitmapData bmpData = output.LockBits(
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
                    output.UnlockBits(bmpData);
                }
            }

            return output;
        }

        /// <summary>
        /// Flips an 8bpp indexed image based on a given direction.
        /// </summary>
        public static unsafe Bitmap FlipBitmap(Bitmap orig8bpp, FlipDirection flipDir)
        {
            if (orig8bpp.PixelFormat != PixelFormat.Format8bppIndexed)
            {
                throw new ArgumentException("Original image must be 8bpp Indexed.");
            }

            int width = orig8bpp.Width;
            int height = orig8bpp.Height;

            // Output will be 32bpp true color due to mathematical lighting calculations
            Bitmap outputBmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);

            BitmapData origData = orig8bpp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format8bppIndexed);
            BitmapData outData = outputBmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

            // Extract the color palette from the 8bpp image
            Color[] palette = orig8bpp.Palette.Entries;

            try
            {
                byte* origPtr = (byte*)origData.Scan0;
                byte* outPtr = (byte*)outData.Scan0;

                for (int y = 0; y < height; y++)
                {
                    var srcY = flipDir == FlipDirection.Vertical ? height - 1 - y : y;

                    byte* outRow = outPtr + (y * outData.Stride);
                    byte* origRow = origPtr + (srcY * origData.Stride);

                    for (int x = 0; x < width; x++)
                    {
                        var srcX = flipDir == FlipDirection.Horizontal ? width - 1 - x : x;

                        byte paletteIndex = origRow[srcX];
                        Color origColor = palette[paletteIndex];

                        // Write to Output
                        // Strict Format32bppArgb memory ordering: Blue, Green, Red, Alpha
                        int outIdx = x * 4;
                        outRow[outIdx] = origColor.B;
                        outRow[outIdx + 1] = origColor.G;
                        outRow[outIdx + 2] = origColor.R;
                        outRow[outIdx + 3] = origColor.A;
                    }
                }
            }
            finally
            {
                // Clean up memory locks
                orig8bpp.UnlockBits(origData);
                outputBmp.UnlockBits(outData);
            }

            return ImgUtil.Convert32bppTo8bppIndexed(outputBmp);
        }

        private static Dictionary<int, byte> SetupPaletteDict()
        {
            List<Color> palette = GetTaPalette();
            Dictionary<int, byte> colorToIndex = new Dictionary<int, byte>();
            for (int i = 0; i < palette.Count; i++)
            {
                int argb = palette[i].ToArgb();

                // In case a palette has duplicate colors, keep the first index found
                if (!colorToIndex.ContainsKey(argb))
                {
                    colorToIndex[argb] = (byte)i;
                }
            }

            return colorToIndex;
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