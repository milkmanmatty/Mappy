namespace Mappy.IO
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    using Mappy.Models;

    using TAUtil.HpiUtil;
    using TAUtil.Sct;

    /// <summary>
    /// Writes section snapshots into an HPI archive as SCT files.
    /// </summary>
    public static class SectionArchiveSaver
    {
        public static bool IsValidPathSegment(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            var trimmed = value.Trim();
            if (trimmed == "." || trimmed == "..")
            {
                return false;
            }

            return trimmed.IndexOfAny(Path.GetInvalidFileNameChars()) < 0;
        }

        public static string NormalizeFileName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var name = value.Trim();
            if (name.EndsWith(".sct", StringComparison.OrdinalIgnoreCase))
            {
                name = name.Substring(0, name.Length - 4).Trim();
            }

            if (!IsValidPathSegment(name))
            {
                return null;
            }

            return name + ".sct";
        }

        public static void Save(
            string hpiPath,
            string world,
            string category,
            IReadOnlyList<SectionExportEntry> entries)
        {
            if (!IsValidPathSegment(world) || !IsValidPathSegment(category))
            {
                throw new IOException("World and category must be single path segments.");
            }

            var temps = new List<string>();
            try
            {
                foreach (var entry in entries)
                {
                    var fileName = NormalizeFileName(entry.FileName);
                    if (fileName == null)
                    {
                        throw new IOException("Invalid section file name.");
                    }

                    var tmp = Path.GetTempFileName();
                    temps.Add(tmp);
                    using (var writer = new SctWriter(File.Create(tmp)))
                    {
                        writer.WriteSct(new MapTileSctAdapter(entry.Tile));
                    }
                }

                using (var wr = new HpiWriter(hpiPath, HpiWriter.CompressionMethod.ZLib))
                {
                    for (var i = 0; i < entries.Count; i++)
                    {
                        var fileName = NormalizeFileName(entries[i].FileName);
                        var internalPath = "sections\\" + world.Trim() + "\\" + category.Trim() + "\\" + fileName;
                        wr.AddFile(internalPath, temps[i]);
                    }
                }
            }
            finally
            {
                foreach (var tmp in temps)
                {
                    if (File.Exists(tmp))
                    {
                        File.Delete(tmp);
                    }
                }
            }
        }
    }
}
