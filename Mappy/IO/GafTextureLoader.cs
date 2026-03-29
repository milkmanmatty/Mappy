namespace Mappy.IO
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Mappy.IO.Gaf;
    using TAUtil.Gaf;
    using TAUtil.Gdi.Bitmap;
    using TAUtil.Hpi;

    public static class GafTextureLoader
    {
        public static GafTextureFrame TryGetFrame(string textureName)
        {
            if (string.IsNullOrWhiteSpace(textureName))
            {
                return null;
            }

            var key = textureName.Trim();
            var vkey = VersionedKey(key);
            if (Miss.Contains(vkey))
            {
                return null;
            }

            if (Cache.TryGetValue(vkey, out var cached))
            {
                return cached;
            }

            var hpis = new List<string>(LoadingUtils.EnumerateSearchHpis());
            hpis.Reverse();

            foreach (var hpi in hpis)
            {
                using (var archive = new HpiArchive(hpi))
                {
                    var frame = TryLoadFromTexturesDirectory(archive, key);
                    if (frame != null)
                    {
                        Cache[vkey] = frame;
                        return frame;
                    }
                }
            }

            Miss.Add(vkey);
            return null;
        }

        private static GafTextureFrame TryLoadFromTexturesDirectory(HpiArchive archive, string entryName)
        {
            foreach (var fileInfo in EnumerateTextureGafFiles(archive))
            {
                var gaf = LoadGafEntries(fileInfo, archive);
                if (gaf == null)
                {
                    continue;
                }

                foreach (var e in gaf)
                {
                    if (e == null || e.Name == null)
                    {
                        continue;
                    }

                    if (!string.Equals(e.Name, entryName, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var frame = FirstEntryFrameToTexture(e);
                    if (frame != null)
                    {
                        return frame;
                    }
                }
            }

            return null;
        }

        private static IEnumerable<HpiArchive.FileInfo> EnumerateTextureGafFiles(HpiArchive archive)
        {
            var texturesDir = archive.FindDirectory("textures");
            if (texturesDir == null)
            {
                yield break;
            }

            foreach (var fi in archive.GetFilesRecursive(texturesDir))
            {
                if (fi.Name.EndsWith(".gaf", StringComparison.OrdinalIgnoreCase))
                {
                    yield return fi;
                }
            }
        }

        private static GafTextureFrame FirstEntryFrameToTexture(GafEntry entry)
        {
            if (entry.Frames == null || entry.Frames.Length == 0)
            {
                return null;
            }

            var frame = entry.Frames[0];
            if (frame?.Data == null || frame.Width == 0 || frame.Height == 0)
            {
                return null;
            }

            var expected = frame.Width * frame.Height;
            if (frame.Data.Length < expected)
            {
                return null;
            }

            var indices = new byte[expected];
            Buffer.BlockCopy(frame.Data, 0, indices, 0, expected);
            var bmp = BitmapConvert.ToBitmap(frame.Data, frame.Width, frame.Height);
            return new GafTextureFrame(bmp, indices, frame.Width, frame.Height, frame.TransparencyIndex);
        }

        private static GafEntry[] LoadGafEntries(HpiArchive.FileInfo fileInfo, HpiArchive archive)
        {
            if (fileInfo.Size < 1)
            {
                return null;
            }

            var fileBuffer = new byte[fileInfo.Size];
            archive.Extract(fileInfo, fileBuffer);
            var adapter = new GafEntryAllFramesArrayAdapter();
            using (var b = new GafReader(new MemoryStream(fileBuffer), adapter))
            {
                b.Read();
            }

            return adapter.Entries;
        }

        private static readonly Dictionary<string, GafTextureFrame> Cache = new Dictionary<string, GafTextureFrame>(StringComparer.OrdinalIgnoreCase);

        private static readonly HashSet<string> Miss = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private static string VersionedKey(string textureName) => "gafx5:" + textureName;
    }
}
