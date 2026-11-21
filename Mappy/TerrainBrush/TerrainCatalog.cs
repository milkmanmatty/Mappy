namespace Mappy.TerrainBrush
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;

    using Mappy.Data;

    using TAUtil.Hpi;

    /// <summary>
    /// Builds brush worlds from the section records Mappy already loaded.
    /// The brush archive lays files out as sections\CoastTiles\GreenWorld, so the
    /// loader's world is the piece family and its category is the terrain world.
    /// Each piece name labels some corners, and the label is matched to a base
    /// by the height of those corners.
    /// </summary>
    public sealed class TerrainCatalog
    {
        public const string MissingTilesetMessage =
            "Tile Painting needs to be configured in Edit -> Preferences before first use";

        private static readonly HashSet<string> PieceCategories = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "CoastTiles",
            "ShallowCoastTiles",
            "ShallowDeepTiles",
            "CliffTiles",
            "CoastDSL",
            "CoastalCliff",
        };

        private const string SlopeCategory = "Slopes";

        private static readonly Dictionary<string, TerrainPathKind> PathCategories =
            new Dictionary<string, TerrainPathKind>(StringComparer.OrdinalIgnoreCase)
        {
            { "Rivers", TerrainPathKind.Channel },
            { "Ridges", TerrainPathKind.Crest },
        };

        /// <summary>
        /// Folders that name a world differently from its BaseTiles folder.
        /// </summary>
        private static readonly Dictionary<string, string> WorldAliases =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "GreenLand", "GreenWorld" },
        };

        private readonly Dictionary<string, TerrainWorld> worldsByName;

        private TerrainCatalog(IReadOnlyList<TerrainWorld> worlds, string diagnostics)
        {
            this.Worlds = worlds;
            this.Diagnostics = diagnostics ?? string.Empty;
            this.worldsByName = new Dictionary<string, TerrainWorld>(StringComparer.OrdinalIgnoreCase);
            foreach (var world in worlds)
            {
                this.worldsByName[world.Name] = world;
            }
        }

        public IReadOnlyList<TerrainWorld> Worlds { get; }

        public string Diagnostics { get; }

        /// <summary>
        /// True for the section folders the brush paints from, which are
        /// not useful as worlds in the Sections tab.
        /// </summary>
        public static bool IsBrushFolder(string world)
        {
            return world != null
                && (string.Equals(world, "BaseTiles", StringComparison.OrdinalIgnoreCase)
                    || PieceCategories.Contains(world));
        }

        public TerrainWorld Find(string worldName)
        {
            if (string.IsNullOrEmpty(worldName))
            {
                return null;
            }

            TerrainWorld world;
            return this.worldsByName.TryGetValue(worldName, out world) ? world : null;
        }

        public static TerrainCatalog Build(IEnumerable<SectionInfo> sections, Func<SectionInfo, TerrainSample> readSample)
        {
            if (sections == null)
            {
                throw new ArgumentNullException(nameof(sections));
            }

            if (readSample == null)
            {
                throw new ArgumentNullException(nameof(readSample));
            }

            var notes = new List<string>();
            var builds = new Dictionary<string, WorldBuild>(StringComparer.OrdinalIgnoreCase);
            foreach (var section in sections)
            {
                var kind = Classify(section);
                if (kind == SectionKind.Ignore || string.IsNullOrEmpty(section.Category))
                {
                    continue;
                }

                string pathStyle = null;
                if (kind == SectionKind.Path)
                {
                    pathStyle = PathStyleName(section.World, HpiPath.GetFileName(section.SctFileName));
                    if (pathStyle == null)
                    {
                        continue;
                    }
                }

                string alias;
                var worldName = WorldAliases.TryGetValue(section.Category, out alias) ? alias : section.Category;
                WorldBuild build;
                if (!builds.TryGetValue(worldName, out build))
                {
                    build = new WorldBuild(worldName);
                    builds.Add(worldName, build);
                }

                if (kind == SectionKind.Base)
                {
                    build.Name = section.Category;
                }

                TerrainSample sample;
                try
                {
                    sample = readSample(section);
                }
                catch (Exception ex)
                {
                    notes.Add(section.Category + " " + section.SctFileName + ": " + ex.Message);
                    continue;
                }

                if (sample.TileWidth <= 0 || sample.TileHeight <= 0)
                {
                    notes.Add(section.Category + " " + section.SctFileName + ": section has no tiles");
                    continue;
                }

                if (kind == SectionKind.Base)
                {
                    int nominal;
                    if (!TryBaseNominal(section.SctFileName, out nominal))
                    {
                        notes.Add(section.Category + " " + section.SctFileName + ": base section is not named Base-<number>");
                        continue;
                    }

                    build.Bases.Add(new BaseBuild(nominal, sample, ToRef(section, sample)));
                }
                else if (kind == SectionKind.Path)
                {
                    build.Paths.Add(new PathBuild(pathStyle, PathCategories[section.World], sample, ToRef(section, sample)));
                }
                else
                {
                    var fileName = HpiPath.GetFileName(section.SctFileName);
                    TerrainPieceName piece;
                    if (!TerrainPieceName.TryParse(fileName, out piece))
                    {
                        notes.Add(section.Category + " " + fileName + ": transition name was not understood");
                        continue;
                    }

                    var pieceBuild = new PieceBuild(piece, sample, ToRef(section, sample));
                    if (kind == SectionKind.Slope)
                    {
                        build.Slopes.Add(pieceBuild);
                    }
                    else
                    {
                        build.Pieces.Add(pieceBuild);
                    }
                }
            }

            var worlds = new List<TerrainWorld>();
            foreach (var build in builds.Values)
            {
                if (build.Bases.Count == 0)
                {
                    continue;
                }

                worlds.Add(Finish(build, notes));
            }

            worlds.Sort((left, right) => string.Compare(left.Name, right.Name, StringComparison.OrdinalIgnoreCase));
            return new TerrainCatalog(worlds, string.Join(Environment.NewLine, notes));
        }

        private static TerrainWorld Finish(WorldBuild build, List<string> notes)
        {
            var bases = CollapseBases(build);
            var layouts = new Dictionary<TerrainWorld.LayoutKey, List<TerrainSectionRef>>();
            foreach (var resolved in ResolveLayouts(build.Name, build.Pieces, bases, notes))
            {
                Add(layouts, resolved.Key, resolved.Value.Section);
            }

            var slopes = new Dictionary<TerrainWorld.LayoutKey, List<TerrainSectionRef>>();
            var plainSlopes = new Dictionary<TerrainWorld.LayoutKey, List<TerrainSectionRef>>();
            foreach (var resolved in ResolveLayouts(build.Name, build.Slopes, bases, notes))
            {
                Add(slopes, resolved.Key, resolved.Value.Section);
                var name = resolved.Value.Name;
                if (name.Transition == null && !name.Prefix.StartsWith("Single", StringComparison.OrdinalIgnoreCase))
                {
                    Add(plainSlopes, resolved.Key, resolved.Value.Section);
                }
            }

            var count = bases.Count;
            var joined = new bool[count, count];
            foreach (var layout in layouts.Keys)
            {
                var distinct = new List<int>(4);
                Consider(distinct, layout.TopLeft);
                Consider(distinct, layout.TopRight);
                Consider(distinct, layout.BottomLeft);
                Consider(distinct, layout.BottomRight);
                if (distinct.Count == 2)
                {
                    joined[distinct[0], distinct[1]] = true;
                    joined[distinct[1], distinct[0]] = true;
                }
            }

            var distance = new int[count, count];
            var stepToward = new int[count, count];
            for (var from = 0; from < count; from++)
            {
                for (var to = 0; to < count; to++)
                {
                    stepToward[from, to] = to;
                    if (from == to)
                    {
                        distance[from, to] = 0;
                    }
                    else
                    {
                        distance[from, to] = joined[from, to] ? 1 : TerrainWorld.Far;
                    }
                }
            }

            for (var via = 0; via < count; via++)
            {
                for (var from = 0; from < count; from++)
                {
                    for (var to = 0; to < count; to++)
                    {
                        var through = distance[from, via] + distance[via, to];
                        if (through < distance[from, to])
                        {
                            distance[from, to] = through;
                            stepToward[from, to] = stepToward[from, via];
                        }
                    }
                }
            }

            return new TerrainWorld(
                build.Name,
                bases,
                Freeze(layouts),
                distance,
                stepToward,
                Freeze(slopes),
                Freeze(plainSlopes),
                BuildPathStyles(build, bases, notes));
        }

        private static List<TerrainPathStyle> BuildPathStyles(WorldBuild build, List<TerrainBase> bases, List<string> notes)
        {
            var styles = new Dictionary<string, TerrainPathStyle>(StringComparer.OrdinalIgnoreCase);
            foreach (var path in build.Paths)
            {
                var label = build.Name + " " + path.Section.SctFileName;
                if (path.Sample.Edges == null)
                {
                    notes.Add(label + ": edge heights were not read");
                    continue;
                }

                var corners = new int[TerrainCorners.Count];
                var ok = true;
                for (var corner = 0; corner < corners.Length; corner++)
                {
                    bool tie;
                    corners[corner] = Nearest(bases, path.Sample.Corner(corner), out tie);
                    ok &= !tie;
                }

                if (!ok)
                {
                    notes.Add(label + ": a corner height is equally close to two bases");
                    continue;
                }

                var mask = TerrainPathShape.Measure(bases, path.Kind, corners, path.Sample.Edges);
                if (mask == TerrainPathShape.NotAPath)
                {
                    notes.Add(label + ": corners are neither one base nor a mouth");
                    continue;
                }

                int ground;
                if (mask == TerrainSides.None
                    && TerrainPathShape.FindMouth(bases, path.Kind, corners, out ground) != TerrainSides.None)
                {
                    notes.Add(label + ": mouth has no path running into it");
                    continue;
                }

                TerrainPathStyle style;
                if (!styles.TryGetValue(path.Style, out style))
                {
                    style = new TerrainPathStyle(path.Style, path.Kind);
                    styles.Add(path.Style, style);
                }

                style.Add(corners, mask, path.Section);
            }

            var sorted = new List<TerrainPathStyle>(styles.Values);
            foreach (var style in sorted)
            {
                foreach (var other in sorted)
                {
                    if (other.Kind == style.Kind && other.PieceCount > style.PieceCount
                        && (style.Fallback == null || other.PieceCount > style.Fallback.PieceCount))
                    {
                        style.Fallback = other;
                    }
                }
            }

            sorted.Sort((left, right) => string.Compare(left.Name, right.Name, StringComparison.OrdinalIgnoreCase));
            return sorted;
        }

        private static void Add(
            Dictionary<TerrainWorld.LayoutKey, List<TerrainSectionRef>> layouts,
            TerrainWorld.LayoutKey key,
            TerrainSectionRef section)
        {
            List<TerrainSectionRef> existing;
            if (!layouts.TryGetValue(key, out existing))
            {
                existing = new List<TerrainSectionRef>();
                layouts.Add(key, existing);
            }

            existing.Add(section);
        }

        private static Dictionary<TerrainWorld.LayoutKey, TerrainSectionRef[]> Freeze(
            Dictionary<TerrainWorld.LayoutKey, List<TerrainSectionRef>> layouts)
        {
            var frozen = new Dictionary<TerrainWorld.LayoutKey, TerrainSectionRef[]>();
            foreach (var layout in layouts)
            {
                frozen.Add(layout.Key, layout.Value.ToArray());
            }

            return frozen;
        }

        private static List<KeyValuePair<TerrainWorld.LayoutKey, PieceBuild>> ResolveLayouts(
            string worldName,
            List<PieceBuild> pieces,
            List<TerrainBase> bases,
            List<string> notes)
        {
            var result = new List<KeyValuePair<TerrainWorld.LayoutKey, PieceBuild>>();
            var groups = new Dictionary<string, List<PieceBuild>>(StringComparer.OrdinalIgnoreCase);
            foreach (var piece in pieces)
            {
                List<PieceBuild> group;
                if (!groups.TryGetValue(piece.Name.Prefix, out group))
                {
                    group = new List<PieceBuild>();
                    groups.Add(piece.Name.Prefix, group);
                }

                group.Add(piece);
            }

            foreach (var group in groups)
            {
                var heights = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
                foreach (var piece in group.Value)
                {
                    for (var corner = 0; corner < TerrainCorners.Count; corner++)
                    {
                        List<int> samples;
                        var label = piece.Name.CornerLabels[corner];
                        if (!heights.TryGetValue(label, out samples))
                        {
                            samples = new List<int>();
                            heights.Add(label, samples);
                        }

                        samples.Add(piece.Sample.Corner(corner));
                    }
                }

                var resolved = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                foreach (var entry in heights)
                {
                    string reason;
                    var index = ResolveLabel(entry.Value, bases, out reason);
                    if (index < 0)
                    {
                        notes.Add(worldName + " " + group.Key + " '" + entry.Key + "': " + reason);
                        continue;
                    }

                    resolved.Add(entry.Key, index);
                }

                var dropped = 0;
                foreach (var piece in group.Value)
                {
                    var corners = new int[TerrainCorners.Count];
                    var ok = true;
                    for (var corner = 0; corner < corners.Length; corner++)
                    {
                        int index;
                        if (!resolved.TryGetValue(piece.Name.CornerLabels[corner], out index))
                        {
                            ok = false;
                            break;
                        }

                        corners[corner] = index;
                    }

                    if (!ok)
                    {
                        dropped++;
                        continue;
                    }

                    if (corners[0] == corners[1] && corners[0] == corners[2] && corners[0] == corners[3])
                    {
                        notes.Add(worldName + " " + piece.Section.SctFileName + ": corners all resolved to " + bases[corners[0]].Name);
                        continue;
                    }

                    var key = new TerrainWorld.LayoutKey(corners[0], corners[1], corners[2], corners[3]);
                    result.Add(new KeyValuePair<TerrainWorld.LayoutKey, PieceBuild>(key, piece));
                }

                if (dropped > 0)
                {
                    notes.Add(worldName + " " + group.Key + ": dropped " + dropped.ToString(CultureInfo.InvariantCulture) + " pieces");
                }
            }

            return result;
        }

        private static List<TerrainBase> CollapseBases(WorldBuild build)
        {
            var grouped = new Dictionary<int, List<BaseBuild>>();
            foreach (var candidate in build.Bases)
            {
                List<BaseBuild> same;
                if (!grouped.TryGetValue(candidate.Nominal, out same))
                {
                    same = new List<BaseBuild>();
                    grouped.Add(candidate.Nominal, same);
                }

                same.Add(candidate);
            }

            var nominals = new List<int>(grouped.Keys);
            nominals.Sort();
            var bases = new List<TerrainBase>(nominals.Count);
            foreach (var nominal in nominals)
            {
                var same = grouped[nominal];
                var heights = new List<int>(same.Count);
                var sections = new List<TerrainSectionRef>(same.Count);
                foreach (var candidate in same)
                {
                    heights.Add(candidate.Sample.TopLeft);
                    sections.Add(candidate.Section);
                }

                heights.Sort();
                bases.Add(new TerrainBase(
                    "Base-" + nominal.ToString(CultureInfo.InvariantCulture),
                    nominal,
                    heights[heights.Count / 2],
                    sections));
            }

            return bases;
        }

        private static int ResolveLabel(List<int> heights, List<TerrainBase> bases, out string reason)
        {
            heights.Sort();
            var median = heights[heights.Count / 2];
            bool tie;
            var medianIndex = Nearest(bases, median, out tie);
            if (tie)
            {
                reason = "median height " + median.ToString(CultureInfo.InvariantCulture) + " is equally close to two bases";
                return -1;
            }

            var agree = 0;
            foreach (var height in heights)
            {
                bool sampleTie;
                var index = Nearest(bases, height, out sampleTie);
                if (!sampleTie && index == medianIndex)
                {
                    agree++;
                }
            }

            if (agree * 4 < heights.Count * 3)
            {
                reason = "only " + agree.ToString(CultureInfo.InvariantCulture) + " of "
                    + heights.Count.ToString(CultureInfo.InvariantCulture) + " corners match " + bases[medianIndex].Name;
                return -1;
            }

            reason = null;
            return medianIndex;
        }

        private static int Nearest(List<TerrainBase> bases, int height, out bool tie)
        {
            var best = 0;
            var bestDistance = Math.Abs(height - bases[0].MeasuredHeight);
            tie = false;
            for (var index = 1; index < bases.Count; index++)
            {
                var candidate = Math.Abs(height - bases[index].MeasuredHeight);
                if (candidate < bestDistance)
                {
                    best = index;
                    bestDistance = candidate;
                    tie = false;
                }
                else if (candidate == bestDistance)
                {
                    tie = true;
                }
            }

            return best;
        }

        private static void Consider(List<int> distinct, int index)
        {
            if (!distinct.Contains(index))
            {
                distinct.Add(index);
            }
        }

        private static TerrainSectionRef ToRef(SectionInfo section, TerrainSample sample)
        {
            return new TerrainSectionRef(section.HpiFileName, section.SctFileName, sample.TileWidth, sample.TileHeight, sample.Edges);
        }

        private static SectionKind Classify(SectionInfo section)
        {
            if (section == null || string.IsNullOrEmpty(section.Category))
            {
                return SectionKind.Ignore;
            }

            if (string.Equals(section.World, "BaseTiles", StringComparison.OrdinalIgnoreCase))
            {
                return SectionKind.Base;
            }

            if (string.Equals(section.World, SlopeCategory, StringComparison.OrdinalIgnoreCase))
            {
                return SectionKind.Slope;
            }

            if (section.World != null && PathCategories.ContainsKey(section.World))
            {
                return SectionKind.Path;
            }

            return PieceCategories.Contains(section.World) ? SectionKind.Piece : SectionKind.Ignore;
        }

        /// <summary>
        /// The style a river or ridge piece belongs to, taken from the first word
        /// of its name (River, RiverB, Ridge), or null for pieces the path brush
        /// does not use. RiverIntersect pieces belong to River. Spacer pieces
        /// break a ridge, and other words (such as Pass) are not paths.
        /// </summary>
        private static string PathStyleName(string category, string fileName)
        {
            var stem = HpiPath.GetFileNameWithoutExtension(fileName);
            if (string.IsNullOrEmpty(stem) || stem.IndexOf("Spacer", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return null;
            }

            var first = stem.Split('-')[0];
            const string Intersect = "Intersect";
            if (first.EndsWith(Intersect, StringComparison.OrdinalIgnoreCase))
            {
                first = first.Substring(0, first.Length - Intersect.Length);
            }

            var singular = category.TrimEnd('s', 'S');
            return first.Length > 0 && first.StartsWith(singular, StringComparison.OrdinalIgnoreCase) ? first : null;
        }

        private static bool TryBaseNominal(string path, out int nominal)
        {
            nominal = 0;
            var stem = HpiPath.GetFileNameWithoutExtension(path);
            if (string.IsNullOrEmpty(stem) || stem.Length <= 5
                || !stem.StartsWith("Base-", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var digits = stem.Substring(5);
            if (digits.Length == 0)
            {
                return false;
            }

            foreach (var character in digits)
            {
                if (character < '0' || character > '9')
                {
                    return false;
                }
            }

            nominal = int.Parse(digits, CultureInfo.InvariantCulture);
            return true;
        }

        private enum SectionKind
        {
            Ignore,
            Base,
            Piece,
            Slope,
            Path,
        }

        private sealed class WorldBuild
        {
            public WorldBuild(string name)
            {
                this.Name = name;
                this.Bases = new List<BaseBuild>();
                this.Pieces = new List<PieceBuild>();
                this.Slopes = new List<PieceBuild>();
                this.Paths = new List<PathBuild>();
            }

            public string Name { get; set; }

            public List<BaseBuild> Bases { get; }

            public List<PieceBuild> Pieces { get; }

            public List<PieceBuild> Slopes { get; }

            public List<PathBuild> Paths { get; }
        }

        private sealed class PathBuild
        {
            public PathBuild(string style, TerrainPathKind kind, TerrainSample sample, TerrainSectionRef section)
            {
                this.Style = style;
                this.Kind = kind;
                this.Sample = sample;
                this.Section = section;
            }

            public string Style { get; }

            public TerrainPathKind Kind { get; }

            public TerrainSample Sample { get; }

            public TerrainSectionRef Section { get; }
        }

        private sealed class BaseBuild
        {
            public BaseBuild(int nominal, TerrainSample sample, TerrainSectionRef section)
            {
                this.Nominal = nominal;
                this.Sample = sample;
                this.Section = section;
            }

            public int Nominal { get; }

            public TerrainSample Sample { get; }

            public TerrainSectionRef Section { get; }
        }

        private sealed class PieceBuild
        {
            public PieceBuild(TerrainPieceName name, TerrainSample sample, TerrainSectionRef section)
            {
                this.Name = name;
                this.Sample = sample;
                this.Section = section;
            }

            public TerrainPieceName Name { get; }

            public TerrainSample Sample { get; }

            public TerrainSectionRef Section { get; }
        }
    }
}
