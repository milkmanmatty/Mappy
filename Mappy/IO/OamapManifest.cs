namespace Mappy.IO
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Mappy.Models;

    public sealed class OamapManifest
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string Version { get; set; }

        public string Description { get; set; }

        public string AuthorName { get; set; }

        public string AuthorEmail { get; set; }

        public int Revision { get; set; }

        public string Packager { get; set; }

        public List<MapEntry> Maps { get; } = new List<MapEntry>();

        public OamapPackInfo ToPackInfo(string mapName, int archiveMapCount)
        {
            MapEntry entry = null;
            if (!string.IsNullOrWhiteSpace(mapName))
            {
                entry = this.Maps.FirstOrDefault(
                    map => string.Equals(map.Stem, mapName, StringComparison.OrdinalIgnoreCase));
            }

            if (entry == null && archiveMapCount <= 1 && this.Maps.Count == 1)
            {
                entry = this.Maps[0];
            }

            if (entry == null || string.IsNullOrWhiteSpace(entry.Stem))
            {
                return null;
            }

            var players = entry.Players;
            if (players < 2 || players > 10)
            {
                players = 2;
            }

            var revision = this.Revision;
            if (revision < 1 || revision > 65535)
            {
                revision = 1;
            }

            var info = new OamapPackInfo
            {
                PackId = this.Id ?? string.Empty,
                PackName = string.IsNullOrWhiteSpace(this.Name) ? entry.Stem : this.Name,
                Version = string.IsNullOrWhiteSpace(this.Version) ? "1.0" : this.Version,
                Description = this.Description ?? string.Empty,
                AuthorName = string.IsNullOrWhiteSpace(this.AuthorName) ? "unknown" : this.AuthorName,
                AuthorEmail = this.AuthorEmail ?? string.Empty,
                PackagingRevision = revision,
                Packager = string.IsNullOrWhiteSpace(this.Packager) ? "Mappy" : this.Packager,
                Stem = entry.Stem,
                MapName = string.IsNullOrWhiteSpace(entry.Name) ? entry.Stem : entry.Name,
                MapDescription = entry.Description ?? string.Empty,
                Players = players,
            };

            return string.IsNullOrEmpty(info.Validate()) ? info : null;
        }

        public sealed class MapEntry
        {
            public string Stem { get; set; }

            public string Name { get; set; }

            public string Description { get; set; }

            public int Players { get; set; }

            public List<string> Files { get; } = new List<string>();
        }
    }
}
