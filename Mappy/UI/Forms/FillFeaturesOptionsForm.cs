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

            var okButton = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Location = new Point(100, 18 + rowHeight * 4),
            };

            var cancelButton = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new Point(186, 18 + rowHeight * 4),
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
            this.Controls.Add(okButton);
            this.Controls.Add(cancelButton);

            this.ClientSize = new Size(285, 18 + rowHeight * 5 + 10);
        }

        public int MinHeight => (int)this.minHeightUpDown.Value;

        public int MaxHeight => (int)this.maxHeightUpDown.Value;

        public new int Padding => (int)this.paddingUpDown.Value;
    }
}