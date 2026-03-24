namespace Mappy.Data
{
    public readonly struct UnitCatalogLoadRecord
    {
        public UnitCatalogLoadRecord(string name, UnitSideCategory side, string displayName = null)
        {
            this.Name = name ?? string.Empty;
            this.Side = side;
            this.DisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim();
        }

        public string Name { get; }

        public UnitSideCategory Side { get; }

        public string DisplayName { get; }
    }
}
