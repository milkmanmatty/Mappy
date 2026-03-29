namespace Mappy.Util
{
    using System.Drawing;

    public static class PlayerSlotVisuals
    {
        private static readonly Color[] Backgrounds =
        {
            Color.FromArgb(0, 102, 204),
            Color.FromArgb(226, 54, 46),
            Color.White,
            Color.FromArgb(50, 205, 50),
            Color.FromArgb(0, 51, 102),
            Color.FromArgb(128, 0, 180),
            Color.FromArgb(255, 220, 50),
            Color.Black,
            Color.FromArgb(128, 128, 128),
            Color.FromArgb(255, 200, 160),
            Color.FromArgb(0, 120, 120),
        };

        private static readonly Color[] Foregrounds =
        {
            Color.White,
            Color.White,
            Color.Black,
            Color.Black,
            Color.White,
            Color.White,
            Color.Black,
            Color.White,
            Color.White,
            Color.Black,
            Color.White,
        };

        public static int ClampPlayerSlot(int player)
        {
            if (player < 1)
            {
                return 1;
            }

            if (player > 11)
            {
                return 11;
            }

            return player;
        }

        public static Color BackgroundForPlayer(int player)
        {
            return Backgrounds[ClampPlayerSlot(player) - 1];
        }

        public static Color ForegroundForPlayer(int player)
        {
            return Foregrounds[ClampPlayerSlot(player) - 1];
        }
    }
}
