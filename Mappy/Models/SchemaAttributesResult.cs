namespace Mappy.Models
{
    using Mappy.Data;

    public class SchemaAttributesResult
    {
        public string SchemaType { get; set; }

        public string AiProfile { get; set; }

        public int SurfaceMetal { get; set; }

        public int MohoMetal { get; set; }

        public int HumanMetal { get; set; }

        public int ComputerMetal { get; set; }

        public int HumanEnergy { get; set; }

        public int ComputerEnergy { get; set; }

        public string MeteorWeapon { get; set; }

        public int MeteorRadius { get; set; }

        public int MeteorDuration { get; set; }

        public double MeteorDensity { get; set; }

        public int MeteorInterval { get; set; }

        public static SchemaAttributesResult FromSchema(MapSchema schema)
        {
            return new SchemaAttributesResult
                {
                    SchemaType = schema.SchemaType,
                    AiProfile = schema.AiProfile,
                    SurfaceMetal = schema.SurfaceMetal,
                    MohoMetal = schema.MohoMetal,
                    HumanMetal = schema.HumanMetal,
                    ComputerMetal = schema.ComputerMetal,
                    HumanEnergy = schema.HumanEnergy,
                    ComputerEnergy = schema.ComputerEnergy,
                    MeteorWeapon = schema.MeteorWeapon,
                    MeteorRadius = schema.MeteorRadius,
                    MeteorDensity = schema.MeteorDensity,
                    MeteorDuration = schema.MeteorDuration,
                    MeteorInterval = schema.MeteorInterval,
                };
        }

        public void ApplyTo(MapSchema schema)
        {
            schema.AiProfile = this.AiProfile;
            schema.SurfaceMetal = this.SurfaceMetal;
            schema.MohoMetal = this.MohoMetal;
            schema.HumanMetal = this.HumanMetal;
            schema.ComputerMetal = this.ComputerMetal;
            schema.HumanEnergy = this.HumanEnergy;
            schema.ComputerEnergy = this.ComputerEnergy;
            schema.MeteorWeapon = this.MeteorWeapon ?? string.Empty;
            schema.MeteorRadius = this.MeteorRadius;
            schema.MeteorDensity = this.MeteorDensity;
            schema.MeteorDuration = this.MeteorDuration;
            schema.MeteorInterval = this.MeteorInterval;
        }
    }
}
