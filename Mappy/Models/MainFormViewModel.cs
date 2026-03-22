namespace Mappy.Models
{
    using System;
    using System.Drawing;
    using System.Globalization;
    using System.Linq;
    using System.Reactive;
    using System.Reactive.Linq;
    using Mappy;
    using Mappy.Collections;
    using Mappy.Data;
    using Mappy.Models.Enums;
    using Mappy.Services;
    using Mappy.Util;

    public class MainFormViewModel : IMainFormViewModel
    {
        private const string ProgramName = "Mappy";

        private readonly Dispatcher dispatcher;

        public MainFormViewModel(IReadOnlyApplicationModel model, Dispatcher dispatcher, FeatureService featureService)
        {
            var map = model.PropertyAsObservable(x => x.Map, nameof(model.Map));
            var mapOpen = map.Select(x => x.IsSome);
            var isDirty = map.ObservePropertyOrDefault(x => x.IsMarked, "IsMarked", true).Select(x => !x);
            var filePath = map.ObservePropertyOrDefault(x => x.FilePath, "FilePath", null);
            var isFileReadOnly = map.ObservePropertyOrDefault(x => x.IsFileReadOnly, "IsFileReadOnly", false);

            this.CanUndo = map.ObservePropertyOrDefault(x => x.CanUndo, nameof(UndoableMapModel.CanUndo), false);
            this.CanRedo = map.ObservePropertyOrDefault(x => x.CanRedo, nameof(UndoableMapModel.CanRedo), false);
            this.CanCut = map.ObservePropertyOrDefault(x => x.CanCut, nameof(UndoableMapModel.CanCut), false);
            this.CanCopy = map.ObservePropertyOrDefault(x => x.CanCopy, nameof(UndoableMapModel.CanCopy), false);
            this.CanPaste = map.Select(x => x.IsSome);
            this.CanFill = map.ObservePropertyOrDefault(x => x.CanFill, nameof(UndoableMapModel.CanFill), false);
            this.CanFlip = map.ObservePropertyOrDefault(x => x.CanFlip, nameof(UndoableMapModel.CanFlip), false);
            this.GridVisible = model.PropertyAsObservable(x => x.GridVisible, nameof(model.GridVisible));
            this.GridSize = model.PropertyAsObservable(x => x.GridSize, nameof(model.GridSize));
            this.HeightmapVisible = model.PropertyAsObservable(x => x.HeightmapVisible, nameof(model.HeightmapVisible));
            this.HeightGridVisible = model.PropertyAsObservable(x => x.HeightGridVisible, nameof(model.HeightGridVisible));
            this.VoidsVisible = model.PropertyAsObservable(x => x.VoidsVisible, nameof(model.VoidsVisible));
            this.FeaturesVisible = model.PropertyAsObservable(x => x.FeaturesVisible, nameof(model.FeaturesVisible));
            this.MinimapVisible = model.PropertyAsObservable(x => x.MinimapVisible, nameof(model.MinimapVisible));
            this.SeaLevel = map.ObservePropertyOrDefault(x => x.SeaLevel, "SeaLevel", 0);
            this.HeightEditInterval = model.PropertyAsObservable(x => x.HeightEditInterval, nameof(model.HeightEditInterval));
            this.HeightEditMode = model.PropertyAsObservable(x => x.HeightEditMode, nameof(model.HeightEditMode));
            this.HeightEditSetValue = model.PropertyAsObservable(x => x.HeightEditSetValue, nameof(model.HeightEditSetValue));
            this.HeightEditCursorSize = model.PropertyAsObservable(x => x.HeightEditCursorSize, nameof(model.HeightEditCursorSize));
            this.VoidEditCursorSize = model.PropertyAsObservable(x => x.VoidEditCursorSize, nameof(model.VoidEditCursorSize));

            this.CanSaveAs = mapOpen;
            this.CanCloseMap = mapOpen;
            this.CanImportMinimap = mapOpen;
            this.CanExportMinimap = mapOpen;
            this.CanImportHeightmap = mapOpen;
            this.CanExportHeightmap = mapOpen;
            this.CanImportCustomSection = mapOpen;
            this.CanExportMapImage = mapOpen;
            this.CanGenerateMinimap = mapOpen;
            this.CanGenerateMinimapHighQuality = mapOpen;
            this.CanOpenAttributes = mapOpen;
            this.CanResizeMap = mapOpen;
            this.CanChangeSeaLevel = mapOpen;

            // set up CanSave observable
            var canSave = Observable.CombineLatest(
                mapOpen,
                filePath.Select(x => x != null),
                isFileReadOnly.Select(x => !x))
                .Select(x => x.All(y => y))
                .Replay(1);
            canSave.Connect();
            this.CanSave = canSave;

            // set up TitleText observable
            var cleanFileName = filePath.Select(x => (x ?? "Untitled"));
            var dirtyFileName = cleanFileName.Select(x => x + "*");

            var fileName = isDirty
                .Select(x => x ? dirtyFileName : cleanFileName)
                .Switch();
            var readOnlyFileName = fileName.Select(y => y + " [read only]");

            var fileNameTitle = isFileReadOnly
                .Select(x => x ? readOnlyFileName : fileName)
                .Switch();

            var defaultTitle = Observable.Return(ProgramName);
            var openFileTitle = fileNameTitle.Select(y => y + " - " + ProgramName);
            var titleText = mapOpen
                .Select(x => x ? openFileTitle : defaultTitle)
                .Switch()
                .Replay(1);
            titleText.Connect();

            this.TitleText = titleText;

            var mousePosition = map.ObservePropertyOrDefault(m => m.MousePosition, "MousePosition", Maybe.None<Point>());
            this.MousePositionText = mousePosition
                .Select(p => p.Match(pos => $"X: {pos.X}, Y: {pos.Y}", () => "X: -, Y: -"));
            this.HeightText = map.CombineLatest(
                mousePosition,
                (currentMap, mousePos) => currentMap.Match(
                    m => mousePos.Match(pos => GetHeightText(m, pos), () => "H: -"),
                    () => "H: -"));
            this.VoidText = map.CombineLatest(
                mousePosition,
                (currentMap, mousePos) => currentMap.Match(
                    m => mousePos.Match(pos => GetVoidText(m, pos), () => "Void: -"),
                    () => "Void: -"));

            var hoveredFeatureId = map.ObservePropertyOrDefault(m => m.HoveredFeature, "HoveredFeature", Maybe.None<Guid>());
            var featureStatusRefresh = Observable.Merge(
                Observable.Return(Unit.Default),
                Observable.FromEventPattern(
                    h => featureService.FeaturesChanged += h,
                    h => featureService.FeaturesChanged -= h).Select(_ => Unit.Default));

            this.HoveredFeatureText = hoveredFeatureId.CombineLatest(featureStatusRefresh, (id, _) => id)
                .Select(id => id.Select(idd =>
                {
                    var featureName = model.Map.UnsafeValue.GetFeatureInstance(idd).FeatureName;
                    return featureService.TryGetFeature(featureName).Select(feature =>
                    {
                        var reclaimInfo = feature.ReclaimInfo.Match(rec => $" E: {rec.EnergyValue}, M: {rec.MetalValue}", () => string.Empty);
                        var metalSpotInfo = feature.MetalSpotValue > 0
                            ? $" Metal spot: {FormatMetalSpotStatusValue(feature)}"
                            : string.Empty;
                        return $"{featureName}{reclaimInfo}{metalSpotInfo}";
                    }).Or(featureName);
                }).Or("---"));

            this.dispatcher = dispatcher;
        }

        public IObservable<bool> CanCloseMap { get; }

        public IObservable<bool> CanSave { get; }

        public IObservable<bool> CanSaveAs { get; }

        public IObservable<bool> CanImportMinimap { get; }

        public IObservable<bool> CanExportMinimap { get; }

        public IObservable<bool> CanImportHeightmap { get; }

        public IObservable<bool> CanExportHeightmap { get; }

        public IObservable<bool> CanImportCustomSection { get; }

        public IObservable<bool> CanExportMapImage { get; }

        public IObservable<bool> CanGenerateMinimap { get; }

        public IObservable<bool> CanGenerateMinimapHighQuality { get; }

        public IObservable<bool> CanOpenAttributes { get; }

        public IObservable<bool> CanResizeMap { get; }

        public IObservable<bool> CanChangeSeaLevel { get; }

        public IObservable<string> TitleText { get; }

        public IObservable<bool> CanUndo { get; }

        public IObservable<bool> CanRedo { get; }

        public IObservable<bool> CanCut { get; }

        public IObservable<bool> CanCopy { get; }

        public IObservable<bool> CanPaste { get; }

        public IObservable<bool> CanFill { get; }

        public IObservable<bool> CanFlip { get; }

        public IObservable<bool> GridVisible { get; }

        public IObservable<Size> GridSize { get; }

        public IObservable<bool> HeightmapVisible { get; }

        public IObservable<bool> HeightGridVisible { get; }

        public IObservable<bool> VoidsVisible { get; }

        public IObservable<bool> FeaturesVisible { get; }

        public IObservable<bool> MinimapVisible { get; }

        public IObservable<int> SeaLevel { get; }

        public IObservable<int> HeightEditInterval { get; }

        public IObservable<HeightEditMode> HeightEditMode { get; }

        public IObservable<int> HeightEditSetValue { get; }

        public IObservable<int> HeightEditCursorSize { get; }

        public IObservable<int> VoidEditCursorSize { get; }

        public IObservable<string> MousePositionText { get; }

        public IObservable<string> HeightText { get; }

        public IObservable<string> VoidText { get; }

        public IObservable<string> HoveredFeatureText { get; }

        public void ToggleHeightMapMenuItemClick()
        {
            this.dispatcher.ToggleHeightmap();
        }

        public void ToggleHeightGridMenuItemClick()
        {
            this.dispatcher.ToggleHeightGrid();
        }

        public void ToggleVoidsMenuItemClick()
        {
            this.dispatcher.ToggleVoids();
        }

        public void ToggleMinimapMenuItemClick()
        {
            this.dispatcher.ToggleMinimap();
        }

        public void ToggleFeaturesMenuItemClick()
        {
            this.dispatcher.ToggleFeatures();
        }

        public void PreferencesMenuItemClick()
        {
            this.dispatcher.OpenPreferences();
        }

        public void AboutMenuItemClick()
        {
            this.dispatcher.ShowAbout();
        }

        public void MapAttributesMenuItemClick()
        {
            this.dispatcher.OpenMapAttributes();
        }

        public void GridColorMenuItemClick()
        {
            this.dispatcher.ChooseColor();
        }

        public void NewMenuItemClick()
        {
            this.dispatcher.New();
        }

        public void OpenMenuItemClick()
        {
            this.dispatcher.Open();
        }

        public void DragDropFile(string filename)
        {
            this.dispatcher.OpenFromDragDrop(filename);
        }

        public void SaveMenuItemClick()
        {
            this.dispatcher.Save();
        }

        public void SaveAsMenuItemClick()
        {
            this.dispatcher.SaveAs();
        }

        public void CloseMenuItemClick()
        {
            this.dispatcher.CloseMap();
        }

        public void UndoMenuItemClick()
        {
            this.dispatcher.Undo();
        }

        public void RedoMenuItemClick()
        {
            this.dispatcher.Redo();
        }

        public void FormCloseButtonClick()
        {
            this.dispatcher.Close();
        }

        public void ExitMenuItemClick()
        {
            this.dispatcher.Close();
        }

        public void GenerateMinimapMenuItemClick()
        {
            this.dispatcher.RefreshMinimap();
        }

        public void GenerateMinimapHighQualityMenuItemClick()
        {
            this.dispatcher.RefreshMinimapHighQualityWithProgress();
        }

        public void GridOffMenuItemClick()
        {
            this.dispatcher.HideGrid();
        }

        public void GridMenuItemClick(Size s)
        {
            this.dispatcher.EnableGridWithSize(s);
        }

        public void SeaLevelTrackBarValueChanged(int value)
        {
            this.dispatcher.SetSeaLevel(value);
        }

        public void SeaLevelTrackBarMouseUp()
        {
            this.dispatcher.FlushSeaLevel();
        }

        public void HeightEditIntervalChanged(int value)
        {
            this.dispatcher.SetHeightEditInterval(value);
        }

        public void HeightEditModeChanged(HeightEditMode mode)
        {
            this.dispatcher.SetHeightEditMode(mode);
        }

        public void HeightEditSetValueChanged(int value)
        {
            this.dispatcher.SetHeightEditSetValue(value);
        }

        public void HeightEditCursorSizeChanged(int value)
        {
            this.dispatcher.SetHeightEditCursorSize(value);
        }

        public void VoidEditCursorSizeChanged(int value)
        {
            this.dispatcher.SetVoidEditCursorSize(value);
        }

        public void CopyMenuItemClick()
        {
            this.dispatcher.CopySelectionToClipboard();
        }

        public void CutMenuItemClick()
        {
            this.dispatcher.CutSelectionToClipboard();
        }

        public void PasteMenuItemClick()
        {
            this.dispatcher.PasteFromClipboard();
        }

        public void FillMenuItemClick()
        {
            this.dispatcher.FillMap();
        }

        public void ResizeMapMenuItemClick()
        {
            this.dispatcher.ResizeMap();
        }

        public void FlipHorizontallyMenuItemClick()
        {
            this.dispatcher.FlipHorizontally();
        }

        public void FlipVerticallyMenuItemClick()
        {
            this.dispatcher.FlipVertically();
        }

        public void ImportMinimapMenuItemClick()
        {
            this.dispatcher.ImportMinimap();
        }

        public void ExportMinimapMenuItemClick()
        {
            this.dispatcher.ExportMinimap();
        }

        public void ImportHeightmapMenuItemClick()
        {
            this.dispatcher.ImportHeightmap();
        }

        public void ExportHeightmapMenuItemClick()
        {
            this.dispatcher.ExportHeightmap();
        }

        public void ExportMapImageMenuItemClick()
        {
            this.dispatcher.ExportMapImage();
        }

        public void ImportCustomSectionMenuItemClick()
        {
            this.dispatcher.ImportCustomSection();
        }

        public void Load()
        {
            this.dispatcher.Initialize();
        }

        public void ChangeSelectedTabType(GUITab tabType)
        {
            this.dispatcher.ChangeSelectedTab(tabType);
        }

        public void CenterViewOnStartPosition(int index)
        {
            this.dispatcher.CenterViewOnStartPosition(index);
        }

        private static string GetHeightText(IReadOnlyMapModel map, Point mousePosition)
        {
            var gridPos = Util.ScreenToHeightIndex(map.Tile.HeightGrid, mousePosition);
            if (!gridPos.HasValue)
            {
                return "H: -";
            }

            var heightIndex = (gridPos.Value.Y * map.Tile.HeightGrid.Width) + gridPos.Value.X;
            var h = map.Tile.HeightGrid[heightIndex];
            return $"H: {h}";
        }

        private static string GetVoidText(IReadOnlyMapModel map, Point mousePosition)
        {
            var gridWidth = map.Voids.Width;
            var gridHeight = map.Voids.Height;
            if (gridWidth < 2 || gridHeight < 2)
            {
                return "Void: -";
            }

            var approxX = Clamp(mousePosition.X / 16, 0, gridWidth - 2);
            var approxY = Clamp(mousePosition.Y / 16, 0, gridHeight - 2);

            var startX = Math.Max(0, approxX - 1);
            var endX = Math.Min(gridWidth - 2, approxX + 1);

            var startY = Math.Max(0, approxY - 8);
            var endY = Math.Min(gridHeight - 2, approxY + 8);

            for (var y = startY; y <= endY; y++)
            {
                for (var x = startX; x <= endX; x++)
                {
                    if (!GetVoidAt(map.Voids, x, y))
                    {
                        continue;
                    }

                    if (IsPointInProjectedCell(mousePosition, map.Tile.HeightGrid, x, y))
                    {
                        return "Void: yes";
                    }
                }
            }

            return "Void: no";
        }

        private static bool IsPointInProjectedCell(Point point, IGrid<int> heights, int x, int y)
        {
            var p00 = GetProjectedPoint(heights, x, y);
            var p10 = GetProjectedPoint(heights, x + 1, y);
            var p11 = GetProjectedPoint(heights, x + 1, y + 1);
            var p01 = GetProjectedPoint(heights, x, y + 1);

            return IsPointInTriangle(point, p00, p10, p11) || IsPointInTriangle(point, p00, p11, p01);
        }

        private static Point GetProjectedPoint(IGrid<int> heights, int x, int y)
        {
            var heightIndex = (y * heights.Width) + x;
            var extraY = heights[heightIndex] / 2;
            return new Point(x * 16, (y * 16) - extraY);
        }

        private static bool GetVoidAt(ISparseGrid<bool> voids, int x, int y)
        {
            var voidIndex = (y * voids.Width) + x;
            return voids[voidIndex];
        }

        private static bool IsPointInTriangle(Point p, Point a, Point b, Point c)
        {
            var b1 = Sign(p, a, b) < 0;
            var b2 = Sign(p, b, c) < 0;
            var b3 = Sign(p, c, a) < 0;
            return b1 == b2 && b2 == b3;
        }

        private static int Sign(Point p1, Point p2, Point p3)
        {
            return ((p1.X - p3.X) * (p2.Y - p3.Y)) - ((p2.X - p3.X) * (p1.Y - p3.Y));
        }

        private static int Clamp(int value, int min, int max)
        {
            return Math.Max(min, Math.Min(max, value));
        }

        private static string FormatMetalSpotStatusValue(Feature feature)
        {
            var raw = feature.MetalSpotValue;
            if (!MappySettings.Settings.ShowCalculatedMetalDepositValue)
            {
                return raw.ToString(CultureInfo.CurrentCulture);
            }

            var footprintTiles = feature.Footprint.Width * feature.Footprint.Height;
            var value = raw * 0.001 * footprintTiles;
            return Math.Round(value, 1, MidpointRounding.AwayFromZero).ToString("0.0", CultureInfo.CurrentCulture);
        }
    }
}
