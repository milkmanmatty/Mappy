namespace MappyTest
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;

    using Mappy.Collections;
    using Mappy.Data;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class CompositeHeightGridTest
    {
        [TestMethod]
        public void LiftedSelectionReadsFloatingHeightsNotZeroedBase()
        {
            var baseHeights = new Grid<int>(6, 6);
            GridMethods.Fill(baseHeights, 40);
            GridMethods.Fill(baseHeights, 2, 2, 2, 2, 0);

            var lifted = new MapTile(1, 1);
            GridMethods.Fill(lifted.HeightGrid, 40);
            var floating = new List<Positioned<IMapTile>>
            {
                new Positioned<IMapTile>(lifted, new Point(1, 1)),
            };

            var view = new CompositeHeightGrid(baseHeights, floating);

            Assert.AreEqual(40, view.Get(1, 2));
            Assert.AreEqual(40, view.Get(2, 1));
            Assert.AreEqual(40, view.Get(2, 2));
            Assert.AreEqual(40, view.Get(3, 3));
            Assert.AreEqual(40, view.Get(4, 2));
        }

        [TestMethod]
        public void MovedSelectionLeavesHoleAndFollowsTile()
        {
            var baseHeights = new Grid<int>(6, 6);
            GridMethods.Fill(baseHeights, 50);
            GridMethods.Fill(baseHeights, 0, 0, 2, 2, 0);

            var lifted = new MapTile(1, 1);
            GridMethods.Fill(lifted.HeightGrid, 90);
            var tile = new Positioned<IMapTile>(lifted, new Point(0, 0));
            var floating = new List<Positioned<IMapTile>> { tile };
            var view = new CompositeHeightGrid(baseHeights, floating);

            tile.Location = new Point(1, 1);

            Assert.AreEqual(0, view.Get(0, 0));
            Assert.AreEqual(0, view.Get(1, 1));
            Assert.AreEqual(90, view.Get(2, 2));
            Assert.AreEqual(90, view.Get(3, 3));
            Assert.AreEqual(50, view.Get(4, 2));
        }

        [TestMethod]
        public void LaterFloatingTileWinsOverlap()
        {
            var baseHeights = new Grid<int>(4, 4);
            GridMethods.Fill(baseHeights, 1);

            var lower = new MapTile(1, 1);
            GridMethods.Fill(lower.HeightGrid, 10);
            var upper = new MapTile(1, 1);
            GridMethods.Fill(upper.HeightGrid, 20);

            var floating = new List<Positioned<IMapTile>>
            {
                new Positioned<IMapTile>(lower, new Point(0, 0)),
                new Positioned<IMapTile>(upper, new Point(0, 0)),
            };

            var view = new CompositeHeightGrid(baseHeights, floating);

            Assert.AreEqual(20, view.Get(0, 0));
            Assert.AreEqual(20, view.Get(1, 1));
            Assert.AreEqual(1, view.Get(2, 2));
        }

        [TestMethod]
        public void MergeRestoresCommittedBaseHeights()
        {
            var baseHeights = new Grid<int>(4, 4);
            GridMethods.Fill(baseHeights, 30);
            GridMethods.Fill(baseHeights, 2, 2, 2, 2, 0);

            var lifted = new MapTile(1, 1);
            lifted.HeightGrid.Set(0, 0, 70);
            lifted.HeightGrid.Set(1, 0, 71);
            lifted.HeightGrid.Set(0, 1, 72);
            lifted.HeightGrid.Set(1, 1, 73);

            var floating = new List<Positioned<IMapTile>>
            {
                new Positioned<IMapTile>(lifted, new Point(1, 1)),
            };
            var view = new CompositeHeightGrid(baseHeights, floating);

            GridMethods.Copy(lifted.HeightGrid, baseHeights, 0, 0, 2, 2, 2, 2);
            floating.Clear();

            Assert.AreEqual(70, view.Get(2, 2));
            Assert.AreEqual(71, view.Get(3, 2));
            Assert.AreEqual(72, view.Get(2, 3));
            Assert.AreEqual(73, view.Get(3, 3));
            Assert.AreEqual(30, view.Get(1, 1));
        }

        [TestMethod]
        public void IndexerMatchesCoordinates()
        {
            var baseHeights = new Grid<int>(2, 2);
            baseHeights.Set(1, 0, 8);
            var view = new CompositeHeightGrid(baseHeights, new List<Positioned<IMapTile>>());

            Assert.AreEqual(8, view[1]);
        }

        [TestMethod]
        [ExpectedException(typeof(NotSupportedException))]
        public void IndexerRejectsWrites()
        {
            var baseHeights = new Grid<int>(2, 2);
            var view = new CompositeHeightGrid(baseHeights, new List<Positioned<IMapTile>>());
            view[0] = 1;
        }
    }
}
