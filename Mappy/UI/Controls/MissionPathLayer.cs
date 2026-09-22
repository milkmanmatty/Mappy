namespace Mappy.UI.Controls
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Drawing.Text;
    using System.Globalization;

    using Mappy;
    using Mappy.Collections;
    using Mappy.Data;
    using Mappy.Models;
    using Mappy.Util;

    public sealed class MissionPathLayer : AbstractLayer
    {
        private const float ScreenLineWidth = 2f;

        private IMainModel map;

        public MissionPathLayer()
        {
            this.Enabled = false;
        }

        public void SetMap(IMainModel map)
        {
            this.map = map;
            this.InvalidatePaths();
        }

        public void InvalidatePaths()
        {
            if (this.Enabled)
            {
                this.OnLayerChanged();
            }
        }

        protected override void DoDraw(Graphics graphics, Rectangle clipRectangle)
        {
            if (this.map == null)
            {
                return;
            }

            var schemas = this.map.Attributes.Schemas;
            if (schemas.Count == 0)
            {
                return;
            }

            var settings = MappySettings.Settings;
            var zoom = Math.Abs(graphics.Transform.Elements[0]);
            if (zoom < 0.01f)
            {
                zoom = 1f;
            }

            var inactiveOpacity = settings.GetInactiveSchemaOpacityOrDefault();
            var heightGrid = this.map.BaseTile != null ? this.map.BaseTile.HeightGrid : null;
            var activeSchema = this.map.ActiveSchemaIndex;

            var state = graphics.Save();
            try
            {
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var font = new Font(
                    SystemFonts.DefaultFont.FontFamily,
                    SystemFonts.DefaultFont.SizeInPoints * zoom,
                    SystemFonts.DefaultFont.Style,
                    GraphicsUnit.Point))
                {
                    for (var pass = 0; pass < 2; pass++)
                    {
                        for (var schemaIndex = 0; schemaIndex < schemas.Count; schemaIndex++)
                        {
                            var isActive = schemaIndex == activeSchema;
                            if ((pass == 0 && isActive) || (pass == 1 && !isActive))
                            {
                                continue;
                            }

                            var opacity = isActive ? 1f : inactiveOpacity;
                            this.DrawSchema(
                                graphics,
                                font,
                                zoom,
                                heightGrid,
                                schemas[schemaIndex].Units,
                                opacity,
                                settings);
                        }
                    }
                }
            }
            finally
            {
                graphics.Restore(state);
            }
        }

        private void DrawSchema(
            Graphics graphics,
            Font font,
            float zoom,
            IGrid<int> heightGrid,
            IList<SchemaUnit> units,
            float opacity,
            Configuration settings)
        {
            var moveColor = this.ApplyOpacity(settings.GetMissionMovePathColorOrDefault(), opacity);
            var attackColor = this.ApplyOpacity(settings.GetMissionAttackPathColorOrDefault(), opacity);
            var patrolColor = this.ApplyOpacity(settings.GetMissionPatrolPathColorOrDefault(), opacity);
            var patrolDashColor = this.ApplyOpacity(settings.GetMissionPatrolDashColorOrDefault(), opacity);
            var waitText = this.ApplyOpacity(settings.GetMissionWaitTextColorOrDefault(), opacity);
            var waitBackplate = this.ApplyOpacity(settings.GetMissionWaitBackplateColorOrDefault(), opacity);
            var penWidth = ScreenLineWidth / zoom;

            using (var movePen = new Pen(moveColor, penWidth))
            using (var attackPen = new Pen(attackColor, penWidth))
            using (var patrolPen = new Pen(patrolColor, penWidth))
            using (var patrolDashPen = new Pen(patrolDashColor, penWidth))
            {
                movePen.StartCap = LineCap.Round;
                movePen.EndCap = LineCap.Round;
                attackPen.StartCap = LineCap.Round;
                attackPen.EndCap = LineCap.Round;
                patrolPen.StartCap = LineCap.Round;
                patrolPen.EndCap = LineCap.Round;
                patrolDashPen.StartCap = LineCap.Flat;
                patrolDashPen.EndCap = LineCap.Flat;
                patrolDashPen.DashStyle = DashStyle.Custom;
                patrolDashPen.DashPattern = new[] { 6f, 4f };

                foreach (var unit in units)
                {
                    if (unit == null || string.IsNullOrWhiteSpace(unit.InitialMission))
                    {
                        continue;
                    }

                    var route = InitialMissionPath.Build(
                        unit.InitialMission,
                        unit.XPos,
                        unit.ZPos,
                        unit.Id,
                        units);

                    foreach (var segment in route.Segments)
                    {
                        var start = this.Project(heightGrid, segment.X1, segment.Z1);
                        var end = this.Project(heightGrid, segment.X2, segment.Z2);
                        switch (segment.Kind)
                        {
                            case InitialMissionPath.SegmentKind.Patrol:
                                graphics.DrawLine(patrolPen, start, end);
                                graphics.DrawLine(patrolDashPen, start, end);
                                break;
                            case InitialMissionPath.SegmentKind.Attack:
                                graphics.DrawLine(attackPen, start, end);
                                break;
                            default:
                                graphics.DrawLine(movePen, start, end);
                                break;
                        }
                    }

                    foreach (var wait in route.Waits)
                    {
                        var point = this.Project(heightGrid, wait.X, wait.Z);
                        this.DrawWaitBox(graphics, font, zoom, point, wait.Seconds, wait.StackIndex, waitText, waitBackplate);
                    }
                }
            }
        }

        private void DrawWaitBox(
            Graphics graphics,
            Font font,
            float zoom,
            Point point,
            int seconds,
            int stackIndex,
            Color textColor,
            Color backplateColor)
        {
            var state = graphics.Save();
            try
            {
                graphics.ScaleTransform(1f / zoom, 1f / zoom);
                graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                graphics.SmoothingMode = SmoothingMode.None;

                var text = seconds.ToString(CultureInfo.InvariantCulture);
                var measured = graphics.MeasureString(text, font);
                var boxWidth = Math.Max(14f, measured.Width + 6f);
                var boxHeight = Math.Max(14f, measured.Height + 2f);
                var boxX = (point.X * zoom) - (boxWidth / 2f);
                var boxY = (point.Y * zoom) - (boxHeight / 2f) - (stackIndex * (boxHeight + 2f));
                var box = new RectangleF(boxX, boxY, boxWidth, boxHeight);

                using (var back = new SolidBrush(backplateColor))
                {
                    graphics.FillRectangle(back, box);
                }

                using (var brush = new SolidBrush(textColor))
                using (var format = new StringFormat())
                {
                    format.Alignment = StringAlignment.Center;
                    format.LineAlignment = StringAlignment.Center;
                    graphics.DrawString(text, font, brush, box, format);
                }
            }
            finally
            {
                graphics.Restore(state);
            }
        }

        private Point Project(IGrid<int> heightGrid, int x, int z)
        {
            var heightMid = 0;
            if (heightGrid != null)
            {
                var fx = x / 16;
                var fy = z / 16;
                if (fx >= 0 && fy >= 0 && fx < heightGrid.Width - 1 && fy < heightGrid.Height - 1)
                {
                    heightMid = Util.ComputeMidpointHeight(heightGrid, fx, fy);
                }
                else if (fx >= 0 && fy >= 0 && fx < heightGrid.Width && fy < heightGrid.Height)
                {
                    heightMid = heightGrid.Get(fx, fy);
                }
            }

            return new Point(x, z - (heightMid / 2));
        }

        private Color ApplyOpacity(Color color, float opacity)
        {
            if (opacity >= 0.995f)
            {
                return color;
            }

            return Color.FromArgb(
                Math.Max(0, Math.Min(255, (int)Math.Round(color.A * opacity))),
                color.R,
                color.G,
                color.B);
        }
    }
}
