namespace Mappy.Models
{
    using System;
    using System.Globalization;
    using System.Text;
    using System.Text.RegularExpressions;

    public sealed class OamapPackInfo
    {
        private static readonly Regex PackIdPattern = new Regex(
            "^[a-z0-9]+(-[a-z0-9]+)*$",
            RegexOptions.CultureInvariant | RegexOptions.Compiled);

        public string PackId { get; set; }

        public string PackName { get; set; }

        public string Version { get; set; }

        public string Description { get; set; }

        public string AuthorName { get; set; }

        public string AuthorEmail { get; set; }

        public int PackagingRevision { get; set; }

        public string Packager { get; set; }

        public string Stem { get; set; }

        public string MapName { get; set; }

        public string MapDescription { get; set; }

        public int Players { get; set; }

        public static OamapPackInfo CreateDefaults(IReadOnlyMapModel map, string savePath, string packager)
        {
            var baseName = System.IO.Path.GetFileNameWithoutExtension(savePath ?? string.Empty);
            if (string.IsNullOrWhiteSpace(baseName))
            {
                baseName = "map";
            }

            var mission = map?.Attributes?.Name;
            var description = OneLine(map?.Attributes?.Description);
            var stem = SanitizeStem(baseName);
            var maxIdBytes = Math.Min(32, 63 - 1 - Encoding.UTF8.GetByteCount(stem));
            if (maxIdBytes < 1)
            {
                maxIdBytes = 1;
            }

            var title = string.IsNullOrWhiteSpace(mission) ? baseName : mission.Trim();
            title = TrimToChars(OneLine(title), 64);
            if (title.Length == 0)
            {
                title = "Untitled Map";
            }

            var packDescription = TrimToChars(description, 120);
            var mapDescription = TrimToUtf8Bytes(description, 127);

            return new OamapPackInfo
            {
                PackId = ToKebabId(baseName, maxIdBytes),
                PackName = title,
                Version = "1.0",
                Description = packDescription,
                AuthorName = "unknown",
                AuthorEmail = string.Empty,
                PackagingRevision = 1,
                Packager = TrimToChars(string.IsNullOrWhiteSpace(packager) ? "Mappy" : OneLine(packager), 64),
                Stem = stem,
                MapName = title,
                MapDescription = mapDescription,
                Players = SuggestPlayers(map),
            };
        }

        public string Validate()
        {
            var idError = ValidatePackId(this.PackId);
            if (idError != null)
            {
                return idError;
            }

            var nameError = ValidateRequiredLine(this.PackName, "Pack name", 1, 64, false);
            if (nameError != null)
            {
                return nameError;
            }

            var versionError = ValidateRequiredLine(this.Version, "Version", 1, 32, false);
            if (versionError != null)
            {
                return versionError;
            }

            if (!string.IsNullOrWhiteSpace(this.Description))
            {
                var descriptionError = ValidateRequiredLine(this.Description.Trim(), "Pack description", 1, 120, false);
                if (descriptionError != null)
                {
                    return descriptionError;
                }
            }

            var authorError = ValidateRequiredLine(this.AuthorName, "Author name", 1, 128, true);
            if (authorError != null)
            {
                return authorError;
            }

            if (!string.IsNullOrWhiteSpace(this.AuthorEmail) && !IsEmail(this.AuthorEmail.Trim()))
            {
                return "Author email must look like name@example.com.";
            }

            if (this.PackagingRevision < 1 || this.PackagingRevision > 65535)
            {
                return "Packaging revision must be from 1 to 65535.";
            }

            var packagerError = ValidateRequiredLine(this.Packager, "Packager", 1, 64, false);
            if (packagerError != null)
            {
                return packagerError;
            }

            var stemError = ValidateStem(this.Stem);
            if (stemError != null)
            {
                return stemError;
            }

            var mapNameError = ValidateRequiredLine(this.MapName, "Map title", 1, 64, false);
            if (mapNameError != null)
            {
                return mapNameError;
            }

            if (!string.IsNullOrWhiteSpace(this.MapDescription)
                && Encoding.UTF8.GetByteCount(this.MapDescription.Trim()) > 127)
            {
                return "Map description must be one line of at most 127 bytes.";
            }

            if (!string.IsNullOrWhiteSpace(this.MapDescription) && !IsOneLine(this.MapDescription))
            {
                return "Map description must be one line of at most 127 bytes.";
            }

            if (this.Players < 2 || this.Players > 10)
            {
                return "Players must be from 2 to 10.";
            }

            var joined = Encoding.UTF8.GetByteCount(this.Stem.Trim()) + 1 + Encoding.UTF8.GetByteCount(this.PackId.Trim());
            if (joined > 63)
            {
                return "The joined map name (stem@id) must be at most 63 bytes.";
            }

            return null;
        }

        private static int SuggestPlayers(IReadOnlyMapModel map)
        {
            var fromOta = TryParseNumPlayers(map?.Attributes?.NumPlayers);
            if (fromOta.HasValue)
            {
                return fromOta.Value;
            }

            var best = 0;
            if (map?.Attributes?.Schemas != null)
            {
                foreach (var schema in map.Attributes.Schemas)
                {
                    var count = 0;
                    for (var i = 0; i < 10; i++)
                    {
                        if (schema.GetStartPosition(i).HasValue)
                        {
                            count++;
                        }
                    }

                    if (count > best)
                    {
                        best = count;
                    }
                }
            }

            if (best >= 2 && best <= 10)
            {
                return best;
            }

            return 2;
        }

        private static int? TryParseNumPlayers(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return null;
            }

