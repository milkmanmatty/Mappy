namespace Mappy.Operations
{
    using System.Collections.Generic;
    using System.Linq;

    using Mappy.Models;

    public class ClearMarkerLinesOperation : IReplayableOperation
    {
        private readonly ISelectionModel model;

        private List<MarkerLine> previous;

        public ClearMarkerLinesOperation(ISelectionModel model)
        {
            this.model = model;
        }

        public void Execute()
        {
            this.previous = this.model.MarkerLines.ToList();
            this.model.ReplaceMarkerLines(null);
        }

        public void Undo()
        {
            this.model.ReplaceMarkerLines(this.previous);
        }
    }
}
