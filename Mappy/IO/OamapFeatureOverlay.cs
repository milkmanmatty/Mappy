namespace Mappy.IO
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;

    using Mappy.Services;

    using TAUtil.HpiUtil;

    public static class OamapFeatureOverlay
    {
        public static void Mount(FeatureService featureService, ZipArchive zip, IList<string> files)
        {
            if (featureService == null)
            {
                return;
            }

            featureService.ClearSupplementalHpi();
            var wanted = (files ?? new List<string>())
                .Select(path => (path ?? string.Empty).Replace('\\', '/').TrimStart('/'))
                .Where(IsOverlayPath)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (wanted.Count == 0 || zip == null)
            {
                return;
            }

            var directory = Path.Combine(Path.GetTempPath(), "mappy-oamap-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            var hpiPath = Path.Combine(directory, "overlay.hpi");
            try
            {
                using (var writer = new HpiWriter(hpiPath, HpiWriter.CompressionMethod.ZLib))
                {
                    foreach (var archivePath in wanted)
                    {
                        var entry = FindEntry(zip, archivePath);
                        if (entry == null)
                        {
                            continue;
                        }

                        var diskPath = Path.Combine(directory, archivePath.Replace('/', Path.DirectorySeparatorChar));
                        var folder = Path.GetDirectoryName(diskPath);
                        if (!string.IsNullOrEmpty(folder))
                        {
                            Directory.CreateDirectory(folder);
                        }

                        using (var input = entry.Open())
                        using (var output = File.Create(diskPath))
                        {
                            input.CopyTo(output);
                        }

                        writer.AddFile(archivePath.Replace('/', '\\'), diskPath);
                    }
                }

                var infos = FeatureLoadingUtils.LoadFromHpi(hpiPath);
                featureService.SetSupplementalHpi(hpiPath, directory, infos);
            }
            catch (Exception)
            {
                featureService.ClearSupplementalHpi();
                TryDelete(directory);
            }
        }

        public static ZipArchiveEntry FindEntry(ZipArchive zip, string archivePath)
        {
            var wanted = archivePath.Replace('\\', '/');
            foreach (var entry in zip.Entries)
            {
                var name = entry.FullName.Replace('\\', '/');
                if (string.Equals(name, wanted, StringComparison.OrdinalIgnoreCase))
                {
                    return entry;
                }
            }

            return null;
        }

        private static bool IsOverlayPath(string path)
        {
            if (path.StartsWith("features/", StringComparison.OrdinalIgnoreCase)
                && path.EndsWith(".tdf", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (path.StartsWith("anims/", StringComparison.OrdinalIgnoreCase)
                && path.EndsWith(".gaf", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return path.StartsWith("objects3d/", StringComparison.OrdinalIgnoreCase)
                && path.EndsWith(".3do", StringComparison.OrdinalIgnoreCase);
        }

        private static void TryDelete(string directory)
        {
            try
            {
                if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
                {
                    Directory.Delete(directory, true);
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
