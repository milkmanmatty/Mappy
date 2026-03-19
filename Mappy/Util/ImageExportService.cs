namespace Mappy.Util
{
    using System.Drawing;
    using System.Drawing.Imaging;
    using Data;

    public class ImageExportService
    {
        public void ExportSection(IMapTile section, string pngFilename, string heightFilename)
        {
            // Export the heightmap
            Bitmap heightBitmap = ImgUtil.ExportHeightmap(section.HeightGrid);
            heightBitmap.Save(heightFilename, ImageFormat.Png);
            heightBitmap.Dispose();

            // Export the graphics to PNG
            var graphicBitmap = ImgUtil.ExportGraphic(section.TileGrid);
            graphicBitmap.Save(pngFilename, ImageFormat.Png);
            graphicBitmap.Dispose();
        }
    }
}