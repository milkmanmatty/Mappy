namespace Mappy.Operations.SelectionModel
{
    using System;
    using System.Collections.Generic;

    using Mappy.Models;

    public class DeselectOperation : IReplayableOperation
    {
        private readonly ISelectionModel model;

        private int? prevTile;

        private int? prevStartSchema;

        private int? prevStartSlot;

        private List<Guid> features;

        private List<MapUnitRef> units;

        public DeselectOperation(ISelectionModel model)
        {
            this.model = model;
        }

        public void Execute()
        {
            this.prevTile = this.model.SelectedTile;
            this.features = new List<Guid>(this.model.SelectedFeatures);
            this.units = new List<MapUnitRef>(this.model.SelectedUnits);
            this.prevStartSchema = this.model.SelectedStartSchemaIndex;
            this.prevStartSlot = this.model.SelectedStartPosition;

            this.model.DeselectAll();
        }

        public void Undo()
        {
            if (this.prevTile.HasValue)
            {
                this.model.SelectTile(this.prevTile.Value);
            }

            if (this.prevStartSchema.HasValue && this.prevStartSlot.HasValue)
            {
                this.model.SelectStartPosition(this.prevStartSchema.Value, this.prevStartSlot.Value);
            }

            foreach (var f in this.features)
            {
                this.model.SelectFeature(f);
            }

            foreach (var u in this.units)
            {
                this.model.SelectUnit(u);
            }
        }
    }
}
