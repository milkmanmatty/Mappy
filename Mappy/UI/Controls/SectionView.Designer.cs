namespace Mappy.UI.Controls
{
    partial class SectionView
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (this.components != null))
            {
                this.components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.control = new Mappy.UI.Controls.DoubleComboListView();
            this.SuspendLayout();
            // 
            // control
            // 
            this.control.Dock = System.Windows.Forms.DockStyle.Fill;
            this.control.Location = new System.Drawing.Point(0, 0);
            this.control.Name = "control";
            this.control.Size = new System.Drawing.Size(202, 482);
            this.control.TabIndex = 0;
            // 
            // SectionView
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
            this.Controls.Add(this.control);
            this.Margin = new System.Windows.Forms.Padding(0);
            this.Name = "SectionView";
            this.Size = new System.Drawing.Size(202, 482);
            this.ResumeLayout(false);

        }

        #endregion

        private DoubleComboListView control;
    }
}
