namespace Mappy.UI.Drawables
{
    using System;
    using System.Drawing;

    public sealed class DrawableFilledEllipse : AbstractDrawable, IDisposable
    {
        private readonly Brush fillBrush;

        private readonly Pen borderPen;

        public DrawableFilledEllipse(Brush fillBrush, Pen borderPen, Size size)
        {
            this.fillBrush = fillBrush;
            this.borderPen = borderPen;
            this.Size = size;
        }

        public override Size Size { get; }

        public override int Width => this.Size.Width;

        public override int Height => this.Size.Height;

        public static DrawableFilledEllipse CreateSimple(Size size, Color color, Color borderColor)
        {
            return CreateSimple(size, color, borderColor, 1);
        }

        public static DrawableFilledEllipse CreateSimple(
            Size size,
            Color color,
            Color borderColor,
            int borderWidth)
        {
            return new DrawableFilledEllipse(
                new SolidBrush(color),
                new Pen(borderColor, borderWidth),
                size);
        }

        public override void Draw(Graphics graphics, Rectangle clipRectangle)
        {
            graphics.FillEllipse(this.fillBrush, 0, 0, this.Width - 1, this.Height - 1);
            graphics.DrawEllipse(this.borderPen, 0, 0, this.Width - 1, this.Height - 1);
        }

        public void Dispose()
        {
            this.fillBrush.Dispose();
            this.borderPen.Dispose();
        }
    }
}
