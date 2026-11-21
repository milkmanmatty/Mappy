namespace MappyTest
{
    using System.IO;
    using System.Linq;

    using Mappy;
    using Mappy.Data;
    using Mappy.Services;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class SectionServiceTest
    {
        [TestMethod]
        public void CategoriesIncludeSectionsWhoseWorldDiffersOnlyByCase()
        {
            var previous = MappySettings.Settings.SplitTiles;
            MappySettings.Settings.SplitTiles = false;
            try
            {
                var service = new SectionService();
                service.AddSections(new[]
                {
                    new SectionInfo
                    {
                        World = "ZHON",
                        Category = "Shallow_flat",
                        HpiFileName = "ZHON_EXTRA_BONUS_Shallows.hpi",
                        SctFileName = @"sections\ZHON\Shallow_flat\a.sct",
                    },
                    new SectionInfo
                    {
                        World = "Zhon",
                        Category = "Coast Cliffs Jungle",
                        HpiFileName = "ZHON_II.ufo",
                        SctFileName = @"sections\Zhon\Coast Cliffs Jungle\b.sct",
                    },
                    new SectionInfo
                    {
                        World = "Zhon",
                        Category = "Shallow_Jungle_",
                        HpiFileName = "ZHON_EXTRA_BONUS_Shallows_Jungle.hpi",
                        SctFileName = @"sections\Zhon\Shallow_Jungle_\c.sct",
                    },
                });

                var worlds = service.EnumerateWorlds().ToList();
                Assert.AreEqual(1, worlds.Count);

                var categories = service.EnumerateCategories(worlds[0]).ToList();
                CollectionAssert.AreEquivalent(
                    new[] { "Shallow_flat", "Coast Cliffs Jungle", "Shallow_Jungle_" },
                    categories);
            }
            finally
            {
                MappySettings.Settings.SplitTiles = previous;
            }
        }

        [TestMethod]
        public void SplitTilesSeparatesATilesetBySourceArchive()
        {
            var previous = MappySettings.Settings.SplitTiles;
            try
            {
                MappySettings.Settings.SplitTiles = true;
                var service = CreateZhonService();

                var worlds = service.EnumerateWorlds().ToList();
                CollectionAssert.AreEquivalent(
                    new[]
                    {
                        "ZHON (ZHON_EXTRA_BONUS_Shallows.hpi)",
                        "Zhon (ZHON_II.ufo)",
                        "Zhon (ZHON_EXTRA_BONUS_Shallows_Jungle.hpi)",
                    },
                    worlds);

                var allWorlds = service.EnumerateAllWorlds().ToList();
                Assert.AreEqual(1, allWorlds.Count);
                Assert.IsTrue(string.Equals(allWorlds[0], "ZHON", System.StringComparison.InvariantCultureIgnoreCase));

                var categories = service.EnumerateCategories("ZHON (ZHON_EXTRA_BONUS_Shallows.hpi)").ToList();
                CollectionAssert.AreEqual(new[] { "Shallow_flat" }, categories);
            }
            finally
            {
                MappySettings.Settings.SplitTiles = previous;
            }
        }

        [TestMethod]
        public void SplitTilesKeepsTheTilesetFilterOnTheWorldName()
        {
            var previous = MappySettings.Settings.SplitTiles;
            try
            {
                MappySettings.Settings.SplitTiles = true;
                var service = CreateZhonService();
                service.AddSections(new[]
                {
                    new SectionInfo
                    {
                        World = "Metal",
                        Category = "Hills",
                        HpiFileName = @"C:\maps\metal.hpi",
                        SctFileName = @"sections\Metal\Hills\a.sct",
                    },
                });

                service.SetWorldFilter(new[] { "ZHON" });

                CollectionAssert.AreEquivalent(
                    new[]
                    {
                        "ZHON (ZHON_EXTRA_BONUS_Shallows.hpi)",
                        "Zhon (ZHON_II.ufo)",
                        "Zhon (ZHON_EXTRA_BONUS_Shallows_Jungle.hpi)",
                    },
                    service.EnumerateWorlds().ToList());
            }
            finally
            {
                MappySettings.Settings.SplitTiles = previous;
            }
        }

        [TestMethod]
        public void SplitTilesUsesTheFullArchivePathWhenFileNamesCollide()
        {
            var previous = MappySettings.Settings.SplitTiles;
            try
            {
                MappySettings.Settings.SplitTiles = true;
                var service = new SectionService();
                service.AddSections(new[]
                {
                    new SectionInfo
                    {
                        World = "Green",
                        Category = "Water",
                        HpiFileName = @"C:\base\pack.hpi",
                        SctFileName = @"sections\Green\Water\a.sct",
                    },
                    new SectionInfo
                    {
                        World = "Green",
                        Category = "Cliffs",
                        HpiFileName = @"C:\extra\pack.hpi",
                        SctFileName = @"sections\Green\Cliffs\b.sct",
                    },
                });

                CollectionAssert.AreEquivalent(
                    new[]
                    {
                        @"Green (C:\base\pack.hpi)",
                        @"Green (C:\extra\pack.hpi)",
                    },
                    service.EnumerateWorlds().ToList());

                CollectionAssert.AreEqual(
                    new[] { "Cliffs" },
                    service.EnumerateCategories(@"Green (C:\extra\pack.hpi)").ToList());
            }
            finally
            {
                MappySettings.Settings.SplitTiles = previous;
            }
        }

        [TestMethod]
        public void BrushFoldersAreNotListedAsWorlds()
        {
            var previous = MappySettings.Settings.SplitTiles;
            try
            {
                foreach (var split in new[] { false, true })
                {
                    MappySettings.Settings.SplitTiles = split;
                    var service = new SectionService();
                    service.AddSections(new[]
                    {
                        new SectionInfo
                        {
                            World = "GreenWorld",
                            Category = "Hills",
                            HpiFileName = "brush.hpi",
                            SctFileName = @"sections\GreenWorld\Hills\a.sct",
                        },
                        new SectionInfo
                        {
                            World = "BaseTiles",
                            Category = "GreenWorld",
                            HpiFileName = "brush.hpi",
                            SctFileName = @"sections\BaseTiles\GreenWorld\b.sct",
                        },
                        new SectionInfo
                        {
                            World = "coasttiles",
                            Category = "GreenWorld",
                            HpiFileName = "brush.hpi",
                            SctFileName = @"sections\coasttiles\GreenWorld\c.sct",
                        },
                        new SectionInfo
                        {
                            World = "Rivers",
                            Category = "GreenLand",
                            HpiFileName = "brush.hpi",
                            SctFileName = @"sections\Rivers\GreenLand\d.sct",
                        },
                    });

                    var expected = split
                        ? new[] { "GreenWorld (brush.hpi)", "Rivers (brush.hpi)" }
                        : new[] { "GreenWorld", "Rivers" };
                    CollectionAssert.AreEquivalent(expected, service.EnumerateWorlds().ToList());
                    Assert.AreEqual(4, service.EnumerateAll().Count());
                }
            }
            finally
            {
                MappySettings.Settings.SplitTiles = previous;
            }
        }

        [TestMethod]
        public void ExcludedBrushArchiveIsHiddenFromTheSectionList()
        {
            var previousSplit = MappySettings.Settings.SplitTiles;
            var previousExclude = MappySettings.Settings.ExcludeBrushTilesFromSections;
            var previousUrl = MappySettings.Settings.BrushTilesUrl;
            var brushPath = Path.Combine(BrushTiles.InstallDirectory, "brush.hpi");
            try
            {
                MappySettings.Settings.BrushTilesUrl = "https://example.com/brush.hpi";
                MappySettings.Settings.ExcludeBrushTilesFromSections = true;
                foreach (var split in new[] { false, true })
                {
                    MappySettings.Settings.SplitTiles = split;
                    var service = new SectionService();
                    service.AddSections(new[]
                    {
                        new SectionInfo
                        {
                            World = "GreenWorld",
                            Category = "Hills",
                            HpiFileName = brushPath,
                            SctFileName = @"sections\GreenWorld\Hills\a.sct",
                        },
                        new SectionInfo
                        {
                            World = "GreenWorld",
                            Category = "Lakes",
                            HpiFileName = @"C:\maps\green.hpi",
                            SctFileName = @"sections\GreenWorld\Lakes\b.sct",
                        },
                        new SectionInfo
                        {
                            World = "Rivers",
                            Category = "GreenLand",
                            HpiFileName = brushPath,
                            SctFileName = @"sections\Rivers\GreenLand\c.sct",
                        },
                    });

                    var expectedWorlds = split
                        ? new[] { "GreenWorld (green.hpi)" }
                        : new[] { "GreenWorld" };
                    CollectionAssert.AreEquivalent(expectedWorlds, service.EnumerateWorlds().ToList());
                    CollectionAssert.AreEquivalent(
                        new[] { "Lakes" },
                        service.EnumerateCategories(expectedWorlds[0]).ToList());
                    Assert.AreEqual(3, service.EnumerateAll().Count());
                }
            }
            finally
            {
                MappySettings.Settings.SplitTiles = previousSplit;
                MappySettings.Settings.ExcludeBrushTilesFromSections = previousExclude;
                MappySettings.Settings.BrushTilesUrl = previousUrl;
            }
        }

        private static SectionService CreateZhonService()
        {
            var service = new SectionService();
            service.AddSections(new[]
            {
                new SectionInfo
                {
                    World = "ZHON",
                    Category = "Shallow_flat",
                    HpiFileName = "ZHON_EXTRA_BONUS_Shallows.hpi",
                    SctFileName = @"sections\ZHON\Shallow_flat\a.sct",
                },
                new SectionInfo
                {
                    World = "Zhon",
                    Category = "Coast Cliffs Jungle",
                    HpiFileName = "ZHON_II.ufo",
                    SctFileName = @"sections\Zhon\Coast Cliffs Jungle\b.sct",
                },
                new SectionInfo
                {
                    World = "Zhon",
                    Category = "Shallow_Jungle_",
                    HpiFileName = "ZHON_EXTRA_BONUS_Shallows_Jungle.hpi",
                    SctFileName = @"sections\Zhon\Shallow_Jungle_\c.sct",
                },
            });
            return service;
        }
    }
}
