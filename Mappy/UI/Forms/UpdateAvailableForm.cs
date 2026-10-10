namespace Mappy.UI.Forms
{
    using System;
    using System.Windows.Forms;

    public partial class UpdateAvailableForm : Form
    {
        public UpdateAvailableForm()
        {
            this.InitializeComponent();
        }

        public UpdateAvailableForm(string tagName, string installedVersion, string releaseNotes)
            : this()
        {
            var version = string.IsNullOrWhiteSpace(tagName) ? "a new version" : tagName.Trim();
            var installed = string.IsNullOrWhiteSpace(installedVersion) ? "unknown" : installedVersion.Trim();
            this.headerLabel.Text = "Version " + version + " is available (you have " + installed + ").";
            this.notesTextBox.Text = NormalizeNotes(releaseNotes);
        }

        private static string NormalizeNotes(string releaseNotes)
        {
            if (string.IsNullOrWhiteSpace(releaseNotes))
            {
                return "No release notes were published for this version.";
            }

            return releaseNotes.Replace("\r\n", "\n").Replace('\r', '\n').Replace("\n", Environment.NewLine);
        }
    }
}
