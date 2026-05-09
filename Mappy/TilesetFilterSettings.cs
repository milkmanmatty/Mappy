namespace Mappy
{
    using System.Xml.Serialization;

    [XmlRoot("TilesetFilter")]
    public sealed class TilesetFilterSettings
    {
        public string[] VisibleWorlds { get; set; }
    }
}