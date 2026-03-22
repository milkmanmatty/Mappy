namespace Mappy.UI.Forms
{
    using System.Windows.Forms;
    using Mappy.Models.Enums;

    public partial class FlipSectionForm : Form
    {
        public FlipSectionForm()
        {
            this.InitializeComponent();
        }

        public FlipDirection FlipDirection => this.flipHoriRadio.Checked ? FlipDirection.Horizontal : FlipDirection.Vertical;

        public bool RelightShadows => this.relightRadio.Checked;
    }
}