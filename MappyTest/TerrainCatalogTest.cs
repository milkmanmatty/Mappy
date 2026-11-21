namespace MappyTest
{
    using System.Collections.Generic;
    using System.Linq;

    using Mappy.Data;
    using Mappy.TerrainBrush;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class TerrainCatalogTest
    {
        [TestMethod]
        public void CoastPiecesResolveToTheTwoBasesTheyJoin()
        {
            var samples = new Dictionary<string, TerrainSample>();
            var sections = new List<SectionInfo>
            {
                Base(samples, "Demo", 0, 0, 16),
                Base(samples, "Demo", 85, 85, 8),
            };

            foreach (var mask in TerrainCorners.TransitionMasks)
            {
                var file = @"sections\Demo\CoastTiles\" + CoastName(mask) + ".sct";
                sections.Add(Piece(samples, "Demo", "CoastTiles", file, Heights(mask, 85, 0)));
            }

            var world = Build(sections, samples).Find("demo");

            Assert.IsNotNull(world);
            Assert.AreEqual("Demo", world.Name);
            CollectionAssert.AreEqual(new[] { 0, 85 }, world.Bases.Select(x => x.Nominal).ToArray());
            Assert.AreEqual(8, world.Bases[1].Sections[0].TileWidth);
            Assert.AreEqual("0~85", Format(world.JoinedNominalPairs()));
            AssertTwelve(world, 0, 85);
        }

        [TestMethod]
        public void HighAndLowLabelsInOneFamilyResolveToTheSamePair()
        {
            var samples = new Dictionary<string, TerrainSample>();
            var sections = new List<SectionInfo>
            {
                Base(samples, "Ridge", 85, 85, 16),
                Base(samples, "Ridge", 219, 219, 16),
                Piece(samples, "Ridge", "CliffTiles", "Cliff-High-B.sct", Heights(12, 219, 85)),
                Piece(samples, "Ridge", "CliffTiles", "Cliff-Low-TL.sct", Heights(14, 219, 85)),
            };

            var world = Build(sections, samples).Find("Ridge");

            Assert.AreEqual("85~219", Format(world.JoinedNominalPairs()));
            Assert.IsTrue(world.HasLayout(0, 0, 1, 1));
            Assert.IsTrue(world.HasLayout(0, 1, 1, 1));
            Assert.AreEqual(1, world.Distance(0, 1));
            Assert.AreEqual(1, world.StepToward(0, 1));
        }

        [TestMethod]
        public void ALabelEquallyCloseToTwoBasesIsDropped()
        {
            var samples = new Dictionary<string, TerrainSample>();
            var sections = new List<SectionInfo>
            {
                Base(samples, "Tie", 0, 0, 16),
                Base(samples, "Tie", 10, 10, 16),
                Piece(samples, "Tie", "CoastTiles", "Coast-Water-B.sct", new TerrainSample
                {
                    TileWidth = 16,
                    TileHeight = 16,
                    TopLeft = 5,
                    TopRight = 5,
                    BottomLeft = 5,
                    BottomRight = 5,
                }),
            };

            var catalog = Build(sections, samples);
            var world = catalog.Find("Tie");

            Assert.AreEqual(0, world.JoinedNominalPairs().Count);
            StringAssert.Contains(catalog.Diagnostics, "equally close to two bases");
        }

        private static TerrainCatalog Build(List<SectionInfo> sections, Dictionary<string, TerrainSample> samples)
        {
            return TerrainCatalog.Build(sections, section => samples[section.SctFileName]);
        }

        private static SectionInfo Base(Dictionary<string, TerrainSample> samples, string world, int nominal, int height, int tiles)
        {
            var file = @"sections\" + world + @"\BaseTiles\Base-" + nominal + ".sct";
            var sample = new TerrainSample
            {
                TileWidth = tiles,
                TileHeight = tiles,
                TopLeft = height,
                TopRight = height,
                BottomLeft = height,
                BottomRight = height,
            };
            samples[file] = sample;
            return new SectionInfo
            {
                World = "BaseTiles",
                Category = world,
                HpiFileName = "tiles.hpi",
                SctFileName = file,
            };
        }

        private static SectionInfo Piece(
            Dictionary<string, TerrainSample> samples,
            string world,
            string category,
            string file,
            TerrainSample sample)
        {
            samples[file] = sample;
            return new SectionInfo
            {
                World = category,
                Category = world,
                HpiFileName = "tiles.hpi",
                SctFileName = file,
            };
        }

        private static TerrainSample Heights(int highMask, int high, int low)
        {
            var sample = new TerrainSample { TileWidth = 16, TileHeight = 16 };
            sample.TopLeft = (highMask & 1) != 0 ? high : low;
            sample.TopRight = (highMask & 2) != 0 ? high : low;
            sample.BottomLeft = (highMask & 4) != 0 ? high : low;
            sample.BottomRight = (highMask & 8) != 0 ? high : low;
            return sample;
        }

        private static string CoastName(int landMask)
        {
            switch (landMask)
            {
                case 1:
                    return "Coast-Land-TL";
                case 2:
                    return "Coast-Land-TR";
                case 4:
                    return "Coast-Land-BL";
                case 8:
                    return "Coast-Land-BR";
                case 3:
                    return "Coast-Water-B";
                case 12:
                    return "Coast-Water-T";
                case 5:
                    return "Coast-Water-R";
                case 10:
                    return "Coast-Water-L";
                case 14:
                    return "Coast-Water-TL";
                case 13:
                    return "Coast-Water-TR";
                case 11:
                    return "Coast-Water-BL";
                case 7:
                    return "Coast-Water-BR";
                default:
                    Assert.Fail("No coast name for mask " + landMask);
                    return null;
            }
        }

        private static void AssertTwelve(TerrainWorld world, int lowNominal, int highNominal)
        {
            var low = world.IndexOfNominal(lowNominal);
            var high = world.IndexOfNominal(highNominal);
            foreach (var mask in TerrainCorners.TransitionMasks)
            {
                var corners = new int[4];
                for (var corner = 0; corner < 4; corner++)
                {
                    corners[corner] = (mask & TerrainCorners.Bit(corner)) != 0 ? high : low;
                }

                Assert.IsTrue(
                    world.HasLayout(corners[0], corners[1], corners[2], corners[3]),
                    lowNominal + "~" + highNominal + " mask " + mask);
            }
        }

        private static string Format(List<int[]> pairs)
        {
            return string.Join(", ", pairs.Select(pair => pair[0] + "~" + pair[1]));
        }
    }
}
