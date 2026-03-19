namespace Mappy.UI.Forms
{
    using System;
    using System.Drawing;
    using System.Globalization;
    using System.Reactive.Linq;
    using System.Windows.Forms;
    using Controls;
    using Mappy;
    using Models;
    using Util;

    public partial class MainForm : Form
    {
        private IMainFormViewModel model;

        public MainForm()
        {
            this.InitializeComponent();
        }

        public MapViewPanel MapViewPanel => this.mapViewPanel;

        public SectionView SectionView => this.sectionsView;

        public SectionView FeatureView => this.featureView;

        public void SetModel(IMainFormViewModel newModel)
        {
            var gridSize = newModel.GridSize.CombineLatest(
                newModel.GridVisible,
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
            newModel.CanSave.Subscribe(x => this.saveMenuItem.Enabled = x);
            newModel.CanSaveAs.Subscribe(x => this.saveAsMenuItem.Enabled = x);
            newModel.CanCloseMap.Subscribe(x => this.closeMenuItem.Enabled = x);

            newModel.CanImportMinimap.Subscribe(x => this.importMinimapMenuItem.Enabled = x);
            newModel.CanExportMinimap.Subscribe(x => this.exportMinimapMenuItem.Enabled = x);

            newModel.CanImportHeightmap.Subscribe(x => this.importHeightmapMenuItem.Enabled = x);
            newModel.CanExportHeightmap.Subscribe(x => this.exportHeightmapMenuItem.Enabled = x);

            newModel.CanExportMapImage.Subscribe(x => this.exportMapImageMenuItem.Enabled = x);
            newModel.CanImportCustomSection.Subscribe(x => this.importCustomSectionMenuItem.Enabled = x);

            // edit menu bindings
            newModel.CanUndo.Subscribe(x => this.undoMenuItem.Enabled = x);
            newModel.CanRedo.Subscribe(x => this.redoMenuItem.Enabled = x);
            newModel.CanCopy.Subscribe(x => this.copyMenuItem.Enabled = x);
            newModel.CanCut.Subscribe(x => this.cutMenuItem.Enabled = x);
            newModel.CanPaste.Subscribe(x => this.pasteMenuItem.Enabled = x);
            newModel.CanFill.Subscribe(x => this.fillMenuItem.Enabled = x);
            newModel.CanResizeMap.Subscribe(x => this.resizeMapMenuItem.Enabled = x);
            newModel.CanFlip.Subscribe(x =>
            {
                this.flipHorizontallyMenuItem.Enabled = x;
                this.flipVerticallyMenuItem.Enabled = x;
            });

            newModel.CanGenerateMinimap.Subscribe(x => this.generateMinimapMenuItem.Enabled = x);
            newModel.CanGenerateMinimapHighQuality.Subscribe(x => this.generateMinimapHighQualityMenuItem.Enabled = x);

            newModel.CanOpenAttributes.Subscribe(x => this.mapAttributesMenuItem.Enabled = x);

            // view menu bindings
            newModel.MinimapVisible.Subscribe(x => this.toggleMinimapMenuItem.Checked = x);
            newModel.HeightmapVisible.Subscribe(x => this.toggleHeightmapMenuItem.Checked = x);
            newModel.HeightGridVisible.Subscribe(x => this.toggleHeightGridMenuItem.Checked = x);
            newModel.VoidsVisible.Subscribe(x => this.toggleVoidsMenuItem.Checked = x);
            newModel.FeaturesVisible.Subscribe(x => this.toggleFeaturesMenuItem.Checked = x);

            // sea level widget bindings
            newModel.CanChangeSeaLevel.Subscribe(x => this.seaLevelLabel.Enabled = x);
            newModel.CanChangeSeaLevel.Subscribe(x => this.seaLevelValueLabel.Enabled = x);
            newModel.CanChangeSeaLevel.Subscribe(x => this.seaLevelTrackbar.Enabled = x);

            newModel.SeaLevel.Subscribe(x => this.seaLevelTrackbar.Value = x);
            newModel.SeaLevel
                .Select(x => x.ToString(CultureInfo.CurrentCulture))
                .Subscribe(x => this.seaLevelValueLabel.Text = x);
            newModel.HeightEditInterval.Subscribe(x => this.intervalNumericUpDown.Value = x);
            newModel.HeightEditMode.Subscribe(
                x =>
                    {
                        this.incrementDecrementHeightRadioButton.Checked = x == HeightEditMode.IncrementDecrement;
                        this.setHeightRadioButton.Checked = x == HeightEditMode.Set;
                        this.intervalLabel.Enabled = x == HeightEditMode.IncrementDecrement;
                        this.intervalNumericUpDown.Enabled = x == HeightEditMode.IncrementDecrement;
                        this.selectedHeightLabel.Enabled = x == HeightEditMode.Set;
                        this.selectedHeightNumericUpDown.Enabled = x == HeightEditMode.Set;
                    });
            newModel.HeightEditSetValue.Subscribe(x => this.selectedHeightNumericUpDown.Value = x);
            newModel.HeightEditCursorSize.Subscribe(x => this.cursorSizeNumericUpDown.Value = x);
            newModel.VoidEditCursorSize.Subscribe(x => this.voidCursorSizeNumericUpDown.Value = x);

            // title text bindings
            newModel.TitleText.Subscribe(x => this.Text = x);

            newModel.MousePositionText.Subscribe(x => this.mousePositionLabel.Text = x);
            newModel.HeightText.Subscribe(x => this.heightLabel.Text = x);
            newModel.VoidText.Subscribe(x => this.voidLabel.Text = x);
            newModel.HoveredFeatureText.Subscribe(x => this.hoveredFeatureLabel.Text = x);

            this.model = newModel;
        }

        private void OpenMenuItemClick(object sender, EventArgs e)
        {
            this.model.OpenMenuItemClick();
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

        private void AboutMenuItemClick(object sender, EventArgs e)
        {
            this.model.AboutMenuItemClick();
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

        private void MapAttributesMenuItemClick(object sender, EventArgs e)
        {
            this.model.MapAttributesMenuItemClick();
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

        private void VoidCursorSizeNumericUpDownValueChanged(object sender, EventArgs e)
        {
            this.model.VoidEditCursorSizeChanged((int)this.voidCursorSizeNumericUpDown.Value);
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

        private void FlipHorizontallyMenuItemClick(object sender, EventArgs e)
        {
            this.model.FlipHorizontallyMenuItemClick();
        }

        private void FlipVerticallyMenuItemClick(object sender, EventArgs e)
        {
            this.model.FlipVerticallyMenuItemClick();
        }

        private void MainFormLoad(object sender, EventArgs e)
        {
            this.RestoreWindowState();
            this.FormClosed += this.MainFormFormClosed;
            this.model.Load();
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

            var minWidth = this.sidebarSplitter.MinSize;
            var maxWidth = this.ClientSize.Width - this.sidebarSplitter.MinExtra;
            if (maxWidth < minWidth)
            {
                maxWidth = minWidth;
            }

            this.sidebarTabs.Width = Math.Max(minWidth, Math.Min(maxWidth, settings.SidebarTabsWidth));
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
            settings.SidebarTabsWidth = this.sidebarTabs.Width;
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

        private void GUITabs_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.sidebarTabs.SelectedTab == null)
            {
                return;
            }

            this.model.ChangeSelectedTabType(Util.MapTabNameToGUIType(this.sidebarTabs.SelectedTab.Name));
        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.D1)
            {
                this.sidebarTabs.SelectedTab = this.sectionsTab;
                e.Handled = true;
            }

            if (e.Control && e.KeyCode == Keys.D2)
            {
                this.sidebarTabs.SelectedTab = this.featuresTab;
                e.Handled = true;
            }

            if (e.Control && e.KeyCode == Keys.D3)
            {
                this.sidebarTabs.SelectedTab = this.startPositionsTab;
                e.Handled = true;
            }

            if (e.Control && e.KeyCode == Keys.D4)
            {
                this.sidebarTabs.SelectedTab = this.heightTab;
                e.Handled = true;
            }

            if (e.Control && e.KeyCode == Keys.D5)
            {
                this.sidebarTabs.SelectedTab = this.voidTab;
                e.Handled = true;
            }

            if (e.Control && e.KeyCode == Keys.D6)
            {
                this.sidebarTabs.SelectedTab = this.attributesTab;
                e.Handled = true;
            }

            if (e.Shift && e.KeyCode >= Keys.D0 && e.KeyCode <= Keys.D9)
            {
                var playerIndex = e.KeyCode == Keys.D0 ? 9 : (e.KeyCode - Keys.D1);
                this.model.CenterViewOnStartPosition(playerIndex);
                e.Handled = true;
            }
        }
    }
}
