namespace Mappy.IO
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    using TAUtil.Hpi;

    public class UnitNameFbiLoader : AbstractHpiLoader<string>
    {
        protected override void LoadFile(HpiArchive archive, HpiArchive.FileInfo file)
        {
            var name = Path.GetFileNameWithoutExtension(file.Name);
            if (!string.IsNullOrEmpty(name))
            {
                this.Records.Add(name);
            }
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
    }
}
