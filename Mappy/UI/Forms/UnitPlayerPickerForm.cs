namespace Mappy.UI.Forms
{
    using System.Drawing;
    using System.Windows.Forms;

    public sealed class UnitPlayerPickerForm : Form
    {
        private readonly ListBox listBox;

        public UnitPlayerPickerForm()
        {
            this.Text = "Player";
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.StartPosition = FormStartPosition.Manual;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;
            this.ControlBox = false;
            this.BackColor = Color.White;

            const int rowH = 20;
            this.listBox = new ListBox
            {
                BorderStyle = BorderStyle.None,
                IntegralHeight = false,
                ItemHeight = rowH,
                Location = new Point(2, 2),
                Size = new Size(136, rowH * 11),
                Font = SystemFonts.MenuFont,
            };

            for (var i = 1; i <= 11; i++)
            {
                this.listBox.Items.Add("Player " + i);
            }

            this.listBox.MouseClick += this.ListBox_MouseClick;
            this.listBox.KeyDown += this.ListBox_KeyDown;
            this.KeyPreview = true;
            this.KeyDown += this.Form_KeyDown;

            this.Controls.Add(this.listBox);
            this.ClientSize = new Size(140, (rowH * 11) + 4);
            this.MinimumSize = this.Size;
            this.MaximumSize = this.Size;
            this.Shown += (_, __) => this.listBox.Focus();
        }

        public int? SelectedPlayer { get; private set; }

        private void ListBox_MouseClick(object sender, MouseEventArgs e)
        {
            var i = this.listBox.IndexFromPoint(e.Location);
            if (i >= 0)
            {
                this.SelectedPlayer = i + 1;
                this.DialogResult = DialogResult.OK;
            }
        }

        private void ListBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                e.Handled = true;
                this.DialogResult = DialogResult.Cancel;
            }
        }

        private void Form_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                this.DialogResult = DialogResult.Cancel;
            }
        }
    }
}
