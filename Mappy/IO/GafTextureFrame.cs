namespace Mappy.IO
{
    using System;
    using System.Drawing;

    public sealed class GafTextureFrame
    {
        public GafTextureFrame(Bitmap bitmap, byte[] indices, int width, int height, byte transparencyIndex)
        {
            this.Bitmap = bitmap ?? throw new ArgumentNullException(nameof(bitmap));
            this.Indices = indices ?? throw new ArgumentNullException(nameof(indices));
            this.Width = width;
            this.Height = height;
            this.TransparencyIndex = transparencyIndex;
        }

        public Bitmap Bitmap { get; }

        public byte[] Indices { get; }

        public int Width { get; }

        public int Height { get; }

        public byte TransparencyIndex { get; }
    }
}
