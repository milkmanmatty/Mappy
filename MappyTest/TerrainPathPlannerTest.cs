namespace MappyTest
{
    using System.Drawing;

    using Mappy.TerrainBrush;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class TerrainPathPlannerTest
    {
        private static readonly int[] Land = { 1, 1, 1, 1 };

        private static readonly int[] Mouth = { 0, 0, 1, 1 };

        [TestMethod]
        public void LoneClickOnFlatLandPlacesTheLonePiece()
        {
            var style = River();
            var plan = TerrainPathPlanner.Plan(style, 3, 3, null, new Point(1, 1), (x, y) => Cell(Land, TerrainSides.None));

            Assert.IsTrue(plan.Accepted, plan.Refusal);
            Assert.AreEqual(1, plan.Steps.Count);
            Assert.AreEqual(1, plan.Steps[0].X);
            Assert.AreEqual(1, plan.Steps[0].Y);
            Assert.AreEqual(TerrainSides.None, plan.Steps[0].Mask);
            Assert.AreEqual("lone", plan.Steps[0].Sections[0].SctFileName);
        }

        [TestMethod]
        public void DragToANeighbourJoinsBothCellsWithFacingSides()
        {
            var style = River();
            var plan = TerrainPathPlanner.Plan(
                style,
                3,
                3,
                new Point(0, 0),
                new Point(1, 0),
                (x, y) => Cell(Land, TerrainSides.None));

            Assert.IsTrue(plan.Accepted, plan.Refusal);
            Assert.AreEqual(2, plan.Steps.Count);
            Assert.AreEqual(0, plan.Steps[0].X);
            Assert.AreEqual(TerrainSides.Right, plan.Steps[0].Mask);
            Assert.AreEqual("straight", plan.Steps[0].Sections[0].SctFileName);
            Assert.AreEqual(1, plan.Steps[1].X);
            Assert.AreEqual(TerrainSides.Left, plan.Steps[1].Mask);
            Assert.AreEqual("straight", plan.Steps[1].Sections[0].SctFileName);
        }

        [TestMethod]
        public void MoveOntoACoastUsesTheMouthPiece()
        {
            var style = River();
            var plan = TerrainPathPlanner.Plan(
                style,
                2,
                2,
                new Point(0, 1),
                new Point(0, 0),
                (x, y) => y == 0 ? Cell(Mouth, TerrainSides.None) : Cell(Land, TerrainSides.None));

            Assert.IsTrue(plan.Accepted, plan.Refusal);
            Assert.AreEqual(2, plan.Steps.Count);
            Assert.AreEqual(1, plan.Steps[0].Y);
            Assert.AreEqual(TerrainSides.Top, plan.Steps[0].Mask);
            Assert.AreEqual("land-up", plan.Steps[0].Sections[0].SctFileName);
            Assert.AreEqual(0, plan.Steps[1].Y);
            Assert.AreEqual(TerrainSides.Bottom, plan.Steps[1].Mask);
            Assert.AreEqual("mouth", plan.Steps[1].Sections[0].SctFileName);
        }

        [TestMethod]
        public void MoveOntoOpenWaterEndsWithoutWriting()
        {
            var style = River();
            var plan = TerrainPathPlanner.Plan(
                style,
                3,
                3,
                new Point(0, 0),
                new Point(1, 0),
                (x, y) => x == 0
                    ? Cell(Land, TerrainSides.None)
                    : Cell(new[] { 0, 0, 0, 0 }, TerrainPathShape.NotAPath));

            Assert.IsTrue(plan.Accepted, plan.Refusal);
            Assert.AreEqual(0, plan.Steps.Count);
        }

        [TestMethod]
        public void AJoinTheStyleHasNoPieceForIsRefused()
        {
            var style = River();
            var plan = TerrainPathPlanner.Plan(
                style,
                3,
                3,
                new Point(0, 0),
                new Point(1, 0),
                (x, y) => Cell(Land, x == 0 ? TerrainSides.Top : TerrainSides.None));

            Assert.IsFalse(plan.Accepted);
            StringAssert.Contains(plan.Refusal, "cannot join");
            Assert.AreEqual(0, plan.Steps.Count);
        }

        private static TerrainPathStyle River()
        {
            var style = new TerrainPathStyle("River", TerrainPathKind.Channel);
            style.Add(Land, TerrainSides.None, Piece("lone"));
            style.Add(Land, TerrainSides.Left | TerrainSides.Right, Piece("straight"));
            style.Add(Land, TerrainSides.Top, Piece("land-up"));
            style.Add(Mouth, TerrainSides.Bottom, Piece("mouth"));
            return style;
        }

        private static TerrainSectionRef Piece(string name)
        {
            return new TerrainSectionRef("tiles.hpi", name, 16, 16);
        }

        private static TerrainPathCell Cell(int[] corners, int mask)
        {
            return new TerrainPathCell(corners, mask);
        }
    }
}
