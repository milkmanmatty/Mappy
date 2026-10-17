namespace Mappy.UI.Forms
{
    using System;
    using System.Drawing;
    using System.Globalization;
    using System.Windows.Forms;

    using Mappy.Models;

    public partial class MapAttributesForm : Form
    {
        private const double WindEditorToGameScale = 166.6;

        private bool schemaComboProgrammaticChange;

        public MapAttributesForm()
        {
            this.InitializeComponent();
            this.comboSchema.SelectedIndexChanged += this.SchemaComboSelectedIndexChanged;
            this.schemaAttributesBindingSource.PositionChanged += this.SchemaPositionChanged;
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

        public void SetInitialSchemaIndex(int initialSchemaIndex)
        {
            var result = this.mapAttributesResultBindingSource.Current as MapAttributesResult;
            if (result == null)
            {
                return;
            }

            this.schemaComboProgrammaticChange = true;
            try
            {
                this.comboSchema.Items.Clear();

                for (var i = 0; i < result.Schemas.Count; i++)
                {
                    var label = "Schema " + i + ": " + result.Schemas[i].SchemaType;
                    this.comboSchema.Items.Add(label);
                }

                if (result.Schemas.Count == 0)
                {
                    return;
                }

                var index = Math.Min(Math.Max(0, initialSchemaIndex), result.Schemas.Count - 1);
                this.schemaAttributesBindingSource.Position = index;
                this.comboSchema.SelectedIndex = index;
            }
            finally
            {
                this.schemaComboProgrammaticChange = false;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (this.DialogResult == DialogResult.OK)
            {
                this.Validate();
                this.schemaAttributesBindingSource.EndEdit();
                this.mapAttributesResultBindingSource.EndEdit();
            }

            base.OnFormClosing(e);
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

        private void SchemaComboSelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.schemaComboProgrammaticChange)
            {
                return;
            }

            var combo = (ComboBox)sender;
            if (combo.SelectedIndex < 0)
            {
                return;
            }

            this.ApplySchemaIndex(combo.SelectedIndex);
        }

        private void SchemaPositionChanged(object sender, EventArgs e)
        {
            if (this.schemaComboProgrammaticChange)
            {
                return;
            }

            this.ApplySchemaIndex(this.schemaAttributesBindingSource.Position);
        }

        private void ApplySchemaIndex(int index)
        {
            this.Validate();
            this.schemaAttributesBindingSource.EndEdit();

            this.schemaComboProgrammaticChange = true;
            try
            {
                if (this.schemaAttributesBindingSource.Position != index)
                {
                    this.schemaAttributesBindingSource.Position = index;
                }

                if (this.comboSchema.SelectedIndex != index
                    && index >= 0
                    && index < this.comboSchema.Items.Count)
                {
                    this.comboSchema.SelectedIndex = index;
                }
            }
            finally
            {
                this.schemaComboProgrammaticChange = false;
            }
        }
    }
}
