namespace Mappy.Models
{
    using System;
    using System.Drawing;

    using Mappy.Data;

    [Serializable]
    public class SchemaUnitClipboardRecord
    {
        public SchemaUnitClipboardRecord()
        {
        }

        public SchemaUnitClipboardRecord(SchemaUnit u, Point viewportLocation)
        {
            this.Unitname = u.Unitname;
            this.Ident = u.Ident ?? string.Empty;
            this.VPOffsetX = u.XPos - viewportLocation.X;
            this.VPOffsetY = u.ZPos - viewportLocation.Y;
            this.Player = u.Player;
            this.HealthPercentage = u.HealthPercentage;
            this.Angle = u.Angle;
            this.Kills = u.Kills;
        }

        public string Unitname { get; set; }

        public string Ident { get; set; }

        public int VPOffsetX { get; set; }

        public int VPOffsetY { get; set; }

        public int Player { get; set; }

        public int HealthPercentage { get; set; }

        public int Angle { get; set; }

        public int Kills { get; set; }

        public SchemaUnit ToNewSchemaUnit(Point viewportLocation, int mapWidthPx, int mapHeightPx)
        {
            var x = viewportLocation.X + this.VPOffsetX;
            var z = viewportLocation.Y + this.VPOffsetY;
            x = Math.Max(0, Math.Min(x, mapWidthPx - 1));
            z = Math.Max(0, Math.Min(z, mapHeightPx - 1));

            return new SchemaUnit(Guid.NewGuid(), this.Unitname ?? string.Empty)
            {
                Ident = this.Ident ?? string.Empty,
                XPos = x,
                ZPos = z,
                Player = this.Player,
                HealthPercentage = this.HealthPercentage,
                Angle = this.Angle,
                Kills = this.Kills,
                YPos = 0,
            };
        }
    }
}
