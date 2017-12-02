﻿namespace Mappy.Data
{
    using System;

    using Mappy.Util;

    using TAUtil.Tdf;

    public class FeatureRecord
    {
        public string Name { get; set; }

        public string World { get; set; }

        public string Category { get; set; }

        public int FootprintX { get; set; }

        public int FootprintY { get; set; }

        public string AnimFileName { get; set; }

        public string SequenceName { get; set; }

        public string ObjectName { get; set; }

        public static FeatureRecord FromTdfNode(TdfNode n)
        {
            // At least one Cavedog feature has a bad footprintz
            // (CCDATA.CCX/features/Water/CORALS.TDF, Coral20)
            // so we have to cope with them without complaining.
            if (!TdfConvert.TryToInt32(n.Entries.GetOrDefault("footprintx", "0"), out int footprintX))
            {
                footprintX = 1;
            }

            if (!TdfConvert.TryToInt32(n.Entries.GetOrDefault("footprintz", "0"), out int footprintZ))
            {
                footprintZ = 1;
            }

            return new FeatureRecord
                {
                    Name = n.Name,
                    World = n.Entries.GetOrDefault("world", string.Empty),
                    Category = n.Entries.GetOrDefault("category", string.Empty),
                    FootprintX = footprintX,
                    FootprintY = footprintZ,
                    AnimFileName = n.Entries.GetOrDefault("filename", string.Empty),
                    SequenceName = n.Entries.GetOrDefault("seqname", string.Empty),
                    ObjectName = n.Entries.GetOrDefault("object", string.Empty)
                };
        }
    }
}
