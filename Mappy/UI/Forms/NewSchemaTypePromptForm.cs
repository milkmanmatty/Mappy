namespace Mappy.UI.Forms
{
    using System;
    using System.Drawing;
    using System.Windows.Forms;

    public sealed class NewSchemaTypePromptForm : Form
    {
        private readonly TextBox typeTextBox;
        private readonly Func<string, string> validateTrimmedName;

        public NewSchemaTypePromptForm(string defaultSchemaType, Func<string, string> validateTrimmedName = null)
        {
            this.validateTrimmedName = validateTrimmedName;
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
            var ok = new Button { Text = "OK", Location = new Point(160, 78) };
            ok.Click += (_, __) => this.TryAccept();
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

        private void TryAccept()
        {
            var trimmed = (this.typeTextBox.Text ?? string.Empty).Trim();
            if (trimmed.Length == 0)
            {
                MessageBox.Show(this, "Schema name cannot be empty.", @"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (this.validateTrimmedName != null)
            {
                var err = this.validateTrimmedName(trimmed);
                if (!string.IsNullOrEmpty(err))
                {
                    MessageBox.Show(this, err, @"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
