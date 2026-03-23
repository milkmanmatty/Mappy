namespace Mappy.Operations
{
    using System;
    using Mappy.Data;
    using Mappy.Models;

    public class RemoveSchemaUnitOperation : IReplayableOperation
    {
        private SchemaUnit removed;

        public RemoveSchemaUnitOperation(IMapModel map, int schemaIndex, Guid unitId)
        {
            this.Map = map;
            this.SchemaIndex = schemaIndex;
            this.UnitId = unitId;
        }

        public IMapModel Map { get; }

        public int SchemaIndex { get; }

        public Guid UnitId { get; }

        public void Execute()
        {
            this.removed = this.Map.Attributes.GetUnit(this.SchemaIndex, this.UnitId).ClonePreservingId();
            this.Map.RemoveSchemaUnit(this.SchemaIndex, this.UnitId);
        }

        public void Undo()
        {
            this.Map.AddSchemaUnit(this.SchemaIndex, this.removed);
        }
    }
}
