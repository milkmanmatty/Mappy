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
        /// Removes shading from an image using a corresponding heightmap.
        /// </summary>
        /// <param name="referenceImage">The original image with shadows.</param>
        /// <param name="heightMap">The grayscale heightmap (same dimensions as reference).</param>
        /// <param name="lightDirection">Light direction of the referenceImage.</param>
        /// <param name="ambient">Ambient light floor (0.0 to 1.0) to prevent dividing by zero.</param>
        /// <param name="depthScale">Exaggerates or flattens the heightmap data.</param>
        /// <returns>A new Bitmap with shadows flattened.</returns>
        public static Bitmap RemoveShadows(
            Bitmap referenceImage,
            Bitmap heightMap,
            Vector3 lightDirection,
            float ambient = 0.5f,
            float depthScale = 10.0f)
        {
            // For TA this will never be true, but check anyway just in case
            if (referenceImage.Width != heightMap.Width || referenceImage.Height != heightMap.Height)
            {
                heightMap = BicubicResize(heightMap, referenceImage);
            }

            int width = referenceImage.Width;
            int height = referenceImage.Height;

            Bitmap output = new Bitmap(width, height, PixelFormat.Format32bppArgb);

            // Normalize the light vector
            var normL = Vector3.Normalize(lightDirection);

            Rectangle rect = new Rectangle(0, 0, width, height);
            BitmapData refData = referenceImage.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            BitmapData heightData = heightMap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            BitmapData outData = output.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

            unsafe
            {
                var refPtr = (byte*)refData.Scan0;
                var hgtPtr = (byte*)heightData.Scan0;
                var outPtr = (byte*)outData.Scan0;

                int stride = refData.Stride;

                for (var y = 1; y < height - 1; y++)
                {
                    for (var x = 1; x < width - 1; x++)
                    {
                        // 1. Calculate normals from the heightmap using central difference
                        // We read the Blue channel (offset 0) assuming the heightmap is grayscale
                        float hLeft = hgtPtr[(y * stride) + ((x - 1) * 4)];
                        float hRight = hgtPtr[(y * stride) + ((x + 1) * 4)];
                        float hUp = hgtPtr[((y - 1) * stride) + (x * 4)];
                        float hDown = hgtPtr[((y + 1) * stride) + (x * 4)];

                        float dzdx = (hRight - hLeft) / 255f * depthScale;
                        float dzdy = (hDown - hUp) / 255f * depthScale;

                        // Normalize normal vector
                        Vector3 normN = Vector3.Normalize(new Vector3(-dzdx, -dzdy, 1.0f));

                        // 2. Calculate Illumination (Dot Product of Normal and Light Vector)
                        float dotProduct = Vector3.Dot(normN, normL);
                        float intensity = Math.Max(0, dotProduct);

                        // Final illumination factors in ambient light
                        float illumination = ambient + ((1.0f - ambient) * intensity);

                        // 3. Reverse the shading on the reference image
                        int pixelIdx = (y * stride) + (x * 4);

                        byte b = refPtr[pixelIdx];
                        byte g = refPtr[pixelIdx + 1];
                        byte r = refPtr[pixelIdx + 2];
                        byte a = refPtr[pixelIdx + 3];

                        // Divide color by illumination to brighten shadowed areas
                        int newB = Math.Min(255, (int)(b / illumination));
                        int newG = Math.Min(255, (int)(g / illumination));
                        int newR = Math.Min(255, (int)(r / illumination));

                        outPtr[pixelIdx] = (byte)newB;
                        outPtr[pixelIdx + 1] = (byte)newG;
                        outPtr[pixelIdx + 2] = (byte)newR;
                        outPtr[pixelIdx + 3] = a; // Keep original alpha
                    }
                }
            }

            referenceImage.UnlockBits(refData);
            heightMap.UnlockBits(heightData);
            output.UnlockBits(outData);

            return output;
        }

        public static Bitmap RelightToBottomLeft(
            Bitmap graphic,
            Bitmap heights,
            LightDirection currentDir)
        {
            return RelightGraphic(graphic, heights, currentDir, LightDirection.BottomLeft);
        }

        public static Bitmap RelightGraphic(
            Bitmap graphic,
            Bitmap heights,
            LightDirection curLightDir,
            LightDirection newLightDir = LightDirection.BottomLeft,
            float ambient = 0.1f,
            float bumpStrength = 8.0f)
        {
            // Ensure the heightmap matches the source dimensions
            // For TA this will never be true, but check anyway just in case
            if (heights.Width != graphic.Width || heights.Height != graphic.Height)
            {
                heights = BicubicResize(heights, graphic);
            }

            int width = graphic.Width;
            int heightVal = graphic.Height;
            Bitmap output = new Bitmap(width, heightVal, PixelFormat.Format8bppIndexed);
            ImgUtil.ForceTaPalette(output);

            BitmapData srcData = graphic.LockBits(
                new Rectangle(0, 0, width, heightVal),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);
            BitmapData hgtData = heights.LockBits(
                new Rectangle(0, 0, width, heightVal),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);
            BitmapData outData = output.LockBits(
                new Rectangle(0, 0, width, heightVal),
                ImageLockMode.WriteOnly,
                PixelFormat.Format32bppArgb);

            // Setup Lighting Vectors
            Vector3 newLightDirVec = Vector3.Normalize(ImgUtil.GetLightDirectionFromEnum(newLightDir));
            Vector3 curLightDirVec = Vector3.Normalize(ImgUtil.GetLightDirectionFromEnum(curLightDir));
            Vector3 viewVector = new Vector3(0, 0, 1f);
            Vector3 halfway = Vector3.Normalize(newLightDirVec + viewVector);

            unsafe
            {
                var srcPtr = (byte*)srcData.Scan0;
                var hgtPtr = (byte*)hgtData.Scan0;
                var outPtr = (byte*)outData.Scan0;

                for (var y = 1; y < heightVal - 1; y++)
                {
                    for (var x = 1; x < width - 1; x++)
                    {
                        // Calculate byte offsets
                        int curr = (y * srcData.Stride) + (x * 4);
                        int left = curr - 4;
                        int right = curr + 4;
                        int top = ((y - 1) * srcData.Stride) + (x * 4);
                        int bottom = ((y + 1) * srcData.Stride) + (x * 4);

                        // Calculate Gradients from Heightmap (B-channel of heightmap)
                        float dx = (hgtPtr[left] - hgtPtr[right]) / 255f;
                        float dy = (hgtPtr[top] - hgtPtr[bottom]) / 255f;
                        Vector3 normal = Vector3.Normalize(new Vector3(dx * bumpStrength, dy * bumpStrength, 1.0f));

                        // Lighting Math
                        float origShading = Math.Max(Vector3.Dot(normal, curLightDirVec), 0) + ambient;
                        float targetShading = Math.Max(Vector3.Dot(normal, newLightDirVec), 0) + ambient;
                        float spec = (float)Math.Pow(Math.Max(Vector3.Dot(normal, halfway), 0), 32f) * 0.5f;

                        // Apply to Channels (B, G, R, A order in GDI+)
                        for (var c = 0; c < 3; c++)
                        {
                            float color = srcPtr[curr + c];
                            outPtr[curr + c] =
                                (byte)Util.Clamp((color / origShading * targetShading) + (spec * 255), 0, 255);
                        }

                        outPtr[curr + 3] = srcPtr[curr + 3]; // Alpha
                    }
                }
            }

            graphic.UnlockBits(srcData);
            heights.UnlockBits(hgtData);
            output.UnlockBits(outData);

            Quantization.ToTAPalette(output);

            return output;
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

                    byte* outRow = outPtr + (srcY * outData.Stride);
                    byte* origRow = origPtr + (srcY * origData.Stride);

                    for (int x = 0; x < width; x++)
                    {
                        var srcX = flipDir == FlipDirection.Horizontal ? width - 1 - x : x;

                        Vector3 normal = GenerateNormalMap(
                            heightPtr,
                            srcX,
                            srcY,
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

            // Quantization.ToTAPalette(outputBmp);

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