namespace Mappy.UI.Forms
{
    partial class UpdateAvailableForm
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.headerLabel = new System.Windows.Forms.Label();
            this.notesTextBox = new System.Windows.Forms.RichTextBox();
            this.updateButton = new System.Windows.Forms.Button();
            this.notNowButton = new System.Windows.Forms.Button();
            this.skipButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            //
            // headerLabel
            //
            this.headerLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.headerLabel.Location = new System.Drawing.Point(12, 12);
            this.headerLabel.Name = "headerLabel";
            this.headerLabel.Size = new System.Drawing.Size(496, 32);
            this.headerLabel.TabIndex = 0;
            this.headerLabel.Text = "A new version is available.";
            //
            // notesTextBox
            //
            this.notesTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.notesTextBox.Location = new System.Drawing.Point(12, 47);
            this.notesTextBox.Name = "notesTextBox";
            this.notesTextBox.ReadOnly = true;
            this.notesTextBox.Size = new System.Drawing.Size(496, 280);
            this.notesTextBox.TabIndex = 1;
            this.notesTextBox.Text = "";
            //
            // skipButton
            //
            this.skipButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.skipButton.DialogResult = System.Windows.Forms.DialogResult.Ignore;
            this.skipButton.Location = new System.Drawing.Point(12, 341);
            this.skipButton.Name = "skipButton";
            this.skipButton.Size = new System.Drawing.Size(240, 23);
            this.skipButton.TabIndex = 2;
            this.skipButton.Text = "Do not remind me about this version";
            this.skipButton.UseVisualStyleBackColor = true;
            //
            // notNowButton
            //
            this.notNowButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.notNowButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.notNowButton.Location = new System.Drawing.Point(352, 341);
            this.notNowButton.Name = "notNowButton";
            this.notNowButton.Size = new System.Drawing.Size(75, 23);
            this.notNowButton.TabIndex = 3;
            this.notNowButton.Text = "Not now";
            this.notNowButton.UseVisualStyleBackColor = true;
            //
            // updateButton
            //
            this.updateButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.updateButton.DialogResult = System.Windows.Forms.DialogResult.Yes;
            this.updateButton.Location = new System.Drawing.Point(433, 341);
            this.updateButton.Name = "updateButton";
            this.updateButton.Size = new System.Drawing.Size(75, 23);
            this.updateButton.TabIndex = 4;
            this.updateButton.Text = "Update";
            this.updateButton.UseVisualStyleBackColor = true;
            //
            // UpdateAvailableForm
            //
            this.AcceptButton = this.updateButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.notNowButton;
            this.ClientSize = new System.Drawing.Size(520, 376);
            this.Controls.Add(this.updateButton);
            this.Controls.Add(this.notNowButton);
            this.Controls.Add(this.skipButton);
            this.Controls.Add(this.notesTextBox);
            this.Controls.Add(this.headerLabel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(480, 320);
            this.Name = "UpdateAvailableForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Update Available";
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Label headerLabel;
        private System.Windows.Forms.RichTextBox notesTextBox;
        private System.Windows.Forms.Button updateButton;
        private System.Windows.Forms.Button notNowButton;
        private System.Windows.Forms.Button skipButton;
    }
}
