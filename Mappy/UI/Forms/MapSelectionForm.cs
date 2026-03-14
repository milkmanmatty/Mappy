namespace Mappy.UI.Forms
{
    using System.Windows.Forms;

    public partial class MapSelectionForm : Form
    {
        public MapSelectionForm()
        {
            this.InitializeComponent();
        }

        public ListBox.ObjectCollection Items => this.listBox1.Items;

        public object SelectedItem => this.listBox1.SelectedItem;

        private void ListBox1DoubleClick(object sender, System.EventArgs e)
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
