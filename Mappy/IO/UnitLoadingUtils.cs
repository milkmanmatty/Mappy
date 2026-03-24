namespace Mappy.IO
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Mappy.Data;

    public static class UnitLoadingUtils
    {
        public static bool LoadUnitCatalog(
            Action<int> progressCallback,
            Func<bool> cancelCallback,
            out LoadResult<UnitCatalogLoadRecord> result)
        {
            var loader = new UnitFbiCatalogLoader();
            if (!loader.LoadFiles(progressCallback, cancelCallback))
            {
                result = null;
                return false;
            }

            var merged = new Dictionary<string, UnitCatalogLoadRecord>(StringComparer.OrdinalIgnoreCase);
            foreach (var r in loader.Records)
            {
                if (string.IsNullOrWhiteSpace(r.Name))
                {
                    continue;
                }

                var name = r.Name.Trim();
                if (!merged.TryGetValue(name, out var existing))
                {
                    merged[name] = r;
                }
                else
                {
                    var side = existing.Side;
                    if (existing.Side == UnitSideCategory.Other && r.Side != UnitSideCategory.Other)
                    {
                        side = r.Side;
                    }

                    var display = MergeDisplayName(existing.DisplayName, r.DisplayName);
                    merged[name] = new UnitCatalogLoadRecord(name, side, display);
                }
            }

            var distinct = merged.Values.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).ToList();
            progressCallback(100);

            result = new LoadResult<UnitCatalogLoadRecord>
            {
                Records = distinct,
                Errors = loader.HpiErrors,
                FileErrors = loader.FileErrors,
            };
            return true;
        }

        private static string MergeDisplayName(string a, string b)
        {
            if (!string.IsNullOrWhiteSpace(a))
            {
                return a.Trim();
            }

            if (!string.IsNullOrWhiteSpace(b))
            {
                return b.Trim();
            }

            return null;
        }
    }
}
