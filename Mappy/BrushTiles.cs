namespace Mappy
{
    using System;
    using System.Collections.Specialized;
    using System.Globalization;
    using System.IO;
    using System.Security.Cryptography;
    using System.Text;

    using Mappy.Services;

    /// <summary>
    /// Downloads the curated brush tileset beside Mappy and records it on the search path.
    /// </summary>
    public static class BrushTiles
    {
        public const string ArchiveFileName = "GE-Favorites-RandomMapGeneratorTiles.hpi";

        public const string GalaxyEditorUrl = "https://github.com/MattA9K/Total-Annihilation-Galaxy-Editor";

        public static string InstallDirectory =>
            AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        public static bool IsExcludedFromSectionList(string hpiFileName)
        {
            return MappySettings.Settings.ExcludeBrushTilesFromSections && IsBrushArchive(hpiFileName);
        }

        public static bool IsBrushArchive(string hpiFileName)
        {
            if (string.IsNullOrWhiteSpace(hpiFileName))
            {
                return false;
            }

            string destination;
            string error;
            if (!TryGetDestinationPath(MappySettings.Settings.GetBrushTilesUrlOrDefault(), out destination, out error))
            {
                return false;
            }

            try
            {
                return string.Equals(
                    Path.GetFullPath(hpiFileName),
                    Path.GetFullPath(destination),
                    StringComparison.OrdinalIgnoreCase);
            }
            catch (ArgumentException)
            {
                return false;
            }
            catch (NotSupportedException)
            {
                return false;
            }
        }

        public static bool TryGetDestinationPath(string url, out string path, out string error)
        {
            path = null;
            error = null;
            if (string.IsNullOrWhiteSpace(url))
            {
                error = "Enter the URL of the brush tileset.";
                return false;
            }

            Uri uri;
            if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out uri)
                || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                error = "Enter a valid http or https URL.";
                return false;
            }

            var fileName = Path.GetFileName(Uri.UnescapeDataString(uri.AbsolutePath));
            if (string.IsNullOrEmpty(fileName)
                || fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
                || !fileName.EndsWith(".hpi", StringComparison.OrdinalIgnoreCase))
            {
                error = "The URL must point to an .hpi file.";
                return false;
            }

            path = Path.Combine(InstallDirectory, fileName);
            return true;
        }

        public static void Download(
            string url,
            Action<int, string> reportProgress,
            Func<bool> cancellationPending)
        {
            string destination;
            string error;
            if (!TryGetDestinationPath(url, out destination, out error))
            {
                throw new UpdateService.UpdateCheckException(error);
            }

            var directory = Path.GetDirectoryName(destination);
            if (string.IsNullOrEmpty(directory))
            {
                throw new UpdateService.UpdateCheckException("Mappy could not find its install folder.");
            }

            var tempPath = destination + ".download";
            try
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }

                UpdateService.DownloadToFile(url.Trim(), tempPath, reportProgress, cancellationPending);
                if (cancellationPending != null && cancellationPending())
                {
                    throw new UpdateService.UpdateCancelledException();
                }

                reportProgress?.Invoke(100, "Saving brush tiles...");
                if (File.Exists(destination))
                {
                    File.Replace(tempPath, destination, null);
                }
                else
                {
                    File.Move(tempPath, destination);
                }
            }
            catch (UpdateService.UpdateCancelledException)
            {
                throw;
            }
            catch (UpdateService.UpdateCheckException)
            {
                throw;
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new UpdateService.UpdateCheckException(
                    "Mappy could not write the tileset next to Mappy.exe. Run Mappy from a folder you can write to.",
                    ex);
            }
            catch (IOException ex)
            {
                throw new UpdateService.UpdateCheckException(
                    "Mappy could not write the tileset next to Mappy.exe. " + ex.Message,
                    ex);
            }
            finally
            {
                TryDelete(tempPath);
            }

            reportProgress?.Invoke(100, "Checking the download...");
            var hash = ComputeSha256(destination);
            var settings = MappySettings.Settings;
            settings.BrushTilesUrl = url.Trim();
            settings.BrushTilesDownloadedUtc = DateTime.UtcNow;
            settings.BrushTilesSha256 = hash;
            EnsureInstallDirectoryOnSearchPath(settings);
            MappySettings.SaveSettings();
        }

        public static string ComputeSha256(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(stream);
                var builder = new StringBuilder(hash.Length * 2);
                foreach (var part in hash)
                {
                    builder.Append(part.ToString("x2", CultureInfo.InvariantCulture));
                }

                return builder.ToString();
            }
        }

        public static void EnsureInstallDirectoryOnSearchPath(Configuration settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            var dir = InstallDirectory;
            if (settings.SearchPaths == null)
            {
                settings.SearchPaths = new StringCollection();
            }

            foreach (var existing in settings.SearchPaths)
            {
                if (string.Equals(TrimDirectory(existing), dir, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }

            settings.SearchPaths.Add(dir);
        }

        private static string TrimDirectory(string path)
        {
            return (path ?? string.Empty).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
