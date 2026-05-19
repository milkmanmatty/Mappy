namespace Mappy.Models
{
    public enum FillFeaturesCountMode
    {
        Percentage,
        FixedCount,
    }

    public class FillFeaturesOptions
    {
        public int MinHeight { get; set; } = 0;

        public int MaxHeight { get; set; } = 255;

        public int Padding { get; set; } = 32;

        public FillFeaturesCountMode CountMode { get; set; } = FillFeaturesCountMode.Percentage;

        public int DensityPercent { get; set; } = 50;

        public int FixedCount { get; set; } = 20;
    }
}
