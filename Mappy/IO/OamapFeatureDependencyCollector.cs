namespace Mappy.IO
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Mappy.Services;

    using TAUtil.Hpi;
    using TAUtil.Tdf;

    public static class OamapFeatureDependencyCollector
    {
        private static readonly HashSet<string> BaseArchiveFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "TOTALA1.HPI",
            "TOTALA2.HPI",
            "TOTALA3.HPI",
            "TOTALA4.HPI",
            "CCDATA.CCX",
            "CCMAPS.CCX",
            "REV31.GP3",
        };

        public static OamapDependencyResult Collect(IEnumerable<string> featureNames, FeatureService featureService)
        {
            var result = new OamapDependencyResult();
            var names = (featureNames ?? Enumerable.Empty<string>())
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (names.Count == 0)
            {
                return result;
            }

            var allHpis = LoadingUtils.EnumerateSearchHpis().ToList();
            if (!string.IsNullOrEmpty(featureService?.SupplementalHpiPath)
                && File.Exists(featureService.SupplementalHpiPath))
            {
                allHpis.Add(featureService.SupplementalHpiPath);
            }

            var baseNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var basePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var customFeatures = new Dictionary<string, FeatureSource>(StringComparer.OrdinalIgnoreCase);
            var customArchives = new List<ArchiveIndex>();

            foreach (var hpiPath in allHpis)
            {
                var isBase = IsBaseArchive(hpiPath);
                try
                {
                    using (var archive = new HpiArchive(hpiPath))
                    {
                        var index = isBase ? null : new ArchiveIndex(hpiPath);
                        ScanRoot(archive, "features", isBase, baseNames, basePaths, customFeatures, index);
                        ScanRoot(archive, "anims", isBase, baseNames, basePaths, customFeatures, index);
                        ScanRoot(archive, "objects3d", isBase, baseNames, basePaths, customFeatures, index);
                        if (index != null)
                        {
                            customArchives.Add(index);
                        }
                    }
                }
                catch (Exception)
                {
                    // Skip archives that cannot be read. The map can still be packed.
                }
            }

            var packed = new Dictionary<string, OamapPackedFile>(StringComparer.OrdinalIgnoreCase);
            foreach (var featureName in names)
            {
                if (baseNames.Contains(featureName))
                {
                    continue;
                }

                if (!customFeatures.TryGetValue(featureName, out var source))
                {
                    result.Warnings.Add("Could not find a feature definition for \"" + featureName + "\" outside the base game.");
                    continue;
                }

                if (featureService != null && featureService.TryGetFeatureInfo(featureName, out var info))
                {
                    if (string.IsNullOrWhiteSpace(source.AnimFileName))
                    {
                        source.AnimFileName = info.AnimFileName;
                    }

                    if (string.IsNullOrWhiteSpace(source.ObjectName))
                    {
                        source.ObjectName = info.ObjectName;
                    }
                }

                TryPack(
                    source.NormalizedTdfPath,
                    source.HpiPath,
                    source.TdfFullPath,
                    basePaths,
                    customArchives,
                    packed,
                    result,
                    featureName);

                var animPath = AnimArchivePath(source.AnimFileName);
                if (animPath != null)
                {
                    TryPackResolved(animPath, basePaths, customArchives, packed, result, featureName);
                }

                var objectPath = ObjectArchivePath(source.ObjectName);
                if (objectPath != null)
                {
                    TryPackResolved(objectPath, basePaths, customArchives, packed, result, featureName);
                }
            }

            result.Files.AddRange(packed.Values.OrderBy(file => file.ArchivePath, StringComparer.Ordinal));
            return result;
        }

        private static void TryPackResolved(
            string normalizedPath,
            HashSet<string> basePaths,
            List<ArchiveIndex> customArchives,
            Dictionary<string, OamapPackedFile> packed,
            OamapDependencyResult result,
            string featureName)
        {
            if (basePaths.Contains(normalizedPath))
            {
                return;
            }

            if (!TryFindCustomFile(customArchives, normalizedPath, out var hpiPath, out var fullPath))
            {
                result.Warnings.Add("Could not find " + normalizedPath + " for feature \"" + featureName + "\".");
                return;
            }

            TryPack(normalizedPath, hpiPath, fullPath, basePaths, customArchives, packed, result, featureName);
        }

        private static void TryPack(
            string normalizedPath,
            string hpiPath,
            string fullPath,
            HashSet<string> basePaths,
            List<ArchiveIndex> customArchives,
            Dictionary<string, OamapPackedFile> packed,
            OamapDependencyResult result,
            string featureName)
        {
            if (string.IsNullOrEmpty(normalizedPath) || packed.ContainsKey(normalizedPath))
            {
                return;
            }

            if (basePaths.Contains(normalizedPath))
            {
                result.Warnings.Add(
                    "Left \"" + featureName + "\" file " + normalizedPath + " out because that path is already in the base game.");
                return;
            }

            if (!IsPackablePath(normalizedPath))
            {
                return;
            }

            if (!TryExtract(hpiPath, fullPath, out var bytes))
            {
                if (!TryFindCustomFile(customArchives, normalizedPath, out hpiPath, out fullPath)
                    || !TryExtract(hpiPath, fullPath, out bytes))
                {
                    result.Warnings.Add("Could not read " + normalizedPath + " for feature \"" + featureName + "\".");
                    return;
                }
            }

            packed[normalizedPath] = new OamapPackedFile
            {
                ArchivePath = normalizedPath,
                Bytes = bytes,
            };
        }

        private static bool TryExtract(string hpiPath, string fullPath, out byte[] bytes)
        {
            bytes = null;
            if (string.IsNullOrEmpty(hpiPath) || string.IsNullOrEmpty(fullPath))
            {
                return false;
            }

            try
            {
                using (var archive = new HpiArchive(hpiPath))
                {
                    var file = archive.FindFile(fullPath);
                    if (file == null)
                    {
                        return false;
                    }

                    bytes = new byte[file.Size];
                    archive.Extract(file, bytes);
                    return true;
                }
            }
            catch (Exception)
            {
                bytes = null;
                return false;
            }
        }

        private static bool TryFindCustomFile(
            List<ArchiveIndex> customArchives,
            string normalizedPath,
            out string hpiPath,
            out string fullPath)
        {
            for (var i = customArchives.Count - 1; i >= 0; i--)
            {
                if (customArchives[i].Files.TryGetValue(normalizedPath, out fullPath))
                {
                    hpiPath = customArchives[i].HpiPath;
                    return true;
                }
            }

            hpiPath = null;
            fullPath = null;
            return false;
        }

        private static void ScanRoot(
            HpiArchive archive,
            string root,
            bool isBase,
            HashSet<string> baseNames,
            HashSet<string> basePaths,
            Dictionary<string, FeatureSource> customFeatures,
            ArchiveIndex customIndex)
        {
            foreach (var file in archive.GetFilesRecursive(root))
            {
                var normalized = NormalizeArchivePath(file.FullPath);
                if (normalized.Length == 0)
                {
                    continue;
                }

                if (isBase)
                {
                    basePaths.Add(normalized);
                    if (normalized.EndsWith(".tdf", StringComparison.OrdinalIgnoreCase))
                    {
                        CollectSectionNames(archive, file, baseNames, null, null);
                    }
                }
                else if (customIndex != null)
                {
                    customIndex.Files[normalized] = file.FullPath;
                    if (normalized.EndsWith(".tdf", StringComparison.OrdinalIgnoreCase))
                    {
                        CollectSectionNames(archive, file, null, customFeatures, customIndex.HpiPath);
                    }
                }
            }
        }

        private static void CollectSectionNames(
            HpiArchive archive,
            HpiArchive.FileInfo file,
            HashSet<string> baseNames,
            Dictionary<string, FeatureSource> customFeatures,
            string hpiPath)
        {
            TdfNode root;
            try
            {
                var bytes = new byte[file.Size];
                archive.Extract(file, bytes);
                using (var stream = new MemoryStream(bytes))
                {
                    root = TdfNode.LoadTdf(stream);
                }
            }
            catch (Exception)
            {
                return;
            }

            if (root?.Keys == null)
            {
                return;
            }

            foreach (var section in root.Keys.Values)
            {
                if (section == null || string.IsNullOrWhiteSpace(section.Name))
                {
                    continue;
                }

                if (baseNames != null)
                {
                    baseNames.Add(section.Name);
                    continue;
                }

                if (customFeatures == null)
                {
                    continue;
                }

                customFeatures[section.Name] = new FeatureSource
                {
                    HpiPath = hpiPath,
                    TdfFullPath = file.FullPath,
                    NormalizedTdfPath = NormalizeArchivePath(file.FullPath),
                    AnimFileName = EntryOrEmpty(section, "filename"),
                    ObjectName = EntryOrEmpty(section, "object"),
                };
            }
        }

        private static string EntryOrEmpty(TdfNode section, string key)
        {
            if (section.Entries != null && section.Entries.TryGetValue(key, out var value) && value != null)
            {
                return value.Trim();
            }

            return string.Empty;
        }

        private static string AnimArchivePath(string filename)
        {
            if (string.IsNullOrWhiteSpace(filename))
            {
                return null;
            }

            var name = filename.Trim().Replace('\\', '/');
            if (name.StartsWith("anims/", StringComparison.OrdinalIgnoreCase))
            {
                return name.EndsWith(".gaf", StringComparison.OrdinalIgnoreCase) ? name : name + ".gaf";
            }

            var file = name;
            var slash = file.LastIndexOf('/');
            if (slash >= 0)
            {
                file = file.Substring(slash + 1);
            }

            if (!file.EndsWith(".gaf", StringComparison.OrdinalIgnoreCase))
            {
                file += ".gaf";
            }

            return "anims/" + file;
        }

        private static string ObjectArchivePath(string objectName)
        {
            if (string.IsNullOrWhiteSpace(objectName))
            {
                return null;
            }

            var name = objectName.Trim().Replace('\\', '/');
            var file = name;
            var slash = file.LastIndexOf('/');
            if (slash >= 0)
            {
                file = file.Substring(slash + 1);
            }

            if (!file.EndsWith(".3do", StringComparison.OrdinalIgnoreCase))
            {
                file += ".3do";
            }

            return "objects3d/" + file;
        }

        private static bool IsPackablePath(string normalizedPath)
        {
            if (normalizedPath.StartsWith("features/", StringComparison.OrdinalIgnoreCase)
                && normalizedPath.EndsWith(".tdf", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (normalizedPath.StartsWith("anims/", StringComparison.OrdinalIgnoreCase)
                && normalizedPath.EndsWith(".gaf", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return normalizedPath.StartsWith("objects3d/", StringComparison.OrdinalIgnoreCase)
                && normalizedPath.EndsWith(".3do", StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeArchivePath(string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath))
            {
                return string.Empty;
            }

            return fullPath.Replace('\\', '/').TrimStart('/');
        }

        private static bool IsBaseArchive(string path)
        {
            return BaseArchiveFileNames.Contains(Path.GetFileName(path) ?? string.Empty);
        }

        private sealed class FeatureSource
        {
            public string HpiPath { get; set; }

            public string TdfFullPath { get; set; }

            public string NormalizedTdfPath { get; set; }

            public string AnimFileName { get; set; }

            public string ObjectName { get; set; }
        }

        private sealed class ArchiveIndex
        {
            public ArchiveIndex(string hpiPath)
            {
                this.HpiPath = hpiPath;
                this.Files = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            public string HpiPath { get; }

            public Dictionary<string, string> Files { get; }
        }
    }
}
