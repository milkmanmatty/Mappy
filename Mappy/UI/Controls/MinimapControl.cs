namespace Mappy.UI.Controls
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Drawing;
    using System.Linq;
    using System.Windows.Forms;

    public sealed class MinimapControl : Control
    {
        private readonly Dictionary<int, MarkerInfo> markers = new Dictionary<int, MarkerInfo>();

        private bool rectVisible = true;
        private Color rectColor = Color.Black;
        private float rectThickness = 1.0f;
        private Rectangle viewportRect;

        public MinimapControl()
        {
            this.DoubleBuffered = true;
            this.ResizeRedraw = true;
        }

        [DefaultValue(true)]
        public bool RectVisible
        {
            get => this.rectVisible;

            set
            {
                this.rectVisible = value;
                this.Invalidate(this.CoveredRect);
            }
        }

        [DefaultValue(typeof(Color), "Black")]
        public Color RectColor
        {
            get => this.rectColor;

            set
            {
                this.rectColor = value;
                this.Invalidate(this.CoveredRect);
            }
        }

        [DefaultValue(1.0f)]
        public float RectThickness
        {
            get => this.rectThickness;

            set
            {
                this.rectThickness = value;
                this.Invalidate(this.CoveredRect);
            }
        }

        public Rectangle ViewportRect
        {
            get => this.viewportRect;

            set
            {
                this.Invalidate(this.CoveredRect);
                this.viewportRect = value;
                this.Invalidate(this.CoveredRect);
            }
        }

        public override Image BackgroundImage
        {
            get => base.BackgroundImage;

            set
            {
                base.BackgroundImage = value;
                this.BackgroundImageLayout = ImageLayout.None;
                this.Invalidate();
            }
        }

        protected override Size DefaultSize => new Size(252, 252);

        private Rectangle CoveredRect => this.ClientRectangle;

        public Point ControlToImagePoint(Point p)
        {
            if (this.BackgroundImage == null || this.BackgroundImage.Width <= 0 || this.BackgroundImage.Height <= 0)
            {
                return p;
            }

            var r = this.ComputeImageRect();
            if (r.Width <= 0 || r.Height <= 0)
            {
                return Point.Empty;
            }

            var x = (int)Math.Round((p.X - r.X) * this.BackgroundImage.Width / r.Width);
            var y = (int)Math.Round((p.Y - r.Y) * this.BackgroundImage.Height / r.Height);
            x = Math.Max(0, Math.Min(this.BackgroundImage.Width - 1, x));
            y = Math.Max(0, Math.Min(this.BackgroundImage.Height - 1, y));
            return new Point(x, y);
        }

        public void SetMarker(int id, Point position, Color color)
        {
            MarkerInfo oldInfo;
            if (this.markers.TryGetValue(id, out oldInfo))
            {
                this.InvalidateMarker(oldInfo.Position);
            }

            this.markers[id] = new MarkerInfo(position, color);
            this.InvalidateMarker(position);
        }

        public void RemoveMarker(int id)
        {
            MarkerInfo oldInfo;
            if (this.markers.TryGetValue(id, out oldInfo))
            {
                this.InvalidateMarker(oldInfo.Position);
            }

            this.markers.Remove(id);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            e.Graphics.Clear(this.BackColor);
            if (this.BackgroundImage != null)
            {
                e.Graphics.DrawImage(this.BackgroundImage, this.ComputeImageRect());
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (this.BackgroundImage != null)
            {
                foreach (var marker in this.markers.Select(x => x.Value))
                {
                    DrawMarker(e.Graphics, this.ImageToControlPoint(marker.Position), marker.Color);
                }

                if (this.RectVisible)
                {
                    var displayRect = this.ImageToControlRect(this.viewportRect);
                    using (var p = new Pen(this.RectColor, this.RectThickness))
                    {
                        e.Graphics.DrawRectangle(p, displayRect);
                    }
                }
            }
            else
            {
                foreach (var marker in this.markers.Select(x => x.Value))
                {
                    DrawMarker(e.Graphics, marker.Position, marker.Color);
                }

                if (this.RectVisible)
                {
                    using (var pen = new Pen(this.RectColor, this.RectThickness))
                    {
                        e.Graphics.DrawRectangle(pen, this.ViewportRect);
                    }
                }
            }
        }

        private RectangleF ComputeImageRect()
        {
            if (this.BackgroundImage == null)
            {
                return new RectangleF(PointF.Empty, this.ClientSize);
            }

            float sx = this.Width / (float)this.BackgroundImage.Width;
            float sy = this.Height / (float)this.BackgroundImage.Height;
            float scale = Math.Min(sx, sy);
            float w = this.BackgroundImage.Width * scale;
            float h = this.BackgroundImage.Height * scale;
            return new RectangleF((this.Width - w) / 2f, (this.Height - h) / 2f, w, h);
        }

        private Point ImageToControlPoint(Point p)
        {
            if (this.BackgroundImage == null)
            {
                return p;
            }

            var r = this.ComputeImageRect();
            return new Point(
                (int)Math.Round(r.X + (p.X * r.Width / this.BackgroundImage.Width)),
                (int)Math.Round(r.Y + (p.Y * r.Height / this.BackgroundImage.Height)));
        }

        private Rectangle ImageToControlRect(Rectangle r)
        {
            if (this.BackgroundImage == null)
            {
                return r;
            }

            var ir = this.ComputeImageRect();
            float sx = ir.Width / this.BackgroundImage.Width;
            float sy = ir.Height / this.BackgroundImage.Height;
            return new Rectangle(
                (int)Math.Round(ir.X + (r.X * sx)),
                (int)Math.Round(ir.Y + (r.Y * sy)),
                (int)Math.Round(r.Width * sx),
                (int)Math.Round(r.Height * sy));
        }

        private static void DrawMarker(Graphics g, Point position, Color color)
        {
            var bounds = GetMarkerBounds(position);
            using (Brush b = new SolidBrush(color))
            {
                g.FillRectangle(b, bounds);
            }
        }

        private static Rectangle GetMarkerBounds(Point position)
        {
            return new Rectangle(
                position.X - 1,
                position.Y - 1,
                3,
                3);
        }

        private void InvalidateMarker(Point imagePosition)
        {
            if (this.BackgroundImage == null)
            {
                this.Invalidate();
                return;
            }

            var controlPoint = this.ImageToControlPoint(imagePosition);
            this.Invalidate(GetMarkerBounds(controlPoint));
        }

        private struct MarkerInfo
        {
            public MarkerInfo(Point position, Color color)
            {
                this.Position = position;
                this.Color = color;
            }

            public Point Position { get; }

            public Color Color { get; }
        }
    }
}
