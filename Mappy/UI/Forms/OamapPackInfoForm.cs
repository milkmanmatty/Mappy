namespace Mappy.UI.Forms
{
    using System.Drawing;
    using System.Windows.Forms;

    using Mappy.Models;

    public sealed class OamapPackInfoForm : Form
    {
        private readonly TextBox packIdBox;
        private readonly TextBox packNameBox;
        private readonly TextBox versionBox;
        private readonly TextBox descriptionBox;
        private readonly TextBox authorNameBox;
        private readonly TextBox authorEmailBox;
        private readonly NumericUpDown revisionUpDown;
        private readonly TextBox packagerBox;
        private readonly TextBox stemBox;
        private readonly TextBox mapNameBox;
        private readonly TextBox mapDescriptionBox;
        private readonly NumericUpDown playersUpDown;

        public OamapPackInfoForm(OamapPackInfo defaults)
        {
            defaults = defaults ?? new OamapPackInfo();
            this.Text = "Open Annihilation map pack";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;
            this.AutoScroll = true;
            this.ClientSize = new Size(440, 560);

            var y = 12;
            var note = new Label
            {
                Text = "Standard Total Annihilation features stay out of the pack. They are loaded from the base game.",
                Location = new Point(12, y),
                Size = new Size(416, 32),
            };
            this.Controls.Add(note);
            y += 40;

            this.packIdBox = this.AddText(ref y, "Pack id", defaults.PackId);
            this.packNameBox = this.AddText(ref y, "Pack name", defaults.PackName);
            this.versionBox = this.AddText(ref y, "Version", string.IsNullOrEmpty(defaults.Version) ? "1.0" : defaults.Version);
            this.descriptionBox = this.AddText(ref y, "Description (optional)", defaults.Description);
            this.authorNameBox = this.AddText(ref y, "Author name", string.IsNullOrEmpty(defaults.AuthorName) ? "unknown" : defaults.AuthorName);
            this.authorEmailBox = this.AddText(ref y, "Author email (optional)", defaults.AuthorEmail);
            this.revisionUpDown = this.AddNumber(ref y, "Packaging revision", 1, 65535, defaults.PackagingRevision < 1 ? 1 : defaults.PackagingRevision);
            this.packagerBox = this.AddText(ref y, "Packager", defaults.Packager);
            this.stemBox = this.AddText(ref y, "Map stem", defaults.Stem);
            this.mapNameBox = this.AddText(ref y, "Map title", defaults.MapName);
            this.mapDescriptionBox = this.AddText(ref y, "Map description (optional)", defaults.MapDescription);
            var players = defaults.Players < 2 || defaults.Players > 10 ? 2 : defaults.Players;
            this.playersUpDown = this.AddNumber(ref y, "Players", 2, 10, players);

            var ok = new Button
            {
                Text = "OK",
                Location = new Point(256, y + 8),
                Size = new Size(80, 26),
            };
            ok.Click += (_, __) => this.TryAccept();
            var cancel = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new Point(344, y + 8),
                Size = new Size(80, 26),
            };
            this.AcceptButton = ok;
            this.CancelButton = cancel;
            this.Controls.Add(ok);
            this.Controls.Add(cancel);
            this.ClientSize = new Size(440, y + 46);
        }

        public OamapPackInfo Result { get; private set; }

        private TextBox AddText(ref int y, string label, string value)
        {
            this.Controls.Add(new Label
            {
                Text = label,
                Location = new Point(12, y),
                AutoSize = true,
            });
            var box = new TextBox
            {
                Text = value ?? string.Empty,
                Location = new Point(12, y + 16),
                Width = 412,
            };
            this.Controls.Add(box);
            y += 42;
            return box;
        }

        private NumericUpDown AddNumber(ref int y, string label, int min, int max, int value)
        {
            this.Controls.Add(new Label
            {
                Text = label,
                Location = new Point(12, y),
                AutoSize = true,
            });
            if (value < min)
            {
                value = min;
            }

            if (value > max)
            {
                value = max;
            }

            var box = new NumericUpDown
            {
                Minimum = min,
                Maximum = max,
                Value = value,
                Location = new Point(12, y + 16),
                Width = 120,
            };
            this.Controls.Add(box);
            y += 42;
            return box;
        }

        private void TryAccept()
        {
            var info = new OamapPackInfo
            {
                PackId = this.packIdBox.Text.Trim(),
                PackName = this.packNameBox.Text.Trim(),
                Version = this.versionBox.Text.Trim(),
                Description = this.descriptionBox.Text.Trim(),
                AuthorName = this.authorNameBox.Text.Trim(),
                AuthorEmail = this.authorEmailBox.Text.Trim(),
                PackagingRevision = (int)this.revisionUpDown.Value,
                Packager = this.packagerBox.Text.Trim(),
                Stem = this.stemBox.Text.Trim(),
                MapName = this.mapNameBox.Text.Trim(),
                MapDescription = this.mapDescriptionBox.Text.Trim(),
                Players = (int)this.playersUpDown.Value,
            };

            var error = info.Validate();
            if (!string.IsNullOrEmpty(error))
            {
                MessageBox.Show(this, error, @"Open Annihilation map pack", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            this.Result = info;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
