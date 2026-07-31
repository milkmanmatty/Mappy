namespace Mappy.UI.Forms
{
    using System;
    using System.Drawing;
    using System.Reactive.Linq;
    using System.Runtime.InteropServices;
    using System.Windows.Forms;

    using Mappy.Models;

    public partial class MinimapForm : Form
    {
        private const int WmSizing = 0x214;
        private const int WmszLeft = 1;
        private const int WmszRight = 2;
        private const int WmszTop = 3;
        private const int WmszTopLeft = 4;
        private const int WmszTopRight = 5;
        private const int WmszBottom = 6;
        private const int WmszBottomLeft = 7;
        private const int WmszBottomRight = 8;

        private static readonly Color[] StartPositionColors = new[]
            {
                Color.FromArgb(0, 0, 255),
                Color.FromArgb(255, 0, 0),
                Color.FromArgb(255, 255, 255),
                Color.FromArgb(0, 255, 0),
                Color.FromArgb(0, 0, 128),
                Color.FromArgb(128, 0, 255),
                Color.FromArgb(255, 255, 0),
                Color.FromArgb(0, 0, 0),
                Color.FromArgb(128, 128, 255),
                Color.FromArgb(255, 180, 140),
            };

        private IMinimapFormViewModel model;

        private Size? userClientSize;

        public MinimapForm()
        {
            this.InitializeComponent();
        }

        public void SetModel(IMinimapFormViewModel miniFormModel)
        {
            miniFormModel.PropertyAsObservable(x => x.MinimapVisible, nameof(miniFormModel.MinimapVisible))
                .Subscribe(x => this.Visible = x);

            miniFormModel.PropertyAsObservable(x => x.MinimapImage, nameof(miniFormModel.MinimapImage))
                .Select(x => x.Or(null))
                .Subscribe(this.SetMinimapImage);

            miniFormModel.PropertyAsObservable(x => x.MinimapRect, nameof(miniFormModel.MinimapRect))
                .Subscribe(x => this.minimapControl.ViewportRect = x);

            for (var i = 0; i < miniFormModel.StartPositions.Count; i++)
            {
                var i1 = i;
                var pos = miniFormModel.StartPositions[i];
                pos.Subscribe(x => x.Do(
                            y => this.minimapControl.SetMarker(i1, y, StartPositionColors[i1]),
                            () => this.minimapControl.RemoveMarker(i1)));
            }

            this.model = miniFormModel;
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (this.Visible)
            {
                this.ApplyClientSizeForImage(this.minimapControl.BackgroundImage?.Size ?? new Size(252, 252));
            }
        }

        protected override void OnResizeEnd(EventArgs e)
        {
            base.OnResizeEnd(e);
            this.userClientSize = this.ClientSize;
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WmSizing)
            {
                var rc = (Rect)Marshal.PtrToStructure(m.LParam, typeof(Rect));
                this.AdjustWindowRectToAspectRatio(ref rc, m.WParam.ToInt32());
                Marshal.StructureToPtr(rc, m.LParam, true);
            }

            base.WndProc(ref m);
        }

        private void MinimapFormFormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;

                this.model.FormCloseButtonClick();
            }
        }

        private void SetMinimapImage(Image image)
        {
            this.minimapControl.BackgroundImage = image;
            if (this.Visible && image != null && image.Width > 0 && image.Height > 0)
            {
                this.ApplyClientSizeForImage(image.Size);
            }
        }

        private void ApplyClientSizeForImage(Size imageSize)
        {
            if (imageSize.Width <= 0 || imageSize.Height <= 0)
            {
                return;
            }

            if (this.userClientSize.HasValue)
            {
                var aspectRatio = (double)imageSize.Width / imageSize.Height;
                var width = Math.Max(1, this.userClientSize.Value.Width);
                var height = Math.Max(1, (int)Math.Round(width / aspectRatio));
                this.ClientSize = new Size(width, height);
                this.userClientSize = this.ClientSize;
            }
            else
            {
                this.ClientSize = imageSize;
            }
        }

        private void MinimapControl1MouseDown(object sender, MouseEventArgs e)
        {
            this.model.MouseDown(this.minimapControl.ControlToImagePoint(e.Location));
        }

        private void MinimapControl1MouseMove(object sender, MouseEventArgs e)
        {
            this.model.MouseMove(this.minimapControl.ControlToImagePoint(e.Location));
        }

        private void MinimapControl1MouseUp(object sender, MouseEventArgs e)
        {
            this.model.MouseUp();
        }

        private void AdjustWindowRectToAspectRatio(ref Rect rc, int edge)
        {
            var imageSize = this.minimapControl.BackgroundImage?.Size ?? new Size(252, 252);
            if (imageSize.Width <= 0 || imageSize.Height <= 0)
            {
                return;
            }

            var aspectRatio = (double)imageSize.Width / imageSize.Height;
            var borderSize = this.Size - this.ClientSize;

            var clientWidth = rc.Right - rc.Left - borderSize.Width;
            var clientHeight = rc.Bottom - rc.Top - borderSize.Height;
            var resizeFromLeft =
                edge == WmszLeft ||
                edge == WmszTopLeft ||
                edge == WmszBottomLeft;
            var resizeFromTop =
                edge == WmszTop ||
                edge == WmszTopLeft ||
                edge == WmszTopRight;
            var widthDriven =
                edge != WmszTop &&
                edge != WmszBottom;

            var proposedBounds = Rectangle.FromLTRB(rc.Left, rc.Top, rc.Right, rc.Bottom);
            var workingArea = Screen.FromRectangle(proposedBounds).WorkingArea;
            var maximumWindowWidth = resizeFromLeft
                ? rc.Right - workingArea.Left
                : workingArea.Right - rc.Left;
            var maximumWindowHeight = resizeFromTop
                ? rc.Bottom - workingArea.Top
                : workingArea.Bottom - rc.Top;
            var maximumClientWidth = Math.Max(1, maximumWindowWidth - borderSize.Width);
            var maximumClientHeight = Math.Max(1, maximumWindowHeight - borderSize.Height);

            if (widthDriven)
            {
                clientWidth = Math.Max(1, Math.Min(clientWidth, maximumClientWidth));
                clientWidth = Math.Min(
                    clientWidth,
                    Math.Max(1, (int)Math.Floor(maximumClientHeight * aspectRatio)));
                clientHeight = Math.Max(1, (int)Math.Round(clientWidth / aspectRatio));
            }
            else
            {
                clientHeight = Math.Max(1, Math.Min(clientHeight, maximumClientHeight));
                clientHeight = Math.Min(
                    clientHeight,
                    Math.Max(1, (int)Math.Floor(maximumClientWidth / aspectRatio)));
                clientWidth = Math.Max(1, (int)Math.Round(clientHeight * aspectRatio));
            }

            var windowWidth = clientWidth + borderSize.Width;
            var windowHeight = clientHeight + borderSize.Height;

            if (resizeFromLeft)
            {
                rc.Left = rc.Right - windowWidth;
            }
            else
            {
                rc.Right = rc.Left + windowWidth;
            }

            if (resizeFromTop)
            {
                rc.Top = rc.Bottom - windowHeight;
            }
            else
            {
                rc.Bottom = rc.Top + windowHeight;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Rect
        {
            public int Left;

            public int Top;

            public int Right;

            public int Bottom;
        }
    }
}
