namespace MappyTest
{
    using Mappy.TerrainBrush;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class TerrainPieceNameTest
    {
        [TestMethod]
        public void CliffWithAHeightAndAVariationNamesTheBottomEdge()
        {
            var piece = Parse("Cliff-230-High-B-02.sct");

            Assert.AreEqual("Cliff-230", piece.Prefix);
            CollectionAssert.AreEqual(new[] { "!High", "!High", "High", "High" }, piece.CornerLabels);
        }

        [TestMethod]
        public void CoastNamesOneCornerAndLeavesTheRestImplicit()
        {
            var piece = Parse("Coast-Lava-TL-01");

            Assert.AreEqual("Coast", piece.Prefix);
            CollectionAssert.AreEqual(new[] { "Lava", "!Lava", "!Lava", "!Lava" }, piece.CornerLabels);
        }

        [TestMethod]
        public void CoastalCliffNamesThreeDifferentCorners()
        {
            var piece = Parse("CC-219-B-85-TL-0-TR-01");

            Assert.AreEqual("CC", piece.Prefix);
            CollectionAssert.AreEqual(new[] { "85", "0", "219", "219" }, piece.CornerLabels);
        }

        [TestMethod]
        public void AllFillsTheCornersALaterRegionDoesNotName()
        {
            var piece = Parse("CC-219-ALL-0-BL");

            Assert.AreEqual("CC", piece.Prefix);
            CollectionAssert.AreEqual(new[] { "219", "219", "0", "219" }, piece.CornerLabels);
        }

        [TestMethod]
        public void DiagonalRegionCoversBothCorners()
        {
            var piece = Parse("DSL-Land-BL-Shallow-TL_and_BR-Water-TR");

            Assert.AreEqual("DSL", piece.Prefix);
            CollectionAssert.AreEqual(new[] { "Shallow", "Water", "Land", "Shallow" }, piece.CornerLabels);
        }

        [TestMethod]
        public void RegionMatchIgnoresCase()
        {
            var piece = Parse("coast-water-b-01");

            Assert.AreEqual("coast", piece.Prefix);
            CollectionAssert.AreEqual(new[] { "!water", "!water", "water", "water" }, piece.CornerLabels);
        }

        [TestMethod]
        public void BaseNamesAreNotTransitionPieces()
        {
            TerrainPieceName piece;
            Assert.IsFalse(TerrainPieceName.TryParse("Base-85", out piece));
            Assert.IsFalse(TerrainPieceName.TryParse("Base-85.sct", out piece));
            Assert.IsFalse(TerrainPieceName.TryParse(null, out piece));
            Assert.IsFalse(TerrainPieceName.TryParse(string.Empty, out piece));
        }

        private static TerrainPieceName Parse(string name)
        {
            TerrainPieceName piece;
            Assert.IsTrue(TerrainPieceName.TryParse(name, out piece), name);
            return piece;
        }
    }
}
