namespace Mappy.UI.Forms
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using System.Windows.Forms;

    public sealed class TilesetsForm : Form
    {
        private readonly CheckBox selectAllCheckBox;

        private readonly List<CheckBox> worldCheckBoxes = new List<CheckBox>();

        private readonly IReadOnlyList<string> worldOrder;

        private bool syncingChecks;

        public TilesetsForm(IEnumerable<string> allWorlds, IEnumerable<string> currentFilter)
        {
            this.Text = "Tilesets";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            this.worldOrder = allWorlds.Distinct(StringComparer.InvariantCultureIgnoreCase)
                .OrderBy(x => x, StringComparer.InvariantCultureIgnoreCase)
                .ToList();

            var filterSet = currentFilter == null ? null : new HashSet<string>(currentFilter, StringComparer.InvariantCultureIgnoreCase);

            var scrollPanel = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(396, 320),
                AutoScroll = true,
                BorderStyle = BorderStyle.FixedSingle,
            };

            int y = 12;
            this.syncingChecks = true;
            try
            {
                this.selectAllCheckBox = new CheckBox
                {
                    Text = "Select all",
                    Location = new Point(12, y),
                    AutoSize = true,
                };

                bool allChecked = filterSet == null || this.worldOrder.All(w => filterSet.Contains(w));
                this.selectAllCheckBox.Checked = allChecked;
                scrollPanel.Controls.Add(this.selectAllCheckBox);
                y += 28;

                foreach (var world in this.worldOrder)
                {
                    var isChecked = filterSet == null || filterSet.Contains(world);
                    var cb = new CheckBox
                    {
                        Text = world,
                        Location = new Point(24, y),
                        Width = 340,
                        Checked = isChecked,
                        AutoEllipsis = true,
                    };
                    scrollPanel.Controls.Add(cb);
                    this.worldCheckBoxes.Add(cb);
                    y += 24;
                }
            }
            finally
            {
                this.syncingChecks = false;
            }

            this.selectAllCheckBox.CheckedChanged += this.SelectAllCheckBox_CheckedChanged;
            foreach (var cb in this.worldCheckBoxes)
            {
                cb.CheckedChanged += this.WorldCheckBox_CheckedChanged;
            }

            var okButton = new Button
            {
                Text = "OK",
                Location = new Point(212, 332),
                DialogResult = DialogResult.None,
            };
            okButton.Click += (_, __) => this.TryAccept();

            var cancelButton = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new Point(293, 332),
            };

            this.AcceptButton = okButton;
            this.CancelButton = cancelButton;
            this.Controls.Add(scrollPanel);
            this.Controls.Add(okButton);
            this.Controls.Add(cancelButton);
            this.ClientSize = new Size(400, 375);
        }

        public IReadOnlyList<string> SelectedWorldsAfterOk { get; private set; }

        private void SelectAllCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (this.syncingChecks)
            {
                return;
            }

            this.syncingChecks = true;
            try
            {
                bool on = this.selectAllCheckBox.Checked;
                foreach (var cb in this.worldCheckBoxes)
                {
                    cb.Checked = on;
                }
            }
            finally
            {
                this.syncingChecks = false;
            }
        }

        private void WorldCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (this.syncingChecks)
            {
                return;
            }

            bool all = this.worldCheckBoxes.All(cb => cb.Checked);
            if (this.selectAllCheckBox.Checked == all)
            {
                return;
            }

            this.syncingChecks = true;
            try
            {
                this.selectAllCheckBox.Checked = all;
            }
            finally
            {
                this.syncingChecks = false;
            }
        }

        private void TryAccept()
        {
            var chosen = this.worldCheckBoxes.Where(cb => cb.Checked).Select(cb => cb.Text).ToList();
            if (chosen.Count == 0)
            {
                MessageBox.Show(
                    this,
                    "Select at least one tileset.",
                    @"Tilesets",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            this.SelectedWorldsAfterOk = chosen.Count == this.worldOrder.Count
                ? null
                : chosen;

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
