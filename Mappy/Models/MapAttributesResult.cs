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

        // Victory (flag conditions write "1" when enabled)
        public bool KillEnemyCommander { get; set; }

        public bool DestroyAllUnits { get; set; }

        public string BuildUnitType { get; set; } = string.Empty;

        public string KillUnitType { get; set; } = string.Empty;

        public string MoveUnitToRadius { get; set; } = string.Empty;

        public string CaptureUnitType { get; set; } = string.Empty;

        public bool KillAllMobileUnits { get; set; }

        public string KillAllOfType { get; set; } = string.Empty;

        public string UnitTypePassesX { get; set; } = string.Empty;

        public string UnitTypePassesZ { get; set; } = string.Empty;

        public string VictoryTimerRunsOut { get; set; } = string.Empty;

        // Defeat
        public bool CommanderKilled { get; set; }

        public bool AllUnitsKilled { get; set; }

        public string UnitTypeKilled { get; set; } = string.Empty;

        public string DeathTimerRunsOut { get; set; } = string.Empty;

        public string AllUnitsKilledOfType { get; set; } = string.Empty;

        public string AnyUnitPassesX { get; set; } = string.Empty;

        public string AnyUnitPassesZ { get; set; } = string.Empty;

        public static MapAttributesResult FromModel(IMapModel map)
        {
            var attrs = map.Attributes;
            var si = map.ActiveSchemaIndex;
            if (si < 0 || si >= attrs.Schemas.Count)
            {
                si = 0;
            }

            var sch = attrs.Schemas[si];
            var conditions = attrs.VictoryConditions;
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
                    KillEnemyCommander = HasFlag(conditions, "KillEnemyCommander"),
                    DestroyAllUnits = HasFlag(conditions, "DestroyAllUnits"),
                    BuildUnitType = GetValue(conditions, "BuildUnitType"),
                    KillUnitType = GetValue(conditions, "KillUnitType"),
                    MoveUnitToRadius = GetValue(conditions, "MoveUnitToRadius"),
                    CaptureUnitType = GetValue(conditions, "CaptureUnitType"),
                    KillAllMobileUnits = HasFlag(conditions, "KillAllMobileUnits"),
                    KillAllOfType = GetValue(conditions, "KillAllOfType"),
                    UnitTypePassesX = GetValue(conditions, "UnitTypePassesX"),
                    UnitTypePassesZ = GetValue(conditions, "UnitTypePassesZ"),
                    VictoryTimerRunsOut = GetValue(conditions, "VictoryTimerRunsOut"),
                    CommanderKilled = HasFlag(conditions, "CommanderKilled"),
                    AllUnitsKilled = HasFlag(conditions, "AllUnitsKilled"),
                    UnitTypeKilled = GetValue(conditions, "UnitTypeKilled"),
                    DeathTimerRunsOut = GetValue(conditions, "DeathTimerRunsOut"),
                    AllUnitsKilledOfType = GetValue(conditions, "AllUnitsKilledOfType"),
                    AnyUnitPassesX = GetValue(conditions, "AnyUnitPassesX"),
                    AnyUnitPassesZ = GetValue(conditions, "AnyUnitPassesZ"),
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

            var conditions = attrs.VictoryConditions;
            MergeFlag(conditions, "KillEnemyCommander", this.KillEnemyCommander);
            MergeFlag(conditions, "DestroyAllUnits", this.DestroyAllUnits);
            MergeValue(conditions, "BuildUnitType", this.BuildUnitType);
            MergeValue(conditions, "KillUnitType", this.KillUnitType);
            MergeValue(conditions, "MoveUnitToRadius", this.MoveUnitToRadius);
            MergeValue(conditions, "CaptureUnitType", this.CaptureUnitType);
            MergeFlag(conditions, "KillAllMobileUnits", this.KillAllMobileUnits);
            MergeValue(conditions, "KillAllOfType", this.KillAllOfType);
            MergeValue(conditions, "UnitTypePassesX", this.UnitTypePassesX);
            MergeValue(conditions, "UnitTypePassesZ", this.UnitTypePassesZ);
            MergeValue(conditions, "VictoryTimerRunsOut", this.VictoryTimerRunsOut);
            MergeFlag(conditions, "CommanderKilled", this.CommanderKilled);
            MergeFlag(conditions, "AllUnitsKilled", this.AllUnitsKilled);
            MergeValue(conditions, "UnitTypeKilled", this.UnitTypeKilled);
            MergeValue(conditions, "DeathTimerRunsOut", this.DeathTimerRunsOut);
            MergeValue(conditions, "AllUnitsKilledOfType", this.AllUnitsKilledOfType);
            MergeValue(conditions, "AnyUnitPassesX", this.AnyUnitPassesX);
            MergeValue(conditions, "AnyUnitPassesZ", this.AnyUnitPassesZ);
        }

        private static bool HasFlag(IDictionary<string, string> conditions, string key)
        {
            return conditions.ContainsKey(key);
        }

        private static string GetValue(IDictionary<string, string> conditions, string key)
        {
            return conditions.TryGetValue(key, out var value) ? value : string.Empty;
        }

        private static void MergeFlag(IDictionary<string, string> conditions, string key, bool enabled)
        {
            if (enabled)
            {
                conditions[key] = "1";
            }
            else
            {
                conditions.Remove(key);
            }
        }

        private static void MergeValue(IDictionary<string, string> conditions, string key, string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                conditions[key] = value;
            }
            else
            {
                conditions.Remove(key);
            }
        }
    }
}
