namespace Mappy.UI.Forms
{
    using System.IO;
    using System.Windows.Forms;
    using Mappy.Util;

    public partial class ExportCustomSectionForm : Form
    {
        public ExportCustomSectionForm()
        {
            this.InitializeComponent();
            this.GraphicPath = Path.Combine(ImgUtil.ExportDir, "Export_Graphic.png");
            this.HeightmapPath = Path.Combine(ImgUtil.ExportDir, "Export_Heightmap.png");
        }

        public string GraphicPath
        {
            get => this.textBox1.Text;
            set => this.textBox1.Text = value;
        }

        public string HeightmapPath
        {
            get => this.textBox2.Text;
            set => this.textBox2.Text = value;
        }

        private void ExportButtonClick(object sender, System.EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void CancelButtonClick(object sender, System.EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void GraphicBrowseClick(object sender, System.EventArgs e)
        {
            var dlg = new OpenFileDialog();
            dlg.Title = @"Open Graphic Image";
            dlg.Filter = @"PNG images|*.png|All files|*.*";
            var result = dlg.ShowDialog(this);
            if (result == DialogResult.OK)
            {
                this.GraphicPath = dlg.FileName;
            }
        }

        private void HeightmapBrowseClick(object sender, System.EventArgs e)
        {
            var dlg = new OpenFileDialog();
            dlg.Title = @"Open Heightmap Image";
            dlg.Filter = @"PNG images|*.png|All files|*.*";
            var result = dlg.ShowDialog(this);
            if (result == DialogResult.OK)
            {
                this.HeightmapPath = dlg.FileName;
            }
        }
    }
}