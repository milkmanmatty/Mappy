﻿namespace Mappy.Services
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
    using Mappy.UI.Controls;
    using Mappy.UI.Forms;
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

        private readonly MapLoadingService mapLoadingService;

        private readonly ImageImportService imageImportingService;

        private readonly BitmapCache tileCache;

        private readonly Random rng = new Random();

        private readonly SectionView sectionView;

        private readonly FeatureView featureView;

        private readonly MainForm mainForm;

        private readonly AccessibleFeatures accessibleFeatures;

        public Dispatcher(
            CoreModel model,
            IDialogService dialogService,
            SectionService sectionService,
            SectionBitmapService sectionBitmapService,
            FeatureService featureService,
            MapLoadingService mapLoadingService,
            ImageImportService imageImportingService,
            BitmapCache tileCache,
            MainForm mainForm)
        {
            this.model = model;
            this.dialogService = dialogService;
            this.sectionService = sectionService;
            this.sectionBitmapService = sectionBitmapService;
            this.featureService = featureService;
            this.mapLoadingService = mapLoadingService;
            this.imageImportingService = imageImportingService;
            this.tileCache = tileCache;
            this.mainForm = mainForm;
            this.sectionView = mainForm.SectionView;
            this.featureView = mainForm.FeatureView;
            this.accessibleFeatures = new AccessibleFeatures();
        }

        // I feel that this is almost completely the wrong place to put this, but have no better ideas.
        public static IMapTile FillTile { get; set; }

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

                    LoadResult<Section> result;
                    if (!SectionLoadingUtils.LoadSections(
                        i => w.ReportProgress((50 * i) / 100),
                        () => w.CancellationPending,
                        out result))
                    {
                        args.Cancel = true;
                        return;
                    }

                    LoadResult<Feature> featureResult;
                    if (!FeatureLoadingUtils.LoadFeatures(
                        i => w.ReportProgress(50 + ((50 * i) / 100)),
                        () => w.CancellationPending,
                        out featureResult))
                    {
                        args.Cancel = true;
                        return;
                    }

                    args.Result = new SectionFeatureLoadResult(
                            result.Records,
                            featureResult.Records,
                            result.Errors
                                .Concat(featureResult.Errors)
                                .GroupBy(x => x.HpiPath)
                                .Select(x => x.First())
                                .ToList(),
                            result.FileErrors
                                .Concat(featureResult.FileErrors)
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
            this.dialogService.CapturePreferences();
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
                        var data = Clipboard.GetData(DataFormats.Serializable);
                        if (data == null)
                        {
                            return;
                        }

                        var loc = map.ViewportLocation;
                        loc.X += this.model.ViewportWidth / 2;
                        loc.Y += this.model.ViewportHeight / 2;

                        var tile = data as IMapTile;
                        if (tile != null)
                        {
                            this.DeduplicateTiles(tile.TileGrid);
                            map.PasteMapTile(tile, loc.X, loc.Y);
                        }
                        else
                        {
                            var record = data as FeatureClipboardRecord;
                            if (record != null)
                            {
                                map.DragDropFeature(record.FeatureName, loc.X, loc.Y);
                                return;
                            }

                            var featureList = data as List<FeatureClipboardRecord>;
                            if (featureList != null)
                            {
                                map.Deselect();
                                var placedFeatures = new List<DrawableItem>();
                                foreach (var feature in featureList)
                                {
                                    // Split these up so they can be debugged better
                                    int xLocUnsafe = map.ViewportLocation.X + feature.VPOffsetX;
                                    int xLoc = Math.Min(map.MapWidth * 32, Math.Max(0, xLocUnsafe));    // force between 0 and MapWidth

                                    int yLocUnsafe = map.ViewportLocation.Y + feature.VPOffsetY;
                                    int yLoc = Math.Min(map.MapHeight * 32, Math.Max(0, yLocUnsafe));   // force between 0 and MapHeight

                                    map.DragDropFeatureWithoutDeselect(feature.FeatureName, xLoc, yLoc);
                                }
                            }
                        }
                    });
        }

        public void FillSelection()
        {
            this.model.Map.IfSome(
                map =>
                    {
                        if (TryCopyForFill(map))
                        {
                            var data = FillTile;
                            if (data == null)
                            {
                                return;
                            }

                            // Should never be anything but a tile section (no features etc.)
                            var tile = data as IMapTile;
                            if (tile != null)
                            {
                                // Do some quick maffs, figure out how many iterations are needs for the current tile to fill the entire map.
                                int xIterations = (int)Math.Ceiling((double)map.MapWidth / (double)tile.TileGrid.Width);
                                int yIterations = (int)Math.Ceiling((double)map.MapHeight / (double)tile.TileGrid.Height);

                                this.DeduplicateTiles(tile.TileGrid); // Is this needed within the loops? TODO: investigate

                                // Maybe should start at 1? But can't assume the current selected tile is in top left (0,0)
                                for (int x = 0; x < xIterations; x++)
                                {
                                    for (int y = 0; y < yIterations; y++)
                                    {
                                        int testLocX = (x * tile.TileGrid.Width * 32) + (tile.TileGrid.Width * 16);
                                        int testLocY = (y * tile.TileGrid.Height * 32) + (tile.TileGrid.Height * 16);
                                        map.PasteMapTile(tile, testLocX, testLocY);
                                    }
                                }

                                this.DeduplicateTiles(tile.TileGrid); // For good measure
                            }
                        }
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
            this.model.Map.IfSome(this.ImportCustomSectionHelper);
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

        public void SetViewportSize(Size size)
        {
            this.model.SetViewportSize(size);
        }

        public void SetStartPosition(int positionNumber, int x, int y)
        {
            this.model.Map.IfSome(map => map.DragDropStartPosition(positionNumber, x, y));
        }

        public Maybe<FeatureInstance> DragDropFeature(string featureName, int x, int y)
        {
            List<Maybe<FeatureInstance>> featsAdded = new List<Maybe<FeatureInstance>>();
            this.model.Map.IfSome(map => featsAdded.Add(map.DragDropFeature(featureName, x, y)));
            return featsAdded.FirstOrDefault();
        }

        public void PlaceFeature(int x, int y)
        {
            // Cheeky null check
            if (this.featureView != null)
            {
                string featName = this.featureView.GetCurrentSelectedItem().Text;
                this.model.Map.IfSome(map => map.DragDropFeature(featName, x, y));
            }
        }

        public Maybe<Feature> FetchCurrentFeatureListSelection()
        {
            try
            {
                return this.featureService.TryGetFeature(this.featureView.GetCurrentSelectedItem().Text);
            }
            catch (NullReferenceException)
            {
                return this.featureService.TryGetFeature(string.Empty);
            }
        }

        public void SubscribeToFeatures(IObservable<ILayer> source)
        {
            source.Subscribe(this.accessibleFeatures);
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
            this.model.Map.IfSome(x => x.CommitBandbox(this.mainForm.ActiveTab));
        }

        public Rectangle FetchBandbox()
        {
            return this.model.Map.HasValue ? this.model.Map.UnsafeValue.BandboxRectangle : new Rectangle(); // default value type constructor throws a warning, but "default" cannot be used in PR
        }

        public Point FetchBandboxStartLoc()
        {
            return this.model.Map.HasValue ? this.model.Map.UnsafeValue.BandboxStart : new Point(-1, -1); // default value type constructor throws a warning, but "default" cannot be used in PR
        }

        public Point FetchBandboxFinishLoc()
        {
            return this.model.Map.HasValue ? this.model.Map.UnsafeValue.BandboxFinish : new Point(-1, -1); // default value type constructor throws a warning, but "default" cannot be used in PR
        }

        public ActiveTab FetchActiveTab()
        {
            return this.mainForm.ActiveTab;
        }

        public FeaturePlacementMode FetchCurrentFeaturePlacementMode()
        {
            return this.featureView.ActiveFeaturePlacementMode;
        }

        public int FetchMagnitude()
        {
            return this.featureView.Magnitude;
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

        public void SelectFeatures(List<FeatureInstance> features)
        {
            foreach (var f in features)
            {
                this.model.Map.IfSome(x => x.SelectFeatureWithoutDeselect(f.Id));
            }
        }

        public void SelectStartPosition(int index)
        {
            this.model.Map.IfSome(x => x.SelectStartPosition(index));
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
                MapSaver.SaveOta(map.Attributes, tmpOtaName);
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

            if (map.SelectedTile.HasValue)
            {
                var tile = map.FloatingTiles[map.SelectedTile.Value].Item;
                Clipboard.SetData(DataFormats.Serializable, tile);
                return true;
            }

            return false;
        }

        private static bool TryCopyForFill(UndoableMapModel map)
        {
            if (map.SelectedTile.HasValue)
            {
                FillTile = map.FloatingTiles[map.SelectedTile.Value].Item;
                return true;
            }

            return false;
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
                        break;
                    case ".TNT":
                        this.OpenTnt(filename);
                        break;
                    case ".SCT":
                        this.OpenSct(filename);
                        break;
                    default:
                        this.dialogService.ShowError($"Mappy doesn't know how to open {ext} files");
                        return;
                }

                this.SetAccessibleFeatures();
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
            this.model.Map = Maybe.Some(this.mapLoadingService.CreateFromHpi(filename, tntPath, readOnly));
        }

        private void OpenTnt(string filename)
        {
            this.model.Map = Maybe.Some(this.mapLoadingService.CreateFromTnt(filename));
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
            this.model.Map = Maybe.Some(MapLoadingService.CreateMap(width, height));
            this.SetAccessibleFeatures();
        }

        private void OpenSct(string filename)
        {
            this.model.Map = Maybe.Some(this.mapLoadingService.CreateFromSct(filename));
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
                Bitmap bmp;
                using (var s = File.OpenRead(loc))
                {
                    bmp = (Bitmap)Image.FromStream(s);
                }

                if (bmp.Width != width || bmp.Height != height)
                {
                    var msg = string.Format(
                        "Heightmap has incorrect dimensions. The required dimensions are {0}x{1}.",
                        width,
                        height);
                    this.dialogService.ShowError(msg);
                    return Maybe.None<Grid<int>>();
                }

                return Maybe.Some(Mappy.Util.Util.ReadHeightmap(bmp));
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
                Bitmap bmp;
                using (var s = File.OpenRead(loc))
                {
                    bmp = (Bitmap)Image.FromStream(s);
                }

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
            var worker = Mappy.Util.Util.RenderMinimapWorker();

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

            worker.RunWorkerAsync(map);
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
                var b = Mappy.Util.Util.ExportHeightmap(map.BaseTile.HeightGrid);
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
                        var success = Mappy.Util.Util.WriteMapImage(
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

        private void ImportCustomSectionHelper(UndoableMapModel map)
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

        private void SetAccessibleFeatures()
        {
            if (this.model.Map.IsSome)
            {
                this.model.Map.UnsafeValue.SetAccessibleFeatures(this.accessibleFeatures);
            }
        }
    }
}
