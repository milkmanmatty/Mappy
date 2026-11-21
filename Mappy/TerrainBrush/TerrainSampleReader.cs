namespace Mappy.TerrainBrush
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    using Mappy.Data;

    using TAUtil.Hpi;
    using TAUtil.Sct;

    /// <summary>
    /// Reads the tile size, the four corner heights and the edge heights of a section from its archive.
    /// </summary>
    public sealed class TerrainSampleReader : IDisposable
    {
        private readonly Dictionary<string, HpiArchive> archives =
            new Dictionary<string, HpiArchive>(StringComparer.OrdinalIgnoreCase);

        public TerrainSample Read(SectionInfo section)
        {
            if (section == null)
            {
                throw new ArgumentNullException(nameof(section));
            }

            var archive = this.Open(section.HpiFileName);
            var file = archive.FindFile(section.SctFileName);
            if (file == null)
            {
                throw new FileNotFoundException("Section is not in the archive.", section.SctFileName);
            }

            var buffer = new byte[file.Size];
            archive.Extract(file, buffer);
            using (var reader = new SctReader(new MemoryStream(buffer)))
            {
                var width = reader.WidthInAttrs;
                var height = reader.HeightInAttrs;
                if (width <= 0 || height <= 0)
                {
                    throw new InvalidDataException("Section has no attribute cells.");
                }

                var count = width * height;
                var corners = new int[count];
                var index = 0;
                foreach (var attr in reader.EnumerateAttrs())
                {
                    if (index >= count)
                    {
                        break;
                    }

                    corners[index] = attr.Height;
                    index++;
                }

                if (index < count)
                {
                    throw new InvalidDataException("Section ended before its corner heights.");
                }

                return new TerrainSample
                {
                    TileWidth = reader.DataWidth,
                    TileHeight = reader.DataHeight,
                    TopLeft = corners[0],
                    TopRight = corners[width - 1],
                    BottomLeft = corners[(height - 1) * width],
                    BottomRight = corners[(height * width) - 1],
                    Edges = TerrainEdges.Read((x, y) => corners[(y * width) + x], width, height),
                };
            }
        }

        public void Dispose()
        {
            foreach (var archive in this.archives.Values)
            {
                archive.Dispose();
            }

            this.archives.Clear();
        }

        private HpiArchive Open(string path)
        {
            HpiArchive archive;
            if (!this.archives.TryGetValue(path, out archive))
            {
                archive = new HpiArchive(path);
                this.archives.Add(path, archive);
            }

            return archive;
        }
    }
}
