namespace Mappy.UI.Controls
{
    using System;
    using System.Drawing;
    using System.Windows.Forms;

    using Mappy.Models;

    public partial class MapViewPanel : UserControl
    {
        private const int AutoScrollEdgeThreshold = 24;

        private IMapViewViewModel model;

        private Point oldAutoScrollPos;

        private readonly Timer dragScrollTimer = new Timer { Interval = 16 };

        private bool dragInProgress;

        private bool panning;

        private bool spaceKeyDown;

        private Point panLastMousePos;

        public MapViewPanel()
        {
            this.InitializeComponent();

            this.mapView.Layers.Add(new DummyLayer());
            this.mapView.Layers.Add(new DummyLayer());
            this.mapView.Layers.Add(new DummyLayer());
            this.mapView.Layers.Add(new DummyLayer());

            this.dragScrollTimer.Tick += this.DragScrollTimerTick;
            this.Disposed += this.MapViewPanelDisposed;
        }

        public void SetModel(IMapViewViewModel model)
        {
            model.CanvasSize.Subscribe(x => this.mapView.CanvasSize = x);
            model.ViewportLocation.Subscribe(x => this.mapView.AutoScrollPosition = x);

            model.ItemsLayer.Subscribe(x => this.mapView.Layers[0] = x);
            model.VoidLayer.Subscribe(x => this.mapView.Layers[1] = x);
            this.mapView.Layers[2] = model.GridLayer;
            this.mapView.Layers[3] = model.GuidesLayer;

            this.model = model;
        }

        private void MapViewDragDrop(object sender, DragEventArgs e)
        {
            var loc = this.mapView.ToVirtualPoint(this.mapView.PointToClient(new Point(e.X, e.Y)));
            this.model.DragDrop(e.Data, loc);
        }

        private void MapViewMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && this.spaceKeyDown)
            {
                this.StartPanning(e.Location);
                return;
            }

