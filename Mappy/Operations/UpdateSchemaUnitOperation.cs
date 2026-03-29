namespace Mappy.Operations
{
    using Mappy.Data;
    using Mappy.Models;

    public class UpdateSchemaUnitOperation : IReplayableOperation
    {
        private SchemaUnit oldUnit;

        public UpdateSchemaUnitOperation(IMapModel map, int schemaIndex, SchemaUnit newState)
        {
            this.Map = map;
            this.SchemaIndex = schemaIndex;
            this.NewState = newState;
        }

        public IMapModel Map { get; }

        public int SchemaIndex { get; }

        public SchemaUnit NewState { get; }

        public void Execute()
        {
            this.oldUnit = this.Map.Attributes.GetUnit(this.SchemaIndex, this.NewState.Id).ClonePreservingId();
            this.Map.UpdateSchemaUnit(this.SchemaIndex, this.NewState);
        }

        public void Undo()
        {
            this.Map.UpdateSchemaUnit(this.SchemaIndex, this.oldUnit);
        }
    }
}
