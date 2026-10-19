namespace Mappy.Services
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text;

    using Mappy.Collections;
    using Mappy.Data;
    using Mappy.IO;
    using Mappy.Models;

    using TAUtil.Hpi;
    using TAUtil.Sct;
    using TAUtil.Tdf;
    using TAUtil.Tnt;

    public class MapLoadingService
    {
        private readonly SectionFactory sectionFactory;

        private readonly MapModelFactory mapModelFactory;

        public MapLoadingService(SectionFactory sectionFactory, MapModelFactory mapModelFactory)
        {
            this.sectionFactory = sectionFactory;
            this.mapModelFactory = mapModelFactory;
        }

        public static UndoableMapModel CreateMap(int width, int height)
        {
            var map = new MapModel(width, height);
            GridMethods.Fill(map.Tile.TileGrid, Globals.DefaultTile);
            return new UndoableMapModel(map, null, false);
        }

        public UndoableMapModel CreateFromSct(string filename)
        {
            MapTile t;
            using (var s = new SctReader(filename))
            {
                t = this.sectionFactory.TileFromSct(s);
            }

            return new UndoableMapModel(new MapModel(t), filename, true);
        }

        public UndoableMapModel CreateFromTnt(string filename)
        {
            MapModel m;

            var otaFileName = filename.Substring(0, filename.Length - 4) + ".ota";
            if (File.Exists(otaFileName))
            {
                TdfNode attrs;
                using (var ota = File.OpenRead(otaFileName))
                {
                    attrs = TdfNode.LoadTdf(ota);
                }

                using (var s = new TntReader(filename))
                {
                    m = this.mapModelFactory.FromTntAndOta(s, attrs);
                }

                m.ReplaceUseOnlyUnitNames(UseOnlyTdf.ReadLoose(filename, m.Attributes.UseOnlyUnits));
            }
            else
            {
                using (var s = new TntReader(filename))
                {
                    m = this.mapModelFactory.FromTnt(s);
                }
            }

            return new UndoableMapModel(m, filename, false);
        }

        public UndoableMapModel CreateFromHpi(string hpipath, string mappath)
        {
            return this.CreateFromHpi(hpipath, mappath, false);
        }

        public UndoableMapModel CreateFromHpi(string hpipath, string mappath, bool readOnly)
        {
            MapModel m;

            using (var hpi = new HpiArchive(hpipath))
            {
                var otaPath = HpiPath.ChangeExtension(mappath, ".ota");

                TdfNode n;

                var otaFileInfo = hpi.FindFile(otaPath);
                var otaFileBuffer = new byte[otaFileInfo.Size];
                hpi.Extract(otaFileInfo, otaFileBuffer);
                using (var ota = new MemoryStream(otaFileBuffer))
                {
                    n = TdfNode.LoadTdf(ota);
                }

                var tntFileInfo = hpi.FindFile(mappath);
                var tntFileBuffer = new byte[tntFileInfo.Size];
                hpi.Extract(tntFileInfo, tntFileBuffer);
                using (var s = new TntReader(new MemoryStream(tntFileBuffer)))
                {
                    m = this.mapModelFactory.FromTntAndOta(s, n);
                }

                m.ReplaceUseOnlyUnitNames(UseOnlyTdf.ReadHpi(hpi, m.Attributes.UseOnlyUnits));
                var mapModel = new UndoableMapModel(m, hpipath, readOnly);
                ResourceMarkerSidecar.TryLoadFromArchive(mapModel, hpi, mappath);
                TryAttachOamapInfo(hpi, mappath, mapModel);
                return mapModel;
            }
        }

        public UndoableMapModel CreateFromTntAndOtaBytes(byte[] tntBytes, byte[] otaBytes, string filename)
        {
            TdfNode attrs;
            using (var ota = new MemoryStream(otaBytes))
            {
                attrs = TdfNode.LoadTdf(ota);
            }

            MapModel model;
            using (var tnt = new TntReader(new MemoryStream(tntBytes)))
            {
                model = this.mapModelFactory.FromTntAndOta(tnt, attrs);
            }

            return new UndoableMapModel(model, filename, false);
        }

        private static void TryAttachOamapInfo(HpiArchive hpi, string mappath, UndoableMapModel mapModel)
        {
            try
            {
                var info = hpi.FindFile("oamap.yaml");
                if (info == null || info.Size <= 0)
                {
                    return;
                }

                var buffer = new byte[info.Size];
                hpi.Extract(info, buffer);
                var yaml = Encoding.UTF8.GetString(buffer);
                var manifest = OamapManifestReader.TryRead(yaml);
                if (manifest == null)
                {
                    return;
                }

                var mapName = HpiPath.GetFileNameWithoutExtension(mappath);
                var mapCount = hpi.GetFiles("maps").Count(file => file.Name.EndsWith(".tnt", StringComparison.OrdinalIgnoreCase));
                mapModel.OamapInfo = manifest.ToPackInfo(mapName, mapCount);
            }
            catch (Exception)
            {
            }
        }
    }
}
