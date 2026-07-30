namespace Mappy
{
    using System.Xml.Serialization;

    public class RecentMapEntry
    {
        public string FilePath { get; set; }

        /// <summary>
        /// Gets or sets the map basename inside an archive (without extension). Null/empty for standalone files.
        /// </summary>
        public string MapName { get; set; }

        [XmlIgnore]
        public string DisplayText
        {
            get
            {
                if (string.IsNullOrEmpty(this.MapName))
                {
                    return this.FilePath ?? string.Empty;
                }

                return $"{this.FilePath} ({this.MapName})";
            }
        }
    }
}
