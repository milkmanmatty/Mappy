namespace Mappy.UI.Forms
{
    using System.Windows.Forms;

    public partial class InfoForm : Form
    {
        public InfoForm()
        {
            this.InitializeComponent();

            this.richTextBox1.Text = Properties.Resources.InfoText;
            this.label1.Text = string.Format(
                @"{0} v{1}",
                Application.ProductName,
                Application.ProductVersion);
        }
    }
}
