namespace Mappy.Models
{
    using System;

    /// <summary>
    /// Identifies a unit instance within a specific OTA schema.
    /// </summary>
    public sealed class MapUnitRef : IEquatable<MapUnitRef>
    {
        public MapUnitRef(int schemaIndex, Guid unitId)
        {
            this.SchemaIndex = schemaIndex;
            this.UnitId = unitId;
        }

        public int SchemaIndex { get; }

        public Guid UnitId { get; }

        public bool Equals(MapUnitRef other)
        {
            return other != null && this.SchemaIndex == other.SchemaIndex && this.UnitId == other.UnitId;
        }

        public override bool Equals(object obj) => this.Equals(obj as MapUnitRef);

        public override int GetHashCode() => (this.SchemaIndex * 397) ^ this.UnitId.GetHashCode();
    }
}
