namespace Mappy.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Drawing;
    using System.Reactive.Linq;
    using System.Reactive.Subjects;
    using System.Windows.Forms;

    using Mappy.Collections;
    using Mappy.Data;
    using Mappy.Models.Enums;
    using Mappy.Services;
    using Mappy.UI.Controls;
    using Mappy.UI.Drawables;
    using Mappy.UI.Tags;
    using Mappy.Util;

    public class MapViewViewModel : IMapViewViewModel
    {
        private const int BandboxDepth = 100000000;

        private static readonly Color BandboxFillColor = Color.FromArgb(127, Color.Blue);

        private static readonly Color BandboxBorderColor = Color.FromArgb(127, Color.Black);

        private static readonly Color HeightCursorFillColor = Color.FromArgb(0, Color.Red);

        private static readonly Color HeightCursorBorderColor = Color.Red;

        private const int HeightCursorDepth = BandboxDepth + 1;

        private static readonly IDrawable[] StartPositionImages = LoadStartPositionImages();

        private static readonly Feature DefaultFeatureRecord = new Feature
        {
            Name = "default",
            Offset = new Point(0, 0),
            Footprint = new Size(1, 1),
            Image = Mappy.Properties.Resources.nofeature
        };

        private readonly List<DrawableItem> tileMapping = new List<DrawableItem>();

        private readonly IDictionary<Guid, DrawableItem> featureMapping =
            new Dictionary<Guid, DrawableItem>();

        private readonly DrawableItem[] startPositionMapping = new DrawableItem[10];

        private readonly GridLayer grid = new GridLayer(16, Color.Black);

        private readonly GuideLayer guides = new GuideLayer();

        private readonly BehaviorSubject<SelectableItemsLayer> itemsLayer =
            new BehaviorSubject<SelectableItemsLayer>(new SelectableItemsLayer(0, 0));

        private readonly BehaviorSubject<AbstractLayer> voidLayer = new BehaviorSubject<AbstractLayer>(new DummyLayer());

        private readonly IReadOnlyApplicationModel model;

        private readonly Dispatcher dispatcher;

        private readonly FeatureService featureService;

        private IMainModel mapModel;

        private bool mouseDown;

        private Point lastMousePos;

        private Point lastHoverPos;

        private bool bandboxMode;

        private DrawableItem bandboxMapping;

        private DrawableItem heightCursorMapping;

        private DrawableItem voidCursorMapping;

        private DrawableTile baseTile;

        private DrawableItem baseItem;

        private bool featuresVisible;

        private string mapFilePath;

        private readonly BehaviorSubject<bool> heightEditModeObservable = new BehaviorSubject<bool>(false);

        private readonly BehaviorSubject<bool> voidEditModeObservable = new BehaviorSubject<bool>(false);

        private bool heightEditMode;

        private bool voidEditMode;

        private int heightEditInterval = 4;

        private int heightEditCursorSize = 1;

        private int voidEditCursorSize = 1;

        private int activeHeightBrushDelta;

        private bool? activeVoidBrushValue;

        private Point? lastHeightBrushPoint;

        private Point? lastVoidBrushPoint;

        private Point? pinnedSingleHeightPoint;

        private Point pinnedSingleHeightPointAnchor;

        private Point? pinnedAreaHeightPoint;

        private Point pinnedAreaHeightPointAnchor;

        public MapViewViewModel(IReadOnlyApplicationModel model, Dispatcher dispatcher, FeatureService featureService)
        {
            var heightmapVisible = model.PropertyAsObservable(x => x.HeightmapVisible, nameof(model.HeightmapVisible));
            var heightGridVisible = model.PropertyAsObservable(x => x.HeightGridVisible, nameof(model.HeightGridVisible));
            var voidsVisible = model.PropertyAsObservable(x => x.VoidsVisible, nameof(model.VoidsVisible));
            var gridVisible = model.PropertyAsObservable(x => x.GridVisible, nameof(model.GridVisible));
            var gridColor = model.PropertyAsObservable(x => x.GridColor, nameof(model.GridColor));
            var gridSize = model.PropertyAsObservable(x => x.GridSize, nameof(model.GridSize));
            var featuresVisible = model.PropertyAsObservable(x => x.FeaturesVisible, nameof(model.FeaturesVisible));
            var selectedTab = model.PropertyAsObservable(x => x.SelectedGUITab, nameof(model.SelectedGUITab));
            var heightEditInterval = model.PropertyAsObservable(x => x.HeightEditInterval, nameof(model.HeightEditInterval));
            var heightEditCursorSize = model.PropertyAsObservable(x => x.HeightEditCursorSize, nameof(model.HeightEditCursorSize));
            var voidEditCursorSize = model.PropertyAsObservable(x => x.VoidEditCursorSize, nameof(model.VoidEditCursorSize));
            var map = model.PropertyAsObservable(x => x.Map, nameof(model.Map));
            var mapFilePath = map.ObservePropertyOrDefault(x => x.FilePath, "FilePath", null);

            var mapWidth = map.ObservePropertyOrDefault(x => x.MapWidth, "MapWidth", 0);
            var mapHeight = map.ObservePropertyOrDefault(x => x.MapHeight, "MapHeight", 0);

            this.ViewportLocation = map.ObservePropertyOrDefault(
                x => x.ViewportLocation,
                "ViewportLocation",
                Point.Empty);

            map.Subscribe(this.SetMapModel);
            gridVisible.Subscribe(x => this.grid.Enabled = x);
            gridColor.Subscribe(x => this.grid.Color = x);

            // FIXME: this should not ignore height
            gridSize.Subscribe(x => this.grid.CellSize = x.Width);
            heightmapVisible.Subscribe(this.RefreshHeightmapVisibility);
            heightGridVisible.Subscribe(this.RefreshHeightGridVisibility);
            voidsVisible.Subscribe(x => this.voidLayer.Value.Enabled = x);
            featuresVisible.Subscribe(x => this.featuresVisible = x);
            selectedTab.Subscribe(this.OnSelectedTabChanged);
            heightEditInterval.Subscribe(x => this.heightEditInterval = Math.Max(1, x));
            heightEditCursorSize.Subscribe(this.OnHeightEditCursorSizeChanged);
            voidEditCursorSize.Subscribe(this.OnVoidEditCursorSizeChanged);
            mapFilePath.Subscribe(this.OnMapFilePathChanged);

            this.CanvasSize = mapWidth.CombineLatest(mapHeight, (w, h) => new Size(w * 32, h * 32));
            this.CanvasSize.Subscribe(
                x =>
                    {
                        this.guides.ClearGuides();
                        this.guides.AddHorizontalGuide(x.Height - 128);
                        this.guides.AddVerticalGuide(x.Width - 32);
                    });
            mapWidth.CombineLatest(mapHeight, (w, h) => new Size(w, h))
                .Skip(1)
                .Subscribe(_ => this.ResetView());

            map.ObservePropertyOrDefault(x => x.SelectedTile, "SelectedTile", null)
                .Subscribe(_ => this.RefreshSelection());
            map.ObservePropertyOrDefault(x => x.SelectedStartPosition, "SelectedStartPosition", null)
                .Subscribe(_ => this.RefreshSelection());

            map
                .Where(x => x.IsSome)
                .Select(x => x.UnsafeValue) // will never be null due to where clause
                .Select(x => x.SelectedFeatures)
                .Subscribe(x => x.CollectionChanged += this.SelectedFeaturesCollectionChanged);

            map.Select(
                x => x.Match<AbstractLayer>(
                    y => new VoidLayer(y) { Enabled = model.VoidsVisible },
                    () => new DummyLayer()))
                .Subscribe(this.voidLayer);

            this.model = model;
            this.dispatcher = dispatcher;
            this.featureService = featureService;
        }

        public IObservable<Size> CanvasSize { get; }

        public IObservable<Point> ViewportLocation { get; }

        public IObservable<bool> HeightEditMode => this.heightEditModeObservable;

        public IObservable<bool> VoidEditMode => this.voidEditModeObservable;

        public IObservable<ILayer> ItemsLayer => this.itemsLayer;

        public IObservable<ILayer> VoidLayer => this.voidLayer;

        public ILayer GridLayer => this.grid;

        public ILayer GuidesLayer => this.guides;

        public void DragDrop(IDataObject data, Point location)
        {
            if (data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])data.GetData(DataFormats.FileDrop);
                if (files.Length < 1)
                {
                    return;
                }

                this.dispatcher.OpenFromDragDrop(files[0]);
            }
            else if (data.GetDataPresent(typeof(StartPositionDragData)))
            {
                var posData = (StartPositionDragData)data.GetData(typeof(StartPositionDragData));
                this.dispatcher.SetStartPosition(posData.PositionNumber, location.X, location.Y);
            }
            else if (data.GetDataPresent(DataFormats.Text))
            {
                var dataString = (string)data.GetData(DataFormats.Text);
                int id;
                if (int.TryParse(dataString, out id))
                {
                    this.dispatcher.DragDropSection(id, location.X, location.Y);
                }
                else
                {
                    this.dispatcher.DragDropFeature(dataString, location.X, location.Y);
                }
            }
        }

        public void ClientSizeChanged(Size size)
        {
            this.dispatcher.SetViewportSize(size);
        }

        public void ScrollPositionChanged(Point position)
        {
            this.dispatcher.SetViewportLocation(position);
        }

        public void MouseLeftDown(Point location)
        {
            this.mouseDown = true;
            this.lastMousePos = location;

            if (this.heightEditMode)
            {
                this.activeHeightBrushDelta = this.heightEditInterval;
                this.lastHeightBrushPoint = null;
                this.ApplyHeightBrushAt(location);
                return;
            }

            if (this.voidEditMode)
            {
                this.activeVoidBrushValue = true;
                this.lastVoidBrushPoint = null;
                this.ApplyVoidBrushAt(location);
                return;
            }

            if (!this.itemsLayer.Value.IsInSelection(location.X, location.Y))
            {
                var hit = this.itemsLayer.Value.HitTest(location.X, location.Y);
                if (hit != null && hit.Tag is IMapItemTag)
                {
                    this.SelectFromTag(hit.Tag);
                }
                else
                {
                    this.dispatcher.ClearSelection();
                    this.dispatcher.StartBandbox(location.X, location.Y);
                    this.bandboxMode = true;
                }
            }
        }

        public void MouseRightDown(Point location)
        {
            this.mouseDown = true;
            this.lastMousePos = location;

            if (this.heightEditMode)
            {
                this.activeHeightBrushDelta = -this.heightEditInterval;
                this.lastHeightBrushPoint = null;
                this.ApplyHeightBrushAt(location);
                return;
            }

            if (this.voidEditMode)
            {
                this.activeVoidBrushValue = false;
                this.lastVoidBrushPoint = null;
                this.ApplyVoidBrushAt(location);
                return;
            }

            if (!this.itemsLayer.Value.IsInSelection(location.X, location.Y) &&
                this.featureService.SelectedFeature != null)
            {
                this.dispatcher.DragDropFeature(
                    this.featureService.SelectedFeature.Name,
                    location.X,
                    location.Y
                );
            }
        }

        public void MouseMove(Point location)
        {
            this.dispatcher.UpdateMousePosition(Maybe.Return(location));
            this.lastHoverPos = location;
            this.UpdateHeightCursor(location);
            this.UpdateVoidCursor(location);

            var hit = this.itemsLayer.Value.HitTest(location.X, location.Y);
            this.dispatcher.SetHoveredFeature(hit != null && hit.Tag is FeatureTag featureTag ? Maybe.Return(featureTag.FeatureId) : Maybe.None<Guid>());

            try
            {
                if (!this.mouseDown)
                {
                    return;
                }

                if (this.heightEditMode && this.activeHeightBrushDelta != 0)
                {
                    this.ApplyHeightBrushAt(location);
                    return;
                }

                if (this.voidEditMode && this.activeVoidBrushValue.HasValue)
                {
                    this.ApplyVoidBrushAt(location);
                    return;
                }

                if (this.bandboxMode)
                {
                    this.dispatcher.GrowBandbox(
                        location.X - this.lastMousePos.X,
                        location.Y - this.lastMousePos.Y);
                }
                else
                {
                    this.dispatcher.TranslateSelection(
                        location.X - this.lastMousePos.X,
                        location.Y - this.lastMousePos.Y);
                }
            }
            finally
            {
                this.lastMousePos = location;
            }
        }

        public void MouseUp()
        {
            this.mouseDown = false;

            if (this.heightEditMode && this.activeHeightBrushDelta != 0)
            {
                this.activeHeightBrushDelta = 0;
                this.lastHeightBrushPoint = null;
                this.dispatcher.FlushHeightBrush();
                return;
            }

            if (this.voidEditMode && this.activeVoidBrushValue.HasValue)
            {
                this.activeVoidBrushValue = null;
                this.lastVoidBrushPoint = null;
                this.dispatcher.FlushVoidBrush();
                return;
            }

            if (this.bandboxMode)
            {
                this.dispatcher.CommitBandbox();
                this.bandboxMode = false;
            }
            else
            {
                this.dispatcher.FlushTranslation();
            }
        }

        public void KeyDown(Keys key)
        {
            if (key == Keys.Delete)
            {
                this.dispatcher.DeleteSelection();
            }
        }

        public void LeaveFocus()
        {
            if (this.activeHeightBrushDelta != 0)
            {
                this.activeHeightBrushDelta = 0;
                this.lastHeightBrushPoint = null;
                this.dispatcher.FlushHeightBrush();
            }

            if (this.activeVoidBrushValue.HasValue)
            {
                this.activeVoidBrushValue = null;
                this.lastVoidBrushPoint = null;
                this.dispatcher.FlushVoidBrush();
            }

            this.ClearHeightCursor();
            this.ClearVoidCursor();
            this.dispatcher.ClearSelection();
        }

        private static IDrawable[] LoadStartPositionImages()
        {
            var arr = new IDrawable[10];
            for (var i = 0; i < 10; i++)
            {
                var image = new DrawableBitmap(Mappy.Util.Util.GetStartImage(i + 1));
                arr[i] = image;
            }

            return arr;
        }

        private void SetMapModel(Maybe<UndoableMapModel> model)
        {
            this.mapModel = model.Or(null);
            this.WireMapModel();
            this.ResetView();
        }

        private void ResetView()
        {
            this.UpdateItemsLayer();

            this.UpdateBaseTile();

            this.UpdateFloatingTiles();

            this.UpdateFeatures();

            this.UpdateStartPositions();
        }

        private void UpdateItemsLayer()
        {
            this.heightCursorMapping = null;
            this.voidCursorMapping = null;
            if (this.mapModel == null)
            {
                this.itemsLayer.OnNext(new SelectableItemsLayer(0, 0));
            }
            else
            {
                this.itemsLayer.OnNext(
                    new SelectableItemsLayer(
                        this.mapModel.MapWidth * 32,
                        this.mapModel.MapHeight * 32));
            }
        }

        private void UpdateStartPositions()
        {
            for (var i = 0; i < 10; i++)
            {
                this.UpdateStartPosition(i);
            }
        }

        private void UpdateFeatures()
        {
            foreach (var f in this.featureMapping.Values)
            {
                this.itemsLayer.Value.Items.Remove(f);
            }

            this.featureMapping.Clear();

            if (this.mapModel == null)
            {
                return;
            }

            foreach (var f in this.mapModel.EnumerateFeatureInstances())
            {
                this.InsertFeature(f.Id);
            }
        }

        private void UpdateBaseTile()
        {
            if (this.baseItem != null)
            {
                this.itemsLayer.Value.Items.Remove(this.baseItem);
            }

            if (this.mapModel == null)
            {
                this.baseTile = null;
                this.baseItem = null;
                return;
            }

            this.baseTile = new DrawableTile(this.mapModel.BaseTile);
            this.baseTile.BackgroundColor = Color.CornflowerBlue;
            this.baseTile.DrawHeightMap = this.model.HeightmapVisible;
            this.baseTile.DrawHeightGrid = this.model.HeightGridVisible;
            this.baseTile.SeaLevel = this.mapModel.SeaLevel;
            this.baseTile.MapFilePath = this.mapFilePath;
            this.baseItem = new DrawableItem(
                0,
                0,
                -1,
                this.baseTile);

            this.baseItem.Locked = true;

            this.itemsLayer.Value.Items.Add(this.baseItem);
        }

        private void RefreshHeightmapVisibility(bool visible)
        {
            if (this.baseTile == null)
            {
                return;
            }

            this.baseTile.DrawHeightMap = visible;
        }

        private void RefreshHeightGridVisibility(bool visible)
        {
            if (this.baseTile == null)
            {
                return;
            }

            this.baseTile.DrawHeightGrid = visible;
        }

        private void RefreshSeaLevel()
        {
            this.baseTile.SeaLevel = this.mapModel.SeaLevel;
        }

        private void OnMapFilePathChanged(string path)
        {
            this.mapFilePath = path;
            if (this.baseTile != null)
            {
                this.baseTile.MapFilePath = path;
                this.baseTile.Invalidate();
            }
        }

        private void WireMapModel()
        {
            if (this.mapModel == null)
            {
                return;
            }

            this.mapModel.TilesChanged += this.TilesChanged;
            this.mapModel.BaseTileGraphicsChanged += this.BaseTileChanged;
            this.mapModel.BaseTileHeightChanged += this.BaseTileChanged;

            foreach (var t in this.mapModel.FloatingTiles)
            {
                t.LocationChanged += this.TileLocationChanged;
            }

            this.mapModel.FeatureInstanceChanged += this.FeatureInstanceChanged;

            this.mapModel.StartPositionChanged += this.StartPositionChanged;

            this.mapModel.PropertyChanged += this.MapModelPropertyChanged;
        }

        private void SelectedFeaturesCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            // Amend the feature selection list rather than rebuild it on every selection
            switch (e.Action)
            {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    if (e.NewItems != null)
                    {
                        foreach (var idObj in e.NewItems)
                        {
                            this.TryAddFeatureSelection((Guid)idObj);
                        }
                    }

                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                    if (e.OldItems != null)
                    {
                        foreach (var idObj in e.OldItems)
                        {
                            this.TryRemoveFeatureSelection((Guid)idObj);
                        }
                    }

                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:
                    if (e.OldItems != null)
                    {
                        foreach (var idObj in e.OldItems)
                        {
                            this.TryRemoveFeatureSelection((Guid)idObj);
                        }
                    }

                    if (e.NewItems != null)
                    {
                        foreach (var idObj in e.NewItems)
                        {
                            this.TryAddFeatureSelection((Guid)idObj);
                        }
                    }

                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Move:
                    // same item set, nothing to do
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                default:
                    this.RefreshSelection();
                    break;
            }
        }

        private void TryAddFeatureSelection(Guid id)
        {
            if (this.featureMapping.ContainsKey(id))
            {
                this.itemsLayer.Value.AddToSelection(this.featureMapping[id]);
            }
        }

        private void TryRemoveFeatureSelection(Guid id)
        {
            if (this.featureMapping.ContainsKey(id))
            {
                this.itemsLayer.Value.RemoveFromSelection(this.featureMapping[id]);
            }
        }

        private void MapModelPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            switch (propertyChangedEventArgs.PropertyName)
            {
                case "SeaLevel":
                    this.RefreshSeaLevel();
                    break;
                case "BandboxRectangle":
                    this.UpdateBandbox();
                    break;
            }
        }

        private void UpdateBandbox()
        {
            if (this.bandboxMapping != null)
            {
                this.itemsLayer.Value.Items.Remove(this.bandboxMapping);
            }

            if (this.mapModel == null)
            {
                return;
            }

            if (this.mapModel.BandboxRectangle == Rectangle.Empty)
            {
                return;
            }

            var bandbox = DrawableBandbox.CreateSimple(
                this.mapModel.BandboxRectangle.Size,
                BandboxFillColor,
                BandboxBorderColor);

            this.bandboxMapping = new DrawableItem(
                this.mapModel.BandboxRectangle.X,
                this.mapModel.BandboxRectangle.Y,
                BandboxDepth,
                bandbox);

            this.bandboxMapping.Locked = true;

            this.itemsLayer.Value.Items.Add(this.bandboxMapping);
        }

        private void OnSelectedTabChanged(GUITab tab)
        {
            var heightEnabled = tab == GUITab.Height;
            var voidEnabled = tab == GUITab.Void;
            if (this.heightEditMode == heightEnabled && this.voidEditMode == voidEnabled)
            {
                return;
            }

            this.heightEditMode = heightEnabled;
            this.voidEditMode = voidEnabled;
            this.heightEditModeObservable.OnNext(heightEnabled);
            this.voidEditModeObservable.OnNext(voidEnabled);

            this.mouseDown = false;
            this.bandboxMode = false;

            this.activeHeightBrushDelta = 0;
            this.lastHeightBrushPoint = null;
            this.pinnedSingleHeightPoint = null;
            this.pinnedAreaHeightPoint = null;
            this.ClearHeightCursor();
            this.dispatcher.FlushHeightBrush();

            this.activeVoidBrushValue = null;
            this.lastVoidBrushPoint = null;
            this.ClearVoidCursor();
            this.dispatcher.FlushVoidBrush();
        }

        private void OnHeightEditCursorSizeChanged(int size)
        {
            this.heightEditCursorSize = Math.Max(1, size);
            this.UpdateHeightCursor(this.lastHoverPos);
        }

        private void OnVoidEditCursorSizeChanged(int size)
        {
            this.voidEditCursorSize = Math.Max(1, size);
            this.UpdateVoidCursor(this.lastHoverPos);
        }

        private void ApplyHeightBrushAt(Point location)
        {
            if (this.activeHeightBrushDelta == 0 || this.mapModel == null)
            {
                return;
            }

            Point? anchor;
            if (this.heightEditCursorSize == 1)
            {
                anchor = this.ResolveSingleHeightAnchor(this.mapModel.BaseTile.HeightGrid, location);
            }
            else
            {
                this.pinnedSingleHeightPoint = null;
                anchor = this.ResolveAreaHeightAnchor(this.mapModel.BaseTile.HeightGrid, location);
            }

            if (!anchor.HasValue)
            {
                return;
            }

            if (this.heightEditCursorSize > 1 &&
                this.lastHeightBrushPoint.HasValue &&
                this.lastHeightBrushPoint.Value == anchor.Value)
            {
                return;
            }

            this.lastHeightBrushPoint = anchor;
            if (this.heightEditCursorSize == 1)
            {
                this.pinnedSingleHeightPoint = anchor;
                this.dispatcher.AdjustHeightPoint(anchor.Value.X, anchor.Value.Y, this.activeHeightBrushDelta);
            }
            else
            {
                this.pinnedAreaHeightPoint = anchor;
                this.pinnedAreaHeightPointAnchor = location;
                this.dispatcher.AdjustHeightBrushAtAnchor(
                    anchor.Value.X,
                    anchor.Value.Y,
                    this.activeHeightBrushDelta,
                    this.heightEditCursorSize);
            }

            // Ensure contour and height-grid overlays redraw immediately after edits.
            this.baseTile?.Invalidate();

            // Keep the indicator locked to the edited point as projection changes.
            this.UpdateHeightCursor(location);

            // Force status-bar observables to recompute height at current cursor location.
            this.dispatcher.UpdateMousePosition(Maybe.None<Point>());
            this.dispatcher.UpdateMousePosition(Maybe.Return(location));
        }

        private void UpdateHeightCursor(Point location)
        {
            if (!this.heightEditMode || this.mapModel == null)
            {
                this.ClearHeightCursor();
                return;
            }

            var center = Util.ScreenToHeightIndex(this.mapModel.BaseTile.HeightGrid, location);
            if (!center.HasValue)
            {
                return;
            }

            var heightGrid = this.mapModel.BaseTile.HeightGrid;
            var width = heightGrid.Width;
            var height = heightGrid.Height;

            var size = Math.Max(1, this.heightEditCursorSize);
            var endX = center.Value.X + 1;
            var endY = center.Value.Y + 1;
            var startX = Math.Max(0, endX - size);
            var startY = Math.Max(0, endY - size);
            endX = Math.Min(width, endX);
            endY = Math.Min(height, endY);

            if (size == 1)
            {
                var cornerIndex = this.ResolveSingleHeightAnchor(heightGrid, location);
                if (!cornerIndex.HasValue)
                {
                    return;
                }

                var corner = ProjectHeightPoint(heightGrid, cornerIndex.Value.X, cornerIndex.Value.Y);
                var marker = new Rectangle(corner.X - 2, corner.Y - 2, 4, 4);
                var cursor = DrawableBandbox.CreateSimple(
                    marker.Size,
                    HeightCursorFillColor,
                    HeightCursorBorderColor);
                var item = new DrawableItem(
                    marker.X,
                    marker.Y,
                    HeightCursorDepth,
                    cursor);
                item.Locked = true;
                this.ReplaceHeightCursor(item);
                return;
            }

            var points = BuildProjectedBoundaryPoints(heightGrid, startX, startY, endX, endY);
            if (points.Length < 3)
            {
                return;
            }

            var minX = points[0].X;
            var minY = points[0].Y;
            var maxX = points[0].X;
            var maxY = points[0].Y;
            for (var i = 1; i < points.Length; i++)
            {
                minX = Math.Min(minX, points[i].X);
                minY = Math.Min(minY, points[i].Y);
                maxX = Math.Max(maxX, points[i].X);
                maxY = Math.Max(maxY, points[i].Y);
            }

            var localPoints = new Point[points.Length];
            for (var i = 0; i < points.Length; i++)
            {
                localPoints[i] = new Point(points[i].X - minX, points[i].Y - minY);
            }

            var polySize = new Size(Math.Max(1, (maxX - minX) + 1), Math.Max(1, (maxY - minY) + 1));
            var poly = new DrawablePolyline(localPoints, polySize, new Pen(HeightCursorBorderColor, 1));

            var polyItem = new DrawableItem(
                minX,
                minY,
                HeightCursorDepth,
                poly);
            polyItem.Locked = true;
            this.ReplaceHeightCursor(polyItem);
        }

        private void ApplyVoidBrushAt(Point location)
        {
            if (!this.activeVoidBrushValue.HasValue || this.mapModel == null)
            {
                return;
            }

            var anchor = Util.ScreenToHeightIndex(this.mapModel.BaseTile.HeightGrid, location);
            if (!anchor.HasValue)
            {
                return;
            }

            if (this.lastVoidBrushPoint.HasValue && this.lastVoidBrushPoint.Value == anchor.Value)
            {
                return;
            }

            this.lastVoidBrushPoint = anchor.Value;
            this.dispatcher.SetVoidBrushAtAnchor(
                anchor.Value.X,
                anchor.Value.Y,
                this.voidEditCursorSize,
                this.activeVoidBrushValue.Value);

            // Ensure void overlay repaints immediately.
            this.baseTile?.Invalidate();
            this.UpdateVoidCursor(location);

            // Force status-bar observables to recompute at the current cursor location.
            this.dispatcher.UpdateMousePosition(Maybe.None<Point>());
            this.dispatcher.UpdateMousePosition(Maybe.Return(location));
        }

        private void UpdateVoidCursor(Point location)
        {
            if (!this.voidEditMode || this.mapModel == null)
            {
                this.ClearVoidCursor();
                return;
            }

            var anchor = Util.ScreenToHeightIndex(this.mapModel.BaseTile.HeightGrid, location);
            if (!anchor.HasValue)
            {
                this.ClearVoidCursor();
                return;
            }

            var grid = this.mapModel.BaseTile.HeightGrid;
            var size = Math.Max(1, this.voidEditCursorSize);
            var startX = anchor.Value.X;
            var startY = anchor.Value.Y;
            var endX = Math.Min(grid.Width, startX + size);
            var endY = Math.Min(grid.Height, startY + size);
            if (endX <= startX || endY <= startY)
            {
                return;
            }

            var points = BuildProjectedBoundaryPoints(grid, startX, startY, endX, endY);
            if (points.Length < 3)
            {
                return;
            }

            var minX = points[0].X;
            var minY = points[0].Y;
            var maxX = points[0].X;
            var maxY = points[0].Y;
            for (var i = 1; i < points.Length; i++)
            {
                minX = Math.Min(minX, points[i].X);
                minY = Math.Min(minY, points[i].Y);
                maxX = Math.Max(maxX, points[i].X);
                maxY = Math.Max(maxY, points[i].Y);
            }

            var localPoints = new Point[points.Length];
            for (var i = 0; i < points.Length; i++)
            {
                localPoints[i] = new Point(points[i].X - minX, points[i].Y - minY);
            }

            var polySize = new Size(Math.Max(1, (maxX - minX) + 1), Math.Max(1, (maxY - minY) + 1));
            var poly = new DrawablePolyline(localPoints, polySize, new Pen(HeightCursorBorderColor, 1));
            var polyItem = new DrawableItem(minX, minY, HeightCursorDepth, poly)
            {
                Locked = true
            };
            this.ReplaceVoidCursor(polyItem);
        }

        private void ReplaceHeightCursor(DrawableItem item)
        {
            this.ClearHeightCursor();
            this.heightCursorMapping = item;
            this.itemsLayer.Value.Items.Add(this.heightCursorMapping);
        }

        private void ClearHeightCursor()
        {
            if (this.heightCursorMapping != null)
            {
                this.itemsLayer.Value.Items.Remove(this.heightCursorMapping);
                this.heightCursorMapping = null;
            }
        }

        private void ReplaceVoidCursor(DrawableItem item)
        {
            this.ClearVoidCursor();
            this.voidCursorMapping = item;
            this.itemsLayer.Value.Items.Add(this.voidCursorMapping);
        }

        private void ClearVoidCursor()
        {
            if (this.voidCursorMapping != null)
            {
                this.itemsLayer.Value.Items.Remove(this.voidCursorMapping);
                this.voidCursorMapping = null;
            }
        }

        private static Point ProjectHeightPoint(IGrid<int> heightGrid, int x, int y)
        {
            var sampleX = Util.Clamp(x, 0, heightGrid.Width - 1);
            var sampleY = Util.Clamp(y, 0, heightGrid.Height - 1);
            var h = heightGrid.Get(sampleX, sampleY);
            return new Point(x * 16, (y * 16) - (h / 2));
        }

        private static Point[] BuildProjectedBoundaryPoints(IGrid<int> heightGrid, int startX, int startY, int endX, int endY)
        {
            var points = new List<Point>();
            for (var x = startX; x <= endX; x++)
            {
                points.Add(ProjectHeightPoint(heightGrid, x, startY));
            }

            for (var y = startY + 1; y <= endY; y++)
            {
                points.Add(ProjectHeightPoint(heightGrid, endX, y));
            }

            for (var x = endX - 1; x >= startX; x--)
            {
                points.Add(ProjectHeightPoint(heightGrid, x, endY));
            }

            for (var y = endY - 1; y > startY; y--)
            {
                points.Add(ProjectHeightPoint(heightGrid, startX, y));
            }

            return points.ToArray();
        }

        private Point? ResolveSingleHeightAnchor(IGrid<int> heightGrid, Point location)
        {
            if (this.pinnedSingleHeightPoint.HasValue && IsNear(location, this.pinnedSingleHeightPointAnchor, 12))
            {
                var p = this.pinnedSingleHeightPoint.Value;
                if (p.X >= 0 && p.Y >= 0 && p.X < heightGrid.Width && p.Y < heightGrid.Height)
                {
                    return p;
                }
            }

            var next = Util.ScreenToNearestHeightPointIndex(heightGrid, location);
            if (next.HasValue)
            {
                this.pinnedSingleHeightPoint = next;
                this.pinnedSingleHeightPointAnchor = location;
            }

            return next;
        }

        private Point? ResolveAreaHeightAnchor(IGrid<int> heightGrid, Point location)
        {
            if (this.pinnedAreaHeightPoint.HasValue && IsNear(location, this.pinnedAreaHeightPointAnchor, 12))
            {
                var p = this.pinnedAreaHeightPoint.Value;
                if (p.X >= 0 && p.Y >= 0 && p.X < heightGrid.Width && p.Y < heightGrid.Height)
                {
                    return p;
                }
            }

            var next = Util.ScreenToHeightIndex(heightGrid, location);
            if (next.HasValue)
            {
                this.pinnedAreaHeightPoint = next;
                this.pinnedAreaHeightPointAnchor = location;
                return next;
            }

            // Fallback around the cursor so repeated clicks can continue after projection shifts.
            Point? best = null;
            var bestDist = int.MaxValue;
            const int SearchRadius = 14;
            for (var dy = -SearchRadius; dy <= SearchRadius; dy += 2)
            {
                for (var dx = -SearchRadius; dx <= SearchRadius; dx += 2)
                {
                    var probe = new Point(location.X + dx, location.Y + dy);
                    var hit = Util.ScreenToHeightIndex(heightGrid, probe);
                    if (!hit.HasValue)
                    {
                        continue;
                    }

                    var dist = (dx * dx) + (dy * dy);
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        best = hit;
                    }
                }
            }

            if (best.HasValue)
            {
                this.pinnedAreaHeightPoint = best;
                this.pinnedAreaHeightPointAnchor = location;
            }

            return best;
        }

        private static bool IsNear(Point a, Point b, int threshold)
        {
            var dx = a.X - b.X;
            var dy = a.Y - b.Y;
            return (dx * dx) + (dy * dy) <= threshold * threshold;
        }

        private void RefreshSelection()
        {
            this.itemsLayer.Value.ClearSelection();

            if (this.mapModel == null)
            {
                return;
            }

            if (this.mapModel.SelectedTile.HasValue)
            {
                if (this.tileMapping.Count > this.mapModel.SelectedTile)
                {
                    this.itemsLayer.Value.AddToSelection(this.tileMapping[this.mapModel.SelectedTile.Value]);
                }
            }
            else if (this.mapModel.SelectedFeatures.Count > 0)
            {
                foreach (var item in this.mapModel.SelectedFeatures)
                {
                    if (this.featureMapping.ContainsKey(item))
                    {
                        this.itemsLayer.Value.AddToSelection(this.featureMapping[item]);
                    }
                }
            }
            else if (this.mapModel.SelectedStartPosition.HasValue)
            {
                var mapping = this.startPositionMapping[this.mapModel.SelectedStartPosition.Value];
                if (mapping != null)
                {
                    this.itemsLayer.Value.AddToSelection(mapping);
                }
            }
        }

        private void BaseTileChanged(object sender, EventArgs e)
        {
            this.baseTile.Invalidate();
        }

        private void TileLocationChanged(object sender, EventArgs e)
        {
            var item = (Positioned<IMapTile>)sender;
            var index = this.mapModel.FloatingTiles.IndexOf(item);

            this.RemoveTile(index);
            this.InsertTile(item, index);
        }

        private void StartPositionChanged(object sender, StartPositionChangedEventArgs e)
        {
            this.UpdateStartPosition(e.Index);
        }

        private void UpdateStartPosition(int index)
        {
            if (this.startPositionMapping[index] != null)
            {
                var mapping = this.startPositionMapping[index];
                this.itemsLayer.Value.Items.Remove(mapping);
                this.itemsLayer.Value.RemoveFromSelection(mapping);
                this.startPositionMapping[index] = null;
            }

            if (this.mapModel == null)
            {
                return;
            }

            var p = this.mapModel.GetStartPosition(index);
            if (p.HasValue)
            {
                var heightX = p.Value.X / 16;
                var heightY = p.Value.Y / 16;
                var heightValue = 0;
                if (heightX >= 0 && heightX < this.mapModel.BaseTile.HeightGrid.Width
                    && heightY >= 0 && heightY < this.mapModel.BaseTile.HeightGrid.Height)
                {
                    heightValue = this.mapModel.BaseTile.HeightGrid.Get(heightX, heightY);
                }

                var img = StartPositionImages[index];
                var i = new DrawableItem(
                    p.Value.X - (img.Width / 2),
                    p.Value.Y - 58 - (heightValue / 2),
                    int.MaxValue,
                    img);
                i.Tag = new StartPositionTag(index);
                this.startPositionMapping[index] = i;
                this.itemsLayer.Value.Items.Add(i);

                if (this.mapModel.SelectedStartPosition == index)
                {
                    this.itemsLayer.Value.AddToSelection(i);
                }
            }
        }

        private void TilesChanged(object sender, ListChangedEventArgs e)
        {
            switch (e.ListChangedType)
            {
                case ListChangedType.ItemAdded:
                    this.InsertTile(this.mapModel.FloatingTiles[e.NewIndex], e.NewIndex);
                    this.mapModel.FloatingTiles[e.NewIndex].LocationChanged += this.TileLocationChanged;
                    break;
                case ListChangedType.ItemDeleted:
                    this.RemoveTile(e.NewIndex);
                    break;
                case ListChangedType.ItemMoved:
                    this.RemoveTile(e.OldIndex);
                    this.InsertTile(this.mapModel.FloatingTiles[e.NewIndex], e.NewIndex);
                    break;
                case ListChangedType.Reset:
                    this.UpdateFloatingTiles();
                    break;
                default:
                    throw new ArgumentException("unknown list changed type: " + e.ListChangedType);
            }
        }

        private void FeatureInstanceChanged(object sender, FeatureInstanceEventArgs e)
        {
            switch (e.Action)
            {
                case FeatureInstanceEventArgs.ActionType.Add:
                    this.InsertFeature(e.FeatureInstanceId);
                    break;
                case FeatureInstanceEventArgs.ActionType.Move:
                    this.UpdateFeature(e.FeatureInstanceId);
                    break;
                case FeatureInstanceEventArgs.ActionType.Remove:
                    this.RemoveFeature(e.FeatureInstanceId);
                    break;
            }
        }

        private void UpdateFloatingTiles()
        {
            foreach (var t in this.tileMapping)
            {
                this.itemsLayer.Value.Items.Remove(t);
            }

            this.tileMapping.Clear();

            if (this.mapModel == null)
            {
                return;
            }

            var count = 0;
            foreach (var t in this.mapModel.FloatingTiles)
            {
                this.InsertTile(t, count++);
            }
        }

        private void InsertTile(Positioned<IMapTile> t, int index)
        {
            var drawable = new DrawableTile(t.Item);
            drawable.BackgroundColor = Color.CornflowerBlue;
            var i = new DrawableItem(
                    t.Location.X * 32,
                    t.Location.Y * 32,
                    index,
                    drawable);
            i.Tag = new SectionTag(index);
            this.tileMapping.Insert(index, i);
            this.itemsLayer.Value.Items.Add(i);

            if (this.mapModel.SelectedTile == index)
            {
                this.itemsLayer.Value.AddToSelection(i);
            }
        }

        private void RemoveTile(int index)
        {
            var item = this.tileMapping[index];
            this.itemsLayer.Value.Items.Remove(item);
            this.itemsLayer.Value.RemoveFromSelection(item);
            this.tileMapping.RemoveAt(index);
        }

        private int ToFeatureIndex(GridCoordinates p)
        {
            return this.ToFeatureIndex(p.X, p.Y);
        }

        private int ToFeatureIndex(int x, int y)
        {
            return (y * this.mapModel.FeatureGridWidth) + x;
        }

        private void InsertFeature(Guid id)
        {
            var f = this.mapModel.GetFeatureInstance(id);
            var coords = f.Location;
            var index = this.ToFeatureIndex(coords);

            var featureRecord = this.featureService.TryGetFeature(f.FeatureName).Or(DefaultFeatureRecord);

            var r = featureRecord.GetDrawBounds(this.mapModel.BaseTile.HeightGrid, coords.X, coords.Y);
            var mapBounds = new Rectangle(0, 0, this.mapModel.MapWidth * 32, this.mapModel.MapHeight * 32);
            if (!r.IntersectsWith(mapBounds))
            {
                return;
            }
            var i = new DrawableItem(
                    r.X,
                    r.Y,
                    index + 1000, // magic number to separate from tiles
                    new DrawableBitmap(featureRecord.Image));
            i.Tag = new FeatureTag(f.Id);
            i.Visible = this.featuresVisible;
            this.featureMapping[f.Id] = i;
            this.itemsLayer.Value.Items.Add(i);

            if (this.mapModel.SelectedFeatures.Contains(f.Id))
            {
                this.itemsLayer.Value.AddToSelection(i);
            }
        }

        private void UpdateFeature(Guid id)
        {
            this.RemoveFeature(id);
            this.InsertFeature(id);
        }

        private void RemoveFeature(Guid id)
        {
            if (this.featureMapping.ContainsKey(id))
            {
                var item = this.featureMapping[id];
                this.itemsLayer.Value.Items.Remove(item);
                this.itemsLayer.Value.RemoveFromSelection(item);
                this.featureMapping.Remove(id);
            }
        }

        private void SelectFromTag(object tag)
        {
            var t = (IMapItemTag)tag;
            t.SelectItem(this.dispatcher);
        }
    }
}
