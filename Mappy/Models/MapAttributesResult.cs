namespace Mappy.Models
{
    using System.Collections.Generic;

    using Mappy.Data;

    public class MapAttributesResult
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public string Memory { get; set; }

        public string MissionHint { get; set; }

        public string Brief { get; set; }

        public string Narration { get; set; }

        public string Glamour { get; set; }

        public string GlamourSound { get; set; }

        public string UseOnlyUnits { get; set; }

        public bool NoMovie { get; set; }

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

        public OptionalStringAttribute AllUnitsKilled { get; set; } = new OptionalStringAttribute();

        public OptionalStringAttribute AllUnitsKilledOfType { get; set; } = new OptionalStringAttribute();

        public OptionalStringAttribute AnyUnitPassesX { get; set; } = new OptionalStringAttribute();

        public OptionalStringAttribute AnyUnitPassesZ { get; set; } = new OptionalStringAttribute();

        public OptionalStringAttribute BuildUnitType { get; set; } = new OptionalStringAttribute();

        public OptionalStringAttribute CaptureUnitType { get; set; } = new OptionalStringAttribute();

        public OptionalStringAttribute CommanderKilled { get; set; } = new OptionalStringAttribute();

        public OptionalStringAttribute DeathTimerRunsOut { get; set; } = new OptionalStringAttribute();

        public OptionalStringAttribute DestroyAllUnits { get; set; } = new OptionalStringAttribute();

        public OptionalStringAttribute KillAllMobileUnits { get; set; } = new OptionalStringAttribute();

        public OptionalStringAttribute KillAllOfType { get; set; } = new OptionalStringAttribute();

        public OptionalStringAttribute KillEnemyCommander { get; set; } = new OptionalStringAttribute();

        public OptionalStringAttribute KillUnitType { get; set; } = new OptionalStringAttribute();

        public OptionalStringAttribute MoveUnitToRadius { get; set; } = new OptionalStringAttribute();

        public OptionalStringAttribute UnitTypeKilled { get; set; } = new OptionalStringAttribute();

        public OptionalStringAttribute UnitTypePassesX { get; set; } = new OptionalStringAttribute();

        public OptionalStringAttribute UnitTypePassesZ { get; set; } = new OptionalStringAttribute();

        public OptionalStringAttribute VictoryTimerRunsOut { get; set; } = new OptionalStringAttribute();

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
                    MissionHint = attrs.MissionHint,
                    Brief = attrs.Brief,
                    Narration = attrs.Narration,
                    Glamour = attrs.Glamour,
                    GlamourSound = attrs.GlamourSound,
                    UseOnlyUnits = attrs.UseOnlyUnits,
                    NoMovie = attrs.NoMovie,
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
                    AllUnitsKilled = FromVictory(attrs.VictoryConditions, "AllUnitsKilled"),
                    AllUnitsKilledOfType = FromVictory(attrs.VictoryConditions, "AllUnitsKilledOfType"),
                    AnyUnitPassesX = FromVictory(attrs.VictoryConditions, "AnyUnitPassesX"),
                    AnyUnitPassesZ = FromVictory(attrs.VictoryConditions, "AnyUnitPassesZ"),
                    BuildUnitType = FromVictory(attrs.VictoryConditions, "BuildUnitType"),
                    CaptureUnitType = FromVictory(attrs.VictoryConditions, "CaptureUnitType"),
                    CommanderKilled = FromVictory(attrs.VictoryConditions, "CommanderKilled"),
                    DeathTimerRunsOut = FromVictory(attrs.VictoryConditions, "DeathTimerRunsOut"),
                    DestroyAllUnits = FromVictory(attrs.VictoryConditions, "DestroyAllUnits"),
                    KillAllMobileUnits = FromVictory(attrs.VictoryConditions, "KillAllMobileUnits"),
                    KillAllOfType = FromVictory(attrs.VictoryConditions, "KillAllOfType"),
                    KillEnemyCommander = FromVictory(attrs.VictoryConditions, "KillEnemyCommander"),
                    KillUnitType = FromVictory(attrs.VictoryConditions, "KillUnitType"),
                    MoveUnitToRadius = FromVictory(attrs.VictoryConditions, "MoveUnitToRadius"),
                    UnitTypeKilled = FromVictory(attrs.VictoryConditions, "UnitTypeKilled"),
                    UnitTypePassesX = FromVictory(attrs.VictoryConditions, "UnitTypePassesX"),
                    UnitTypePassesZ = FromVictory(attrs.VictoryConditions, "UnitTypePassesZ"),
                    VictoryTimerRunsOut = FromVictory(attrs.VictoryConditions, "VictoryTimerRunsOut"),
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
            attrs.MissionHint = this.MissionHint ?? string.Empty;
            attrs.Brief = this.Brief ?? string.Empty;
            attrs.Narration = this.Narration ?? string.Empty;
            attrs.Glamour = this.Glamour ?? string.Empty;
            attrs.GlamourSound = this.GlamourSound ?? string.Empty;
            attrs.UseOnlyUnits = this.UseOnlyUnits ?? string.Empty;
            attrs.NoMovie = this.NoMovie;
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

            MergeVictory(attrs.VictoryConditions, "AllUnitsKilled", this.AllUnitsKilled);
            MergeVictory(attrs.VictoryConditions, "AllUnitsKilledOfType", this.AllUnitsKilledOfType);
            MergeVictory(attrs.VictoryConditions, "AnyUnitPassesX", this.AnyUnitPassesX);
            MergeVictory(attrs.VictoryConditions, "AnyUnitPassesZ", this.AnyUnitPassesZ);
            MergeVictory(attrs.VictoryConditions, "BuildUnitType", this.BuildUnitType);
            MergeVictory(attrs.VictoryConditions, "CaptureUnitType", this.CaptureUnitType);
            MergeVictory(attrs.VictoryConditions, "CommanderKilled", this.CommanderKilled);
            MergeVictory(attrs.VictoryConditions, "DeathTimerRunsOut", this.DeathTimerRunsOut);
            MergeVictory(attrs.VictoryConditions, "DestroyAllUnits", this.DestroyAllUnits);
            MergeVictory(attrs.VictoryConditions, "KillAllMobileUnits", this.KillAllMobileUnits);
            MergeVictory(attrs.VictoryConditions, "KillAllOfType", this.KillAllOfType);
            MergeVictory(attrs.VictoryConditions, "KillEnemyCommander", this.KillEnemyCommander);
            MergeVictory(attrs.VictoryConditions, "KillUnitType", this.KillUnitType);
            MergeVictory(attrs.VictoryConditions, "MoveUnitToRadius", this.MoveUnitToRadius);
            MergeVictory(attrs.VictoryConditions, "UnitTypeKilled", this.UnitTypeKilled);
            MergeVictory(attrs.VictoryConditions, "UnitTypePassesX", this.UnitTypePassesX);
            MergeVictory(attrs.VictoryConditions, "UnitTypePassesZ", this.UnitTypePassesZ);
            MergeVictory(attrs.VictoryConditions, "VictoryTimerRunsOut", this.VictoryTimerRunsOut);
        }

        private static OptionalStringAttribute FromVictory(IDictionary<string, string> conditions, string key)
        {
            if (conditions.TryGetValue(key, out var value))
            {
                return new OptionalStringAttribute { Enabled = true, Value = value };
            }

            return new OptionalStringAttribute { Enabled = false, Value = string.Empty };
        }

        private static void MergeVictory(IDictionary<string, string> conditions, string key, OptionalStringAttribute entry)
        {
            if (entry != null && entry.Enabled)
            {
                conditions[key] = entry.Value ?? string.Empty;
            }
            else
            {
                conditions.Remove(key);
            }
        }
    }
}
