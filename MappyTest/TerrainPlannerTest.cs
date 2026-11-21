namespace MappyTest
{
    using System.Collections.Generic;
    using System.Linq;

    using Mappy.Data;
    using Mappy.TerrainBrush;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class TerrainPlannerTest
    {
        [TestMethod]
        public void OneCellOnFlatLandDrawsACoastRing()
        {
            var world = World(new[] { 0, 85 }, Pair(0, 85));
            var plan = TerrainPlanner.Plan(world, 5, 5, 1, 1, 1, 0, (x, y) => 1);

            Assert.IsTrue(plan.Accepted, plan.Refusal);
            AssertCorners(plan, 1, 1, 0, 0, 0, 0);
            AssertCorners(plan, 1, 0, 1, 1, 0, 0);
            AssertCorners(plan, 1, 2, 0, 0, 1, 1);
            AssertCorners(plan, 0, 1, 1, 0, 1, 0);
            AssertCorners(plan, 2, 1, 0, 1, 0, 1);
            AssertCorners(plan, 0, 0, 1, 1, 1, 0);
            AssertValid(world, plan);
        }

        [TestMethod]
        public void OneCornerMakesAPondFromFourCoastPieces()
        {
            var world = World(new[] { 0, 85 }, Pair(0, 85));
            var plan = TerrainPlanner.PlanCorners(world, 4, 4, 2, 2, 1, 0, (x, y) => 1);

            Assert.IsTrue(plan.Accepted, plan.Refusal);
            Assert.AreEqual(4, plan.Cells.Count);
            AssertCorners(plan, 1, 1, 1, 1, 1, 0);
            AssertCorners(plan, 2, 1, 1, 1, 0, 1);
            AssertCorners(plan, 1, 2, 1, 0, 1, 1);
            AssertCorners(plan, 2, 2, 0, 1, 1, 1);
            AssertValid(world, plan);
        }

        [TestMethod]
        public void DraggingAlongCornersMakesAChannelWithNoFlatCell()
        {
            var world = World(new[] { 0, 85 }, Pair(0, 85));
            var corners = new int[7, 5];
            for (var y = 0; y < 5; y++)
            {
                for (var x = 0; x < 7; x++)
                {
                    corners[x, y] = 1;
                }
            }

            for (var x = 2; x <= 4; x++)
            {
                var plan = TerrainPlanner.PlanCorners(world, 6, 4, x, 2, 1, 0, (cx, cy) => corners[cx, cy]);
                Apply(plan, corners);
                AssertValid(world, plan);
            }

            Assert.AreEqual(0, corners[2, 2]);
            Assert.AreEqual(0, corners[3, 2]);
            Assert.AreEqual(0, corners[4, 2]);
            for (var y = 0; y < 5; y++)
            {
                for (var x = 0; x < 7; x++)
                {
                    if (y != 2 || x < 2 || x > 4)
                    {
                        Assert.AreEqual(1, corners[x, y], x + "," + y);
                    }
                }
            }
        }

        [TestMethod]
        public void PaintingTheNextCellExtendsTheCoast()
        {
            var world = World(new[] { 0, 85 }, Pair(0, 85));
            var corners = new int[6, 6];
            for (var y = 0; y < 6; y++)
            {
                for (var x = 0; x < 6; x++)
                {
                    corners[x, y] = 1;
                }
            }

            var first = TerrainPlanner.Plan(world, 5, 5, 1, 1, 1, 0, (x, y) => corners[x, y]);
            Apply(first, corners);
            var second = TerrainPlanner.Plan(world, 5, 5, 2, 1, 1, 0, (x, y) => corners[x, y]);
            Apply(second, corners);

            Assert.AreEqual(0, corners[1, 1]);
            Assert.AreEqual(0, corners[2, 1]);
            Assert.AreEqual(0, corners[3, 1]);
            Assert.AreEqual(1, corners[4, 1]);
            Assert.AreEqual(1, corners[5, 1]);
            Assert.AreEqual(1, corners[1, 0]);
            Assert.AreEqual(0, corners[2, 2]);
            AssertCorners(second, 2, 1, 0, 0, 0, 0);
            AssertCorners(second, 3, 1, 0, 1, 0, 1);
            AssertValid(world, second);
        }

        [TestMethod]
        public void ADiagonalLayoutIsResolvedTowardThePaintedBase()
        {
            var world = World(new[] { 0, 85 }, Pair(0, 85));
            var plan = TerrainPlanner.Plan(
                world,
                4,
                4,
                1,
                1,
                1,
                0,
                (x, y) => x == 0 && y == 0 ? 0 : 1);

            Assert.IsTrue(plan.Accepted, plan.Refusal);
            AssertCorners(plan, 1, 1, 0, 0, 0, 0);
            AssertCorners(plan, 0, 0, 0, 1, 0, 0);
            AssertValid(world, plan);
            foreach (var cell in plan.Cells)
            {
                var diagonal = cell.TopLeft == cell.BottomRight
                    && cell.TopRight == cell.BottomLeft
                    && cell.TopLeft != cell.TopRight;
                Assert.IsFalse(diagonal, cell.X + "," + cell.Y);
            }
        }

        [TestMethod]
        public void APlateauOnWaterKeepsALandStripBetweenThem()
        {
            var world = World(new[] { 0, 85, 219 }, Pair(0, 85), Pair(85, 219));
            var plan = TerrainPlanner.Plan(world, 7, 7, 3, 3, 1, 2, (x, y) => 0);

            Assert.IsTrue(plan.Accepted, plan.Refusal);
            AssertCorners(plan, 3, 3, 2, 2, 2, 2);
            AssertCorners(plan, 3, 2, 1, 1, 2, 2);
            AssertCorners(plan, 3, 1, 0, 0, 1, 1);
            AssertCorners(plan, 2, 3, 1, 2, 1, 2);
            AssertValid(world, plan);
            foreach (var cell in plan.Cells)
            {
                var bases = new[] { cell.TopLeft, cell.TopRight, cell.BottomLeft, cell.BottomRight };
                Assert.IsFalse(bases.Contains(0) && bases.Contains(2), cell.X + "," + cell.Y);
            }
        }

        [TestMethod]
        public void AThreeBaseCellWithNoPieceLosesACorner()
        {
            var world = World(new[] { 0, 68, 85 }, Pair(0, 68), Pair(0, 85), Pair(68, 85));
            var plan = TerrainPlanner.Plan(
                world,
                6,
                6,
                3,
                1,
                1,
                2,
                (x, y) => (x == 2 && (y == 0 || y == 1)) ? 1 : 0);

            Assert.IsTrue(plan.Accepted, plan.Refusal);
            AssertCorners(plan, 3, 1, 2, 2, 2, 2);
            AssertValid(world, plan);
            foreach (var cell in plan.Cells)
            {
                var distinct = new[] { cell.TopLeft, cell.TopRight, cell.BottomLeft, cell.BottomRight }.Distinct().Count();
                Assert.IsTrue(distinct < 3, cell.X + "," + cell.Y);
            }
        }

        [TestMethod]
        public void OneCornerOnACoastDoesNotRunDownTheShallowBand()
        {
            var world = World(new[] { 0, 68, 85 }, Pair(0, 68), Pair(0, 85), Pair(68, 85));
            var corners = new int[7, 7];
            for (var y = 0; y < 7; y++)
            {
                for (var x = 0; x < 7; x++)
                {
                    corners[x, y] = x <= 2 ? 2 : x == 3 ? 1 : 0;
                }
            }

            var original = (int[,])corners.Clone();
            var plan = TerrainPlanner.PlanCorners(world, 6, 6, 3, 1, 1, 0, (x, y) => corners[x, y]);
            Apply(plan, corners);
            AssertValid(world, plan);

            Assert.AreEqual(0, corners[3, 1]);
            var changed = 0;
            for (var y = 0; y < 7; y++)
            {
                for (var x = 0; x < 7; x++)
                {
                    if (corners[x, y] != original[x, y])
                    {
                        changed++;
                        Assert.IsTrue(y <= 2, "changed " + x + "," + y);
                    }
                }
            }

            Assert.IsTrue(changed <= 4, changed + " corners changed");
        }

        [TestMethod]
        public void TheLargestBrushPlansQuickly()
        {
            var world = World(new[] { 0, 68, 85 }, Pair(0, 68), Pair(68, 85));
            var watch = System.Diagnostics.Stopwatch.StartNew();
            var plan = TerrainPlanner.Plan(world, 24, 24, 8, 8, 8, 0, (x, y) => 2);
            watch.Stop();

            Assert.IsTrue(plan.Accepted, plan.Refusal);
            AssertValid(world, plan);
            Assert.IsTrue(watch.ElapsedMilliseconds < 250, watch.ElapsedMilliseconds + " ms");
        }

        [TestMethod]
        public void BasesWithNoPiecesAreRefused()
        {
            var world = World(new[] { 0, 85 });
            var plan = TerrainPlanner.Plan(world, 4, 4, 1, 1, 1, 0, (x, y) => 1);

            Assert.IsFalse(plan.Accepted);
            StringAssert.Contains(plan.Refusal, "no piece");
            Assert.AreEqual(0, plan.Cells.Count);
        }

        [TestMethod]
        public void ARepairPastTheSpreadLimitIsRefused()
        {
            var world = World(new[] { 0, 85, 219 }, Pair(0, 85), Pair(85, 219));
            var plan = TerrainPlanner.Plan(world, 7, 7, 3, 3, 1, 2, (x, y) => 0, 0);

            Assert.IsFalse(plan.Accepted);
            StringAssert.Contains(plan.Refusal, "no piece");
            Assert.AreEqual(0, plan.Cells.Count);
        }

        private static void Apply(TerrainPlan plan, int[,] corners)
        {
            Assert.IsTrue(plan.Accepted, plan.Refusal);
            foreach (var cell in plan.Cells)
            {
                corners[cell.X, cell.Y] = cell.TopLeft;
                corners[cell.X + 1, cell.Y] = cell.TopRight;
                corners[cell.X, cell.Y + 1] = cell.BottomLeft;
                corners[cell.X + 1, cell.Y + 1] = cell.BottomRight;
            }
        }

        private static void AssertCorners(TerrainPlan plan, int x, int y, int topLeft, int topRight, int bottomLeft, int bottomRight)
        {
            var cell = plan.Cells.Single(item => item.X == x && item.Y == y);
            Assert.AreEqual(topLeft, cell.TopLeft, x + "," + y);
            Assert.AreEqual(topRight, cell.TopRight, x + "," + y);
            Assert.AreEqual(bottomLeft, cell.BottomLeft, x + "," + y);
            Assert.AreEqual(bottomRight, cell.BottomRight, x + "," + y);
        }

        private static void AssertValid(TerrainWorld world, TerrainPlan plan)
        {
            foreach (var cell in plan.Cells)
            {
                Assert.IsTrue(
                    world.HasLayout(cell.TopLeft, cell.TopRight, cell.BottomLeft, cell.BottomRight),
                    cell.X + "," + cell.Y);
            }
        }

        private static TerrainWorld World(int[] bases, params int[][] pairs)
        {
            var samples = new Dictionary<string, TerrainSample>();
            var sections = new List<SectionInfo>();
            foreach (var nominal in bases)
            {
                var file = @"sections\Brush\BaseTiles\Base-" + nominal + ".sct";
                var sample = Flat(nominal, 16);
                samples[file] = sample;
                sections.Add(new SectionInfo
                {
                    World = "BaseTiles",
                    Category = "Brush",
                    HpiFileName = "tiles.hpi",
                    SctFileName = file,
                });
            }

            foreach (var pair in pairs)
            {
                foreach (var mask in TerrainCorners.TransitionMasks)
                {
                    var file = @"sections\Brush\CoastTiles\Join" + pair[0] + "x" + pair[1] + "-" + PieceName(mask) + ".sct";
                    var sample = Flat(0, 16);
                    sample.TopLeft = (mask & 1) != 0 ? pair[1] : pair[0];
                    sample.TopRight = (mask & 2) != 0 ? pair[1] : pair[0];
                    sample.BottomLeft = (mask & 4) != 0 ? pair[1] : pair[0];
                    sample.BottomRight = (mask & 8) != 0 ? pair[1] : pair[0];
                    samples[file] = sample;
                    sections.Add(new SectionInfo
                    {
                        World = "CoastTiles",
                        Category = "Brush",
                        HpiFileName = "tiles.hpi",
                        SctFileName = file,
                    });
                }
            }

            var catalog = TerrainCatalog.Build(sections, section => samples[section.SctFileName]);
            Assert.AreEqual(string.Empty, catalog.Diagnostics, catalog.Diagnostics);
            return catalog.Find("Brush");
        }

        private static string PieceName(int highMask)
        {
            switch (highMask)
            {
                case 1:
                    return "High-TL";
                case 2:
                    return "High-TR";
                case 4:
                    return "High-BL";
                case 8:
                    return "High-BR";
                case 3:
                    return "High-T";
                case 12:
                    return "High-B";
                case 5:
                    return "High-L";
                case 10:
                    return "High-R";
                case 14:
                    return "Low-TL";
                case 13:
                    return "Low-TR";
                case 11:
                    return "Low-BL";
                case 7:
                    return "Low-BR";
                default:
                    return null;
            }
        }

        private static TerrainSample Flat(int height, int tiles)
        {
            return new TerrainSample
            {
                TileWidth = tiles,
                TileHeight = tiles,
                TopLeft = height,
                TopRight = height,
                BottomLeft = height,
                BottomRight = height,
            };
        }

        private static int[] Pair(int low, int high)
        {
            return new[] { low, high };
        }
    }
}
