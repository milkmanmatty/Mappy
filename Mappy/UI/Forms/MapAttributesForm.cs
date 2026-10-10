namespace Mappy.UI.Forms
{
    using System;
    using System.Drawing;
    using System.Globalization;
    using System.Windows.Forms;

    public partial class MapAttributesForm : Form
    {
        private const double WindEditorToGameScale = 166.6;

        public MapAttributesForm()
        {
            this.InitializeComponent();
            this.numericUpDown1.ValueChanged += this.WindSpeedValueChanged;
            this.numericUpDown2.ValueChanged += this.WindSpeedValueChanged;
            this.Load += this.MapAttributesFormLoad;
            this.AddUseOnlyEditButton();
        }

        public event EventHandler EditUseOnlyUnitsClick;

        public string UseOnlyFileName
        {
            get => this.textBoxUseOnlyUnits.Text;
            set
            {
                this.textBoxUseOnlyUnits.Text = value ?? string.Empty;
                this.textBoxUseOnlyUnits.DataBindings["Text"]?.WriteValue();
            }
        }

        private static string FormatWindGameUnits(decimal editorUnits)
        {
            var gameUnits = (double)editorUnits / WindEditorToGameScale;
            return gameUnits.ToString("F1", CultureInfo.CurrentCulture) + " in-game";
        }

        private void AddUseOnlyEditButton()
        {
            this.textBoxUseOnlyUnits.Width = 80;
            var editButton = new Button
            {
                Text = "Edit...",
                Size = new Size(52, 23),
                Location = new Point(this.textBoxUseOnlyUnits.Right + 6, this.textBoxUseOnlyUnits.Top - 1),
                TabIndex = this.textBoxUseOnlyUnits.TabIndex,
            };
            editButton.Click += (sender, args) => this.EditUseOnlyUnitsClick?.Invoke(this, EventArgs.Empty);
            this.textBoxUseOnlyUnits.Parent.Controls.Add(editButton);
        }

        private void MapAttributesFormLoad(object sender, EventArgs e)
        {
            this.UpdateWindGameUnitLabels();
        }

        private void WindSpeedValueChanged(object sender, EventArgs e)
        {
            this.UpdateWindGameUnitLabels();
        }

        private void UpdateWindGameUnitLabels()
        {
            this.minWindInGameLabel.Text = FormatWindGameUnits(this.numericUpDown1.Value);
            this.maxWindInGameLabel.Text = FormatWindGameUnits(this.numericUpDown2.Value);
        }
    }
}
