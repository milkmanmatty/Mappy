namespace Mappy.UI.Forms
{
    using System.Drawing;
    using System.Windows.Forms;
    using Mappy.Models.Enums;

    public sealed class ExportMapImageOptionsForm : Form
    {
        private readonly CheckBox includeSectionsCheckBox;
        private readonly ComboBox featuresComboBox;
        private readonly CheckBox includeUnitsCheckBox;

        public ExportMapImageOptionsForm()
        {
            this.Text = "Export Map Image";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            int y = 14;

            this.includeSectionsCheckBox = new CheckBox
            {
                Text = "Include sections",
                Checked = true,
                AutoSize = true,
                Location = new Point(14, y),
            };
            this.Controls.Add(this.includeSectionsCheckBox);
            y += 28;

            var featuresLabel = new Label
            {
                Text = "Include features:",
                AutoSize = true,
                Location = new Point(14, y + 3),
            };
            this.Controls.Add(featuresLabel);

            this.featuresComboBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(130, y),
                Width = 160,
            };
            this.featuresComboBox.Items.Add("None");
            this.featuresComboBox.Items.Add("All");
            this.featuresComboBox.Items.Add("Metal deposits only");
            this.featuresComboBox.SelectedIndex = 0;
            this.Controls.Add(this.featuresComboBox);
            y += 30;

            this.includeUnitsCheckBox = new CheckBox
            {
                Text = "Include units",
                Checked = false,
                AutoSize = true,
                Location = new Point(14, y),
            };
            this.Controls.Add(this.includeUnitsCheckBox);
            y += 36;

            var ok = new Button
            {
                Text = "Export...",
                DialogResult = DialogResult.OK,
                Location = new Point(100, y),
                Width = 80,
            };
            var cancel = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new Point(190, y),
                Width = 80,
            };
            this.AcceptButton = ok;
            this.CancelButton = cancel;
            this.Controls.Add(ok);
            this.Controls.Add(cancel);

            this.ClientSize = new Size(310, y + 36);
        }

        public bool IncludeSections => this.includeSectionsCheckBox.Checked;

        public FeatureExportMode FeatureMode
        {
            get
            {
                switch (this.featuresComboBox.SelectedIndex)
                {
                    case 1: return FeatureExportMode.All;
                    case 2: return FeatureExportMode.MetalDepositsOnly;
                    default: return FeatureExportMode.None;
                }
            }
        }

        public bool IncludeUnits => this.includeUnitsCheckBox.Checked;
    }
}
