namespace MappyTest
{
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
