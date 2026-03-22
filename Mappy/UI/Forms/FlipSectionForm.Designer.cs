using System.ComponentModel;

namespace Mappy.UI.Forms
{
    partial class FlipSectionForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FlipSectionForm));
            this.label1 = new System.Windows.Forms.Label();
            this.flipDirectionGroupBox = new System.Windows.Forms.GroupBox();
            this.flipVertRadio = new System.Windows.Forms.RadioButton();
            this.flipHoriRadio = new System.Windows.Forms.RadioButton();
            this.RelightGroupBox = new System.Windows.Forms.GroupBox();
            this.doNotRelightRadio = new System.Windows.Forms.RadioButton();
            this.relightRadio = new System.Windows.Forms.RadioButton();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.button1 = new System.Windows.Forms.Button();
            this.warningImg = new System.Windows.Forms.PictureBox();
            this.warningLabel = new System.Windows.Forms.Label();
            this.flipDirectionGroupBox.SuspendLayout();
            this.RelightGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.warningImg)).BeginInit();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(12, 128);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(491, 75);
            this.label1.TabIndex = 1;
            this.label1.Text = "Which direction do you want to flip the selected section?";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // flipDirectionGroupBox
            // 
            this.flipDirectionGroupBox.Controls.Add(this.flipVertRadio);
            this.flipDirectionGroupBox.Controls.Add(this.flipHoriRadio);
            this.flipDirectionGroupBox.Location = new System.Drawing.Point(12, 206);
            this.flipDirectionGroupBox.Name = "flipDirectionGroupBox";
            this.flipDirectionGroupBox.Size = new System.Drawing.Size(491, 113);
            this.flipDirectionGroupBox.TabIndex = 2;
            this.flipDirectionGroupBox.TabStop = false;
            this.flipDirectionGroupBox.Text = "Flip direction";
            // 
            // flipVertRadio
            // 
            this.flipVertRadio.Location = new System.Drawing.Point(9, 66);
            this.flipVertRadio.Name = "flipVertRadio";
            this.flipVertRadio.Size = new System.Drawing.Size(476, 32);
            this.flipVertRadio.TabIndex = 1;
            this.flipVertRadio.Text = "Flip Vertically";
            this.flipVertRadio.UseVisualStyleBackColor = true;
            // 
            // flipHoriRadio
            // 
            this.flipHoriRadio.Checked = true;
            this.flipHoriRadio.Location = new System.Drawing.Point(9, 30);
            this.flipHoriRadio.Name = "flipHoriRadio";
            this.flipHoriRadio.Size = new System.Drawing.Size(476, 27);
            this.flipHoriRadio.TabIndex = 0;
            this.flipHoriRadio.TabStop = true;
            this.flipHoriRadio.Text = "Flip Horizontally";
            this.flipHoriRadio.UseVisualStyleBackColor = true;
            // 
            // RelightGroupBox
            // 
            this.RelightGroupBox.Controls.Add(this.doNotRelightRadio);
            this.RelightGroupBox.Controls.Add(this.relightRadio);
            this.RelightGroupBox.Location = new System.Drawing.Point(12, 393);
            this.RelightGroupBox.Name = "RelightGroupBox";
            this.RelightGroupBox.Size = new System.Drawing.Size(491, 110);
            this.RelightGroupBox.TabIndex = 3;
            this.RelightGroupBox.TabStop = false;
            this.RelightGroupBox.Text = "Relight terrain?";
            // 
            // doNotRelightRadio
            // 
            this.doNotRelightRadio.Location = new System.Drawing.Point(9, 68);
            this.doNotRelightRadio.Name = "doNotRelightRadio";
            this.doNotRelightRadio.Size = new System.Drawing.Size(476, 28);
            this.doNotRelightRadio.TabIndex = 1;
            this.doNotRelightRadio.Text = "Do not relight terrain shadows";
            this.doNotRelightRadio.UseVisualStyleBackColor = true;
            // 
            // relightRadio
            // 
            this.relightRadio.Checked = true;
            this.relightRadio.Location = new System.Drawing.Point(9, 25);
            this.relightRadio.Name = "relightRadio";
            this.relightRadio.Size = new System.Drawing.Size(476, 37);
            this.relightRadio.TabIndex = 0;
            this.relightRadio.TabStop = true;
            this.relightRadio.Text = "Relight terrain shadows";
            this.relightRadio.UseVisualStyleBackColor = true;
            // 
            // label2
            // 
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(12, 329);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(492, 41);
            this.label2.TabIndex = 4;
            this.label2.Text = "Relight terrain shadows?\r\n\r\n";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label3
            // 
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(12, 370);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(489, 20);
            this.label3.TabIndex = 5;
            this.label3.Text = "This will not change shadows cast from one object to another";
            // 
            // button1
            // 
            this.button1.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.button1.Location = new System.Drawing.Point(12, 509);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(491, 47);
            this.button1.TabIndex = 6;
            this.button1.Text = "Flip Terrain Section";
            this.button1.UseVisualStyleBackColor = true;
            // 
            // warningImg
            // 
            this.warningImg.Image = global::Mappy.Properties.Resources.Warning;
            this.warningImg.Location = new System.Drawing.Point(12, 29);
            this.warningImg.Name = "warningImg";
            this.warningImg.Size = new System.Drawing.Size(72, 72);
            this.warningImg.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
            this.warningImg.TabIndex = 7;
            this.warningImg.TabStop = false;
            // 
            // warningLabel
            // 
            this.warningLabel.Location = new System.Drawing.Point(90, 18);
            this.warningLabel.Name = "warningLabel";
            this.warningLabel.Size = new System.Drawing.Size(413, 110);
            this.warningLabel.TabIndex = 8;
            this.warningLabel.Text = resources.GetString("warningLabel.Text");
            // 
            // FlipSectionForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(515, 567);
            this.Controls.Add(this.warningLabel);
            this.Controls.Add(this.warningImg);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.RelightGroupBox);
            this.Controls.Add(this.flipDirectionGroupBox);
            this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FlipSectionForm";
            this.ShowIcon = false;
            this.Text = " ";
            this.TopMost = true;
            this.flipDirectionGroupBox.ResumeLayout(false);
            this.RelightGroupBox.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.warningImg)).EndInit();
            this.ResumeLayout(false);
        }

        private System.Windows.Forms.Label warningLabel;

        private System.Windows.Forms.PictureBox warningImg;

        private System.Windows.Forms.Button button1;

        private System.Windows.Forms.Label label3;

        private System.Windows.Forms.RadioButton flipHoriRadio;
        private System.Windows.Forms.RadioButton flipVertRadio;
        private System.Windows.Forms.RadioButton relightRadio;
        private System.Windows.Forms.RadioButton doNotRelightRadio;

        private System.Windows.Forms.GroupBox flipDirectionGroupBox;
        private System.Windows.Forms.GroupBox RelightGroupBox;
        private System.Windows.Forms.Label label2;

        private System.Windows.Forms.Label label1;

        #endregion
    }
}