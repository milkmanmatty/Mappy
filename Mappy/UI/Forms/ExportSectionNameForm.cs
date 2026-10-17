namespace Mappy.UI.Forms
{
    using System;
    using System.Drawing;
    using System.Windows.Forms;

    using Mappy.IO;

    public class ExportSectionNameForm : Form
    {
        private readonly TextBox nameTextBox;

        public ExportSectionNameForm(string currentName)
        {
            this.Text = "Section name";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimizeBox = false;
            this.MaximizeBox = false;
            this.ShowInTaskbar = false;
            this.ClientSize = new Size(320, 96);

            var label = new Label
            {
                AutoSize = true,
                Location = new Point(12, 14),
                Text = "SCT file name",
            };

            this.nameTextBox = new TextBox
            {
                Location = new Point(12, 34),
                Width = 296,
                Text = currentName ?? string.Empty,
            };

            var okButton = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.None,
                Location = new Point(152, 64),
                Size = new Size(75, 23),
            };
            okButton.Click += this.OkClick;

            var cancelButton = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new Point(233, 64),
                Size = new Size(75, 23),
            };

            this.Controls.Add(label);
            this.Controls.Add(this.nameTextBox);
            this.Controls.Add(okButton);
            this.Controls.Add(cancelButton);
            this.AcceptButton = okButton;
            this.CancelButton = cancelButton;
        }

        public string FileName { get; private set; }

        private void OkClick(object sender, EventArgs e)
        {
            var name = SectionArchiveSaver.NormalizeFileName(this.nameTextBox.Text);
            if (name == null)
            {
                MessageBox.Show(
                    this,
                    "Enter a file name without path characters. It will be saved as a .sct file.",
                    "Section name",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            this.FileName = name;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
