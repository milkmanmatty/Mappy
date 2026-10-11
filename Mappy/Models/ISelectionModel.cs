namespace Mappy.Models
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;

    using Mappy.Collections;

    public interface ISelectionModel : IMapModel, INotifyPropertyChanged
    {
        event EventHandler<FeatureInstanceEventArgs> FeatureInstanceChanged;

        event EventHandler MarkerLinesChanged;

        event EventHandler<GridEventArgs> TileGridChanged;

        event EventHandler<GridEventArgs> HeightGridChanged;

        event ListChangedEventHandler FloatingTilesChanged;

        event EventHandler<SparseGridEventArgs> VoidsChanged;

        int? SelectedTile { get; }

        int? SelectedStartPosition { get; }

        int? SelectedStartSchemaIndex { get; }

        ObservableCollection<Guid> SelectedFeatures { get; }

        ObservableCollection<MapUnitRef> SelectedUnits { get; }

        IReadOnlyList<MarkerLine> MarkerLines { get; }

        Guid? SelectedMarkerLine { get; }

        void SelectTile(int index);

        void DeselectTile();

        void TranslateSelectedTile(int x, int y);

        void DeleteSelectedTile();

        void MergeSelectedTile();

        void SelectFeature(Guid id);

        void DeselectFeature(Guid id);

        void DeselectFeatures();

        void DeletedSelectedFeatures();

        void SelectUnit(MapUnitRef unitRef);

        void DeselectUnit(MapUnitRef unitRef);

        void DeselectUnits();

        void DeleteSelectedUnits();

        void SelectStartPosition(int schemaIndex, int startSlotIndex);

        void DeselectStartPosition();

        void TranslateSelectedStartPosition(int x, int y);

        void DeleteSelectedStartPosition();

        void DeselectAll();

        void AddMarkerLine(MarkerLine line);

        void RemoveMarkerLine(Guid id);

        MarkerLine FindMarkerLine(Guid id);

        void ReplaceMarkerLines(IEnumerable<MarkerLine> lines);

        void SelectMarkerLine(Guid id);

        void DeselectMarkerLine();
    }
}
