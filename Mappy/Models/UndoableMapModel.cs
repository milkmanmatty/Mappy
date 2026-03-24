namespace Mappy.Models
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Drawing;
    using System.Linq;
    using Mappy.Collections;
    using Mappy.Data;
    using Mappy.Models.BandboxBehaviours;
    using Mappy.Models.Enums;
    using Mappy.Operations;
    using Mappy.Operations.SelectionModel;
    using Mappy.Util;
    using Mappy.Util.ImageSampling;

    public sealed class UndoableMapModel : Notifier, IMainModel, IBandboxModel, IReadOnlyMapModel
    {
        private readonly OperationManager undoManager = new OperationManager();

        private readonly IBandboxBehaviour tileBandboxBehaviour;

        private readonly IBandboxBehaviour freeBandboxBehaviour;

        private ISelectionModel model;

        private IBandboxBehaviour currentBandboxBehaviour;

        private GUITab selectedGuiTab;

        private Point viewportLocation;

        private int deltaX;

        private int deltaY;

        private bool previousTranslationOpen;

        private bool previousSeaLevelOpen;

        private bool previousHeightBrushOpen;

        private bool previousVoidBrushOpen;

        private string openFilePath;

        private bool isFileReadOnly;

        private bool canCut;

        private bool canCopy;

        private bool canFill;

        private bool canFlip;

        private Maybe<Point> mousePosition;

        private Maybe<Guid> hoveredFeature;

        public UndoableMapModel(ISelectionModel model, string path, bool readOnly)
        {
            this.FilePath = path;
            this.IsFileReadOnly = readOnly;

            this.model = model;
            this.AttachModelHandlers(this.model);

            this.tileBandboxBehaviour = new TileBandboxBehaviour(this);
            this.tileBandboxBehaviour.PropertyChanged += this.BandboxBehaviourPropertyChanged;

            this.freeBandboxBehaviour = new FreeBandboxBehaviour(this);
            this.freeBandboxBehaviour.PropertyChanged += this.BandboxBehaviourPropertyChanged;

            this.currentBandboxBehaviour = this.tileBandboxBehaviour;

            this.undoManager.CanUndoChanged += this.UndoManagerOnCanUndoChanged;
            this.undoManager.CanRedoChanged += this.UndoManagerOnCanRedoChanged;
            this.undoManager.IsMarkedChanged += this.UndoManagerOnIsMarkedChanged;
        }

        public event EventHandler<ListChangedEventArgs> TilesChanged;

        public event EventHandler<GridEventArgs> BaseTileGraphicsChanged;

        public event EventHandler<GridEventArgs> BaseTileHeightChanged;

        public event EventHandler<StartPositionChangedEventArgs> StartPositionChanged;

        public event EventHandler<FeatureInstanceEventArgs> FeatureInstanceChanged;

        public bool CanCopy
        {
            get => this.canCopy;
            private set => this.SetField(ref this.canCopy, value, nameof(this.CanCopy));
        }

        public bool CanCut
        {
            get => this.canCut;
            private set => this.SetField(ref this.canCut, value, nameof(this.CanCut));
        }

        public bool CanFill
        {
            get => this.canFill;
            private set => this.SetField(ref this.canFill, value, nameof(this.CanFill));
        }

        public bool CanFlip
        {
            get => this.canFlip;
            private set => this.SetField(ref this.canFlip, value, nameof(this.CanFlip));
        }

        public string FilePath
        {
            get => this.openFilePath;
            private set => this.SetField(ref this.openFilePath, value, nameof(this.FilePath));
        }

        public bool IsFileReadOnly
        {
            get => this.isFileReadOnly;
            private set => this.SetField(ref this.isFileReadOnly, value, nameof(this.IsFileReadOnly));
        }

        public bool CanUndo => this.undoManager.CanUndo;

        public bool CanRedo => this.undoManager.CanRedo;

        public bool IsMarked => this.undoManager.IsMarked;

        public Bitmap Minimap => this.model.Minimap;

        public int SeaLevel => this.model.SeaLevel;

        public Rectangle BandboxRectangle => this.currentBandboxBehaviour.BandboxRectangle;

        public IMapTile BaseTile => this.model.Tile;

        IMapTile IReadOnlyMapModel.Tile => this.model.Tile;

        public int MapWidth => this.model.Tile.TileGrid.Width;

        public int MapHeight => this.model.Tile.TileGrid.Height;

        public int FeatureGridWidth => this.model.FeatureGridWidth;

        public int FeatureGridHeight => this.model.FeatureGridHeight;

        public ISparseGrid<bool> Voids => this.model.Voids;

        public MapAttributes Attributes => this.model.Attributes;

        public IList<Positioned<IMapTile>> FloatingTiles => this.model.FloatingTiles;

        public int? SelectedStartPosition => this.model.SelectedStartPosition;

        public int? SelectedStartSchemaIndex => this.model.SelectedStartSchemaIndex;

        public ObservableCollection<MapUnitRef> SelectedUnits => this.model.SelectedUnits;

        public int ActiveSchemaIndex
        {
            get => this.model.ActiveSchemaIndex;
            set => this.model.ActiveSchemaIndex = value;
        }

        public Point ViewportLocation
        {
            get => this.viewportLocation;
            set => this.SetField(ref this.viewportLocation, value, nameof(this.ViewportLocation));
        }

        public ObservableCollection<Guid> SelectedFeatures => this.model.SelectedFeatures;

        public int? SelectedTile => this.model.SelectedTile;

        public Maybe<Point> MousePosition
        {
            get => this.mousePosition;
            set => this.SetField(ref this.mousePosition, value, nameof(this.MousePosition));
        }

        public Maybe<Guid> HoveredFeature
        {
            get => this.hoveredFeature;
            set => this.SetField(ref this.hoveredFeature, value, nameof(this.HoveredFeature));
        }

        public void Undo()
        {
            this.undoManager.Undo();
            this.BaseTileHeightChanged?.Invoke(this, new GridEventArgs(0, 0));
        }

        public void Redo()
        {
            this.undoManager.Redo();
            this.BaseTileHeightChanged?.Invoke(this, new GridEventArgs(0, 0));
        }

        public void AddFeatureInstance(FeatureInstance instance)
        {
            this.model.AddFeatureInstance(instance);
        }

        public FeatureInstance GetFeatureInstance(Guid id)
        {
            return this.model.GetFeatureInstance(id);
        }

        public FeatureInstance GetFeatureInstanceAt(int x, int y)
        {
            return this.model.GetFeatureInstanceAt(x, y);
        }

        public void RemoveFeatureInstance(Guid id)
        {
            this.model.RemoveFeatureInstance(id);
        }

        public bool HasFeatureInstanceAt(int x, int y)
        {
            return this.model.HasFeatureInstanceAt(x, y);
        }

        public void UpdateFeatureInstance(FeatureInstance instance)
        {
            this.model.UpdateFeatureInstance(instance);
        }

        public void SelectTile(int index)
        {
            this.undoManager.Execute(
                new CompositeOperation(
                    OperationFactory.CreateDeselectAndMergeOperation(this.model),
                    new SelectTileOperation(this.model, index)));
        }

        public void SelectFeature(Guid id)
        {
            this.undoManager.Execute(
                new CompositeOperation(
                    OperationFactory.CreateDeselectAndMergeOperation(this.model),
                    new SelectFeatureOperation(this.model, id)));
        }

        public void SelectStartPosition(int schemaIndex, int startSlotIndex)
        {
            this.undoManager.Execute(new CompositeOperation(
                OperationFactory.CreateDeselectAndMergeOperation(this.model),
                new SelectStartPositionOperation(this.model, schemaIndex, startSlotIndex)));
        }

        public void DragDropStartPosition(int startSlotIndex, int x, int y)
        {
            var location = new Point(x, y);
            var schema = this.model.ActiveSchemaIndex;

            var op = new CompositeOperation(
                OperationFactory.CreateDeselectAndMergeOperation(this.model),
                new ChangeStartPositionOperation(this.model, schema, startSlotIndex, location),
                new SelectStartPositionOperation(this.model, schema, startSlotIndex));

            this.undoManager.Execute(op);
            this.previousTranslationOpen = false;
        }

        public void DragDropTile(IMapTile tile, int x, int y)
        {
            var quantX = x / 32;
            var quantY = y / 32;

            this.AddAndSelectTile(tile, quantX, quantY);
        }

        public void SetSeaLevel(int value)
        {
            if (this.SeaLevel == value)
            {
                return;
            }

            var op = new SetSealevelOperation(this.model, value);

            SetSealevelOperation prevOp = null;
            if (this.undoManager.CanUndo && this.previousSeaLevelOpen)
            {
                prevOp = this.undoManager.PeekUndo() as SetSealevelOperation;
            }

            if (prevOp == null)
            {
                this.undoManager.Execute(op);
            }
            else
            {
                op.Execute();
                var combinedOp = prevOp.Combine(op);
                this.undoManager.Replace(combinedOp);
            }

            this.previousSeaLevelOpen = true;
        }

        public void FlushSeaLevel()
        {
            this.previousSeaLevelOpen = false;
        }

        public void AdjustHeightBrush(int x, int y, int delta, int cursorSize)
        {
            if (delta == 0)
            {
                return;
            }

            var size = Math.Max(1, cursorSize);
            var grid = this.model.Tile.HeightGrid;

            if (size == 1)
            {
                var point = Util.ScreenToNearestHeightPointIndex(grid, new Point(x, y));
                if (point.HasValue)
                {
                    this.AdjustHeightPoint(point.Value.X, point.Value.Y, delta);
                }

                return;
            }

            var center = this.ScreenToHeightIndex(x, y);
            if (!center.HasValue)
            {
                return;
            }

            this.AdjustHeightBrushAtAnchor(center.Value.X, center.Value.Y, delta, size);
        }

        public void AdjustHeightBrushAtAnchor(int anchorX, int anchorY, int delta, int cursorSize)
        {
            if (delta == 0)
            {
                return;
            }

            var size = Math.Max(1, cursorSize);
            var grid = this.model.Tile.HeightGrid;
            var width = grid.Width;
            var height = grid.Height;

            if (anchorX < 0 || anchorY < 0 || anchorX >= width || anchorY >= height)
            {
                return;
            }

            if (size == 1)
            {
                this.AdjustHeightPoint(anchorX, anchorY, delta);
                return;
            }

            var endX = anchorX + 1;
            var endY = anchorY + 1;
            var startX = Math.Max(0, endX - size);
            var startY = Math.Max(0, endY - size);
            endX = Math.Min(width, endX);
            endY = Math.Min(height, endY);

            var changes = new List<HeightBrushOperation.HeightChange>();
            for (var yy = startY; yy < endY; yy++)
            {
                for (var xx = startX; xx < endX; xx++)
                {
                    var idx = (yy * width) + xx;
                    var oldValue = grid[idx];
                    var newValue = Util.Clamp(oldValue + delta, 0, 255);
                    if (newValue != oldValue)
                    {
                        changes.Add(new HeightBrushOperation.HeightChange(idx, oldValue, newValue));
                    }
                }
            }

            if (changes.Count == 0)
            {
                return;
            }

            this.ApplyHeightBrushOperation(new HeightBrushOperation(grid, changes));
        }

        public void AdjustHeightPoint(int pointX, int pointY, int delta)
        {
            if (delta == 0)
            {
                return;
            }

            var grid = this.model.Tile.HeightGrid;
            if (pointX < 0 || pointY < 0 || pointX >= grid.Width || pointY >= grid.Height)
            {
                return;
            }

            var idx = (pointY * grid.Width) + pointX;
            var oldValue = grid[idx];
            var newValue = Util.Clamp(oldValue + delta, 0, 255);
            if (newValue == oldValue)
            {
                return;
            }

            var singleChange = new List<HeightBrushOperation.HeightChange>
            {
                new HeightBrushOperation.HeightChange(idx, oldValue, newValue),
            };
            this.ApplyHeightBrushOperation(new HeightBrushOperation(grid, singleChange));
        }

        public void SetHeightBrushAtAnchor(int anchorX, int anchorY, int value, int cursorSize)
        {
            var setValue = Util.Clamp(value, 0, 255);
            var size = Math.Max(1, cursorSize);
            var grid = this.model.Tile.HeightGrid;
            var width = grid.Width;
            var height = grid.Height;

            if (anchorX < 0 || anchorY < 0 || anchorX >= width || anchorY >= height)
            {
                return;
            }

            if (size == 1)
            {
                this.SetHeightPoint(anchorX, anchorY, setValue);
                return;
            }

            var endX = anchorX + 1;
            var endY = anchorY + 1;
            var startX = Math.Max(0, endX - size);
            var startY = Math.Max(0, endY - size);
            endX = Math.Min(width, endX);
            endY = Math.Min(height, endY);

            var changes = new List<HeightBrushOperation.HeightChange>();
            for (var yy = startY; yy < endY; yy++)
            {
                for (var xx = startX; xx < endX; xx++)
                {
                    var idx = (yy * width) + xx;
                    var oldValue = grid[idx];
                    if (oldValue != setValue)
                    {
                        changes.Add(new HeightBrushOperation.HeightChange(idx, oldValue, setValue));
                    }
                }
            }

            if (changes.Count == 0)
            {
                return;
            }

            this.ApplyHeightBrushOperation(new HeightBrushOperation(grid, changes));
        }

        public void SetHeightPoint(int pointX, int pointY, int value)
        {
            var setValue = Util.Clamp(value, 0, 255);
            var grid = this.model.Tile.HeightGrid;
            if (pointX < 0 || pointY < 0 || pointX >= grid.Width || pointY >= grid.Height)
            {
                return;
            }

            var idx = (pointY * grid.Width) + pointX;
            var oldValue = grid[idx];
            if (oldValue == setValue)
            {
                return;
            }

            var singleChange = new List<HeightBrushOperation.HeightChange>
            {
                new HeightBrushOperation.HeightChange(idx, oldValue, setValue),
            };
            this.ApplyHeightBrushOperation(new HeightBrushOperation(grid, singleChange));
        }

        public void FlushHeightBrush()
        {
            this.previousHeightBrushOpen = false;
        }

        public void SetVoidBrushAtAnchor(int anchorX, int anchorY, int cursorSize, bool value)
        {
            var size = Math.Max(1, cursorSize);
            var grid = this.model.Voids;
            var width = grid.Width;
            var height = grid.Height;

            if (anchorX < 0 || anchorY < 0 || anchorX >= width || anchorY >= height)
            {
                return;
            }

            var endX = anchorX + 1;
            var endY = anchorY + 1;
            var startX = Math.Max(0, endX - size);
            var startY = Math.Max(0, endY - size);
            endX = Math.Min(width, endX);
            endY = Math.Min(height, endY);

            var changes = new List<VoidBrushOperation.VoidChange>();
            var featuresToRemove = new Dictionary<Guid, FeatureInstance>();
            for (var yy = startY; yy < endY; yy++)
            {
                for (var xx = startX; xx < endX; xx++)
                {
                    var idx = (yy * width) + xx;
                    var oldValue = grid[idx];
                    if (oldValue != value)
                    {
                        changes.Add(new VoidBrushOperation.VoidChange(idx, oldValue, value));
                    }

                    // Marking cells as void removes features occupying those cells.
                    if (value)
                    {
                        var feature = this.model.GetFeatureInstanceAt(xx, yy);
                        if (feature != null && !featuresToRemove.ContainsKey(feature.Id))
                        {
                            featuresToRemove[feature.Id] = feature;
                        }
                    }
                }
            }

            if (changes.Count == 0 && featuresToRemove.Count == 0)
            {
                return;
            }

            this.ApplyVoidBrushOperation(
                new VoidBrushOperation(
                    grid,
                    changes,
                    this.model,
                    featuresToRemove.Values));
        }

        public void FlushVoidBrush()
        {
            this.previousVoidBrushOpen = false;
        }

        public void ResizeMap(int newWidth, int newHeight)
        {
            if (newWidth < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(newWidth));
            }

            if (newHeight < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(newHeight));
            }

            if (newWidth == this.MapWidth && newHeight == this.MapHeight)
            {
                return;
            }

            var resizedModel = CreateResizedModel(this.model, newWidth, newHeight);
            this.undoManager.Execute(new ResizeMapOperation(this, this.model, resizedModel));
            this.previousTranslationOpen = false;
            this.previousSeaLevelOpen = false;
            this.previousHeightBrushOpen = false;
            this.previousVoidBrushOpen = false;
        }

        public IEnumerable<FeatureInstance> EnumerateFeatureInstances()
        {
            return this.model.EnumerateFeatureInstances();
        }

        public void DragDropFeature(string name, int x, int y, bool deselect = true)
        {
            var featurePos = this.ScreenToHeightIndex(x, y);
            if (featurePos.HasValue && !this.HasFeatureInstanceAt(featurePos.Value.X, featurePos.Value.Y))
            {
                var inst = new FeatureInstance(Guid.NewGuid(), name, featurePos.Value.X, featurePos.Value.Y);
                var addOp = new AddFeatureOperation(this.model, inst);
                var selectOp = new SelectFeatureOperation(this.model, inst.Id);
                if (deselect)
                {
                    var op = new CompositeOperation(
                        OperationFactory.CreateDeselectAndMergeOperation(this.model),
                        addOp,
                        selectOp);
                    this.undoManager.Execute(op);
                }
                else
                {
                    var op = new CompositeOperation(
                        addOp,
                        selectOp);
                    this.undoManager.Execute(op);
                }
            }
        }

        public void TranslateSelection(int x, int y)
        {
            if (this.SelectedStartPosition.HasValue && this.SelectedStartSchemaIndex.HasValue)
            {
                this.TranslateStartPosition(
                    this.SelectedStartSchemaIndex.Value,
                    this.SelectedStartPosition.Value,
                    x,
                    y);
            }
            else if (this.SelectedUnits.Count > 0)
            {
                this.TranslateSelectedUnitsPixelDelta(x, y);
            }
            else if (this.SelectedTile.HasValue)
            {
                this.deltaX += x;
                this.deltaY += y;

                this.TranslateSection(
                    this.SelectedTile.Value,
                    this.deltaX / 32,
                    this.deltaY / 32);

                this.deltaX %= 32;
                this.deltaY %= 32;
            }
            else if (this.SelectedFeatures.Count > 0)
            {
                // TODO: restore old behaviour
                // where heightmap is taken into account when placing features
                this.deltaX += x;
                this.deltaY += y;

                var quantX = this.deltaX / 16;
                var quantY = this.deltaY / 16;

                var success = this.TranslateFeatureBatch(
                    this.SelectedFeatures,
                    quantX,
                    quantY);

                if (success)
                {
                    this.deltaX %= 16;
                    this.deltaY %= 16;
                }
            }
        }

        public void FillWithSelectedTile()
        {
            if (this.SelectedTile == null)
            {
                return;
            }

            var fillOp = OperationFactory.CreateTileAreaOperation(this.FloatingTiles[this.SelectedTile.Value].Item, this.BaseTile);
            var deselectOp = new DeselectOperation(this.model);
            var removeOp = new RemoveTileOperation(this.FloatingTiles, this.SelectedTile.Value);

            this.undoManager.Execute(new CompositeOperation(fillOp, deselectOp, removeOp));
        }

        public void FlushTranslation()
        {
            this.previousTranslationOpen = false;
            this.deltaX = 0;
            this.deltaY = 0;
        }

        public void ClearSelection()
        {
            if (this.SelectedTile == null && (this.SelectedFeatures == null || this.SelectedFeatures.Count == 0)
                && (this.SelectedStartPosition == null || this.SelectedStartSchemaIndex == null)
                && (this.SelectedUnits == null || this.SelectedUnits.Count == 0))
            {
                return;
            }

            if (this.previousTranslationOpen)
            {
                this.FlushTranslation();
            }

            var deselectOp = new DeselectOperation(this.model);

            if (this.SelectedTile.HasValue)
            {
                var mergeOp = OperationFactory.CreateMergeSectionOperation(this.model, this.SelectedTile.Value);
                this.undoManager.Execute(new CompositeOperation(deselectOp, mergeOp));
            }
            else
            {
                this.undoManager.Execute(deselectOp);
            }
        }

        public void DeleteSelection()
        {
            if (this.SelectedFeatures.Count > 0)
            {
                var ops = new List<IReplayableOperation>();
                ops.Add(new DeselectOperation(this.model));
                ops.AddRange(this.SelectedFeatures.Select(x => new RemoveFeatureOperation(this.model, x)));
                this.undoManager.Execute(new CompositeOperation(ops));
            }

            if (this.SelectedTile.HasValue)
            {
                var deSelectOp = new DeselectOperation(this.model);
                var removeOp = new RemoveTileOperation(this.FloatingTiles, this.SelectedTile.Value);
                this.undoManager.Execute(new CompositeOperation(deSelectOp, removeOp));
            }

            if (this.SelectedStartPosition.HasValue && this.SelectedStartSchemaIndex.HasValue)
            {
                var deSelectOp = new DeselectOperation(this.model);
                var removeOp = new RemoveStartPositionOperation(
                    this.model,
                    this.SelectedStartSchemaIndex.Value,
                    this.SelectedStartPosition.Value);
                this.undoManager.Execute(new CompositeOperation(deSelectOp, removeOp));
            }

            if (this.SelectedUnits.Count > 0)
            {
                var unitRefs = this.SelectedUnits.ToList();
                var ops = new List<IReplayableOperation>();
                ops.Add(new DeselectOperation(this.model));
                ops.AddRange(unitRefs.Select(u => new RemoveSchemaUnitOperation(this.model, u.SchemaIndex, u.UnitId)));
                this.undoManager.Execute(new CompositeOperation(ops));
            }
        }

        public void UpdateSelectedGUITab(GUITab newTab)
        {
            this.selectedGuiTab = newTab;
            switch (newTab)
            {
                case GUITab.Features:
                case GUITab.Mission:
                    this.currentBandboxBehaviour = this.freeBandboxBehaviour;
                    break;
                case GUITab.Sections:
                case GUITab.Starts:
                case GUITab.Height:
                case GUITab.Void:
                case GUITab.Attributes:
                case GUITab.Other:
                default:
                    this.currentBandboxBehaviour = this.tileBandboxBehaviour;
                    return;
            }
        }

        public Point? GetStartPosition(int index)
        {
            return this.model.Attributes.GetStartPosition(this.model.ActiveSchemaIndex, index);
        }

        public IReadOnlyList<Point?> GetStartPositionVariantsForSlot(int startSlotIndex)
        {
            var list = new List<Point?>();
            for (var s = 0; s < this.model.Attributes.Schemas.Count; s++)
            {
                list.Add(this.model.Attributes.GetStartPosition(s, startSlotIndex));
            }

            return list;
        }

        public void DragDropSchemaUnit(string unitName, int x, int y, int player)
        {
            var schema = this.model.ActiveSchemaIndex;
            var u = new SchemaUnit(Guid.NewGuid(), unitName)
            {
                XPos = x,
                ZPos = y,
                Player = player,
            };
            var hx = x / 16;
            var hy = y / 16;
            if (hx >= 0 && hy >= 0 && hx < this.model.Tile.HeightGrid.Width && hy < this.model.Tile.HeightGrid.Height)
            {
                u.YPos = this.model.Tile.HeightGrid.Get(hx, hy);
            }

            var addOp = new AddSchemaUnitOperation(this.model, schema, u);
            var selectOp = new SelectUnitOperation(this.model, new MapUnitRef(schema, u.Id));
            var op = new CompositeOperation(
                OperationFactory.CreateDeselectAndMergeOperation(this.model),
                addOp,
                selectOp);
            this.undoManager.Execute(op);
        }

        public void SelectUnit(MapUnitRef r)
        {
            this.undoManager.Execute(
                new CompositeOperation(
                    OperationFactory.CreateDeselectAndMergeOperation(this.model),
                    new SelectUnitOperation(this.model, r)));
        }

        public void ApplySchemaUnitEdit(int schemaIndex, SchemaUnit edited)
        {
            this.undoManager.Execute(new UpdateSchemaUnitOperation(this.model, schemaIndex, edited));
        }

        public void MoveSchemaUnitBetweenSchemas(int fromSchemaIndex, int toSchemaIndex, SchemaUnit edited)
        {
            if (fromSchemaIndex == toSchemaIndex)
            {
                this.ApplySchemaUnitEdit(fromSchemaIndex, edited);
                return;
            }

            var removeOp = new RemoveSchemaUnitOperation(this.model, fromSchemaIndex, edited.Id);
            var addOp = new AddSchemaUnitOperation(this.model, toSchemaIndex, edited);
            var selectOp = new SelectUnitOperation(this.model, new MapUnitRef(toSchemaIndex, edited.Id));
            this.undoManager.Execute(
                new CompositeOperation(
                    OperationFactory.CreateDeselectAndMergeOperation(this.model),
                    removeOp,
                    addOp,
                    selectOp));
        }

        public void PasteSchemaUnitCopies(IReadOnlyList<SchemaUnit> units)
        {
            if (units == null || units.Count == 0)
            {
                return;
            }

            var schema = this.model.ActiveSchemaIndex;
            var ops = new List<IReplayableOperation>();
            ops.Add(OperationFactory.CreateDeselectAndMergeOperation(this.model));
            foreach (var u in units)
            {
                SyncSchemaUnitYPosFromHeightGrid(u, this.model);
                ops.Add(new AddSchemaUnitOperation(this.model, schema, u));
            }

            foreach (var u in units)
            {
                ops.Add(new AddUnitToSelectionOperation(this.model, new MapUnitRef(schema, u.Id)));
            }

            this.undoManager.Execute(new CompositeOperation(ops));
        }

        private static void SyncSchemaUnitYPosFromHeightGrid(SchemaUnit u, IMapModel map)
        {
            var hx = u.XPos / 16;
            var hy = u.ZPos / 16;
            if (hx >= 0 && hy >= 0 && hx < map.Tile.HeightGrid.Width && hy < map.Tile.HeightGrid.Height)
            {
                u.YPos = map.Tile.HeightGrid.Get(hx, hy);
            }
        }

        public void LiftAndSelectArea(int x, int y, int width, int height)
        {
            if (this.currentBandboxBehaviour == this.tileBandboxBehaviour)
            {
                var liftOp = OperationFactory.CreateClippedLiftAreaOperation(this.model, x, y, width, height);
                var index = this.FloatingTiles.Count;
                var selectOp = new SelectTileOperation(this.model, index);
                this.undoManager.Execute(new CompositeOperation(liftOp, selectOp));
            }
            else if (this.currentBandboxBehaviour == this.freeBandboxBehaviour)
            {
                var loc1 = new Point(x, y);
                var loc2 = new Point(x + width, y + height);
                var minX = Math.Min(loc1.X, loc2.X);
                var maxX = Math.Max(loc1.X, loc2.X);
                var minY = Math.Min(loc1.Y, loc2.Y);
                var maxY = Math.Max(loc1.Y, loc2.Y);

                if (this.selectedGuiTab == GUITab.Mission)
                {
                    var selections = new List<IReplayableOperation>();
                    for (var si = 0; si < this.model.Attributes.Schemas.Count; si++)
                    {
                        foreach (var u in this.model.Attributes.Schemas[si].Units)
                        {
                            if (u.XPos >= minX && u.XPos <= maxX && u.ZPos >= minY && u.ZPos <= maxY)
                            {
                                selections.Add(new AddUnitToSelectionOperation(this.model, new MapUnitRef(si, u.Id)));
                            }
                        }
                    }

                    if (selections.Count > 0)
                    {
                        this.undoManager.Execute(new CompositeOperation(selections));
                    }

                    return;
                }

                var validItems = new List<FeatureInstance>();
                foreach (var f in this.EnumerateFeatureInstances())
                {
                    var bounds = f.BaseFeature.GetDrawBounds(this.BaseTile.HeightGrid, f.X, f.Y);
                    if ((bounds.X + (bounds.Width * 0.5)) >= loc1.X && (bounds.Y + (bounds.Height * 0.5)) >= loc1.Y &&
                        (bounds.X + (bounds.Width * 0.5)) <= loc2.X && (bounds.Y + (bounds.Height * 0.5)) <= loc2.Y)
                    {
                        validItems.Add(f);
                    }
                }

                var featureSelections = new List<IReplayableOperation>();
                for (int i = 0; i < validItems.Count; i++)
                {
                    featureSelections.Add(new SelectFeatureOperation(this.model, validItems[i].Id));
                }

                this.undoManager.Execute(new CompositeOperation(featureSelections));
            }
        }

        public void StartBandbox(int x, int y)
        {
            this.currentBandboxBehaviour.StartBandbox(x, y);
        }

        public void GrowBandbox(int x, int y)
        {
            this.currentBandboxBehaviour.GrowBandbox(x, y);
        }

        public void CommitBandbox()
        {
            this.currentBandboxBehaviour.CommitBandbox();
        }

        public void ReplaceHeightmap(Grid<int> heightmap)
        {
            if (heightmap.Width != this.model.Tile.HeightGrid.Width
                || heightmap.Height != this.model.Tile.HeightGrid.Height)
            {
                throw new ArgumentException(
                    @"Dimensions do not match map heightmap",
                    nameof(heightmap));
            }

            var op = new CopyAreaOperation<int>(
                heightmap,
                this.model.Tile.HeightGrid,
                0,
                0,
                0,
                0,
                heightmap.Width,
                heightmap.Height);
            this.undoManager.Execute(op);
            SyncAllSchemaUnitHeightsFromHeightGrid(this.model);
        }

        public void SetMinimap(Bitmap minimap)
        {
            var op = new UpdateMinimapOperation(this.model, minimap);
            this.undoManager.Execute(op);
        }

        public void PasteMapTileNoDeduplicateTopLeft(IMapTile tile)
        {
            var x = this.ViewportLocation.X / 32;
            var y = this.ViewportLocation.Y / 32;

            this.AddAndSelectTile(tile, x, y);
        }

        public MapAttributesResult GetAttributes()
        {
            return MapAttributesResult.FromModel(this.model);
        }

        public void UpdateAttributes(MapAttributesResult newAttrs)
        {
            this.undoManager.Execute(new ChangeAttributesOperation(this.model, newAttrs));
        }

        public void MarkSaved(string filename)
        {
            this.FilePath = filename;
            this.IsFileReadOnly = false;
            this.undoManager.SetNowAsMark();
        }

        public void PasteMapTile(IMapTile tile, int x, int y)
        {
            this.PasteMapTileNoDeduplicate(tile, x, y);
        }

        private static void SyncAllSchemaUnitHeightsFromHeightGrid(ISelectionModel map)
        {
            var grid = map.Tile.HeightGrid;
            for (var si = 0; si < map.Attributes.Schemas.Count; si++)
            {
                foreach (var u in map.Attributes.Schemas[si].Units.ToList())
                {
                    var hx = u.XPos / 16;
                    var hz = u.ZPos / 16;
                    if (hx < 0 || hz < 0 || hx >= grid.Width || hz >= grid.Height)
                    {
                        continue;
                    }

                    var h = grid.Get(hx, hz);
                    if (u.YPos == h)
                    {
                        continue;
                    }

                    var nu = u.ClonePreservingId();
                    nu.YPos = h;
                    map.UpdateSchemaUnit(si, nu);
                }
            }
        }

        private void SyncSchemaUnitsYPosForHeightGridCell(ISelectionModel map, int hx, int hz)
        {
            var grid = map.Tile.HeightGrid;
            if (hx < 0 || hz < 0 || hx >= grid.Width || hz >= grid.Height)
            {
                return;
            }

            var height = grid.Get(hx, hz);
            for (var si = 0; si < map.Attributes.Schemas.Count; si++)
            {
                foreach (var u in map.Attributes.Schemas[si].Units.ToList())
                {
                    if (u.XPos / 16 != hx || u.ZPos / 16 != hz)
                    {
                        continue;
                    }

                    if (u.YPos == height)
                    {
                        continue;
                    }

                    var nu = u.ClonePreservingId();
                    nu.YPos = height;
                    map.UpdateSchemaUnit(si, nu);
                }
            }
        }

        private static ISelectionModel CreateResizedModel(ISelectionModel source, int newWidth, int newHeight)
        {
            var resizedModel = new MapModel(newWidth, newHeight);

            var fillTile = source.Tile.TileGrid.Get(0, 0);
            GridMethods.Fill(resizedModel.Tile.TileGrid, fillTile);

            var copyTileWidth = Math.Min(source.Tile.TileGrid.Width, resizedModel.Tile.TileGrid.Width);
            var copyTileHeight = Math.Min(source.Tile.TileGrid.Height, resizedModel.Tile.TileGrid.Height);
            GridMethods.Copy(source.Tile.TileGrid, resizedModel.Tile.TileGrid, 0, 0, 0, 0, copyTileWidth, copyTileHeight);

            var copyHeightWidth = Math.Min(source.Tile.HeightGrid.Width, resizedModel.Tile.HeightGrid.Width);
            var copyHeightHeight = Math.Min(source.Tile.HeightGrid.Height, resizedModel.Tile.HeightGrid.Height);
            GridMethods.Copy(source.Tile.HeightGrid, resizedModel.Tile.HeightGrid, 0, 0, 0, 0, copyHeightWidth, copyHeightHeight);

            var copyVoidWidth = Math.Min(source.Voids.Width, resizedModel.Voids.Width);
            var copyVoidHeight = Math.Min(source.Voids.Height, resizedModel.Voids.Height);
            GridMethods.Merge(source.Voids, resizedModel.Voids, 0, 0, 0, 0, copyVoidWidth, copyVoidHeight);

            var mapBounds = new Rectangle(0, 0, newWidth * 32, newHeight * 32);
            foreach (var feature in source.EnumerateFeatureInstances())
            {
                if (feature.X >= 0
                    && feature.Y >= 0
                    && feature.X < resizedModel.FeatureGridWidth
                    && feature.Y < resizedModel.FeatureGridHeight)
                {
                    var drawBounds = feature.BaseFeature.GetDrawBounds(
                        resizedModel.Tile.HeightGrid,
                        feature.X,
                        feature.Y);
                    if (mapBounds.Contains(drawBounds))
                    {
                        resizedModel.AddFeatureInstance(feature);
                    }
                }
            }

            resizedModel.Attributes.CopyFrom(source.Attributes);
            resizedModel.ActiveSchemaIndex = Math.Min(source.ActiveSchemaIndex, Math.Max(0, resizedModel.Attributes.Schemas.Count - 1));

            var maxX = (newWidth * 32) - 1;
            var maxY = (newHeight * 32) - 1;
            var hgW = resizedModel.Tile.HeightGrid.Width;
            var hgH = resizedModel.Tile.HeightGrid.Height;
            for (var si = 0; si < resizedModel.Attributes.Schemas.Count; si++)
            {
                for (var i = 0; i < 10; i++)
                {
                    var startPosition = resizedModel.Attributes.GetStartPosition(si, i);
                    if (!startPosition.HasValue)
                    {
                        continue;
                    }

                    var p = startPosition.Value;
                    if (p.X < 0 || p.Y < 0 || p.X > maxX || p.Y > maxY)
                    {
                        resizedModel.Attributes.SetStartPosition(si, i, null);
                    }
                }

                var toRemove = resizedModel.Attributes.Schemas[si].Units
                    .Where(
                        u =>
                        {
                            if (u.XPos < 0 || u.ZPos < 0 || u.XPos > maxX || u.ZPos > maxY)
                            {
                                return true;
                            }

                            var hx = u.XPos / 16;
                            var hz = u.ZPos / 16;
                            return hx < 0 || hz < 0 || hx >= hgW || hz >= hgH;
                        })
                    .Select(u => u.Id)
                    .ToList();
                foreach (var id in toRemove)
                {
                    resizedModel.RemoveSchemaUnit(si, id);
                }
            }

            SyncAllSchemaUnitHeightsFromHeightGrid(resizedModel);

            if (source.Minimap == null)
            {
                resizedModel.Minimap = null;
            }
            else
            {
                using (var adapter = new MapPixelImageAdapter(resizedModel.Tile.TileGrid))
                {
                    resizedModel.Minimap = Util.GenerateMinimap(adapter);
                }
            }

            return resizedModel;
        }

        private void ApplyHeightBrushOperation(HeightBrushOperation op)
        {
            var previousOp = this.undoManager.CanUndo && this.previousHeightBrushOpen
                ? this.undoManager.PeekUndo() as HeightBrushOperation
                : null;

            if (previousOp == null)
            {
                this.undoManager.Execute(op);
            }
            else
            {
                op.Execute();
                this.undoManager.Replace(previousOp.Combine(op));
            }

            this.previousHeightBrushOpen = true;
        }

        private void ApplyVoidBrushOperation(VoidBrushOperation op)
        {
            var previousOp = this.undoManager.CanUndo && this.previousVoidBrushOpen
                ? this.undoManager.PeekUndo() as VoidBrushOperation
                : null;

            if (previousOp == null)
            {
                this.undoManager.Execute(op);
            }
            else
            {
                op.Execute();
                this.undoManager.Replace(previousOp.Combine(op));
            }

            this.previousVoidBrushOpen = true;
        }

        private void ReplaceModel(ISelectionModel newModel)
        {
            if (ReferenceEquals(this.model, newModel))
            {
                return;
            }

            this.DetachModelHandlers(this.model);
            this.model = newModel;
            this.AttachModelHandlers(this.model);

            this.deltaX = 0;
            this.deltaY = 0;
            this.previousTranslationOpen = false;
            this.previousSeaLevelOpen = false;
            this.previousHeightBrushOpen = false;
            this.previousVoidBrushOpen = false;

            this.OnPropertyChanged(nameof(this.MapWidth));
            this.OnPropertyChanged(nameof(this.MapHeight));
            this.OnPropertyChanged(nameof(this.FeatureGridWidth));
            this.OnPropertyChanged(nameof(this.FeatureGridHeight));
            this.OnPropertyChanged(nameof(this.SeaLevel));
            this.OnPropertyChanged(nameof(this.Minimap));
            this.OnPropertyChanged(nameof(this.CanCut));
            this.OnPropertyChanged(nameof(this.CanCopy));
            this.OnPropertyChanged(nameof(this.CanFill));
            this.OnPropertyChanged(nameof(this.SelectedTile));
            this.OnPropertyChanged(nameof(this.SelectedFeatures));
            this.OnPropertyChanged(nameof(this.SelectedStartPosition));
            this.OnPropertyChanged(nameof(this.SelectedStartSchemaIndex));
            this.OnPropertyChanged(nameof(this.SelectedUnits));
            this.OnPropertyChanged(nameof(this.ActiveSchemaIndex));
        }

        private void PasteMapTileNoDeduplicate(IMapTile tile, int x, int y)
        {
            var normX = x / 32;
            var normY = y / 32;

            normX -= tile.TileGrid.Width / 2;
            normY -= tile.TileGrid.Height / 2;

            this.AddAndSelectTile(tile, normX, normY);
        }

        private void FloatingTilesOnListChanged(object sender, ListChangedEventArgs e)
        {
            this.TilesChanged?.Invoke(this, e);
        }

        private void TileOnTileGridChanged(object sender, GridEventArgs e)
        {
            this.BaseTileGraphicsChanged?.Invoke(this, e);
        }

        private void TileOnHeightGridChanged(object sender, GridEventArgs e)
        {
            this.BaseTileHeightChanged?.Invoke(this, e);
            this.SyncSchemaUnitsYPosForHeightGridCell(this.model, e.X, e.Y);
        }

        private void AttributesOnStartPositionChanged(object sender, StartPositionChangedEventArgs e)
        {
            this.StartPositionChanged?.Invoke(this, e);
        }

        private void SelectedUnitsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            this.OnPropertyChanged(nameof(this.SelectedUnits));
            this.UpdateCanCopy();
            this.UpdateCanCut();
        }

        private void AddAndSelectTile(IMapTile tile, int x, int y)
        {
            if (x < 0)
            {
                x = 0;
            }

            if (y < 0)
            {
                y = 0;
            }

            if (x >= this.MapWidth)
            {
                x = this.MapWidth - 1;
            }

            if (y >= this.MapHeight)
            {
                y = this.MapHeight - 1;
            }

            var floatingSection = new Positioned<IMapTile>(tile, new Point(x, y));
            var addOp = new AddFloatingTileOperation(this.model, floatingSection);

            // Tile's index should always be 0,
            // because all other tiles are merged before adding this one.
            var index = 0;

            var selectOp = new SelectTileOperation(this.model, index);
            var op = new CompositeOperation(
                OperationFactory.CreateDeselectAndMergeOperation(this.model),
                addOp,
                selectOp);

            this.undoManager.Execute(op);
        }

        private Point? ScreenToHeightIndex(int x, int y)
        {
            return Util.ScreenToHeightIndex(this.model.Tile.HeightGrid, new Point(x, y));
        }

        private bool TranslateFeatureBatch(ICollection<Guid> ids, int x, int y)
        {
            if (x == 0 && y == 0)
            {
                return true;
            }

            var coordSet = new HashSet<GridCoordinates>(ids.Select(i => this.GetFeatureInstance(i).Location));

            // pre-move check to see if anything is in our way
            foreach (var item in coordSet)
            {
                var translatedPoint = new GridCoordinates(item.X + x, item.Y + y);

                if (translatedPoint.X < 0
                    || translatedPoint.Y < 0
                    || translatedPoint.X >= this.FeatureGridWidth
                    || translatedPoint.Y >= this.FeatureGridHeight)
                {
                    return false;
                }

                var isBlocked = !coordSet.Contains(translatedPoint) &&
                    this.HasFeatureInstanceAt(translatedPoint.X, translatedPoint.Y);
                if (isBlocked)
                {
                    return false;
                }
            }

            var newOp = new BatchMoveFeatureOperation(this.model, ids, x, y);

            BatchMoveFeatureOperation lastOp = null;
            if (this.undoManager.CanUndo)
            {
                lastOp = this.undoManager.PeekUndo() as BatchMoveFeatureOperation;
            }

            if (this.previousTranslationOpen && lastOp != null && lastOp.CanCombine(newOp))
            {
                newOp.Execute();
                this.undoManager.Replace(lastOp.Combine(newOp));
            }
            else
            {
                this.undoManager.Execute(newOp);
            }

            this.previousTranslationOpen = true;

            return true;
        }

        private void TranslateSection(int index, int x, int y)
        {
            this.TranslateSection(this.FloatingTiles[index], x, y);
        }

        private void TranslateSection(Positioned<IMapTile> tile, int x, int y)
        {
            if (tile.Location.X + tile.Item.TileGrid.Width + x <= 0)
            {
                x = -tile.Location.X - (tile.Item.TileGrid.Width - 1);
            }

            if (tile.Location.Y + tile.Item.TileGrid.Height + y <= 0)
            {
                y = -tile.Location.Y - (tile.Item.TileGrid.Height - 1);
            }

            if (tile.Location.X + x >= this.MapWidth)
            {
                x = this.MapWidth - tile.Location.X - 1;
            }

            if (tile.Location.Y + x >= this.MapHeight)
            {
                y = this.MapHeight - tile.Location.Y - 1;
            }

            if (x == 0 && y == 0)
            {
                return;
            }

            var newOp = new MoveTileOperation(tile, x, y);

            MoveTileOperation lastOp = null;
            if (this.undoManager.CanUndo)
            {
                lastOp = this.undoManager.PeekUndo() as MoveTileOperation;
            }

            if (this.previousTranslationOpen && lastOp != null && lastOp.Tile == tile)
            {
                newOp.Execute();
                this.undoManager.Replace(lastOp.Combine(newOp));
            }
            else
            {
                this.undoManager.Execute(new MoveTileOperation(tile, x, y));
            }

            this.previousTranslationOpen = true;
        }

        private void TranslateStartPosition(int schemaIndex, int i, int x, int y)
        {
            var startPos = this.model.Attributes.GetStartPosition(schemaIndex, i);

            if (startPos == null)
            {
                throw new ArgumentException("Start position " + i + " has not been placed");
            }

            this.TranslateStartPositionTo(schemaIndex, i, startPos.Value.X + x, startPos.Value.Y + y);
        }

        private void TranslateStartPositionTo(int schemaIndex, int i, int x, int y)
        {
            var newOp = new ChangeStartPositionOperation(this.model, schemaIndex, i, new Point(x, y));

            ChangeStartPositionOperation lastOp = null;
            if (this.undoManager.CanUndo)
            {
                lastOp = this.undoManager.PeekUndo() as ChangeStartPositionOperation;
            }

            if (this.previousTranslationOpen && lastOp != null && lastOp.SchemaIndex == schemaIndex && lastOp.StartSlotIndex == i)
            {
                newOp.Execute();
                this.undoManager.Replace(lastOp.Combine(newOp));
            }
            else
            {
                this.undoManager.Execute(newOp);
            }

            this.previousTranslationOpen = true;
        }

        private void TranslateSelectedUnitsPixelDelta(int dx, int dy)
        {
            if (dx == 0 && dy == 0)
            {
                return;
            }

            var refs = this.SelectedUnits.ToList();
            if (refs.Count == 0)
            {
                return;
            }

            var ops = new List<IReplayableOperation>(refs.Count);
            foreach (var r in refs)
            {
                var u = this.model.Attributes.GetUnit(r.SchemaIndex, r.UnitId);
                var nu = u.ClonePreservingId();
                nu.XPos += dx;
                nu.ZPos += dy;
                var hx = nu.XPos / 16;
                var hz = nu.ZPos / 16;
                var grid = this.model.Tile.HeightGrid;
                if (hx >= 0 && hz >= 0 && hx < grid.Width && hz < grid.Height)
                {
                    nu.YPos = grid.Get(hx, hz);
                }

                ops.Add(new UpdateSchemaUnitOperation(this.model, r.SchemaIndex, nu));
            }

            this.undoManager.Execute(new CompositeOperation(ops));
            this.previousTranslationOpen = false;
        }

        private void BandboxBehaviourPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "BandboxRectangle":
                    this.OnPropertyChanged("BandboxRectangle");
                    break;
            }
        }

        private void SelectedFeaturesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            this.UpdateCanCut();
            this.UpdateCanCopy();
        }

        private void UpdateCanCopy()
        {
            this.CanCopy = this.SelectedTile.HasValue || this.SelectedFeatures.Count > 0 || this.SelectedUnits.Count > 0;
        }

        private void UpdateCanCut()
        {
            this.CanCut = this.SelectedTile.HasValue || this.SelectedFeatures.Count > 0 || this.SelectedUnits.Count > 0;
        }

        private void UpdateCanFill()
        {
            this.CanFill = this.SelectedTile.HasValue;
        }

        private void UpdateCanFlip()
        {
            this.CanFlip = this.SelectedTile.HasValue;
        }

        private void UndoManagerOnIsMarkedChanged(object sender, EventArgs eventArgs)
        {
            this.OnPropertyChanged("IsMarked");
        }

        private void UndoManagerOnCanRedoChanged(object sender, EventArgs eventArgs)
        {
            this.OnPropertyChanged("CanRedo");
        }

        private void UndoManagerOnCanUndoChanged(object sender, EventArgs eventArgs)
        {
            this.OnPropertyChanged("CanUndo");
        }

        private void ModelOnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            this.OnPropertyChanged(propertyChangedEventArgs.PropertyName);

            switch (propertyChangedEventArgs.PropertyName)
            {
                case "SelectedTile":
                    this.UpdateCanCut();
                    this.UpdateCanCopy();
                    this.UpdateCanFill();
                    this.UpdateCanFlip();
                    break;
            }
        }

        private void AttachModelHandlers(ISelectionModel selectionModel)
        {
            selectionModel.PropertyChanged += this.ModelOnPropertyChanged;
            selectionModel.FloatingTilesChanged += this.FloatingTilesOnListChanged;
            selectionModel.TileGridChanged += this.TileOnTileGridChanged;
            selectionModel.HeightGridChanged += this.TileOnHeightGridChanged;
            selectionModel.Attributes.StartPositionChanged += this.AttributesOnStartPositionChanged;
            selectionModel.FeatureInstanceChanged += this.ModelOnFeatureInstanceChanged;
            selectionModel.SelectedFeatures.CollectionChanged += this.SelectedFeaturesCollectionChanged;
            selectionModel.SelectedUnits.CollectionChanged += this.SelectedUnitsCollectionChanged;
        }

        private void DetachModelHandlers(ISelectionModel selectionModel)
        {
            selectionModel.PropertyChanged -= this.ModelOnPropertyChanged;
            selectionModel.FloatingTilesChanged -= this.FloatingTilesOnListChanged;
            selectionModel.TileGridChanged -= this.TileOnTileGridChanged;
            selectionModel.HeightGridChanged -= this.TileOnHeightGridChanged;
            selectionModel.Attributes.StartPositionChanged -= this.AttributesOnStartPositionChanged;
            selectionModel.FeatureInstanceChanged -= this.ModelOnFeatureInstanceChanged;
            selectionModel.SelectedFeatures.CollectionChanged -= this.SelectedFeaturesCollectionChanged;
            selectionModel.SelectedUnits.CollectionChanged -= this.SelectedUnitsCollectionChanged;
        }

        private void ModelOnFeatureInstanceChanged(object sender, FeatureInstanceEventArgs e)
        {
            this.FeatureInstanceChanged?.Invoke(this, e);
        }

        private sealed class ResizeMapOperation : IReplayableOperation
        {
            private readonly UndoableMapModel owner;
            private readonly ISelectionModel oldModel;
            private readonly ISelectionModel newModel;

            public ResizeMapOperation(UndoableMapModel owner, ISelectionModel oldModel, ISelectionModel newModel)
            {
                this.owner = owner;
                this.oldModel = oldModel;
                this.newModel = newModel;
            }

            public void Execute()
            {
                this.owner.ReplaceModel(this.newModel);
            }

            public void Undo()
            {
                this.owner.ReplaceModel(this.oldModel);
            }
        }
    }
}
