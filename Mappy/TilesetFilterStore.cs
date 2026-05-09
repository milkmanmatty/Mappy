namespace Mappy
{
    using System;
    using System.IO;
    using System.Xml.Serialization;

    public static class TilesetFilterStore
    {
        private static readonly string ConfigFileLocation = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            @"ArmouredFish\Mappy\tilesets.xml");

        public static TilesetFilterSettings Load()
        {
            if (!File.Exists(ConfigFileLocation))
            {
                return new TilesetFilterSettings();
            }

            try
            {
                var serializer = new XmlSerializer(typeof(TilesetFilterSettings));
                using (Stream st = File.OpenRead(ConfigFileLocation))
                {
                    var loaded = (TilesetFilterSettings)serializer.Deserialize(st);
                    return loaded ?? new TilesetFilterSettings();
                }
            }
            catch (InvalidOperationException)
            {
                return new TilesetFilterSettings();
            }
            catch (IOException)
            {
                return new TilesetFilterSettings();
            }
        }

        public static void Save(TilesetFilterSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            try
            {
                var dir = Path.GetDirectoryName(ConfigFileLocation);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                using (Stream st = File.Create(ConfigFileLocation))
                {
                    var serializer = new XmlSerializer(typeof(TilesetFilterSettings));
                    serializer.Serialize(st, settings);
                }
            }
            catch (UnauthorizedAccessException)
            {
            }
            catch (IOException)
            {
            }
        }
    }
}