namespace Mappy.IO
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Text.RegularExpressions;

    using TAUtil.Hpi;

    public static class UseOnlyTdf
    {
        public const string ArchiveDirectory = "camps\\useonly\\";

        private static readonly Regex BlockName = new Regex(@"\[([^\]]+)\]", RegexOptions.Compiled);

        public static string NormalizeFileName(string useOnlyUnits)
        {
            if (string.IsNullOrWhiteSpace(useOnlyUnits))
            {
                return null;
            }

            var trimmed = useOnlyUnits.Trim().Replace('/', '\\');
            var name = Path.GetFileName(trimmed);
            return string.IsNullOrWhiteSpace(name) ? null : name;
        }

        public static string SuggestFileName(string mapFilePath, string missionName)
        {
            var fromPath = string.IsNullOrWhiteSpace(mapFilePath)
                ? null
                : SanitizeFileName(Path.GetFileNameWithoutExtension(mapFilePath));
            if (!string.IsNullOrEmpty(fromPath))
            {
                return fromPath + ".tdf";
            }

            var fromMission = SanitizeFileName(missionName);
            if (!string.IsNullOrEmpty(fromMission))
            {
                return fromMission + ".tdf";
            }

            return "mission.tdf";
        }

        public static string EnsureFileName(string currentFileName, string mapFilePath, string missionName)
        {
            var existing = NormalizeFileName(currentFileName);
            return existing ?? SuggestFileName(mapFilePath, missionName);
        }

        public static string GetLoosePath(string mapFilePath, string useOnlyFileName)
        {
            var directory = Path.GetDirectoryName(mapFilePath) ?? string.Empty;
            return Path.Combine(directory, "camps", "useonly", useOnlyFileName);
        }

        public static string GetArchivePath(string useOnlyFileName)
        {
            return ArchiveDirectory + useOnlyFileName;
        }

        public static IList<string> Read(Stream stream)
        {
            string text;
            using (var reader = new StreamReader(stream, Encoding.Default, true))
            {
                text = reader.ReadToEnd();
            }

            var names = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (Match match in BlockName.Matches(text))
            {
                var name = match.Groups[1].Value.Trim();
                if (name.Length == 0 || !seen.Add(name))
                {
                    continue;
                }

                names.Add(name);
            }

            return names;
        }

        public static IList<string> ReadLoose(string mapFilePath, string useOnlyUnits)
        {
            var fileName = NormalizeFileName(useOnlyUnits);
            if (fileName == null || string.IsNullOrWhiteSpace(mapFilePath))
            {
                return new List<string>();
            }

            var path = GetLoosePath(mapFilePath, fileName);
            if (!File.Exists(path))
            {
                return new List<string>();
            }

            using (var stream = File.OpenRead(path))
            {
                return Read(stream);
            }
        }

        public static IList<string> ReadHpi(HpiArchive archive, string useOnlyUnits)
        {
            var fileName = NormalizeFileName(useOnlyUnits);
            if (fileName == null || archive == null)
            {
                return new List<string>();
            }

            var info = archive.FindFile(GetArchivePath(fileName));
            if (info == null || info.Size <= 0)
            {
                return new List<string>();
            }

            var buffer = new byte[info.Size];
            archive.Extract(info, buffer);
            using (var stream = new MemoryStream(buffer, false))
            {
                return Read(stream);
            }
        }

        public static void Write(Stream stream, IEnumerable<string> unitNames, Func<string, string> tedClassOf)
        {
            using (var writer = new StreamWriter(stream, Encoding.ASCII))
            {
                string previousClass = null;
                var any = false;
                if (unitNames == null)
                {
                    return;
                }

                foreach (var raw in unitNames)
                {
                    if (string.IsNullOrWhiteSpace(raw))
                    {
                        continue;
                    }

                    var name = raw.Trim();
                    var tedClass = tedClassOf == null ? string.Empty : (tedClassOf(name) ?? string.Empty);
                    if (any && !string.Equals(previousClass, tedClass, StringComparison.OrdinalIgnoreCase))
                    {
                        writer.WriteLine();
                    }

                    writer.Write('[');
                    writer.Write(name);
                    writer.WriteLine("] {}");
                    previousClass = tedClass;
                    any = true;
                }
            }
        }

        public static void WriteLoose(string mapFilePath, string useOnlyUnits, IReadOnlyList<string> unitNames, Func<string, string> tedClassOf)
        {
            var fileName = NormalizeFileName(useOnlyUnits);
            if (fileName == null || string.IsNullOrWhiteSpace(mapFilePath))
            {
                return;
            }

            var path = GetLoosePath(mapFilePath, fileName);
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var tempPath = path + ".mappytemp";
            try
            {
                using (var stream = File.Create(tempPath))
                {
                    Write(stream, unitNames, tedClassOf);
                }

                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                File.Move(tempPath, path);
            }
            catch
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }

                throw;
            }
        }

        private static string SanitizeFileName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            var invalid = Path.GetInvalidFileNameChars();
            var builder = new StringBuilder(name.Length);
            foreach (var ch in name.Trim())
            {
                var skip = false;
                for (var i = 0; i < invalid.Length; i++)
                {
                    if (ch == invalid[i])
                    {
                        skip = true;
                        break;
                    }
                }

                builder.Append(skip ? '_' : ch);
            }

            var sanitized = builder.ToString().Trim();
            return sanitized.Length == 0 ? null : sanitized;
        }
    }
}