            var matches = Regex.Matches(raw, @"\d+");
            if (matches.Count == 0)
            {
                return null;
            }

            var max = 0;
            foreach (Match match in matches)
            {
                if (!int.TryParse(match.Value, NumberStyles.None, CultureInfo.InvariantCulture, out var value)
                    || value < 2
                    || value > 10)
                {
                    return null;
                }

                if (value > max)
                {
                    max = value;
                }
            }

            return max;
        }

        private static string ValidatePackId(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return "Pack id is required.";
            }

            var trimmed = id.Trim();
            if (Encoding.UTF8.GetByteCount(trimmed) > 32 || !PackIdPattern.IsMatch(trimmed))
            {
                return "Pack id must be lower-case kebab-case, 1 to 32 bytes, with no @.";
            }

            return null;
        }

        private static string ValidateStem(string stem)
        {
            if (string.IsNullOrWhiteSpace(stem))
            {
                return "Map stem is required.";
            }

            var trimmed = stem.Trim();
            if (trimmed.Length != stem.Length && (stem.StartsWith(" ") || stem.EndsWith(" ")))
            {
                return "Map stem must not start or end with a space or a dot.";
            }

            if (Encoding.UTF8.GetByteCount(trimmed) > 40 || !IsValidStem(trimmed))
            {
                return "Map stem must be 1 to 40 bytes of letters, digits, space, and _ - . ' ( ), and must not start or end with a space or a dot.";
            }

            return null;
        }

        private static bool IsValidStem(string stem)
        {
            if (stem.Length == 0 || stem[0] == ' ' || stem[0] == '.' || stem[stem.Length - 1] == ' ' || stem[stem.Length - 1] == '.')
            {
                return false;
            }

            foreach (var c in stem)
            {
                if (!IsStemChar(c))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsStemChar(char c)
        {
            return (c >= 'A' && c <= 'Z')
                || (c >= 'a' && c <= 'z')
                || (c >= '0' && c <= '9')
                || c == ' '
                || c == '_'
                || c == '-'
                || c == '.'
                || c == '\''
                || c == '('
                || c == ')';
        }

        private static string ValidateRequiredLine(string value, string label, int minChars, int maxChars, bool measureBytes)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return label + " is required.";
            }

            var trimmed = value.Trim();
            if (!IsOneLine(trimmed))
            {
                return label + " must be a single line.";
            }

            var length = measureBytes ? Encoding.UTF8.GetByteCount(trimmed) : trimmed.Length;
            if (length < minChars || length > maxChars)
            {
                var unit = measureBytes ? "bytes" : "characters";
                return label + " must be " + minChars + " to " + maxChars + " " + unit + ".";
            }

            return null;
        }

        private static bool IsOneLine(string value)
        {
            return value.IndexOf('\r') < 0 && value.IndexOf('\n') < 0;
        }

        private static bool IsEmail(string value)
        {
            if (!IsOneLine(value) || value.IndexOf(' ') >= 0)
            {
                return false;
            }

            var at = value.IndexOf('@');
            if (at <= 0 || at != value.LastIndexOf('@') || at == value.Length - 1)
            {
                return false;
            }

            var domain = value.Substring(at + 1);
            var dot = domain.LastIndexOf('.');
            return dot > 0 && dot < domain.Length - 1;
        }

        private static string OneLine(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return value.Replace("\r\n", " ").Replace('\r', ' ').Replace('\n', ' ').Trim();
        }

        private static string TrimToChars(string value, int maxChars)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maxChars)
            {
                return value ?? string.Empty;
            }

            return value.Substring(0, maxChars).TrimEnd();
        }

        private static string TrimToUtf8Bytes(string value, int maxBytes)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            if (Encoding.UTF8.GetByteCount(value) <= maxBytes)
            {
                return value;
            }

            var end = value.Length;
            while (end > 0 && Encoding.UTF8.GetByteCount(value.Substring(0, end)) > maxBytes)
            {
                end--;
            }

            return end == 0 ? string.Empty : value.Substring(0, end).TrimEnd();
        }

        private static string SanitizeStem(string raw)
        {
            var sb = new StringBuilder();
            foreach (var c in (raw ?? string.Empty).Trim())
            {
                if (IsStemChar(c) && c != ' ')
                {
                    sb.Append(c);
                }
                else if (c == ' ' || sb.Length > 0)
                {
                    if (sb.Length > 0 && sb[sb.Length - 1] != '_')
                    {
                        sb.Append(c == ' ' ? ' ' : '_');
                    }
                    else if (c == ' ' && sb.Length > 0 && sb[sb.Length - 1] != ' ')
                    {
                        sb.Append(' ');
                    }
                }
            }

            var stem = sb.ToString().Trim(' ', '.', '_');
            if (stem.Length == 0)
            {
                return "map";
            }

            if (stem.Length > 40)
            {
                stem = stem.Substring(0, 40).TrimEnd(' ', '.');
            }

            return stem.Length == 0 ? "map" : stem;
        }

        private static string ToKebabId(string raw, int maxBytes)
        {
            var sb = new StringBuilder();
            foreach (var c in (raw ?? string.Empty).ToLowerInvariant())
            {
                if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9'))
                {
                    sb.Append(c);
                }
                else if (sb.Length > 0 && sb[sb.Length - 1] != '-')
                {
                    sb.Append('-');
                }
            }

            var id = sb.ToString().Trim('-');
            if (id.Length == 0)
            {
                id = "map";
            }

            if (id.Length > maxBytes)
            {
                id = id.Substring(0, maxBytes).TrimEnd('-');
            }

            if (id.Length == 0)
            {
                id = "m";
            }

            return id;
        }
    }
}
