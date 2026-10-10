using System.ComponentModel;

namespace Mappy.UI.Forms
{
    partial class UseOnlyUnitsForm
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
            this.filterRow = new System.Windows.Forms.TableLayoutPanel();
            this.searchBox = new System.Windows.Forms.TextBox();
            this.sideLabel = new System.Windows.Forms.Label();
            this.sideCombo = new System.Windows.Forms.ComboBox();
            this.typeLabel = new System.Windows.Forms.Label();
            this.typeCombo = new System.Windows.Forms.ComboBox();
            this.listsLayout = new System.Windows.Forms.TableLayoutPanel();
            this.availablePanel = new System.Windows.Forms.Panel();
            this.availableList = new System.Windows.Forms.ListBox();
            this.availableLabel = new System.Windows.Forms.Label();
            this.buttonColumn = new System.Windows.Forms.FlowLayoutPanel();
            this.addButton = new System.Windows.Forms.Button();
            this.removeButton = new System.Windows.Forms.Button();
            this.addMatchingButton = new System.Windows.Forms.Button();
            this.clearButton = new System.Windows.Forms.Button();
            this.selectedPanel = new System.Windows.Forms.Panel();
            this.selectedList = new System.Windows.Forms.ListBox();
            this.selectedLabel = new System.Windows.Forms.Label();
            this.buttonsPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.cancelButton = new System.Windows.Forms.Button();
            this.okButton = new System.Windows.Forms.Button();
            this.filterRow.SuspendLayout();
            this.listsLayout.SuspendLayout();
            this.availablePanel.SuspendLayout();
            this.buttonColumn.SuspendLayout();
            this.selectedPanel.SuspendLayout();
            this.buttonsPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // filterRow
            // 
            this.filterRow.AutoSize = true;
            this.filterRow.ColumnCount = 5;
            this.filterRow.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.filterRow.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.filterRow.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.filterRow.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.filterRow.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.filterRow.Controls.Add(this.searchBox, 0, 0);
            this.filterRow.Controls.Add(this.sideLabel, 1, 0);
            this.filterRow.Controls.Add(this.sideCombo, 2, 0);
            this.filterRow.Controls.Add(this.typeLabel, 3, 0);
            this.filterRow.Controls.Add(this.typeCombo, 4, 0);
            this.filterRow.Dock = System.Windows.Forms.DockStyle.Top;
            this.filterRow.Location = new System.Drawing.Point(0, 0);
            this.filterRow.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.filterRow.Name = "filterRow";
            this.filterRow.Padding = new System.Windows.Forms.Padding(6, 5, 6, 3);
            this.filterRow.RowCount = 1;
            this.filterRow.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.filterRow.Size = new System.Drawing.Size(696, 32);
            this.filterRow.TabIndex = 0;
            // 
            // searchBox
            // 
            this.searchBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.searchBox.Location = new System.Drawing.Point(8, 7);
            this.searchBox.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.searchBox.Name = "searchBox";
            this.searchBox.Size = new System.Drawing.Size(415, 20);
            this.searchBox.TabIndex = 0;
            this.searchBox.TextChanged += new System.EventHandler(this.SearchBoxTextChanged);
            // 
            // sideLabel
            // 
            this.sideLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.sideLabel.AutoSize = true;
            this.sideLabel.Location = new System.Drawing.Point(431, 12);
            this.sideLabel.Margin = new System.Windows.Forms.Padding(6, 4, 0, 0);
            this.sideLabel.Name = "sideLabel";
            this.sideLabel.Size = new System.Drawing.Size(28, 13);
            this.sideLabel.TabIndex = 1;
            this.sideLabel.Text = "Side";
            // 
            // sideCombo
            // 
            this.sideCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.sideCombo.FormattingEnabled = true;
            this.sideCombo.Location = new System.Drawing.Point(465, 5);
            this.sideCombo.Margin = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.sideCombo.Name = "sideCombo";
            this.sideCombo.Size = new System.Drawing.Size(91, 21);
            this.sideCombo.TabIndex = 2;
            this.sideCombo.SelectedIndexChanged += new System.EventHandler(this.SideComboSelectedIndexChanged);
            // 
            // typeLabel
            // 
            this.typeLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.typeLabel.AutoSize = true;
            this.typeLabel.Location = new System.Drawing.Point(562, 12);
            this.typeLabel.Margin = new System.Windows.Forms.Padding(6, 4, 0, 0);
            this.typeLabel.Name = "typeLabel";
            this.typeLabel.Size = new System.Drawing.Size(31, 13);
            this.typeLabel.TabIndex = 3;
            this.typeLabel.Text = "Type";
            // 
            // typeCombo
            // 
            this.typeCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.typeCombo.FormattingEnabled = true;
            this.typeCombo.Location = new System.Drawing.Point(599, 5);
            this.typeCombo.Margin = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.typeCombo.Name = "typeCombo";
            this.typeCombo.Size = new System.Drawing.Size(91, 21);
            this.typeCombo.TabIndex = 4;
            this.typeCombo.SelectedIndexChanged += new System.EventHandler(this.TypeComboSelectedIndexChanged);
            // 
            // listsLayout
            // 
            this.listsLayout.ColumnCount = 3;
            this.listsLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.listsLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.listsLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.listsLayout.Controls.Add(this.availablePanel, 0, 0);
            this.listsLayout.Controls.Add(this.buttonColumn, 1, 0);
            this.listsLayout.Controls.Add(this.selectedPanel, 2, 0);
            this.listsLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listsLayout.Location = new System.Drawing.Point(0, 32);
            this.listsLayout.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.listsLayout.Name = "listsLayout";
            this.listsLayout.RowCount = 1;
            this.listsLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.listsLayout.Size = new System.Drawing.Size(696, 342);
            this.listsLayout.TabIndex = 1;
            // 
            // availablePanel
            // 
            this.availablePanel.Controls.Add(this.availableList);
            this.availablePanel.Controls.Add(this.availableLabel);
            this.availablePanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.availablePanel.Location = new System.Drawing.Point(2, 2);
            this.availablePanel.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.availablePanel.Name = "availablePanel";
            this.availablePanel.Padding = new System.Windows.Forms.Padding(6, 3, 3, 3);
            this.availablePanel.Size = new System.Drawing.Size(283, 338);
            this.availablePanel.TabIndex = 0;
            // 
            // availableList
            // 
            this.availableList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.availableList.FormattingEnabled = true;
            this.availableList.IntegralHeight = false;
            this.availableList.Location = new System.Drawing.Point(6, 19);
            this.availableList.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.availableList.Name = "availableList";
            this.availableList.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
            this.availableList.Size = new System.Drawing.Size(274, 316);
            this.availableList.TabIndex = 1;
            this.availableList.DoubleClick += new System.EventHandler(this.AvailableListDoubleClick);
            // 
            // availableLabel
            // 
            this.availableLabel.AutoSize = true;
            this.availableLabel.Dock = System.Windows.Forms.DockStyle.Top;
            this.availableLabel.Location = new System.Drawing.Point(6, 3);
            this.availableLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.availableLabel.Name = "availableLabel";
            this.availableLabel.Padding = new System.Windows.Forms.Padding(0, 0, 0, 3);
            this.availableLabel.Size = new System.Drawing.Size(50, 16);
            this.availableLabel.TabIndex = 0;
            this.availableLabel.Text = "Available";
            // 
            // buttonColumn
            // 
            this.buttonColumn.AutoSize = true;
            this.buttonColumn.Controls.Add(this.addButton);
            this.buttonColumn.Controls.Add(this.removeButton);
            this.buttonColumn.Controls.Add(this.addMatchingButton);
            this.buttonColumn.Controls.Add(this.clearButton);
            this.buttonColumn.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonColumn.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.buttonColumn.Location = new System.Drawing.Point(289, 2);
            this.buttonColumn.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.buttonColumn.Name = "buttonColumn";
            this.buttonColumn.Padding = new System.Windows.Forms.Padding(0, 18, 0, 0);
            this.buttonColumn.Size = new System.Drawing.Size(117, 338);
            this.buttonColumn.TabIndex = 1;
            this.buttonColumn.WrapContents = false;
            // 
            // addButton
            // 
            this.addButton.AutoSize = true;
            this.addButton.Location = new System.Drawing.Point(3, 21);
            this.addButton.MinimumSize = new System.Drawing.Size(90, 18);
            this.addButton.Name = "addButton";
            this.addButton.Size = new System.Drawing.Size(111, 25);
            this.addButton.TabIndex = 0;
            this.addButton.Text = "Add >";
            this.addButton.UseVisualStyleBackColor = true;
            this.addButton.Click += new System.EventHandler(this.AddButtonClick);
            // 
            // removeButton
            // 
            this.removeButton.AutoSize = true;
            this.removeButton.Location = new System.Drawing.Point(3, 52);
            this.removeButton.MinimumSize = new System.Drawing.Size(90, 18);
            this.removeButton.Name = "removeButton";
            this.removeButton.Size = new System.Drawing.Size(111, 25);
            this.removeButton.TabIndex = 1;
            this.removeButton.Text = "< Remove";
            this.removeButton.UseVisualStyleBackColor = true;
            this.removeButton.Click += new System.EventHandler(this.RemoveButtonClick);
            // 
            // addMatchingButton
            // 
            this.addMatchingButton.AutoSize = true;
            this.addMatchingButton.Location = new System.Drawing.Point(3, 83);
            this.addMatchingButton.MinimumSize = new System.Drawing.Size(90, 18);
            this.addMatchingButton.Name = "addMatchingButton";
            this.addMatchingButton.Size = new System.Drawing.Size(111, 25);
            this.addMatchingButton.TabIndex = 2;
            this.addMatchingButton.Text = "Add matching >>";
            this.addMatchingButton.UseVisualStyleBackColor = true;
            this.addMatchingButton.Click += new System.EventHandler(this.AddMatchingButtonClick);
            // 
            // clearButton
            // 
            this.clearButton.AutoSize = true;
            this.clearButton.Location = new System.Drawing.Point(3, 114);
            this.clearButton.MinimumSize = new System.Drawing.Size(90, 18);
            this.clearButton.Name = "clearButton";
            this.clearButton.Size = new System.Drawing.Size(111, 25);
            this.clearButton.TabIndex = 3;
            this.clearButton.Text = "Clear";
            this.clearButton.UseVisualStyleBackColor = true;
            this.clearButton.Click += new System.EventHandler(this.ClearButtonClick);
            // 
            // selectedPanel
            // 
            this.selectedPanel.Controls.Add(this.selectedList);
            this.selectedPanel.Controls.Add(this.selectedLabel);
            this.selectedPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.selectedPanel.Location = new System.Drawing.Point(410, 2);
            this.selectedPanel.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.selectedPanel.Name = "selectedPanel";
            this.selectedPanel.Padding = new System.Windows.Forms.Padding(3, 3, 6, 3);
            this.selectedPanel.Size = new System.Drawing.Size(284, 338);
            this.selectedPanel.TabIndex = 2;
            // 
            // selectedList
            // 
            this.selectedList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.selectedList.FormattingEnabled = true;
            this.selectedList.IntegralHeight = false;
            this.selectedList.Location = new System.Drawing.Point(3, 19);
            this.selectedList.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.selectedList.Name = "selectedList";
            this.selectedList.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
            this.selectedList.Size = new System.Drawing.Size(275, 316);
            this.selectedList.TabIndex = 1;
            this.selectedList.DoubleClick += new System.EventHandler(this.SelectedListDoubleClick);
            // 
            // selectedLabel
            // 
            this.selectedLabel.AutoSize = true;
            this.selectedLabel.Dock = System.Windows.Forms.DockStyle.Top;
            this.selectedLabel.Location = new System.Drawing.Point(3, 3);
            this.selectedLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.selectedLabel.Name = "selectedLabel";
            this.selectedLabel.Padding = new System.Windows.Forms.Padding(0, 0, 0, 3);
            this.selectedLabel.Size = new System.Drawing.Size(44, 16);
            this.selectedLabel.TabIndex = 0;
            this.selectedLabel.Text = "Allowed";
            // 
            // buttonsPanel
            // 
            this.buttonsPanel.AutoSize = true;
            this.buttonsPanel.Controls.Add(this.cancelButton);
            this.buttonsPanel.Controls.Add(this.okButton);
            this.buttonsPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.buttonsPanel.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.buttonsPanel.Location = new System.Drawing.Point(0, 374);
            this.buttonsPanel.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.buttonsPanel.Name = "buttonsPanel";
            this.buttonsPanel.Padding = new System.Windows.Forms.Padding(6, 5, 6, 5);
            this.buttonsPanel.Size = new System.Drawing.Size(696, 39);
            this.buttonsPanel.TabIndex = 2;
            // 
            // cancelButton
            // 
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.Location = new System.Drawing.Point(626, 7);
            this.cancelButton.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(56, 25);
            this.cancelButton.TabIndex = 0;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            // 
            // okButton
            // 
            this.okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.okButton.Location = new System.Drawing.Point(566, 7);
            this.okButton.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(56, 25);
            this.okButton.TabIndex = 1;
            this.okButton.Text = "OK";
            this.okButton.UseVisualStyleBackColor = true;
            this.okButton.Click += new System.EventHandler(this.OkButtonClick);
            // 
            // UseOnlyUnitsForm
            // 
            this.AcceptButton = this.okButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.cancelButton;
            this.ClientSize = new System.Drawing.Size(696, 413);
            this.Controls.Add(this.listsLayout);
            this.Controls.Add(this.buttonsPanel);
            this.Controls.Add(this.filterRow);
            this.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(484, 287);
            this.Name = "UseOnlyUnitsForm";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Use Only Units";
            this.Shown += new System.EventHandler(this.UseOnlyUnitsFormShown);
            this.filterRow.ResumeLayout(false);
            this.filterRow.PerformLayout();
            this.listsLayout.ResumeLayout(false);
            this.listsLayout.PerformLayout();
            this.availablePanel.ResumeLayout(false);
            this.availablePanel.PerformLayout();
            this.buttonColumn.ResumeLayout(false);
            this.buttonColumn.PerformLayout();
            this.selectedPanel.ResumeLayout(false);
            this.selectedPanel.PerformLayout();
            this.buttonsPanel.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel filterRow;

        private System.Windows.Forms.TextBox searchBox;

        private System.Windows.Forms.Label sideLabel;

        private System.Windows.Forms.ComboBox sideCombo;

        private System.Windows.Forms.Label typeLabel;

        private System.Windows.Forms.ComboBox typeCombo;

        private System.Windows.Forms.TableLayoutPanel listsLayout;

        private System.Windows.Forms.Panel availablePanel;

        private System.Windows.Forms.ListBox availableList;

        private System.Windows.Forms.Label availableLabel;

        private System.Windows.Forms.FlowLayoutPanel buttonColumn;

        private System.Windows.Forms.Button addButton;

        private System.Windows.Forms.Button removeButton;

        private System.Windows.Forms.Button addMatchingButton;

        private System.Windows.Forms.Button clearButton;

        private System.Windows.Forms.Panel selectedPanel;

        private System.Windows.Forms.ListBox selectedList;

        private System.Windows.Forms.Label selectedLabel;

        private System.Windows.Forms.FlowLayoutPanel buttonsPanel;

        private System.Windows.Forms.Button cancelButton;

        private System.Windows.Forms.Button okButton;
    }
}
