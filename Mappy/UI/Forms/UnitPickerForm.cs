namespace Mappy.UI.Forms
{
    using System;
    using System.Drawing;
    using System.Windows.Forms;

    using Mappy.Services;

    public sealed class UnitPickerForm : Form
    {
        private readonly UnitCatalogService catalog;

        private readonly TextBox searchBox;

        private readonly ListBox listBox;

        private readonly Button okButton;

        public UnitPickerForm(UnitCatalogService catalog)
        {
            this.catalog = catalog;

            this.Text = "Choose unit";
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimizeBox = false;
            this.MaximizeBox = false;
            this.ShowInTaskbar = false;
            this.ClientSize = new Size(360, 420);
            this.MinimumSize = new Size(280, 320);

            this.searchBox = new TextBox
            {
                Dock = DockStyle.Fill,
            };
            this.searchBox.TextChanged += (s, e) => this.RefreshList();

            var clear = new Button
            {
                Text = "Clear",
                AutoSize = true,
                Anchor = AnchorStyles.Left | AnchorStyles.Top,
                Margin = new Padding(4, 0, 0, 0),
            };
            clear.Click += (s, e) => this.searchBox.Clear();

            var searchRow = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 2,
                Padding = new Padding(8, 8, 8, 4),
            };
            searchRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            searchRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            searchRow.Controls.Add(this.searchBox, 0, 0);
            searchRow.Controls.Add(clear, 1, 0);

            this.listBox = new ListBox
            {
                Dock = DockStyle.Fill,
                IntegralHeight = false,
            };
            this.listBox.DoubleClick += (s, e) => this.AcceptSelection();
            this.listBox.SelectedIndexChanged += (s, e) => this.UpdateOkEnabled();

            this.okButton = new Button
            {
                Text = "OK",
                Width = 75,
                Enabled = false,
            };
            this.okButton.Click += (s, e) => this.AcceptSelection();

            var cancel = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Width = 75,
            };

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                Padding = new Padding(8),
            };
            buttons.Controls.Add(cancel);
            buttons.Controls.Add(this.okButton);

            this.AcceptButton = this.okButton;
            this.CancelButton = cancel;
            this.Controls.Add(this.listBox);
            this.Controls.Add(buttons);
            this.Controls.Add(searchRow);

            this.RefreshList();
            this.Shown += (s, e) => this.searchBox.Focus();
        }

        public string SelectedInternalName { get; private set; }

        private void AcceptSelection()
        {
            if (!(this.listBox.SelectedItem is UnitPickerItem item) || string.IsNullOrEmpty(item.InternalName))
            {
                return;
            }

            this.SelectedInternalName = item.InternalName;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void UpdateOkEnabled()
        {
            this.okButton.Enabled = this.listBox.SelectedItem is UnitPickerItem;
        }

        private void RefreshList()
        {
            var query = (this.searchBox.Text ?? string.Empty).Trim();
            string previous = null;
            if (this.listBox.SelectedItem is UnitPickerItem selected)
            {
                previous = selected.InternalName;
            }

            this.listBox.BeginUpdate();
            this.listBox.Items.Clear();
            if (this.catalog != null)
            {
                foreach (var name in this.catalog.EnumerateSorted())
                {
                    if (!this.Matches(name, query))
                    {
                        continue;
                    }

                    var item = new UnitPickerItem(name, this.catalog.FormatUnitPickerLabel(name));
                    this.listBox.Items.Add(item);
                    if (previous != null && string.Equals(previous, name, StringComparison.OrdinalIgnoreCase))
                    {
                        this.listBox.SelectedItem = item;
                    }
                }
            }

            this.listBox.EndUpdate();
            this.UpdateOkEnabled();
        }

        private bool Matches(string internalName, string query)
        {
            if (string.IsNullOrEmpty(query))
            {
                return true;
            }

            if (internalName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            var searchable = this.catalog.GetUnitPickerSearchableText(internalName);
            if (!string.IsNullOrEmpty(searchable)
                && searchable.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            var label = this.catalog.FormatUnitPickerLabel(internalName);
            return !string.IsNullOrEmpty(label)
                && label.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private sealed class UnitPickerItem
        {
            public UnitPickerItem(string internalName, string label)
            {
                this.InternalName = internalName;
                this.Label = string.IsNullOrEmpty(label) ? internalName : label;
            }

            public string InternalName { get; }

            public string Label { get; }

            public override string ToString()
            {
                return this.Label;
            }
        }
    }
}
