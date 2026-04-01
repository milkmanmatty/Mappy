namespace Mappy.Models
{
    using Mappy.Models.Enums;

    public sealed class MapImageExportOptions
    {
        public bool IncludeSections { get; set; } = true;

        public FeatureExportMode FeatureMode { get; set; } = FeatureExportMode.None;

        public int? UnitSchemaIndex { get; set; }

        public string FilePath { get; set; }
    }
}
