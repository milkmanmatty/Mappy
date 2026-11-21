namespace Mappy.UI.Forms
{
    using System;
    using System.Drawing;
    using System.Globalization;
    using System.Reactive.Linq;
    using System.Windows.Forms;

    using Mappy;
    using Mappy.Models;
    using Mappy.Models.Enums;
    using Mappy.TerrainBrush;
    using Mappy.UI.Controls;
    using Mappy.Util;

    public partial class MainForm : Form
    {
        private const int MinimapModeToolTipDurationMilliseconds = 20000;

        private readonly ToolTip minimapModeToolTip;
        private IMainFormViewModel model;
        private PaletteForm paletteForm;
        private int dockedSidebarWidth;
        private bool isPaletteFloating;
        private bool updatingZoomControls;

        private bool missionPathsVisible;

        private bool terrainRefreshing;

        private string terrainWorldName = string.Empty;

        private int terrainBaseIndex;

        private int terrainBrushSize = 16;

        private string terrainTool = string.Empty;

        private bool terrainUseSlopes;

        private string terrainStatus = string.Empty;

        private ImageList terrainBaseImages;

        public MainForm()
        {
            this.InitializeComponent();
            if (this.components == null)
            {
                this.components = new System.ComponentModel.Container();
            }

            this.minimapModeToolTip = new ToolTip(this.components)
            {
                ShowAlways = true,
            };
            this.ConfigureMinimapModeToolTips();
            this.ConfigureZoomControls();
            MappySettings.SettingsSaved += this.OnSettingsSaved;
            this.ApplyAdjustmentWheelSteps();
            this.dockedSidebarWidth = this.sidebarTabs.Width;
            this.InitializeExportsTab();
        }

        private void ConfigureMinimapModeToolTips()
        {
            this.editMenuItem.DropDown.ShowItemToolTips = false;

            var minimapModeItems = new[]
            {
                this.generateMinimapMenuItem,
                this.generateMinimapHighQualityMenuItem,
                this.generateMinimapEnhancedColoursMenuItem,
            };

            foreach (var item in minimapModeItems)
            {
                item.MouseEnter += this.MinimapModeMenuItemMouseEnter;
                item.MouseLeave += this.MinimapModeMenuItemMouseLeave;
            }

            this.editMenuItem.DropDown.Closed += this.EditMenuDropDownClosed;
        }

        private void MinimapModeMenuItemMouseEnter(object sender, EventArgs e)
        {
            var item = sender as ToolStripItem;
            var owner = item?.Owner as ToolStripDropDown;
            if (item == null || owner == null || string.IsNullOrEmpty(item.ToolTipText))
            {
                return;
            }

            var location = new Point(item.Bounds.Right, item.Bounds.Top);
            this.minimapModeToolTip.Show(
                item.ToolTipText,
                owner,
                location,
                MinimapModeToolTipDurationMilliseconds);
        }

        private void MinimapModeMenuItemMouseLeave(object sender, EventArgs e)
        {
            var item = sender as ToolStripItem;
            var owner = item?.Owner as ToolStripDropDown;
            if (owner != null)
            {
                this.minimapModeToolTip.Hide(owner);
            }
        }

        private void EditMenuDropDownClosed(object sender, ToolStripDropDownClosedEventArgs e)
        {
            this.minimapModeToolTip.Hide(this.editMenuItem.DropDown);
        }

        private void ConfigureZoomControls()
        {
            this.zoomTrackBar.Minimum = 0;
            this.zoomTrackBar.Maximum = MapViewPanel.ZoomLevelCount - 1;
            this.zoomTrackBar.MouseWheelStep = 1;

            var zoomToolTip = new ToolTip(this.components);
            zoomToolTip.SetToolTip(this.zoomTrackBar, @"Zoom (Ctrl+Mouse Wheel over the map)");

            this.mapViewPanel.ZoomChanged += this.MapViewPanelZoomChanged;
            this.RefreshZoomControls();
        }

        private void MapViewPanelZoomChanged(object sender, EventArgs e)
        {
            this.RefreshZoomControls();
        }

        private void RefreshZoomControls()
        {
            // Guard against the slider echoing the change we are pushing into it.
            this.updatingZoomControls = true;
            try
            {
                this.zoomTrackBar.Value = this.mapViewPanel.ZoomLevelIndex;
            }
            finally
            {
                this.updatingZoomControls = false;
            }

            this.zoomValueLabel.Text = (this.mapViewPanel.ZoomFactor * 100)
                .ToString("0.##", CultureInfo.CurrentCulture) + "%";
        }

        private void ZoomTrackBarValueChanged(object sender, EventArgs e)
        {
            if (this.updatingZoomControls)
            {
                return;
            }

            this.mapViewPanel.SetZoomLevelIndex(this.zoomTrackBar.Value);
        }

        private void OnSettingsSaved(object sender, EventArgs e)
        {
            this.ApplyAdjustmentWheelSteps();
        }

        private void ApplyAdjustmentWheelSteps()
        {
            var settings = MappySettings.Settings;
            this.selectedHeightNumericUpDown.MouseWheelStep = settings.GetHeightSelectedHeightWheelStepOrDefault();
            this.intervalNumericUpDown.MouseWheelStep = settings.GetHeightIntervalWheelStepOrDefault();
            this.cursorSizeNumericUpDown.MouseWheelStep = settings.GetHeightCursorSizeWheelStepOrDefault();
            this.voidCursorSizeNumericUpDown.MouseWheelStep = settings.GetVoidCursorSizeWheelStepOrDefault();
            this.seaLevelTrackbar.MouseWheelStep = settings.GetSeaLevelWheelStepOrDefault();
        }

        public MapViewPanel MapViewPanel => this.mapViewPanel;

        public SectionView SectionView => this.sectionsView;

        public SectionView FeatureView => this.featureView;

        public void SetModel(IMainFormViewModel model)
        {
            var gridSize = model.GridSize.CombineLatest(
                model.GridVisible,
                (size, visible) => visible ? new Size?(size) : null);

            gridSize.Subscribe(x => this.gridOffMenuItem.Checked = x == null);
            gridSize.Subscribe(x => this.grid16MenuItem.Checked = x == new Size(16, 16));
            gridSize.Subscribe(x => this.grid32MenuItem.Checked = x == new Size(32, 32));
            gridSize.Subscribe(x => this.grid64MenuItem.Checked = x == new Size(64, 64));
            gridSize.Subscribe(x => this.grid128MenuItem.Checked = x == new Size(128, 128));
            gridSize.Subscribe(x => this.grid256MenuItem.Checked = x == new Size(256, 256));
            gridSize.Subscribe(x => this.grid512MenuItem.Checked = x == new Size(512, 512));
            gridSize.Subscribe(x => this.grid1024MenuItem.Checked = x == new Size(1024, 1024));

            // file menu bindings
            model.CanSave.Subscribe(x => this.saveMenuItem.Enabled = x);
            model.CanSaveAs.Subscribe(x => this.saveAsMenuItem.Enabled = x);
            model.CanCloseMap.Subscribe(x => this.closeMenuItem.Enabled = x);

            model.CanImportMinimap.Subscribe(x => this.importMinimapMenuItem.Enabled = x);
            model.CanExportMinimap.Subscribe(x => this.exportMinimapMenuItem.Enabled = x);

            model.CanImportHeightmap.Subscribe(x => this.importHeightmapMenuItem.Enabled = x);
            model.CanExportHeightmap.Subscribe(x => this.exportHeightmapMenuItem.Enabled = x);

            model.CanExportMapImage.Subscribe(x => this.exportMapImageMenuItem.Enabled = x);
            model.CanImportCustomSection.Subscribe(x => this.importCustomSectionMenuItem.Enabled = x);

            // edit menu bindings
            model.CanUndo.Subscribe(x => this.undoMenuItem.Enabled = x);
            model.CanRedo.Subscribe(x => this.redoMenuItem.Enabled = x);
            model.CanCopy.Subscribe(x => this.copyMenuItem.Enabled = x);
            model.CanCut.Subscribe(x => this.cutMenuItem.Enabled = x);
            model.CanPaste.Subscribe(x => this.pasteMenuItem.Enabled = x);
            model.CanFill.Subscribe(x => this.fillMenuItem.Enabled = x);
            model.CanFlip.Subscribe(x =>
            {
                this.flipMenuItem.Enabled = x;
                this.exportSectionMenuItem.Enabled = x;
                this.exportAddButton.Enabled = x;
            });
            model.CanResizeMap.Subscribe(x => this.resizeMapMenuItem.Enabled = x);
            model.CanReplaceFeature.Subscribe(x => this.replaceFeatureMenuItem.Enabled = x);
            model.CanClearAllFeatures.Subscribe(x => this.clearAllFeaturesMenuItem.Enabled = x);
            model.CanFillFeatures.Subscribe(x => this.fillSelectionMenuItem.Enabled = x);

            model.CanGenerateMinimap.Subscribe(x => this.generateMinimapMenuItem.Enabled = x);
            model.CanGenerateMinimapHighQuality.Subscribe(x => this.generateMinimapHighQualityMenuItem.Enabled = x);
            model.CanGenerateMinimapEnhancedColours.Subscribe(x => this.generateMinimapEnhancedColoursMenuItem.Enabled = x);

            model.CanOpenAttributes.Subscribe(x => this.mapAttributesMenuItem.Enabled = x);
            model.CanOpenAttributes.Subscribe(x => this.oamapPropertiesMenuItem.Enabled = x);
            model.CanOpenAttributes.Subscribe(x => this.useOnlyUnitsMenuItem.Enabled = x);

            // view menu bindings
            model.MinimapVisible.Subscribe(x => this.toggleMinimapMenuItem.Checked = x);
            model.HeightmapVisible.Subscribe(x => this.toggleHeightmapMenuItem.Checked = x);
            model.HeightGridVisible.Subscribe(x => this.toggleHeightGridMenuItem.Checked = x);
            model.VoidsVisible.Subscribe(x => this.toggleVoidsMenuItem.Checked = x);
            model.FeaturesVisible.Subscribe(x => this.toggleFeaturesMenuItem.Checked = x);

            model.UnitPlacementPlayerMenuChoice.Subscribe(
                c =>
                    {
                        this.playerPromptMenuItem.Checked = c == UnitPlacementPlayerMenuChoice.Prompt;
                        this.player1MenuItem.Checked = c == UnitPlacementPlayerMenuChoice.Player1;
                        this.player2MenuItem.Checked = c == UnitPlacementPlayerMenuChoice.Player2;
                        this.player3MenuItem.Checked = c == UnitPlacementPlayerMenuChoice.Player3;
                        this.player4MenuItem.Checked = c == UnitPlacementPlayerMenuChoice.Player4;
                        this.player5MenuItem.Checked = c == UnitPlacementPlayerMenuChoice.Player5;
                        this.player6MenuItem.Checked = c == UnitPlacementPlayerMenuChoice.Player6;
                        this.player7MenuItem.Checked = c == UnitPlacementPlayerMenuChoice.Player7;
                        this.player8MenuItem.Checked = c == UnitPlacementPlayerMenuChoice.Player8;
                        this.player9MenuItem.Checked = c == UnitPlacementPlayerMenuChoice.Player9;
                        this.player10MenuItem.Checked = c == UnitPlacementPlayerMenuChoice.Player10;
                        this.player11MenuItem.Checked = c == UnitPlacementPlayerMenuChoice.Player11;
                    });

            // sea level widget bindings
            model.CanChangeSeaLevel.Subscribe(x => this.seaLevelLabel.Enabled = x);
            model.CanChangeSeaLevel.Subscribe(x => this.seaLevelValueLabel.Enabled = x);
            model.CanChangeSeaLevel.Subscribe(x => this.seaLevelTrackbar.Enabled = x);

            model.SeaLevel.Subscribe(x => this.seaLevelTrackbar.Value = x);
            model.SeaLevel
                .Select(x => x.ToString(CultureInfo.CurrentCulture))
                .Subscribe(x => this.seaLevelValueLabel.Text = x);
            model.HeightEditInterval.Subscribe(x => this.intervalNumericUpDown.Value = x);
            model.HeightEditMode.Subscribe(
                x =>
                    {
                        this.incrementDecrementHeightRadioButton.Checked = x == HeightEditMode.IncrementDecrement;
                        this.setHeightRadioButton.Checked = x == HeightEditMode.Set;
                        this.smoothHeightRadioButton.Checked = x == HeightEditMode.Smooth;
                        this.intervalLabel.Enabled = x == HeightEditMode.IncrementDecrement;
                        this.intervalNumericUpDown.Enabled = x == HeightEditMode.IncrementDecrement;
                        this.selectedHeightLabel.Enabled = x == HeightEditMode.Set;
                        this.selectedHeightNumericUpDown.Enabled = x == HeightEditMode.Set;
                    });
            model.HeightEditSetValue.Subscribe(x => this.selectedHeightNumericUpDown.Value = x);
            model.HeightEditCursorSize.Subscribe(x => this.cursorSizeNumericUpDown.Value = x);
            model.VoidEditCursorSize.Subscribe(x => this.voidCursorSizeNumericUpDown.Value = x);
            model.TerrainWorldName.Subscribe(this.OnTerrainWorldName);
            model.TerrainBaseIndex.Subscribe(this.OnTerrainBaseIndex);
            model.TerrainBrushSize.Subscribe(this.OnTerrainBrushSize);
            model.TerrainTool.Subscribe(this.OnTerrainTool);
            model.TerrainUseSlopes.Subscribe(this.OnTerrainUseSlopes);
            model.TerrainStatus.Subscribe(this.OnTerrainStatus);

            // title text bindings
            model.TitleText.Subscribe(x => this.Text = x);

            model.MousePositionText.Subscribe(x => this.mousePositionLabel.Text = x);
            model.HeightText.Subscribe(x => this.heightLabel.Text = x);
            model.VoidText.Subscribe(x => this.voidLabel.Text = x);
            model.HoveredFeatureText.Subscribe(x => this.hoveredFeatureLabel.Text = x);

            this.model = model;
        }

        private void OpenMenuItemClick(object sender, EventArgs e)
        {
            this.model.OpenMenuItemClick();
        }

        private void RecentMenuItemDropDownOpening(object sender, EventArgs e)
        {
            this.recentMenuItem.DropDownItems.Clear();

            var entries = RecentFiles.GetEntries();
            if (entries.Count == 0)
            {
                var emptyItem = new ToolStripMenuItem("(Empty)")
                {
                    Enabled = false,
                };
                this.recentMenuItem.DropDownItems.Add(emptyItem);
                return;
            }

            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                var item = new ToolStripMenuItem(entry.DisplayText)
                {
                    Tag = entry,
                };
                item.Click += this.RecentMapMenuItemClick;
                this.recentMenuItem.DropDownItems.Add(item);
            }
        }

        private void RecentMapMenuItemClick(object sender, EventArgs e)
        {
            var item = sender as ToolStripMenuItem;
            var entry = item?.Tag as RecentMapEntry;
            if (entry == null)
            {
                return;
            }

            this.model.OpenRecentMenuItemClick(entry);
        }

        private void ToggleHeightmapMenuItemClick(object sender, EventArgs e)
        {
            this.model.ToggleHeightMapMenuItemClick();
        }

        private void PreferencesMenuItemClick(object sender, EventArgs e)
        {
            this.model.PreferencesMenuItemClick();
        }

        private void SaveAsMenuItemClick(object sender, EventArgs e)
        {
            this.model.SaveAsMenuItemClick();
        }

        private void SaveMenuItemClick(object sender, EventArgs e)
        {
            this.model.SaveMenuItemClick();
        }

        private void ToggleMinimapMenuItemClick(object sender, EventArgs e)
        {
            this.model.ToggleMinimapMenuItemClick();
        }

        private void UndoMenuItemClick(object sender, EventArgs e)
        {
            this.model.UndoMenuItemClick();
        }

        private void RedoMenuItemClick(object sender, EventArgs e)
        {
            this.model.RedoMenuItemClick();
        }

        private void MainFormFormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                this.model.FormCloseButtonClick();
                e.Cancel = true;
            }
        }

        private void ExitMenuItemClick(object sender, EventArgs e)
        {
            this.model.ExitMenuItemClick();
        }

        private void NewMenuItemClick(object sender, EventArgs e)
        {
            this.model.NewMenuItemClick();
        }

        private void InfoMenuItemClick(object sender, EventArgs e)
        {
            this.model.InfoMenuItemClick();
        }

        private void AboutMenuItemClick(object sender, EventArgs e)
        {
            this.model.AboutMenuItemClick();
        }

        private void CheckForUpdatesMenuItemClick(object sender, EventArgs e)
        {
            this.model.CheckForUpdatesMenuItemClick();
        }

        private void GenerateMinimapMenuItemClick(object sender, EventArgs e)
        {
            this.model.GenerateMinimapMenuItemClick();
        }

        private void GridOffMenuItemClick(object sender, EventArgs e)
        {
            this.model.GridOffMenuItemClick();
        }

        private void GridMenuItemClick(object sender, EventArgs e)
        {
            var item = (ToolStripMenuItem)sender;
            var size = Convert.ToInt32(item.Tag);
            var s = new Size(size, size);

            this.model.GridMenuItemClick(s);
        }

        private void GridColorMenuItemClick(object sender, EventArgs e)
        {
            this.model.GridColorMenuItemClick();
        }

        private void ToggleFeaturesMenuItemClick(object sender, EventArgs e)
        {
            this.model.ToggleFeaturesMenuItemClick();
        }

        private void PlayerPlacementMenuItemClick(object sender, EventArgs e)
        {
            var item = (ToolStripMenuItem)sender;
            var tag = Convert.ToInt32(item.Tag, CultureInfo.InvariantCulture);
            this.model.UnitPlacementPlayerMenuItemClick((UnitPlacementPlayerMenuChoice)tag);
        }

        private void MapAttributesMenuItemClick(object sender, EventArgs e)
        {
            this.model.MapAttributesMenuItemClick();
        }

        private void OamapPropertiesMenuItemClick(object sender, EventArgs e)
        {
            this.model.OamapPropertiesMenuItemClick();
        }

        private void UseOnlyUnitsMenuItemClick(object sender, EventArgs e)
        {
            this.model.UseOnlyUnitsMenuItemClick();
        }

        private void SeaLevelTrackBarValueChanged(object sender, EventArgs e)
        {
            this.model.SeaLevelTrackBarValueChanged(this.seaLevelTrackbar.Value);
        }

        private void CloseMenuItemClick(object sender, EventArgs e)
        {
            this.model.CloseMenuItemClick();
        }

        private void GenerateMinimapHighQualityMenuItemClick(object sender, EventArgs e)
        {
            this.model.GenerateMinimapHighQualityMenuItemClick();
        }

        private void GenerateMinimapEnhancedColoursMenuItemClick(object sender, EventArgs e)
        {
            this.model.GenerateMinimapEnhancedColoursMenuItemClick();
        }

        private void SeaLevelTrackBarMouseUp(object sender, MouseEventArgs e)
        {
            this.model.SeaLevelTrackBarMouseUp();
        }

        private void HeightIntervalNumericUpDownValueChanged(object sender, EventArgs e)
        {
            this.model.HeightEditIntervalChanged((int)this.intervalNumericUpDown.Value);
        }

        private void CursorSizeNumericUpDownValueChanged(object sender, EventArgs e)
        {
            this.model.HeightEditCursorSizeChanged((int)this.cursorSizeNumericUpDown.Value);
        }

        private void IncrementDecrementHeightRadioButtonCheckedChanged(object sender, EventArgs e)
        {
            if (this.model == null || !this.incrementDecrementHeightRadioButton.Checked)
            {
                return;
            }

            this.model.HeightEditModeChanged(HeightEditMode.IncrementDecrement);
        }

        private void SetHeightRadioButtonCheckedChanged(object sender, EventArgs e)
        {
            if (this.model == null || !this.setHeightRadioButton.Checked)
            {
                return;
            }

            this.model.HeightEditModeChanged(HeightEditMode.Set);
        }

        private void SmoothHeightRadioButtonCheckedChanged(object sender, EventArgs e)
        {
            if (this.model == null || !this.smoothHeightRadioButton.Checked)
            {
                return;
            }

            this.model.HeightEditModeChanged(HeightEditMode.Smooth);
        }

        private void SelectedHeightNumericUpDownValueChanged(object sender, EventArgs e)
        {
            if (this.model == null)
            {
                return;
            }

            this.model.HeightEditSetValueChanged((int)this.selectedHeightNumericUpDown.Value);
        }

        private void VoidCursorSizeNumericUpDownValueChanged(object sender, EventArgs e)
        {
            this.model.VoidEditCursorSizeChanged((int)this.voidCursorSizeNumericUpDown.Value);
        }

        private void OnTerrainWorldName(string worldName)
        {
            this.terrainWorldName = worldName ?? string.Empty;
        }

        private void OnTerrainBaseIndex(int baseIndex)
        {
            this.terrainBaseIndex = baseIndex;
            if (this.terrainRefreshing || !string.IsNullOrEmpty(this.terrainTool))
            {
                return;
            }

            var item = this.FindBaseItem(baseIndex);
            if (item == null || item.Selected)
            {
                return;
            }

            this.terrainRefreshing = true;
            item.Selected = true;
            item.EnsureVisible();
            this.terrainRefreshing = false;
        }

        private void OnTerrainTool(string pathStyle)
        {
            this.terrainTool = pathStyle ?? string.Empty;
            if (this.terrainRefreshing)
            {
                return;
            }

            this.terrainRefreshing = true;
            this.SelectListedTool();
            this.UpdateTerrainBrushControls();
            this.terrainRefreshing = false;
        }

        private void OnTerrainUseSlopes(bool useSlopes)
        {
            this.terrainUseSlopes = useSlopes;
            if (this.terrainRefreshing || this.terrainUseSlopesCheckBox.Checked == useSlopes)
            {
                return;
            }

            this.terrainRefreshing = true;
            this.terrainUseSlopesCheckBox.Checked = useSlopes;
            this.terrainRefreshing = false;
        }

        private void OnTerrainBrushSize(int tiles)
        {
            this.terrainBrushSize = tiles;
            if (this.terrainRefreshing)
            {
                return;
            }

            var index = Array.IndexOf(TerrainPlanner.BrushSizes, tiles);
            if (index >= 0 && this.terrainSizeComboBox.SelectedIndex != index)
            {
                this.terrainRefreshing = true;
                this.terrainSizeComboBox.SelectedIndex = index;
                this.terrainRefreshing = false;
            }
        }

        private void OnTerrainStatus(string status)
        {
            this.terrainStatus = status ?? string.Empty;
            if (this.terrainWorldComboBox.Visible)
            {
                this.terrainStatusLabel.Text = this.terrainStatus;
            }
        }

        private void TerrainWorldComboBoxSelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.model == null || this.terrainRefreshing || this.terrainWorldComboBox.SelectedIndex < 0)
            {
                return;
            }

            this.terrainRefreshing = true;
            try
            {
                var name = (string)this.terrainWorldComboBox.SelectedItem;
                this.model.TerrainWorldChanged(name);
                this.FillTerrainBases(this.model.LoadTerrainCatalog().Find(name));
            }
            finally
            {
                this.terrainRefreshing = false;
            }
        }

        private void TerrainSizeComboBoxSelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.model == null || this.terrainRefreshing)
            {
                return;
            }

            var index = this.terrainSizeComboBox.SelectedIndex;
            if (index >= 0 && index < TerrainPlanner.BrushSizes.Length)
            {
                this.model.TerrainBrushSizeChanged(TerrainPlanner.BrushSizes[index]);
            }
        }

        private void TerrainBaseListSelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.model == null || this.terrainRefreshing || this.terrainBaseList.SelectedIndices.Count == 0)
            {
                return;
            }

            var item = this.terrainBaseList.SelectedItems[0];
            var style = item.Tag as string;
            if (style != null)
            {
                this.model.TerrainToolChanged(style);
            }
            else if (item.Tag is int)
            {
                this.model.TerrainBaseChanged((int)item.Tag);
            }

            this.UpdateTerrainBrushControls();
        }

        private void TerrainUseSlopesCheckBoxCheckedChanged(object sender, EventArgs e)
        {
            if (this.model == null || this.terrainRefreshing)
            {
                return;
            }

            this.model.TerrainUseSlopesChanged(this.terrainUseSlopesCheckBox.Checked);
        }

        private void RefreshTerrainTab()
        {
            if (this.model == null)
            {
                return;
            }

            this.terrainRefreshing = true;
            try
            {
                var catalog = this.model.LoadTerrainCatalog();
                var hasWorlds = catalog.Worlds.Count > 0;
                this.terrainWorldLabel.Visible = hasWorlds;
                this.terrainWorldComboBox.Visible = hasWorlds;
                this.terrainSizeLabel.Visible = hasWorlds;
                this.terrainSizeComboBox.Visible = hasWorlds;
                this.terrainUseSlopesCheckBox.Visible = hasWorlds;
                this.terrainBaseList.Visible = hasWorlds;
                if (!hasWorlds)
                {
                    this.terrainStatusLabel.Text = TerrainCatalog.MissingTilesetMessage;
                    return;
                }

                this.terrainWorldComboBox.Items.Clear();
                foreach (var world in catalog.Worlds)
                {
                    this.terrainWorldComboBox.Items.Add(world.Name);
                }

                var worldIndex = this.terrainWorldComboBox.FindStringExact(this.terrainWorldName);
                if (worldIndex < 0)
                {
                    worldIndex = 0;
                    this.model.TerrainWorldChanged((string)this.terrainWorldComboBox.Items[0]);
                }

                this.terrainWorldComboBox.SelectedIndex = worldIndex;
                var sizeIndex = Array.IndexOf(TerrainPlanner.BrushSizes, this.terrainBrushSize);
                this.terrainSizeComboBox.SelectedIndex = sizeIndex < 0 ? 1 : sizeIndex;
                this.FillTerrainBases(catalog.Find((string)this.terrainWorldComboBox.SelectedItem));
                this.terrainStatusLabel.Text = this.terrainStatus;
            }
            finally
            {
                this.terrainRefreshing = false;
            }
        }

        private void FillTerrainBases(TerrainWorld world)
        {
            this.terrainBaseList.Items.Clear();
            if (this.terrainBaseImages != null)
            {
                this.terrainBaseList.SmallImageList = null;
                this.terrainBaseImages.Dispose();
                this.terrainBaseImages = null;
            }

            if (world == null)
            {
                this.terrainUseSlopesCheckBox.Enabled = false;
                this.UpdateTerrainBrushControls();
                return;
            }

            this.terrainBaseImages = new ImageList
            {
                ImageSize = new Size(32, 32),
                ColorDepth = ColorDepth.Depth32Bit,
            };
            for (var index = 0; index < world.Bases.Count; index++)
            {
                var basis = world.Bases[index];
                var section = basis.Sections.Count > 0 ? basis.Sections[0] : null;
                var item = new System.Windows.Forms.ListViewItem(basis.Name, this.AddTerrainImage(section));
                item.Tag = index;
                this.terrainBaseList.Items.Add(item);
            }

            foreach (var style in world.PathStyles)
            {
                var item = new System.Windows.Forms.ListViewItem(style.Name, this.AddTerrainImage(style.Thumbnail));
                item.Tag = style.Name;
                this.terrainBaseList.Items.Add(item);
            }

            this.terrainBaseList.SmallImageList = this.terrainBaseImages;
            if (!string.IsNullOrEmpty(this.terrainTool) && world.FindPathStyle(this.terrainTool) == null)
            {
                this.model.TerrainToolChanged(string.Empty);
            }

            this.terrainUseSlopesCheckBox.Enabled = world.HasSlopes;
            this.terrainUseSlopesCheckBox.Checked = this.terrainUseSlopes;
            this.SelectListedTool();
            this.UpdateTerrainBrushControls();
        }

        private int AddTerrainImage(TerrainSectionRef section)
        {
            Bitmap thumbnail = null;
            if (section != null)
            {
                thumbnail = this.model.TerrainBaseThumbnail(section.HpiFileName, section.SctFileName);
            }

            var owned = false;
            if (thumbnail == null)
            {
                thumbnail = new Bitmap(32, 32);
                owned = true;
            }

            this.terrainBaseImages.Images.Add(thumbnail);
            if (owned)
            {
                thumbnail.Dispose();
            }

            return this.terrainBaseImages.Images.Count - 1;
        }

        private void SelectListedTool()
        {
            System.Windows.Forms.ListViewItem match = null;
            if (!string.IsNullOrEmpty(this.terrainTool))
            {
                foreach (System.Windows.Forms.ListViewItem item in this.terrainBaseList.Items)
                {
                    if (string.Equals(item.Tag as string, this.terrainTool, StringComparison.OrdinalIgnoreCase))
                    {
                        match = item;
                        break;
                    }
                }
            }

            if (match == null)
            {
                match = this.FindBaseItem(this.terrainBaseIndex);
            }

            if (match != null && !match.Selected)
            {
                match.Selected = true;
                match.EnsureVisible();
            }
        }

        private System.Windows.Forms.ListViewItem FindBaseItem(int baseIndex)
        {
            System.Windows.Forms.ListViewItem last = null;
            foreach (System.Windows.Forms.ListViewItem item in this.terrainBaseList.Items)
            {
                if (!(item.Tag is int))
                {
                    continue;
                }

                last = item;
                if ((int)item.Tag == baseIndex)
                {
                    return item;
                }
            }

            return last;
        }

        private void UpdateTerrainBrushControls()
        {
            var paintingBase = string.IsNullOrEmpty(this.terrainTool);
            this.terrainSizeLabel.Enabled = paintingBase;
            this.terrainSizeComboBox.Enabled = paintingBase;
        }

        private void CopyMenuItemClick(object sender, EventArgs e)
        {
            this.model.CopyMenuItemClick();
        }

        private void PasteMenuItemClick(object sender, EventArgs e)
        {
            this.model.PasteMenuItemClick();
        }

        private void CutMenuItemClick(object sender, EventArgs e)
        {
            this.model.CutMenuItemClick();
        }

        private void FillMenuItemClick(object sender, EventArgs e)
        {
            this.model.FillMenuItemClick();
        }

        private void ResizeMapMenuItemClick(object sender, EventArgs e)
        {
            this.model.ResizeMapMenuItemClick();
        }

        private void FlipMenuItemClick(object sender, EventArgs e)
        {
            this.model.FlipMenuItemClick();
        }

        private void ReplaceFeatureMenuItemClick(object sender, EventArgs e)
        {
            this.model.ReplaceFeatureMenuItemClick();
        }

        private void ClearAllFeaturesMenuItemClick(object sender, EventArgs e)
        {
            this.model.ClearAllFeaturesMenuItemClick();
        }

        private void FillFeaturesOptionsMenuItemClick(object sender, EventArgs e)
        {
            this.model.FillFeaturesOptionsMenuItemClick();
        }

        private void FillSelectionMenuItemClick(object sender, EventArgs e)
        {
            this.model.FillSelectionMenuItemClick();
        }

        private void TilesetsMenuItemClick(object sender, EventArgs e)
        {
            this.model.TilesetsMenuItemClick();
        }

        private void ExportSectionMenuItemClick(object sender, EventArgs e)
        {
            this.model.ExportSectionMenuItemClick();
        }

        private void MainFormLoad(object sender, EventArgs e)
        {
            this.RestoreWindowState();
            this.FormClosed += this.MainFormFormClosed;
            this.model.Load();

            if (MappySettings.Settings.SidebarFloating)
            {
                this.FloatPalette();
            }
        }

        private void RestoreWindowState()
        {
            var settings = MappySettings.Settings;
            if (settings.WindowSizeWidth <= 0 || settings.WindowSizeHeight <= 0)
            {
                return;
            }

            var savedBounds = new Rectangle(
                settings.WindowLocationX,
                settings.WindowLocationY,
                settings.WindowSizeWidth,
                settings.WindowSizeHeight);

            if (!this.IsBoundsOnAnyScreen(savedBounds))
            {
                return;
            }

            this.StartPosition = FormStartPosition.Manual;
            this.Bounds = savedBounds;
            var state = (FormWindowState)Math.Max(0, Math.Min(2, settings.WindowState));
            if (state != FormWindowState.Minimized)
            {
                this.WindowState = state;
            }

            this.RestoreSidebarTabsWidth(settings);
        }

        private void RestoreSidebarTabsWidth(Configuration settings)
        {
            if (settings.SidebarTabsWidth <= 0)
            {
                return;
            }

            this.dockedSidebarWidth = settings.SidebarTabsWidth;

            if (this.isPaletteFloating)
            {
                return;
            }

            var minWidth = this.sidebarSplitter.MinSize;
            var maxWidth = this.ClientSize.Width - this.sidebarSplitter.MinExtra;
            if (maxWidth < minWidth)
            {
                maxWidth = minWidth;
            }

            this.sidebarTabs.Width = Math.Max(minWidth, Math.Min(maxWidth, settings.SidebarTabsWidth));
            this.dockedSidebarWidth = this.sidebarTabs.Width;
        }

        private bool IsBoundsOnAnyScreen(Rectangle bounds)
        {
            foreach (Screen screen in Screen.AllScreens)
            {
                if (screen.WorkingArea.IntersectsWith(bounds))
                {
                    return true;
                }
            }

            return false;
        }

        private void MainFormFormClosed(object sender, FormClosedEventArgs e)
        {
            this.SaveWindowState();
        }

        private void SaveWindowState()
        {
            var bounds = (this.WindowState == FormWindowState.Maximized || this.WindowState == FormWindowState.Minimized)
                ? this.RestoreBounds
                : this.Bounds;
            var settings = MappySettings.Settings;
            settings.WindowState = (int)this.WindowState;
            settings.WindowLocationX = bounds.X;
            settings.WindowLocationY = bounds.Y;
            settings.WindowSizeWidth = bounds.Width;
            settings.WindowSizeHeight = bounds.Height;
            settings.SidebarFloating = this.isPaletteFloating;
            if (this.isPaletteFloating)
            {
                settings.SidebarTabsWidth = this.dockedSidebarWidth;
                this.paletteForm?.SaveBoundsToSettings();
            }
            else
            {
                settings.SidebarTabsWidth = this.sidebarTabs.Width;
            }

            MappySettings.SaveSettings();
        }

        private void EnsurePaletteForm()
        {
            if (this.paletteForm != null)
            {
                return;
            }

            this.paletteForm = new PaletteForm
            {
                Owner = this,
                Icon = this.Icon,
            };
            this.paletteForm.DockRequested += (s, e) => this.DockPalette();
            this.paletteForm.PaletteKeyDown += this.PaletteForm_KeyDown;
        }

        internal bool ProcessPaletteCmdKey(ref Message msg, Keys keyData)
        {
            return this.ProcessCmdKey(ref msg, keyData);
        }

        private void FloatPaletteMenuItemClick(object sender, EventArgs e)
        {
            if (this.isPaletteFloating)
            {
                this.DockPalette();
            }
            else
            {
                this.FloatPalette();
            }
        }

        private void FloatPalette()
        {
            if (this.isPaletteFloating)
            {
                return;
            }

            this.EnsurePaletteForm();
            this.dockedSidebarWidth = this.sidebarTabs.Width > 0
                ? this.sidebarTabs.Width
                : this.dockedSidebarWidth;

            this.Controls.Remove(this.sidebarTabs);
            this.sidebarSplitter.Visible = false;

            this.sidebarTabs.Dock = DockStyle.Fill;
            this.paletteForm.Controls.Add(this.sidebarTabs);

            var settings = MappySettings.Settings;
            if (!this.paletteForm.TryRestoreBoundsFromSettings())
            {
                this.paletteForm.PlaceNearOwner(this, this.dockedSidebarWidth);
            }

            this.isPaletteFloating = true;
            this.floatPaletteMenuItem.Checked = true;
            settings.SidebarFloating = true;
            this.paletteForm.Show();
            MappySettings.SaveSettings();
        }

        private void DockPalette()
        {
            if (!this.isPaletteFloating)
            {
                return;
            }

            this.paletteForm?.SaveBoundsToSettings();
            this.paletteForm.Hide();
            this.paletteForm.Controls.Remove(this.sidebarTabs);

            this.sidebarTabs.Dock = DockStyle.Left;
            this.Controls.Add(this.sidebarTabs);
            this.Controls.SetChildIndex(
                this.sidebarTabs,
                this.Controls.GetChildIndex(this.sidebarSplitter) + 1);

            this.sidebarSplitter.Visible = true;
            var width = this.dockedSidebarWidth > 0
                ? this.dockedSidebarWidth
                : MappySettings.Settings.SidebarTabsWidth;
            if (width > 0)
            {
                var minWidth = this.sidebarSplitter.MinSize;
                var maxWidth = this.ClientSize.Width - this.sidebarSplitter.MinExtra;
                if (maxWidth < minWidth)
                {
                    maxWidth = minWidth;
                }

                this.sidebarTabs.Width = Math.Max(minWidth, Math.Min(maxWidth, width));
                this.dockedSidebarWidth = this.sidebarTabs.Width;
            }

            this.isPaletteFloating = false;
            this.floatPaletteMenuItem.Checked = false;
            MappySettings.Settings.SidebarFloating = false;
            MappySettings.SaveSettings();
        }

        private void ExportMinimapMenuItemClick(object sender, EventArgs e)
        {
            this.model.ExportMinimapMenuItemClick();
        }

        private void ExportHeightmapMenuItemClick(object sender, EventArgs e)
        {
            this.model.ExportHeightmapMenuItemClick();
        }

        private void ImportMinimapMenuItemClick(object sender, EventArgs e)
        {
            this.model.ImportMinimapMenuItemClick();
        }

        private void ImportHeightmapMenuItemClick(object sender, EventArgs e)
        {
            this.model.ImportHeightmapMenuItemClick();
        }

        private void ExportMapImageMenuItemClick(object sender, EventArgs e)
        {
            this.model.ExportMapImageMenuItemClick();
        }

        private void ImportCustomSectionMenuItemClick(object sender, EventArgs e)
        {
            this.model.ImportCustomSectionMenuItemClick();
        }

        private void MainFormDragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void MainFormDragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var data = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (data.Length > 1)
                {
                    return;
                }

                this.model.DragDropFile(data[0]);
            }
        }

        private void ToggleVoidsMenuItemClick(object sender, EventArgs e)
        {
            this.model.ToggleVoidsMenuItemClick();
        }

        private void ToggleHeightGridMenuItemClick(object sender, EventArgs e)
        {
            this.model.ToggleHeightGridMenuItemClick();
        }

        private void ClearMarkersButtonClick(object sender, EventArgs e)
        {
            this.model?.ClearMarkerLines();
        }

        private void MarkerDrawModeChanged(object sender, EventArgs e)
        {
            if (sender is RadioButton button && button.Checked)
            {
                this.mapViewPanel.SetMarkerDrawFreehand(button == this.markerFreehandRadioButton);
            }
        }

        private void GUITabs_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.sidebarTabs.SelectedTab == null)
            {
                return;
            }

            this.model.ChangeSelectedTabType(Util.MapTabNameToGUIType(this.sidebarTabs.SelectedTab.Name));
            if (this.sidebarTabs.SelectedTab == this.terrainTab)
            {
                this.RefreshTerrainTab();
            }
        }

        private void PaletteForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape && this.model != null && this.model.TryCancelMissionCoordinatePick())
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                return;
            }

            if (this.TryHandlePaletteTabShortcut(e))
            {
                return;
            }

            this.TryHandleTerrainSizeShortcut(e);
        }

        private void ToggleOrdersMenuItemClick(object sender, EventArgs e)
        {
            this.SetMissionPathsVisible(!this.missionPathsVisible);
        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape && this.model != null && this.model.TryCancelMissionCoordinatePick())
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                return;
            }

            if (this.TryHandlePaletteTabShortcut(e))
            {
                return;
            }

            if (this.TryHandleTerrainSizeShortcut(e))
            {
                return;
            }

            if (e.Shift && e.KeyCode >= Keys.D0 && e.KeyCode <= Keys.D9)
            {
                var playerIndex = e.KeyCode == Keys.D0 ? 9 : (e.KeyCode - Keys.D1);
                this.model.CenterViewOnStartPosition(playerIndex);
                e.Handled = true;
            }
        }

        public void SetMissionPathsVisible(bool visible)
        {
            this.missionPathsVisible = visible;
            this.toggleOrdersMenuItem.Checked = visible;
            this.mapViewPanel.SetMissionPathsVisible(visible);
        }

        private bool TryHandlePaletteTabShortcut(KeyEventArgs e)
        {
            if (!e.Control)
            {
                return false;
            }

            TabPage target = null;
            if (e.KeyCode == Keys.D1)
            {
                target = this.sectionsTab;
            }
            else if (e.KeyCode == Keys.D2)
            {
                target = this.featuresTab;
            }
            else if (e.KeyCode == Keys.D3)
            {
                target = this.startPositionsTab;
            }
            else if (e.KeyCode == Keys.D4)
            {
                target = this.heightTab;
            }
            else if (e.KeyCode == Keys.D5)
            {
                target = this.voidTab;
            }
            else if (e.KeyCode == Keys.D6)
            {
                target = this.attributesTab;
            }
            else if (e.KeyCode == Keys.D7 && this.missionTab != null)
            {
                target = this.missionTab;
            }
            else if (e.KeyCode == Keys.D8)
            {
                target = this.markersTab;
            }
            else if (e.KeyCode == Keys.D9)
            {
                target = this.exportsTab;
            }
            else if (e.KeyCode == Keys.D0)
            {
                target = this.terrainTab;
            }

            if (target == null)
            {
                return false;
            }

            this.sidebarTabs.SelectedTab = target;
            e.Handled = true;
            e.SuppressKeyPress = true;
            return true;
        }

        private bool TryHandleTerrainSizeShortcut(KeyEventArgs e)
        {
            if (e.Control || e.Alt || e.Shift || this.model == null || this.sidebarTabs.SelectedTab != this.terrainTab)
            {
                return false;
            }

            int direction;
            if (e.KeyCode == Keys.OemOpenBrackets)
            {
                direction = -1;
            }
            else if (e.KeyCode == Keys.OemCloseBrackets)
            {
                direction = 1;
            }
            else
            {
                return false;
            }

            if (Form.ActiveForm != null && Form.ActiveForm.ActiveControl is TextBoxBase)
            {
                return false;
            }

            var index = Array.IndexOf(TerrainPlanner.BrushSizes, this.terrainBrushSize);
            if (index < 0)
            {
                index = 1;
            }

            var next = Math.Max(0, Math.Min(TerrainPlanner.BrushSizes.Length - 1, index + direction));
            this.model.TerrainBrushSizeChanged(TerrainPlanner.BrushSizes[next]);
            e.Handled = true;
            e.SuppressKeyPress = true;
            return true;
        }
    }
}
