namespace Mappy.UI.Controls
{
    using System;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Windows.Forms;

    public sealed class LayerView : ScrollableControl
    {
        private const int ClientCoordinateLimit = int.MaxValue / 4;

        // Need to know the width to help avoid smearing for items outside of the updated region
        private const int MaxLayerPenWidth = 3;

        private static readonly float[] ZoomLevels = { 0.125f, 0.25f, 0.5f, 0.75f, 1.0f, 1.5f, 2.0f, 3.0f, 4.0f };

        private readonly LayerCollection layers = new LayerCollection();

        private Size canvasSize;

        private float zoomFactor = 1.0f;

        public LayerView()
        {
            this.DoubleBuffered = true;
            this.layers.FullRedraw += this.LayersOnFullRedraw;
            this.layers.AreaChanged += this.LayersOnAreaChanged;
        }

        public event EventHandler CanvasSizeChanged;

        public event EventHandler ZoomFactorChanged;

        public static int ZoomLevelCount => ZoomLevels.Length;

        public Func<int, bool, bool> ShiftMouseWheelHandler { get; set; }

        public LayerCollection Layers => this.layers;

        public Size CanvasSize
        {
            get => this.canvasSize;

            set
            {
                if (value != this.canvasSize)
                {
                    this.canvasSize = value;
                    this.OnCanvasSizeChanged();
                }
            }
        }

        public float ZoomFactor => this.zoomFactor;

        public int ZoomLevelIndex => IndexOfNearestZoomLevel(this.zoomFactor);

        public Size ScaledCanvasSize => new Size(
            (int)Math.Ceiling(this.canvasSize.Width * this.zoomFactor),
            (int)Math.Ceiling(this.canvasSize.Height * this.zoomFactor));

        public Size VisibleVirtualSize => new Size(
            (int)Math.Ceiling(this.ClientSize.Width / this.zoomFactor),
            (int)Math.Ceiling(this.ClientSize.Height / this.zoomFactor));

        public Point MaxScrollPosition
        {
            get
            {
                var scaled = this.ScaledCanvasSize;
                return new Point(
                    Math.Max(scaled.Width - this.ClientSize.Width, 0),
                    Math.Max(scaled.Height - this.ClientSize.Height, 0));
            }
        }

        // Used to calculate the size of the region to update
        private int StrokeAllowance =>
            (int)Math.Ceiling(MaxLayerPenWidth * this.zoomFactor / 2.0) + 1;

        private Point ClientCentre => new Point(this.ClientSize.Width / 2, this.ClientSize.Height / 2);

        public Point ToVirtualPoint(Point clientPoint)
        {
            var scroll = this.AutoScrollPosition;
            return new Point(
                (int)Math.Floor((clientPoint.X - scroll.X) / this.zoomFactor),
                (int)Math.Floor((clientPoint.Y - scroll.Y) / this.zoomFactor));
        }

        public Rectangle ToClientRect(Rectangle rect)
        {
            var scroll = this.AutoScrollPosition;
            return Rectangle.FromLTRB(
                ToClientCoordinate(rect.Left, this.zoomFactor, scroll.X, false),
                ToClientCoordinate(rect.Top, this.zoomFactor, scroll.Y, false),
                ToClientCoordinate((long)rect.Left + rect.Width, this.zoomFactor, scroll.X, true),
                ToClientCoordinate((long)rect.Top + rect.Height, this.zoomFactor, scroll.Y, true));
        }

        public Rectangle ToVirtualRect(Rectangle clientRect)
        {
            var scroll = this.AutoScrollPosition;
            return Rectangle.FromLTRB(
                (int)Math.Floor((clientRect.Left - scroll.X) / this.zoomFactor),
                (int)Math.Floor((clientRect.Top - scroll.Y) / this.zoomFactor),
                (int)Math.Ceiling((clientRect.Right - scroll.X) / this.zoomFactor),
                (int)Math.Ceiling((clientRect.Bottom - scroll.Y) / this.zoomFactor));
        }

        public void ScrollToVirtualLocation(Point virtualLocation)
        {
            this.AutoScrollPosition = new Point(
                (int)Math.Round(virtualLocation.X * this.zoomFactor),
                (int)Math.Round(virtualLocation.Y * this.zoomFactor));
        }

        public bool ZoomBySteps(int steps, Point clientAnchor)
        {
            return this.ZoomToLevel(this.ZoomLevelIndex + steps, clientAnchor);
        }

        public bool SetZoomLevelIndex(int levelIndex)
        {
            return this.ZoomToLevel(levelIndex, this.ClientCentre);
        }

