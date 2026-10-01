namespace MappyTest
{
    using System;
    using System.Drawing;
    using System.Windows.Forms;

    using Mappy.Collections;
    using Mappy.Data;
    using Mappy.Models;
    using Mappy.Services;
    using Mappy.UI.Forms;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ResizeMapAnchorTest
    {
        [TestMethod]
        public void AnchorOffsetsPutOddTileOnRightAndBottom()
        {
            var centredGrowth = new ResizeMapOptions(new Size(11, 8), new Point(1, 1));
            Assert.AreEqual(new Point(2, 1), centredGrowth.GetTileOffset(new Size(6, 5)));

            var centredCrop = new ResizeMapOptions(new Size(6, 5), new Point(1, 1));
            Assert.AreEqual(new Point(-2, -1), centredCrop.GetTileOffset(new Size(11, 8)));

            var topLeft = new ResizeMapOptions(new Size(11, 8), new Point(0, 0));
            Assert.AreEqual(Point.Empty, topLeft.GetTileOffset(new Size(6, 5)));

            var bottomRight = new ResizeMapOptions(new Size(11, 8), new Point(2, 2));
            Assert.AreEqual(new Point(5, 3), bottomRight.GetTileOffset(new Size(6, 5)));
        }

        [TestMethod]
        public void ResizeAnchorGridShowsTheActualTileSplit()
        {
            using (var dialog = new NewMapForm(256, 256, "Resize Map", "OK"))
            {
                var grid = (TableLayoutPanel)dialog.Controls.Find("resizeAnchorGrid", true)[0];
                Assert.AreEqual(Point.Empty, dialog.ResizeAnchor);
                Assert.IsFalse(dialog.MoveStandardBorder);

                ((RadioButton)grid.GetControlFromPosition(1, 1)).Checked = true;
                ((TextBox)dialog.Controls.Find("widthTextBox", true)[0]).Text = "261";
                ((TextBox)dialog.Controls.Find("heightTextBox", true)[0]).Text = "259";

                Assert.AreEqual(new Point(1, 1), dialog.ResizeAnchor);
                var readout = dialog.Controls.Find("resizeChangeLabel", true)[0].Text;
                StringAssert.Contains(readout, "Left +2, right +3");
                StringAssert.Contains(readout, "Top +1, bottom +2");

                ((CheckBox)dialog.Controls.Find("moveStandardBorderCheckBox", true)[0]).Checked = true;
                Assert.IsTrue(dialog.MoveStandardBorder);
                Assert.AreEqual("Anchor playable area:", dialog.Controls.Find("resizeAnchorLabel", true)[0].Text);
            }
        }

        [TestMethod]
        public void MovingBorderPreservesItsStripsAndFillsTheirOldPositions()
        {
            using (var fill = new Bitmap(32, 32))
            using (var right = new Bitmap(32, 32))
            using (var bottom = new Bitmap(32, 32))
            using (var corner = new Bitmap(32, 32))
            {
                var source = new MapModel(4, 7);
                source.Minimap.Dispose();
                source.Minimap = null;
                GridMethods.Fill(source.Tile.TileGrid, fill);
                GridMethods.Fill(source.Tile.TileGrid, 3, 0, 1, 3, right);
                GridMethods.Fill(source.Tile.TileGrid, 0, 3, 3, 4, bottom);
                GridMethods.Fill(source.Tile.TileGrid, 3, 3, 1, 4, corner);
                source.Tile.HeightGrid.Set(6, 2, 71);
                source.Tile.HeightGrid.Set(2, 8, 83);
                source.Tile.HeightGrid.Set(7, 13, 94);
                source.Voids.Set(6, 2, true);
                source.Voids.Set(2, 8, true);
                source.Voids.Set(7, 13, true);

                var map = new UndoableMapModel(source, null, false);
                map.ResizeMap(6, 9, 0, 0, true);

                Assert.AreSame(fill, map.BaseTile.TileGrid.Get(3, 1));
                Assert.AreSame(fill, map.BaseTile.TileGrid.Get(1, 3));
                Assert.AreSame(right, map.BaseTile.TileGrid.Get(5, 4));
                Assert.AreSame(bottom, map.BaseTile.TileGrid.Get(4, 7));
                Assert.AreSame(corner, map.BaseTile.TileGrid.Get(5, 8));
                Assert.AreEqual(71, map.BaseTile.HeightGrid.Get(10, 2));
                Assert.AreEqual(83, map.BaseTile.HeightGrid.Get(2, 12));
                Assert.AreEqual(94, map.BaseTile.HeightGrid.Get(11, 17));
                Assert.IsTrue(map.Voids.HasValue(10, 2));
                Assert.IsTrue(map.Voids.HasValue(2, 12));
                Assert.IsTrue(map.Voids.HasValue(11, 17));
                Assert.IsFalse(map.Voids.HasValue(6, 2));
                Assert.IsFalse(map.Voids.HasValue(2, 8));

                map.Undo();
                Assert.AreEqual(4, map.MapWidth);
                Assert.AreSame(right, map.BaseTile.TileGrid.Get(3, 1));
                Assert.AreSame(bottom, map.BaseTile.TileGrid.Get(1, 3));
                Assert.IsTrue(map.Voids.HasValue(7, 13));
                map.Redo();
                Assert.AreSame(corner, map.BaseTile.TileGrid.Get(5, 8));
            }
        }

        [TestMethod]
        public void MovingBorderExtendsNearestTilesAndKeepsAnchorAlignment()
        {
            using (var fill = new Bitmap(32, 32))
            {
                var source = new MapModel(4, 7);
                source.Minimap.Dispose();
                source.Minimap = null;
                GridMethods.Fill(source.Tile.TileGrid, fill);
                // Distinct height values identify every source half-tile, including corners.
                for (var y = 0; y < 14; y++)
                {
                    for (var x = 0; x < 8; x++)
                    {
                        source.Tile.HeightGrid.Set(x, y, (100 * y) + x);
                    }
                }

                var map = new UndoableMapModel(source, null, false);
                map.ResizeMap(6, 10, 1, 1, true);

                Assert.AreEqual(202, map.BaseTile.HeightGrid.Get(4, 4)); // Playable terrain follows the centre anchor.
                Assert.AreEqual(207, map.BaseTile.HeightGrid.Get(11, 4)); // Right edge follows source row 1.
                Assert.AreEqual(6, map.BaseTile.HeightGrid.Get(10, 0)); // Extend above with the first right tile.
                Assert.AreEqual(507, map.BaseTile.HeightGrid.Get(11, 11)); // Extend below with the last right tile.
                Assert.AreEqual(601, map.BaseTile.HeightGrid.Get(3, 12)); // Bottom edge follows source column 0.
                Assert.AreEqual(600, map.BaseTile.HeightGrid.Get(0, 12)); // Extend to the left.
                Assert.AreEqual(605, map.BaseTile.HeightGrid.Get(9, 12)); // Extend to the right.
                Assert.AreEqual(1307, map.BaseTile.HeightGrid.Get(11, 19)); // Bottom-right corner remains intact.
            }
        }

        [TestMethod]
        public void MovingBorderSupportsEveryAnchorForGrowthAndShrinking()
        {
            using (var fill = new Bitmap(32, 32))
            using (var marker = new Bitmap(32, 32))
            using (var corner = new Bitmap(32, 32))
            {
                var source = new MapModel(7, 11);
                source.Minimap.Dispose();
                source.Minimap = null;
                GridMethods.Fill(source.Tile.TileGrid, fill);
                source.Tile.TileGrid.Set(3, 3, marker);
                source.Tile.TileGrid.Set(6, 10, corner);
                source.Tile.HeightGrid.Set(6, 6, 151);
                source.Voids.Set(6, 6, true);
                var sizes = new[] { new Size(9, 14), new Size(5, 9) };
                var expectedX = new[] { new[] { 0, 1, 2 }, new[] { 0, -1, -2 } };
                var expectedY = new[] { new[] { 0, 1, 3 }, new[] { 0, -1, -2 } };

                for (var sizeIndex = 0; sizeIndex < sizes.Length; sizeIndex++)
                {
                    for (var y = 0; y < 3; y++)
                    {
                        for (var x = 0; x < 3; x++)
                        {
                            var options = new ResizeMapOptions(sizes[sizeIndex], new Point(x, y), true);
                            var offset = options.GetTileOffset(new Size(7, 11));
                            var map = new UndoableMapModel(source, null, false);
                            map.ResizeMap(options.Size.Width, options.Size.Height, offset.X, offset.Y, options.MoveStandardBorder);
                            var markerX = 3 + expectedX[sizeIndex][x];
                            var markerY = 3 + expectedY[sizeIndex][y];
                            Assert.AreSame(marker, map.BaseTile.TileGrid.Get(markerX, markerY));
                            Assert.AreEqual(151, map.BaseTile.HeightGrid.Get(markerX * 2, markerY * 2));
                            Assert.IsTrue(map.Voids.HasValue(markerX * 2, markerY * 2));
                            Assert.AreSame(corner, map.BaseTile.TileGrid.Get(options.Size.Width - 1, options.Size.Height - 1));
                        }
                    }
                }
            }
        }

        [TestMethod]
        public void LeavingBorderOptionOffRetainsOrdinaryResizeBehavior()
        {
            using (var fill = new Bitmap(32, 32))
            using (var right = new Bitmap(32, 32))
            {
                var source = new MapModel(4, 7);
                source.Minimap.Dispose();
                source.Minimap = null;
                GridMethods.Fill(source.Tile.TileGrid, fill);
                source.Tile.TileGrid.Set(3, 1, right);
                var map = new UndoableMapModel(source, null, false);
                map.ResizeMap(6, 9, 0, 0);
                Assert.AreSame(right, map.BaseTile.TileGrid.Get(3, 1));
                Assert.AreSame(fill, map.BaseTile.TileGrid.Get(5, 1));
            }
        }

        [TestMethod]
        public void MovingBorderCropsPlayableContentsAndRelocatesBorderContents()
        {
            var previousFeatureService = FeatureInstance.FeatureService;
            FeatureInstance.FeatureService = new FeatureService();
            try
            {
                using (var fill = new Bitmap(32, 32))
                {
                    var source = new MapModel(6, 9);
                    source.Minimap.Dispose();
                    source.Minimap = null;
                    GridMethods.Fill(source.Tile.TileGrid, fill);
                    var croppedFeature = Guid.NewGuid();
                    var borderFeature = Guid.NewGuid();
                    source.AddFeatureInstance(new FeatureInstance(croppedFeature, "missing", 6, 2));
                    source.AddFeatureInstance(new FeatureInstance(borderFeature, "missing", 2, 10));
                    source.Attributes.SetStartPosition(0, 0, new Point(100, 32));
                    source.Attributes.SetStartPosition(0, 1, new Point(164, 32));
                    source.Attributes.SetStartPosition(0, 2, new Point(32, 180));
                    source.Attributes.SetStartPosition(0, 3, new Point(164, 180));
                    var croppedUnit = Guid.NewGuid();
                    var borderUnit = Guid.NewGuid();
                    source.Attributes.AddUnit(0, new SchemaUnit(croppedUnit, "ARMCOM") { XPos = 100, ZPos = 32 });
                    source.Attributes.AddUnit(0, new SchemaUnit(borderUnit, "ARMCOM") { XPos = 164, ZPos = 180 });

                    var map = new UndoableMapModel(source, null, false);
                    map.ResizeMap(4, 7, 0, 0, true);

                    Assert.IsFalse(map.Attributes.GetStartPosition(0, 0).HasValue);
                    Assert.AreEqual(new Point(100, 32), map.Attributes.GetStartPosition(0, 1).Value);
                    Assert.AreEqual(new Point(32, 116), map.Attributes.GetStartPosition(0, 2).Value);
                    Assert.AreEqual(new Point(100, 116), map.Attributes.GetStartPosition(0, 3).Value);
                    Assert.AreEqual(1, map.Attributes.Schemas[0].Units.Count);
                    Assert.AreEqual(100, map.Attributes.GetUnit(0, borderUnit).XPos);
                    Assert.AreEqual(116, map.Attributes.GetUnit(0, borderUnit).ZPos);
                    Assert.AreEqual(1, System.Linq.Enumerable.Count(map.EnumerateFeatureInstances()));
                    Assert.AreEqual(new GridCoordinates(2, 6), map.GetFeatureInstance(borderFeature).Location);

                    map.Undo();
                    Assert.AreEqual(new Point(100, 32), map.Attributes.GetStartPosition(0, 0).Value);
                    Assert.AreEqual(164, map.Attributes.GetUnit(0, borderUnit).XPos);
                    Assert.AreEqual(new GridCoordinates(6, 2), map.GetFeatureInstance(croppedFeature).Location);
                }
            }
            finally
            {
                FeatureInstance.FeatureService = previousFeatureService;
            }
        }

        [TestMethod]
        public void ResizeShiftsTerrainFeaturesStartsUnitsVoidsAndHeight()
        {
            var previousFeatureService = FeatureInstance.FeatureService;
            FeatureInstance.FeatureService = new FeatureService();
            try
            {
                using (var fillTile = new Bitmap(32, 32))
                using (var markedTile = new Bitmap(32, 32))
                {
                    var source = new MapModel(4, 5) { SeaLevel = 42 };
                    source.Minimap.Dispose();
                    source.Minimap = null;
                    GridMethods.Fill(source.Tile.TileGrid, fillTile);
                    source.Tile.TileGrid.Set(1, 1, markedTile);
                    source.Tile.HeightGrid.Set(2, 2, 77);
                    source.Voids.Set(2, 2, true);

                    var featureId = Guid.NewGuid();
                    source.AddFeatureInstance(new FeatureInstance(featureId, "missing", 2, 2));
                    source.Attributes.SetStartPosition(0, 0, new Point(48, 64));
                    var unitId = Guid.NewGuid();
                    source.Attributes.AddUnit(0, new SchemaUnit(unitId, "ARMCOM") { XPos = 48, ZPos = 64 });

                    var map = new UndoableMapModel(source, null, false);
                    map.ResizeMap(6, 7, 1, 1);

                    Assert.AreEqual(6, map.MapWidth);
                    Assert.AreEqual(7, map.MapHeight);
                    Assert.AreSame(markedTile, map.BaseTile.TileGrid.Get(2, 2));
                    Assert.AreEqual(77, map.BaseTile.HeightGrid.Get(4, 4));
                    Assert.IsTrue(map.Voids.HasValue(4, 4));
                    Assert.AreEqual(new GridCoordinates(4, 4), map.GetFeatureInstance(featureId).Location);
                    Assert.AreEqual(new Point(80, 96), map.Attributes.GetStartPosition(0, 0).Value);
                    Assert.AreEqual(80, map.Attributes.GetUnit(0, unitId).XPos);
                    Assert.AreEqual(96, map.Attributes.GetUnit(0, unitId).ZPos);
                    Assert.AreEqual(42, map.SeaLevel);

                    map.Undo();
                    Assert.AreEqual(4, map.MapWidth);
                    Assert.AreSame(markedTile, map.BaseTile.TileGrid.Get(1, 1));
                    Assert.AreEqual(new Point(48, 64), map.Attributes.GetStartPosition(0, 0).Value);

                    map.Redo();
                    Assert.AreEqual(6, map.MapWidth);
                    Assert.AreSame(markedTile, map.BaseTile.TileGrid.Get(2, 2));
                }
            }
            finally
            {
                FeatureInstance.FeatureService = previousFeatureService;
            }
        }

        [TestMethod]
        public void ShrinkingFromLeftCropsContentOutsideTheNewMap()
        {
            using (var fillTile = new Bitmap(32, 32))
            using (var markedTile = new Bitmap(32, 32))
            {
                var source = new MapModel(4, 5);
                source.Minimap.Dispose();
                source.Minimap = null;
                GridMethods.Fill(source.Tile.TileGrid, fillTile);
                source.Tile.TileGrid.Set(1, 1, markedTile);
                source.Attributes.SetStartPosition(0, 0, new Point(0, 32));
                var unitId = Guid.NewGuid();
                source.Attributes.AddUnit(0, new SchemaUnit(unitId, "ARMCOM") { XPos = 0, ZPos = 32 });

                var map = new UndoableMapModel(source, null, false);
                map.ResizeMap(3, 5, -1, 0);

                Assert.AreSame(markedTile, map.BaseTile.TileGrid.Get(0, 1));
                Assert.IsFalse(map.Attributes.GetStartPosition(0, 0).HasValue);
                Assert.AreEqual(0, map.Attributes.Schemas[0].Units.Count);
            }
        }
    }
}
