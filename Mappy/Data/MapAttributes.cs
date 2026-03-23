namespace Mappy.Data
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Mappy.Util;
    using TAUtil.Tdf;

    /// <summary>
    /// Contains all the metadata about a map.
    /// </summary>
    public class MapAttributes : Notifier
    {
        private static readonly HashSet<string> GlobalHeaderKnownKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "missionname", "missiondescription", "planet", "missionhint", "brief", "narration", "glamour",
            "lineofsight", "mapping", "tidalstrength", "solarstrength", "lavaworld", "killmul", "timemul",
            "minwindspeed", "maxwindspeed", "gravity", "waterdoesdamage", "waterdamage", "numplayers",
            "size", "memory", "useonlyunits", "SCHEMACOUNT",
        };

        private static readonly HashSet<string> SchemaKnownKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Type", "aiprofile", "SurfaceMetal", "MohoMetal", "HumanMetal", "ComputerMetal", "HumanEnergy", "ComputerEnergy",
            "MeteorWeapon", "MeteorRadius", "MeteorDensity", "MeteorDuration", "MeteorInterval",
        };

        private string name;
        private string description;
        private string planet;
        private int gravity;
        private string numPlayers;
        private string memory;

        private int tidalStrength;
        private int solarStrength;

        private int minWindSpeed;
        private int maxWindSpeed;

        private bool lavaWorld;

        private bool waterDoesDamage;
        private int waterDamage;

        public MapAttributes()
        {
            this.Name = "Untitled Map";
            this.Description = "A map made with Mappy";
            this.Gravity = 112;
            this.Memory = string.Empty;
            this.NumPlayers = "2";

            this.TidalStrength = 20;
            this.SolarStrength = 20;

            this.MinWindSpeed = 0;
            this.MaxWindSpeed = 3000;

            this.LavaWorld = false;

            this.WaterDoesDamage = false;
            this.WaterDamage = 0;

            this.schemaList.Add(new MapSchema(0));
        }

        private readonly List<MapSchema> schemaList = new List<MapSchema>();

        public event EventHandler<StartPositionChangedEventArgs> StartPositionChanged;

        public event EventHandler<SchemaUnitsChangedEventArgs> SchemaUnitsChanged;


        public IReadOnlyList<MapSchema> Schemas => this.schemaList;

        public Dictionary<string, string> GlobalHeaderExtraEntries { get; } =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public string Name
        {
            get => this.name;
            set => this.SetField(ref this.name, value, nameof(this.Name));
        }

        public string Description
        {
            get => this.description;
            set => this.SetField(ref this.description, value, nameof(this.Description));
        }

        public string Planet
        {
            get => this.planet;
            set => this.SetField(ref this.planet, value, nameof(this.Planet));
        }

        public int Gravity
        {
            get => this.gravity;
            set => this.SetField(ref this.gravity, value, nameof(this.Gravity));
        }

        public string Memory
        {
            get => this.memory;
            set => this.SetField(ref this.memory, value, nameof(this.Memory));
        }

        public string NumPlayers
        {
            get => this.numPlayers;
            set => this.SetField(ref this.numPlayers, value, nameof(this.NumPlayers));
        }

        public int TidalStrength
        {
            get => this.tidalStrength;
            set => this.SetField(ref this.tidalStrength, value, nameof(this.TidalStrength));
        }

        public int SolarStrength
        {
            get => this.solarStrength;
            set => this.SetField(ref this.solarStrength, value, nameof(this.SolarStrength));
        }

        public int MinWindSpeed
        {
            get => this.minWindSpeed;
            set => this.SetField(ref this.minWindSpeed, value, nameof(this.MinWindSpeed));
        }

        public bool LavaWorld
        {
            get => this.lavaWorld;
            set => this.SetField(ref this.lavaWorld, value, nameof(this.LavaWorld));
        }

        public bool WaterDoesDamage
        {
            get => this.waterDoesDamage;
            set => this.SetField(ref this.waterDoesDamage, value, nameof(this.WaterDoesDamage));
        }

        public int MaxWindSpeed
        {
            get => this.maxWindSpeed;
            set => this.SetField(ref this.maxWindSpeed, value, nameof(this.MaxWindSpeed));
        }

        public int WaterDamage
        {
            get => this.waterDamage;
            set => this.SetField(ref this.waterDamage, value, nameof(this.WaterDamage));
        }

        public Point? GetStartPosition(int schemaIndex, int startIndex)
        {
            return this.schemaList[schemaIndex].GetStartPosition(startIndex);
        }

        public void SetStartPosition(int schemaIndex, int startIndex, Point? coordinates)
        {
            var sch = this.schemaList[schemaIndex];
            if (sch.GetStartPosition(startIndex) != coordinates)
            {
                sch.SetStartPosition(startIndex, coordinates);
                this.OnStartPositionChanged(new StartPositionChangedEventArgs(startIndex, schemaIndex));
            }
        }

        public SchemaUnit GetUnit(int schemaIndex, Guid id)
        {
            return this.schemaList[schemaIndex].Units.First(u => u.Id == id);
        }

        public void AddUnit(int schemaIndex, SchemaUnit unit)
        {
            this.schemaList[schemaIndex].Units.Add(unit);
            this.OnSchemaUnitsChanged(new SchemaUnitsChangedEventArgs(schemaIndex, SchemaUnitsChangedEventArgs.ActionKind.Add, unit.Id));
        }

        public void RemoveUnit(int schemaIndex, Guid id)
        {
            var list = this.schemaList[schemaIndex].Units;
            for (var i = 0; i < list.Count; i++)
            {
                if (list[i].Id == id)
                {
                    list.RemoveAt(i);
                    this.OnSchemaUnitsChanged(new SchemaUnitsChangedEventArgs(schemaIndex, SchemaUnitsChangedEventArgs.ActionKind.Remove, id));
                    return;
                }
            }
        }

        public void ReplaceUnit(int schemaIndex, SchemaUnit unit)
        {
            var list = this.schemaList[schemaIndex].Units;
            for (var i = 0; i < list.Count; i++)
            {
                if (list[i].Id == unit.Id)
                {
                    list[i] = unit;
                    this.OnSchemaUnitsChanged(new SchemaUnitsChangedEventArgs(schemaIndex, SchemaUnitsChangedEventArgs.ActionKind.Move, unit.Id));
                    return;
                }
            }
        }

        public static MapAttributes Load(TdfNode n)
        {
            var gh = n.Keys["GlobalHeader"];
            var m = new MapAttributes();

            m.Name = gh.Entries.GetOrDefault("missionname", string.Empty);
            m.Description = gh.Entries.GetOrDefault("missiondescription", string.Empty);
            m.Planet = gh.Entries.GetOrDefault("planet", string.Empty);
            m.TidalStrength = TdfConvert.ToInt32(gh.Entries.GetOrDefault("tidalstrength", "0"));
            m.SolarStrength = TdfConvert.ToInt32(gh.Entries.GetOrDefault("solarstrength", "0"));
            m.LavaWorld = TdfConvert.ToBool(gh.Entries.GetOrDefault("lavaworld", "0"));
            m.MinWindSpeed = TdfConvert.ToInt32(gh.Entries.GetOrDefault("minwindspeed", "0"));
            m.MaxWindSpeed = TdfConvert.ToInt32(gh.Entries.GetOrDefault("maxwindspeed", "0"));
            m.Gravity = TdfConvert.ToInt32(gh.Entries.GetOrDefault("gravity", "0"));
            m.WaterDoesDamage = TdfConvert.ToBool(gh.Entries.GetOrDefault("waterdoesdamage", "0"));
            m.WaterDamage = TdfConvert.ToInt32(gh.Entries.GetOrDefault("waterdamage", "0"));
            m.NumPlayers = gh.Entries.GetOrDefault("numplayers", string.Empty);
            m.Memory = gh.Entries.GetOrDefault("memory", string.Empty);

            foreach (var e in gh.Entries)
            {
                if (!GlobalHeaderKnownKeys.Contains(e.Key))
                {
                    m.GlobalHeaderExtraEntries[e.Key] = e.Value;
                }
            }

            m.schemaList.Clear();

            var schemaKeyRegex = new Regex(@"^Schema\s+(\d+)$", RegexOptions.IgnoreCase);
            var schemaEntries = gh.Keys
                .Select(kv => new { kv.Key, Match = schemaKeyRegex.Match(kv.Key) })
                .Where(x => x.Match.Success)
                .Select(x => (Number: int.Parse(x.Match.Groups[1].Value, CultureInfo.InvariantCulture), Key: x.Key, Node: gh.Keys[x.Key]))
                .OrderBy(x => x.Number)
                .ToList();

            if (schemaEntries.Count == 0)
            {
                m.schemaList.Add(ParseSchemaNode(new TdfNode("Schema 0"), 0));
            }
            else
            {
                foreach (var se in schemaEntries)
                {
                    m.schemaList.Add(ParseSchemaNode(se.Node, se.Number));
                }
            }

            return m;
        }

        public void WriteOta(Stream st, int mapWidthIn512Tiles, int mapHeightIn512Tiles)
        {
            var r = new TdfNode("GlobalHeader");

            r.Entries["missionname"] = this.Name;
            r.Entries["missiondescription"] = this.Description;
            r.Entries["planet"] = this.Planet;
            r.Entries["missionhint"] = string.Empty;
            r.Entries["brief"] = string.Empty;
            r.Entries["narration"] = string.Empty;
            r.Entries["glamour"] = string.Empty;
            r.Entries["lineofsight"] = "0";
            r.Entries["mapping"] = "0";
            r.Entries["tidalstrength"] = TdfConvert.ToString(this.TidalStrength);
            r.Entries["solarstrength"] = TdfConvert.ToString(this.SolarStrength);
            r.Entries["lavaworld"] = TdfConvert.ToString(this.LavaWorld);
            r.Entries["killmul"] = "50";
            r.Entries["timemul"] = "0";
            r.Entries["minwindspeed"] = TdfConvert.ToString(this.MinWindSpeed);
            r.Entries["maxwindspeed"] = TdfConvert.ToString(this.MaxWindSpeed);
            r.Entries["gravity"] = TdfConvert.ToString(this.Gravity);
            r.Entries["waterdoesdamage"] = TdfConvert.ToString(this.WaterDoesDamage);
            r.Entries["waterdamage"] = TdfConvert.ToString(this.WaterDamage);
            r.Entries["numplayers"] = this.numPlayers;
            r.Entries["size"] = $"{mapWidthIn512Tiles} x {mapHeightIn512Tiles}";
            r.Entries["memory"] = this.memory;
            r.Entries["useonlyunits"] = string.Empty;
            r.Entries["SCHEMACOUNT"] = TdfConvert.ToString(this.schemaList.Count);

            foreach (var e in this.GlobalHeaderExtraEntries)
            {
                r.Entries[e.Key] = e.Value;
            }

            for (var i = 0; i < this.schemaList.Count; i++)
            {
                var sch = this.schemaList[i];
                sch.SchemaNumber = i;
                var nodeName = "Schema " + i;
                var s = BuildSchemaTdfNode(sch, nodeName);
                r.Keys[nodeName] = s;
            }

            r.WriteTdf(st);
        }

        public MapSchema AddSchema()
        {
            var idx = this.schemaList.Count;
            var sch = new MapSchema(idx)
            {
                SchemaType = "Easy",
            };
            this.schemaList.Add(sch);
            return sch;
        }

        public bool RemoveSchemaAt(int index)
        {
            if (this.schemaList.Count <= 1 || index < 0 || index >= this.schemaList.Count)
            {
                return false;
            }

            this.schemaList.RemoveAt(index);
            for (var i = 0; i < this.schemaList.Count; i++)
            {
                this.schemaList[i].SchemaNumber = i;
            }

            return true;
        }

        public void CopyFrom(MapAttributes source)
        {
            this.Name = source.Name;
            this.Description = source.Description;
            this.Planet = source.Planet;
            this.Gravity = source.Gravity;
            this.Memory = source.Memory;
            this.NumPlayers = source.NumPlayers;
            this.TidalStrength = source.TidalStrength;
            this.SolarStrength = source.SolarStrength;
            this.MinWindSpeed = source.MinWindSpeed;
            this.MaxWindSpeed = source.MaxWindSpeed;
            this.LavaWorld = source.LavaWorld;
            this.WaterDoesDamage = source.WaterDoesDamage;
            this.WaterDamage = source.WaterDamage;

            this.GlobalHeaderExtraEntries.Clear();
            foreach (var e in source.GlobalHeaderExtraEntries)
            {
                this.GlobalHeaderExtraEntries[e.Key] = e.Value;
            }

            this.schemaList.Clear();
            foreach (var sch in source.schemaList)
            {
                this.schemaList.Add(CloneSchema(sch));
            }
        }

        private static MapSchema CloneSchema(MapSchema sch)
        {
            var n = new MapSchema(sch.SchemaNumber)
            {
                SchemaType = sch.SchemaType,
                AiProfile = sch.AiProfile,
                SurfaceMetal = sch.SurfaceMetal,
                MohoMetal = sch.MohoMetal,
                HumanMetal = sch.HumanMetal,
                ComputerMetal = sch.ComputerMetal,
                HumanEnergy = sch.HumanEnergy,
                ComputerEnergy = sch.ComputerEnergy,
                MeteorWeapon = sch.MeteorWeapon,
                MeteorRadius = sch.MeteorRadius,
                MeteorDensity = sch.MeteorDensity,
                MeteorDuration = sch.MeteorDuration,
                MeteorInterval = sch.MeteorInterval,
            };
            foreach (var e in sch.ExtraEntries)
            {
                n.ExtraEntries[e.Key] = e.Value;
            }

            foreach (var kv in sch.ExtraChildNodes)
            {
                n.ExtraChildNodes[kv.Key] = kv.Value;
            }

            for (var i = 0; i < 10; i++)
            {
                n.SetStartPosition(i, sch.GetStartPosition(i));
            }

            foreach (var u in sch.Units)
            {
                n.Units.Add(u.ClonePreservingId());
            }

            return n;
        }

        private static MapSchema ParseSchemaNode(TdfNode schemaNode, int schemaNumber)
        {
            var sch = new MapSchema(schemaNumber);

            sch.SchemaType = schemaNode.Entries.GetOrDefault("Type", sch.SchemaType);
            sch.AiProfile = schemaNode.Entries.GetOrDefault("aiprofile", sch.AiProfile);
            sch.SurfaceMetal = TdfConvert.ToInt32(schemaNode.Entries.GetOrDefault("SurfaceMetal", TdfConvert.ToString(sch.SurfaceMetal)));
            sch.MohoMetal = TdfConvert.ToInt32(schemaNode.Entries.GetOrDefault("MohoMetal", TdfConvert.ToString(sch.MohoMetal)));
            sch.HumanMetal = TdfConvert.ToInt32(schemaNode.Entries.GetOrDefault("HumanMetal", TdfConvert.ToString(sch.HumanMetal)));
            sch.ComputerMetal = TdfConvert.ToInt32(schemaNode.Entries.GetOrDefault("ComputerMetal", TdfConvert.ToString(sch.ComputerMetal)));
            sch.HumanEnergy = TdfConvert.ToInt32(schemaNode.Entries.GetOrDefault("HumanEnergy", TdfConvert.ToString(sch.HumanEnergy)));
            sch.ComputerEnergy = TdfConvert.ToInt32(schemaNode.Entries.GetOrDefault("ComputerEnergy", TdfConvert.ToString(sch.ComputerEnergy)));
            sch.MeteorWeapon = schemaNode.Entries.GetOrDefault("MeteorWeapon", string.Empty);
            sch.MeteorRadius = TdfConvert.ToInt32(schemaNode.Entries.GetOrDefault("MeteorRadius", "0"));
            sch.MeteorDensity = TdfConvert.ToDouble(schemaNode.Entries.GetOrDefault("MeteorDensity", "0"));
            sch.MeteorDuration = TdfConvert.ToInt32(schemaNode.Entries.GetOrDefault("MeteorDuration", "0"));
            sch.MeteorInterval = TdfConvert.ToInt32(schemaNode.Entries.GetOrDefault("MeteorInterval", "0"));

            foreach (var e in schemaNode.Entries)
            {
                if (!SchemaKnownKeys.Contains(e.Key))
                {
                    sch.ExtraEntries[e.Key] = e.Value;
                }
            }

            if (schemaNode.Keys.ContainsKey("specials"))
            {
                var specials = schemaNode.Keys["specials"];
                foreach (var special in specials.Keys.Values)
                {
                    var type = special.Entries.GetOrDefault("specialwhat", string.Empty);
                    if (type.Length < "StartPosX".Length || !type.StartsWith("StartPos", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var id = TdfConvert.ToInt32(type.Substring(8));
                    var x = TdfConvert.ToInt32(special.Entries.GetOrDefault("XPos", "0"));
                    var y = TdfConvert.ToInt32(special.Entries.GetOrDefault("ZPos", "0"));
                    sch.SetStartPosition(id - 1, new Point(x, y));
                }
            }

            if (schemaNode.Keys.ContainsKey("units"))
            {
                var unitsRoot = schemaNode.Keys["units"];
                var ordered = unitsRoot.Keys.OrderBy(kv => ParseUnitKeyIndex(kv.Key)).ToList();
                foreach (var kv in ordered)
                {
                    var un = kv.Value;
                    var unitName = un.Entries.GetOrDefault("Unitname", string.Empty);
                    var u = new SchemaUnit(Guid.NewGuid(), unitName)
                    {
                        Ident = un.Entries.GetOrDefault("Ident", string.Empty),
                        XPos = TdfConvert.ToInt32(un.Entries.GetOrDefault("XPos", "0")),
                        YPos = TdfConvert.ToInt32(un.Entries.GetOrDefault("YPos", "0")),
                        ZPos = TdfConvert.ToInt32(un.Entries.GetOrDefault("ZPos", "0")),
                        Player = TdfConvert.ToInt32(un.Entries.GetOrDefault("Player", "1")),
                        HealthPercentage = TdfConvert.ToInt32(un.Entries.GetOrDefault("HealthPercentage", "100")),
                        Angle = TdfConvert.ToInt32(un.Entries.GetOrDefault("Angle", "0")),
                        Kills = TdfConvert.ToInt32(un.Entries.GetOrDefault("Kills", "0")),
                    };
                    sch.Units.Add(u);
                }
            }

            foreach (var kv in schemaNode.Keys)
            {
                if (string.Equals(kv.Key, "specials", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(kv.Key, "units", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                sch.ExtraChildNodes[kv.Key] = kv.Value;
            }

            return sch;
        }

        private static int ParseUnitKeyIndex(string key)
        {
            if (key.Length > 4 && key.StartsWith("unit", StringComparison.OrdinalIgnoreCase)
                && int.TryParse(key.Substring(4), NumberStyles.Integer, CultureInfo.InvariantCulture, out var n))
            {
                return n;
            }

            return int.MaxValue;
        }

        private static TdfNode BuildSchemaTdfNode(MapSchema sch, string nodeName)
        {
            var s = new TdfNode(nodeName);

            s.Entries["Type"] = sch.SchemaType;
            s.Entries["aiprofile"] = sch.AiProfile;
            s.Entries["SurfaceMetal"] = TdfConvert.ToString(sch.SurfaceMetal);
            s.Entries["MohoMetal"] = TdfConvert.ToString(sch.MohoMetal);
            s.Entries["HumanMetal"] = TdfConvert.ToString(sch.HumanMetal);
            s.Entries["ComputerMetal"] = TdfConvert.ToString(sch.ComputerMetal);
            s.Entries["HumanEnergy"] = TdfConvert.ToString(sch.HumanEnergy);
            s.Entries["ComputerEnergy"] = TdfConvert.ToString(sch.ComputerEnergy);
            s.Entries["MeteorWeapon"] = sch.MeteorWeapon;
            s.Entries["MeteorRadius"] = TdfConvert.ToString(sch.MeteorRadius);
            s.Entries["MeteorDensity"] = TdfConvert.ToString(sch.MeteorDensity);
            s.Entries["MeteorDuration"] = TdfConvert.ToString(sch.MeteorDuration);
            s.Entries["MeteorInterval"] = TdfConvert.ToString(sch.MeteorInterval);

            foreach (var e in sch.ExtraEntries)
            {
                if (!SchemaKnownKeys.Contains(e.Key))
                {
                    s.Entries[e.Key] = e.Value;
                }
            }

            var specials = new TdfNode("specials");
            var count = 0;
            for (var i = 0; i < 10; i++)
            {
                var p = sch.GetStartPosition(i);
                if (!p.HasValue)
                {
                    continue;
                }

                var spec = new TdfNode("special" + count);
                spec.Entries["specialwhat"] = "StartPos" + (i + 1);
                spec.Entries["XPos"] = TdfConvert.ToString(p.Value.X);
                spec.Entries["ZPos"] = TdfConvert.ToString(p.Value.Y);
                specials.Keys[spec.Name] = spec;
                count++;
            }

            s.Keys["specials"] = specials;

            if (sch.Units.Count > 0)
            {
                var unitsRoot = new TdfNode("units");
                for (var i = 0; i < sch.Units.Count; i++)
                {
                    var u = sch.Units[i];
                    var un = new TdfNode("unit" + i);
                    un.Entries["Unitname"] = u.Unitname;
                    un.Entries["Ident"] = u.Ident;
                    un.Entries["XPos"] = TdfConvert.ToString(u.XPos);
                    un.Entries["YPos"] = TdfConvert.ToString(u.YPos);
                    un.Entries["ZPos"] = TdfConvert.ToString(u.ZPos);
                    un.Entries["Player"] = TdfConvert.ToString(u.Player);
                    un.Entries["HealthPercentage"] = TdfConvert.ToString(u.HealthPercentage);
                    un.Entries["Angle"] = TdfConvert.ToString(u.Angle);
                    un.Entries["Kills"] = TdfConvert.ToString(u.Kills);
                    unitsRoot.Keys[un.Name] = un;
                }

                s.Keys["units"] = unitsRoot;
            }

            foreach (var kv in sch.ExtraChildNodes)
            {
                if (!s.Keys.ContainsKey(kv.Key))
                {
                    s.Keys[kv.Key] = kv.Value;
                }
            }

            return s;
        }

        protected virtual void OnStartPositionChanged(StartPositionChangedEventArgs e)
        {
            this.StartPositionChanged?.Invoke(this, e);
        }

        protected virtual void OnSchemaUnitsChanged(SchemaUnitsChangedEventArgs e)
        {
            this.SchemaUnitsChanged?.Invoke(this, e);
        }
    }
}
