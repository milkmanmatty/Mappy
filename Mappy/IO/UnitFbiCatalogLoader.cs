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
        private static readonly HashSet<string> UnitFolderNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "units", "ZUnits" };

        protected override void LoadFile(HpiArchive archive, HpiArchive.FileInfo file)
        {
            var name = Path.GetFileNameWithoutExtension(file.Name);
            if (string.IsNullOrEmpty(name))
            {
                return;
            }

            var side = UnitCatalogSide.Unknown;
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
                        side = UnitCatalogSide.Normalize(FindSideRaw(root));
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
            foreach (var f in EnumerateUnitFbis(r.GetRoot()))
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

        private static IEnumerable<HpiArchive.FileInfo> EnumerateUnitFbis(HpiArchive.DirectoryInfo dir)
        {
            foreach (var entry in dir.Entries)
            {
                if (entry is HpiArchive.DirectoryInfo sub)
                {
                    if (UnitFolderNames.Contains(sub.Name))
                    {
                        foreach (var f in GetUnitFbisRecursive(sub))
                        {
                            yield return f;
                        }
                    }

                    foreach (var f in EnumerateUnitFbis(sub))
                    {
                        yield return f;
                    }
                }
            }
        }

        private static IEnumerable<HpiArchive.FileInfo> GetUnitFbisRecursive(HpiArchive.DirectoryInfo dir)
        {
            foreach (var entry in dir.Entries)
            {
                if (entry is HpiArchive.FileInfo fi)
                {
                    if (IsMissionUnitFbi(fi.Name))
                    {
                        yield return fi;
                    }
                }
                else if (entry is HpiArchive.DirectoryInfo di)
                {
                    foreach (var f in GetUnitFbisRecursive(di))
                    {
                        yield return f;
                    }
                }
            }
        }

        private static bool IsMissionUnitFbi(string fileName)
        {
            if (!fileName.EndsWith(".fbi", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            return true;
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
    }
}
