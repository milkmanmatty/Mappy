namespace Mappy.UI.Forms
{
    using System;
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
        }

        private static string FormatWindGameUnits(decimal editorUnits)
        {
            var gameUnits = (double)editorUnits / WindEditorToGameScale;
            return gameUnits.ToString("F1", CultureInfo.CurrentCulture) + " in-game";
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
