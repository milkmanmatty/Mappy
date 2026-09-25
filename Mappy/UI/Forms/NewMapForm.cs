namespace Mappy.UI.Forms
{
    using System;
    using System.ComponentModel;
    using System.Drawing;
    using System.Windows.Forms;

    public partial class NewMapForm : Form
    {
        internal const int MaximumNewMapDimension = 1024;

        private readonly bool creatingNewMap;

        private readonly int maximumDimension;

        public NewMapForm()
            : this(
                MappySettings.Settings.GetDefaultNewMapWidthOrDefault(),
                MappySettings.Settings.GetDefaultNewMapHeightOrDefault(),
                "New Map",
                "Create",
                true)
        {
        }

        public NewMapForm(int width, int height, string title, string actionText)
            : this(width, height, title, actionText, false)
        {
        }

        private NewMapForm(int width, int height, string title, string actionText, bool creatingNewMap)
        {
            this.InitializeComponent();
            this.creatingNewMap = creatingNewMap;
            this.maximumDimension = creatingNewMap ? MaximumNewMapDimension : int.MaxValue;
            this.Text = title;
            this.button1.Text = actionText;
            this.addStandardBorderCheckBox.Visible = creatingNewMap;
            this.finalSizeLabel.Visible = creatingNewMap;
            if (!creatingNewMap)
            {
                this.ClientSize = new Size(this.ClientSize.Width, 159);
            }

            this.widthTextBox.Text = (creatingNewMap ? Math.Min(width, MaximumNewMapDimension) : width).ToString();
            this.heightTextBox.Text = (creatingNewMap ? Math.Min(height, MaximumNewMapDimension) : height).ToString();
            this.UpdateDimensionLabels();
        }

        public int MapWidth
        {
            get; private set;
        }

        public int MapHeight
        {
            get; private set;
        }

        private void FormValidating(object sender, CancelEventArgs e)
        {
            e.Cancel = !this.ValidateFields();
        }

        private bool ValidateFields()
        {
            int width;
            int height;
            if (!this.TryGetFinalMapSize(out width, out height))
            {
                return false;
            }

            this.MapWidth = width;
            this.MapHeight = height;
            return true;
        }

        private bool TryGetFinalMapSize(out int width, out int height)
        {
            width = 0;
            height = 0;

            int playableWidth;
            int playableHeight;
            if (!int.TryParse(this.widthTextBox.Text, out playableWidth)
                || !int.TryParse(this.heightTextBox.Text, out playableHeight)
                || playableWidth < 1 || playableHeight < 1
                || playableWidth > this.maximumDimension || playableHeight > this.maximumDimension)
            {
                return false;
            }

            var addBorder = this.creatingNewMap && this.addStandardBorderCheckBox.Checked;
            width = playableWidth + (addBorder ? 1 : 0);
            height = playableHeight + (addBorder ? 4 : 0);

            // Minimap rendering subtracts one tile horizontally and four vertically.
            return width >= 2 && height >= 5;
        }

        private void Button1Click(object sender, EventArgs e)
        {
            if (this.ValidateFields())
            {
                this.DialogResult = DialogResult.OK;
            }
        }

        private void WidthTextChanged(object sender, EventArgs e)
        {
            this.UpdateDimensionLabels();
        }

        private void HeightTextChanged(object sender, EventArgs e)
        {
            this.UpdateDimensionLabels();
        }

        private void AddStandardBorderCheckBoxCheckedChanged(object sender, EventArgs e)
        {
            this.UpdateDimensionLabels();
        }

        private void WidthStepClick(object sender, EventArgs e)
        {
            this.AdjustDimension(this.widthTextBox, (int)((Button)sender).Tag);
        }

        private void HeightStepClick(object sender, EventArgs e)
        {
            this.AdjustDimension(this.heightTextBox, (int)((Button)sender).Tag);
        }

        private void AdjustDimension(TextBox textBox, int step)
        {
            int current;
            if (!int.TryParse(textBox.Text, out current))
            {
                current = 1;
            }

            var next = Math.Max(1, Math.Min(this.maximumDimension, (long)current + step));
            textBox.Text = next.ToString();
            textBox.Focus();
            textBox.SelectAll();
        }

        private void UpdateDimensionLabels()
        {
            this.convertedWidthLabel.Text = this.GetConvertedDimension(this.widthTextBox.Text);
            this.convertedHeightLabel.Text = this.GetConvertedDimension(this.heightTextBox.Text);

            int width;
            int height;
            this.finalSizeLabel.Text = this.TryGetFinalMapSize(out width, out height)
                ? $@"Final map size: {width} × {height} tiles"
                : "Final map size: invalid dimensions";
        }

        private string GetConvertedDimension(string text)
        {
            int value;
            return int.TryParse(text, out value) ? $@"({value / 16.0f})" : string.Empty;
        }
    }
}
