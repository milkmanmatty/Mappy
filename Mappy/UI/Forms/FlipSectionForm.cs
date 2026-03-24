namespace Mappy.UI.Forms
{
    using System;
    using System.Windows.Forms;
    using Mappy.Models.Enums;

    public partial class FlipSectionForm : Form
    {
        public FlipSectionForm()
        {
            this.InitializeComponent();
            this.SetLabel();
        }

        public FlipDirection FlipDirection => this.flipHoriRadio.Checked ? FlipDirection.Horizontal : FlipDirection.Vertical;

        public bool RelightShadows => this.relightRadio.Checked;

        private void SetLabel()
        {
            DateTime today = DateTime.Now;

            if (today.Month == 4 && today.Day == 1)
            {
                this.button1.Text = @"Confess your sins to MilkmanMatty";
            }
        }
    }
}