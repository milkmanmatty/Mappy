namespace Mappy.Data
{
    using System;

    public static class UnitCatalogSide
    {
        public const string Unknown = "";

        public static string Normalize(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return Unknown;
            }

            return raw.Trim();
        }

        public static int CompareTabOrder(string a, string b) => string.Compare(FormatTabLabel(a), FormatTabLabel(b), StringComparison.OrdinalIgnoreCase);

        public static string FormatTabLabel(string side)
        {
            side = Normalize(side);
            return side.Length == 0 ? "Unknown" : side;
        }
    }
}