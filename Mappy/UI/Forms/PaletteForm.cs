namespace Mappy.UI.Forms
{
    using System;
    using System.Drawing;
    using System.Windows.Forms;

    public partial class PaletteForm : Form
    {
        private bool hasBeenShown;

        public PaletteForm()
        {
            this.InitializeComponent();
        }

        public event EventHandler DockRequested;

        public event KeyEventHandler PaletteKeyDown;

        public bool TryRestoreBoundsFromSettings()
        {
            var settings = MappySettings.Settings;
            if (!settings.HasSidebarFloatBounds)
            {
                return false;
            }

            var width = settings.SidebarFloatSizeWidth > 0 ? settings.SidebarFloatSizeWidth : this.Width;
            var height = settings.SidebarFloatSizeHeight > 0 ? settings.SidebarFloatSizeHeight : this.Height;
            var savedBounds = new Rectangle(
                settings.SidebarFloatLocationX,
                settings.SidebarFloatLocationY,
                width,
                height);

            if (!IsBoundsOnAnyScreen(savedBounds))
            {
                return false;
            }

            this.StartPosition = FormStartPosition.Manual;
            this.Bounds = savedBounds;
            return true;
        }

        public void SaveBoundsToSettings()
        {
            if (!this.hasBeenShown)
            {
                return;
            }

            var settings = MappySettings.Settings;
            settings.SidebarFloatLocationX = this.Location.X;
            settings.SidebarFloatLocationY = this.Location.Y;
            settings.SidebarFloatSizeWidth = this.Width;
            settings.SidebarFloatSizeHeight = this.Height;
            settings.HasSidebarFloatBounds = true;
            MappySettings.SaveSettings();
        }

        public void PlaceNearOwner(Form owner, int preferredWidth)
        {
            if (owner == null)
            {
                return;
            }

            this.StartPosition = FormStartPosition.Manual;
            var width = Math.Max(200, preferredWidth);
            var height = Math.Max(300, owner.ClientSize.Height);
            this.Size = new Size(width, height);

            var location = owner.PointToScreen(new Point(0, 0));
            var proposed = new Rectangle(location.X, location.Y, width, height);
            if (!IsBoundsOnAnyScreen(proposed))
            {
                proposed = new Rectangle(owner.Left, owner.Top, width, height);
            }

            this.Location = proposed.Location;
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (this.Visible)
            {
                this.hasBeenShown = true;
            }
        }

        protected override void OnResizeEnd(EventArgs e)
        {
            base.OnResizeEnd(e);
            this.SaveBoundsToSettings();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (base.ProcessCmdKey(ref msg, keyData))
            {
                return true;
            }

            var main = this.Owner as MainForm;
            return main != null && main.ProcessPaletteCmdKey(ref msg, keyData);
        }

        private static bool IsBoundsOnAnyScreen(Rectangle bounds)
        {
            foreach (Screen screen in Screen.AllScreens)
            {
                if (screen.WorkingArea.IntersectsWith(bounds))
                {
                    return true;
                }
            }

            return false;
        }

        private void PaletteFormFormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.SaveBoundsToSettings();
                this.DockRequested?.Invoke(this, EventArgs.Empty);
            }
        }

        private void PaletteForm_KeyDown(object sender, KeyEventArgs e)
        {
            this.PaletteKeyDown?.Invoke(this, e);
        }
    }
}
