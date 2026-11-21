namespace MappyTest
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Mappy.Data;
    using Mappy.TerrainBrush;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using TAUtil.Hpi;

    [TestClass]
    public class TerrainCatalogGateTest
    {
        [TestMethod]
        public void ArchiveBuildsTheMeasuredWorlds()
        {
            var path = Environment.GetEnvironmentVariable("MAPPY_BRUSH_HPI");
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                Assert.Inconclusive("Set MAPPY_BRUSH_HPI to the brush tileset archive to run this test.");
            }

            TerrainCatalog catalog;
            using (var reader = new TerrainSampleReader())
            {
                catalog = TerrainCatalog.Build(ReadSections(path), reader.Read);
            }

            Assert.AreEqual(string.Empty, catalog.Diagnostics, catalog.Diagnostics);
            Assert.AreEqual(Expected.Count, catalog.Worlds.Count, string.Join(", ", catalog.Worlds.Select(world => world.Name)));

            foreach (var expected in Expected)
            {
                var world = catalog.Find(expected.Key);
                Assert.IsNotNull(world, expected.Key);
                CollectionAssert.AreEqual(
                    expected.Value.Bases,
                    world.Bases.Select(x => x.Nominal).ToArray(),
                    expected.Key);
                Assert.AreEqual(Format(expected.Value.Pairs), Format(world.JoinedNominalPairs()), expected.Key);
                foreach (var pair in expected.Value.Pairs)
                {
                    AssertTwelve(world, pair[0], pair[1]);
                }
            }

            var green = catalog.Find("GreenWorld");
            var ridge = green.FindPathStyle("Ridge");
            Assert.IsNotNull(ridge, "GreenWorld Ridge");
            Assert.IsTrue(ridge.PieceCount > 0, "GreenWorld Ridge pieces");
            var river = green.FindPathStyle("River");
            var riverB = green.FindPathStyle("RiverB");
            Assert.IsNotNull(river, "GreenWorld River");
            Assert.IsTrue(river.PieceCount > 0, "GreenWorld River pieces");
            Assert.IsNotNull(riverB, "GreenWorld RiverB");
            Assert.IsTrue(riverB.PieceCount > 0, "GreenWorld RiverB pieces");
            foreach (var name in new[] { "GreenWorld", "Desert", "Mars" })
            {
                Assert.IsTrue(catalog.Find(name).HasSlopes, name + " slopes");
            }
        }

        private static List<SectionInfo> ReadSections(string path)
        {
            var sections = new List<SectionInfo>();
            using (var archive = new HpiArchive(path))
            {
                foreach (var file in archive.GetFilesRecursive("sections"))
                {
                    if (!file.Name.EndsWith(".sct", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var directory = HpiPath.GetDirectoryName(file.FullPath);
                    if (string.IsNullOrEmpty(directory))
                    {
                        continue;
                    }

                    var parts = directory.Split('\\');
                    if (parts.Length < 3)
                    {
                        continue;
                    }

                    sections.Add(new SectionInfo
                    {
                        HpiFileName = path,
                        SctFileName = file.FullPath,
                        World = parts[1],
                        Category = parts[2],
                    });
                }
            }

            return sections;
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
                    world.Name + " " + lowNominal + "~" + highNominal + " mask " + mask);
            }
        }

        private static string Format(IEnumerable<int[]> pairs)
        {
            return string.Join(", ", pairs.Select(pair => pair[0] + "~" + pair[1]));
        }

        private static readonly Dictionary<string, ExpectedWorld> Expected = new Dictionary<string, ExpectedWorld>
        {
            { "Acid", World(new[] { 0, 85 }, Pair(0, 85)) },
            { "Archipelago", World(new[] { 0, 68, 78, 194 }, Pair(0, 68), Pair(0, 78), Pair(68, 78), Pair(78, 194)) },
            { "Crystal", World(new[] { 0, 85 }, Pair(0, 85)) },
            { "Desert", World(new[] { 0, 68, 80, 155, 230 }, Pair(0, 68), Pair(0, 80), Pair(68, 80), Pair(80, 155), Pair(155, 230)) },
            { "GreenWorld", World(new[] { 0, 68, 85, 219 }, Pair(0, 68), Pair(0, 85), Pair(0, 219), Pair(68, 85), Pair(68, 219), Pair(85, 219)) },
            { "Ice", World(new[] { 0, 85, 170 }, Pair(0, 85), Pair(85, 170)) },
            { "Lava", World(new[] { 41, 86, 190 }, Pair(41, 86), Pair(86, 190)) },
            { "Lush", World(new[] { 27, 77, 183, 255 }, Pair(27, 77), Pair(77, 183), Pair(77, 255)) },
            { "Mars", World(new[] { 0, 85, 255 }, Pair(0, 85), Pair(85, 255)) },
            { "Metal", World(new[] { 0, 68, 85, 139, 219, 255 }, Pair(0, 68), Pair(0, 85), Pair(68, 85), Pair(85, 139), Pair(85, 219), Pair(139, 255)) },
        };

        private static int[] Pair(int low, int high)
        {
            return new[] { low, high };
        }

        private static ExpectedWorld World(int[] bases, params int[][] pairs)
        {
            return new ExpectedWorld { Bases = bases, Pairs = pairs };
        }

        private sealed class ExpectedWorld
        {
            public int[] Bases;

            public int[][] Pairs;
        }
    }
}
