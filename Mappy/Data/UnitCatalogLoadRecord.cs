namespace Mappy.Data
{
    public readonly struct UnitCatalogLoadRecord
    {
        public UnitCatalogLoadRecord(string name, UnitSideCategory side)
        {
            this.Name = name ?? string.Empty;
            this.Side = side;
        }

        public string Name { get; }

        public UnitSideCategory Side { get; }
    }
}
