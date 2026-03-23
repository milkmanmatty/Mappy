namespace Mappy.Operations.SelectionModel
{
    using Mappy.Models;

    public class AddUnitToSelectionOperation : IReplayableOperation
    {
        private readonly ISelectionModel model;

        private readonly MapUnitRef unitRef;

        public AddUnitToSelectionOperation(ISelectionModel model, MapUnitRef unitRef)
        {
            this.model = model;
            this.unitRef = unitRef;
        }

        public void Execute()
        {
            this.model.SelectUnit(this.unitRef);
        }

        public void Undo()
        {
            this.model.DeselectUnit(this.unitRef);
        }
    }
}
