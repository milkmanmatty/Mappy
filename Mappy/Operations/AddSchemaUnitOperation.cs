namespace Mappy.Operations
{
    using Mappy.Data;
    using Mappy.Models;

    public class AddSchemaUnitOperation : IReplayableOperation
    {
        public AddSchemaUnitOperation(IMapModel map, int schemaIndex, SchemaUnit unit)
        {
            this.Map = map;
            this.SchemaIndex = schemaIndex;
            this.Unit = unit;
        }

        public IMapModel Map { get; }

        public int SchemaIndex { get; }

        public SchemaUnit Unit { get; }

        public void Execute()
        {
            this.Map.AddSchemaUnit(this.SchemaIndex, this.Unit);
        }

        public void Undo()
        {
            this.Map.RemoveSchemaUnit(this.SchemaIndex, this.Unit.Id);
        }
    }
}
