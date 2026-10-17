namespace Mappy.IO
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.Linq;

    using Mappy.Collections;
    using Mappy.Data;
    using Mappy.Util;

    using TAUtil.Gdi.Bitmap;
    using TAUtil.Sct;
    using TAUtil.Tnt;

    /// <summary>
    /// Adapter for writing an <see cref="IMapTile"/> as an SCT section.
    /// </summary>
    public class MapTileSctAdapter : ISctSource
    {
        private readonly IMapTile tile;

        private readonly byte[][] tiles;

        private readonly IDictionary<Bitmap, int> reverseTiles;

        private readonly byte[] minimap;

        public MapTileSctAdapter(IMapTile tile)
        {
            this.tile = tile;
            var bitmaps = Util.GetUsedTiles(tile).ToArray();
            this.reverseTiles = Util.ReverseMapping(bitmaps);
            this.tiles = bitmaps.Select(bitmap => BitmapConvert.ToBytes(bitmap)).ToArray();
            this.minimap = BuildMinimap(tile);
        }

        public int DataWidth => this.tile.TileGrid.Width;

        public int DataHeight => this.tile.TileGrid.Height;

        public int TileCount => this.tiles.Length;

        public IEnumerable<byte[]> EnumerateTiles()
        {
            return this.tiles;
        }

        public IEnumerable<int> EnumerateData()
        {
            return this.tile.TileGrid.Select(x => this.reverseTiles[x]);
        }

        public IEnumerable<TileAttr> EnumerateAttrs()
        {
            var heightWidth = this.tile.HeightGrid.Width;
            var heightHeight = this.tile.HeightGrid.Height;
            for (var y = 0; y < this.DataHeight * 2; y++)
            {
                for (var x = 0; x < this.DataWidth * 2; x++)
                {
                    var height = 0;
                    if (x < heightWidth && y < heightHeight)
                    {
                        height = this.tile.HeightGrid.Get(x, y);
                    }

                    if (height < 0)
                    {
                        height = 0;
                    }
                    else if (height > 255)
                    {
                        height = 255;
                    }

                    yield return new TileAttr
                    {
                        Height = (byte)height,
                        Feature = TileAttr.FeatureNone,
                    };
                }
            }
        }

        public byte[] GetMinimap()
        {
            return this.minimap;
        }

        private static byte[] BuildMinimap(IMapTile tile)
        {
            var minimapBytes = new byte[SctReader.MinimapWidth * SctReader.MinimapHeight];
            if (tile.TileGrid.Width <= 0 || tile.TileGrid.Height <= 0)
            {
                return minimapBytes;
            }

            using (var graphic = ImgUtil.GetBitmapFromTilegrid(tile.TileGrid))
            {
                var destWidth = SctReader.MinimapWidth;
                var destHeight = SctReader.MinimapHeight;
                var scale = Math.Min(destWidth / (float)graphic.Width, destHeight / (float)graphic.Height);
                var drawWidth = Math.Max(1, (int)Math.Round(graphic.Width * scale));
                var drawHeight = Math.Max(1, (int)Math.Round(graphic.Height * scale));
                if (drawWidth > destWidth)
                {
                    drawWidth = destWidth;
                }

                if (drawHeight > destHeight)
                {
                    drawHeight = destHeight;
                }

                var offsetX = (destWidth - drawWidth) / 2;
                var offsetY = (destHeight - drawHeight) / 2;

                var data = graphic.LockBits(
                    new Rectangle(0, 0, graphic.Width, graphic.Height),
                    ImageLockMode.ReadOnly,
                    PixelFormat.Format8bppIndexed);
                try
                {
                    unsafe
                    {
                        var scan0 = (byte*)data.Scan0;
                        for (var y = 0; y < drawHeight; y++)
                        {
                            var srcY = Math.Min(graphic.Height - 1, (y * graphic.Height) / drawHeight);
                            var srcRow = scan0 + (srcY * data.Stride);
                            var destRow = ((offsetY + y) * destWidth) + offsetX;
                            for (var x = 0; x < drawWidth; x++)
                            {
                                var srcX = Math.Min(graphic.Width - 1, (x * graphic.Width) / drawWidth);
                                minimapBytes[destRow + x] = srcRow[srcX];
                            }
                        }
                    }
                }
                finally
                {
                    graphic.UnlockBits(data);
                }
            }

            return minimapBytes;
        }
    }
}
