namespace Mappy.Util
{
    using System;
    using System.Globalization;
    using Mappy;
    using Mappy.Data;

    public static class MetalSpotDisplayFormat
    {
        public static string FormatDisplayValue(Feature feature)
        {
            var raw = feature.MetalSpotValue;
            if (!MappySettings.Settings.ShowCalculatedMetalDepositValue)
            {
                return raw.ToString(CultureInfo.CurrentCulture);
            }

            return FormatCalculatedTotal(GetCalculatedValue(feature));
        }

        public static double GetCalculatedValue(Feature feature)
        {
            if (feature == null || feature.MetalSpotValue <= 0)
            {
                return 0;
            }

            var footprintTiles = feature.Footprint.Width * feature.Footprint.Height;
            if (footprintTiles < 1)
            {
                footprintTiles = 1;
            }

            // Apparently TA adds 1 to the base metal value (this fixes e.g. SlateMetal5)
            return (feature.MetalSpotValue + 1) * 0.001 * footprintTiles;
        }

        public static string FormatCalculatedTotal(double sum)
        {
            return Math.Round(sum, 1, MidpointRounding.AwayFromZero).ToString("0.0", CultureInfo.CurrentCulture);
        }
    }
}