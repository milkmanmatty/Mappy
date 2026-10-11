namespace Mappy.Operations
{
    using Mappy.Models;

    public class AddMarkerLineOperation : IReplayableOperation
    {
        private readonly ISelectionModel model;

        private readonly MarkerLine line;

        public AddMarkerLineOperation(ISelectionModel model, MarkerLine line)
        {
            this.model = model;
            this.line = line;
        }

        public void Execute()
        {
            this.model.AddMarkerLine(this.line);
        }

        public void Undo()
        {
            this.model.RemoveMarkerLine(this.line.Id);
        }
    }
}
