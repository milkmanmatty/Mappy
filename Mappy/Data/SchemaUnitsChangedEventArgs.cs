namespace Mappy.Data
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Mappy.Util;
    using TAUtil.Tdf;

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
        }
    }
}
