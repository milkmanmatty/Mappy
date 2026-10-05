namespace Mappy.Services
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.IO.Compression;
    using System.Net;
    using System.Reflection;
    using System.Runtime.Serialization;
    using System.Runtime.Serialization.Json;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Windows.Forms;

    public static class UpdateService
    {
        private const string UserAgent = "Mappy";

        private static readonly Regex RepositoryPattern = new Regex(
            @"^[A-Za-z0-9](?:[A-Za-z0-9._-]*[A-Za-z0-9])?/[A-Za-z0-9](?:[A-Za-z0-9._-]*[A-Za-z0-9])?$",
            RegexOptions.CultureInvariant | RegexOptions.Compiled);

        private static readonly Regex AssetPattern = new Regex(
            @"^mappy-.+-win-x86\.zip$",
            RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static Version GetCurrentVersion()
        {
            return Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0, 0);
        }

        public static bool IsNewer(Version remote, Version local)
        {
            if (remote == null || local == null)
            {
                return false;
            }

            return remote > local;
        }

        public static UpdateRelease FetchLatestRelease(string repository, Func<bool> cancellationPending)
        {
            EnsureTls();
            if (cancellationPending != null && cancellationPending())
            {
                throw new UpdateCancelledException();
            }

            var normalized = NormalizeRepository(repository);
            if (!RepositoryPattern.IsMatch(normalized))
            {
                throw new UpdateCheckException("The update repository must be a GitHub owner/repo, for example milkmanmatty/Mappy.");
            }

            var url = "https://api.github.com/repos/" + normalized + "/releases/latest";
            GitHubRelease release;
            try
            {
                var json = DownloadString(url);
                if (cancellationPending != null && cancellationPending())
                {
                    throw new UpdateCancelledException();
                }

                using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                {
                    var serializer = new DataContractJsonSerializer(typeof(GitHubRelease));
                    release = serializer.ReadObject(stream) as GitHubRelease;
                }
            }
            catch (UpdateCancelledException)
            {
                throw;
            }
            catch (UpdateCheckException)
            {
                throw;
            }
            catch (WebException ex)
            {
                throw new UpdateCheckException(DescribeWebException(ex, normalized), ex);
            }
            catch (Exception ex)
            {
                throw new UpdateCheckException("There was a problem checking for updates.", ex);
            }

            if (release == null || string.IsNullOrWhiteSpace(release.TagName))
            {
                throw new UpdateCheckException("GitHub did not return a release version.");
            }

            if (!TryParseReleaseVersion(release.TagName, out var version))
            {
                throw new UpdateCheckException("The latest release tag \"" + release.TagName + "\" is not a version Mappy can compare.");
            }

            var asset = FindWindowsAsset(release);
            if (asset == null || string.IsNullOrWhiteSpace(asset.BrowserDownloadUrl))
            {
                throw new UpdateCheckException(
                    "The latest release does not include a Windows package (mappy-*-win-x86.zip).");
            }

            return new UpdateRelease
            {
                TagName = release.TagName.Trim(),
                Version = version,
                AssetName = asset.Name,
                DownloadUrl = asset.BrowserDownloadUrl,
                Size = asset.Size,
            };
        }

        public static PreparedUpdate DownloadAndExtract(
            UpdateRelease release,
            Action<int, string> reportProgress,
            Func<bool> cancellationPending)
        {
            if (release == null)
            {
                throw new ArgumentNullException(nameof(release));
            }

            EnsureTls();
            var workDirectory = Path.Combine(Path.GetTempPath(), "MappyUpdate", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(workDirectory);
            var zipPath = Path.Combine(workDirectory, SanitizeFileName(release.AssetName));
            var extractDirectory = Path.Combine(workDirectory, "extract");

            try
            {
                reportProgress?.Invoke(0, "Downloading update...");
                DownloadFile(release.DownloadUrl, zipPath, release.Size, reportProgress, cancellationPending);
                if (cancellationPending != null && cancellationPending())
                {
                    throw new UpdateCancelledException();
                }

                reportProgress?.Invoke(100, "Extracting update...");
                ExtractZipSafely(zipPath, extractDirectory);

                var exeName = Path.GetFileName(Application.ExecutablePath);
                if (string.IsNullOrEmpty(exeName) || !File.Exists(Path.Combine(extractDirectory, exeName)))
                {
                    throw new UpdateCheckException("The update package does not contain " + (exeName ?? "Mappy.exe") + ".");
                }

                return new PreparedUpdate
                {
                    WorkDirectory = workDirectory,
                    ZipPath = zipPath,
                    ExtractDirectory = extractDirectory,
                };
            }
            catch (UpdateCancelledException)
            {
                TryDeleteDirectory(workDirectory);
                throw;
            }
            catch (UpdateCheckException)
            {
                TryDeleteDirectory(workDirectory);
                throw;
            }
            catch (Exception ex)
            {
                TryDeleteDirectory(workDirectory);
                throw new UpdateCheckException("There was a problem downloading the update.", ex);
            }
        }

        public static void StartUpdater(PreparedUpdate prepared)
        {
            if (prepared == null)
            {
                throw new ArgumentNullException(nameof(prepared));
            }

            var installDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var executablePath = Application.ExecutablePath;
            var batPath = Path.Combine(prepared.WorkDirectory, "update.bat");
            File.WriteAllText(batPath, BuildUpdaterScript(prepared, installDirectory, executablePath), Encoding.ASCII);

            var startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/c \"" + batPath + "\"",
                CreateNoWindow = true,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden,
                WorkingDirectory = prepared.WorkDirectory,
            };

            Process.Start(startInfo);
        }

        public static void TryDeleteDirectory(string path)
        {
            try
            {
                if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        private static bool TryParseReleaseVersion(string tagName, out Version version)
        {
            version = null;
            if (string.IsNullOrWhiteSpace(tagName))
            {
                return false;
            }

            var text = tagName.Trim();
            if (text.StartsWith("v", StringComparison.OrdinalIgnoreCase))
            {
                text = text.Substring(1);
            }

            var cut = text.IndexOfAny(new[] { '-', '+' });
            if (cut >= 0)
            {
                text = text.Substring(0, cut);
            }

            return Version.TryParse(text, out version);
        }

        private static string NormalizeRepository(string repository)
        {
            var text = (repository ?? string.Empty).Trim().TrimEnd('/');
            const string httpsPrefix = "https://github.com/";
            const string httpPrefix = "http://github.com/";
            if (text.StartsWith(httpsPrefix, StringComparison.OrdinalIgnoreCase))
            {
                text = text.Substring(httpsPrefix.Length);
            }
            else if (text.StartsWith(httpPrefix, StringComparison.OrdinalIgnoreCase))
            {
                text = text.Substring(httpPrefix.Length);
            }

            if (text.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
            {
                text = text.Substring(0, text.Length - 4);
            }

            return text.Trim().TrimEnd('/');
        }

        private static GitHubAsset FindWindowsAsset(GitHubRelease release)
        {
            if (release.Assets == null)
            {
                return null;
            }

            foreach (var asset in release.Assets)
            {
                if (asset == null || string.IsNullOrWhiteSpace(asset.Name))
                {
                    continue;
                }

                if (AssetPattern.IsMatch(asset.Name))
                {
                    return asset;
                }
            }

            return null;
        }

        private static void EnsureTls()
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
        }

        private static string DownloadString(string url)
        {
            var request = CreateRequest(url);
            using (var response = (HttpWebResponse)request.GetResponse())
            using (var stream = response.GetResponseStream())
            using (var reader = new StreamReader(stream ?? Stream.Null))
            {
                return reader.ReadToEnd();
            }
        }

        private static void DownloadFile(
            string url,
            string destinationPath,
            long expectedSize,
            Action<int, string> reportProgress,
            Func<bool> cancellationPending)
        {
            var request = CreateRequest(url);
            using (var response = (HttpWebResponse)request.GetResponse())
            using (var stream = response.GetResponseStream())
            using (var file = File.Create(destinationPath))
            {
                if (stream == null)
                {
                    throw new UpdateCheckException("The update download returned no data.");
                }

                var total = response.ContentLength > 0 ? response.ContentLength : expectedSize;
                var buffer = new byte[81920];
                long received = 0;
                int read;
                while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    if (cancellationPending != null && cancellationPending())
                    {
                        throw new UpdateCancelledException();
                    }

                    file.Write(buffer, 0, read);
                    received += read;
                    if (total > 0)
                    {
                        var percent = (int)Math.Min(100, (received * 100L) / total);
                        reportProgress?.Invoke(percent, "Downloading update...");
                    }
                }
            }
        }

        private static HttpWebRequest CreateRequest(string url)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.UserAgent = UserAgent;
            request.Accept = "application/vnd.github+json";
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            return request;
        }

        private static void ExtractZipSafely(string zipPath, string destinationDirectory)
        {
            Directory.CreateDirectory(destinationDirectory);
            var destRoot = Path.GetFullPath(destinationDirectory);
            if (!destRoot.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
            {
                destRoot += Path.DirectorySeparatorChar;
            }

            using (var archive = ZipFile.OpenRead(zipPath))
            {
                foreach (var entry in archive.Entries)
                {
                    var fullPath = Path.GetFullPath(Path.Combine(destinationDirectory, entry.FullName.Replace('/', Path.DirectorySeparatorChar)));
                    if (!fullPath.StartsWith(destRoot, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new UpdateCheckException("The update package contains an invalid path.");
                    }

                    if (string.IsNullOrEmpty(entry.Name))
                    {
                        Directory.CreateDirectory(fullPath);
                        continue;
                    }

                    var parent = Path.GetDirectoryName(fullPath);
                    if (!string.IsNullOrEmpty(parent))
                    {
                        Directory.CreateDirectory(parent);
                    }

                    entry.ExtractToFile(fullPath, true);
                }
            }
        }

        private static string BuildUpdaterScript(PreparedUpdate prepared, string installDirectory, string executablePath)
        {
            var pid = Process.GetCurrentProcess().Id;
            var builder = new StringBuilder();
            builder.AppendLine("@echo off");
            builder.AppendLine("setlocal");
            builder.AppendLine("set \"PID=" + pid + "\"");
            builder.AppendLine("set \"SRC=" + prepared.ExtractDirectory + "\"");
            builder.AppendLine("set \"DEST=" + TrimTrailingSlash(installDirectory) + "\"");
            builder.AppendLine("set \"EXE=" + executablePath + "\"");
            builder.AppendLine("set \"ZIP=" + prepared.ZipPath + "\"");
            builder.AppendLine(":wait");
            builder.AppendLine("tasklist /FI \"PID eq %PID%\" 2>nul | find \"%PID%\" >nul");
            builder.AppendLine("if not errorlevel 1 (");
            builder.AppendLine("  ping -n 2 127.0.0.1 >nul");
            builder.AppendLine("  goto wait");
            builder.AppendLine(")");
            builder.AppendLine("ping -n 2 127.0.0.1 >nul");
            builder.AppendLine("set /a TRIES=0");
            builder.AppendLine(":copy");
            builder.AppendLine("set /a TRIES+=1");
            builder.AppendLine("xcopy /E /Y /I \"%SRC%\\*\" \"%DEST%\" >nul");
            builder.AppendLine("if not errorlevel 1 goto launch");
            builder.AppendLine("if %TRIES% GEQ 15 goto launch");
            builder.AppendLine("ping -n 2 127.0.0.1 >nul");
            builder.AppendLine("goto copy");
            builder.AppendLine(":launch");
            builder.AppendLine("start \"\" \"%EXE%\"");
            builder.AppendLine("del /q \"%ZIP%\" >nul 2>&1");
            builder.AppendLine("rmdir /s /q \"%SRC%\" >nul 2>&1");
            builder.AppendLine("del \"%~f0\"");
            return builder.ToString();
        }

        private static string TrimTrailingSlash(string path)
        {
            return (path ?? string.Empty).TrimEnd('\\', '/');
        }

        private static string SanitizeFileName(string name)
        {
            var fileName = string.IsNullOrWhiteSpace(name) ? "mappy-update.zip" : Path.GetFileName(name);
            foreach (var invalid in Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(invalid, '_');
            }

            return string.IsNullOrWhiteSpace(fileName) ? "mappy-update.zip" : fileName;
        }

        private static string DescribeWebException(WebException ex, string repository)
        {
            var response = ex.Response as HttpWebResponse;
            if (response != null && response.StatusCode == HttpStatusCode.NotFound)
            {
                return "No release was found for " + repository + ".";
            }

            return "Could not reach GitHub to check for updates.";
        }

        public sealed class UpdateRelease
        {
            public string TagName { get; set; }

            public Version Version { get; set; }

            public string AssetName { get; set; }

            public string DownloadUrl { get; set; }

            public long Size { get; set; }
        }

        public sealed class PreparedUpdate
        {
            public string WorkDirectory { get; set; }

            public string ZipPath { get; set; }

            public string ExtractDirectory { get; set; }
        }

        public class UpdateCheckException : Exception
        {
            public UpdateCheckException(string message)
                : base(message)
            {
            }

            public UpdateCheckException(string message, Exception innerException)
                : base(message, innerException)
            {
            }
        }

        public class UpdateCancelledException : Exception
        {
        }

        [DataContract]
        private sealed class GitHubRelease
        {
            [DataMember(Name = "tag_name")]
            public string TagName { get; set; }

            [DataMember(Name = "assets")]
            public GitHubAsset[] Assets { get; set; }
        }

        [DataContract]
        private sealed class GitHubAsset
        {
            [DataMember(Name = "name")]
            public string Name { get; set; }

            [DataMember(Name = "browser_download_url")]
            public string BrowserDownloadUrl { get; set; }

            [DataMember(Name = "size")]
            public long Size { get; set; }
        }
    }
}
