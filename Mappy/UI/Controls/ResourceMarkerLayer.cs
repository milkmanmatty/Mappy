namespace Mappy.UI.Controls
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using System.Drawing.Drawing2D;
    using System.Drawing.Text;
    using System.Globalization;

    using Mappy;
    using Mappy.Models;
    using Mappy.Util;

    public sealed class ResourceMarkerLayer : AbstractLayer
    {
        private const float ScreenLineWidth = 4f;

        private const float ScreenSelectedOutlineWidth = 7f;

        private IMainModel map;

        private Point[] previewPoints;

        private bool regionsDirty = true;

        private float lastZoom = 1f;

        private IReadOnlyList<ResourceRegionLabel> labels = Array.Empty<ResourceRegionLabel>();

        public ResourceMarkerLayer()
        {
            MappySettings.SettingsSaved += this.SettingsSaved;
        }

        public void SetMap(IMainModel map)
        {
            this.map = map;
            this.previewPoints = null;
            this.InvalidateMarkers();
        }

        public void SetPreview(IReadOnlyList<Point> points)
        {
            if (points == null || points.Count == 0)
            {
                this.previewPoints = null;
            }
            else
            {
                var copy = new Point[points.Count];
                for (var i = 0; i < points.Count; i++)
                {
                    copy[i] = points[i];
                }

                this.previewPoints = copy;
            }

            this.OnLayerChanged();
        }

        public void InvalidateMarkers()
        {
            this.regionsDirty = true;
            this.OnLayerChanged();
        }

        public void InvalidateAppearance()
        {
            this.OnLayerChanged();
        }

        public Guid? HitTestLine(Point point)
        {
            if (this.map?.MarkerLines == null)
            {
                return null;
            }

            var threshold = 10f / Math.Max(this.lastZoom, 0.05f);
            var best = double.MaxValue;
            Guid? hit = null;
            var limit = threshold * threshold;
            foreach (var line in this.map.MarkerLines)
            {
                var distance = DistanceToPolylineSquared(point, line.Points);
                if (distance <= limit && distance < best)
                {
                    best = distance;
                    hit = line.Id;
                }
            }

            return hit;

            double DistanceToPolylineSquared(Point probe, IReadOnlyList<Point> points)
            {
                if (points == null || points.Count == 0)
                {
                    return double.MaxValue;
                }

                if (points.Count == 1)
                {
                    var dx = probe.X - points[0].X;
                    var dy = probe.Y - points[0].Y;
                    return (dx * dx) + (dy * dy);
                }

                var bestDistance = double.MaxValue;
                for (var i = 1; i < points.Count; i++)
                {
                    var distance = DistanceToSegmentSquared(probe, points[i - 1], points[i]);
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                    }
                }

                return bestDistance;
            }

            double DistanceToSegmentSquared(Point probe, Point start, Point end)
            {
                var abx = end.X - start.X;
                var aby = end.Y - start.Y;
                var apx = probe.X - start.X;
                var apy = probe.Y - start.Y;
                var lengthSquared = (abx * abx) + (aby * aby);
                if (lengthSquared == 0)
                {
                    return (apx * apx) + (apy * apy);
                }

                var t = ((apx * (double)abx) + (apy * (double)aby)) / lengthSquared;
                if (t < 0)
                {
                    t = 0;
                }
                else if (t > 1)
                {
                    t = 1;
                }

                var closestX = start.X + (t * abx);
                var closestY = start.Y + (t * aby);
                var dx = probe.X - closestX;
                var dy = probe.Y - closestY;
                return (dx * dx) + (dy * dy);
            }
        }

        protected override void DoDraw(Graphics graphics, Rectangle clipRectangle)
        {
            if (this.map == null)
            {
                return;
            }

            var zoom = Math.Abs(graphics.Transform.Elements[0]);
            if (zoom < 0.01f)
            {
                zoom = 1f;
            }

            this.lastZoom = zoom;
            this.EnsureLabels();

            var state = graphics.Save();
            try
            {
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                var color = MappySettings.Settings.GetResourceMarkerLineColorOrDefault();
                var selected = this.map.SelectedMarkerLine;
                var penWidth = ScreenLineWidth / zoom;
                var outlineWidth = ScreenSelectedOutlineWidth / zoom;
                using (var outline = new Pen(Color.White, outlineWidth))
                using (var pen = new Pen(color, penWidth))
                using (var previewPen = new Pen(Color.FromArgb(170, color), penWidth))
                {
                    outline.StartCap = LineCap.Round;
                    outline.EndCap = LineCap.Round;
                    outline.LineJoin = LineJoin.Round;
                    pen.StartCap = LineCap.Round;
                    pen.EndCap = LineCap.Round;
                    pen.LineJoin = LineJoin.Round;
                    previewPen.StartCap = LineCap.Round;
                    previewPen.EndCap = LineCap.Round;
                    previewPen.LineJoin = LineJoin.Round;

                    foreach (var line in this.map.MarkerLines)
                    {
                        if (selected.HasValue && line.Id == selected.Value)
                        {
                            DrawPolyline(graphics, outline, line.Points);
                        }

                        DrawPolyline(graphics, pen, line.Points);
                    }

                    DrawPolyline(graphics, previewPen, this.previewPoints);
                }

                using (var font = new Font(
                    FontFamily.GenericSansSerif,
                    18f,
                    FontStyle.Bold,
                    GraphicsUnit.Point))
                {
                    foreach (var label in this.labels)
                    {
                        this.DrawLabel(graphics, font, zoom, label);
                    }
                }
            }
            finally
            {
                graphics.Restore(state);
            }
        }

        private void EnsureLabels()
        {
            if (!this.regionsDirty)
            {
                return;
            }

            this.labels = ResourceMarkerRegions.Build(this.map);
            this.regionsDirty = false;
        }

        private void DrawLabel(Graphics graphics, Font font, float zoom, ResourceRegionLabel label)
        {
            var state = graphics.Save();
            try
            {
                graphics.ScaleTransform(1f / zoom, 1f / zoom);
                graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                graphics.SmoothingMode = SmoothingMode.None;

                var text = string.Format(
                    CultureInfo.CurrentCulture,
                    "M: {0}\nR(M): {1}\nR(E): {2}",
                    MetalSpotDisplayFormat.FormatCalculatedTotal(label.PatchMetal),
                    label.WreckMetal,
                    label.Energy);
                var measured = graphics.MeasureString(text, font);
                var boxWidth = measured.Width + 16f;
                var boxHeight = measured.Height + 10f;
                var box = new RectangleF(
                    (label.Location.X * zoom) - (boxWidth / 2f),
                    (label.Location.Y * zoom) - (boxHeight / 2f),
                    boxWidth,
                    boxHeight);

                using (var back = new SolidBrush(Color.FromArgb(170, 0, 0, 0)))
                using (var textBrush = new SolidBrush(Color.White))
                using (var format = new StringFormat())
                {
                    format.Alignment = StringAlignment.Center;
                    format.LineAlignment = StringAlignment.Center;
                    graphics.FillRectangle(back, box);
                    graphics.DrawString(text, font, textBrush, box, format);
                }
            }
            finally
            {
                graphics.Restore(state);
            }
        }

        private static void DrawPolyline(Graphics graphics, Pen pen, IReadOnlyList<Point> points)
        {
            if (points == null || points.Count < 2)
            {
                return;
            }

            var drawable = points as Point[] ?? points.ToArray();
            graphics.DrawLines(pen, drawable);
        }

        private void SettingsSaved(object sender, EventArgs e)
        {
            this.InvalidateAppearance();
        }
    }
}
