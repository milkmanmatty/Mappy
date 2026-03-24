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
                    merged[name] = new UnitCatalogLoadRecord(name, r.Side);
                }
                else if (existing.Side == UnitSideCategory.Other && r.Side != UnitSideCategory.Other)
                {
                    merged[name] = new UnitCatalogLoadRecord(name, r.Side);
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
    }
}
