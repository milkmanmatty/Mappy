namespace Mappy.Operations
{
    using System;

    using Mappy.Models;

    public class RemoveMarkerLineOperation : IReplayableOperation
    {
        private readonly ISelectionModel model;

        private readonly Guid id;

        private MarkerLine removed;

        public RemoveMarkerLineOperation(ISelectionModel model, Guid id)
        {
            this.model = model;
            this.id = id;
        }

        public void Execute()
        {
            this.removed = this.model.FindMarkerLine(this.id);
            this.model.RemoveMarkerLine(this.id);
        }

        public void Undo()
        {
            if (this.removed != null)
            {
                this.model.AddMarkerLine(this.removed);
            }
        }
    }
}
