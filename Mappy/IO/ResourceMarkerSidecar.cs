namespace Mappy.IO
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.IO;
    using System.Runtime.Serialization;
    using System.Runtime.Serialization.Json;

    using Mappy.Models;

    using TAUtil.Hpi;

    public static class ResourceMarkerSidecar
    {
        public static string GetSidecarPath(string mapFilePath)
        {
            if (string.IsNullOrWhiteSpace(mapFilePath))
            {
                return null;
            }

            var directory = Path.GetDirectoryName(mapFilePath);
            var baseName = Path.GetFileNameWithoutExtension(mapFilePath);
            if (string.IsNullOrEmpty(directory) || string.IsNullOrEmpty(baseName))
            {
                return null;
            }

            return Path.Combine(directory, baseName + ".markers.json");
        }

        public static void TryLoad(UndoableMapModel map)
        {
            if (map == null || !Mappy.MappySettings.Settings.SaveResourceMarkersWithMap)
            {
                return;
            }

            var path = GetSidecarPath(map.FilePath);
            if (path == null || !File.Exists(path))
            {
                return;
            }

            try
            {
                using (var stream = File.OpenRead(path))
                {
                    Apply(map, stream);
                }
            }
            catch (IOException)
            {
            }
            catch (SerializationException)
            {
            }
        }

        public static void TryLoadFromArchive(UndoableMapModel map, HpiArchive archive, string tntPath)
        {
            if (map == null || archive == null || string.IsNullOrWhiteSpace(tntPath)
                || !Mappy.MappySettings.Settings.SaveResourceMarkersWithMap)
            {
                return;
            }

            var markersPath = HpiPath.ChangeExtension(tntPath, ".markers.json");
            if (string.IsNullOrEmpty(markersPath))
            {
                return;
            }

            try
            {
                var info = archive.FindFile(markersPath);
                if (info == null || info.Size <= 0)
                {
                    return;
                }

                var buffer = new byte[info.Size];
                archive.Extract(info, buffer);
                using (var stream = new MemoryStream(buffer, false))
                {
                    Apply(map, stream);
                }
            }
            catch (IOException)
            {
            }
            catch (SerializationException)
            {
            }
        }

        public static string TryCreateTempFile(UndoableMapModel map)
        {
            if (map == null || !Mappy.MappySettings.Settings.SaveResourceMarkersWithMap
                || map.MarkerLines == null || map.MarkerLines.Count == 0)
            {
                return null;
            }

            var tempPath = Path.GetTempFileName();
            using (var stream = File.Create(tempPath))
            {
                Write(map, stream);
            }

            return tempPath;
        }

        public static string TryExtractArchiveEntry(string archivePath)
        {
            if (string.IsNullOrWhiteSpace(archivePath) || !File.Exists(archivePath))
            {
                return null;
            }

            var name = Path.GetFileNameWithoutExtension(archivePath);
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            try
            {
                using (var archive = new HpiArchive(archivePath))
                {
                    var info = archive.FindFile("maps\\" + name + ".markers.json");
                    if (info == null || info.Size <= 0)
                    {
                        return null;
                    }

                    var buffer = new byte[info.Size];
                    archive.Extract(info, buffer);
                    var tempPath = Path.GetTempFileName();
                    File.WriteAllBytes(tempPath, buffer);
                    return tempPath;
                }
            }
            catch (IOException)
            {
                return null;
            }
        }

        public static void TrySave(UndoableMapModel map, string mapFilePath)
        {
            if (map == null || !Mappy.MappySettings.Settings.SaveResourceMarkersWithMap)
            {
                return;
            }

            var path = GetSidecarPath(mapFilePath);
            if (path == null)
            {
                return;
            }

            if (map.MarkerLines == null || map.MarkerLines.Count == 0)
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                return;
            }

            var tempPath = path + ".mappytemp";
            using (var stream = File.Create(tempPath))
            {
                Write(map, stream);
            }

            if (File.Exists(path))
            {
                File.Delete(path);
            }

            File.Move(tempPath, path);
        }

        private static void Apply(UndoableMapModel map, Stream stream)
        {
            var serializer = new DataContractJsonSerializer(typeof(ResourceMarkerFile));
            var file = serializer.ReadObject(stream) as ResourceMarkerFile;
            if (file?.Lines == null)
            {
                return;
            }

            var lines = new List<MarkerLine>();
            foreach (var dto in file.Lines)
            {
                if (dto == null || !Guid.TryParse(dto.Id, out var id))
                {
                    continue;
                }

                if (dto.Points != null && dto.Points.Count >= 2)
                {
                    var points = new List<Point>(dto.Points.Count);
                    foreach (var point in dto.Points)
                    {
                        if (point == null)
                        {
                            continue;
                        }

                        points.Add(new Point(point.X, point.Y));
                    }

                    if (points.Count >= 2)
                    {
                        lines.Add(new MarkerLine(id, points));
                        continue;
                    }
                }

                lines.Add(new MarkerLine(id, new Point(dto.X1, dto.Y1), new Point(dto.X2, dto.Y2)));
            }

            map.LoadMarkerLines(lines);
        }

        private static void Write(UndoableMapModel map, Stream stream)
        {
            var file = new ResourceMarkerFile
            {
                Version = 1,
                Lines = new List<ResourceMarkerLineDto>(),
            };

            foreach (var line in map.MarkerLines)
            {
                var points = new List<ResourceMarkerPointDto>(line.Points.Count);
                foreach (var point in line.Points)
                {
                    points.Add(new ResourceMarkerPointDto { X = point.X, Y = point.Y });
                }

                file.Lines.Add(new ResourceMarkerLineDto
                {
                    Id = line.Id.ToString(),
                    X1 = line.Start.X,
                    Y1 = line.Start.Y,
                    X2 = line.End.X,
                    Y2 = line.End.Y,
                    Points = points,
                });
            }

            var serializer = new DataContractJsonSerializer(typeof(ResourceMarkerFile));
            serializer.WriteObject(stream, file);
        }

        [DataContract]
        private sealed class ResourceMarkerFile
        {
            [DataMember(Name = "version")]
            public int Version { get; set; }

            [DataMember(Name = "lines")]
            public List<ResourceMarkerLineDto> Lines { get; set; }
        }

        [DataContract]
        private sealed class ResourceMarkerLineDto
        {
            [DataMember(Name = "id")]
            public string Id { get; set; }

            [DataMember(Name = "x1")]
            public int X1 { get; set; }

            [DataMember(Name = "y1")]
            public int Y1 { get; set; }

            [DataMember(Name = "x2")]
            public int X2 { get; set; }

            [DataMember(Name = "y2")]
            public int Y2 { get; set; }

            [DataMember(Name = "points")]
            public List<ResourceMarkerPointDto> Points { get; set; }
        }

        [DataContract]
        private sealed class ResourceMarkerPointDto
        {
            [DataMember(Name = "x")]
            public int X { get; set; }

            [DataMember(Name = "y")]
            public int Y { get; set; }
        }
    }
}
