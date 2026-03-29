namespace Mappy.Util
{
    using System.Drawing;
    using TAUtil.Gdi.Palette;

    public static class TaTeamColorRemap
    {
        public const int RampLength = 16;
        public const int AccentIndex = 9;

        private static readonly int[] Ramp16Starts = { 96, 112, 128 };

        public const int BrightBlueStart = 224;
        public const int BrightBlueLength = 8;

        public static Color[] BuildTargetRamp(int player)
        {
            var primary = PlayerSlotVisuals.BackgroundForPlayer(player);
            var ramp = new Color[RampLength];
            var light = BlendRgb(primary, Color.White, 0.45);
            var dark = BlendRgb(primary, Color.Black, 0.55);
            for (var i = 0; i < RampLength; i++)
            {
                var t = i / (float)(RampLength - 1);
                ramp[i] = BlendRgb(light, dark, t);
            }

            return ramp;
        }

        public static int FlatShadeArgb(int colorIndex, Color[] targetRamp, IPalette taPalette)
        {
            var ci = colorIndex & 255;

            if (ci == AccentIndex)
            {
                return Color.FromArgb(255, targetRamp[0]).ToArgb();
            }

            foreach (var start in Ramp16Starts)
            {
                if (ci >= start && ci < start + RampLength)
                {
                    return Color.FromArgb(255, targetRamp[ci - start]).ToArgb();
                }
            }

            if (ci >= BrightBlueStart && ci < BrightBlueStart + BrightBlueLength)
            {
                var step = ((ci - BrightBlueStart) * (RampLength - 1)) / (BrightBlueLength - 1);
                return Color.FromArgb(255, targetRamp[step]).ToArgb();
            }

            var c = taPalette[ci];
            var alpha = c.A == 0 ? (byte)255 : c.A;
            return Color.FromArgb(alpha, c.R, c.G, c.B).ToArgb();
        }

        private static Color BlendRgb(Color a, Color b, double t)
        {
            return Color.FromArgb(
                255,
                (int)(a.R + ((b.R - a.R) * t)),
                (int)(a.G + ((b.G - a.G) * t)),
                (int)(a.B + ((b.B - a.B) * t)));
        }
    }
}
