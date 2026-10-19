namespace Mappy.IO
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text;

    using Mappy.Models;

    public static class OamapManifestWriter
    {
        public static string Write(OamapPackInfo info, string size, string previewPath, IList<string> files)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            var sb = new StringBuilder();
            sb.Append("oamap: 1\n");
            sb.Append("id: ").Append(Quote(info.PackId.Trim())).Append('\n');
            sb.Append("name: ").Append(Quote(info.PackName.Trim())).Append('\n');
            sb.Append("version: ").Append(Quote(info.Version.Trim())).Append('\n');
            if (!string.IsNullOrWhiteSpace(info.Description))
            {
                sb.Append("description: ").Append(Quote(info.Description.Trim())).Append('\n');
            }

            sb.Append("author: {name: ").Append(Quote(info.AuthorName.Trim()));
            if (!string.IsNullOrWhiteSpace(info.AuthorEmail))
            {
                sb.Append(", email: ").Append(Quote(info.AuthorEmail.Trim()));
            }

            sb.Append("}\n");
            sb.Append("packaging: {revision: ");
            sb.Append(info.PackagingRevision.ToString(CultureInfo.InvariantCulture));
            sb.Append(", date: ");
            sb.Append(Quote(DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
            sb.Append(", packager: ").Append(Quote(info.Packager.Trim())).Append("}\n");
            sb.Append("maps:\n");
            sb.Append("  - stem: ").Append(Quote(info.Stem.Trim())).Append('\n');
            sb.Append("    name: ").Append(Quote(info.MapName.Trim())).Append('\n');
            sb.Append("    players: ").Append(info.Players.ToString(CultureInfo.InvariantCulture)).Append('\n');
            sb.Append("    size: ").Append(size).Append('\n');
            if (!string.IsNullOrWhiteSpace(info.MapDescription))
            {
                sb.Append("    description: ").Append(Quote(info.MapDescription.Trim())).Append('\n');
            }

            if (!string.IsNullOrEmpty(previewPath))
            {
                sb.Append("    preview: ").Append(Quote(previewPath)).Append('\n');
            }

            sb.Append("    files:\n");
            if (files != null)
            {
                foreach (var file in files)
                {
                    sb.Append("      - ").Append(Quote(file)).Append('\n');
                }
            }

            return sb.ToString();
        }

        private static string Quote(string value)
        {
            var escaped = (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"");
            return "\"" + escaped + "\"";
        }
    }
}
