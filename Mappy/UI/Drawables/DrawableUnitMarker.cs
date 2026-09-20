namespace Mappy.UI.Drawables
{
    using System;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Drawing.Text;

    public sealed class DrawableUnitMarker : AbstractDrawable
    {
        private readonly Bitmap body;

        private readonly string label;

        private readonly int labelHeight;

        private readonly int pad;

        private readonly int labelContentWidth;

        private readonly float opacity;

        public DrawableUnitMarker(
            Bitmap body,
            string label,
            int labelHeight,
            int pad,
            int labelContentWidth,
            float opacity = 1f)
        {
            this.body = body ?? throw new ArgumentNullException(nameof(body));
            this.label = label ?? string.Empty;
            this.labelHeight = labelHeight;
            this.pad = pad;
            this.labelContentWidth = labelContentWidth;
            this.opacity = opacity;
        }

        public override Size Size => this.body.Size;

        public override int Width => this.body.Width;

        public override int Height => this.body.Height;

        public override void Draw(Graphics graphics, Rectangle clipRectangle)
        {
            graphics.DrawImageUnscaled(this.body, 0, 0);

            if (string.IsNullOrEmpty(this.label))
            {
                return;
            }

            this.DrawLabel(graphics);
        }

        private void DrawLabel(Graphics graphics)
        {
            var settings = MappySettings.Settings;
            var textColor = ApplyOpacity(settings.GetUnitNameTextColorOrDefault(), this.opacity);
            var backplateColor = ApplyOpacity(settings.GetUnitNameBackplateColorOrDefault(), this.opacity);

            var labelY = this.body.Height - this.labelHeight - this.pad;
            var labelRect = new Rectangle(
                this.pad,
                labelY,
                this.body.Width - (this.pad * 2),
                this.labelHeight);

            // Map zoom comes from LayerView's ScaleTransform. Counter-scale so glyphs
            // are rasterized at screen resolution instead of being bitmap-upscaled.
            var zoom = Math.Abs(graphics.Transform.Elements[0]);
            if (zoom < 0.01f)
            {
                zoom = 1f;
            }

            var state = graphics.Save();
            try
            {
                graphics.ScaleTransform(1f / zoom, 1f / zoom);
                graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                graphics.SmoothingMode = SmoothingMode.None;

                var screenRect = new RectangleF(
                    labelRect.X * zoom,
                    labelRect.Y * zoom,
                    labelRect.Width * zoom,
                    labelRect.Height * zoom);

                using (var font = new Font(
                    SystemFonts.DefaultFont.FontFamily,
                    SystemFonts.DefaultFont.SizeInPoints * zoom,
                    SystemFonts.DefaultFont.Style,
                    GraphicsUnit.Point))
                {
                    if (settings.ShowUnitNameBackplate)
                    {
                        var backplateWidth = Math.Min(screenRect.Width, this.labelContentWidth * zoom);
                        var backplateX = screenRect.X + ((screenRect.Width - backplateWidth) / 2f);
                        using (var br = new SolidBrush(backplateColor))
                        {
                            graphics.FillRectangle(
                                br,
                                backplateX,
                                screenRect.Y,
                                backplateWidth,
                                screenRect.Height);
                        }
                    }

                    using (var brush = new SolidBrush(textColor))
                    using (var format = new StringFormat())
                    {
                        format.Alignment = StringAlignment.Center;
                        format.LineAlignment = StringAlignment.Center;
                        format.FormatFlags = StringFormatFlags.NoWrap | StringFormatFlags.NoClip;
                        format.Trimming = StringTrimming.None;
                        graphics.DrawString(this.label, font, brush, screenRect, format);
                    }
                }
            }
            finally
            {
                graphics.Restore(state);
            }
        }

        private static Color ApplyOpacity(Color color, float opacity)
        {
            if (opacity >= 0.995f)
            {
                return color;
            }

            return Color.FromArgb(
                Math.Max(0, Math.Min(255, (int)Math.Round(color.A * opacity))),
                color);
        }
    }
}
