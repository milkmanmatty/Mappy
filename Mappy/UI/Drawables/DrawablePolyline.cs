namespace Mappy.UI.Drawables
{
    using System;
    using System.Drawing;

    public sealed class DrawablePolyline : AbstractDrawable, IDisposable
    {
        private readonly Pen pen;

        private readonly Point[] points;

        private readonly Size size;

        public DrawablePolyline(Point[] points, Size size, Pen pen)
        {
            this.points = points;
            this.size = size;
            this.pen = pen;
        }

        public override Size Size => this.size;

        public override int Width => this.size.Width;

        public override int Height => this.size.Height;

        public override void Draw(Graphics graphics, Rectangle clipRectangle)
        {
            if (this.points.Length >= 2)
            {
                graphics.DrawPolygon(this.pen, this.points);
            }
        }

        public void Dispose()
        {
            this.pen.Dispose();
        }
    }
}
