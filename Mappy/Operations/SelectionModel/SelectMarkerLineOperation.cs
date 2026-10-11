namespace Mappy.Operations.SelectionModel
{
    using System;

    using Mappy.Models;

    public class SelectMarkerLineOperation : IReplayableOperation
    {
        private readonly ISelectionModel model;

        private readonly Guid id;

        private Guid? previous;

        public SelectMarkerLineOperation(ISelectionModel model, Guid id)
        {
            this.model = model;
            this.id = id;
        }

        public void Execute()
        {
            this.previous = this.model.SelectedMarkerLine;
            this.model.SelectMarkerLine(this.id);
        }

        public void Undo()
        {
            if (this.previous.HasValue && this.previous.Value != this.id)
            {
                this.model.SelectMarkerLine(this.previous.Value);
            }
            else
            {
                this.model.DeselectMarkerLine();
            }
        }
    }
}
