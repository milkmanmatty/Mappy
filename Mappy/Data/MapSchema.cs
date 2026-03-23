namespace Mappy.Data
{
    using System.Collections.Generic;
    using System.Drawing;

    using TAUtil.Tdf;

    public class MapSchema
    {
        private readonly Point?[] startPositions = new Point?[10];

        public MapSchema(int schemaNumber)
        {
            this.SchemaNumber = schemaNumber;
            this.SchemaType = schemaNumber == 0 ? "Network 1" : "Easy";
            this.AiProfile = "DEFAULT";
            this.SurfaceMetal = 3;
            this.MohoMetal = 30;
            this.HumanMetal = 1000;
            this.ComputerMetal = 1000;
            this.HumanEnergy = 1000;
            this.ComputerEnergy = 1000;
            this.Units = new List<SchemaUnit>();
        }

        public int SchemaNumber { get; set; }

        public string SchemaType { get; set; }

        public string AiProfile { get; set; }

        public int SurfaceMetal { get; set; }

        public int MohoMetal { get; set; }

        public int HumanMetal { get; set; }

        public int ComputerMetal { get; set; }

        public int HumanEnergy { get; set; }

        public int ComputerEnergy { get; set; }

        public string MeteorWeapon { get; set; } = string.Empty;

        public int MeteorRadius { get; set; }

        public double MeteorDensity { get; set; }

        public int MeteorDuration { get; set; }

        public int MeteorInterval { get; set; }

        public Dictionary<string, string> ExtraEntries { get; } = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, TdfNode> ExtraChildNodes { get; } = new Dictionary<string, TdfNode>(System.StringComparer.OrdinalIgnoreCase);

        public IList<SchemaUnit> Units { get; }

        public Point? GetStartPosition(int i) => this.startPositions[i];

        public void SetStartPosition(int i, Point? coordinates) => this.startPositions[i] = coordinates;
    }
}
