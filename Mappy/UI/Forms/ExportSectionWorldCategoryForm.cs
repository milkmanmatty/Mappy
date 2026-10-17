namespace Mappy.UI.Forms
{
    using System;
    using System.Drawing;
    using System.Windows.Forms;

    using Mappy.IO;

    public class ExportSectionWorldCategoryForm : Form
    {
        private readonly TextBox worldTextBox;

        private readonly TextBox categoryTextBox;

        public ExportSectionWorldCategoryForm()
        {
            this.Text = "Export Sections";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimizeBox = false;
            this.MaximizeBox = false;
            this.ShowInTaskbar = false;
            this.ClientSize = new Size(320, 132);

            var worldLabel = new Label
            {
                AutoSize = true,
                Location = new Point(12, 14),
                Text = "World",
            };

            this.worldTextBox = new TextBox
            {
                Location = new Point(80, 11),
                Width = 228,
                Text = "Custom",
            };

            var categoryLabel = new Label
            {
                AutoSize = true,
                Location = new Point(12, 44),
                Text = "Category",
            };

            this.categoryTextBox = new TextBox
            {
                Location = new Point(80, 41),
                Width = 228,
                Text = "Amendments",
            };

            var okButton = new Button
            {
                Text = "OK",
                Location = new Point(152, 96),
                Size = new Size(75, 23),
            };
            okButton.Click += this.OkClick;

            var cancelButton = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new Point(233, 96),
                Size = new Size(75, 23),
            };

            this.Controls.Add(worldLabel);
            this.Controls.Add(this.worldTextBox);
            this.Controls.Add(categoryLabel);
            this.Controls.Add(this.categoryTextBox);
            this.Controls.Add(okButton);
            this.Controls.Add(cancelButton);
            this.AcceptButton = okButton;
            this.CancelButton = cancelButton;
        }

        public string World { get; private set; }

        public string Category { get; private set; }

        private void OkClick(object sender, EventArgs e)
        {
            var world = this.worldTextBox.Text.Trim();
            var category = this.categoryTextBox.Text.Trim();
            if (!SectionArchiveSaver.IsValidPathSegment(world) || !SectionArchiveSaver.IsValidPathSegment(category))
            {
                MessageBox.Show(
                    this,
                    "World and category must be non-empty names without path characters.",
                    "Export Sections",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            this.World = world;
            this.Category = category;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