        protected override void OnPaint(PaintEventArgs pe)
        {
            base.OnPaint(pe);

            if (this.zoomFactor != 1.0f)
            {
                pe.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
                pe.Graphics.PixelOffsetMode = PixelOffsetMode.Half;
            }

            // Translate and scale the graphics context to virtual coordinates.
            pe.Graphics.TranslateTransform(
                this.AutoScrollPosition.X,
                this.AutoScrollPosition.Y);
            pe.Graphics.ScaleTransform(this.zoomFactor, this.zoomFactor);

            // Translate the clip rectangle to virtual coordinates
            // and limit it to within canvas bounds.
            var canvasClipRectangle = Rectangle.Intersect(
                new Rectangle(Point.Empty, this.CanvasSize),
                this.ToVirtualRect(pe.ClipRectangle));

            // paint the layers
            this.layers.Draw(pe.Graphics, canvasClipRectangle);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            this.Focus();

            base.OnMouseDown(e);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            var modifiers = ModifierKeys;

            if ((modifiers & (Keys.Control | Keys.Shift)) == Keys.Control)
            {
                var zoomSteps = e.Delta / SystemInformation.MouseWheelScrollDelta;
                if (zoomSteps != 0)
                {
                    this.ZoomBySteps(zoomSteps, this.GetZoomAnchor());
                }

                MarkHandled(e);
                return;
            }

            // vertical scroll
            if ((modifiers & Keys.Shift) != Keys.Shift)
            {
                base.OnMouseWheel(e);
                return;
            }

            var ctrlPressed = (modifiers & Keys.Control) == Keys.Control;
            if (this.ShiftMouseWheelHandler?.Invoke(e.Delta, ctrlPressed) == true)
            {
                return;
            }

            // horizontal scroll
            var maxX = this.MaxScrollPosition.X;
            if (maxX == 0)
            {
                return;
            }

            var current = new Point(-this.AutoScrollPosition.X, -this.AutoScrollPosition.Y);

            var notchDelta = e.Delta / SystemInformation.MouseWheelScrollDelta;
            var wheelLines = SystemInformation.MouseWheelScrollLines;

            int deltaX;
            if (wheelLines == -1)
            {
                deltaX = notchDelta * this.ClientSize.Width * -1;
            }
            else
            {
                var step = this.VerticalScroll.SmallChange > 0 ? this.VerticalScroll.SmallChange : 16;
                var lines = wheelLines > 0 ? wheelLines : 1;
                deltaX = notchDelta * step * lines * -1;
            }

            var nextX = Math.Max(0, Math.Min(maxX, current.X + deltaX));
            if (nextX != current.X)
            {
                this.AutoScrollPosition = new Point(nextX, current.Y);
            }
        }

        private static void MarkHandled(MouseEventArgs e)
        {
            if (e is HandledMouseEventArgs handled)
            {
                handled.Handled = true;
            }
        }

        private static int Clamp(int value, int min, int max)
        {
            return Math.Max(min, Math.Min(max, value));
        }

        private static int ToClientCoordinate(long virtualValue, float zoom, int scroll, bool roundUp)
        {
            var scaled = (virtualValue * (double)zoom) + scroll;
            var rounded = roundUp ? Math.Ceiling(scaled) : Math.Floor(scaled);

            return (int)Math.Max(-ClientCoordinateLimit, Math.Min(ClientCoordinateLimit, rounded));
        }

        private static int IndexOfNearestZoomLevel(float zoom)
        {
            var nearest = 0;
            for (var i = 1; i < ZoomLevels.Length; i++)
            {
                if (Math.Abs(ZoomLevels[i] - zoom) < Math.Abs(ZoomLevels[nearest] - zoom))
                {
                    nearest = i;
                }
            }

            return nearest;
        }

        private bool ZoomToLevel(int levelIndex, Point clientAnchor)
        {
            var oldZoom = this.zoomFactor;
            var newZoom = ZoomLevels[Clamp(levelIndex, 0, ZoomLevels.Length - 1)];
            if (newZoom == oldZoom)
            {
                return false;
            }

            var scroll = this.AutoScrollPosition;
            var anchorX = (clientAnchor.X - scroll.X) / oldZoom;
            var anchorY = (clientAnchor.Y - scroll.Y) / oldZoom;

            this.zoomFactor = newZoom;
            this.AutoScrollMinSize = this.ScaledCanvasSize;

            var max = this.MaxScrollPosition;
            this.AutoScrollPosition = new Point(
                Clamp((int)Math.Round((anchorX * newZoom) - clientAnchor.X), 0, max.X),
                Clamp((int)Math.Round((anchorY * newZoom) - clientAnchor.Y), 0, max.Y));

            this.Invalidate();
            this.ZoomFactorChanged?.Invoke(this, EventArgs.Empty);

            return true;
        }

        private Point GetZoomAnchor()
        {
            var clientPoint = this.PointToClient(Cursor.Position);
            if (this.ClientRectangle.Contains(clientPoint))
            {
                return clientPoint;
            }

            return this.ClientCentre;
        }

        private void OnCanvasSizeChanged()
        {
            this.AutoScrollMinSize = this.ScaledCanvasSize;
            this.CanvasSizeChanged?.Invoke(this, EventArgs.Empty);
        }

        private void LayersOnAreaChanged(object sender, AreaChangedEventArgs e)
        {
            var virtualRect = e.ChangedRectangle;
            var clientRect = this.ToClientRect(virtualRect);

            var allowance = this.StrokeAllowance;
            clientRect.Inflate(allowance, allowance);

            var intersect = Rectangle.Intersect(clientRect, this.ClientRectangle);

            if (intersect != Rectangle.Empty)
            {
                this.Invalidate(intersect);
            }
        }

        private void LayersOnFullRedraw(object sender, EventArgs e)
        {
            this.Invalidate();
        }
    }
}
