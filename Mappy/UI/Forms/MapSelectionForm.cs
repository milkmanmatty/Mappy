namespace Mappy.UI.Forms
{
    using System;
    using System.Drawing;
    using System.Windows.Forms;

    public partial class MapSelectionForm : Form
    {
        private readonly Func<string, Bitmap> previewLoader;

        public MapSelectionForm()
            : this(null)
        {
        }

        public MapSelectionForm(Func<string, Bitmap> previewLoader)
        {
            this.previewLoader = previewLoader;
            this.InitializeComponent();
            this.listBox1.SelectedIndexChanged += this.ListBox1_SelectedIndexChanged;
            this.Load += this.MapSelectionForm_Load;
            this.FormClosed += this.MapSelectionForm_FormClosed;
        }

        public ListBox.ObjectCollection Items => this.listBox1.Items;

        public object SelectedItem => this.listBox1.SelectedItem;

        private void MapSelectionForm_Load(object sender, EventArgs e)
        {
            if (this.previewLoader != null && this.listBox1.Items.Count > 0 && this.listBox1.SelectedIndex < 0)
            {
                this.listBox1.SelectedIndex = 0;
            }
        }

        private void MapSelectionForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            this.ClearPreview();
        }

        private void ClearPreview()
        {
            var previous = this.pictureBoxPreview.Image;
            this.pictureBoxPreview.Image = null;
            previous?.Dispose();
        }

        private void ListBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.ClearPreview();
            if (this.previewLoader == null || this.listBox1.SelectedItem == null)
            {
                return;
            }

            try
            {
                var name = (string)this.listBox1.SelectedItem;
                var bmp = this.previewLoader(name);
                this.pictureBoxPreview.Image = bmp;
            }
            catch
            {
                // :(
            }
        }

        private void ListBox1DoubleClick(object sender, EventArgs e)
        {
            if (this.listBox1.SelectedItem == null)
            {
                return;
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
