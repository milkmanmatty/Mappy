namespace Mappy.Services
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.IO;
    using System.Linq;
    using System.Windows.Forms;
    using Mappy.Collections;
    using Mappy.Data;
    using Mappy.IO;
    using Mappy.Models;
    using Mappy.Models.Enums;
    using Mappy.Util;
    using Mappy.Util.ImageSampling;
    using TAUtil;
    using TAUtil.Gdi.Palette;
    using TAUtil.Hpi;
    using TAUtil.Tnt;

    public class Dispatcher
    {
        private readonly CoreModel model;

        private readonly IDialogService dialogService;

        private readonly SectionService sectionService;

        private readonly SectionBitmapService sectionBitmapService;

        private readonly FeatureService featureService;

        private readonly UnitCatalogService unitCatalogService;

        private readonly MapLoadingService mapLoadingService;

        private readonly ImageImportService imageImportingService;

        private readonly BitmapCache tileCache;

        private readonly Random rng = new Random();

        private readonly int[] startPositionViewCycle = new int[10];

        public Dispatcher(
            CoreModel model,
            IDialogService dialogService,
            SectionService sectionService,
            SectionBitmapService sectionBitmapService,
            FeatureService featureService,
            UnitCatalogService unitCatalogService,
            MapLoadingService mapLoadingService,
            ImageImportService imageImportingService,
            BitmapCache tileCache)
        {
            this.model = model;
            this.dialogService = dialogService;
            this.sectionService = sectionService;
            this.sectionBitmapService = sectionBitmapService;
            this.featureService = featureService;
            this.unitCatalogService = unitCatalogService;
            this.mapLoadingService = mapLoadingService;
            this.imageImportingService = imageImportingService;
            this.tileCache = tileCache;
        }

        public void Initialize()
        {
            var dlg = this.dialogService.CreateProgressView();
            dlg.Title = "Loading Mappy";
            dlg.ShowProgress = true;
            dlg.CancelEnabled = true;

            var worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.WorkerSupportsCancellation = true;
            worker.DoWork += (sender, args) =>
                {
                    var w = (BackgroundWorker)sender;

                    if (!SectionLoadingUtils.LoadSections(
                        i => w.ReportProgress((40 * i) / 100),
                        () => w.CancellationPending,
                        out var result))
                    {
                        args.Cancel = true;
                        return;
                    }

                    if (!FeatureLoadingUtils.LoadFeatures(
                        i => w.ReportProgress(40 + ((30 * i) / 100)),
                        () => w.CancellationPending,
                        out var featureResult))
                    {
                        args.Cancel = true;
                        return;
                    }

                    if (!UnitLoadingUtils.LoadUnitCatalog(
                        i => w.ReportProgress(70 + ((30 * i) / 100)),
                        () => w.CancellationPending,
                        out var unitResult))
                    {
                        args.Cancel = true;
                        return;
                    }

                    args.Result = new SectionFeatureLoadResult(
                            result.Records,
                            featureResult.Records,
                            unitResult.Records,
                            result.Errors
                                .Concat(featureResult.Errors)
                                .Concat(unitResult.Errors)
                                .GroupBy(x => x.HpiPath)
                                .Select(x => x.First())
                                .ToList(),
                            result.FileErrors
                                .Concat(featureResult.FileErrors)
                                .Concat(unitResult.FileErrors)
                                .ToList());
                };

            worker.ProgressChanged += (sender, args) => dlg.Progress = args.ProgressPercentage;
            worker.RunWorkerCompleted += (sender, args) =>
                {
                    if (args.Error != null)
                    {
                        Program.HandleUnexpectedException(args.Error);
                        Application.Exit();
                        return;
                    }

                    if (args.Cancelled)
                    {
                        dlg.Close();
                        return;
                    }

                    var sectionResult = (SectionFeatureLoadResult)args.Result;

                    this.sectionService.AddSections(sectionResult.Sections);

                    this.featureService.AddFeatures(sectionResult.Features);

                    this.unitCatalogService.AddCatalogRecords(sectionResult.UnitCatalog);

                    if (sectionResult.Errors.Count > 0 || sectionResult.FileErrors.Count > 0)
                    {
                        var hpisList = sectionResult.Errors.Select(x => x.HpiPath);
                        var filesList = sectionResult.FileErrors.Select(x => x.HpiPath + "\\" + x.FeaturePath);
                        this.dialogService.ShowError(
                            "Failed to load the following files:\n\n"
                                + string.Join("\n", hpisList) + "\n"
                                + string.Join("\n", filesList));
                    }

                    dlg.Close();
                };

            dlg.CancelPressed += (sender, args) => worker.CancelAsync();

            dlg.MessageText = "Loading sections and features ...";
            worker.RunWorkerAsync();

            dlg.Display();
        }

        public void UpdateMousePosition(Maybe<Point> mousePosition)
        {
            this.model.Map.IfSome(map => map.MousePosition = mousePosition);
        }

        public void SetHoveredFeature(Maybe<Guid> featureId)
        {
            this.model.Map.IfSome(map => map.HoveredFeature = featureId);
        }

        public void HideGrid()
        {
            this.model.GridVisible = false;
        }

        public void EnableGridWithSize(Size s)
        {
            this.model.GridSize = s;
            this.model.GridVisible = true;
        }

        public void ChooseColor()
        {
            var c = this.dialogService.AskUserGridColor(this.model.GridColor);
            if (c.HasValue)
            {
                this.model.GridColor = c.Value;
            }
        }

        public void ShowInfo()
        {
            this.dialogService.ShowInfo();
        }

        public void ShowAbout()
        {
            this.dialogService.ShowAbout();
        }

        public void Undo()
        {
            this.model.Map.IfSome(x => x.Undo());
        }

        public void Redo()
        {
            this.model.Map.IfSome(x => x.Redo());
        }

        public void New()
        {
            if (!this.CheckOkayDiscard())
            {
                return;
            }

            var size = this.dialogService.AskUserNewMapSize();
            if (size.Width == 0 || size.Height == 0)
            {
                return;
            }

            this.New(size.Width, size.Height);
        }

        public void Open()
        {
            if (!this.CheckOkayDiscard())
            {
                return;
            }

            var filename = this.dialogService.AskUserToOpenFile();
            if (string.IsNullOrEmpty(filename))
            {
                return;
            }

            this.OpenMap(filename);
        }

        public void OpenFromDragDrop(string filename)
        {
            if (!this.CheckOkayDiscard())
            {
                return;
            }

            this.OpenMap(filename);
        }

        public bool Save()
        {
            return this.model.Map.Match(
                some: map =>
                {
                    if (map.FilePath == null || map.IsFileReadOnly)
                    {
                        return this.SaveAs();
                    }

                    return this.SaveHelper(map, map.FilePath);
                },
                none: () => false);
        }

        public bool SaveAs()
        {
            return this.model.Map.Match(
                some: map =>
                    {
                        var path = this.dialogService.AskUserToSaveFile();

                        if (path == null)
                        {
                            return false;
                        }

                        return this.SaveHelper(map, path);
                    },
                none: () => false);
        }

        public void OpenPreferences()
        {
            if (!this.dialogService.CapturePreferences())
            {
                return;
            }

            this.sectionService.NotifySectionsChanged();
            this.featureService.NotifyFeaturesChanged();
            this.unitCatalogService.NotifyUnitPickerLabelsChanged();
        }

        public void Close()
        {
            if (this.CheckOkayDiscard())
            {
                Application.Exit();
            }
        }

        public void CloseMap()
        {
            if (this.CheckOkayDiscard())
            {
                this.model.Map = Maybe.None<UndoableMapModel>();
            }
        }

        public void DragDropSection(int sectionId, int x, int y)
        {
            this.model.Map.IfSome(
                map =>
                    {
                        var record = this.sectionService.Get(sectionId);
                        var tile = this.sectionBitmapService.LoadSection(record.HpiFileName, record.SctFileName);
                        map.DragDropTile(tile, x, y);
                    });
        }

        public void CopySelectionToClipboard()
        {
            this.model.Map.IfSome(x => TryCopyToClipboard(x));
        }

        public void CutSelectionToClipboard()
        {
            this.model.Map.IfSome(
                x =>
                    {
                        if (TryCopyToClipboard(x))
                        {
                            x.DeleteSelection();
                        }
                    });
        }

        public void PasteFromClipboard()
        {
            this.model.Map.IfSome(
                map =>
                    {
                        object data = Clipboard.GetData(DataFormats.Serializable);
                        if (data == null)
                        {
                            return;
                        }

                        Point loc = map.ViewportLocation;
                        loc.X += this.model.ViewportWidth / 2;
                        loc.Y += this.model.ViewportHeight / 2;

                        IMapTile tile = data as IMapTile;
                        if (tile != null)
                        {
                            this.DeduplicateTiles(tile.TileGrid);
                            map.PasteMapTile(tile, loc.X, loc.Y);
                        }
                        else
                        {
                            if (data is FeatureClipboardRecord featureRecord)
                            {
                                map.DragDropFeature(featureRecord.FeatureName, loc.X, loc.Y);
                                return;
                            }

                            var featureList = data as List<FeatureClipboardRecord>;
                            if (featureList != null)
                            {
                                map.ClearSelection();
                                foreach (var feature in featureList)
                                {
                                    // Split these up so they can be debugged better
                                    // force locs between 0 and MapWidth/Height
                                    int xLocUnsafe = map.ViewportLocation.X + feature.VPOffsetX;
                                    int xLoc = Math.Min(map.MapWidth * 32, Math.Max(0, xLocUnsafe));

                                    int yLocUnsafe = map.ViewportLocation.Y + feature.VPOffsetY;
                                    int yLoc = Math.Min(map.MapHeight * 32, Math.Max(0, yLocUnsafe));

                                    map.DragDropFeature(feature.FeatureName, xLoc, yLoc, false);
                                }

                                return;
                            }

                            if (data is SchemaUnitClipboardRecord unitRecord)
                            {
                                this.PasteSchemaUnitsFromClipboardRecords(map, new[] { unitRecord });
                                return;
                            }

                            var unitList = data as List<SchemaUnitClipboardRecord>;
                            if (unitList != null)
                            {
                                this.PasteSchemaUnitsFromClipboardRecords(map, unitList);
                                return;
                            }
                        }
                    });
        }

        public void FillMap()
        {
            this.model.Map.IfSome(map => map.FillWithSelectedTile());
        }

        public void ResizeMap()
        {
            this.model.Map.IfSome(
                map =>
                {
                    var newSize = this.dialogService.AskUserResizeMapSize(map.MapWidth, map.MapHeight);
                    if (newSize.Width <= 0 || newSize.Height <= 0)
                    {
                        return;
                    }

                    if (newSize.Width == map.MapWidth && newSize.Height == map.MapHeight)
                    {
                        return;
                    }

                    map.ClearSelection();
                    var oldViewportLocation = map.ViewportLocation;
                    map.ResizeMap(newSize.Width, newSize.Height);
                    this.model.SetViewportLocation(oldViewportLocation);
                });
        }

        public void Flip()
        {
            // Show dialog to get user preferences
            var options = this.dialogService.AskUserForFlipOptions();
            if (options == null || options.Cancelled)
            {
                return; // User cancelled
            }

            this.model.Map.IfSome(
                map =>
                {
                    if (!map.SelectedTile.HasValue || !map.FloatingTiles.Any() || map.FloatingTiles[map.SelectedTile.Value].Item == null)
                    {
                        return;
                    }

                    ImgUtil.ValidateDir(ImgUtil.TempDir);

                    IMapTile floatTile = map.FloatingTiles[map.SelectedTile.Value].Item;

                    MapTile destTile = new MapTile(floatTile.TileGrid.Width, floatTile.TileGrid.Height);
                    GridMethods.Copy(floatTile.TileGrid, destTile.TileGrid, 0, 0, 0, 0, floatTile.TileGrid.Width, floatTile.TileGrid.Height);
                    GridMethods.Copy(floatTile.HeightGrid, destTile.HeightGrid, 0, 0, 0, 0, floatTile.HeightGrid.Width, floatTile.HeightGrid.Height);

                    // Only flip heightmap, graphic will be flipped later
                    GridMethods.FlipArea(
                        floatTile.HeightGrid,
                        destTile.HeightGrid,
                        floatTile.HeightGrid.Width,
                        floatTile.HeightGrid.Height,
                        options.Direction);

                    var prefix = options.Direction == FlipDirection.Horizontal ? "fh" : "fv";
                    var graphicPath = Path.Combine(ImgUtil.TempDir, prefix + "_Result.png");
                    var heightPath = Path.Combine(ImgUtil.TempDir, prefix + "Heightmap.png");

                    Bitmap heightBitmap = ImgUtil.GetBitmapFromHeightmapGrid(destTile.HeightGrid);
                    heightBitmap.Save(heightPath, ImageFormat.Png); // export before resize

                    Bitmap graphicBitmap = ImgUtil.GetBitmapFromTilegrid(destTile.TileGrid);
                    Bitmap result;

                    if (options.ApplyShadows)
                    {
                        // For TA this will never be true, but check anyway just in case
                        if (graphicBitmap.Width != heightBitmap.Width || graphicBitmap.Height != heightBitmap.Height)
                        {
                            heightBitmap = ImageRelightingService.BicubicResize(heightBitmap, graphicBitmap);
                        }

                        result = ImageRelightingService.FlipAndRelightBitmap(
                            graphicBitmap,
                            heightBitmap,
                            options.Direction,
                            ImgUtil.GetLightDirectionFromEnum(LightDirection.BottomLeft));
                    }
                    else
                    {
                        result = ImgUtil.FlipBitmap(graphicBitmap, options.Direction);
                    }

                    result.Save(graphicPath, ImageFormat.Png);

                    heightBitmap.Dispose();
                    result.Dispose();

                    this.ImportCustomSectionHelper(map, graphicPath, heightPath);

                    // ImgUtil.ClearTemps();
                });
        }

        public void ExportSelectedSection()
        {
            var options = this.dialogService.AskUserToChooseSectionExportPaths();
            if (options == null || string.IsNullOrEmpty(options.GraphicPath) || string.IsNullOrEmpty(options.HeightmapPath))
            {
                return; // User cancelled
            }

            this.model.Map.IfSome(
                map =>
                {
                    if (!map.SelectedTile.HasValue || !map.FloatingTiles.Any() || map.FloatingTiles[map.SelectedTile.Value].Item == null)
                    {
                        return;
                    }

                    ImgUtil.ValidateDir(ImgUtil.ExportDir);

                    IMapTile floatTile = map.FloatingTiles[map.SelectedTile.Value].Item;

                    MapTile destTile = new MapTile(floatTile.TileGrid.Width, floatTile.TileGrid.Height);
                    GridMethods.Copy(floatTile.TileGrid, destTile.TileGrid, 0, 0, 0, 0, floatTile.TileGrid.Width, floatTile.TileGrid.Height);
                    GridMethods.Copy(floatTile.HeightGrid, destTile.HeightGrid, 0, 0, 0, 0, floatTile.HeightGrid.Width, floatTile.HeightGrid.Height);

                    var graphicPath = Path.Combine(options.GraphicPath);
                    var heightPath = Path.Combine(options.HeightmapPath);

                    Bitmap heightBitmap = ImgUtil.GetBitmapFromHeightmapGrid(destTile.HeightGrid);
                    heightBitmap.Save(heightPath, ImageFormat.Png);

                    Bitmap graphicBitmap = ImgUtil.GetBitmapFromTilegrid(destTile.TileGrid);
                    graphicBitmap.Save(graphicPath, ImageFormat.Png);

                    heightBitmap.Dispose();
                    graphicBitmap.Dispose();
            });
        }

        public void RefreshMinimap()
        {
            this.model.Map.IfSome(
                map =>
                    {
                        Bitmap minimap;
                        using (var adapter = new MapPixelImageAdapter(map.BaseTile.TileGrid))
                        {
                            minimap = Util.GenerateMinimap(adapter);
                        }

                        map.SetMinimap(minimap);
                    });
        }

        public void RefreshMinimapHighQualityWithProgress()
        {
            this.model.Map.IfSome(this.RefreshMinimapHighQualityWithProgressHelper);
        }

        public void ExportHeightmap()
        {
            this.model.Map.IfSome(this.ExportHeightmapHelper);
        }

        public void ExportMinimap()
        {
            this.model.Map.IfSome(this.ExportMinimapHelper);
        }

        public void ExportMapImage()
        {
            this.model.Map.IfSome(this.ExportMapImageHelper);
        }

        public void ImportCustomSection()
        {
            this.model.Map.IfSome(this.ImportCustomSectionInteractiveHelper);
        }

        public void ImportHeightmap()
        {
            this.model.Map.IfSome(
                map =>
                    {
                        var w = map.BaseTile.HeightGrid.Width;
                        var h = map.BaseTile.HeightGrid.Height;

                        var newHeightmap = this.LoadHeightmapFromUser(w, h);
                        newHeightmap.IfSome(map.ReplaceHeightmap);
                    });
        }

        public void ImportMinimap()
        {
            this.model.Map.IfSome(
                map =>
                    {
                        var minimap = this.LoadMinimapFromUser();
                        minimap.IfSome(map.SetMinimap);
                    });
        }

        public void ToggleFeatures()
        {
            this.model.FeaturesVisible = !this.model.FeaturesVisible;
        }

        public void ToggleHeightmap()
        {
            this.model.HeightmapVisible = !this.model.HeightmapVisible;
        }

        public void ToggleHeightGrid()
        {
            this.model.HeightGridVisible = !this.model.HeightGridVisible;
        }

        public void ToggleVoids()
        {
            this.model.VoidsVisible = !this.model.VoidsVisible;
        }

        public void ToggleMinimap()
        {
            this.model.MinimapVisible = !this.model.MinimapVisible;
        }

        public void OpenMapAttributes()
        {
            this.model.Map.IfSome(
                map =>
                    {
                        var r = this.dialogService.AskUserForMapAttributes(map.GetAttributes());
                        if (r != null)
                        {
                            map.UpdateAttributes(r);
                        }
                    });
        }

        public void SetSeaLevel(int value)
        {
            this.model.Map.IfSome(x => x.SetSeaLevel(value));
        }

        public void FlushSeaLevel()
        {
            this.model.Map.IfSome(x => x.FlushSeaLevel());
        }

        public void HideMinimap()
        {
            this.model.MinimapVisible = false;
        }

        public void SetViewportLocation(Point location)
        {
            this.model.SetViewportLocation(location);
        }

        public void CenterViewOnStartPosition(int startSlotIndex)
        {
            this.model.Map.IfSome(
                map =>
                    {
                        if (startSlotIndex < 0 || startSlotIndex >= 10)
                        {
                            return;
                        }

                        var variants = map.GetStartPositionVariantsForSlot(startSlotIndex);
                        var schemaIndices = new List<int>();
                        for (var s = 0; s < variants.Count; s++)
                        {
                            if (variants[s].HasValue)
                            {
                                schemaIndices.Add(s);
                            }
                        }

                        if (schemaIndices.Count == 0)
                        {
                            return;
                        }

                        var distinctPlaces = schemaIndices
                            .Select(s => variants[s].Value)
                            .GroupBy(p => (p.X, p.Y))
                            .ToList();

                        Point chosen;
                        if (distinctPlaces.Count == 1)
                        {
                            chosen = distinctPlaces[0].First();
                        }
                        else
                        {
                            var c = this.startPositionViewCycle[startSlotIndex] % schemaIndices.Count;
                            var sch = schemaIndices[c];
                            chosen = variants[sch].Value;
                            this.startPositionViewCycle[startSlotIndex] = (c + 1) % schemaIndices.Count;
                        }

                        var viewportX = chosen.X - (this.model.ViewportWidth / 2);
                        var viewportY = chosen.Y - (this.model.ViewportHeight / 2);
                        this.model.SetViewportLocation(new Point(viewportX, viewportY));
                    });
        }

        public void CenterViewOnSchemaUnit(int schemaIndex, Guid unitId)
        {
            this.model.Map.IfSome(
                map =>
                    {
                        if (schemaIndex < 0 || schemaIndex >= map.Attributes.Schemas.Count)
                        {
                            return;
                        }

                        SchemaUnit u;
                        try
                        {
                            u = map.Attributes.GetUnit(schemaIndex, unitId);
                        }
                        catch (InvalidOperationException)
                        {
                            return;
                        }

                        var viewportX = u.XPos - (this.model.ViewportWidth / 2);
                        var viewportY = u.ZPos - (this.model.ViewportHeight / 2);
                        this.model.SetViewportLocation(new Point(viewportX, viewportY));
                    });
        }

        public void SetViewportSize(Size size)
        {
            this.model.SetViewportSize(size);
        }

        public void SetStartPosition(int positionNumber, int x, int y)
        {
            this.model.Map.IfSome(map => map.DragDropStartPosition(positionNumber, x, y));
        }

        public void DragDropFeature(string featureName, int x, int y)
        {
            this.model.Map.IfSome(map => map.DragDropFeature(featureName, x, y));
        }

        public void DeleteSelection()
        {
            this.model.Map.IfSome(x => x.DeleteSelection());
        }

        public void ClearSelection()
        {
            this.model.Map.IfSome(x => x.ClearSelection());
        }

        public void DragDropStartPosition(int index, int x, int y)
        {
            this.model.Map.IfSome(map => map.DragDropStartPosition(index, x, y));
        }

        public void DragDropTile(IMapTile tile, int x, int y)
        {
            this.model.Map.IfSome(map => map.DragDropTile(tile, x, y));
        }

        public void StartBandbox(int x, int y)
        {
            this.model.Map.IfSome(map => map.StartBandbox(x, y));
        }

        public void GrowBandbox(int x, int y)
        {
            this.model.Map.IfSome(map => map.GrowBandbox(x, y));
        }

        public void CommitBandbox()
        {
            this.model.Map.IfSome(x => x.CommitBandbox());
        }

        public void TranslateSelection(int x, int y)
        {
            this.model.Map.IfSome(map => map.TranslateSelection(x, y));
        }

        public void FlushTranslation()
        {
            this.model.Map.IfSome(x => x.FlushTranslation());
        }

        public void SelectTile(int index)
        {
            this.model.Map.IfSome(x => x.SelectTile(index));
        }

        public void SelectFeature(Guid id)
        {
            this.model.Map.IfSome(x => x.SelectFeature(id));
        }

        public void SelectStartPosition(int schemaIndex, int startSlotIndex)
        {
            this.model.Map.IfSome(x => x.SelectStartPosition(schemaIndex, startSlotIndex));
        }

        public void SelectUnit(int schemaIndex, Guid unitId)
        {
            this.model.Map.IfSome(x => x.SelectUnit(new MapUnitRef(schemaIndex, unitId)));
        }

        public void PlaceUnitFromSidebar(string unitName, int x, int y, Point screenLocation)
        {
            int? player;
            if (this.model.UnitPlacementPlayerMenuChoice == UnitPlacementPlayerMenuChoice.Prompt)
            {
                player = this.dialogService.PickUnitPlayerAtScreenPoint(screenLocation);
                if (!player.HasValue)
                {
                    return;
                }
            }
            else
            {
                player = (int)this.model.UnitPlacementPlayerMenuChoice;
            }

            this.model.Map.IfSome(m => m.DragDropSchemaUnit(unitName, x, y, player.Value));
        }

        public void SetUnitPlacementPlayerMenuChoice(UnitPlacementPlayerMenuChoice choice)
        {
            this.model.UnitPlacementPlayerMenuChoice = choice;
        }

        public void SetActiveSchemaIndex(int index)
        {
            this.model.Map.IfSome(m => m.ActiveSchemaIndex = index);
        }

        public void AddMapSchema()
        {
            var input = this.dialogService.AskUserForNewSchemaType(string.Empty);
            if (input == null)
            {
                return;
            }

            var trimmed = input.Trim();
            if (trimmed.Length == 0)
            {
                this.dialogService.ShowError("Schema name cannot be empty.");
                return;
            }

            this.model.Map.IfSome(
                m =>
                    {
                        if (m.Attributes.Schemas.Any(s => string.Equals(s.SchemaType, trimmed, StringComparison.OrdinalIgnoreCase)))
                        {
                            this.dialogService.ShowError("A schema with that name already exists.");
                            return;
                        }

                        m.Attributes.AddSchema(trimmed);
                        m.ActiveSchemaIndex = m.Attributes.Schemas.Count - 1;
                    });
        }

        public void RemoveActiveMapSchema()
        {
            this.model.Map.IfSome(
                m =>
                    {
                        if (m.Attributes.RemoveSchemaAt(m.ActiveSchemaIndex))
                        {
                            m.ActiveSchemaIndex = Math.Min(m.ActiveSchemaIndex, m.Attributes.Schemas.Count - 1);
                        }
                    });
        }

        public void EditSchemaUnit(int schemaIndex, Guid unitId)
        {
            this.model.Map.IfSome(
                m =>
                    {
                        var u = m.Attributes.GetUnit(schemaIndex, unitId).ClonePreservingId();
                        using (var f = new UI.Forms.UnitPropertiesForm())
                        {
                            f.Bind(u, schemaIndex, m.Attributes.Schemas);
                            if (f.ShowDialog() != DialogResult.OK)
                            {
                                return;
                            }

                            f.ApplyTo(u);
                            var toSchema = f.SelectedSchemaIndex;
                            var n = m.Attributes.Schemas.Count;
                            if (toSchema < 0 || toSchema >= n)
                            {
                                toSchema = schemaIndex;
                            }

                            m.MoveSchemaUnitBetweenSchemas(schemaIndex, toSchema, u);
                        }
                    });
        }

        public void SetSelectedFeature(string featureName)
        {
            var featureFromTag = this.featureService.TryGetFeature(featureName);
            if (featureFromTag.HasValue)
            {
                this.featureService.SelectedFeature = featureFromTag.UnsafeValue;
            }
        }

        public void AdjustHeightBrush(int x, int y, int delta, int cursorSize)
        {
            this.model.Map.IfSome(map => map.AdjustHeightBrush(x, y, delta, cursorSize));
        }

        public void AdjustHeightBrushAtAnchor(int anchorX, int anchorY, int delta, int cursorSize)
        {
            this.model.Map.IfSome(map => map.AdjustHeightBrushAtAnchor(anchorX, anchorY, delta, cursorSize));
        }

        public void AdjustHeightPoint(int pointX, int pointY, int delta)
        {
            this.model.Map.IfSome(map => map.AdjustHeightPoint(pointX, pointY, delta));
        }

        public void SetHeightPoint(int pointX, int pointY, int value)
        {
            this.model.Map.IfSome(map => map.SetHeightPoint(pointX, pointY, value));
        }

        public void SetHeightBrushAtAnchor(int anchorX, int anchorY, int value, int cursorSize)
        {
            this.model.Map.IfSome(map => map.SetHeightBrushAtAnchor(anchorX, anchorY, value, cursorSize));
        }

        public void FlushHeightBrush()
        {
            this.model.Map.IfSome(x => x.FlushHeightBrush());
        }

        public void SetHeightEditInterval(int interval)
        {
            this.model.HeightEditInterval = interval;
        }

        public void SetHeightEditMode(HeightEditMode mode)
        {
            this.model.HeightEditMode = mode;
        }

        public void SetHeightEditSetValue(int value)
        {
            this.model.HeightEditSetValue = value;
        }

        public void SetHeightEditCursorSize(int cursorSize)
        {
            this.model.HeightEditCursorSize = cursorSize;
        }

        public void SetVoidEditCursorSize(int cursorSize)
        {
            this.model.VoidEditCursorSize = cursorSize;
        }

        public void SetVoidBrushAtAnchor(int anchorX, int anchorY, int cursorSize, bool value)
        {
            this.model.Map.IfSome(map => map.SetVoidBrushAtAnchor(anchorX, anchorY, cursorSize, value));
        }

        public void FlushVoidBrush()
        {
            this.model.Map.IfSome(map => map.FlushVoidBrush());
        }

        public void ChangeSelectedTab(GUITab tab)
        {
            this.model.SelectedGUITab = tab;
            this.SetSelectedGUITabForMap();
        }

        private static IEnumerable<string> GetMapNames(HpiArchive hpi)
        {
            return hpi.GetFiles("maps")
                .Where(x => x.Name.EndsWith(".tnt", StringComparison.OrdinalIgnoreCase))
                .Select(x => HpiPath.GetFileNameWithoutExtension(x.Name));
        }

        private static void Save(UndoableMapModel map, string filename)
        {
            // flatten before save --- only the base tile is written to disk
            map.ClearSelection();

            var tntName = filename;
            var otaName = Path.ChangeExtension(filename, ".ota");

            var tmpTntName = tntName + ".mappytemp";
            var tmpOtaName = otaName + ".mappytemp";

            try
            {
                MapSaver.SaveTnt(map, tmpTntName);
                MapSaver.SaveOta(map, tmpOtaName);
                File.Delete(tntName);
                File.Delete(otaName);
                File.Move(tmpTntName, tntName);
                File.Move(tmpOtaName, otaName);
            }
            catch
            {
                // Normally the temp files are deleted by File.Replace.
                // Ensure that they are always deleted if an error occurs.
                File.Delete(tmpTntName);
                File.Delete(tmpOtaName);
                throw;
            }

            map.MarkSaved(filename);
        }

        private static bool TryCopyToClipboard(UndoableMapModel map)
        {
            if (map.SelectedFeatures.Count > 0)
            {
                if (map.SelectedFeatures.Count == 1)
                {
                    var id = map.SelectedFeatures.First();
                    var inst = map.GetFeatureInstance(id);
                    var rec = new FeatureClipboardRecord(inst.FeatureName);
                    Clipboard.SetData(DataFormats.Serializable, rec);
                    return true;
                }

                var loc = map.ViewportLocation;
                var ids = map.SelectedFeatures.ToArray();
                var features = new List<FeatureClipboardRecord>();

                for (int i = 0; i < ids.Length; i++)
                {
                    var ins = map.GetFeatureInstance(ids[i]);
                    features.Add(new FeatureClipboardRecord(ins.FeatureName, (ins.X * 16) - loc.X, (ins.Y * 16) - loc.Y));
                }

                Clipboard.SetData(DataFormats.Serializable, features);
                return true;
            }

            if (map.SelectedUnits.Count > 0)
            {
                var vp = map.ViewportLocation;
                var records = new List<SchemaUnitClipboardRecord>();
                foreach (var r in map.SelectedUnits)
                {
                    var u = map.Attributes.GetUnit(r.SchemaIndex, r.UnitId);
                    records.Add(new SchemaUnitClipboardRecord(u, vp));
                }

                if (records.Count == 1)
                {
                    Clipboard.SetData(DataFormats.Serializable, records[0]);
                }
                else
                {
                    Clipboard.SetData(DataFormats.Serializable, records);
                }

                return true;
            }

            if (map.SelectedTile.HasValue)
            {
                var tile = map.FloatingTiles[map.SelectedTile.Value].Item;
                Clipboard.SetData(DataFormats.Serializable, tile);
                return true;
            }

            return false;
        }

        private void PasteSchemaUnitsFromClipboardRecords(UndoableMapModel map, IList<SchemaUnitClipboardRecord> records)
        {
            if (records == null || records.Count == 0)
            {
                return;
            }

            var vp = map.ViewportLocation;
            var mw = map.MapWidth * 32;
            var mh = map.MapHeight * 32;
            var units = new List<SchemaUnit>(records.Count);
            foreach (var r in records)
            {
                units.Add(r.ToNewSchemaUnit(vp, mw, mh));
            }

            map.PasteSchemaUnitCopies(units);
        }

        private void SetSelectedGUITabForMap()
        {
            this.model.Map.IfSome(x => x.UpdateSelectedGUITab(this.model.SelectedGUITab));
        }

        private void SaveHpi(UndoableMapModel map, string filename)
        {
            // flatten before save --- only the base tile is written to disk
            map.ClearSelection();

            var randomValue = this.rng.Next(1000);
            var tmpExtension = $".mappytemp-{randomValue}";

            var tmpFileName = Path.ChangeExtension(filename, tmpExtension);
            try
            {
                MapSaver.SaveHpi(map, tmpFileName);
                File.Delete(filename);
                File.Move(tmpFileName, filename);
            }
            catch
            {
                // Normally the temp file is deleted by File.Replace.
                // Ensure that it is always deleted if an error occurs.
                File.Delete(tmpFileName);
                throw;
            }

            map.MarkSaved(filename);
        }

        private void DeduplicateTiles(IGrid<Bitmap> tiles)
        {
            var len = tiles.Width * tiles.Height;
            for (var i = 0; i < len; i++)
            {
                tiles[i] = this.tileCache.GetOrAddBitmap(tiles[i]);
            }
        }

        private bool SaveHelper(UndoableMapModel map, string filename)
        {
            if (filename == null)
            {
                throw new ArgumentNullException(nameof(filename));
            }

            var extension = Path.GetExtension(filename).ToUpperInvariant();

            try
            {
                switch (extension)
                {
                    case ".TNT":
                        Save(map, filename);
                        return true;
                    case ".HPI":
                    case ".UFO":
                    case ".CCX":
                    case ".GPF":
                    case ".GP3":
                        this.SaveHpi(map, filename);
                        return true;
                    default:
                        this.dialogService.ShowError("Unrecognized file extension: " + extension);
                        return false;
                }
            }
            catch (IOException e)
            {
                this.dialogService.ShowError("Error saving map: " + e.Message);
                return false;
            }
        }

        private void OpenMap(string filename)
        {
            var ext = Path.GetExtension(filename) ?? string.Empty;
            ext = ext.ToUpperInvariant();

            try
            {
                switch (ext)
                {
                    case ".HPI":
                    case ".UFO":
                    case ".CCX":
                    case ".GPF":
                    case ".GP3":
                        this.OpenFromHapi(filename);
                        return;
                    case ".TNT":
                        this.OpenTnt(filename);
                        return;
                    case ".SCT":
                        this.OpenSct(filename);
                        return;
                    default:
                        this.dialogService.ShowError($"Mappy doesn't know how to open {ext} files");
                        return;
                }
            }
            catch (IOException e)
            {
                this.dialogService.ShowError("IO error opening map: " + e.Message);
            }
            catch (ParseException e)
            {
                this.dialogService.ShowError("Cannot open map: " + e.Message);
            }
        }

        private void OpenFromHapi(string filename)
        {
            List<string> maps;
            bool readOnly;

            using (var h = new HpiArchive(filename))
            {
                maps = GetMapNames(h).ToList();
            }

            string mapName;
            switch (maps.Count)
            {
                case 0:
                    this.dialogService.ShowError("No maps found in " + filename);
                    return;
                case 1:
                    mapName = maps.First();
                    readOnly = false;
                    break;
                default:
                    maps.Sort();
                    mapName = this.dialogService.AskUserToChooseMap(maps);
                    readOnly = true;
                    break;
            }

            if (mapName == null)
            {
                return;
            }

            var tntPath = HpiPath.Combine("maps", mapName + ".tnt");
            var mapModel = this.mapLoadingService.CreateFromHpi(filename, tntPath, readOnly);
            this.model.Map = Maybe.Some(mapModel);
            this.IngestOtaUnitNames(mapModel);
            this.SetSelectedGUITabForMap();
        }

        private void OpenTnt(string filename)
        {
            var mapModel = this.mapLoadingService.CreateFromTnt(filename);
            this.model.Map = Maybe.Some(mapModel);
            this.IngestOtaUnitNames(mapModel);
            this.SetSelectedGUITabForMap();
        }

        private bool CheckOkayDiscard()
        {
            return this.model.Map.Match(
                some: map =>
                    {
                        if (map.IsMarked)
                        {
                            return true;
                        }

                        var r = this.dialogService.AskUserToDiscardChanges();
                        switch (r)
                        {
                            case DialogResult.Yes:
                                return this.Save();
                            case DialogResult.Cancel:
                                return false;
                            case DialogResult.No:
                                return true;
                            default:
                                throw new InvalidOperationException("unexpected dialog result: " + r);
                        }
                    },
                none: () => true);
        }

        private void New(int width, int height)
        {
            var mapModel = MapLoadingService.CreateMap(width, height);
            this.model.Map = Maybe.Some(mapModel);
            this.IngestOtaUnitNames(mapModel);
            this.SetSelectedGUITabForMap();
        }

        private void OpenSct(string filename)
        {
            var mapModel = this.mapLoadingService.CreateFromSct(filename);
            this.model.Map = Maybe.Some(mapModel);
            this.IngestOtaUnitNames(mapModel);
        }

        /// <summary>
        /// Ensures unit types referenced by the map appear in the placement list even when
        /// archive scanning missed them or the user has minimal search paths.
        /// </summary>
        private void IngestOtaUnitNames(UndoableMapModel map)
        {
            if (map == null)
            {
                return;
            }

            var names = map.Attributes.Schemas
                .SelectMany(s => s.Units)
                .Select(u => u.Unitname)
                .Where(n => !string.IsNullOrWhiteSpace(n));
            this.unitCatalogService.AddNames(names);
        }

        private Maybe<Grid<int>> LoadHeightmapFromUser(int width, int height)
        {
            var loc = this.dialogService.AskUserToChooseHeightmap(width, height);
            if (loc == null)
            {
                return Maybe.None<Grid<int>>();
            }

            try
            {
                var bmp = Util.BitmapFromFile(loc);

                if (bmp.Width != width || bmp.Height != height)
                {
                    var msg = string.Format(
                        "Heightmap has incorrect dimensions. The required dimensions are {0}x{1}.",
                        width,
                        height);
                    this.dialogService.ShowError(msg);
                    return Maybe.None<Grid<int>>();
                }

                return Maybe.Some(ImgUtil.ReadHeightmap(bmp));
            }
            catch (Exception)
            {
                this.dialogService.ShowError("There was a problem importing the selected heightmap");
                return Maybe.None<Grid<int>>();
            }
        }

        private Maybe<Bitmap> LoadMinimapFromUser()
        {
            var loc = this.dialogService.AskUserToChooseMinimap();
            if (loc == null)
            {
                return Maybe.None<Bitmap>();
            }

            try
            {
                var bmp = Util.BitmapFromFile(loc);

                if (bmp.Width > TntConstants.MaxMinimapWidth
                    || bmp.Height > TntConstants.MaxMinimapHeight)
                {
                    var msg = string.Format(
                        "Minimap dimensions too large. The maximum size is {0}x{1}.",
                        TntConstants.MaxMinimapWidth,
                        TntConstants.MaxMinimapHeight);

                    this.dialogService.ShowError(msg);
                    return Maybe.None<Bitmap>();
                }

                Quantization.ToTAPalette(bmp);
                return Maybe.Some(bmp);
            }
            catch (Exception)
            {
                this.dialogService.ShowError("There was a problem importing the selected minimap.");
                return Maybe.None<Bitmap>();
            }
        }

        private void RefreshMinimapHighQualityWithProgressHelper(UndoableMapModel map)
        {
            var worker = Util.RenderMinimapWorker();

            var dlg = this.dialogService.CreateProgressView();
            dlg.Title = "Generating Minimap";
            dlg.MessageText = "Generating high quality minimap...";

            dlg.CancelPressed += (o, args) => worker.CancelAsync();
            worker.ProgressChanged += (o, args) => dlg.Progress = args.ProgressPercentage;
            worker.RunWorkerCompleted += (o, args) =>
                {
                    if (args.Error != null)
                    {
                        Program.HandleUnexpectedException(args.Error);
                        Application.Exit();
                        return;
                    }

                    if (!args.Cancelled)
                    {
                        var img = (Bitmap)args.Result;
                        map.SetMinimap(img);
                    }

                    dlg.Close();
                };

            worker.RunWorkerAsync(new Util.RenderMinimapArgs { MapModel = map, FeatureService = this.featureService });
            dlg.Display();
        }

        private void ExportHeightmapHelper(UndoableMapModel map)
        {
            var loc = this.dialogService.AskUserToSaveHeightmap();
            if (loc == null)
            {
                return;
            }

            try
            {
                var b = ImgUtil.GetBitmapFromHeightmapGrid(map.BaseTile.HeightGrid);
                using (var s = File.Create(loc))
                {
                    b.Save(s, ImageFormat.Png);
                }
            }
            catch (Exception)
            {
                this.dialogService.ShowError("There was a problem saving the heightmap.");
            }
        }

        private void ExportMinimapHelper(UndoableMapModel map)
        {
            var loc = this.dialogService.AskUserToSaveMinimap();
            if (loc == null)
            {
                return;
            }

            try
            {
                using (var s = File.Create(loc))
                {
                    map.Minimap.Save(s, ImageFormat.Png);
                }
            }
            catch (Exception)
            {
                this.dialogService.ShowError("There was a problem saving the minimap.");
            }
        }

        private void ExportMapImageHelper(UndoableMapModel map)
        {
            var loc = this.dialogService.AskUserToSaveMapImage();
            if (loc == null)
            {
                return;
            }

            var pv = this.dialogService.CreateProgressView();

            var tempLoc = loc + ".mappy-partial";

            var bg = new BackgroundWorker();
            bg.WorkerReportsProgress = true;
            bg.WorkerSupportsCancellation = true;
            bg.DoWork += (sender, args) =>
                {
                    var worker = (BackgroundWorker)sender;
                    using (var s = File.Create(tempLoc))
                    {
                        var success = Util.WriteMapImage(
                            s,
                            map.BaseTile.TileGrid,
                            worker.ReportProgress,
                            () => worker.CancellationPending);
                        args.Cancel = !success;
                    }
                };

            bg.ProgressChanged += (sender, args) => pv.Progress = args.ProgressPercentage;
            pv.CancelPressed += (sender, args) => bg.CancelAsync();

            bg.RunWorkerCompleted += (sender, args) =>
                {
                    try
                    {
                        pv.Close();

                        if (args.Cancelled)
                        {
                            return;
                        }

                        if (args.Error != null)
                        {
                            this.dialogService.ShowError("There was a problem saving the map image.");
                            return;
                        }

                        if (File.Exists(loc))
                        {
                            File.Replace(tempLoc, loc, null);
                        }
                        else
                        {
                            File.Move(tempLoc, loc);
                        }
                    }
                    finally
                    {
                        if (File.Exists(tempLoc))
                        {
                            File.Delete(tempLoc);
                        }
                    }
                };

            bg.RunWorkerAsync();
            pv.Display();
        }

        private void ImportCustomSectionInteractiveHelper(UndoableMapModel map)
        {
            var paths = this.dialogService.AskUserToChooseSectionImportPaths();
            if (paths == null)
            {
                return;
            }

            var dlg = this.dialogService.CreateProgressView();

            var bg = new BackgroundWorker();
            bg.WorkerSupportsCancellation = true;
            bg.WorkerReportsProgress = true;
            bg.DoWork += (sender, args) =>
                {
                    var w = (BackgroundWorker)sender;
                    var sect = this.imageImportingService.ImportSection(
                        paths.GraphicPath,
                        paths.HeightmapPath,
                        w.ReportProgress,
                        () => w.CancellationPending);
                    if (sect == null)
                    {
                        args.Cancel = true;
                        return;
                    }

                    args.Result = sect;
                };

            bg.ProgressChanged += (sender, args) => dlg.Progress = args.ProgressPercentage;
            dlg.CancelPressed += (sender, args) => bg.CancelAsync();

            bg.RunWorkerCompleted += (sender, args) =>
                {
                    dlg.Close();

                    if (args.Error != null)
                    {
                        this.dialogService.ShowError(
                            "There was a problem importing the section: " + args.Error.Message);
                        return;
                    }

                    if (args.Cancelled)
                    {
                        return;
                    }

                    map.PasteMapTileNoDeduplicateTopLeft((IMapTile)args.Result);
                };

            bg.RunWorkerAsync();

            dlg.Display();
        }

        private void ImportCustomSectionHelper(UndoableMapModel map, string graphicPath, string heightmapPath)
        {
            if (string.IsNullOrEmpty(graphicPath) && string.IsNullOrEmpty(heightmapPath))
            {
                return;
            }

            var bg = new BackgroundWorker();
            bg.WorkerSupportsCancellation = true;
            bg.WorkerReportsProgress = true;
            bg.DoWork += (sender, args) =>
            {
                var w = (BackgroundWorker)sender;
                var sect = this.imageImportingService.ImportSection(
                    graphicPath,
                    heightmapPath,
                    w.ReportProgress,
                    () => w.CancellationPending);
                if (sect == null)
                {
                    args.Cancel = true;
                    return;
                }

                args.Result = sect;
            };

            bg.RunWorkerCompleted += (sender, args) =>
            {
                if (args.Error != null)
                {
                    this.dialogService.ShowError(
                        "There was a problem importing the section: " + args.Error.Message);
                    return;
                }

                if (args.Cancelled)
                {
                    return;
                }

                map.PasteMapTileNoDeduplicateTopLeft((IMapTile)args.Result);
            };

            bg.RunWorkerAsync();
        }
    }
}
