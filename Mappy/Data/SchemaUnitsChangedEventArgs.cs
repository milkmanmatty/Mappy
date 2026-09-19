namespace Mappy.Data
{
    using System;

    public class SchemaUnitsChangedEventArgs : EventArgs
    {
        public SchemaUnitsChangedEventArgs(int schemaIndex, ActionKind action, Guid unitId)
        {
            this.SchemaIndex = schemaIndex;
            this.Action = action;
            this.UnitId = unitId;
        }

        public int SchemaIndex { get; }

        public ActionKind Action { get; }

        public Guid UnitId { get; }

        public enum ActionKind
        {
            Add,
            Remove,
            Move,
            Bulk,
        }
    }
}
