namespace Mappy.UI.Forms
{
    using System.Collections.Generic;
    using System.Drawing;
    using System.Windows.Forms;
    using Mappy.Models.Enums;

    public sealed class ExportMapImageOptionsForm : Form
    {
        private readonly CheckBox includeSectionsCheckBox;
        private readonly CheckBox playableAreaOnlyCheckBox;
        private readonly ComboBox featuresComboBox;
        private readonly ComboBox unitsComboBox;

        public ExportMapImageOptionsForm(IReadOnlyList<string> schemaNames)
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

            var unitsLabel = new Label
            {
                Text = "Include units:",
                AutoSize = true,
                Location = new Point(14, y + 3),
            };
            this.Controls.Add(unitsLabel);

            this.unitsComboBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(130, y),
                Width = 160,
            };
            this.unitsComboBox.Items.Add("None");
            if (schemaNames != null)
            {
                foreach (var name in schemaNames)
                {
                    this.unitsComboBox.Items.Add(name);
                }
            }

            this.unitsComboBox.SelectedIndex = 0;
            this.Controls.Add(this.unitsComboBox);
            y += 32;

            this.playableAreaOnlyCheckBox = new CheckBox
            {
                Text = "Playable area only",
                Checked = false,
                AutoSize = true,
                Location = new Point(14, y),
            };
            this.Controls.Add(this.playableAreaOnlyCheckBox);
            y += 22;

            var playableAreaHint = new Label
            {
                Text = "Checked to look pretty, unchecked to edit and re-import later.",
                AutoSize = false,
                Location = new Point(30, y),
                Size = new Size(320, 32),
            };
            this.Controls.Add(playableAreaHint);
            y += 36;

            var ok = new Button
            {
                Text = "Export...",
                DialogResult = DialogResult.OK,
                Location = new Point(170, y),
                Width = 80,
            };
            var cancel = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new Point(260, y),
                Width = 80,
            };
            this.AcceptButton = ok;
            this.CancelButton = cancel;
            this.Controls.Add(ok);
            this.Controls.Add(cancel);

            this.ClientSize = new Size(360, y + 36);
        }

        public bool IncludeSections => this.includeSectionsCheckBox.Checked;

        public bool PlayableAreaOnly => this.playableAreaOnlyCheckBox.Checked;

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

        /// <summary>
        /// Returns the schema index selected for unit export, or null if "None" was chosen.
        /// </summary>
        public int? UnitSchemaIndex
        {
            get
            {
                var idx = this.unitsComboBox.SelectedIndex;
                return idx > 0 ? idx - 1 : (int?)null;
            }
        }
    }
}
