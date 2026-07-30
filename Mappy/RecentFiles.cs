namespace Mappy
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public static class RecentFiles
    {
        public const int MaxRecentFiles = 5;

        public static IReadOnlyList<RecentMapEntry> GetEntries()
        {
            var entries = MappySettings.Settings.RecentMaps;
            if (entries == null || entries.Length == 0)
            {
                return Array.Empty<RecentMapEntry>();
            }

            return entries
                .Where(e => e != null && !string.IsNullOrWhiteSpace(e.FilePath))
                .Take(MaxRecentFiles)
                .ToList();
        }

        public static void Add(string filePath, string mapName = null)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return;
            }

            var normalizedMapName = string.IsNullOrWhiteSpace(mapName) ? null : mapName;

            var list = GetEntries().ToList();
            list.RemoveAll(e => Matches(e, filePath, normalizedMapName));
            list.Insert(0, new RecentMapEntry
            {
                FilePath = filePath,
                MapName = normalizedMapName,
            });

            if (list.Count > MaxRecentFiles)
            {
                list.RemoveRange(MaxRecentFiles, list.Count - MaxRecentFiles);
            }

            MappySettings.Settings.RecentMaps = list.ToArray();
            MappySettings.SaveSettings();
        }

        public static void Remove(RecentMapEntry entry)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.FilePath))
            {
                return;
            }

            var list = GetEntries().ToList();
            var removed = list.RemoveAll(e => Matches(e, entry.FilePath, entry.MapName));
            if (removed == 0)
            {
                return;
            }

            MappySettings.Settings.RecentMaps = list.Count == 0 ? null : list.ToArray();
            MappySettings.SaveSettings();
        }

        private static bool Matches(RecentMapEntry entry, string filePath, string mapName)
        {
            if (!string.Equals(entry.FilePath, filePath, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var left = string.IsNullOrWhiteSpace(entry.MapName) ? null : entry.MapName;
            var right = string.IsNullOrWhiteSpace(mapName) ? null : mapName;
            return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
        }
    }
}
