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

            var footprintTiles = feature.Footprint.Width * feature.Footprint.Height;
            var value = raw * 0.001 * footprintTiles;
            return Math.Round(value, 1, MidpointRounding.AwayFromZero).ToString("0.0", CultureInfo.CurrentCulture);
        }
    }
}