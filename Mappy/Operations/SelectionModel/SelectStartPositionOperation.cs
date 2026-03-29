namespace Mappy.Operations.SelectionModel
{
    using Mappy.Models;

    public class SelectStartPositionOperation : IReplayableOperation
    {
        private readonly ISelectionModel model;

        private readonly int schemaIndex;

        private readonly int startSlotIndex;

        private int? previousSchema;

        private int? previousSlot;

        public SelectStartPositionOperation(ISelectionModel model, int schemaIndex, int startSlotIndex)
        {
            this.model = model;
            this.schemaIndex = schemaIndex;
            this.startSlotIndex = startSlotIndex;
        }

        public void Execute()
        {
            this.previousSchema = this.model.SelectedStartSchemaIndex;
            this.previousSlot = this.model.SelectedStartPosition;
            this.model.SelectStartPosition(this.schemaIndex, this.startSlotIndex);
        }

        public void Undo()
        {
            if (this.previousSchema.HasValue && this.previousSlot.HasValue)
            {
                this.model.SelectStartPosition(this.previousSchema.Value, this.previousSlot.Value);
            }
            else
            {
                this.model.DeselectStartPosition();
            }
        }
    }
}
