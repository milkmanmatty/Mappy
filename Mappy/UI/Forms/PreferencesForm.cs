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

        private Color unitNameTextColor = Configuration.DefaultUnitNameTextColor;

        private bool unitNameTextColorCustomized;

        private Color unitNameBackplateColor = Configuration.DefaultUnitNameBackplateColor;

        private bool unitNameBackplateColorCustomized;

        private Color missionMovePathColor = Configuration.DefaultMissionMovePathColor;

        private bool missionMovePathColorCustomized;

        private Color missionAttackPathColor = Configuration.DefaultMissionAttackPathColor;

        private bool missionAttackPathColorCustomized;

        private Color missionPatrolPathColor = Configuration.DefaultMissionPatrolPathColor;

        private bool missionPatrolPathColorCustomized;

        private Color missionPatrolDashColor = Configuration.DefaultMissionPatrolDashColor;

        private bool missionPatrolDashColorCustomized;

        private Color missionWaitTextColor = Configuration.DefaultUnitNameTextColor;

        private bool missionWaitTextColorCustomized;

        private Color missionWaitBackplateColor = Configuration.DefaultUnitNameBackplateColor;

        private bool missionWaitBackplateColorCustomized;

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
            this.showUnitNameBackplateCheckBox.Checked = MappySettings.Settings.ShowUnitNameBackplate;
            this.unitNameTextColor = MappySettings.Settings.GetUnitNameTextColorOrDefault();
            this.unitNameTextColorCustomized = MappySettings.Settings.UnitNameTextColorArgb.HasValue;
            this.unitNameBackplateColor = MappySettings.Settings.GetUnitNameBackplateColorOrDefault();
            this.unitNameBackplateColorCustomized = MappySettings.Settings.UnitNameBackplateColorArgb.HasValue;
            this.missionMovePathColor = MappySettings.Settings.GetMissionMovePathColorOrDefault();
            this.missionMovePathColorCustomized = MappySettings.Settings.MissionMovePathColorArgb.HasValue;
            this.missionAttackPathColor = MappySettings.Settings.GetMissionAttackPathColorOrDefault();
            this.missionAttackPathColorCustomized = MappySettings.Settings.MissionAttackPathColorArgb.HasValue;
            this.missionPatrolPathColor = MappySettings.Settings.GetMissionPatrolPathColorOrDefault();
            this.missionPatrolPathColorCustomized = MappySettings.Settings.MissionPatrolPathColorArgb.HasValue;
            this.missionPatrolDashColor = MappySettings.Settings.GetMissionPatrolDashColorOrDefault();
            this.missionPatrolDashColorCustomized = MappySettings.Settings.MissionPatrolDashColorArgb.HasValue;
            this.missionWaitTextColor = MappySettings.Settings.GetMissionWaitTextColorOrDefault();
            this.missionWaitTextColorCustomized = MappySettings.Settings.MissionWaitTextColorArgb.HasValue;
            this.missionWaitBackplateColor = MappySettings.Settings.GetMissionWaitBackplateColorOrDefault();
            this.missionWaitBackplateColorCustomized = MappySettings.Settings.MissionWaitBackplateColorArgb.HasValue;
            this.inactiveSchemaOpacityNumeric.Value = MappySettings.Settings.GetInactiveSchemaOpacityPercentForDialog();
            this.doNotPromptToSaveUnsavedChangesCheckBox.Checked = MappySettings.Settings.DoNotPromptToSaveUnsavedChanges;
            this.splitTilesCheckBox.Checked = MappySettings.Settings.SplitTiles;

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

            var gridSize = settings.GetDefaultGridSizeOrDefault();
            var gridSizeText = gridSize + "x" + gridSize;
            var gridSizeIndex = this.defaultGridSizeComboBox.Items.IndexOf(gridSizeText);
            this.defaultGridSizeComboBox.SelectedIndex = gridSizeIndex >= 0 ? gridSizeIndex : 0;
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

        private void UnitNameTextColorCustomizeButtonClick(object sender, EventArgs e)
        {
            using (var colorDialog = new ColorDialog())
            {
                colorDialog.Color = this.unitNameTextColor;
                colorDialog.FullOpen = true;
                if (colorDialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                this.unitNameTextColor = colorDialog.Color;
                this.unitNameTextColorCustomized = true;
            }
        }

        private void MissionMovePathCustomizeButtonClick(object sender, EventArgs e)
        {
            this.CustomizeMissionColor(ref this.missionMovePathColor, ref this.missionMovePathColorCustomized, false);
        }

        private void MissionAttackPathCustomizeButtonClick(object sender, EventArgs e)
        {
            this.CustomizeMissionColor(ref this.missionAttackPathColor, ref this.missionAttackPathColorCustomized, false);
        }

        private void MissionPatrolPathCustomizeButtonClick(object sender, EventArgs e)
        {
            this.CustomizeMissionColor(ref this.missionPatrolPathColor, ref this.missionPatrolPathColorCustomized, false);
        }

        private void MissionPatrolDashCustomizeButtonClick(object sender, EventArgs e)
        {
            this.CustomizeMissionColor(ref this.missionPatrolDashColor, ref this.missionPatrolDashColorCustomized, false);
        }

        private void MissionWaitTextCustomizeButtonClick(object sender, EventArgs e)
        {
            this.CustomizeMissionColor(ref this.missionWaitTextColor, ref this.missionWaitTextColorCustomized, false);
        }

        private void MissionWaitBackplateCustomizeButtonClick(object sender, EventArgs e)
        {
            this.CustomizeMissionColor(ref this.missionWaitBackplateColor, ref this.missionWaitBackplateColorCustomized, true);
        }

        private void CustomizeMissionColor(ref Color color, ref bool customized, bool preserveAlpha)
        {
            using (var colorDialog = new ColorDialog())
            {
                colorDialog.Color = preserveAlpha
                    ? Color.FromArgb(255, color.R, color.G, color.B)
                    : color;
                colorDialog.FullOpen = true;
                if (colorDialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                color = preserveAlpha
                    ? Color.FromArgb(color.A, colorDialog.Color.R, colorDialog.Color.G, colorDialog.Color.B)
                    : colorDialog.Color;
                customized = true;
            }
        }

        private void UnitNameBackplateColorCustomizeButtonClick(object sender, EventArgs e)
        {
            using (var colorDialog = new ColorDialog())
            {
                colorDialog.Color = Color.FromArgb(
                    255,
                    this.unitNameBackplateColor.R,
                    this.unitNameBackplateColor.G,
                    this.unitNameBackplateColor.B);
                colorDialog.FullOpen = true;
                if (colorDialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                // Preserve the default/custom alpha so the backplate stays partially transparent.
                this.unitNameBackplateColor = Color.FromArgb(
                    this.unitNameBackplateColor.A,
                    colorDialog.Color.R,
                    colorDialog.Color.G,
                    colorDialog.Color.B);
                this.unitNameBackplateColorCustomized = true;
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
            MappySettings.Settings.ShowUnitNameBackplate = this.showUnitNameBackplateCheckBox.Checked;
            MappySettings.Settings.UnitNameTextColorArgb = this.unitNameTextColorCustomized
                ? (int?)this.unitNameTextColor.ToArgb()
                : null;
            MappySettings.Settings.UnitNameBackplateColorArgb = this.unitNameBackplateColorCustomized
                ? (int?)this.unitNameBackplateColor.ToArgb()
                : null;
            MappySettings.Settings.MissionMovePathColorArgb = this.missionMovePathColorCustomized
                ? (int?)this.missionMovePathColor.ToArgb()
                : null;
            MappySettings.Settings.MissionAttackPathColorArgb = this.missionAttackPathColorCustomized
                ? (int?)this.missionAttackPathColor.ToArgb()
                : null;
            MappySettings.Settings.MissionPatrolPathColorArgb = this.missionPatrolPathColorCustomized
                ? (int?)this.missionPatrolPathColor.ToArgb()
                : null;
            MappySettings.Settings.MissionPatrolDashColorArgb = this.missionPatrolDashColorCustomized
                ? (int?)this.missionPatrolDashColor.ToArgb()
                : null;
            MappySettings.Settings.MissionWaitTextColorArgb = this.missionWaitTextColorCustomized
                ? (int?)this.missionWaitTextColor.ToArgb()
                : null;
            MappySettings.Settings.MissionWaitBackplateColorArgb = this.missionWaitBackplateColorCustomized
                ? (int?)this.missionWaitBackplateColor.ToArgb()
                : null;
            MappySettings.Settings.InactiveSchemaOpacityPercent = (int)this.inactiveSchemaOpacityNumeric.Value;
            MappySettings.Settings.DoNotPromptToSaveUnsavedChanges = this.doNotPromptToSaveUnsavedChangesCheckBox.Checked;
            MappySettings.Settings.SplitTiles = this.splitTilesCheckBox.Checked;
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
            MappySettings.Settings.DefaultGridSize = this.ParseGridSizeComboSelection();
            MappySettings.SaveSettings(notifyListeners: true);
        }

        private int ParseGridSizeComboSelection()
        {
            var selected = this.defaultGridSizeComboBox.SelectedItem?.ToString() ?? "16x16";
            var separatorIndex = selected.IndexOf('x');
            if (separatorIndex > 0 && int.TryParse(selected.Substring(0, separatorIndex), out var size) && size > 0)
            {
                return size;
            }

            return 16;
        }
    }
}
