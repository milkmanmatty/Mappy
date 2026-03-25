namespace Mappy
{
    using System;
    using System.IO;
    using System.Xml.Serialization;

    public static class MappySettings
    {
        private static readonly string ConfigFileLocation = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            @"ArmouredFish\Mappy\settings.xml");

        private static Configuration defaultSettings;

        public static event EventHandler SettingsSaved;

        public static Configuration Settings
        {
            get
            {
                if (defaultSettings == null)
                {
                    defaultSettings = LoadSettings();
                }

                return defaultSettings;
            }
        }

        public static void SaveSettings(bool notifyListeners = false)
        {
            try
            {
                var dir = Path.GetDirectoryName(ConfigFileLocation);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                using (Stream st = File.Create(ConfigFileLocation))
                {
                    var s = new XmlSerializer(typeof(Configuration));
                    s.Serialize(st, Settings);
                }

                if (notifyListeners)
                {
                    SettingsSaved?.Invoke(null, EventArgs.Empty);
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Ignore settings save failures so app shutdown never crashes.
            }
            catch (IOException)
            {
                // Ignore transient filesystem failures when saving settings.
            }
        }

        private static Configuration LoadSettings()
        {
            var s = new XmlSerializer(typeof(Configuration));

            if (!File.Exists(ConfigFileLocation))
            {
                return new Configuration();
            }

            try
            {
                using (Stream st = File.OpenRead(ConfigFileLocation))
                {
                    return (Configuration)s.Deserialize(st);
                }
            }
            catch (InvalidOperationException)
            {
                // TODO: notify the user we failed to load their settings
                return new Configuration();
            }
        }
    }
}
