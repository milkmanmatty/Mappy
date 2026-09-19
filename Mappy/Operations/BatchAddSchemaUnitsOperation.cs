namespace Mappy.Operations
{
    using System.Collections.Generic;
    using System.Linq;

    using Mappy.Data;
    using Mappy.Models;

    public class BatchAddSchemaUnitsOperation : IReplayableOperation
    {
        private readonly List<SchemaUnit> units;

        public BatchAddSchemaUnitsOperation(IMapModel map, int schemaIndex, IEnumerable<SchemaUnit> units)
        {
            this.Map = map;
            this.SchemaIndex = schemaIndex;
            this.units = units?.ToList() ?? new List<SchemaUnit>();
        }

        public IMapModel Map { get; }

        public int SchemaIndex { get; }

        public void Execute()
        {
            if (this.units.Count == 0)
            {
                return;
            }

            this.Map.Attributes.BeginSchemaUnitsChangedMute();
            try
            {
                foreach (var unit in this.units)
                {
                    this.Map.AddSchemaUnit(this.SchemaIndex, unit);
                }
            }
            finally
            {
                this.Map.Attributes.EndSchemaUnitsChangedMuteAndNotify(this.SchemaIndex);
            }
        }

        public void Undo()
        {
            if (this.units.Count == 0)
            {
                return;
            }

            this.Map.Attributes.BeginSchemaUnitsChangedMute();
            try
            {
                foreach (var unit in this.units)
                {
                    this.Map.RemoveSchemaUnit(this.SchemaIndex, unit.Id);
                }
            }
            finally
            {
                this.Map.Attributes.EndSchemaUnitsChangedMuteAndNotify(this.SchemaIndex);
            }
        }
    }
}
