namespace Mappy.Services
{
    using System;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Drawing.Imaging;
    using System.Numerics;
    using Mappy.Models.Enums;
    using Mappy.Util;
    using TAUtil.Gdi.Palette;

    public static class ImageRelightingService
    {
        /// <summary>
        /// Resize the heightmap to match the reference graphic.
        /// </summary>
        /// <param name="sourceHeightmap">The heightmap to be resized to match the reference graphic dimensions.</param>
        /// <param name="referenceGraphic">The larger graphic whose dimensions will be used for resizing.</param>
        public static Bitmap BicubicResize(Bitmap sourceHeightmap, Bitmap referenceGraphic)
        {
            int newWidth = referenceGraphic.Width;
            int newHeight = referenceGraphic.Height;

            Bitmap resizedMap = new Bitmap(newWidth, newHeight, sourceHeightmap.PixelFormat);

            // Use a Graphics object to perform the upscale
            using (Graphics g = Graphics.FromImage(resizedMap))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.DrawImage(sourceHeightmap, 0, 0, newWidth, newHeight);
            }

            return resizedMap;
        }

        /// <summary>
        /// De-lights and re-lights an 8bpp indexed image based on a heightmap.
        /// </summary>
        public static unsafe Bitmap FlipAndRelightBitmap(
            Bitmap orig8bpp,
            Bitmap heightmap,
            FlipDirection flipDir,
            Vector3 newLightDir,
            float normalStrength = 2.0f)
        {
            if (orig8bpp.PixelFormat != PixelFormat.Format8bppIndexed)
            {
                throw new ArgumentException("Original image must be 8bpp Indexed.");
            }

            int width = orig8bpp.Width;
            int height = orig8bpp.Height;

            // Output will be 32bpp true color due to mathematical lighting calculations
            Bitmap outputBmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);

            LightDirection flippedLightDir = flipDir == FlipDirection.Horizontal
                ? LightDirection.BottomRight
                : LightDirection.TopLeft;
            Vector3 origLightDir = ImgUtil.GetLightDirectionFromEnum(flippedLightDir);

            // Normalize light vectors to ensure accurate dot products
            origLightDir = Vector3.Normalize(origLightDir);
            newLightDir = Vector3.Normalize(newLightDir);

            BitmapData origData = orig8bpp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format8bppIndexed);
            BitmapData heightData = heightmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            BitmapData outData = outputBmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            var heightMapStride = heightData.Stride;

            // Extract the color palette from the 8bpp image
            Color[] palette = orig8bpp.Palette.Entries;

            // Ensure ambient light to prevent Divide-By-Zero when de-lighting shadows
            float ambientLight = 0.01f;

            try
            {
                byte* origPtr = (byte*)origData.Scan0;
                byte* heightPtr = (byte*)heightData.Scan0;
                byte* outPtr = (byte*)outData.Scan0;

                // We iterate over the TARGET image coordinates
                for (int y = 0; y < height; y++)
                {
                    var srcY = flipDir == FlipDirection.Vertical ? height - 1 - y : y;

                    byte* outRow = outPtr + (y * outData.Stride);
                    byte* origRow = origPtr + (srcY * origData.Stride);

                    for (int x = 0; x < width; x++)
                    {
                        var srcX = flipDir == FlipDirection.Horizontal ? width - 1 - x : x;

                        // Don't use srcX or srcY here!
                        Vector3 normal = GenerateNormalMap(
                            heightPtr,
                            x,
                            y,
                            width,
                            height,
                            heightMapStride,
                            normalStrength);

                        // Use the original light direction to obtain the Original Shading
                        float origShading = Vector3.Dot(normal, origLightDir);

                        // Clamp to ambient to prevent divide by zero in Step 4
                        origShading = Math.Max(ambientLight, origShading);

                        // De-light the Original Image
                        Color origColor = DelightToCreateAlbedo(
                            origRow,
                            srcX,
                            palette,
                            origShading,
                            out float albedoR,
                            out float albedoG,
                            out float albedoB);

                        // Re-light
                        byte r = RelightPixel(
                            newLightDir,
                            normal,
                            ambientLight,
                            albedoR,
                            albedoG,
                            albedoB,
                            origColor,
                            out byte g,
                            out byte b,
                            out byte a);

                        // Write to Output
                        // Strict Format32bppArgb memory ordering: Blue, Green, Red, Alpha
                        int outIdx = x * 4;
                        outRow[outIdx] = b;
                        outRow[outIdx + 1] = g;
                        outRow[outIdx + 2] = r;
                        outRow[outIdx + 3] = a;
                    }
                }
            }
            finally
            {
                // Clean up memory locks
                orig8bpp.UnlockBits(origData);
                heightmap.UnlockBits(heightData);
                outputBmp.UnlockBits(outData);
            }

            return ImgUtil.Convert32bppTo8bppIndexed(outputBmp);
        }

        private static unsafe Vector3 GenerateNormalMap(
            byte* heightPtr,
            int x,
            int y,
            int width,
            int height,
            int heightMapStride,
            float normalStrength = 2.0f)
        {
            // Generate Normal Map (using source coordinates)
            // Using central differences. Clamp to edges to avoid IndexOutOfRange.
            int clampX_minus = Math.Max(0, x - 1);
            int clampX_plus = Math.Min(width - 1, x + 1);
            int clampY_minus = Math.Max(0, y - 1);
            int clampY_plus = Math.Min(height - 1, y + 1);

            // Read Height (Assuming greyscale, so we just read the R channel at byte offset 2)
            float hLeft = heightPtr[(y * heightMapStride) + (clampX_minus * 4) + 2] / 255f;
            float hRight = heightPtr[(y * heightMapStride) + (clampX_plus * 4) + 2] / 255f;
            float hUp = heightPtr[(clampY_minus * heightMapStride) + (x * 4) + 2] / 255f;
            float hDown = heightPtr[(clampY_plus * heightMapStride) + (x * 4) + 2] / 255f;

            float dx = (hRight - hLeft) * normalStrength;
            float dy = (hDown - hUp) * normalStrength;

            return Vector3.Normalize(new Vector3(-dx, -dy, 1.0f));
        }

        private static unsafe Color DelightToCreateAlbedo(
            byte* origRow,
            int srcX,
            Color[] palette,
            float origShading,
            out float albedoR,
            out float albedoG,
            out float albedoB)
        {
            // Lookup exact color via 8bpp Palette
            byte paletteIndex = origRow[srcX];
            Color origColor = palette[paletteIndex];

            // Strip shadows to get flat Albedo
            albedoR = origColor.R / origShading;
            albedoG = origColor.G / origShading;
            albedoB = origColor.B / origShading;
            return origColor;
        }

        private static byte RelightPixel(
            Vector3 newLightDir,
            Vector3 normal,
            float ambientLight,
            float albedoR,
            float albedoG,
            float albedoB,
            Color origColor,
            out byte g,
            out byte b,
            out byte a)
        {
            // Calculate new shading
            float newShading = Vector3.Dot(normal, newLightDir);
            newShading = Math.Max(ambientLight, newShading);

            // Apply new lighting to the Albedo
            float finalR = albedoR * newShading;
            float finalG = albedoG * newShading;
            float finalB = albedoB * newShading;

            // Clamp results to valid 0-255 byte range
            byte r = (byte)Math.Min(255, Math.Max(0, finalR));
            g = (byte)Math.Min(255, Math.Max(0, finalG));
            b = (byte)Math.Min(255, Math.Max(0, finalB));
            a = origColor.A;
            return r;
        }
    }
}