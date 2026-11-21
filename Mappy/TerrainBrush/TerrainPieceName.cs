namespace Mappy.TerrainBrush
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// A transition section name, such as Cliff-230-High-B-02 or CC-219-B-85-TL-0-TR-01.
    /// The name lists a label and a region from the right. Corners the name does not
    /// mention share one implicit label, written here with a leading '!'.
    /// </summary>
    public sealed class TerrainPieceName
    {
        private const string TransitionToken = "Transition";

        private TerrainPieceName(string prefix, string[] cornerLabels, string transition)
        {
            this.Prefix = prefix;
            this.CornerLabels = cornerLabels;
            this.Transition = transition;
        }

        public string Prefix { get; }

        public string[] CornerLabels { get; }

        /// <summary>
        /// The side named by a trailing Transition token, as in Slope-High-B-TransitionL,
        /// where a slope piece meets a cliff. Null when the name has none.
        /// </summary>
        public string Transition { get; }

        public static bool TryParse(string sectionName, out TerrainPieceName piece)
        {
            piece = null;
            if (string.IsNullOrEmpty(sectionName))
            {
                return false;
            }

            var name = sectionName;
            if (name.EndsWith(".sct", StringComparison.OrdinalIgnoreCase))
            {
                name = name.Substring(0, name.Length - 4);
            }

            var tokens = name.Split('-');
            if (tokens.Length >= 2 && IsAllDigits(tokens[tokens.Length - 1]))
            {
                var trimmed = new string[tokens.Length - 1];
                Array.Copy(tokens, trimmed, trimmed.Length);
                tokens = trimmed;
            }

            string transition = null;
            var last = tokens[tokens.Length - 1];
            if (tokens.Length >= 2
                && last.Length > TransitionToken.Length
                && last.StartsWith(TransitionToken, StringComparison.OrdinalIgnoreCase))
            {
                transition = last.Substring(TransitionToken.Length);
                var trimmed = new string[tokens.Length - 1];
                Array.Copy(tokens, trimmed, trimmed.Length);
                tokens = trimmed;
            }

            var labels = new List<string>();
            var regions = new List<int[]>();
            var index = tokens.Length - 1;
            while (index >= 1 && TryRegion(tokens[index], out var region))
            {
                if (tokens[index - 1].Length == 0)
                {
                    return false;
                }

                labels.Add(tokens[index - 1]);
                regions.Add(region);
                index -= 2;
            }

            if (labels.Count == 0 || index < 0)
            {
                return false;
            }

            var prefixParts = new string[index + 1];
            Array.Copy(tokens, prefixParts, prefixParts.Length);
            foreach (var part in prefixParts)
            {
                if (part.Length == 0)
                {
                    return false;
                }
            }

            var corners = new string[TerrainCorners.Count];
            string allLabel = null;
            for (var pair = labels.Count - 1; pair >= 0; pair--)
            {
                var region = regions[pair];
                if (region == null)
                {
                    allLabel = labels[pair];
                    continue;
                }

                foreach (var corner in region)
                {
                    corners[corner] = labels[pair];
                }
            }

            var rest = allLabel ?? "!" + labels[labels.Count - 1];
            for (var corner = 0; corner < corners.Length; corner++)
            {
                if (corners[corner] == null)
                {
                    corners[corner] = rest;
                }
            }

            piece = new TerrainPieceName(string.Join("-", prefixParts), corners, transition);
            return true;
        }

        private static bool TryRegion(string token, out int[] corners)
        {
            switch (token.ToUpperInvariant())
            {
                case "TL":
                    corners = new[] { TerrainCorners.TopLeft };
                    return true;
                case "TR":
                    corners = new[] { TerrainCorners.TopRight };
                    return true;
                case "BL":
                    corners = new[] { TerrainCorners.BottomLeft };
                    return true;
                case "BR":
                    corners = new[] { TerrainCorners.BottomRight };
                    return true;
                case "T":
                    corners = new[] { TerrainCorners.TopLeft, TerrainCorners.TopRight };
                    return true;
                case "B":
                    corners = new[] { TerrainCorners.BottomLeft, TerrainCorners.BottomRight };
                    return true;
                case "L":
                    corners = new[] { TerrainCorners.TopLeft, TerrainCorners.BottomLeft };
                    return true;
                case "R":
                    corners = new[] { TerrainCorners.TopRight, TerrainCorners.BottomRight };
                    return true;
                case "TL_AND_BR":
                    corners = new[] { TerrainCorners.TopLeft, TerrainCorners.BottomRight };
                    return true;
                case "BL_AND_TR":
                    corners = new[] { TerrainCorners.TopRight, TerrainCorners.BottomLeft };
                    return true;
                case "ALL":
                    corners = null;
                    return true;
                default:
                    corners = null;
                    return false;
            }
        }

        private static bool IsAllDigits(string token)
        {
            if (token.Length == 0)
            {
                return false;
            }

            foreach (var character in token)
            {
                if (character < '0' || character > '9')
                {
                    return false;
                }
            }

            return true;
        }
    }
}
