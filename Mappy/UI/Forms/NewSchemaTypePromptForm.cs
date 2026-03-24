namespace Mappy.UI.Forms
{
    using System.Drawing;
    using System.Windows.Forms;

    public sealed class NewSchemaTypePromptForm : Form
    {
        private readonly TextBox typeTextBox;

        public NewSchemaTypePromptForm(string defaultSchemaType)
        {
            this.Text = "New schema";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            var lbl = new Label
            {
                Text = "Schema name:",
                Location = new Point(12, 18),
                AutoSize = true,
            };
            this.typeTextBox = new TextBox
            {
                Text = defaultSchemaType ?? string.Empty,
                Location = new Point(12, 42),
                Width = 320,
            };
            var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new Point(160, 78) };
            var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(252, 78) };
            this.AcceptButton = ok;
            this.CancelButton = cancel;
            this.Controls.Add(lbl);
            this.Controls.Add(this.typeTextBox);
            this.Controls.Add(ok);
            this.Controls.Add(cancel);
            this.ClientSize = new Size(344, 118);
        }

        public string SchemaTypeInput => this.typeTextBox.Text;
    }
}
