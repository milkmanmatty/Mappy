namespace Mappy.IO
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text;

    public static class OamapManifestReader
    {
        public static OamapManifest TryRead(string yaml)
        {
            if (string.IsNullOrWhiteSpace(yaml))
            {
                return null;
            }

            try
            {
                var root = new Parser(yaml).Parse();
                if (root?.Map == null)
                {
                    return null;
                }

                var manifest = new OamapManifest();
                manifest.Id = Scalar(root, "id");
                manifest.Name = Scalar(root, "name");
                manifest.Version = Scalar(root, "version");
                manifest.Description = Scalar(root, "description");

                if (root.Map.TryGetValue("author", out var author) && author.Map != null)
                {
                    manifest.AuthorName = Scalar(author, "name");
                    manifest.AuthorEmail = Scalar(author, "email");
                }

                if (root.Map.TryGetValue("packaging", out var packaging) && packaging.Map != null)
                {
                    manifest.Packager = Scalar(packaging, "packager");
                    if (int.TryParse(Scalar(packaging, "revision"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var revision))
                    {
                        manifest.Revision = revision;
                    }
                }

                if (root.Map.TryGetValue("maps", out var maps) && maps.List != null)
                {
                    foreach (var item in maps.List)
                    {
                        if (item?.Map == null)
                        {
                            continue;
                        }

                        var entry = new OamapManifest.MapEntry
                        {
                            Stem = Scalar(item, "stem"),
                            Name = Scalar(item, "name"),
                            Description = Scalar(item, "description"),
                        };
                        if (int.TryParse(Scalar(item, "players"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var players))
                        {
                            entry.Players = players;
                        }

                        if (item.Map.TryGetValue("files", out var files) && files.List != null)
                        {
                            foreach (var file in files.List)
                            {
                                if (!string.IsNullOrWhiteSpace(file?.Scalar))
                                {
                                    entry.Files.Add(file.Scalar.Replace('\\', '/'));
                                }
                            }
                        }

                        if (!string.IsNullOrWhiteSpace(entry.Stem))
                        {
                            manifest.Maps.Add(entry);
                        }
                    }
                }

                return manifest;
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }

        private static string Scalar(YamlNode node, string key)
        {
            if (node?.Map != null
                && node.Map.TryGetValue(key, out var value)
                && value != null
                && value.Scalar != null)
            {
                return value.Scalar;
            }

            return null;
        }

        private sealed class YamlNode
        {
            public string Scalar { get; set; }

            public Dictionary<string, YamlNode> Map { get; set; }

            public List<YamlNode> List { get; set; }
        }

        private sealed class Parser
        {
            private readonly List<Line> lines = new List<Line>();

            private int index;

            public Parser(string yaml)
            {
                var text = yaml.Replace("\r\n", "\n").Replace('\r', '\n');
                if (text.Length > 0 && text[0] == '\uFEFF')
                {
                    text = text.Substring(1);
                }

                var rawLines = text.Split('\n');
                foreach (var raw in rawLines)
                {
                    var stripped = this.StripComment(raw);
                    if (stripped.Trim().Length == 0)
                    {
                        continue;
                    }

                    var indent = 0;
                    while (indent < stripped.Length && stripped[indent] == ' ')
                    {
                        indent++;
                    }

                    while (indent < stripped.Length && stripped[indent] == '\t')
                    {
                        indent += 2;
                    }

                    var content = stripped.Trim();
                    if (content == "---" || content == "...")
                    {
                        continue;
                    }

                    this.lines.Add(new Line { Indent = indent, Content = content });
                }
            }

            public YamlNode Parse()
            {
                if (this.lines.Count == 0)
                {
                    return null;
                }

                return this.ParseNode(this.lines[0].Indent);
            }

            private YamlNode ParseNode(int minIndent)
            {
                if (this.index >= this.lines.Count || this.lines[this.index].Indent < minIndent)
                {
                    return null;
                }

                if (this.lines[this.index].Content.StartsWith("-", StringComparison.Ordinal))
                {
                    return this.ParseSequence(this.lines[this.index].Indent);
                }

                return this.ParseMapping(this.lines[this.index].Indent);
            }

            private YamlNode ParseMapping(int indent)
            {
                var map = new Dictionary<string, YamlNode>(StringComparer.Ordinal);
                while (this.index < this.lines.Count
                    && this.lines[this.index].Indent == indent
                    && !this.lines[this.index].Content.StartsWith("-", StringComparison.Ordinal))
                {
                    this.ReadKey(map, indent);
                }

                return new YamlNode { Map = map };
            }

            private YamlNode ParseSequence(int indent)
            {
                var list = new List<YamlNode>();
                while (this.index < this.lines.Count
                    && this.lines[this.index].Indent == indent
                    && this.lines[this.index].Content.StartsWith("-", StringComparison.Ordinal))
                {
                    var content = this.lines[this.index].Content.Substring(1).Trim();
                    if (content.Length == 0)
                    {
                        this.index++;
                        list.Add(this.ParseNode(indent + 1) ?? new YamlNode());
                        continue;
                    }

                    if (this.IndexOfColon(content) >= 0)
                    {
                        var map = new Dictionary<string, YamlNode>(StringComparer.Ordinal);
                        this.ReadKey(map, indent);
                        while (this.index < this.lines.Count && this.lines[this.index].Indent > indent)
                        {
                            this.ReadKey(map, this.lines[this.index].Indent);
                        }

                        list.Add(new YamlNode { Map = map });
                        continue;
                    }

                    this.index++;
                    list.Add(new YamlNode { Scalar = this.Unquote(content) });
                }

                return new YamlNode { List = list };
            }

            private void ReadKey(Dictionary<string, YamlNode> map, int keyIndent)
            {
                var content = this.lines[this.index].Content;
                this.index++;
                if (content.StartsWith("-", StringComparison.Ordinal))
                {
                    content = content.Substring(1).Trim();
                }

                var colon = this.IndexOfColon(content);
                if (colon < 0)
                {
                    throw new InvalidOperationException("Expected a key.");
                }

                var key = this.Unquote(content.Substring(0, colon).Trim());
                var rest = content.Substring(colon + 1).Trim();
                YamlNode value;
                if (rest.Length == 0)
                {
                    value = this.ParseNode(keyIndent + 1) ?? new YamlNode { Scalar = string.Empty };
                }
                else if (rest.StartsWith("{", StringComparison.Ordinal) && rest.EndsWith("}", StringComparison.Ordinal))
                {
                    value = this.ParseFlow(rest);
                }
                else
                {
                    value = new YamlNode { Scalar = this.Unquote(rest) };
                }

                map[key] = value;
            }

            private YamlNode ParseFlow(string text)
            {
                var inner = text.Substring(1, text.Length - 2);
                var map = new Dictionary<string, YamlNode>(StringComparer.Ordinal);
                foreach (var part in this.SplitFlow(inner))
                {
                    var piece = part.Trim();
                    if (piece.Length == 0)
                    {
                        continue;
                    }

                    var colon = this.IndexOfColon(piece);
                    if (colon < 0)
                    {
                        continue;
                    }

                    var key = this.Unquote(piece.Substring(0, colon).Trim());
                    var value = this.Unquote(piece.Substring(colon + 1).Trim());
                    map[key] = new YamlNode { Scalar = value };
                }

                return new YamlNode { Map = map };
            }

            private IEnumerable<string> SplitFlow(string text)
            {
                var current = new StringBuilder();
                var inQuote = false;
                for (var i = 0; i < text.Length; i++)
                {
                    var ch = text[i];
                    if (inQuote)
                    {
                        current.Append(ch);
                        if (ch == '\\' && i + 1 < text.Length)
                        {
                            current.Append(text[++i]);
                            continue;
                        }

                        if (ch == '"')
                        {
                            inQuote = false;
                        }

                        continue;
                    }

                    if (ch == '"')
                    {
                        inQuote = true;
                        current.Append(ch);
                        continue;
                    }

                    if (ch == ',')
                    {
                        yield return current.ToString();
                        current.Clear();
                        continue;
                    }

                    current.Append(ch);
                }

                yield return current.ToString();
            }

            private int IndexOfColon(string text)
            {
                var inQuote = false;
                for (var i = 0; i < text.Length; i++)
                {
                    var ch = text[i];
                    if (inQuote)
                    {
                        if (ch == '\\')
                        {
                            i++;
                            continue;
                        }

                        if (ch == '"')
                        {
                            inQuote = false;
                        }

                        continue;
                    }

                    if (ch == '"')
                    {
                        inQuote = true;
                        continue;
                    }

                    if (ch == ':')
                    {
                        return i;
                    }
                }

                return -1;
            }

            private string StripComment(string line)
            {
                var inQuote = false;
                for (var i = 0; i < line.Length; i++)
                {
                    var ch = line[i];
                    if (inQuote)
                    {
                        if (ch == '\\')
                        {
                            i++;
                            continue;
                        }

                        if (ch == '"')
                        {
                            inQuote = false;
                        }

                        continue;
                    }

                    if (ch == '"')
                    {
                        inQuote = true;
                        continue;
                    }

                    if (ch == '#')
                    {
                        return line.Substring(0, i);
                    }
                }

                return line;
            }

            private string Unquote(string value)
            {
                if (value == null)
                {
                    return null;
                }

                if (value.Length >= 2 && value[0] == '"' && value[value.Length - 1] == '"')
                {
                    var inner = value.Substring(1, value.Length - 2);
                    return inner.Replace("\\\"", "\"").Replace("\\\\", "\\");
                }

                return value;
            }

            private struct Line
            {
                public int Indent;

                public string Content;
            }
        }
    }
}
