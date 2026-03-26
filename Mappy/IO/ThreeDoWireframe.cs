namespace Mappy.IO
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Mappy.Util;
    using TAUtil._3do;
    using TAUtil.Hpi;

    public static class ThreeDoWireframe
    {
        private static readonly Dictionary<string, OffsetBitmap> Cache =
            new Dictionary<string, OffsetBitmap>(StringComparer.OrdinalIgnoreCase);

        private static readonly HashSet<string> NegativeCache =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public static OffsetBitmap FromArchiveFile(HpiArchive archive, HpiArchive.FileInfo file)
        {
            var fileBuffer = new byte[file.Size];
            archive.Extract(file, fileBuffer);

            using (var b = new MemoryStream(fileBuffer))
            {
                var adapter = new ModelEdgeReaderAdapter();
                var reader = new ModelReader(b, adapter);
                reader.Read();
                return Util.RenderWireframe(adapter.Edges);
            }
        }

        public static OffsetBitmap TryGetFromSearchPaths(string objectBaseName)
        {
            if (string.IsNullOrWhiteSpace(objectBaseName))
            {
                return null;
            }

            var key = objectBaseName.Trim();
            if (NegativeCache.Contains(key))
            {
                return null;
            }

            if (Cache.TryGetValue(key, out var cached))
            {
                return cached;
            }

            var hpis = new List<string>(LoadingUtils.EnumerateSearchHpis());
            hpis.Reverse();

            foreach (var hpi in hpis)
            {
                using (var archive = new HpiArchive(hpi))
                {
                    var path = HpiPath.Combine("objects3d", key + ".3do");
                    var fileInfo = archive.FindFile(path);
                    if (fileInfo == null)
                    {
                        continue;
                    }

                    var wire = FromArchiveFile(archive, fileInfo);
                    Cache[key] = wire;
                    return wire;
                }
            }

            NegativeCache.Add(key);
            return null;
        }
    }
}
