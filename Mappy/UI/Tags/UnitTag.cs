namespace Mappy.UI.Tags
{
    using System;

    using Mappy.Services;

    public class UnitTag : IMapItemTag
    {
        public UnitTag(int schemaIndex, Guid unitId)
        {
            this.SchemaIndex = schemaIndex;
            this.UnitId = unitId;
        }

        public int SchemaIndex { get; }

        public Guid UnitId { get; }

        public void SelectItem(Dispatcher dispatcher)
        {
            dispatcher.SelectUnit(this.SchemaIndex, this.UnitId);
        }
    }
}