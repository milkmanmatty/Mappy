namespace Mappy.UI.Forms
{
    using System.Drawing;
    using System.Windows.Forms;

    public sealed class UnitPlayerPromptForm : Form
    {
        private readonly NumericUpDown playerUpDown;

        public UnitPlayerPromptForm(int defaultPlayer)
        {
            this.Text = "Player";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            var lbl = new Label
            {
                Text = "Player number:",
                Location = new Point(12, 18),
                AutoSize = true,
            };
            this.playerUpDown = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 10,
                Value = defaultPlayer,
                Location = new Point(120, 14),
                Width = 60,
            };
            var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new Point(60, 50) };
            var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(150, 50) };
            this.AcceptButton = ok;
            this.CancelButton = cancel;
            this.Controls.Add(lbl);
            this.Controls.Add(this.playerUpDown);
            this.Controls.Add(ok);
            this.Controls.Add(cancel);
            this.ClientSize = new Size(260, 95);
        }

        public int PlayerNumber => (int)this.playerUpDown.Value;
    }
}