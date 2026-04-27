namespace Mappy.UI.Forms
{
    using System.Drawing;
    using System.Windows.Forms;

    public sealed class ReplaceFeatureForm : Form
    {
        private readonly TextBox sourceFeatureTextBox;

        private readonly TextBox destinationFeatureTextBox;

        public ReplaceFeatureForm()
        {
            this.Text = "Replace Feature";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            var sourceLabel = new Label
            {
                Text = "Source feature:",
                Location = new Point(12, 18),
                AutoSize = true,
            };
            this.sourceFeatureTextBox = new TextBox
            {
                Location = new Point(128, 15),
                Width = 240,
            };

            var destinationLabel = new Label
            {
                Text = "Destination feature:",
                Location = new Point(12, 50),
                AutoSize = true,
            };
            this.destinationFeatureTextBox = new TextBox
            {
                Location = new Point(128, 47),
                Width = 240,
            };

            var okButton = new Button
            {
                Text = "OK",
                Location = new Point(212, 86),
            };
            okButton.Click += (_, __) => this.TryAccept();

            var cancelButton = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new Point(293, 86),
            };

            this.AcceptButton = okButton;
            this.CancelButton = cancelButton;
            this.Controls.Add(sourceLabel);
            this.Controls.Add(this.sourceFeatureTextBox);
            this.Controls.Add(destinationLabel);
            this.Controls.Add(this.destinationFeatureTextBox);
            this.Controls.Add(okButton);
            this.Controls.Add(cancelButton);
            this.ClientSize = new Size(380, 122);
        }

        public string SourceFeatureName => this.sourceFeatureTextBox.Text;

        public string DestinationFeatureName => this.destinationFeatureTextBox.Text;

        private void TryAccept()
        {
            if (string.IsNullOrWhiteSpace(this.SourceFeatureName))
            {
                MessageBox.Show(this, "Source feature cannot be empty.", @"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(this.DestinationFeatureName))
            {
                MessageBox.Show(this, "Destination feature cannot be empty.", @"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
