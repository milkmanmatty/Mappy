namespace Mappy.IO
{
    using System;
    using System.IO;

    using Mappy.Models;

    using TAUtil.HpiUtil;
    using TAUtil.Tnt;

    /// <summary>
    /// Provides methods for writing out a IMapModel instance
    /// as TNT or HPI.
    /// </summary>
    public static class MapSaver
    {
        public static void SaveTnt(IReadOnlyMapModel map, string filename)
        {
            using (var s = new TntWriter(File.Create(filename)))
            {
                s.WriteTnt(new MapModelTntAdapter(map));
            }
        }

        public static void SaveOta(IReadOnlyMapModel map, string filename)
        {
            var mapDimensions = ComputeMapDimensions512(map);
            using (Stream s = File.Create(filename))
            {
                map.Attributes.WriteOta(s, mapDimensions.Item1, mapDimensions.Item2);
            }
        }

        public static void SaveHpi(IReadOnlyMapModel map, string filename, Func<string, string> useOnlyTedClass = null)
        {
            var namePart = Path.GetFileNameWithoutExtension(filename);

            var tmpTntName = Path.GetTempFileName();
            var tmpOtaName = Path.GetTempFileName();
            string tmpUseOnlyName = null;

            try
            {
                using (var s = new TntWriter(File.Create(tmpTntName)))
                {
                    s.WriteTnt(new MapModelTntAdapter(map));
                }

                var mapDimensions = ComputeMapDimensions512(map);

                using (Stream s = File.Create(tmpOtaName))
                {
                    map.Attributes.WriteOta(s, mapDimensions.Item1, mapDimensions.Item2);
                }

                var useOnlyName = UseOnlyTdf.NormalizeFileName(map.Attributes.UseOnlyUnits);
                if (useOnlyName != null)
                {
                    tmpUseOnlyName = Path.GetTempFileName();
                    using (Stream s = File.Create(tmpUseOnlyName))
                    {
                        UseOnlyTdf.Write(s, map.UseOnlyUnitNames, useOnlyTedClass);
                    }
                }

                var fname = "maps\\" + namePart;

                using (var wr = new HpiWriter(filename, HpiWriter.CompressionMethod.ZLib))
                {
                    wr.AddFile(fname + ".tnt", tmpTntName);
                    wr.AddFile(fname + ".ota", tmpOtaName);
                    if (tmpUseOnlyName != null)
                    {
                        wr.AddFile(UseOnlyTdf.GetArchivePath(useOnlyName), tmpUseOnlyName);
                    }
                }
            }
            finally
            {
                if (File.Exists(tmpTntName))
                {
                    File.Delete(tmpTntName);
                }

                if (File.Exists(tmpOtaName))
                {
                    File.Delete(tmpOtaName);
                }

                if (tmpUseOnlyName != null && File.Exists(tmpUseOnlyName))
                {
                    File.Delete(tmpUseOnlyName);
                }
            }
        }

        private static (int, int) ComputeMapDimensions512(IReadOnlyMapModel map)
        {
            var w = (map.FeatureGridWidth / 32) + ((map.FeatureGridWidth % 32 == 0) ? 0 : 1);
            var h = (map.FeatureGridHeight / 32) + ((map.FeatureGridHeight % 32 == 0) ? 0 : 1);
            return (w, h);
        }
    }
}
