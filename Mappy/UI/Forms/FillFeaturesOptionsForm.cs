namespace Mappy.UI.Forms
{
    using System.Drawing;
    using System.Windows.Forms;

    using Mappy.Models;

    public sealed class FillFeaturesOptionsForm : Form
    {
        private readonly NumericUpDown minHeightUpDown;
        private readonly NumericUpDown maxHeightUpDown;
        private readonly NumericUpDown paddingUpDown;
        private readonly RadioButton percentageRadioButton;
        private readonly RadioButton fixedCountRadioButton;
        private readonly NumericUpDown densityPercentUpDown;
        private readonly NumericUpDown fixedCountUpDown;

        public FillFeaturesOptionsForm(FillFeaturesOptions current, int seaLevel)
        {
            this.Text = "Fill Options";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            var controlX = 108;
            var rowHeight = 30;

            var minHeightLabel = new Label
            {
                Text = "Min. Height:",
                Location = new Point(12, 18),
                AutoSize = true,
            };
            this.minHeightUpDown = new NumericUpDown
            {
                Location = new Point(controlX, 15),
                Width = 70,
                Minimum = 0,
                Maximum = 255,
                Value = current.MinHeight,
            };

            var maxHeightLabel = new Label
            {
                Text = "Max. Height:",
                Location = new Point(12, 18 + rowHeight),
                AutoSize = true,
            };
            this.maxHeightUpDown = new NumericUpDown
            {
                Location = new Point(controlX, 15 + rowHeight),
                Width = 70,
                Minimum = 0,
                Maximum = 255,
                Value = current.MaxHeight,
            };

            var paddingLabel = new Label
            {
                Text = "Padding:",
                Location = new Point(12, 18 + rowHeight * 2),
                AutoSize = true,
            };
            this.paddingUpDown = new NumericUpDown
            {
                Location = new Point(controlX, 15 + rowHeight * 2),
                Width = 70,
                Minimum = 0,
                Maximum = 512,
                Value = current.Padding,
            };
            var paddingPixelsLabel = new Label
            {
                Text = "pixels",
                Location = new Point(controlX + 76, 18 + rowHeight * 2),
                AutoSize = true,
            };

            var seaLevelLabel = new Label
            {
                Text = $"Sea Level: {seaLevel}",
                Location = new Point(12, 18 + rowHeight * 3),
                AutoSize = true,
                ForeColor = SystemColors.GrayText,
            };

            var fillCountGroupBox = new GroupBox
            {
                Text = "Fill count",
                Location = new Point(12, 18 + rowHeight * 4),
                Size = new Size(260, 88),
            };

            this.percentageRadioButton = new RadioButton
            {
                Text = "Percentage:",
                Location = new Point(12, 24),
                AutoSize = true,
                Checked = current.CountMode == FillFeaturesCountMode.Percentage,
            };
            this.densityPercentUpDown = new NumericUpDown
            {
                Location = new Point(128, 22),
                Width = 55,
                Minimum = 1,
                Maximum = 100,
                Value = current.DensityPercent,
            };
            var percentLabel = new Label
            {
                Text = "%",
                Location = new Point(188, 25),
                AutoSize = true,
            };

            this.fixedCountRadioButton = new RadioButton
            {
                Text = "Fixed amount:",
                Location = new Point(12, 52),
                AutoSize = true,
                Checked = current.CountMode == FillFeaturesCountMode.FixedCount,
            };
            this.fixedCountUpDown = new NumericUpDown
            {
                Location = new Point(128, 50),
                Width = 70,
                Minimum = 1,
                Maximum = 100000,
                Value = current.FixedCount,
            };

            this.percentageRadioButton.CheckedChanged += (_, __) => this.UpdateFillCountControls();
            this.fixedCountRadioButton.CheckedChanged += (_, __) => this.UpdateFillCountControls();

            fillCountGroupBox.Controls.Add(this.percentageRadioButton);
            fillCountGroupBox.Controls.Add(this.densityPercentUpDown);
            fillCountGroupBox.Controls.Add(percentLabel);
            fillCountGroupBox.Controls.Add(this.fixedCountRadioButton);
            fillCountGroupBox.Controls.Add(this.fixedCountUpDown);

            var okButton = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Location = new Point(100, fillCountGroupBox.Bottom + 12),
            };

            var cancelButton = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new Point(186, fillCountGroupBox.Bottom + 12),
            };

            this.AcceptButton = okButton;
            this.CancelButton = cancelButton;

            this.Controls.Add(minHeightLabel);
            this.Controls.Add(this.minHeightUpDown);
            this.Controls.Add(maxHeightLabel);
            this.Controls.Add(this.maxHeightUpDown);
            this.Controls.Add(paddingLabel);
            this.Controls.Add(this.paddingUpDown);
            this.Controls.Add(paddingPixelsLabel);
            this.Controls.Add(seaLevelLabel);
            this.Controls.Add(fillCountGroupBox);
            this.Controls.Add(okButton);
            this.Controls.Add(cancelButton);

            this.ClientSize = new Size(285, cancelButton.Bottom + 12);
            this.UpdateFillCountControls();
        }

        public int MinHeight => (int)this.minHeightUpDown.Value;

        public int MaxHeight => (int)this.maxHeightUpDown.Value;

        public new int Padding => (int)this.paddingUpDown.Value;

        public FillFeaturesCountMode CountMode =>
            this.percentageRadioButton.Checked ? FillFeaturesCountMode.Percentage : FillFeaturesCountMode.FixedCount;

        public int DensityPercent => (int)this.densityPercentUpDown.Value;

        public int FixedCount => (int)this.fixedCountUpDown.Value;

        private void UpdateFillCountControls()
        {
            var usePercentage = this.percentageRadioButton.Checked;
            this.densityPercentUpDown.Enabled = usePercentage;
            this.fixedCountUpDown.Enabled = !usePercentage;
        }
    }
}
