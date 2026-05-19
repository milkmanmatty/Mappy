namespace Mappy.UI.Forms
{
    partial class PreferencesForm
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
            this.searchPathsListView = new System.Windows.Forms.ListView();
            this.addButton = new System.Windows.Forms.Button();
            this.removeButton = new System.Windows.Forms.Button();
            this.upButton = new System.Windows.Forms.Button();
            this.downButton = new System.Windows.Forms.Button();
            this.okButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.sidePanel = new System.Windows.Forms.Panel();
            this.bottomPanel = new System.Windows.Forms.Panel();
            this.searchPathsPanel = new System.Windows.Forms.Panel();
            this.mainGroupBox = new System.Windows.Forms.GroupBox();
            this.scrollSpeedGroupBox = new System.Windows.Forms.GroupBox();
            this.dragScrollSpeedYNumeric = new System.Windows.Forms.NumericUpDown();
            this.dragScrollSpeedXNumeric = new System.Windows.Forms.NumericUpDown();
            this.dragScrollSpeedYLabel = new System.Windows.Forms.Label();
            this.dragScrollSpeedXLabel = new System.Windows.Forms.Label();
            this.missionGroupBox = new System.Windows.Forms.GroupBox();
            this.showUnitFriendlyNameOnMapCheckBox = new System.Windows.Forms.CheckBox();
            this.showUnitFriendlyNameFirstCheckBox = new System.Windows.Forms.CheckBox();
            this.inactiveSchemaOpacityNumeric = new System.Windows.Forms.NumericUpDown();
            this.inactiveSchemaOpacityLabel = new System.Windows.Forms.Label();
            this.resourceNamesGroupBox = new System.Windows.Forms.GroupBox();
            this.calculatedMetalDepositValueCheckBox = new System.Windows.Forms.CheckBox();
            this.featureReclaimAmountsCheckBox = new System.Windows.Forms.CheckBox();
            this.fullResourceNamesCheckBox = new System.Windows.Forms.CheckBox();
            this.miscGroupBox = new System.Windows.Forms.GroupBox();
            this.doNotPromptToSaveUnsavedChangesCheckBox = new System.Windows.Forms.CheckBox();
            this.preferencesTabControl = new System.Windows.Forms.TabControl();
            this.miscTabPage = new System.Windows.Forms.TabPage();
            this.missionTabPage = new System.Windows.Forms.TabPage();
            this.adjustmentsTabPage = new System.Windows.Forms.TabPage();
            this.wheelStepGroupBox = new System.Windows.Forms.GroupBox();
            this.seaLevelWheelStepNumeric = new System.Windows.Forms.NumericUpDown();
            this.seaLevelWheelStepLabel = new System.Windows.Forms.Label();
            this.voidCursorSizeWheelStepNumeric = new System.Windows.Forms.NumericUpDown();
            this.voidCursorSizeWheelStepLabel = new System.Windows.Forms.Label();
            this.heightCursorSizeWheelStepNumeric = new System.Windows.Forms.NumericUpDown();
            this.heightCursorSizeWheelStepLabel = new System.Windows.Forms.Label();
            this.heightIntervalWheelStepNumeric = new System.Windows.Forms.NumericUpDown();
            this.heightIntervalWheelStepLabel = new System.Windows.Forms.Label();
            this.heightSelectedHeightWheelStepNumeric = new System.Windows.Forms.NumericUpDown();
            this.heightSelectedHeightWheelStepLabel = new System.Windows.Forms.Label();
            this.sidePanel.SuspendLayout();
            this.bottomPanel.SuspendLayout();
            this.searchPathsPanel.SuspendLayout();
            this.mainGroupBox.SuspendLayout();
            this.scrollSpeedGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dragScrollSpeedYNumeric)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dragScrollSpeedXNumeric)).BeginInit();
            this.missionGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.inactiveSchemaOpacityNumeric)).BeginInit();
            this.resourceNamesGroupBox.SuspendLayout();
            this.miscGroupBox.SuspendLayout();
            this.preferencesTabControl.SuspendLayout();
            this.miscTabPage.SuspendLayout();
            this.missionTabPage.SuspendLayout();
            this.adjustmentsTabPage.SuspendLayout();
            this.wheelStepGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.seaLevelWheelStepNumeric)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.voidCursorSizeWheelStepNumeric)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.heightCursorSizeWheelStepNumeric)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.heightIntervalWheelStepNumeric)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.heightSelectedHeightWheelStepNumeric)).BeginInit();
            this.SuspendLayout();
            // 
            // searchPathsListView
            // 
            this.searchPathsListView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.searchPathsListView.HideSelection = false;
            this.searchPathsListView.Location = new System.Drawing.Point(0, 0);
            this.searchPathsListView.MultiSelect = false;
            this.searchPathsListView.Name = "searchPathsListView";
            this.searchPathsListView.Size = new System.Drawing.Size(332, 190);
            this.searchPathsListView.TabIndex = 0;
            this.searchPathsListView.UseCompatibleStateImageBehavior = false;
            this.searchPathsListView.View = System.Windows.Forms.View.List;
            // 
            // addButton
            // 
            this.addButton.Location = new System.Drawing.Point(3, 83);
            this.addButton.Name = "addButton";
            this.addButton.Size = new System.Drawing.Size(75, 21);
            this.addButton.TabIndex = 1;
            this.addButton.Text = "Add...";
            this.addButton.UseVisualStyleBackColor = true;
            this.addButton.Click += new System.EventHandler(this.AddButtonClick);
            // 
            // removeButton
            // 
            this.removeButton.Location = new System.Drawing.Point(3, 112);
            this.removeButton.Name = "removeButton";
            this.removeButton.Size = new System.Drawing.Size(75, 21);
            this.removeButton.TabIndex = 2;
            this.removeButton.Text = "Remove";
            this.removeButton.UseVisualStyleBackColor = true;
            this.removeButton.Click += new System.EventHandler(this.RemoveButtonClick);
            // 
            // upButton
            // 
            this.upButton.Location = new System.Drawing.Point(3, 3);
            this.upButton.Name = "upButton";
            this.upButton.Size = new System.Drawing.Size(75, 23);
            this.upButton.TabIndex = 3;
            this.upButton.Text = "Move Up";
            this.upButton.UseVisualStyleBackColor = true;
            this.upButton.Click += new System.EventHandler(this.UpButtonClick);
            // 
            // downButton
            // 
            this.downButton.Location = new System.Drawing.Point(3, 32);
            this.downButton.Name = "downButton";
            this.downButton.Size = new System.Drawing.Size(75, 23);
            this.downButton.TabIndex = 4;
            this.downButton.Text = "Move Down";
            this.downButton.UseVisualStyleBackColor = true;
            this.downButton.Click += new System.EventHandler(this.DownButtonClick);
            // 
            // okButton
            // 
            this.okButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.okButton.Location = new System.Drawing.Point(291, 4);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(75, 23);
            this.okButton.TabIndex = 5;
            this.okButton.Text = "OK";
            this.okButton.UseVisualStyleBackColor = true;
            this.okButton.Click += new System.EventHandler(this.OkButtonClick);
            // 
            // cancelButton
            // 
            this.cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.Location = new System.Drawing.Point(372, 4);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(75, 23);
            this.cancelButton.TabIndex = 6;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            // 
            // sidePanel
            // 
            this.sidePanel.Controls.Add(this.upButton);
            this.sidePanel.Controls.Add(this.downButton);
            this.sidePanel.Controls.Add(this.addButton);
            this.sidePanel.Controls.Add(this.removeButton);
            this.sidePanel.Dock = System.Windows.Forms.DockStyle.Right;
            this.sidePanel.Location = new System.Drawing.Point(332, 0);
            this.sidePanel.Name = "sidePanel";
            this.sidePanel.Size = new System.Drawing.Size(82, 190);
            this.sidePanel.TabIndex = 7;
            // 
            // bottomPanel
            // 
            this.bottomPanel.Controls.Add(this.cancelButton);
            this.bottomPanel.Controls.Add(this.okButton);
            this.bottomPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.bottomPanel.Location = new System.Drawing.Point(0, 426);
            this.bottomPanel.Name = "bottomPanel";
            this.bottomPanel.Size = new System.Drawing.Size(450, 30);
            this.bottomPanel.TabIndex = 8;
            // 
            // searchPathsPanel
            // 
            this.searchPathsPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.searchPathsPanel.Controls.Add(this.searchPathsListView);
            this.searchPathsPanel.Controls.Add(this.sidePanel);
            this.searchPathsPanel.Location = new System.Drawing.Point(6, 19);
            this.searchPathsPanel.Name = "searchPathsPanel";
            this.searchPathsPanel.Size = new System.Drawing.Size(414, 190);
            this.searchPathsPanel.TabIndex = 9;
            // 
            // mainGroupBox
            // 
            this.mainGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.mainGroupBox.Controls.Add(this.searchPathsPanel);
            this.mainGroupBox.Location = new System.Drawing.Point(12, 12);
            this.mainGroupBox.Name = "mainGroupBox";
            this.mainGroupBox.Size = new System.Drawing.Size(426, 215);
            this.mainGroupBox.TabIndex = 0;
            this.mainGroupBox.TabStop = false;
            this.mainGroupBox.Text = "Search Paths";
            // 
            // scrollSpeedGroupBox
            // 
            this.scrollSpeedGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.scrollSpeedGroupBox.Controls.Add(this.dragScrollSpeedYNumeric);
            this.scrollSpeedGroupBox.Controls.Add(this.dragScrollSpeedXNumeric);
            this.scrollSpeedGroupBox.Controls.Add(this.dragScrollSpeedYLabel);
            this.scrollSpeedGroupBox.Controls.Add(this.dragScrollSpeedXLabel);
            this.scrollSpeedGroupBox.Location = new System.Drawing.Point(12, 211);
            this.scrollSpeedGroupBox.Name = "scrollSpeedGroupBox";
            this.scrollSpeedGroupBox.Size = new System.Drawing.Size(426, 114);
            this.scrollSpeedGroupBox.TabIndex = 1;
            this.scrollSpeedGroupBox.TabStop = false;
            this.scrollSpeedGroupBox.Text = "Viewport Drag Auto-Scroll";
            // 
            // dragScrollSpeedYNumeric
            // 
            this.dragScrollSpeedYNumeric.Location = new System.Drawing.Point(233, 71);
            this.dragScrollSpeedYNumeric.Maximum = new decimal(new int[] {
            256,
            0,
            0,
            0});
            this.dragScrollSpeedYNumeric.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.dragScrollSpeedYNumeric.Name = "dragScrollSpeedYNumeric";
            this.dragScrollSpeedYNumeric.Size = new System.Drawing.Size(82, 20);
            this.dragScrollSpeedYNumeric.TabIndex = 3;
            this.dragScrollSpeedYNumeric.Value = new decimal(new int[] {
            16,
            0,
            0,
            0});
            // 
            // dragScrollSpeedXNumeric
            // 
            this.dragScrollSpeedXNumeric.Location = new System.Drawing.Point(233, 34);
            this.dragScrollSpeedXNumeric.Maximum = new decimal(new int[] {
            256,
            0,
            0,
            0});
            this.dragScrollSpeedXNumeric.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.dragScrollSpeedXNumeric.Name = "dragScrollSpeedXNumeric";
            this.dragScrollSpeedXNumeric.Size = new System.Drawing.Size(82, 20);
            this.dragScrollSpeedXNumeric.TabIndex = 1;
            this.dragScrollSpeedXNumeric.Value = new decimal(new int[] {
            16,
            0,
            0,
            0});
            // 
            // dragScrollSpeedYLabel
            // 
            this.dragScrollSpeedYLabel.AutoSize = true;
            this.dragScrollSpeedYLabel.Location = new System.Drawing.Point(14, 73);
            this.dragScrollSpeedYLabel.Name = "dragScrollSpeedYLabel";
            this.dragScrollSpeedYLabel.Size = new System.Drawing.Size(77, 13);
            this.dragScrollSpeedYLabel.TabIndex = 2;
            this.dragScrollSpeedYLabel.Text = "Vertical speed:";
            // 
            // dragScrollSpeedXLabel
            // 
            this.dragScrollSpeedXLabel.AutoSize = true;
            this.dragScrollSpeedXLabel.Location = new System.Drawing.Point(14, 36);
            this.dragScrollSpeedXLabel.Name = "dragScrollSpeedXLabel";
            this.dragScrollSpeedXLabel.Size = new System.Drawing.Size(89, 13);
            this.dragScrollSpeedXLabel.TabIndex = 0;
            this.dragScrollSpeedXLabel.Text = "Horizontal speed:";
            // 
            // missionGroupBox
            // 
            this.missionGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.missionGroupBox.Controls.Add(this.showUnitFriendlyNameOnMapCheckBox);
            this.missionGroupBox.Controls.Add(this.showUnitFriendlyNameFirstCheckBox);
            this.missionGroupBox.Controls.Add(this.inactiveSchemaOpacityNumeric);
            this.missionGroupBox.Controls.Add(this.inactiveSchemaOpacityLabel);
            this.missionGroupBox.Location = new System.Drawing.Point(12, 12);
            this.missionGroupBox.Name = "missionGroupBox";
            this.missionGroupBox.Size = new System.Drawing.Size(426, 108);
            this.missionGroupBox.TabIndex = 0;
            this.missionGroupBox.TabStop = false;
            this.missionGroupBox.Text = "Mission";
            // 
            // showUnitFriendlyNameOnMapCheckBox
            // 
            this.showUnitFriendlyNameOnMapCheckBox.AutoSize = true;
            this.showUnitFriendlyNameOnMapCheckBox.Location = new System.Drawing.Point(17, 73);
            this.showUnitFriendlyNameOnMapCheckBox.Name = "showUnitFriendlyNameOnMapCheckBox";
            this.showUnitFriendlyNameOnMapCheckBox.Size = new System.Drawing.Size(159, 17);
            this.showUnitFriendlyNameOnMapCheckBox.TabIndex = 3;
            this.showUnitFriendlyNameOnMapCheckBox.Text = "Prioritise friendly name (map)";
            this.showUnitFriendlyNameOnMapCheckBox.UseVisualStyleBackColor = true;
            // 
            // showUnitFriendlyNameFirstCheckBox
            // 
            this.showUnitFriendlyNameFirstCheckBox.AutoSize = true;
            this.showUnitFriendlyNameFirstCheckBox.Location = new System.Drawing.Point(17, 50);
            this.showUnitFriendlyNameFirstCheckBox.Name = "showUnitFriendlyNameFirstCheckBox";
            this.showUnitFriendlyNameFirstCheckBox.Size = new System.Drawing.Size(154, 17);
            this.showUnitFriendlyNameFirstCheckBox.TabIndex = 2;
            this.showUnitFriendlyNameFirstCheckBox.Text = "Prioritise friendly name (tab)";
            this.showUnitFriendlyNameFirstCheckBox.UseVisualStyleBackColor = true;
            // 
            // inactiveSchemaOpacityNumeric
            // 
            this.inactiveSchemaOpacityNumeric.Location = new System.Drawing.Point(233, 20);
            this.inactiveSchemaOpacityNumeric.Name = "inactiveSchemaOpacityNumeric";
            this.inactiveSchemaOpacityNumeric.Size = new System.Drawing.Size(82, 20);
            this.inactiveSchemaOpacityNumeric.TabIndex = 1;
            this.inactiveSchemaOpacityNumeric.Value = new decimal(new int[] {
            38,
            0,
            0,
            0});
            // 
            // inactiveSchemaOpacityLabel
            // 
            this.inactiveSchemaOpacityLabel.AutoSize = true;
            this.inactiveSchemaOpacityLabel.Location = new System.Drawing.Point(14, 22);
            this.inactiveSchemaOpacityLabel.Name = "inactiveSchemaOpacityLabel";
            this.inactiveSchemaOpacityLabel.Size = new System.Drawing.Size(142, 13);
            this.inactiveSchemaOpacityLabel.TabIndex = 0;
            this.inactiveSchemaOpacityLabel.Text = "Inactive schema opacity (%):";
            // 
            // resourceNamesGroupBox
            // 
            this.resourceNamesGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.resourceNamesGroupBox.Controls.Add(this.calculatedMetalDepositValueCheckBox);
            this.resourceNamesGroupBox.Controls.Add(this.featureReclaimAmountsCheckBox);
            this.resourceNamesGroupBox.Controls.Add(this.fullResourceNamesCheckBox);
            this.resourceNamesGroupBox.Location = new System.Drawing.Point(12, 233);
            this.resourceNamesGroupBox.Name = "resourceNamesGroupBox";
            this.resourceNamesGroupBox.Size = new System.Drawing.Size(426, 95);
            this.resourceNamesGroupBox.TabIndex = 1;
            this.resourceNamesGroupBox.TabStop = false;
            this.resourceNamesGroupBox.Text = "Resource Labels";
            // 
            // calculatedMetalDepositValueCheckBox
            // 
            this.calculatedMetalDepositValueCheckBox.AutoSize = true;
            this.calculatedMetalDepositValueCheckBox.Location = new System.Drawing.Point(17, 68);
            this.calculatedMetalDepositValueCheckBox.Name = "calculatedMetalDepositValueCheckBox";
            this.calculatedMetalDepositValueCheckBox.Size = new System.Drawing.Size(170, 17);
            this.calculatedMetalDepositValueCheckBox.TabIndex = 2;
            this.calculatedMetalDepositValueCheckBox.Text = "Calculated metal deposit value";
            this.calculatedMetalDepositValueCheckBox.UseVisualStyleBackColor = true;
            // 
            // featureReclaimAmountsCheckBox
            // 
            this.featureReclaimAmountsCheckBox.AutoSize = true;
            this.featureReclaimAmountsCheckBox.Location = new System.Drawing.Point(17, 45);
            this.featureReclaimAmountsCheckBox.Name = "featureReclaimAmountsCheckBox";
            this.featureReclaimAmountsCheckBox.Size = new System.Drawing.Size(168, 17);
            this.featureReclaimAmountsCheckBox.TabIndex = 1;
            this.featureReclaimAmountsCheckBox.Text = "Show feature reclaim amounts";
            this.featureReclaimAmountsCheckBox.UseVisualStyleBackColor = true;
            // 
            // fullResourceNamesCheckBox
            // 
            this.fullResourceNamesCheckBox.AutoSize = true;
            this.fullResourceNamesCheckBox.Location = new System.Drawing.Point(17, 22);
            this.fullResourceNamesCheckBox.Name = "fullResourceNamesCheckBox";
            this.fullResourceNamesCheckBox.Size = new System.Drawing.Size(120, 17);
            this.fullResourceNamesCheckBox.TabIndex = 0;
            this.fullResourceNamesCheckBox.Text = "Full resource names";
            this.fullResourceNamesCheckBox.UseVisualStyleBackColor = true;
            // 
            // miscGroupBox
            // 
            this.miscGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.miscGroupBox.Controls.Add(this.doNotPromptToSaveUnsavedChangesCheckBox);
            this.miscGroupBox.Location = new System.Drawing.Point(12, 334);
            this.miscGroupBox.Name = "miscGroupBox";
            this.miscGroupBox.Size = new System.Drawing.Size(426, 50);
            this.miscGroupBox.TabIndex = 2;
            this.miscGroupBox.TabStop = false;
            this.miscGroupBox.Text = "Misc";
            // 
            // doNotPromptToSaveUnsavedChangesCheckBox
            // 
            this.doNotPromptToSaveUnsavedChangesCheckBox.AutoSize = true;
            this.doNotPromptToSaveUnsavedChangesCheckBox.Location = new System.Drawing.Point(17, 22);
            this.doNotPromptToSaveUnsavedChangesCheckBox.Name = "doNotPromptToSaveUnsavedChangesCheckBox";
            this.doNotPromptToSaveUnsavedChangesCheckBox.Size = new System.Drawing.Size(219, 17);
            this.doNotPromptToSaveUnsavedChangesCheckBox.TabIndex = 0;
            this.doNotPromptToSaveUnsavedChangesCheckBox.Text = "Do not prompt to save unsaved changes";
            this.doNotPromptToSaveUnsavedChangesCheckBox.UseVisualStyleBackColor = true;
            // 
            // preferencesTabControl
            // 
            this.preferencesTabControl.Controls.Add(this.miscTabPage);
            this.preferencesTabControl.Controls.Add(this.missionTabPage);
            this.preferencesTabControl.Controls.Add(this.adjustmentsTabPage);
            this.preferencesTabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.preferencesTabControl.Location = new System.Drawing.Point(0, 0);
            this.preferencesTabControl.Name = "preferencesTabControl";
            this.preferencesTabControl.SelectedIndex = 0;
            this.preferencesTabControl.Size = new System.Drawing.Size(450, 426);
            this.preferencesTabControl.TabIndex = 0;
            // 
            // miscTabPage
            // 
            this.miscTabPage.AutoScroll = true;
            this.miscTabPage.Controls.Add(this.miscGroupBox);
            this.miscTabPage.Controls.Add(this.resourceNamesGroupBox);
            this.miscTabPage.Controls.Add(this.mainGroupBox);
            this.miscTabPage.Location = new System.Drawing.Point(4, 22);
            this.miscTabPage.Name = "miscTabPage";
            this.miscTabPage.Padding = new System.Windows.Forms.Padding(3);
            this.miscTabPage.Size = new System.Drawing.Size(442, 400);
            this.miscTabPage.TabIndex = 0;
            this.miscTabPage.Text = "Misc";
            this.miscTabPage.UseVisualStyleBackColor = true;
            // 
            // missionTabPage
            // 
            this.missionTabPage.AutoScroll = true;
            this.missionTabPage.Controls.Add(this.missionGroupBox);
            this.missionTabPage.Location = new System.Drawing.Point(4, 22);
            this.missionTabPage.Name = "missionTabPage";
            this.missionTabPage.Padding = new System.Windows.Forms.Padding(3);
            this.missionTabPage.Size = new System.Drawing.Size(442, 400);
            this.missionTabPage.TabIndex = 1;
            this.missionTabPage.Text = "Mission";
            this.missionTabPage.UseVisualStyleBackColor = true;
            // 
            // adjustmentsTabPage
            // 
            this.adjustmentsTabPage.AutoScroll = true;
            this.adjustmentsTabPage.Controls.Add(this.scrollSpeedGroupBox);
            this.adjustmentsTabPage.Controls.Add(this.wheelStepGroupBox);
            this.adjustmentsTabPage.Location = new System.Drawing.Point(4, 22);
            this.adjustmentsTabPage.Name = "adjustmentsTabPage";
            this.adjustmentsTabPage.Padding = new System.Windows.Forms.Padding(3);
            this.adjustmentsTabPage.Size = new System.Drawing.Size(442, 400);
            this.adjustmentsTabPage.TabIndex = 2;
            this.adjustmentsTabPage.Text = "Adjustments";
            this.adjustmentsTabPage.UseVisualStyleBackColor = true;
            // 
            // wheelStepGroupBox
            // 
            this.wheelStepGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.wheelStepGroupBox.Controls.Add(this.seaLevelWheelStepNumeric);
            this.wheelStepGroupBox.Controls.Add(this.seaLevelWheelStepLabel);
            this.wheelStepGroupBox.Controls.Add(this.voidCursorSizeWheelStepNumeric);
            this.wheelStepGroupBox.Controls.Add(this.voidCursorSizeWheelStepLabel);
            this.wheelStepGroupBox.Controls.Add(this.heightCursorSizeWheelStepNumeric);
            this.wheelStepGroupBox.Controls.Add(this.heightCursorSizeWheelStepLabel);
            this.wheelStepGroupBox.Controls.Add(this.heightIntervalWheelStepNumeric);
            this.wheelStepGroupBox.Controls.Add(this.heightIntervalWheelStepLabel);
            this.wheelStepGroupBox.Controls.Add(this.heightSelectedHeightWheelStepNumeric);
            this.wheelStepGroupBox.Controls.Add(this.heightSelectedHeightWheelStepLabel);
            this.wheelStepGroupBox.Location = new System.Drawing.Point(12, 12);
            this.wheelStepGroupBox.Name = "wheelStepGroupBox";
            this.wheelStepGroupBox.Size = new System.Drawing.Size(426, 186);
            this.wheelStepGroupBox.TabIndex = 0;
            this.wheelStepGroupBox.TabStop = false;
            this.wheelStepGroupBox.Text = "Mouse wheel step";
            // 
            // seaLevelWheelStepNumeric
            // 
            this.seaLevelWheelStepNumeric.Location = new System.Drawing.Point(315, 147);
            this.seaLevelWheelStepNumeric.Maximum = new decimal(new int[] {
            255,
            0,
            0,
            0});
            this.seaLevelWheelStepNumeric.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.seaLevelWheelStepNumeric.Name = "seaLevelWheelStepNumeric";
            this.seaLevelWheelStepNumeric.Size = new System.Drawing.Size(70, 20);
            this.seaLevelWheelStepNumeric.TabIndex = 9;
            this.seaLevelWheelStepNumeric.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // seaLevelWheelStepLabel
            // 
            this.seaLevelWheelStepLabel.AutoSize = true;
            this.seaLevelWheelStepLabel.Location = new System.Drawing.Point(11, 150);
            this.seaLevelWheelStepLabel.Name = "seaLevelWheelStepLabel";
            this.seaLevelWheelStepLabel.Size = new System.Drawing.Size(153, 13);
            this.seaLevelWheelStepLabel.TabIndex = 8;
            this.seaLevelWheelStepLabel.Text = "Attributes tab — sea level slider:";
            // 
            // voidCursorSizeWheelStepNumeric
            // 
            this.voidCursorSizeWheelStepNumeric.Location = new System.Drawing.Point(315, 115);
            this.voidCursorSizeWheelStepNumeric.Maximum = new decimal(new int[] {
            255,
            0,
            0,
            0});
            this.voidCursorSizeWheelStepNumeric.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.voidCursorSizeWheelStepNumeric.Name = "voidCursorSizeWheelStepNumeric";
            this.voidCursorSizeWheelStepNumeric.Size = new System.Drawing.Size(70, 20);
            this.voidCursorSizeWheelStepNumeric.TabIndex = 7;
            this.voidCursorSizeWheelStepNumeric.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // voidCursorSizeWheelStepLabel
            // 
            this.voidCursorSizeWheelStepLabel.AutoSize = true;
            this.voidCursorSizeWheelStepLabel.Location = new System.Drawing.Point(11, 118);
            this.voidCursorSizeWheelStepLabel.Name = "voidCursorSizeWheelStepLabel";
            this.voidCursorSizeWheelStepLabel.Size = new System.Drawing.Size(111, 13);
            this.voidCursorSizeWheelStepLabel.TabIndex = 6;
            this.voidCursorSizeWheelStepLabel.Text = "Void tab — cursor size:";
            // 
            // heightCursorSizeWheelStepNumeric
            // 
            this.heightCursorSizeWheelStepNumeric.Location = new System.Drawing.Point(315, 83);
            this.heightCursorSizeWheelStepNumeric.Maximum = new decimal(new int[] {
            255,
            0,
            0,
            0});
            this.heightCursorSizeWheelStepNumeric.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.heightCursorSizeWheelStepNumeric.Name = "heightCursorSizeWheelStepNumeric";
            this.heightCursorSizeWheelStepNumeric.Size = new System.Drawing.Size(70, 20);
            this.heightCursorSizeWheelStepNumeric.TabIndex = 5;
            this.heightCursorSizeWheelStepNumeric.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // heightCursorSizeWheelStepLabel
            // 
            this.heightCursorSizeWheelStepLabel.AutoSize = true;
            this.heightCursorSizeWheelStepLabel.Location = new System.Drawing.Point(11, 86);
            this.heightCursorSizeWheelStepLabel.Name = "heightCursorSizeWheelStepLabel";
            this.heightCursorSizeWheelStepLabel.Size = new System.Drawing.Size(121, 13);
            this.heightCursorSizeWheelStepLabel.TabIndex = 4;
            this.heightCursorSizeWheelStepLabel.Text = "Height tab — cursor size:";
            // 
            // heightIntervalWheelStepNumeric
            // 
            this.heightIntervalWheelStepNumeric.Location = new System.Drawing.Point(315, 51);
            this.heightIntervalWheelStepNumeric.Maximum = new decimal(new int[] {
            255,
            0,
            0,
            0});
            this.heightIntervalWheelStepNumeric.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.heightIntervalWheelStepNumeric.Name = "heightIntervalWheelStepNumeric";
            this.heightIntervalWheelStepNumeric.Size = new System.Drawing.Size(70, 20);
            this.heightIntervalWheelStepNumeric.TabIndex = 3;
            this.heightIntervalWheelStepNumeric.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // heightIntervalWheelStepLabel
            // 
            this.heightIntervalWheelStepLabel.AutoSize = true;
            this.heightIntervalWheelStepLabel.Location = new System.Drawing.Point(11, 54);
            this.heightIntervalWheelStepLabel.Name = "heightIntervalWheelStepLabel";
            this.heightIntervalWheelStepLabel.Size = new System.Drawing.Size(105, 13);
            this.heightIntervalWheelStepLabel.TabIndex = 2;
            this.heightIntervalWheelStepLabel.Text = "Height tab — interval:";
            // 
            // heightSelectedHeightWheelStepNumeric
            // 
            this.heightSelectedHeightWheelStepNumeric.Location = new System.Drawing.Point(315, 19);
            this.heightSelectedHeightWheelStepNumeric.Maximum = new decimal(new int[] {
            255,
            0,
            0,
            0});
            this.heightSelectedHeightWheelStepNumeric.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.heightSelectedHeightWheelStepNumeric.Name = "heightSelectedHeightWheelStepNumeric";
            this.heightSelectedHeightWheelStepNumeric.Size = new System.Drawing.Size(70, 20);
            this.heightSelectedHeightWheelStepNumeric.TabIndex = 1;
            this.heightSelectedHeightWheelStepNumeric.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // heightSelectedHeightWheelStepLabel
            // 
            this.heightSelectedHeightWheelStepLabel.AutoSize = true;
            this.heightSelectedHeightWheelStepLabel.Location = new System.Drawing.Point(11, 22);
            this.heightSelectedHeightWheelStepLabel.Name = "heightSelectedHeightWheelStepLabel";
            this.heightSelectedHeightWheelStepLabel.Size = new System.Drawing.Size(143, 13);
            this.heightSelectedHeightWheelStepLabel.TabIndex = 0;
            this.heightSelectedHeightWheelStepLabel.Text = "Height tab — selected height:";
            // 
            // PreferencesForm
            // 
            this.AcceptButton = this.okButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.cancelButton;
            this.ClientSize = new System.Drawing.Size(450, 456);
            this.Controls.Add(this.preferencesTabControl);
            this.Controls.Add(this.bottomPanel);
            this.Name = "PreferencesForm";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Preferences";
            this.Load += new System.EventHandler(this.PreferencesFormLoad);
            this.sidePanel.ResumeLayout(false);
            this.bottomPanel.ResumeLayout(false);
            this.searchPathsPanel.ResumeLayout(false);
            this.mainGroupBox.ResumeLayout(false);
            this.scrollSpeedGroupBox.ResumeLayout(false);
            this.scrollSpeedGroupBox.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dragScrollSpeedYNumeric)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dragScrollSpeedXNumeric)).EndInit();
            this.missionGroupBox.ResumeLayout(false);
            this.missionGroupBox.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.inactiveSchemaOpacityNumeric)).EndInit();
            this.resourceNamesGroupBox.ResumeLayout(false);
            this.resourceNamesGroupBox.PerformLayout();
            this.miscGroupBox.ResumeLayout(false);
            this.miscGroupBox.PerformLayout();
            this.preferencesTabControl.ResumeLayout(false);
            this.miscTabPage.ResumeLayout(false);
            this.missionTabPage.ResumeLayout(false);
            this.adjustmentsTabPage.ResumeLayout(false);
            this.wheelStepGroupBox.ResumeLayout(false);
            this.wheelStepGroupBox.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.seaLevelWheelStepNumeric)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.voidCursorSizeWheelStepNumeric)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.heightCursorSizeWheelStepNumeric)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.heightIntervalWheelStepNumeric)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.heightSelectedHeightWheelStepNumeric)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ListView searchPathsListView;
        private System.Windows.Forms.Button addButton;
        private System.Windows.Forms.Button removeButton;
        private System.Windows.Forms.Button upButton;
        private System.Windows.Forms.Button downButton;
        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.Panel sidePanel;
        private System.Windows.Forms.Panel bottomPanel;
        private System.Windows.Forms.Panel searchPathsPanel;
        private System.Windows.Forms.GroupBox mainGroupBox;
        private System.Windows.Forms.GroupBox scrollSpeedGroupBox;
        private System.Windows.Forms.Label dragScrollSpeedYLabel;
        private System.Windows.Forms.Label dragScrollSpeedXLabel;
        private System.Windows.Forms.NumericUpDown dragScrollSpeedYNumeric;
        private System.Windows.Forms.NumericUpDown dragScrollSpeedXNumeric;
        private System.Windows.Forms.GroupBox missionGroupBox;
        private System.Windows.Forms.Label inactiveSchemaOpacityLabel;
        private System.Windows.Forms.NumericUpDown inactiveSchemaOpacityNumeric;
        private System.Windows.Forms.CheckBox showUnitFriendlyNameFirstCheckBox;
        private System.Windows.Forms.CheckBox showUnitFriendlyNameOnMapCheckBox;
        private System.Windows.Forms.GroupBox resourceNamesGroupBox;
        private System.Windows.Forms.CheckBox fullResourceNamesCheckBox;
        private System.Windows.Forms.CheckBox featureReclaimAmountsCheckBox;
        private System.Windows.Forms.CheckBox calculatedMetalDepositValueCheckBox;
        private System.Windows.Forms.GroupBox miscGroupBox;
        private System.Windows.Forms.CheckBox doNotPromptToSaveUnsavedChangesCheckBox;
        private System.Windows.Forms.TabControl preferencesTabControl;
        private System.Windows.Forms.TabPage miscTabPage;
        private System.Windows.Forms.TabPage missionTabPage;
        private System.Windows.Forms.TabPage adjustmentsTabPage;
        private System.Windows.Forms.GroupBox wheelStepGroupBox;
        private System.Windows.Forms.Label heightSelectedHeightWheelStepLabel;
        private System.Windows.Forms.NumericUpDown heightSelectedHeightWheelStepNumeric;
        private System.Windows.Forms.Label heightIntervalWheelStepLabel;
        private System.Windows.Forms.NumericUpDown heightIntervalWheelStepNumeric;
        private System.Windows.Forms.Label heightCursorSizeWheelStepLabel;
        private System.Windows.Forms.NumericUpDown heightCursorSizeWheelStepNumeric;
        private System.Windows.Forms.Label voidCursorSizeWheelStepLabel;
        private System.Windows.Forms.NumericUpDown voidCursorSizeWheelStepNumeric;
        private System.Windows.Forms.Label seaLevelWheelStepLabel;
        private System.Windows.Forms.NumericUpDown seaLevelWheelStepNumeric;
    }
}
