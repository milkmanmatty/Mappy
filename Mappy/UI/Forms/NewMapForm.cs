namespace Mappy.UI.Forms
{
    using System;
    using System.ComponentModel;
    using System.Drawing;
    using System.Windows.Forms;

    using Mappy.Models;

    public partial class NewMapForm : Form
    {
        internal const int MaximumNewMapDimension = 1024;

        private readonly bool creatingNewMap;

        private readonly int maximumDimension;

        private readonly Size originalSize;

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
            this.originalSize = new Size(width, height);
            this.Text = title;
            this.button1.Text = actionText;
            this.addStandardBorderCheckBox.Visible = creatingNewMap;
            this.finalSizeLabel.Visible = creatingNewMap;
            this.resizeAnchorLabel.Visible = !creatingNewMap;
            this.resizeAnchorGrid.Visible = !creatingNewMap;
            this.resizeChangeLabel.Visible = !creatingNewMap;
            this.moveStandardBorderCheckBox.Visible = !creatingNewMap;
            this.moveStandardBorderHelpLabel.Visible = !creatingNewMap;
            this.moveStandardBorderCheckBox.Enabled = width > ResizeMapOptions.StandardBorderWidth
                && height > ResizeMapOptions.StandardBorderHeight;
            if (!creatingNewMap)
            {
                this.ClientSize = new Size(this.ClientSize.Width, 402);
            }

            this.widthTextBox.Text = (creatingNewMap ? Math.Min(width, MaximumNewMapDimension) : width).ToString();
            this.heightTextBox.Text = (creatingNewMap ? Math.Min(height, MaximumNewMapDimension) : height).ToString();
            if (!creatingNewMap)
            {
                this.InitializeResizeAnchorGrid();
            }

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

        public Point ResizeAnchor { get; private set; }

        public bool MoveStandardBorder => !this.creatingNewMap && this.moveStandardBorderCheckBox.Checked;

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

        private void MoveStandardBorderCheckBoxCheckedChanged(object sender, EventArgs e)
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
            var valid = this.TryGetFinalMapSize(out width, out height);
            this.finalSizeLabel.Text = valid
                ? $@"Final map size: {width} × {height} tiles"
                : "Final map size: invalid dimensions";

            if (!this.creatingNewMap)
            {
                this.resizeAnchorLabel.Text = this.MoveStandardBorder ? "Anchor playable area:" : "Anchor existing map:";
                this.resizeChangeLabel.Text = valid
                    ? this.GetResizeChangeText(new Size(width, height))
                    : "Enter valid dimensions to see the change.";
            }
        }

        private void InitializeResizeAnchorGrid()
        {
            var symbols = new[] { "↖", "↑", "↗", "←", "●", "→", "↙", "↓", "↘" };
            var names = new[]
            {
                "top left", "top", "top right",
                "left", "centre", "right",
                "bottom left", "bottom", "bottom right",
            };

            for (var row = 0; row < 3; row++)
            {
                for (var column = 0; column < 3; column++)
                {
                    var index = (row * 3) + column;
                    var button = new RadioButton
                    {
                        Appearance = Appearance.Button,
                        Dock = DockStyle.Fill,
                        FlatStyle = FlatStyle.Flat,
                        Font = new Font("Segoe UI Symbol", 12F),
                        Margin = new Padding(1),
                        Tag = new Point(column, row),
                        Text = symbols[index],
                        TextAlign = ContentAlignment.MiddleCenter,
                        AccessibleName = $@"Anchor existing map at {names[index]}",
                    };
                    button.FlatAppearance.CheckedBackColor = SystemColors.Highlight;
                    button.CheckedChanged += this.ResizeAnchorCheckedChanged;
                    this.resizeAnchorGrid.Controls.Add(button, column, row);
                    button.Checked = column == 0 && row == 0;
                }
            }
        }

        private void ResizeAnchorCheckedChanged(object sender, EventArgs e)
        {
            var selected = (RadioButton)sender;
            if (!selected.Checked)
            {
                return;
            }

            this.ResizeAnchor = (Point)selected.Tag;
            foreach (RadioButton button in this.resizeAnchorGrid.Controls)
            {
                button.ForeColor = button.Checked ? SystemColors.HighlightText : SystemColors.ControlText;
            }

            this.UpdateDimensionLabels();
        }

        private string GetResizeChangeText(Size newSize)
        {
            var options = new ResizeMapOptions(newSize, this.ResizeAnchor);
            var offset = options.GetTileOffset(this.originalSize);
            var right = newSize.Width - this.originalSize.Width - offset.X;
            var bottom = newSize.Height - this.originalSize.Height - offset.Y;
            return $@"Tiles added (+) / cropped (−):
Left {FormatChange(offset.X)}, right {FormatChange(right)}
Top {FormatChange(offset.Y)}, bottom {FormatChange(bottom)}";
        }

        private static string FormatChange(int value)
        {
            return value > 0 ? $@"+{value}" : value.ToString();
        }

        private string GetConvertedDimension(string text)
        {
            int value;
            return int.TryParse(text, out value) ? $@"({value / 16.0f})" : string.Empty;
        }
    }
}
