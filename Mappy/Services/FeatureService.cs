namespace Mappy.Services
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using Mappy.Data;
    using Mappy.IO;
    using Mappy.IO.Gaf;
    using Mappy.Util;
    using TAUtil.Gaf;
    using TAUtil.Gdi.Bitmap;
    using TAUtil.Hpi;

    public class FeatureService
    {
        private readonly Dictionary<string, FeatureInfo> records;

        private readonly Dictionary<string, Feature> featureCache;

        private readonly List<string> supplementalNames = new List<string>();

        private readonly Dictionary<string, FeatureInfo> supplementalReplaced =
            new Dictionary<string, FeatureInfo>(StringComparer.OrdinalIgnoreCase);

        private string supplementalHpiPath;

        private string supplementalDirectory;

        public FeatureService()
        {
            this.records = new Dictionary<string, FeatureInfo>();
            this.featureCache = new Dictionary<string, Feature>();
        }

        public event EventHandler FeaturesChanged;

        public Feature SelectedFeature { get; set; }

        public string SupplementalHpiPath => this.supplementalHpiPath;

        public void AddFeatures(IEnumerable<FeatureInfo> features)
        {
            foreach (var f in features)
            {
                this.AddFeature(f);
            }

            this.FeaturesChanged?.Invoke(this, EventArgs.Empty);
        }

        public void NotifyFeaturesChanged()
        {
            this.FeaturesChanged?.Invoke(this, EventArgs.Empty);
        }

        public Maybe<Feature> TryGetFeature(string name)
        {
            if (!this.records.TryGetValue(name, out var info))
            {
                return Maybe.None<Feature>();
            }

            return Maybe.From(
                this.EnumerateFeaturesFromInternal(new[] { new KeyValuePair<string, FeatureInfo>(name, info) })
                    .FirstOrDefault());
        }

        public bool TryGetFeatureInfo(string name, out FeatureInfo info)
        {
            info = null;
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            if (this.records.TryGetValue(name, out info))
            {
                return true;
            }

            foreach (var pair in this.records)
            {
                if (string.Equals(pair.Key, name, StringComparison.OrdinalIgnoreCase))
                {
                    info = pair.Value;
                    return true;
                }
            }

            return false;
        }

        public void SetSupplementalHpi(string hpiPath, string directory, IEnumerable<FeatureInfo> features)
        {
            this.ClearSupplementalHpi();
            this.supplementalHpiPath = hpiPath;
            this.supplementalDirectory = directory;
            if (features != null)
            {
                foreach (var feature in features)
                {
                    if (feature == null || string.IsNullOrWhiteSpace(feature.Name))
                    {
                        continue;
                    }

                    if (this.TryGetFeatureInfo(feature.Name, out var existing))
                    {
                        this.supplementalReplaced[feature.Name] = existing;
                    }

                    this.records[feature.Name] = feature;
                    this.RemoveCached(feature.Name);
                    this.supplementalNames.Add(feature.Name);
                }
            }

            this.FeaturesChanged?.Invoke(this, EventArgs.Empty);
        }

        public void ClearSupplementalHpi()
        {
            if (this.supplementalNames.Count == 0
                && this.supplementalHpiPath == null
                && this.supplementalDirectory == null)
            {
                return;
            }

            foreach (var name in this.supplementalNames)
            {
                if (this.supplementalReplaced.TryGetValue(name, out var previous))
                {
                    this.records[previous.Name] = previous;
                }
                else
                {
                    this.RemoveRecord(name);
                }

                this.RemoveCached(name);
            }

            this.supplementalNames.Clear();
            this.supplementalReplaced.Clear();
            this.supplementalHpiPath = null;
            var directory = this.supplementalDirectory;
            this.supplementalDirectory = null;
            if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
            {
                try
                {
                    Directory.Delete(directory, true);
                }
                catch (IOException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                }
            }

            this.FeaturesChanged?.Invoke(this, EventArgs.Empty);
        }

        public IEnumerable<string> EnumerateWorlds() =>
            this.records
                .Select(x => x.Value.World)
                .Distinct(StringComparer.InvariantCultureIgnoreCase)
                .OrderBy(x => x, StringComparer.InvariantCultureIgnoreCase);

        public IEnumerable<string> EnumerateCategories(string world) =>
            this.records
                .Select(x => x.Value)
                .Where(x => string.Equals(x.World, world, StringComparison.InvariantCultureIgnoreCase))
                .Select(x => x.Category)
                .Distinct(StringComparer.InvariantCultureIgnoreCase)
                .OrderBy(x => x, StringComparer.InvariantCultureIgnoreCase);

        public IEnumerable<Feature> EnumerateFeatures(string world, string category)
        {
            return this.EnumerateFeaturesInternal(world, category).OrderBy(x => x.Name, StringComparer.InvariantCultureIgnoreCase);
        }

        private static OffsetBitmap LoadRenderFile(HpiArchive archive, HpiArchive.FileInfo file) =>
            ThreeDoWireframe.FromArchiveFile(archive, file);

        private static OffsetBitmap LoadBitmap(GafEntry[] gaf, string sequenceName)
        {
            var entry = gaf.FirstOrDefault(
                x => string.Equals(x.Name, sequenceName, StringComparison.OrdinalIgnoreCase));
            if (entry == null)
            {
                // skip if the sequence is not in this gaf file
                return null;
            }

            var frame = entry.Frames[0];

            Bitmap bmp;
            if (frame.Data == null || frame.Width == 0 || frame.Height == 0)
            {
                bmp = new Bitmap(50, 50);
            }
            else
            {
                bmp = BitmapConvert.ToBitmap(
                    frame.Data,
                    frame.Width,
                    frame.Height,
                    frame.TransparencyIndex);
            }

            return new OffsetBitmap(-frame.OffsetX, -frame.OffsetY, bmp);
        }

        private static GafEntry[] LoadGafEntries(HpiArchive.FileInfo fileInfo, HpiArchive archive)
        {
            // extract and read the file
            var fileBuffer = new byte[fileInfo.Size];
            archive.Extract(fileInfo, fileBuffer);
            var adapter = new GafEntryArrayAdapter();
            using (var b = new GafReader(new MemoryStream(fileBuffer), adapter))
            {
                b.Read();
            }

            var gaf = adapter.Entries;
            return gaf;
        }

        private static string GetResourceFileName(FeatureInfo info)
        {
            var candidate = !string.IsNullOrWhiteSpace(info.AnimFileName)
                ? info.AnimFileName
                : info.ObjectName;
            if (string.IsNullOrWhiteSpace(candidate))
            {
                return string.Empty;
            }

            return Path.GetFileNameWithoutExtension(candidate);
        }

        private IEnumerable<Feature> EnumerateFeaturesInternal(string world, string category)
        {
            var relevantFeatures = this.records
                .Where(
                    x => string.Equals(x.Value.World, world, StringComparison.InvariantCultureIgnoreCase)
                        && string.Equals(x.Value.Category, category, StringComparison.InvariantCultureIgnoreCase));

            return this.EnumerateFeaturesFromInternal(relevantFeatures);
        }

        private IEnumerable<Feature> EnumerateFeaturesFromInternal(IEnumerable<KeyValuePair<string, FeatureInfo>> relevantFeatures)
        {
            var uncachedFeatures = new List<KeyValuePair<string, FeatureInfo>>();
            foreach (var s in relevantFeatures)
            {
                if (this.featureCache.TryGetValue(s.Key, out var v))
                {
                    yield return v;
                }
                else
                {
                    uncachedFeatures.Add(s);
                }
            }

            var hpis = LoadingUtils.EnumerateSearchHpis().ToList();
            if (!string.IsNullOrEmpty(this.supplementalHpiPath))
            {
                hpis.Add(this.supplementalHpiPath);
            }

            hpis.Reverse();

            if (uncachedFeatures.Count == 0)
            {
                yield break;
            }

            foreach (var hpi in hpis)
            {
                var nextUncachedFeatures = new List<KeyValuePair<string, FeatureInfo>>();
                var animFeatures = uncachedFeatures
                    .Where(x => !string.IsNullOrWhiteSpace(x.Value.AnimFileName))
                    .GroupBy(x => x.Value.AnimFileName);
                var objectFeatures = uncachedFeatures.Where(x => !string.IsNullOrWhiteSpace(x.Value.ObjectName));

                var archive = new HpiArchive(hpi);
                foreach (var entry in animFeatures)
                {
                    var fileInfo = archive.FindFile(HpiPath.Combine("anims", entry.Key + ".gaf"));
                    if (fileInfo == null)
                    {
                        nextUncachedFeatures.AddRange(entry);
                        continue;
                    }

                    var gaf = LoadGafEntries(fileInfo, archive);

                    foreach (var item in entry)
                    {
                        var bitmap = LoadBitmap(gaf, item.Value.SequenceName);
                        if (bitmap == null)
                        {
                            nextUncachedFeatures.Add(item);
                            continue;
                        }

                        var f = new Feature
                        {
                            Name = item.Value.Name,
                            World = item.Value.World,
                            Category = item.Value.Category,
                            Footprint = item.Value.Footprint,
                            Image = bitmap.Bitmap,
                            ResourceFileName = GetResourceFileName(item.Value),
                            Offset = new Point(-bitmap.OffsetX, -bitmap.OffsetY),
                            ReclaimInfo = item.Value.ReclaimInfo,
                            MetalSpotValue = item.Value.MetalSpotValue,
                            Permanent = item.Value.Permanent,
                        };
                        this.featureCache.Add(item.Key, f);
                        yield return f;
                    }
                }

                foreach (var entry in objectFeatures)
                {
                    var objectFile = archive.FindFile(HpiPath.Combine("objects3d", entry.Value.ObjectName + ".3do"));
                    if (objectFile == null)
                    {
                        nextUncachedFeatures.Add(entry);
                        continue;
                    }

                    var bitmap = LoadRenderFile(archive, objectFile);

                    var f = new Feature
                    {
                        Name = entry.Value.Name,
                        World = entry.Value.World,
                        Category = entry.Value.Category,
                        Footprint = entry.Value.Footprint,
                        Image = bitmap.Bitmap,
                        ResourceFileName = GetResourceFileName(entry.Value),
                        Offset = new Point(-bitmap.OffsetX, -bitmap.OffsetY),
                        ReclaimInfo = entry.Value.ReclaimInfo,
                        MetalSpotValue = entry.Value.MetalSpotValue,
                        Permanent = entry.Value.Permanent,
                    };
                    this.featureCache.Add(entry.Key, f);
                    yield return f;
                }

                uncachedFeatures = nextUncachedFeatures;
            }
        }

        private void AddFeature(FeatureInfo f)
        {
            this.records[f.Name] = f;
        }

        private void RemoveRecord(string name)
        {
            string match = null;
            foreach (var key in this.records.Keys)
            {
                if (string.Equals(key, name, StringComparison.OrdinalIgnoreCase))
                {
                    match = key;
                    break;
                }
            }

            if (match != null)
            {
                this.records.Remove(match);
            }
        }

        private void RemoveCached(string name)
        {
            string match = null;
            foreach (var key in this.featureCache.Keys)
            {
                if (string.Equals(key, name, StringComparison.OrdinalIgnoreCase))
                {
                    match = key;
                    break;
                }
            }

            if (match != null)
            {
                this.featureCache.Remove(match);
            }
        }
    }
}
