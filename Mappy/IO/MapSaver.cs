namespace Mappy.IO
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Text;

    using Mappy.Models;
    using Mappy.Services;

    using TAUtil.HpiUtil;
    using TAUtil.Tnt;

    /// <summary>
    /// Provides methods for writing out a IMapModel instance
    /// as TNT or HPI.
    /// </summary>
    public static class MapSaver
    {
        private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);

        public static void SaveTnt(IReadOnlyMapModel map, string filename)
        {
            using (var s = new TntWriter(File.Create(filename)))
            {
                s.WriteTnt(new MapModelTntAdapter(map));
            }
        }

        public static void SaveOta(IReadOnlyMapModel map, string filename)
        {
            var mapDimensions = ComputeMapDimensions512(map);
            using (Stream s = File.Create(filename))
            {
                map.Attributes.WriteOta(s, mapDimensions.Item1, mapDimensions.Item2);
            }
        }

        public static void SaveHpi(
            IReadOnlyMapModel map,
            string filename,
            Func<string, string> useOnlyTedClass = null,
            string markersTempFile = null,
            string oamapManifestTempFile = null)
        {
            var namePart = Path.GetFileNameWithoutExtension(filename);

            var tmpTntName = Path.GetTempFileName();
            var tmpOtaName = Path.GetTempFileName();
            string tmpUseOnlyName = null;

            try
            {
                using (var s = new TntWriter(File.Create(tmpTntName)))
                {
                    s.WriteTnt(new MapModelTntAdapter(map));
                }

                var mapDimensions = ComputeMapDimensions512(map);

                using (Stream s = File.Create(tmpOtaName))
                {
                    map.Attributes.WriteOta(s, mapDimensions.Item1, mapDimensions.Item2);
                }

                var useOnlyName = UseOnlyTdf.NormalizeFileName(map.Attributes.UseOnlyUnits);
                if (useOnlyName != null)
                {
                    tmpUseOnlyName = Path.GetTempFileName();
                    using (Stream s = File.Create(tmpUseOnlyName))
                    {
                        UseOnlyTdf.Write(s, map.UseOnlyUnitNames, useOnlyTedClass);
                    }
                }

                var fname = "maps\\" + namePart;

                using (var wr = new HpiWriter(filename, HpiWriter.CompressionMethod.ZLib))
                {
                    wr.AddFile(fname + ".tnt", tmpTntName);
                    wr.AddFile(fname + ".ota", tmpOtaName);
                    if (!string.IsNullOrEmpty(markersTempFile))
                    {
                        wr.AddFile(fname + ".markers.json", markersTempFile);
                    }

                    if (!string.IsNullOrEmpty(oamapManifestTempFile))
                    {
                        wr.AddFile("oamap.yaml", oamapManifestTempFile);
                    }

                    if (tmpUseOnlyName != null)
                    {
                        wr.AddFile(UseOnlyTdf.GetArchivePath(useOnlyName), tmpUseOnlyName);
                    }
                }
            }
            finally
            {
                if (File.Exists(tmpTntName))
                {
                    File.Delete(tmpTntName);
                }

                if (File.Exists(tmpOtaName))
                {
                    File.Delete(tmpOtaName);
                }

                if (tmpUseOnlyName != null && File.Exists(tmpUseOnlyName))
                {
                    File.Delete(tmpUseOnlyName);
                }
            }
        }

        public static IList<string> SaveOamap(
            IReadOnlyMapModel map,
            string filename,
            OamapPackInfo info,
            FeatureService featureService)
        {
            if (map == null)
            {
                throw new ArgumentNullException(nameof(map));
            }

            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            var error = info.Validate();
            if (!string.IsNullOrEmpty(error))
            {
                throw new IOException(error);
            }

            var stem = info.Stem.Trim();
            var size = OamapManifestWriter.MapSize(map.Tile.TileGrid.Width, map.Tile.TileGrid.Height);
            var featureNames = map.EnumerateFeatureInstances()
                .Select(feature => feature.FeatureName)
                .Where(name => !string.IsNullOrWhiteSpace(name));
            var dependencies = OamapFeatureDependencyCollector.Collect(featureNames, featureService);

            var tmpTntName = Path.GetTempFileName();
            var tmpOtaName = Path.GetTempFileName();
            var tmpPreviewName = Path.GetTempFileName();
            var wrotePreview = false;

            try
            {
                SaveTnt(map, tmpTntName);
                SaveOta(map, tmpOtaName);

                string previewPath = null;
                if (map.Minimap != null)
                {
                    using (var clone = new Bitmap(map.Minimap))
                    {
                        clone.Save(tmpPreviewName, ImageFormat.Png);
                    }

                    previewPath = "previews/" + stem + ".png";
                    wrotePreview = true;
                }

                var files = new List<string>
                {
                    "maps/" + stem + ".ota",
                    "maps/" + stem + ".tnt",
                };
                files.AddRange(dependencies.Files.Select(file => file.ArchivePath));

                var yaml = OamapManifestWriter.Write(info, size, previewPath, files);
                using (var stream = new FileStream(filename, FileMode.Create, FileAccess.ReadWrite))
                using (var zip = new ZipArchive(stream, ZipArchiveMode.Create))
                {
                    WriteTextEntry(zip, "oamap.yaml", yaml);
                    WriteFileEntry(zip, "maps/" + stem + ".tnt", tmpTntName);
                    WriteFileEntry(zip, "maps/" + stem + ".ota", tmpOtaName);
                    if (wrotePreview)
                    {
                        WriteFileEntry(zip, previewPath, tmpPreviewName);
                    }

                    foreach (var file in dependencies.Files)
                    {
                        WriteBytesEntry(zip, file.ArchivePath, file.Bytes);
                    }
                }

                return dependencies.Warnings;
            }
            finally
            {
                if (File.Exists(tmpTntName))
                {
                    File.Delete(tmpTntName);
                }

                if (File.Exists(tmpOtaName))
                {
                    File.Delete(tmpOtaName);
                }

                if (File.Exists(tmpPreviewName))
                {
                    File.Delete(tmpPreviewName);
                }
            }
        }

        private static void WriteTextEntry(ZipArchive zip, string name, string text)
        {
            var entry = zip.CreateEntry(name, CompressionLevel.Optimal);
            using (var writer = new StreamWriter(entry.Open(), Utf8NoBom))
            {
                writer.Write(text);
            }
        }

        private static void WriteFileEntry(ZipArchive zip, string name, string sourcePath)
        {
            var entry = zip.CreateEntry(name, CompressionLevel.Optimal);
            using (var input = File.OpenRead(sourcePath))
            using (var output = entry.Open())
            {
                input.CopyTo(output);
            }
        }

        private static void WriteBytesEntry(ZipArchive zip, string name, byte[] bytes)
        {
            var entry = zip.CreateEntry(name, CompressionLevel.Optimal);
            using (var output = entry.Open())
            {
                output.Write(bytes, 0, bytes.Length);
            }
        }

        private static (int, int) ComputeMapDimensions512(IReadOnlyMapModel map)
        {
            var w = (map.FeatureGridWidth / 32) + ((map.FeatureGridWidth % 32 == 0) ? 0 : 1);
            var h = (map.FeatureGridHeight / 32) + ((map.FeatureGridHeight % 32 == 0) ? 0 : 1);
            return (w, h);
        }
    }
}
