namespace Mappy.Data
{
    public readonly struct UnitCatalogLoadRecord
    {
        public UnitCatalogLoadRecord(string name, string side, string displayName = null, string objectName = null, string tedClass = null)
        {
            this.Name = name ?? string.Empty;
            this.Side = UnitCatalogSide.Normalize(side);
            this.DisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim();
            this.ObjectName = string.IsNullOrWhiteSpace(objectName) ? null : objectName.Trim();
            this.TedClass = NormalizeTedClass(tedClass);
        }

        public string Name { get; }

        public string Side { get; }

        public string DisplayName { get; }

        public string ObjectName { get; }

        public string TedClass { get; }

        public static string NormalizeTedClass(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return string.Empty;
            }

            return raw.Trim().ToUpperInvariant();
        }
    }
}
