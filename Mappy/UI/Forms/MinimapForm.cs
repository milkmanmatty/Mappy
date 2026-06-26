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
                .Subscribe(x => this.minimapControl.BackgroundImage = x);

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
                var naturalSize = this.minimapControl.BackgroundImage?.Size ?? new Size(252, 252);
                this.ClientSize = naturalSize;
            }
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

            switch (edge)
            {
                case WmszLeft:
                case WmszRight:
                case WmszTopLeft:
                case WmszTopRight:
                case WmszBottomLeft:
                case WmszBottomRight:
                    clientHeight = (int)Math.Round(clientWidth / aspectRatio);
                    break;
                default:
                    clientWidth = (int)Math.Round(clientHeight * aspectRatio);
                    break;
            }

            var windowWidth = clientWidth + borderSize.Width;
            var windowHeight = clientHeight + borderSize.Height;

            switch (edge)
            {
                case WmszLeft:
                case WmszTopLeft:
                case WmszBottomLeft:
                    rc.Left = rc.Right - windowWidth;
                    break;
                default:
                    rc.Right = rc.Left + windowWidth;
                    break;
            }

            switch (edge)
            {
                case WmszTop:
                case WmszTopLeft:
                case WmszTopRight:
                    rc.Top = rc.Bottom - windowHeight;
                    break;
                default:
                    rc.Bottom = rc.Top + windowHeight;
                    break;
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