            var loc = this.mapView.ToVirtualPoint(e.Location);
            if (e.Button == MouseButtons.Left)
            {
                this.StartDrag();
                this.model.MouseLeftDown(loc);
            }
            else if (e.Button == MouseButtons.Right)
            {
                this.StartDrag();
                this.model.MouseRightDown(loc);
            }
        }

        private void MapViewMouseMove(object sender, MouseEventArgs e)
        {
            if (this.panning)
            {
                this.PanViewport(e.Location);
                this.model.MouseMove(this.mapView.ToVirtualPoint(e.Location));
                return;
            }

            if (this.dragInProgress)
            {
                this.TryAutoScroll(e.Location);
                this.UpdateDragScrollTimer(e.Location);
            }

            var loc = this.mapView.ToVirtualPoint(e.Location);
            this.model.MouseMove(loc);
        }

        private void MapViewMouseUp(object sender, MouseEventArgs e)
        {
            if (this.panning)
            {
                this.StopPanning();
                return;
            }

            this.StopDrag();
            this.model.MouseUp();
        }

        private void MapViewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space)
            {
                this.spaceKeyDown = true;
            }

            this.model.KeyDown(e.KeyCode);
        }

        private void MapViewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space)
            {
                this.spaceKeyDown = false;
            }
        }

        private void MapViewPreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            // If the user presses space while the app doesn't have focus we won't pick up the event :(
            if (e.KeyCode == Keys.Space)
            {
                e.IsInputKey = true;
            }
        }

        private void MapViewLeave(object sender, EventArgs e)
        {
            this.spaceKeyDown = false;

            if (this.panning)
            {
                this.StopPanning();
                return;
            }

            if (this.dragInProgress)
            {
                return;
            }

            this.model.LeaveFocus();
        }

        private void MapViewSizeChanged(object sender, EventArgs e)
        {
            // this null check has to be here
            // since it seems this event fires during construction,
            // before we get a chance to assign the model.
            this.model?.ClientSizeChanged(this.mapView.ClientSize);
        }

        private void MapViewDragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void MapViewPaint(object sender, PaintEventArgs e)
        {
            // We listen to paint to detect when scroll position has changed.
            // We could use the scroll event, but this only detects
            // scrollbar interaction, and won't catch other scrolling
            // such as mouse wheel scrolling.
            var pos = this.mapView.AutoScrollPosition;
            if (pos != this.oldAutoScrollPos)
            {
                var loc = new Point(pos.X * -1, pos.Y * -1);
                this.model.ScrollPositionChanged(loc);
                this.oldAutoScrollPos = pos;
            }
        }

        private void MapViewPanelDisposed(object sender, EventArgs e)
        {
            this.dragScrollTimer.Stop();
            this.dragScrollTimer.Tick -= this.DragScrollTimerTick;
            this.dragScrollTimer.Dispose();
            this.Disposed -= this.MapViewPanelDisposed;
        }

        private void StartPanning(Point location)
        {
            this.panning = true;
            this.panLastMousePos = location;
            this.mapView.Capture = true;
            this.mapView.Cursor = Cursors.Hand;
        }

        private void StopPanning()
        {
            this.panning = false;
            this.mapView.Capture = false;
            this.mapView.Cursor = Cursors.Default;
        }

        private void PanViewport(Point location)
        {
            var deltaX = location.X - this.panLastMousePos.X;
            var deltaY = location.Y - this.panLastMousePos.Y;
            this.panLastMousePos = location;

            if (deltaX == 0 && deltaY == 0)
            {
                return;
            }

            var scrollPos = this.GetScrollPosition();
            var maxX = Math.Max(this.mapView.CanvasSize.Width - this.mapView.ClientSize.Width, 0);
            var maxY = Math.Max(this.mapView.CanvasSize.Height - this.mapView.ClientSize.Height, 0);
            var next = new Point(
                this.Clamp(scrollPos.X - deltaX, 0, maxX),
                this.Clamp(scrollPos.Y - deltaY, 0, maxY));

            if (next != scrollPos)
            {
                this.mapView.AutoScrollPosition = next;
            }
        }

        private void StartDrag()
        {
            this.dragInProgress = true;
            this.mapView.Capture = true;
        }

        private void StopDrag()
        {
            this.dragInProgress = false;
            this.dragScrollTimer.Stop();
            this.mapView.Capture = false;
        }

        private void DragScrollTimerTick(object sender, EventArgs e)
        {
            if (!this.dragInProgress || !this.IsDragButtonDown())
            {
                this.StopDrag();
                return;
            }

            var clientPoint = this.mapView.PointToClient(Cursor.Position);
            this.TryAutoScroll(clientPoint);

            var virtualPoint = this.mapView.ToVirtualPoint(clientPoint);
            this.model.MouseMove(virtualPoint);
        }

        private void UpdateDragScrollTimer(Point clientPoint)
        {
            if (this.IsNearAutoScrollEdge(clientPoint) && this.IsDragButtonDown())
            {
                this.dragScrollTimer.Start();
            }
            else
            {
                this.dragScrollTimer.Stop();
            }
        }

        private bool IsDragButtonDown()
        {
            var buttons = Control.MouseButtons;
            return (buttons & MouseButtons.Left) == MouseButtons.Left ||
                   (buttons & MouseButtons.Right) == MouseButtons.Right;
        }

        private bool IsNearAutoScrollEdge(Point clientPoint)
        {
            return clientPoint.X <= AutoScrollEdgeThreshold ||
                   clientPoint.Y <= AutoScrollEdgeThreshold ||
                   clientPoint.X >= this.mapView.ClientSize.Width - AutoScrollEdgeThreshold ||
                   clientPoint.Y >= this.mapView.ClientSize.Height - AutoScrollEdgeThreshold;
        }

        private void TryAutoScroll(Point clientPoint)
        {
            var viewport = this.GetViewportLocation();

            var deltaX = this.GetAutoScrollDelta(clientPoint.X, this.mapView.ClientSize.Width, true);
            var deltaY = this.GetAutoScrollDelta(clientPoint.Y, this.mapView.ClientSize.Height, false);

            if (deltaX == 0 && deltaY == 0)
            {
                return;
            }

            var maxX = Math.Max(this.mapView.CanvasSize.Width - this.mapView.ClientSize.Width, 0);
            var maxY = Math.Max(this.mapView.CanvasSize.Height - this.mapView.ClientSize.Height, 0);

            var next = new Point(
                this.Clamp(viewport.X + deltaX, 0, maxX),
                this.Clamp(viewport.Y + deltaY, 0, maxY));

            if (next != viewport)
            {
                this.mapView.AutoScrollPosition = next;
            }
        }

        private Point GetViewportLocation()
        {
            var pos = this.mapView.AutoScrollPosition;
            return new Point(-pos.X, -pos.Y);
        }

        private Point GetScrollPosition()
        {
            var pos = this.mapView.AutoScrollPosition;
            return new Point(-pos.X, -pos.Y);
        }

        private int GetAutoScrollDelta(int cursorPosition, int clientSize, bool horizontal)
        {
            var settings = MappySettings.Settings;
            var speed = horizontal
                ? settings.GetDragAutoScrollSpeedXOrDefault()
                : settings.GetDragAutoScrollSpeedYOrDefault();

            if (cursorPosition < AutoScrollEdgeThreshold)
            {
                return -speed;
            }

            if (cursorPosition > clientSize - AutoScrollEdgeThreshold)
            {
                return speed;
            }

            return 0;
        }

        private int Clamp(int value, int min, int max)
        {
            return Math.Max(min, Math.Min(max, value));
        }
    }
}
