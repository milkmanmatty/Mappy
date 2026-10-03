namespace Mappy.Models
{
    using System;
    using System.Drawing;

    /// <summary>
    /// The selected cell anchors the old map, or its playable area when moving the border,
    /// within the resized canvas.
    /// Anchor coordinates are 0 (left/top), 1 (centre), or 2 (right/bottom).
    /// </summary>
    public sealed class ResizeMapOptions
    {
        public const int StandardBorderWidth = 1;

        public const int StandardBorderHeight = 4;

        public ResizeMapOptions(Size size, Point anchor, bool moveStandardBorder = false)
        {
            if (size.Width < 1 || size.Height < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(size));
            }

            if (anchor.X < 0 || anchor.X > 2 || anchor.Y < 0 || anchor.Y > 2)
            {
                throw new ArgumentOutOfRangeException(nameof(anchor));
            }

            this.Size = size;
            this.Anchor = anchor;
            this.MoveStandardBorder = moveStandardBorder;
        }

        public Size Size { get; }

        public Point Anchor { get; }

        public bool MoveStandardBorder { get; }

        public Point GetTileOffset(Size originalSize)
        {
            return new Point(
                GetAxisOffset(this.Size.Width - originalSize.Width, this.Anchor.X),
                GetAxisOffset(this.Size.Height - originalSize.Height, this.Anchor.Y));
        }

        private static int GetAxisOffset(int difference, int anchor)
        {
            switch (anchor)
            {
                case 0:
                    return 0;
                case 1:
                    // Integer division puts any odd leftover on the right or bottom.
                    return difference / 2;
                default:
                    return difference;
            }
        }
    }
}
