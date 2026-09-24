namespace Mappy.UI.Forms
{
    partial class NewMapForm
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
            this.widthTextBox = new System.Windows.Forms.TextBox();
            this.widthLabel = new System.Windows.Forms.Label();
            this.heightLabel = new System.Windows.Forms.Label();
            this.heightTextBox = new System.Windows.Forms.TextBox();
            this.button1 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.convertedWidthLabel = new System.Windows.Forms.Label();
            this.convertedHeightLabel = new System.Windows.Forms.Label();
            this.widthUpButton = new System.Windows.Forms.Button();
            this.widthDownButton = new System.Windows.Forms.Button();
            this.widthUp16Button = new System.Windows.Forms.Button();
            this.widthDown16Button = new System.Windows.Forms.Button();
            this.heightUpButton = new System.Windows.Forms.Button();
            this.heightDownButton = new System.Windows.Forms.Button();
            this.heightUp16Button = new System.Windows.Forms.Button();
            this.heightDown16Button = new System.Windows.Forms.Button();
            this.addStandardBorderCheckBox = new System.Windows.Forms.CheckBox();
            this.borderHelpLabel = new System.Windows.Forms.Label();
            this.finalSizeLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // widthTextBox
            // 
            this.widthTextBox.Location = new System.Drawing.Point(86, 23);
            this.widthTextBox.Name = "widthTextBox";
            this.widthTextBox.Size = new System.Drawing.Size(60, 20);
            this.widthTextBox.TabIndex = 0;
            this.widthTextBox.Text = "256";
            this.widthTextBox.TextChanged += new System.EventHandler(this.WidthTextChanged);
            // 
            // widthLabel
            // 
            this.widthLabel.AutoSize = true;
            this.widthLabel.Location = new System.Drawing.Point(14, 26);
            this.widthLabel.Name = "widthLabel";
            this.widthLabel.Size = new System.Drawing.Size(66, 13);
            this.widthLabel.TabIndex = 1;
            this.widthLabel.Text = "Width (Tiles)";
            // 
            // heightLabel
            // 
            this.heightLabel.AutoSize = true;
            this.heightLabel.Location = new System.Drawing.Point(11, 86);
            this.heightLabel.Name = "heightLabel";
            this.heightLabel.Size = new System.Drawing.Size(69, 13);
            this.heightLabel.TabIndex = 2;
            this.heightLabel.Text = "Height (Tiles)";
            // 
            // heightTextBox
            // 
            this.heightTextBox.Location = new System.Drawing.Point(86, 83);
            this.heightTextBox.Name = "heightTextBox";
            this.heightTextBox.Size = new System.Drawing.Size(60, 20);
            this.heightTextBox.TabIndex = 5;
            this.heightTextBox.Text = "256";
            this.heightTextBox.TextChanged += new System.EventHandler(this.HeightTextChanged);
            // 
            // button1
            // 
            this.button1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.button1.Location = new System.Drawing.Point(156, 224);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 11;
            this.button1.Text = "Create";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.Button1Click);
            // 
            // button2
            // 
            this.button2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.button2.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.button2.Location = new System.Drawing.Point(237, 224);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(75, 23);
            this.button2.TabIndex = 12;
            this.button2.Text = "Cancel";
            this.button2.UseVisualStyleBackColor = true;
            // 
            // convertedWidthLabel
            //
            this.convertedWidthLabel.AutoSize = true;
            this.convertedWidthLabel.Location = new System.Drawing.Point(245, 26);
            this.convertedWidthLabel.Name = "convertedWidthLabel";
            this.convertedWidthLabel.Size = new System.Drawing.Size(19, 13);
            this.convertedWidthLabel.TabIndex = 13;
            this.convertedWidthLabel.Text = "(4)";
            // 
            // convertedHeightLabel
            //
            this.convertedHeightLabel.AutoSize = true;
            this.convertedHeightLabel.Location = new System.Drawing.Point(245, 86);
            this.convertedHeightLabel.Name = "convertedHeightLabel";
            this.convertedHeightLabel.Size = new System.Drawing.Size(19, 13);
            this.convertedHeightLabel.TabIndex = 14;
            this.convertedHeightLabel.Text = "(4)";
            //
            // widthUpButton
            //
            this.widthUpButton.Location = new System.Drawing.Point(151, 8);
            this.widthUpButton.Name = "widthUpButton";
            this.widthUpButton.Size = new System.Drawing.Size(28, 24);
            this.widthUpButton.TabIndex = 1;
            this.widthUpButton.Tag = 1;
            this.widthUpButton.Text = "▲";
            this.widthUpButton.UseVisualStyleBackColor = true;
            this.widthUpButton.AccessibleName = "Increase width by 1 tile";
            this.widthUpButton.Click += new System.EventHandler(this.WidthStepClick);
            //
            // widthDownButton
            //
            this.widthDownButton.Location = new System.Drawing.Point(151, 34);
            this.widthDownButton.Name = "widthDownButton";
            this.widthDownButton.Size = new System.Drawing.Size(28, 24);
            this.widthDownButton.TabIndex = 2;
            this.widthDownButton.Tag = -1;
            this.widthDownButton.Text = "▼";
            this.widthDownButton.UseVisualStyleBackColor = true;
            this.widthDownButton.AccessibleName = "Decrease width by 1 tile";
            this.widthDownButton.Click += new System.EventHandler(this.WidthStepClick);
            //
            // widthUp16Button
            //
            this.widthUp16Button.BackColor = System.Drawing.SystemColors.ControlLight;
            this.widthUp16Button.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold);
            this.widthUp16Button.Location = new System.Drawing.Point(185, 8);
            this.widthUp16Button.Name = "widthUp16Button";
            this.widthUp16Button.Size = new System.Drawing.Size(52, 24);
            this.widthUp16Button.TabIndex = 3;
            this.widthUp16Button.Tag = 16;
            this.widthUp16Button.Text = "+16";
            this.widthUp16Button.UseVisualStyleBackColor = false;
            this.widthUp16Button.AccessibleName = "Increase width by 16 tiles";
            this.widthUp16Button.Click += new System.EventHandler(this.WidthStepClick);
            //
            // widthDown16Button
            //
            this.widthDown16Button.BackColor = System.Drawing.SystemColors.ControlLight;
            this.widthDown16Button.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold);
            this.widthDown16Button.Location = new System.Drawing.Point(185, 34);
            this.widthDown16Button.Name = "widthDown16Button";
            this.widthDown16Button.Size = new System.Drawing.Size(52, 24);
            this.widthDown16Button.TabIndex = 4;
            this.widthDown16Button.Tag = -16;
            this.widthDown16Button.Text = "−16";
            this.widthDown16Button.UseVisualStyleBackColor = false;
            this.widthDown16Button.AccessibleName = "Decrease width by 16 tiles";
            this.widthDown16Button.Click += new System.EventHandler(this.WidthStepClick);
            //
            // heightUpButton
            //
            this.heightUpButton.Location = new System.Drawing.Point(151, 68);
            this.heightUpButton.Name = "heightUpButton";
            this.heightUpButton.Size = new System.Drawing.Size(28, 24);
            this.heightUpButton.TabIndex = 6;
            this.heightUpButton.Tag = 1;
            this.heightUpButton.Text = "▲";
            this.heightUpButton.UseVisualStyleBackColor = true;
            this.heightUpButton.AccessibleName = "Increase height by 1 tile";
            this.heightUpButton.Click += new System.EventHandler(this.HeightStepClick);
            //
            // heightDownButton
            //
            this.heightDownButton.Location = new System.Drawing.Point(151, 94);
            this.heightDownButton.Name = "heightDownButton";
            this.heightDownButton.Size = new System.Drawing.Size(28, 24);
            this.heightDownButton.TabIndex = 7;
            this.heightDownButton.Tag = -1;
            this.heightDownButton.Text = "▼";
            this.heightDownButton.UseVisualStyleBackColor = true;
            this.heightDownButton.AccessibleName = "Decrease height by 1 tile";
            this.heightDownButton.Click += new System.EventHandler(this.HeightStepClick);
            //
            // heightUp16Button
            //
            this.heightUp16Button.BackColor = System.Drawing.SystemColors.ControlLight;
            this.heightUp16Button.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold);
            this.heightUp16Button.Location = new System.Drawing.Point(185, 68);
            this.heightUp16Button.Name = "heightUp16Button";
            this.heightUp16Button.Size = new System.Drawing.Size(52, 24);
            this.heightUp16Button.TabIndex = 8;
            this.heightUp16Button.Tag = 16;
            this.heightUp16Button.Text = "+16";
            this.heightUp16Button.UseVisualStyleBackColor = false;
            this.heightUp16Button.AccessibleName = "Increase height by 16 tiles";
            this.heightUp16Button.Click += new System.EventHandler(this.HeightStepClick);
            //
            // heightDown16Button
            //
            this.heightDown16Button.BackColor = System.Drawing.SystemColors.ControlLight;
            this.heightDown16Button.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold);
            this.heightDown16Button.Location = new System.Drawing.Point(185, 94);
            this.heightDown16Button.Name = "heightDown16Button";
            this.heightDown16Button.Size = new System.Drawing.Size(52, 24);
            this.heightDown16Button.TabIndex = 9;
            this.heightDown16Button.Tag = -16;
            this.heightDown16Button.Text = "−16";
            this.heightDown16Button.UseVisualStyleBackColor = false;
            this.heightDown16Button.AccessibleName = "Decrease height by 16 tiles";
            this.heightDown16Button.Click += new System.EventHandler(this.HeightStepClick);
            //
            // addStandardBorderCheckBox
            //
            this.addStandardBorderCheckBox.AutoSize = true;
            this.addStandardBorderCheckBox.Checked = true;
            this.addStandardBorderCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.addStandardBorderCheckBox.Location = new System.Drawing.Point(14, 131);
            this.addStandardBorderCheckBox.Name = "addStandardBorderCheckBox";
            this.addStandardBorderCheckBox.Size = new System.Drawing.Size(288, 17);
            this.addStandardBorderCheckBox.TabIndex = 10;
            this.addStandardBorderCheckBox.Text = "Add standard map border (+1 width, +4 height)";
            this.addStandardBorderCheckBox.UseVisualStyleBackColor = true;
            this.addStandardBorderCheckBox.CheckedChanged += new System.EventHandler(this.AddStandardBorderCheckBoxCheckedChanged);
            //
            // borderHelpLabel
            //
            this.borderHelpLabel.ForeColor = System.Drawing.SystemColors.GrayText;
            this.borderHelpLabel.Location = new System.Drawing.Point(29, 154);
            this.borderHelpLabel.Name = "borderHelpLabel";
            this.borderHelpLabel.Size = new System.Drawing.Size(283, 28);
            this.borderHelpLabel.TabIndex = 15;
            this.borderHelpLabel.Text = "Recommended. Keeps the playable area aligned\r\nto multiples of 16.";
            //
            // finalSizeLabel
            //
            this.finalSizeLabel.AutoSize = true;
            this.finalSizeLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold);
            this.finalSizeLabel.Location = new System.Drawing.Point(14, 194);
            this.finalSizeLabel.Name = "finalSizeLabel";
            this.finalSizeLabel.Size = new System.Drawing.Size(174, 13);
            this.finalSizeLabel.TabIndex = 16;
            this.finalSizeLabel.Text = "Final map size: 257 × 260 tiles";
            // 
            // NewMapForm
            // 
            this.AcceptButton = this.button1;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.button2;
            this.ClientSize = new System.Drawing.Size(324, 259);
            this.Controls.Add(this.finalSizeLabel);
            this.Controls.Add(this.borderHelpLabel);
            this.Controls.Add(this.addStandardBorderCheckBox);
            this.Controls.Add(this.heightDown16Button);
            this.Controls.Add(this.heightUp16Button);
            this.Controls.Add(this.heightDownButton);
            this.Controls.Add(this.heightUpButton);
            this.Controls.Add(this.widthDown16Button);
            this.Controls.Add(this.widthUp16Button);
            this.Controls.Add(this.widthDownButton);
            this.Controls.Add(this.widthUpButton);
            this.Controls.Add(this.convertedHeightLabel);
            this.Controls.Add(this.convertedWidthLabel);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.heightTextBox);
            this.Controls.Add(this.heightLabel);
            this.Controls.Add(this.widthLabel);
            this.Controls.Add(this.widthTextBox);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "NewMapForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "New Map";
            this.Validating += new System.ComponentModel.CancelEventHandler(this.FormValidating);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox widthTextBox;
        private System.Windows.Forms.Label widthLabel;
        private System.Windows.Forms.Label heightLabel;
        private System.Windows.Forms.TextBox heightTextBox;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Label convertedWidthLabel;
        private System.Windows.Forms.Label convertedHeightLabel;
        private System.Windows.Forms.Button widthUpButton;
        private System.Windows.Forms.Button widthDownButton;
        private System.Windows.Forms.Button widthUp16Button;
        private System.Windows.Forms.Button widthDown16Button;
        private System.Windows.Forms.Button heightUpButton;
        private System.Windows.Forms.Button heightDownButton;
        private System.Windows.Forms.Button heightUp16Button;
        private System.Windows.Forms.Button heightDown16Button;
        private System.Windows.Forms.CheckBox addStandardBorderCheckBox;
        private System.Windows.Forms.Label borderHelpLabel;
        private System.Windows.Forms.Label finalSizeLabel;
    }
}
