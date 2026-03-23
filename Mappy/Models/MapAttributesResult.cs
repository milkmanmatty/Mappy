namespace Mappy.Models
{
    public class MapAttributesResult
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public string Memory { get; set; }

        public string AiProfile { get; set; }

        public string SchemaType { get; set; }

        public string Planet { get; set; }

        public string Players { get; set; }

        public int MinWindSpeed { get; set; }

        public int MaxWindSpeed { get; set; }

        public int TidalStrength { get; set; }

        public int SolarStrength { get; set; }

        public int SeaLevel { get; set; }

        public int Gravity { get; set; }

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

        public int WaterDamage { get; set; }

        public bool ImpassibleWater { get; set; }

        public bool WaterDoesDamage { get; set; }

        public static MapAttributesResult FromModel(IMapModel map)
        {
            var attrs = map.Attributes;
            var si = map.ActiveSchemaIndex;
            if (si < 0 || si >= attrs.Schemas.Count)
            {
                si = 0;
            }

            var sch = attrs.Schemas[si];
            return new MapAttributesResult
                {
                    AiProfile = sch.AiProfile,
                    SchemaType = sch.SchemaType,
                    Description = attrs.Description,
                    Gravity = attrs.Gravity,
                    ImpassibleWater = attrs.LavaWorld,
                    MaxWindSpeed = attrs.MaxWindSpeed,
                    Memory = attrs.Memory,
                    MeteorDensity = sch.MeteorDensity,
                    MeteorDuration = sch.MeteorDuration,
                    MeteorInterval = sch.MeteorInterval,
                    MeteorRadius = sch.MeteorRadius,
                    MeteorWeapon = sch.MeteorWeapon,
                    MinWindSpeed = attrs.MinWindSpeed,
                    MohoMetal = sch.MohoMetal,
                    HumanMetal = sch.HumanMetal,
                    ComputerMetal = sch.ComputerMetal,
                    HumanEnergy = sch.HumanEnergy,
                    ComputerEnergy = sch.ComputerEnergy,
                    Name = attrs.Name,
                    Planet = attrs.Planet,
                    Players = attrs.NumPlayers,
                    SeaLevel = map.SeaLevel,
                    SolarStrength = attrs.SolarStrength,
                    SurfaceMetal = sch.SurfaceMetal,
                    TidalStrength = attrs.TidalStrength,
                    WaterDamage = attrs.WaterDamage,
                    WaterDoesDamage = attrs.WaterDoesDamage,
                };
        }

        public void MergeInto(IMapModel map)
        {
            var attrs = map.Attributes;
            var si = map.ActiveSchemaIndex;
            if (si < 0 || si >= attrs.Schemas.Count)
            {
                si = 0;
            }

            var sch = attrs.Schemas[si];

            attrs.Description = this.Description;
            attrs.Gravity = this.Gravity;
            attrs.LavaWorld = this.ImpassibleWater;
            attrs.MaxWindSpeed = this.MaxWindSpeed;
            attrs.Memory = this.Memory;
            attrs.MinWindSpeed = this.MinWindSpeed;
            attrs.Name = this.Name;
            attrs.Planet = this.Planet;
            attrs.NumPlayers = this.Players;
            map.SeaLevel = this.SeaLevel;
            attrs.SolarStrength = this.SolarStrength;
            attrs.TidalStrength = this.TidalStrength;
            attrs.WaterDamage = this.WaterDamage;
            attrs.WaterDoesDamage = this.WaterDoesDamage;

            sch.AiProfile = this.AiProfile;
            sch.SchemaType = this.SchemaType ?? sch.SchemaType;
            sch.SurfaceMetal = this.SurfaceMetal;
            sch.MohoMetal = this.MohoMetal;
            sch.HumanMetal = this.HumanMetal;
            sch.ComputerMetal = this.ComputerMetal;
            sch.HumanEnergy = this.HumanEnergy;
            sch.ComputerEnergy = this.ComputerEnergy;
            sch.MeteorWeapon = this.MeteorWeapon ?? string.Empty;
            sch.MeteorRadius = this.MeteorRadius;
            sch.MeteorDensity = this.MeteorDensity;
            sch.MeteorDuration = this.MeteorDuration;
            sch.MeteorInterval = this.MeteorInterval;
        }
    }
}