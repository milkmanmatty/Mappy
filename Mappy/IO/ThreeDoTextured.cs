namespace Mappy.IO
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.IO;
    using System.Runtime.InteropServices;
    using Geometry;
    using Mappy.Util;
    using TAUtil._3do;
    using TAUtil.Gdi.Palette;
    using TAUtil.Hpi;

    public static class ThreeDoTextured
    {
        // Barycentric inside test tolerance. Too strict (near 0) leaves shared-edge pixels rejected by
        // every adjacent triangle, so the output buffer (0 = transparent) shows through as stippled gaps.
        private const double BaryEps = 1e-4;

        private const int AngleSteps = 32;

        private static readonly Dictionary<string, OffsetBitmap> Cache =
            new Dictionary<string, OffsetBitmap>(StringComparer.OrdinalIgnoreCase);

        private static readonly HashSet<string> NegativeCache =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private static readonly IPalette TaPalette = PaletteFactory.TAPalette;

        private static int QuantizeAngle(int angleDegrees)
        {
            var normalized = ((angleDegrees % 360) + 360) % 360;
            return (int)Math.Round((double)normalized / 360 * AngleSteps) % AngleSteps;
        }

        private static double AngleStepToRadians(int step)
        {
            return step * (2.0 * Math.PI / AngleSteps);
        }

        private static string TextureCacheKey(string objectBaseName, int playerSlot, int angleStep) =>
            "tdtex16:" + objectBaseName + ":" + playerSlot.ToString(System.Globalization.CultureInfo.InvariantCulture)
            + ":" + angleStep.ToString(System.Globalization.CultureInfo.InvariantCulture);

        private static string NegativeCacheKey(string objectBaseName) => "tdtex16neg:" + objectBaseName;

        public static void PreloadAll(
            IEnumerable<string> objectBaseNames,
            Action<int> reportProgress = null,
            Func<bool> isCancelled = null)
        {
            var names = new List<string>();
            foreach (var n in objectBaseNames)
            {
                if (!string.IsNullOrWhiteSpace(n))
                {
                    names.Add(n);
                }
            }

            for (var i = 0; i < names.Count; i++)
            {
                if (isCancelled != null && isCancelled())
                {
                    return;
                }

                TryGetFromSearchPaths(names[i], 1);

                if (reportProgress != null && names.Count > 0)
                {
                    reportProgress((100 * (i + 1)) / names.Count);
                }
            }
        }

        public static OffsetBitmap TryGetFromSearchPaths(string objectBaseName, int player = 1, int taAngle = 0)
        {
            if (string.IsNullOrWhiteSpace(objectBaseName))
            {
                return null;
            }

            var key = objectBaseName.Trim();
            var playerSlot = PlayerSlotVisuals.ClampPlayerSlot(player);
            var angleStep = QuantizeAngle(taAngle);
            var negKey = NegativeCacheKey(key);
            if (NegativeCache.Contains(negKey))
            {
                return null;
            }

            var cacheKey = TextureCacheKey(key, playerSlot, angleStep);
            if (Cache.TryGetValue(cacheKey, out var cached))
            {
                return cached;
            }

            var hpis = new List<string>(LoadingUtils.EnumerateSearchHpis());
            hpis.Reverse();

            foreach (var hpi in hpis)
            {
                try
                {
                    using (var archive = new HpiArchive(hpi))
                    {
                        var path = HpiPath.Combine("objects3d", key + ".3do");
                        var fileInfo = archive.FindFile(path)
                            ?? FindLeafUnderDirectory(archive, "objects3d", key + ".3do");
                        if (fileInfo == null)
                        {
                            continue;
                        }

                        var fileBuffer = new byte[fileInfo.Size];
                        archive.Extract(fileInfo, fileBuffer);

                        var adapter = new ModelPrimitiveCollectorAdapter();
                        using (var ms = new MemoryStream(fileBuffer, false))
                        {
                            new ModelReader(ms, adapter).Read();
                        }

                        if (adapter.Faces.Count == 0)
                        {
                            continue;
                        }

                        var rendered = RenderFaces(adapter.Faces, playerSlot, angleStep);
                        if (rendered != null)
                        {
                            Cache[cacheKey] = rendered;
                            return rendered;
                        }
                    }
                }
                catch (Exception)
                {
                }
            }

            NegativeCache.Add(negKey);
            return null;
        }

        private static HpiArchive.FileInfo FindLeafUnderDirectory(
            HpiArchive archive,
            string subtree,
            string leafFileName)
        {
            var dir = archive.FindDirectory(subtree);
            if (dir == null)
            {
                return null;
            }

            foreach (var fi in archive.GetFilesRecursive(dir))
            {
                if (string.Equals(fi.Name, leafFileName, StringComparison.OrdinalIgnoreCase))
                {
                    return fi;
                }
            }

            return null;
        }

        private static OffsetBitmap RenderFaces(
            IReadOnlyList<ModelPrimitiveCollectorAdapter.CollectedFace> faces,
            int playerSlot,
            int angleStep)
        {
            var radians = AngleStepToRadians(angleStep);
            var cosA = Math.Cos(radians);
            var sinA = Math.Sin(radians);

            var rotatedFaces = new List<ModelPrimitiveCollectorAdapter.CollectedFace>(faces.Count);
            foreach (var f in faces)
            {
                var rv = new Vector3D[f.VerticesWorld.Length];
                for (var i = 0; i < f.VerticesWorld.Length; i++)
                {
                    var v = f.VerticesWorld[i];
                    rv[i] = new Vector3D(
                        (v.X * cosA) - (v.Z * sinA),
                        v.Y,
                        (v.X * sinA) + (v.Z * cosA));
                }

                rotatedFaces.Add(new ModelPrimitiveCollectorAdapter.CollectedFace(
                    f.ColorIndex, f.TextureName, rv));
            }

            var tris = new List<RasterTri>(rotatedFaces.Count * 2);
            foreach (var f in rotatedFaces)
            {
                AppendFaceTriangles(f, tris);
            }

            if (tris.Count == 0)
            {
                return null;
            }

            var teamRamp = TaTeamColorRemap.BuildTargetRamp(playerSlot);

            double minX = double.PositiveInfinity;
            double minY = double.PositiveInfinity;
            double maxX = double.NegativeInfinity;
            double maxY = double.NegativeInfinity;

            foreach (var t in tris)
            {
                ExpandBounds(t.S0x, t.S0y, ref minX, ref minY, ref maxX, ref maxY);
                ExpandBounds(t.S1x, t.S1y, ref minX, ref minY, ref maxX, ref maxY);
                ExpandBounds(t.S2x, t.S2y, ref minX, ref minY, ref maxX, ref maxY);
            }

            if (double.IsNaN(minX) || double.IsInfinity(minX) || maxX <= minX || maxY <= minY)
            {
                return null;
            }

            const int pad = 2;
            var ox = Math.Floor(minX) - pad;
            var oy = Math.Floor(minY) - pad;
            var width = (int)Math.Ceiling(maxX - ox) + pad;
            var height = (int)Math.Ceiling(maxY - oy) + pad;
            if (width < 1 || height < 1 || width > 4096 || height > 4096)
            {
                return null;
            }

            var zbuf = new double[width * height];
            for (var i = 0; i < zbuf.Length; i++)
            {
                zbuf[i] = double.NegativeInfinity;
            }

            var pixels = new int[width * height];

            foreach (var t in tris)
            {
                RasterTriangle(
                    t,
                    ox,
                    oy,
                    width,
                    height,
                    zbuf,
                    pixels,
                    teamRamp);
            }

            var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            var rect = new Rectangle(0, 0, width, height);
            var data = bmp.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            try
            {
                for (var y = 0; y < height; y++)
                {
                    Marshal.Copy(pixels, y * width, IntPtr.Add(data.Scan0, y * data.Stride), width);
                }
            }
            finally
            {
                bmp.UnlockBits(data);
            }

            return new OffsetBitmap((int)Math.Round(ox), (int)Math.Round(oy), bmp);
        }

        private static void RasterTriangle(
            RasterTri t,
            double originX,
            double originY,
            int width,
            int height,
            double[] zbuf,
            int[] pixels,
            Color[] teamRamp)
        {
            double ax = t.S0x - originX;
            double ay = t.S0y - originY;
            double bx = t.S1x - originX;
            double by = t.S1y - originY;
            double cx = t.S2x - originX;
            double cy = t.S2y - originY;

            var minXi = (int)Math.Floor(Math.Min(ax, Math.Min(bx, cx))) - 1;
            var minYi = (int)Math.Floor(Math.Min(ay, Math.Min(by, cy))) - 1;
            var maxXi = (int)Math.Ceiling(Math.Max(ax, Math.Max(bx, cx))) + 1;
            var maxYi = (int)Math.Ceiling(Math.Max(ay, Math.Max(by, cy))) + 1;

            if (maxXi < 0 || maxYi < 0 || minXi >= width || minYi >= height)
            {
                return;
            }

            minXi = Math.Max(0, minXi);
            minYi = Math.Max(0, minYi);
            maxXi = Math.Min(width - 1, maxXi);
            maxYi = Math.Min(height - 1, maxYi);

            var denom = ((by - cy) * (ax - cx)) + ((cx - bx) * (ay - cy));
            if (Math.Abs(denom) < 1e-12)
            {
                return;
            }

            var invDenom = 1.0 / denom;

            var hasTex = t.TextureIndices != null && t.TextureWidth > 0 && t.TextureHeight > 0;
            var twm = hasTex ? t.TextureWidth - 1 : 0;
            var thm = hasTex ? t.TextureHeight - 1 : 0;

            var argbFlat = 0;
            if (!hasTex)
            {
                argbFlat = t.MissingTexturePlaceholder
                    ? Color.FromArgb(255, 96, 88, 74).ToArgb()
                    : TaTeamColorRemap.FlatShadeArgb(t.ColorIndex, teamRamp, TaPalette);
            }

            for (var py = minYi; py <= maxYi; py++)
            {
                var fy = py + 0.5;
                var row = py * width;
                for (var px = minXi; px <= maxXi; px++)
                {
                    var fx = px + 0.5;

                    var w0 = (((by - cy) * (fx - cx)) + ((cx - bx) * (fy - cy))) * invDenom;
                    var w1 = (((cy - ay) * (fx - cx)) + ((ax - cx) * (fy - cy))) * invDenom;
                    var w2 = 1.0 - w0 - w1;

                    if (w0 < -BaryEps || w1 < -BaryEps || w2 < -BaryEps)
                    {
                        continue;
                    }

                    var zp =
                        (w0 * t.W0.Y)
                        + (w1 * t.W1.Y)
                        + (w2 * t.W2.Y);

                    var idx = row + px;
                    if (zp <= zbuf[idx])
                    {
                        continue;
                    }

                    int argb;
                    if (hasTex)
                    {
                        var tu = (w0 * t.U0) + (w1 * t.U1) + (w2 * t.U2);
                        var tv = (w0 * t.V0) + (w1 * t.V1) + (w2 * t.V2);
                        var tx = (int)Math.Round(tu * twm);
                        var ty = (int)Math.Round(tv * thm);
                        if (tx < 0)
                        {
                            tx = 0;
                        }

                        if (ty < 0)
                        {
                            ty = 0;
                        }

                        if (tx > twm)
                        {
                            tx = twm;
                        }

                        if (ty > thm)
                        {
                            ty = thm;
                        }

                        var pi = t.TextureIndices[(ty * t.TextureWidth) + tx];
                        argb = pi == t.TextureTransparencyIndex
                            ? TaTeamColorRemap.FlatShadeArgb(t.ColorIndex, teamRamp, TaPalette)
                            : TaTeamColorRemap.FlatShadeArgb(pi, teamRamp, TaPalette);
                    }
                    else
                    {
                        argb = argbFlat;
                    }

                    zbuf[idx] = zp;
                    // Surface texels must be opaque; ARGB(0,0,0,0) would composite as holes over the map.
                    pixels[idx] = (argb & 0x00FFFFFF) | unchecked((int)0xFF000000);
                }
            }
        }

        private struct RasterTri
        {
            public Vector3D W0;
            public Vector3D W1;
            public Vector3D W2;
            public double S0x;
            public double S0y;
            public double S1x;
            public double S1y;
            public double S2x;
            public double S2y;
            public double U0;
            public double V0;
            public double U1;
            public double V1;
            public double U2;
            public double V2;
            public int ColorIndex;
            public byte[] TextureIndices;
            public int TextureWidth;
            public int TextureHeight;
            public byte TextureTransparencyIndex;
            public bool MissingTexturePlaceholder;
        }

        private static void AppendFaceTriangles(
            ModelPrimitiveCollectorAdapter.CollectedFace f,
            List<RasterTri> output)
        {
            var v = f.VerticesWorld;
            var n = v.Length;
            if (n < 3)
            {
                return;
            }

            GafTextureFrame texFrame = null;
            var hadTexName = !string.IsNullOrEmpty(f.TextureName);
            var useTexture = hadTexName;
            if (useTexture)
            {
                texFrame = GafTextureLoader.TryGetFrame(f.TextureName);
                useTexture = texFrame?.Bitmap != null;
            }

            var missingTex = hadTexName && !useTexture;

            if (n == 3)
            {
                AddTri(
                    output,
                    v[0],
                    v[1],
                    v[2],
                    0,
                    0,
                    1,
                    0,
                    0,
                    1,
                    f.ColorIndex,
                    useTexture ? texFrame : null,
                    missingTex && !useTexture);
                return;
            }

            if (n == 4)
            {
                AddTri(
                    output,
                    v[0],
                    v[1],
                    v[2],
                    0,
                    0,
                    1,
                    0,
                    1,
                    1,
                    f.ColorIndex,
                    useTexture ? texFrame : null,
                    missingTex && !useTexture);
                AddTri(
                    output,
                    v[0],
                    v[2],
                    v[3],
                    0,
                    0,
                    1,
                    1,
                    0,
                    1,
                    f.ColorIndex,
                    useTexture ? texFrame : null,
                    missingTex && !useTexture);
                return;
            }

            for (var i = 1; i < n - 1; i++)
            {
                AddTri(
                    output,
                    v[0],
                    v[i],
                    v[i + 1],
                    0,
                    0,
                    1,
                    0,
                    0,
                    1,
                    f.ColorIndex,
                    null,
                    missingTex);
            }
        }

        private static void AddTri(
            List<RasterTri> output,
            Vector3D w0,
            Vector3D w1,
            Vector3D w2,
            double u0,
            double v0,
            double u1,
            double v1,
            double u2,
            double v2,
            int colorIndex,
            GafTextureFrame textureFrame,
            bool missingTexturePlaceholder)
        {
            var s0 = Util.ProjectThreeDoVertex(w0);
            var s1 = Util.ProjectThreeDoVertex(w1);
            var s2 = Util.ProjectThreeDoVertex(w2);

            byte[] idx = null;
            var tw = 0;
            var th = 0;
            byte trans = 0;
            if (textureFrame != null)
            {
                idx = textureFrame.Indices;
                tw = textureFrame.Width;
                th = textureFrame.Height;
                trans = textureFrame.TransparencyIndex;
            }

            output.Add(
                new RasterTri
                {
                    W0 = w0,
                    W1 = w1,
                    W2 = w2,
                    S0x = s0.X,
                    S0y = s0.Y,
                    S1x = s1.X,
                    S1y = s1.Y,
                    S2x = s2.X,
                    S2y = s2.Y,
                    U0 = u0,
                    V0 = v0,
                    U1 = u1,
                    V1 = v1,
                    U2 = u2,
                    V2 = v2,
                    ColorIndex = colorIndex,
                    TextureIndices = idx,
                    TextureWidth = tw,
                    TextureHeight = th,
                    TextureTransparencyIndex = trans,
                    MissingTexturePlaceholder = missingTexturePlaceholder,
                });
        }

        private static void ExpandBounds(
            double x,
            double y,
            ref double minX,
            ref double minY,
            ref double maxX,
            ref double maxY)
        {
            if (x < minX)
            {
                minX = x;
            }

            if (x > maxX)
            {
                maxX = x;
            }

            if (y < minY)
            {
                minY = y;
            }

            if (y > maxY)
            {
                maxY = y;
            }
        }
    }
}
