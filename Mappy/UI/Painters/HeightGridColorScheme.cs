namespace Mappy.UI.Painters
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.IO;
    using System.Runtime.Serialization;
    using System.Runtime.Serialization.Json;

    internal sealed class HeightGridColorScheme
    {
        private static readonly Dictionary<string, HeightGridColorScheme> SchemeCache =
            new Dictionary<string, HeightGridColorScheme>(StringComparer.OrdinalIgnoreCase);

        private readonly List<HeightRange> ranges;

        private readonly Color? oceanColor;

        private readonly Color? minColor;

        private readonly Color? maxColor;

        private HeightGridColorScheme(List<HeightRange> ranges, Color? oceanColor, Color? minColor, Color? maxColor)
        {
            this.ranges = ranges;
            this.oceanColor = oceanColor;
            this.minColor = minColor;
            this.maxColor = maxColor;
        }

        public static HeightGridColorScheme LoadForMap(string mapFilePath)
        {
            var key = mapFilePath ?? string.Empty;
            if (SchemeCache.TryGetValue(key, out var cached))
            {
                return cached;
            }

            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            if (!string.IsNullOrWhiteSpace(mapFilePath))
            {
                var mapFileName = Path.GetFileNameWithoutExtension(mapFilePath);
                if (!string.IsNullOrWhiteSpace(mapFileName))
                {
                    var mapJsonPath = Path.Combine(baseDir, mapFileName + ".json");
                    if (TryLoadFromPath(mapJsonPath, out var mapScheme))
                    {
                        SchemeCache[key] = mapScheme;
                        return mapScheme;
                    }
                }
            }

            var defaultJsonPath = Path.Combine(baseDir, "default.json");
            if (TryLoadFromPath(defaultJsonPath, out var defaultScheme))
            {
                SchemeCache[key] = defaultScheme;
                return defaultScheme;
            }

            var fallback = CreateFallback();
            SchemeCache[key] = fallback;
            return fallback;
        }

        public bool TryGetColor(int height, int seaLevel, out Color color)
        {
            if (height == 0 && this.minColor.HasValue)
            {
                color = this.minColor.Value;
                return true;
            }

            if (height == 255 && this.maxColor.HasValue)
            {
                color = this.maxColor.Value;
                return true;
            }

            if (height < seaLevel && this.oceanColor.HasValue)
            {
                color = this.oceanColor.Value;
                return true;
            }

            foreach (var range in this.ranges)
            {
                if (height >= range.Min && height <= range.Max)
                {
                    color = range.Color;
                    return true;
                }
            }

            color = Color.Empty;
            return false;
        }

        private static bool TryParseHexColor(string text, out Color color)
        {
            color = Color.Empty;
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            var value = text.Trim();
            if (!value.StartsWith("#", StringComparison.Ordinal))
            {
                return false;
            }

            value = value.Substring(1);
            if (value.Length == 6 &&
                int.TryParse(value.Substring(0, 2), System.Globalization.NumberStyles.HexNumber, null, out var r) &&
                int.TryParse(value.Substring(2, 2), System.Globalization.NumberStyles.HexNumber, null, out var g) &&
                int.TryParse(value.Substring(4, 2), System.Globalization.NumberStyles.HexNumber, null, out var b))
            {
                color = Color.FromArgb(r, g, b);
                return true;
            }

            if (value.Length == 8 &&
                int.TryParse(value.Substring(0, 2), System.Globalization.NumberStyles.HexNumber, null, out var a) &&
                int.TryParse(value.Substring(2, 2), System.Globalization.NumberStyles.HexNumber, null, out var rr) &&
                int.TryParse(value.Substring(4, 2), System.Globalization.NumberStyles.HexNumber, null, out var gg) &&
                int.TryParse(value.Substring(6, 2), System.Globalization.NumberStyles.HexNumber, null, out var bb))
            {
                color = Color.FromArgb(a, rr, gg, bb);
                return true;
            }

            return false;
        }

        private static bool TryLoadFromPath(string path, out HeightGridColorScheme scheme)
        {
            scheme = null;
            if (!File.Exists(path))
            {
                return false;
            }

            try
            {
                using (var stream = File.OpenRead(path))
                {
                    var serializer = new DataContractJsonSerializer(typeof(HeightGridColorConfig));
                    var cfg = serializer.ReadObject(stream) as HeightGridColorConfig;
                    if (cfg == null)
                    {
                        return false;
                    }

                    var parsedRanges = new List<HeightRange>();
                    if (cfg.Ranges != null)
                    {
                        foreach (var range in cfg.Ranges)
                        {
                            if (range == null)
                            {
                                continue;
                            }

                            if (TryParseHexColor(range.Color, out var parsedColor))
                            {
                                var min = Math.Min(range.Min, range.Max);
                                var max = Math.Max(range.Min, range.Max);
                                parsedRanges.Add(new HeightRange(min, max, parsedColor));
                            }
                        }
                    }

                    var hasOcean = TryParseHexColor(cfg.Ocean, out var ocean);
                    var hasMin = TryParseHexColor(cfg.Min, out var minColor);
                    var hasMax = TryParseHexColor(cfg.Max, out var maxColor);

                    scheme = new HeightGridColorScheme(
                        parsedRanges,
                        hasOcean ? ocean : (Color?)null,
                        hasMin ? minColor : (Color?)null,
                        hasMax ? maxColor : (Color?)null);
                    return true;
                }
            }
            catch (IOException)
            {
                return false;
            }
            catch (SerializationException)
            {
                return false;
            }
        }

        private static HeightGridColorScheme CreateFallback()
        {
            return new HeightGridColorScheme(
                new List<HeightRange>(),
                null,
                null,
                null);
        }

        [DataContract]
        private sealed class HeightGridColorConfig
        {
            [DataMember(Name = "ranges")]
            public List<HeightRangeConfig> Ranges { get; set; }

            [DataMember(Name = "ocean")]
            public string Ocean { get; set; }

            [DataMember(Name = "min")]
            public string Min { get; set; }

            [DataMember(Name = "max")]
            public string Max { get; set; }
        }

        [DataContract]
        private sealed class HeightRangeConfig
        {
            [DataMember(Name = "min")]
            public int Min { get; set; }

            [DataMember(Name = "max")]
            public int Max { get; set; }

            [DataMember(Name = "color")]
            public string Color { get; set; }
        }

        private sealed class HeightRange
        {
            public HeightRange(int min, int max, Color color)
            {
                this.Min = min;
                this.Max = max;
                this.Color = color;
            }

            public int Min { get; }

            public int Max { get; }

            public Color Color { get; }
        }
    }
}
