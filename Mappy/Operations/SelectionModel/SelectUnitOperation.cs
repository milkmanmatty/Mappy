namespace Mappy.Operations.SelectionModel
{
    using System.Collections.Generic;
    using System.Linq;
    using Mappy.Models;

    public class SelectUnitOperation : IReplayableOperation
    {
        private readonly ISelectionModel model;

        private readonly MapUnitRef unitRef;

        private List<MapUnitRef> previousSelection;

        public SelectUnitOperation(ISelectionModel model, MapUnitRef unitRef)
        {
            this.model = model;
            this.unitRef = unitRef;
        }

        public void Execute()
        {
            this.previousSelection = this.model.SelectedUnits.ToList();
            this.model.DeselectUnits();
            this.model.SelectUnit(this.unitRef);
        }

        public void Undo()
        {
            this.model.DeselectUnits();
            foreach (var u in this.previousSelection)
            {
                this.model.SelectUnit(u);
            }
        }
    }
}