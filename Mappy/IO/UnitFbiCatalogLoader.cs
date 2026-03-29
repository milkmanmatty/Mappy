namespace Mappy.IO
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;

    using Mappy.Data;

    using TAUtil.Hpi;
    using TAUtil.Tdf;

    public class UnitFbiCatalogLoader : AbstractHpiLoader<UnitCatalogLoadRecord>
    {
        protected override void LoadFile(HpiArchive archive, HpiArchive.FileInfo file)
        {
            var name = Path.GetFileNameWithoutExtension(file.Name);
            if (string.IsNullOrEmpty(name))
            {
                return;
            }

            var side = UnitSideCategory.Other;
            string displayName = null;
            string objectName = null;
            if (file.Size > 0 && file.Size < 10_000_000)
            {
                try
                {
                    var buf = new byte[file.Size];
                    archive.Extract(file, buf);
                    using (var ms = new MemoryStream(buf, false))
                    using (var reader = new StreamReader(ms, Encoding.Default))
                    {
                        var root = TdfNode.LoadTdf(reader);
                        side = ClassifySideFromTdf(root);
                        displayName = FindUnitNameEntryFromTdf(root);
                        objectName = FindObjectNameFromTdf(root);
                    }
                }
                catch (Exception)
                {
                }
            }

            this.Records.Add(new UnitCatalogLoadRecord(name, side, displayName, objectName));
        }

        protected override IEnumerable<HpiArchive.FileInfo> EnumerateFiles(HpiArchive r)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var f in EnumerateFbisUnderDirectoriesNamed(r.GetRoot(), "units"))
            {
                if (TryAddUnique(seen, f))
                {
                    yield return f;
                }
            }
        }

        private static bool TryAddUnique(HashSet<string> seen, HpiArchive.FileInfo f)
        {
            var key = !string.IsNullOrEmpty(f.FullPath)
                ? f.FullPath
                : (f.Name + "|" + f.Offset.ToString(System.Globalization.CultureInfo.InvariantCulture));
            return seen.Add(key);
        }

        private static IEnumerable<HpiArchive.FileInfo> EnumerateFbisUnderDirectoriesNamed(
            HpiArchive.DirectoryInfo dir,
            string folderName)
        {
            foreach (var entry in dir.Entries)
            {
                if (entry is HpiArchive.DirectoryInfo sub)
                {
                    if (sub.Name.Equals(folderName, StringComparison.OrdinalIgnoreCase))
                    {
                        foreach (var f in GetAllFbisRecursive(sub))
                        {
                            yield return f;
                        }
                    }

                    foreach (var f in EnumerateFbisUnderDirectoriesNamed(sub, folderName))
                    {
                        yield return f;
                    }
                }
            }
        }

        private static IEnumerable<HpiArchive.FileInfo> GetAllFbisRecursive(HpiArchive.DirectoryInfo dir)
        {
            foreach (var entry in dir.Entries)
            {
                if (entry is HpiArchive.FileInfo fi)
                {
                    if (fi.Name.EndsWith(".fbi", StringComparison.OrdinalIgnoreCase))
                    {
                        yield return fi;
                    }
                }
                else if (entry is HpiArchive.DirectoryInfo di)
                {
                    foreach (var f in GetAllFbisRecursive(di))
                    {
                        yield return f;
                    }
                }
            }
        }

        private static string FindSideRaw(TdfNode node)
        {
            if (node == null)
            {
                return null;
            }

            if (node.Entries.TryGetValue("Side", out var s))
            {
                return s;
            }

            foreach (var child in node.Keys.Values)
            {
                var t = FindSideRaw(child);
                if (t != null)
                {
                    return t;
                }
            }

            return null;
        }

        private static string FindUnitNameEntryFromTdf(TdfNode node)
        {
            if (node == null)
            {
                return null;
            }

            if (node.Entries.TryGetValue("Name", out var n))
            {
                return n;
            }

            foreach (var child in node.Keys.Values)
            {
                var t = FindUnitNameEntryFromTdf(child);
                if (t != null)
                {
                    return t;
                }
            }

            return null;
        }

        private static string FindObjectNameFromTdf(TdfNode node)
        {
            if (node == null)
            {
                return null;
            }

            if (node.Entries.TryGetValue("Objectname", out var o))
            {
                return NormalizeObjectName(o);
            }

            foreach (var child in node.Keys.Values)
            {
                var t = FindObjectNameFromTdf(child);
                if (t != null)
                {
                    return t;
                }
            }

            return null;
        }

        private static string NormalizeObjectName(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return null;
            }

            raw = raw.Trim();
            if (raw.EndsWith(".3do", StringComparison.OrdinalIgnoreCase))
            {
                raw = raw.Substring(0, raw.Length - 4);
            }

            return raw;
        }

        private static UnitSideCategory ClassifySideFromTdf(TdfNode root)
        {
            var raw = FindSideRaw(root);
            if (string.IsNullOrWhiteSpace(raw))
            {
                return UnitSideCategory.Other;
            }

            raw = raw.Trim();
            if (raw.Equals("ARM", StringComparison.OrdinalIgnoreCase))
            {
                return UnitSideCategory.Arm;
            }

            if (raw.Equals("CORE", StringComparison.OrdinalIgnoreCase))
            {
                return UnitSideCategory.Core;
            }

            return UnitSideCategory.Other;
        }
    }
}
