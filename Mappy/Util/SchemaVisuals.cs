namespace Mappy.Util
{
    using System.Drawing;

    public static class SchemaVisuals
    {
        private static readonly Color[] Palette =
        {
            Color.FromArgb(220, 60, 60),
            Color.FromArgb(80, 180, 80),
            Color.FromArgb(70, 130, 220),
            Color.FromArgb(230, 170, 50),
            Color.FromArgb(180, 80, 200),
            Color.FromArgb(50, 200, 200),
            Color.FromArgb(240, 120, 160),
            Color.FromArgb(160, 160, 80),
        };

        public static Color ColorForSchema(int schemaIndex)
        {
            return Palette[schemaIndex % Palette.Length];
        }
    }
}