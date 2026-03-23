namespace Mappy.IO
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public static class UnitLoadingUtils
    {
        public static bool LoadUnitNames(
            Action<int> progressCallback,
            Func<bool> cancelCallback,
            out LoadResult<string> result)
        {
            var loader = new UnitNameFbiLoader();
            if (!loader.LoadFiles(progressCallback, cancelCallback))
            {
                result = null;
                return false;
            }

            var distinct = loader.Records.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
            progressCallback(100);

            result = new LoadResult<string>
            {
                Records = distinct,
                Errors = loader.HpiErrors,
                FileErrors = loader.FileErrors,
            };
            return true;
        }
    }
}
