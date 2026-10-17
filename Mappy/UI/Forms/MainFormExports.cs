namespace Mappy.UI.Forms
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Linq;
    using System.Windows.Forms;

    using Mappy.Data;
    using Mappy.IO;
    using Mappy.Models;
    using Mappy.Util;

    using FormsListViewItem = System.Windows.Forms.ListViewItem;

    public partial class MainForm
    {
        private const int ExportThumbnailSize = 128;

        private readonly List<ExportListEntry> exportEntries = new List<ExportListEntry>();

        private ImageList exportImageList;

        private int nextExportNumber = 1;

        private static Bitmap CreateExportThumbnail(IMapTile tile)
        {
            using (var full = ImgUtil.GetBitmapFromTilegrid(tile.TileGrid))
            {
                var thumb = new Bitmap(ExportThumbnailSize, ExportThumbnailSize);
                using (var graphics = Graphics.FromImage(thumb))
                {
                    graphics.Clear(Color.Black);
                    graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
                    graphics.PixelOffsetMode = PixelOffsetMode.Half;
                    var scale = Math.Min(
                        ExportThumbnailSize / (float)Math.Max(1, full.Width),
                        ExportThumbnailSize / (float)Math.Max(1, full.Height));
                    var width = Math.Max(1, (int)Math.Round(full.Width * scale));
                    var height = Math.Max(1, (int)Math.Round(full.Height * scale));
                    var x = (ExportThumbnailSize - width) / 2;
                    var y = (ExportThumbnailSize - height) / 2;
                    graphics.DrawImage(full, new Rectangle(x, y, width, height));
                }

                return thumb;
            }
        }

        private void InitializeExportsTab()
        {
            this.exportImageList = new ImageList(this.components)
            {
                ImageSize = new Size(ExportThumbnailSize, ExportThumbnailSize),
                ColorDepth = ColorDepth.Depth32Bit,
            };
            this.exportsListView.LargeImageList = this.exportImageList;
            this.exportsListView.View = View.LargeIcon;
            this.mapViewPanel.PreserveSelectionOnLeave = this.IsCursorOverExportsTab;
            this.UpdateExportActionButtons();
        }

        private bool IsCursorOverExportsTab()
        {
            var bounds = this.exportsTab.RectangleToScreen(this.exportsTab.ClientRectangle);
            return bounds.Contains(Cursor.Position);
        }

        private void ExportAddButtonClick(object sender, EventArgs e)
        {
            if (this.model == null)
            {
                return;
            }

            var tile = this.model.CopySelectedFloatingTile();
            if (tile == null)
            {
                return;
            }

            this.exportEntries.Add(new ExportListEntry(tile, this.NextDefaultExportName()));
            this.RefreshExportsList(this.exportEntries.Count - 1);
        }

        private void ExportRemoveButtonClick(object sender, EventArgs e)
        {
            if (this.exportsListView.SelectedIndices.Count == 0)
            {
                return;
            }

            var index = this.exportsListView.SelectedIndices[0];
            this.exportEntries.RemoveAt(index);
            var next = Math.Min(index, this.exportEntries.Count - 1);
            this.RefreshExportsList(next);
        }

        private void ExportExportButtonClick(object sender, EventArgs e)
        {
            if (this.model == null || this.exportEntries.Count == 0)
            {
                return;
            }

            var entries = this.exportEntries
                .Select(x => new SectionExportEntry(x.Tile, x.FileName))
                .ToList();
            this.model.ExportSectionAmendments(entries);
        }

        private void ExportsListViewItemActivate(object sender, EventArgs e)
        {
            if (this.exportsListView.SelectedIndices.Count == 0)
            {
                return;
            }

            var index = this.exportsListView.SelectedIndices[0];
            var current = this.exportEntries[index];
            using (var dialog = new ExportSectionNameForm(current.FileName))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                if (this.HasExportName(dialog.FileName, index))
                {
                    MessageBox.Show(
                        this,
                        "A section with that name is already in the list.",
                        "Section name",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                current.FileName = dialog.FileName;
                this.RefreshExportsList(index);
            }
        }

        private void ExportsListViewSelectedIndexChanged(object sender, EventArgs e)
        {
            this.UpdateExportActionButtons();
        }

        private void RefreshExportsList(int selectIndex)
        {
            this.exportsListView.BeginUpdate();
            this.exportsListView.Items.Clear();
            this.exportImageList.Images.Clear();

            for (var i = 0; i < this.exportEntries.Count; i++)
            {
                using (var thumbnail = CreateExportThumbnail(this.exportEntries[i].Tile))
                {
                    this.exportImageList.Images.Add(thumbnail);
                }

                this.exportsListView.Items.Add(new FormsListViewItem(this.exportEntries[i].FileName, i));
            }

            this.exportsListView.EndUpdate();

            if (selectIndex >= 0 && selectIndex < this.exportsListView.Items.Count)
            {
                this.exportsListView.Items[selectIndex].Selected = true;
                this.exportsListView.Items[selectIndex].Focused = true;
            }

            this.UpdateExportActionButtons();
        }

        private void UpdateExportActionButtons()
        {
            this.exportRemoveButton.Enabled = this.exportsListView.SelectedIndices.Count > 0;
            this.exportExportButton.Enabled = this.exportEntries.Count > 0;
        }

        private string NextDefaultExportName()
        {
            while (true)
            {
                var name = this.nextExportNumber + ".sct";
                this.nextExportNumber++;
                if (!this.HasExportName(name, -1))
                {
                    return name;
                }
            }
        }

        private bool HasExportName(string fileName, int ignoreIndex)
        {
            for (var i = 0; i < this.exportEntries.Count; i++)
            {
                if (i == ignoreIndex)
                {
                    continue;
                }

                if (string.Equals(this.exportEntries[i].FileName, fileName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private sealed class ExportListEntry
        {
            public ExportListEntry(IMapTile tile, string fileName)
            {
                this.Tile = tile;
                this.FileName = fileName;
            }

            public IMapTile Tile { get; }

            public string FileName { get; set; }
        }
    }
}
