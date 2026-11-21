namespace MappyTest
{
    using System;
    using System.Drawing;

    using Mappy.Collections;
    using Mappy.Data;
    using Mappy.Models;
    using Mappy.Operations;
    using Mappy.Services;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class TerrainBrushOperationTest
    {
        [TestMethod]
        public void OneDragUndoesBothStepsBackToTheOriginalGrids()
        {
            using (var before = new Bitmap(1, 1))
            using (var first = new Bitmap(1, 1))
            using (var second = new Bitmap(1, 1))
            {
                var map = NewMap(32, 32, before, 40);
                var tiles = Snapshot(map.BaseTile.TileGrid);
                var heights = Snapshot(map.BaseTile.HeightGrid);

                map.ApplyTerrainBrush(new[] { Stamp(first, 3, 0, 0) });
                map.ApplyTerrainBrush(new[] { Stamp(second, 9, 0, 0), Stamp(second, 9, 16, 0) });

                Assert.AreSame(second, map.BaseTile.TileGrid.Get(0, 0));
                Assert.AreSame(second, map.BaseTile.TileGrid.Get(16, 0));
                Assert.AreEqual(9, map.BaseTile.HeightGrid.Get(0, 0));
                Assert.AreEqual(9, map.BaseTile.HeightGrid.Get(32, 0));
                Assert.AreEqual(40, map.BaseTile.HeightGrid.Get(0, 32));

                Assert.IsTrue(map.CanUndo);
                map.Undo();
                Assert.IsFalse(map.CanUndo);
                AssertSame(tiles, map.BaseTile.TileGrid);
                AssertSame(heights, map.BaseTile.HeightGrid);

                map.Redo();
                Assert.AreSame(second, map.BaseTile.TileGrid.Get(0, 0));
                Assert.AreSame(second, map.BaseTile.TileGrid.Get(16, 0));
                Assert.AreEqual(9, map.BaseTile.HeightGrid.Get(31, 31));
                Assert.AreEqual(9, map.BaseTile.HeightGrid.Get(32, 0));
                Assert.AreEqual(40, map.BaseTile.HeightGrid.Get(32, 32));
            }
        }

        [TestMethod]
        public void FeaturesInAChangedCellAreRemovedAndUndoBringsThemBack()
        {
            var previousFeatureService = FeatureInstance.FeatureService;
            FeatureInstance.FeatureService = new FeatureService();
            try
            {
                using (var before = new Bitmap(1, 1))
                using (var piece = new Bitmap(1, 1))
                {
                    var map = NewMap(32, 32, before, 40);
                    var inside = Guid.NewGuid();
                    var outside = Guid.NewGuid();
                    map.AddFeatureInstance(new FeatureInstance(inside, "missing", 5, 7));
                    map.AddFeatureInstance(new FeatureInstance(outside, "missing", 40, 7));

                    map.ApplyTerrainBrush(
                        new[] { Stamp(piece, 3, 0, 0) },
                        new[] { new Rectangle(0, 0, 32, 32) });

                    Assert.IsFalse(map.HasFeatureInstanceAt(5, 7));
                    Assert.IsTrue(map.HasFeatureInstanceAt(40, 7));

                    map.Undo();
                    Assert.IsTrue(map.HasFeatureInstanceAt(5, 7));
                    Assert.IsTrue(map.HasFeatureInstanceAt(40, 7));
                    Assert.AreSame(before, map.BaseTile.TileGrid.Get(0, 0));
                }
            }
            finally
            {
                FeatureInstance.FeatureService = previousFeatureService;
            }
        }

        [TestMethod]
        public void AStampAlreadyOnTheMapMatches()
        {
            using (var before = new Bitmap(1, 1))
            using (var piece = new Bitmap(1, 1))
            {
                var map = NewMap(20, 20, before, 40);
                var stamp = Stamp(piece, 7, 8, 8);
                Assert.IsFalse(stamp.Matches(map.BaseTile.TileGrid, map.BaseTile.HeightGrid));

                map.ApplyTerrainBrush(new[] { stamp });
                Assert.IsTrue(stamp.Matches(map.BaseTile.TileGrid, map.BaseTile.HeightGrid));

                map.BaseTile.HeightGrid.Set(20, 20, 8);
                Assert.IsFalse(stamp.Matches(map.BaseTile.TileGrid, map.BaseTile.HeightGrid));
            }
        }

        [TestMethod]
        public void FlushingTheStrokeMakesTheNextOneItsOwnUndo()
        {
            using (var before = new Bitmap(1, 1))
            using (var first = new Bitmap(1, 1))
            using (var second = new Bitmap(1, 1))
            {
                var map = NewMap(32, 32, before, 40);
                map.ApplyTerrainBrush(new[] { Stamp(first, 3, 0, 0) });
                map.FlushTerrainBrush();
                map.ApplyTerrainBrush(new[] { Stamp(second, 9, 16, 0) });

                map.Undo();
                Assert.AreSame(before, map.BaseTile.TileGrid.Get(16, 0));
                Assert.AreSame(first, map.BaseTile.TileGrid.Get(0, 0));
                Assert.AreEqual(3, map.BaseTile.HeightGrid.Get(0, 0));
                Assert.AreEqual(40, map.BaseTile.HeightGrid.Get(32, 0));

                map.Undo();
                Assert.AreSame(before, map.BaseTile.TileGrid.Get(0, 0));
                Assert.AreEqual(40, map.BaseTile.HeightGrid.Get(0, 0));
                Assert.IsFalse(map.CanUndo);
            }
        }

        [TestMethod]
        public void EndingAStrokeRefreshesTheMinimapInTheSameUndo()
        {
            using (var before = SolidTile(Color.Red))
            using (var piece = SolidTile(Color.Blue))
            {
                var source = new MapModel(32, 32);
                GridMethods.Fill(source.Tile.TileGrid, before);
                var map = new UndoableMapModel(source, null, false);
                var original = map.Minimap;

                map.ApplyTerrainBrush(new[] { Stamp(piece, 3, 0, 0) });
                Assert.AreSame(original, map.Minimap);

                map.FlushTerrainBrush();
                var refreshed = map.Minimap;
                Assert.AreNotSame(original, refreshed);

                map.FlushTerrainBrush();
                Assert.AreSame(refreshed, map.Minimap);

                map.Undo();
                Assert.IsFalse(map.CanUndo);
                Assert.AreSame(original, map.Minimap);
                Assert.AreSame(before, map.BaseTile.TileGrid.Get(0, 0));

                map.Redo();
                Assert.AreSame(refreshed, map.Minimap);
                Assert.AreSame(piece, map.BaseTile.TileGrid.Get(0, 0));
            }
        }

        [TestMethod]
        public void ASmallSectionIsTiledAcrossTheCellAndUndoRestoresIt()
        {
            using (var before = new Bitmap(1, 1))
            using (var piece = new Bitmap(1, 1))
            {
                var map = NewMap(32, 32, before, 40);
                var tiles = Snapshot(map.BaseTile.TileGrid);
                var heights = Snapshot(map.BaseTile.HeightGrid);
                var section = new MapTile(8, 8);
                GridMethods.Fill(section.TileGrid, piece);
                GridMethods.Fill(section.HeightGrid, 5);

                map.ApplyTerrainBrush(new[] { TerrainBrushStamp.FromSection(section, 0, 0) });

                Assert.AreSame(piece, map.BaseTile.TileGrid.Get(0, 0));
                Assert.AreSame(piece, map.BaseTile.TileGrid.Get(8, 8));
                Assert.AreSame(before, map.BaseTile.TileGrid.Get(16, 0));
                Assert.AreEqual(5, map.BaseTile.HeightGrid.Get(0, 0));
                Assert.AreEqual(5, map.BaseTile.HeightGrid.Get(16, 16));
                Assert.AreEqual(40, map.BaseTile.HeightGrid.Get(32, 0));

                map.Undo();
                AssertSame(tiles, map.BaseTile.TileGrid);
                AssertSame(heights, map.BaseTile.HeightGrid);
            }
        }

        [TestMethod]
        public void AStampPastTheMapEdgeIsClippedAndUndoRestoresIt()
        {
            using (var before = new Bitmap(1, 1))
            using (var piece = new Bitmap(1, 1))
            {
                var map = NewMap(20, 20, before, 40);
                var tiles = Snapshot(map.BaseTile.TileGrid);
                var heights = Snapshot(map.BaseTile.HeightGrid);

                map.ApplyTerrainBrush(new[] { Stamp(piece, 7, 8, 8) });

                Assert.AreSame(before, map.BaseTile.TileGrid.Get(7, 8));
                Assert.AreSame(piece, map.BaseTile.TileGrid.Get(8, 8));
                Assert.AreSame(piece, map.BaseTile.TileGrid.Get(19, 19));
                Assert.AreEqual(40, map.BaseTile.HeightGrid.Get(15, 16));
                Assert.AreEqual(7, map.BaseTile.HeightGrid.Get(16, 16));
                Assert.AreEqual(7, map.BaseTile.HeightGrid.Get(39, 39));

                map.Undo();
                AssertSame(tiles, map.BaseTile.TileGrid);
                AssertSame(heights, map.BaseTile.HeightGrid);
            }
        }

        private static UndoableMapModel NewMap(int width, int height, Bitmap tile, int heightValue)
        {
            var source = new MapModel(width, height);
            source.Minimap.Dispose();
            source.Minimap = null;
            GridMethods.Fill(source.Tile.TileGrid, tile);
            GridMethods.Fill(source.Tile.HeightGrid, heightValue);
            return new UndoableMapModel(source, null, false);
        }

        private static Bitmap SolidTile(Color colour)
        {
            var bitmap = new Bitmap(32, 32);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.Clear(colour);
            }

            return bitmap;
        }

        private static TerrainBrushStamp Stamp(Bitmap tile, int heightValue, int tileX, int tileY)
        {
            var tiles = new Grid<Bitmap>(16, 16);
            var heights = new Grid<int>(32, 32);
            GridMethods.Fill(tiles, tile);
            GridMethods.Fill(heights, heightValue);
            return new TerrainBrushStamp(tiles, heights, tileX, tileY);
        }

        private static Bitmap[] Snapshot(IGrid<Bitmap> grid)
        {
            var copy = new Bitmap[grid.Width * grid.Height];
            for (var index = 0; index < copy.Length; index++)
            {
                copy[index] = grid[index];
            }

            return copy;
        }

        private static int[] Snapshot(IGrid<int> grid)
        {
            var copy = new int[grid.Width * grid.Height];
            for (var index = 0; index < copy.Length; index++)
            {
                copy[index] = grid[index];
            }

            return copy;
        }

        private static void AssertSame(Bitmap[] expected, IGrid<Bitmap> grid)
        {
            Assert.AreEqual(expected.Length, grid.Width * grid.Height);
            for (var index = 0; index < expected.Length; index++)
            {
                Assert.AreSame(expected[index], grid[index]);
            }
        }

        private static void AssertSame(int[] expected, IGrid<int> grid)
        {
            Assert.AreEqual(expected.Length, grid.Width * grid.Height);
            for (var index = 0; index < expected.Length; index++)
            {
                Assert.AreEqual(expected[index], grid[index]);
            }
        }
    }
}
