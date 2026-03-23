namespace Mappy.Data
{
    using System;

    public sealed class SchemaUnit
    {
        public SchemaUnit(Guid id, string unitName)
        {
            this.Id = id;
            this.Unitname = unitName ?? string.Empty;
        }

        public Guid Id { get; }

        public string Unitname { get; set; }

        public string Ident { get; set; } = string.Empty;

        public int XPos { get; set; }

        public int YPos { get; set; }

        public int ZPos { get; set; }

        public int Player { get; set; } = 1;

        public int HealthPercentage { get; set; } = 100;

        public int Angle { get; set; }

        public int Kills { get; set; }

        public SchemaUnit CloneWithNewId()
        {
            var u = new SchemaUnit(Guid.NewGuid(), this.Unitname)
            {
                Ident = this.Ident,
                XPos = this.XPos,
                YPos = this.YPos,
                ZPos = this.ZPos,
                Player = this.Player,
                HealthPercentage = this.HealthPercentage,
                Angle = this.Angle,
                Kills = this.Kills,
            };
            return u;
        }

        public SchemaUnit ClonePreservingId()
        {
            var u = new SchemaUnit(this.Id, this.Unitname)
            {
                Ident = this.Ident,
                XPos = this.XPos,
                YPos = this.YPos,
                ZPos = this.ZPos,
                Player = this.Player,
                HealthPercentage = this.HealthPercentage,
                Angle = this.Angle,
                Kills = this.Kills,
            };
            return u;
        }
    }
}
