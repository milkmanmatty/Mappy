namespace Mappy.UI.Forms
{
    using System;
    using System.Drawing;
    using System.Windows.Forms;

    using Mappy;
    using Ookii.Dialogs;

    public partial class PreferencesForm : Form
    {
        private Color blobFeatureBaseColor = Configuration.DefaultBlobFeatureBaseColor;

        private bool blobFeatureBaseColorCustomized;
        public PreferencesForm()
        {
            this.InitializeComponent();
        }

        private void PreferencesFormLoad(object sender, EventArgs e)
        {
            if (MappySettings.Settings.SearchPaths != null)
            {
                foreach (var dir in MappySettings.Settings.SearchPaths)
                {
                    var i = new ListViewItem(dir);
                    this.searchPathsListView.Items.Add(i);
                }
            }

            this.dragScrollSpeedXNumeric.Value = MappySettings.Settings.GetDragAutoScrollSpeedXOrDefault();
            this.dragScrollSpeedYNumeric.Value = MappySettings.Settings.GetDragAutoScrollSpeedYOrDefault();
            this.fullResourceNamesCheckBox.Checked = MappySettings.Settings.FullResourceNames;
            this.featureReclaimAmountsCheckBox.Checked = MappySettings.Settings.ShowFeatureReclaimAmounts;
            this.calculatedMetalDepositValueCheckBox.Checked = MappySettings.Settings.ShowCalculatedMetalDepositValue;
            this.showUnitFriendlyNameFirstCheckBox.Checked = MappySettings.Settings.ShowUnitFriendlyNameFirst;
            this.showUnitFriendlyNameOnMapCheckBox.Checked = MappySettings.Settings.ShowUnitFriendlyNameOnMap;
            this.inactiveSchemaOpacityNumeric.Value = MappySettings.Settings.GetInactiveSchemaOpacityPercentForDialog();
            this.doNotPromptToSaveUnsavedChangesCheckBox.Checked = MappySettings.Settings.DoNotPromptToSaveUnsavedChanges;

            var settings = MappySettings.Settings;
            this.heightSelectedHeightWheelStepNumeric.Value = settings.GetHeightSelectedHeightWheelStepOrDefault();
            this.heightIntervalWheelStepNumeric.Value = settings.GetHeightIntervalWheelStepOrDefault();
            this.heightCursorSizeWheelStepNumeric.Value = settings.GetHeightCursorSizeWheelStepOrDefault();
            this.voidCursorSizeWheelStepNumeric.Value = settings.GetVoidCursorSizeWheelStepOrDefault();
            this.seaLevelWheelStepNumeric.Value = settings.GetSeaLevelWheelStepOrDefault();
            this.stickyClipboardCheckBox.Checked = settings.StickyClipboard;
            this.blobFeatureBaseCheckBox.Checked = settings.BlobFeatureBase;
            this.blobFeatureBaseColor = settings.GetBlobFeatureBaseColorOrDefault();
            this.blobFeatureBaseColorCustomized = settings.BlobFeatureBaseColorArgb.HasValue;

            this.defaultHeightmapVisibleCheckBox.Checked = settings.GetDefaultHeightmapVisibleOrDefault();
            this.defaultHeightGridVisibleCheckBox.Checked = settings.GetDefaultHeightGridVisibleOrDefault();
            this.defaultMinimapVisibleCheckBox.Checked = settings.GetDefaultMinimapVisibleOrDefault();
            this.defaultVoidsVisibleCheckBox.Checked = settings.GetDefaultVoidsVisibleOrDefault();
            this.defaultGridVisibleCheckBox.Checked = settings.GetDefaultGridVisibleOrDefault();
            this.defaultFeaturesVisibleCheckBox.Checked = settings.GetDefaultFeaturesVisibleOrDefault();
        }

        private void BlobFeatureBaseCustomizeButtonClick(object sender, EventArgs e)
        {
            using (var colorDialog = new ColorDialog())
            {
                colorDialog.Color = this.blobFeatureBaseColor;
                colorDialog.FullOpen = true;
                if (colorDialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                this.blobFeatureBaseColor = colorDialog.Color;
                this.blobFeatureBaseColorCustomized = true;
            }
        }

        private void AddButtonClick(object sender, EventArgs e)
        {
            var d = new VistaFolderBrowserDialog();
            var r = d.ShowDialog(this);
            if (r == DialogResult.OK)
            {
                var i = new ListViewItem(d.SelectedPath);
                this.searchPathsListView.Items.Add(i);
                i.Selected = true;
            }

            this.searchPathsListView.Focus();
        }

        private void RemoveButtonClick(object sender, EventArgs e)
        {
            var selectedIndices = this.searchPathsListView.SelectedIndices;
            if (selectedIndices.Count > 0)
            {
                var i = selectedIndices[0];
                this.searchPathsListView.Items.RemoveAt(i);

                if (this.searchPathsListView.Items.Count > 0)
                {
                    this.searchPathsListView.Items[Math.Max(i - 1, 0)].Selected = true;
                    this.searchPathsListView.Focus();
                }
            }
        }

        private void UpButtonClick(object sender, EventArgs e)
        {
            foreach (int i in this.searchPathsListView.SelectedIndices)
            {
                if (i == 0)
                {
                    this.searchPathsListView.Focus();
                    continue;
                }

                var tmp = this.searchPathsListView.Items[i];
                this.searchPathsListView.Items.RemoveAt(i);
                this.searchPathsListView.Items.Insert(i - 1, tmp);

                this.searchPathsListView.Items[i - 1].Selected = true;
                this.searchPathsListView.Focus();
            }
        }

        private void DownButtonClick(object sender, EventArgs e)
        {
            foreach (int i in this.searchPathsListView.SelectedIndices)
            {
                if (i == this.searchPathsListView.Items.Count - 1)
                {
                    this.searchPathsListView.Focus();
                    continue;
                }

                var tmp = this.searchPathsListView.Items[i];
                this.searchPathsListView.Items.RemoveAt(i);
                this.searchPathsListView.Items.Insert(i + 1, tmp);

                this.searchPathsListView.Items[i + 1].Selected = true;
                this.searchPathsListView.Focus();
            }
        }

        private void OkButtonClick(object sender, EventArgs e)
        {
            var s = new System.Collections.Specialized.StringCollection();
            foreach (ListViewItem i in this.searchPathsListView.Items)
            {
                s.Add(i.Text);
            }

            MappySettings.Settings.SearchPaths = s;
            MappySettings.Settings.DragAutoScrollSpeedX = (int)this.dragScrollSpeedXNumeric.Value;
            MappySettings.Settings.DragAutoScrollSpeedY = (int)this.dragScrollSpeedYNumeric.Value;
            MappySettings.Settings.FullResourceNames = this.fullResourceNamesCheckBox.Checked;
            MappySettings.Settings.ShowFeatureReclaimAmounts = this.featureReclaimAmountsCheckBox.Checked;
            MappySettings.Settings.ShowCalculatedMetalDepositValue = this.calculatedMetalDepositValueCheckBox.Checked;
            MappySettings.Settings.ShowUnitFriendlyNameFirst = this.showUnitFriendlyNameFirstCheckBox.Checked;
            MappySettings.Settings.ShowUnitFriendlyNameOnMap = this.showUnitFriendlyNameOnMapCheckBox.Checked;
            MappySettings.Settings.InactiveSchemaOpacityPercent = (int)this.inactiveSchemaOpacityNumeric.Value;
            MappySettings.Settings.DoNotPromptToSaveUnsavedChanges = this.doNotPromptToSaveUnsavedChangesCheckBox.Checked;
            MappySettings.Settings.HeightSelectedHeightWheelStep = (int)this.heightSelectedHeightWheelStepNumeric.Value;
            MappySettings.Settings.HeightIntervalWheelStep = (int)this.heightIntervalWheelStepNumeric.Value;
            MappySettings.Settings.HeightCursorSizeWheelStep = (int)this.heightCursorSizeWheelStepNumeric.Value;
            MappySettings.Settings.VoidCursorSizeWheelStep = (int)this.voidCursorSizeWheelStepNumeric.Value;
            MappySettings.Settings.SeaLevelWheelStep = (int)this.seaLevelWheelStepNumeric.Value;
            MappySettings.Settings.StickyClipboard = this.stickyClipboardCheckBox.Checked;
            MappySettings.Settings.BlobFeatureBase = this.blobFeatureBaseCheckBox.Checked;
            MappySettings.Settings.BlobFeatureBaseColorArgb = this.blobFeatureBaseColorCustomized
                ? (int?)this.blobFeatureBaseColor.ToArgb()
                : null;
            MappySettings.Settings.DefaultHeightmapVisible = this.defaultHeightmapVisibleCheckBox.Checked;
            MappySettings.Settings.DefaultHeightGridVisible = this.defaultHeightGridVisibleCheckBox.Checked;
            MappySettings.Settings.DefaultMinimapVisible = this.defaultMinimapVisibleCheckBox.Checked;
            MappySettings.Settings.DefaultVoidsVisible = this.defaultVoidsVisibleCheckBox.Checked;
            MappySettings.Settings.DefaultGridVisible = this.defaultGridVisibleCheckBox.Checked;
            MappySettings.Settings.DefaultFeaturesVisible = this.defaultFeaturesVisibleCheckBox.Checked;
            MappySettings.SaveSettings(notifyListeners: true);
        }
    }
}
