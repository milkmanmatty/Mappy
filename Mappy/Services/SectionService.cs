namespace Mappy.Services
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;

    using Mappy;
    using Mappy.Data;
    using Mappy.IO;

    using TAUtil.Hpi;
    using TAUtil.Sct;

    public class SectionService
    {
        private readonly Dictionary<int, SectionInfo> sections = new Dictionary<int, SectionInfo>();

        private readonly Dictionary<int, Section> sectionsCache = new Dictionary<int, Section>();

        private Dictionary<WorldArchiveKey, string> splitLabels;

        private Dictionary<string, WorldArchiveKey> splitKeyByLabel;

        private Dictionary<WorldArchiveKey, List<KeyValuePair<int, SectionInfo>>> sectionsBySplitKey;

        private int nextId;

        private HashSet<string> worldFilter;

        public event EventHandler SectionsChanged;

        public SectionInfo Get(int id) => this.sections[id];

        public void AddSections(IEnumerable<SectionInfo> sectionsEnumer)
        {
            foreach (var s in sectionsEnumer)
            {
                this.AddSection(s);
            }

            this.SectionsChanged?.Invoke(this, EventArgs.Empty);
        }

        public void NotifySectionsChanged()
        {
            this.SectionsChanged?.Invoke(this, EventArgs.Empty);
        }

        public IEnumerable<string> EnumerateAllWorlds() => this.EnumerateWorldsUnfiltered();

        public IEnumerable<string> EnumerateWorlds()
        {
            if (MappySettings.Settings.SplitTiles)
            {
                this.EnsureSplitIndex();
                return this.splitLabels
                    .Where(x => this.worldFilter == null || this.worldFilter.Contains(x.Key.World))
                    .Select(x => x.Value)
                    .OrderBy(x => x, StringComparer.InvariantCultureIgnoreCase);
            }

            var worlds = this.EnumerateWorldsUnfiltered();
            if (this.worldFilter == null)
            {
                return worlds;
            }

            return worlds.Where(w => this.worldFilter.Contains(w));
        }

        public void SetWorldFilter(ICollection<string> worlds)
        {
            if (worlds == null || worlds.Count == 0)
            {
                this.worldFilter = null;
                return;
            }

            this.worldFilter = new HashSet<string>(worlds, StringComparer.InvariantCultureIgnoreCase);
        }

        public IReadOnlyCollection<string> GetWorldFilter() => this.worldFilter == null ? null : new ReadOnlyCollection<string>(this.worldFilter.ToList());


        public IEnumerable<string> EnumerateCategories(string world) =>
            this.SectionsInSelectedWorld(world)
                .Select(x => x.Value.Category)
                .Distinct(StringComparer.InvariantCultureIgnoreCase)
                .OrderBy(x => x, StringComparer.InvariantCultureIgnoreCase);

        public IEnumerable<KeyValuePair<int, Section>> EnumerateSections(string world, string category)
        {
            return this.EnumerateSectionsInternal(world, category).OrderBy(
                x => x.Value.Name,
                StringComparer.InvariantCultureIgnoreCase);
        }

        private IEnumerable<string> EnumerateWorldsUnfiltered() =>
            this.sections
                .Select(x => x.Value.World)
                .Distinct(StringComparer.InvariantCultureIgnoreCase)
                .OrderBy(x => x, StringComparer.InvariantCultureIgnoreCase);

        private static Section LoadSection(HpiArchive archive, HpiArchive.FileInfo fileInfo)
        {
            var fileBuffer = new byte[fileInfo.Size];
            archive.Extract(fileInfo, fileBuffer);
            using (var s = new SctReader(new MemoryStream(fileBuffer)))
            {
                var section = new Section(archive.FileName, fileInfo.FullPath);
                section.Name = HpiPath.GetFileNameWithoutExtension(fileInfo.Name);
                section.Minimap = SectionFactory.MinimapFromSct(s);
                section.DataWidth = s.DataWidth;
                section.DataHeight = s.DataHeight;

                var directoryString = HpiPath.GetDirectoryName(fileInfo.FullPath);
                Debug.Assert(directoryString != null, "Null directory for section in HPI.");
                var directories = directoryString.Split('\\');
                section.World = directories[1];
                section.Category = directories[2];

                return section;
            }
        }

        private IEnumerable<KeyValuePair<int, Section>> EnumerateSectionsInternal(string world, string category)
        {
            var relevantSections = this.SectionsInSelectedWorld(world)
                .Where(x => string.Equals(x.Value.Category, category, StringComparison.InvariantCultureIgnoreCase));

            var uncachedSections = new List<KeyValuePair<int, SectionInfo>>();
            foreach (var s in relevantSections)
            {
                if (this.sectionsCache.TryGetValue(s.Key, out var v))
                {
                    yield return new KeyValuePair<int, Section>(s.Key, v);
                }
                else
                {
                    uncachedSections.Add(s);
                }
            }

            foreach (var entry in uncachedSections.GroupBy(x => x.Value.HpiFileName))
            {
                var archive = new HpiArchive(entry.Key);
                foreach (var item in entry)
                {
                    var fileInfo = archive.FindFile(item.Value.SctFileName);
                    if (fileInfo != null)
                    {
                        var section = LoadSection(archive, fileInfo);
                        this.sectionsCache.Add(item.Key, section);
                        yield return new KeyValuePair<int, Section>(item.Key, section);
                    }
                }
            }
        }

        private void AddSection(SectionInfo s)
        {
            var id = this.nextId++;
            this.sections[id] = s;
            this.splitLabels = null;
            this.splitKeyByLabel = null;
            this.sectionsBySplitKey = null;
        }

        private IEnumerable<KeyValuePair<int, SectionInfo>> SectionsInSelectedWorld(string selectedWorld)
        {
            if (string.IsNullOrEmpty(selectedWorld))
            {
                return Enumerable.Empty<KeyValuePair<int, SectionInfo>>();
            }

            if (!MappySettings.Settings.SplitTiles)
            {
                return this.sections.Where(
                    x => string.Equals(x.Value.World, selectedWorld, StringComparison.InvariantCultureIgnoreCase));
            }

            this.EnsureSplitIndex();
            if (!this.splitKeyByLabel.TryGetValue(selectedWorld, out var key)
                || !this.sectionsBySplitKey.TryGetValue(key, out var matches))
            {
                return Enumerable.Empty<KeyValuePair<int, SectionInfo>>();
            }

            return matches;
        }

        private void EnsureSplitIndex()
        {
            if (this.splitLabels != null)
            {
                return;
            }

            var grouped = new Dictionary<WorldArchiveKey, List<KeyValuePair<int, SectionInfo>>>(WorldArchiveKeyComparer.Instance);
            foreach (var entry in this.sections)
            {
                var key = new WorldArchiveKey(entry.Value.World, entry.Value.HpiFileName);
                if (!grouped.TryGetValue(key, out var list))
                {
                    list = new List<KeyValuePair<int, SectionInfo>>();
                    grouped.Add(key, list);
                }

                list.Add(entry);
            }

            this.sectionsBySplitKey = grouped;
            this.splitLabels = this.BuildSplitLabels(grouped.Keys);
            this.splitKeyByLabel = new Dictionary<string, WorldArchiveKey>(StringComparer.InvariantCultureIgnoreCase);
            foreach (var entry in this.splitLabels)
            {
                this.splitKeyByLabel[entry.Value] = entry.Key;
            }
        }

        private Dictionary<WorldArchiveKey, string> BuildSplitLabels(IEnumerable<WorldArchiveKey> keys)
        {
            var groups = keys.ToList();

            var ambiguous = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
            foreach (var bucket in groups.GroupBy(key => this.CollisionKey(key.World, key.HpiFileName), StringComparer.InvariantCultureIgnoreCase))
            {
                if (bucket.Count() > 1)
                {
                    ambiguous.Add(bucket.Key);
                }
            }

            var labels = new Dictionary<WorldArchiveKey, string>(WorldArchiveKeyComparer.Instance);
            foreach (var key in groups)
            {
                var fileName = Path.GetFileName(key.HpiFileName);
                var useFullPath = string.IsNullOrEmpty(fileName) || ambiguous.Contains(this.CollisionKey(key.World, key.HpiFileName));
                var archive = useFullPath ? key.HpiFileName : fileName;
                if (string.IsNullOrEmpty(archive))
                {
                    archive = "unknown";
                }

                labels[key] = $"{key.World} ({archive})";
            }

            return labels;
        }

        private string CollisionKey(string world, string hpiFileName)
        {
            return string.Concat(world ?? string.Empty, "\u001f", Path.GetFileName(hpiFileName) ?? string.Empty);
        }

        private sealed class WorldArchiveKey
        {
            public WorldArchiveKey(string world, string hpiFileName)
            {
                this.World = world ?? string.Empty;
                this.HpiFileName = hpiFileName ?? string.Empty;
            }

            public string World { get; }

            public string HpiFileName { get; }
        }

        private sealed class WorldArchiveKeyComparer : IEqualityComparer<WorldArchiveKey>
        {
            public static readonly WorldArchiveKeyComparer Instance = new WorldArchiveKeyComparer();

            public bool Equals(WorldArchiveKey x, WorldArchiveKey y)
            {
                if (ReferenceEquals(x, y))
                {
                    return true;
                }

                if (x == null || y == null)
                {
                    return false;
                }

                return string.Equals(x.World, y.World, StringComparison.InvariantCultureIgnoreCase)
                    && string.Equals(x.HpiFileName, y.HpiFileName, StringComparison.InvariantCultureIgnoreCase);
            }

            public int GetHashCode(WorldArchiveKey obj)
            {
                unchecked
                {
                    var hash = StringComparer.InvariantCultureIgnoreCase.GetHashCode(obj.World ?? string.Empty);
                    hash = (hash * 397) ^ StringComparer.InvariantCultureIgnoreCase.GetHashCode(obj.HpiFileName ?? string.Empty);
                    return hash;
                }
            }
        }
    }
}
