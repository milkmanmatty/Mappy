namespace MappyTest
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.IO;
    using System.Linq;

    using Mappy;
    using Mappy.Collections;
    using Mappy.Data;
    using Mappy.IO;
    using Mappy.Models;
    using Mappy.Operations;
    using Mappy.Services;
    using Mappy.TerrainBrush;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using TAUtil.Hpi;

    [TestClass]
    public class TerrainRenderGateTest
    {
        [TestMethod]
        public void PaintedCornersReadBackAsTheBaseThePlannerChose()
        {
            var path = Environment.GetEnvironmentVariable("MAPPY_BRUSH_HPI");
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                Assert.Inconclusive("Set MAPPY_BRUSH_HPI to the brush tileset archive to run this test.");
            }

            var sections = new SectionService();
            sections.AddSections(ReadSections(path));
            var bitmaps = new SectionBitmapService(new SectionFactory(new BitmapCache()));
            var core = new CoreModel();
            var dispatcher = new Dispatcher(
                core,
                null,
                sections,
                bitmaps,
                null,
                null,
                null,
                null,
                null);

            var output = Path.Combine(
                Path.GetDirectoryName(typeof(TerrainRenderGateTest).Assembly.Location),
                "TerrainGate");
            Directory.CreateDirectory(output);

            var catalog = dispatcher.LoadTerrainCatalog();
            Assert.AreEqual(string.Empty, catalog.Diagnostics, catalog.Diagnostics);
            var green = catalog.Find("GreenWorld");
            Assert.IsNotNull(green);
            var land = green.IndexOfNominal(85);
            var water = green.IndexOfNominal(0);
            var map = FilledMap(bitmaps, green, land, 5, 3);
            core.Map = Maybe.Some(map);
            dispatcher.SetTerrainWorld(green.Name);
            dispatcher.SetTerrainBase(water);
            dispatcher.SetTerrainBrushSize(16);

            var expected = ReadCorners(green, map);
            var clicks = new[] { new[] { 1, 1 }, new[] { 2, 1 }, new[] { 3, 1 } };
            for (var click = 0; click < clicks.Length; click++)
            {
                var plan = TerrainPlanner.Plan(
                    green,
                    5,
                    3,
                    clicks[click][0],
                    clicks[click][1],
                    1,
                    water,
                    (x, y) => expected[x, y]);
                Assert.IsTrue(plan.Accepted, plan.Refusal);
                Apply(plan, expected);
                dispatcher.PaintTerrain(clicks[click][0] * TerrainPlanner.CellTiles, clicks[click][1] * TerrainPlanner.CellTiles);
                dispatcher.FlushTerrainBrush();
                Assert.AreEqual(string.Empty, core.TerrainStatus, core.TerrainStatus);
                AssertCorners(green, map, expected, "GreenWorld click " + (click + 1));
                Save(map, Path.Combine(output, "green-" + (click + 1) + ".png"));
            }

            map = FilledMap(bitmaps, green, land, 5, 3);
            core.Map = Maybe.Some(map);
            dispatcher.SetTerrainWorld(green.Name);
            dispatcher.SetTerrainBase(water);
            dispatcher.SetTerrainBrushSize(TerrainPlanner.CornerBrush);
            expected = ReadCorners(green, map);
            for (var corner = 1; corner <= 3; corner++)
            {
                var plan = TerrainPlanner.PlanCorners(green, 5, 3, corner, 1, 1, water, (x, y) => expected[x, y]);
                Assert.IsTrue(plan.Accepted, plan.Refusal);
                Apply(plan, expected);
                var beforeClick = Snapshot(map);
                var preview = dispatcher.PreviewTerrain(corner * TerrainPlanner.CellTiles, TerrainPlanner.CellTiles);
                Assert.IsTrue(preview.Accepted, preview.Refusal);
                Assert.IsTrue(preview.Stamps.Count > 0, "preview of corner " + corner + " is empty");
                CollectionAssert.AreEqual(beforeClick, Snapshot(map), "preview changed the map");
                dispatcher.PaintTerrain(corner * TerrainPlanner.CellTiles, TerrainPlanner.CellTiles);
                dispatcher.FlushTerrainBrush();
                AssertChangesInside(beforeClick, map, preview, "corner " + corner);
                Assert.AreEqual(string.Empty, core.TerrainStatus, core.TerrainStatus);
                AssertCorners(green, map, expected, "GreenWorld corner " + corner);
                Save(map, Path.Combine(output, "green-corner-" + corner + ".png"));
            }

            var painted = new Bitmap[map.MapWidth * map.MapHeight];
            for (var index = 0; index < painted.Length; index++)
            {
                painted[index] = map.BaseTile.TileGrid[index];
            }

            var repaint = dispatcher.PreviewTerrain(3 * TerrainPlanner.CellTiles, TerrainPlanner.CellTiles);
            Assert.IsTrue(repaint.Accepted, repaint.Refusal);
            Assert.AreEqual(0, repaint.Stamps.Count, "a repaint should preview no cells");
            dispatcher.PaintTerrain(3 * TerrainPlanner.CellTiles, TerrainPlanner.CellTiles);
            dispatcher.FlushTerrainBrush();
            for (var index = 0; index < painted.Length; index++)
            {
                Assert.AreSame(painted[index], map.BaseTile.TileGrid[index], "repaint changed tile " + index);
            }

            map.Undo();
            Assert.AreEqual(land, TerrainCornerReader.Read(green, map.BaseTile.HeightGrid, 3, 1), "undo should take back corner 3, not an empty repaint");
            Assert.AreEqual(water, TerrainCornerReader.Read(green, map.BaseTile.HeightGrid, 2, 1));

            PaintPath(dispatcher, core, bitmaps, green, land, "Ridge", Path.Combine(output, "green-ridge.png"));
            PaintPath(dispatcher, core, bitmaps, green, land, "River", Path.Combine(output, "green-river.png"));

            foreach (var world in catalog.Worlds)
            {
                for (var ground = 0; ground < world.Bases.Count; ground++)
                {
                    for (var level = 0; level < world.Bases.Count; level++)
                    {
                        if (level == ground)
                        {
                            continue;
                        }

                        PaintPair(dispatcher, core, bitmaps, world, ground, level);
                    }
                }
            }
        }

        private static void PaintPath(
            Dispatcher dispatcher,
            CoreModel core,
            SectionBitmapService bitmaps,
            TerrainWorld world,
            int land,
            string styleName,
            string path)
        {
            var map = FilledMap(bitmaps, world, land, 5, 3);
            var before = Snapshot(map);
            core.Map = Maybe.Some(map);
            dispatcher.SetTerrainWorld(world.Name);
            dispatcher.SetTerrainTool(styleName);
            dispatcher.PaintTerrain(TerrainPlanner.CellTiles, TerrainPlanner.CellTiles);
            dispatcher.PaintTerrain(2 * TerrainPlanner.CellTiles, TerrainPlanner.CellTiles);
            dispatcher.FlushTerrainBrush();
            Assert.AreEqual(string.Empty, core.TerrainStatus, styleName);
            CollectionAssert.AreNotEqual(before, Snapshot(map), styleName + " did not paint");
            Save(map, path);
            dispatcher.SetTerrainTool(string.Empty);
        }

        private static void PaintPair(
            Dispatcher dispatcher,
            CoreModel core,
            SectionBitmapService bitmaps,
            TerrainWorld world,
            int ground,
            int level)
        {
            var label = world.Name + " " + world.Bases[ground].Nominal + " -> " + world.Bases[level].Nominal;
            var map = FilledMap(bitmaps, world, ground, 3, 3);
            var before = map.BaseTile.HeightGrid.Get(0, 0);
            core.Map = Maybe.Some(map);
            dispatcher.SetTerrainWorld(world.Name);
            dispatcher.SetTerrainBase(level);
            dispatcher.SetTerrainBrushSize(16);

            var expected = ReadCorners(world, map);
            for (var y = 0; y < expected.GetLength(1); y++)
            {
                for (var x = 0; x < expected.GetLength(0); x++)
                {
                    Assert.AreEqual(ground, expected[x, y], label + " ground");
                }
            }

            var plan = TerrainPlanner.Plan(world, 3, 3, 1, 1, 1, level, (x, y) => expected[x, y]);
            dispatcher.PaintTerrain(TerrainPlanner.CellTiles, TerrainPlanner.CellTiles);
            dispatcher.FlushTerrainBrush();
            if (!plan.Accepted)
            {
                Assert.AreEqual(plan.Refusal, core.TerrainStatus, label);
                Assert.AreEqual(before, map.BaseTile.HeightGrid.Get(0, 0), label);
                return;
            }

            Apply(plan, expected);
            Assert.AreEqual(string.Empty, core.TerrainStatus, label);
            AssertCorners(world, map, expected, label);
        }

        [TestMethod]
        public void TheStripPastTheLastCellDoesNotTurnEdgeCornersToWater()
        {
            var path = Environment.GetEnvironmentVariable("MAPPY_BRUSH_HPI");
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                Assert.Inconclusive("Set MAPPY_BRUSH_HPI to the brush tileset archive to run this test.");
            }

            var sections = new SectionService();
            sections.AddSections(ReadSections(path));
            var bitmaps = new SectionBitmapService(new SectionFactory(new BitmapCache()));
            var core = new CoreModel();
            var dispatcher = new Dispatcher(core, null, sections, bitmaps, null, null, null, null, null);
            var green = dispatcher.LoadTerrainCatalog().Find("GreenWorld");
            var land = green.IndexOfNominal(85);
            var water = green.IndexOfNominal(0);
            var map = FilledMap(bitmaps, green, land, 4, 3, 8);
            core.Map = Maybe.Some(map);
            dispatcher.SetTerrainWorld(green.Name);
            dispatcher.SetTerrainBase(water);
            dispatcher.SetTerrainBrushSize(TerrainPlanner.CornerBrush);

            dispatcher.PaintTerrain(2 * TerrainPlanner.CellTiles, 2 * TerrainPlanner.CellTiles);
            dispatcher.FlushTerrainBrush();
            Assert.AreEqual(string.Empty, core.TerrainStatus, core.TerrainStatus);

            var expected = new int[6, 5];
            for (var y = 0; y < 5; y++)
            {
                for (var x = 0; x < 6; x++)
                {
                    expected[x, y] = x <= 4 && y <= 3 ? land : water;
                }
            }

            expected[2, 2] = water;
            AssertCorners(green, map, expected, "corner click above the bottom edge");
        }

        [TestMethod]
        public void ACellOnlyPartlyOnTheMapCanBePainted()
        {
            var path = Environment.GetEnvironmentVariable("MAPPY_BRUSH_HPI");
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                Assert.Inconclusive("Set MAPPY_BRUSH_HPI to the brush tileset archive to run this test.");
            }

            var sections = new SectionService();
            sections.AddSections(ReadSections(path));
            var bitmaps = new SectionBitmapService(new SectionFactory(new BitmapCache()));
            var core = new CoreModel();
            var dispatcher = new Dispatcher(core, null, sections, bitmaps, null, null, null, null, null);
            var green = dispatcher.LoadTerrainCatalog().Find("GreenWorld");
            var land = green.IndexOfNominal(85);
            var water = green.IndexOfNominal(0);
            var map = FilledMap(bitmaps, green, water, 4, 3, 8);
            core.Map = Maybe.Some(map);
            dispatcher.SetTerrainWorld(green.Name);
            dispatcher.SetTerrainBase(land);
            dispatcher.SetTerrainBrushSize(16);

            dispatcher.PaintTerrain(4 * TerrainPlanner.CellTiles, 0);
            dispatcher.FlushTerrainBrush();
            Assert.AreEqual(string.Empty, core.TerrainStatus, core.TerrainStatus);
            Assert.AreEqual(72, map.MapWidth);
            Assert.AreNotSame(Globals.DefaultTile, map.BaseTile.TileGrid.Get(71, 0));
            Assert.AreNotSame(Globals.DefaultTile, map.BaseTile.TileGrid.Get(64, 15));
            Assert.AreSame(Globals.DefaultTile, map.BaseTile.TileGrid.Get(71, 40));
            var tiles = map.BaseTile.TileGrid;
            var heights = map.BaseTile.HeightGrid;
            Assert.AreEqual(land, TerrainCornerReader.Read(green, heights, tiles, 4, 0));
            Assert.AreEqual(land, TerrainCornerReader.Read(green, heights, tiles, 5, 0));
            Assert.AreEqual(land, TerrainCornerReader.Read(green, heights, tiles, 4, 1));
            Assert.AreEqual(water, TerrainCornerReader.Read(green, heights, tiles, 3, 1));
        }

        private static object[] Snapshot(UndoableMapModel map)
        {
            var tiles = map.BaseTile.TileGrid;
            var heights = map.BaseTile.HeightGrid;
            var copy = new object[(tiles.Width * tiles.Height) + (heights.Width * heights.Height)];
            for (var index = 0; index < tiles.Width * tiles.Height; index++)
            {
                copy[index] = tiles[index];
            }

            for (var index = 0; index < heights.Width * heights.Height; index++)
            {
                copy[(tiles.Width * tiles.Height) + index] = heights[index];
            }

            return copy;
        }

        private static void AssertChangesInside(object[] before, UndoableMapModel map, TerrainStroke preview, string label)
        {
            var tiles = map.BaseTile.TileGrid;
            for (var y = 0; y < tiles.Height; y++)
            {
                for (var x = 0; x < tiles.Width; x++)
                {
                    if (ReferenceEquals(before[(y * tiles.Width) + x], tiles.Get(x, y)))
                    {
                        continue;
                    }

                    var inside = preview.Stamps.Any(s => x >= s.TileX && x < s.TileX + s.Tiles.Width
                        && y >= s.TileY && y < s.TileY + s.Tiles.Height);
                    Assert.IsTrue(inside, label + ": tile " + x + "," + y + " changed outside the preview");
                }
            }
        }

        private static UndoableMapModel FilledMap(
            SectionBitmapService bitmaps,
            TerrainWorld world,
            int ground,
            int cellsX,
            int cellsY,
            int extraTiles = 0)
        {
            var section = world.Bases[ground].Sections[0];
            var tile = bitmaps.LoadSection(section.HpiFileName, section.SctFileName);
            var stamp = TerrainBrushStamp.FromSection(tile, 0, 0);
            var source = new MapModel(
                (cellsX * TerrainPlanner.CellTiles) + extraTiles,
                (cellsY * TerrainPlanner.CellTiles) + extraTiles);
            source.Minimap.Dispose();
            source.Minimap = null;
            GridMethods.Fill(source.Tile.TileGrid, Globals.DefaultTile);
            for (var y = 0; y < cellsY; y++)
            {
                for (var x = 0; x < cellsX; x++)
                {
                    GridMethods.Copy(
                        stamp.Tiles,
                        source.Tile.TileGrid,
                        x * TerrainPlanner.CellTiles,
                        y * TerrainPlanner.CellTiles);
                    GridMethods.Copy(
                        stamp.Heights,
                        source.Tile.HeightGrid,
                        x * TerrainPlanner.CellTiles * 2,
                        y * TerrainPlanner.CellTiles * 2);
                }
            }

            return new UndoableMapModel(source, null, false);
        }

        private static int[,] ReadCorners(TerrainWorld world, UndoableMapModel map)
        {
            var cellsX = (map.MapWidth + TerrainPlanner.CellTiles - 1) / TerrainPlanner.CellTiles;
            var cellsY = (map.MapHeight + TerrainPlanner.CellTiles - 1) / TerrainPlanner.CellTiles;
            var corners = new int[cellsX + 1, cellsY + 1];
            for (var y = 0; y <= cellsY; y++)
            {
                for (var x = 0; x <= cellsX; x++)
                {
                    corners[x, y] = TerrainCornerReader.Read(world, map.BaseTile.HeightGrid, map.BaseTile.TileGrid, x, y);
                }
            }

            return corners;
        }

        private static void Apply(TerrainPlan plan, int[,] corners)
        {
            foreach (var cell in plan.Cells)
            {
                corners[cell.X, cell.Y] = cell.TopLeft;
                corners[cell.X + 1, cell.Y] = cell.TopRight;
                corners[cell.X, cell.Y + 1] = cell.BottomLeft;
                corners[cell.X + 1, cell.Y + 1] = cell.BottomRight;
            }
        }

        private static void AssertCorners(TerrainWorld world, UndoableMapModel map, int[,] expected, string label)
        {
            for (var y = 0; y < expected.GetLength(1); y++)
            {
                for (var x = 0; x < expected.GetLength(0); x++)
                {
                    var read = TerrainCornerReader.Read(world, map.BaseTile.HeightGrid, map.BaseTile.TileGrid, x, y);
                    Assert.AreEqual(expected[x, y], read, label + " corner " + x + "," + y);
                }
            }
        }

        private static void Save(UndoableMapModel map, string path)
        {
            var tiles = map.BaseTile.TileGrid;
            using (var bitmap = new Bitmap(tiles.Width * MapTile.TileWidth, tiles.Height * MapTile.TileHeight))
            using (var graphics = Graphics.FromImage(bitmap))
            {
                for (var y = 0; y < tiles.Height; y++)
                {
                    for (var x = 0; x < tiles.Width; x++)
                    {
                        var tile = tiles.Get(x, y);
                        if (tile != null)
                        {
                            graphics.DrawImage(tile, x * MapTile.TileWidth, y * MapTile.TileHeight);
                        }
                    }
                }

                bitmap.Save(path, ImageFormat.Png);
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
    }
}
