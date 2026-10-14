using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using WandEnhancer.Core.Services;

namespace WandEnhancer.Core
{
    internal static class UpdateNotifier
    {
        private static readonly string LatestReleaseApiUrl =
            $"https://api.github.com/repos/{Constants.Owner}/{Constants.RepoName}/releases/latest";

        private static readonly string ReleasesApiUrl =
            $"https://api.github.com/repos/{Constants.Owner}/{Constants.RepoName}/releases?per_page=20";

        // Local testing hook: pretend this tag is the latest public release.
        private const string TestUpdateEnvVar = "WAND_ENHANCER_TEST_UPDATE";

        public sealed class UpdateRelease
        {
            public string Version { get; set; }
            public string Notes { get; set; }
            public string Url { get; set; }
            public string DownloadUrl { get; set; }
            public string AssetName { get; set; }
            public long AssetSize { get; set; }
        }

        private sealed class GitHubRelease
        {
            [JsonProperty("tag_name")]
            public string TagName { get; set; }

            public string Body { get; set; }

            [JsonProperty("assets")]
            public GitHubReleaseAsset[] Assets { get; set; }
        }

        private sealed class GitHubReleaseAsset
        {
            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("size")]
            public long Size { get; set; }

            [JsonProperty("browser_download_url")]
            public string BrowserDownloadUrl { get; set; }
        }

        /// <summary>
        /// Checks GitHub for a newer release. <paramref name="onUpdateFound"/> runs on a
        /// background thread as soon as a newer release is detected;
        /// <paramref name="onNotificationClick"/> runs when the notification is clicked.
        /// Without a click action the release page opens in the browser.
        /// </summary>
        public static void CheckInBackground(Action<UpdateRelease> onUpdateFound = null,
            Action<UpdateRelease> onNotificationClick = null)
        {
            Task.Run(() => CheckAsync(onUpdateFound, onNotificationClick));
        }

        public static async Task<string> GetFullChangelogAsync()
        {
            try
            {
                GitHubRelease[] releases = await GetReleasesAsync();
                if (releases == null || releases.Length == 0)
                    return null;

                return string.Join(Environment.NewLine + Environment.NewLine,
                    releases.Select(r => $"## {r.TagName}{Environment.NewLine}{TrimBody(r.Body)}"));
            }
            catch (Exception e)
            {
                LauncherLog.Write($"Could not load the full changelog: {e.Message}", ELogType.Warn);
                return null;
            }
        }

        internal static bool IsNewerVersion(string tagName)
        {
            Version latestVersion;
            return TryParseVersion(tagName, out latestVersion) &&
                   latestVersion.CompareTo(Constants.Version) > 0;
        }

        internal static string BuildNotificationText(string releaseNotes)
        {
            if (string.IsNullOrWhiteSpace(releaseNotes))
                return "Click to view the release notes.";

            foreach (string sourceLine in releaseNotes.Replace("\r", string.Empty).Split('\n'))
            {
                string line = sourceLine.Trim();
                if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
                    continue;

                line = line.TrimStart('-', '*', '>').Trim();
                if (line.Length == 0)
                    continue;

                if (line.Length > 180)
                    line = line.Substring(0, 177).TrimEnd() + "...";

                return line + Environment.NewLine + "Click to view the release notes.";
            }

            return "Click to view the release notes.";
        }

        private static async Task CheckAsync(Action<UpdateRelease> onUpdateFound,
            Action<UpdateRelease> onNotificationClick)
        {
            try
            {
                var settings = SettingsManager.LoadSettings();
                if (settings?.CheckUpdates == false)
                {
                    LauncherLog.Write("Update check is disabled in settings.", ELogType.Info);
                    return;
                }

                GitHubRelease release = await GetNewestReleaseAsync(settings?.CheckPrereleases == true);
                if (release == null)
                    return;

                string testTag = Environment.GetEnvironmentVariable(TestUpdateEnvVar);
                if (!string.IsNullOrWhiteSpace(testTag))
                    release.TagName = testTag;

                if (!IsNewerVersion(release.TagName))
                    return;

                var update = new UpdateRelease
                {
                    Version = release.TagName.Trim().TrimStart('v', 'V'),
                    Notes = release.Body,
                    Url = Constants.RepositoryUrl + "/releases/tag/" + Uri.EscapeDataString(release.TagName)
                };

                var exeAsset = release.Assets?.FirstOrDefault(a =>
                    !string.IsNullOrEmpty(a.Name) &&
                    (a.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) || a.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)));

                if (exeAsset != null)
                {
                    update.DownloadUrl = exeAsset.BrowserDownloadUrl;
                    update.AssetName = exeAsset.Name;
                    update.AssetSize = exeAsset.Size;
                }
                else
                {
                    update.DownloadUrl = $"{Constants.RepositoryUrl}/releases/download/{release.TagName}/WandEnhancer.exe";
                    update.AssetName = "WandEnhancer.exe";
                }

                LauncherLog.Write($"WandEnhancer {update.Version} is available: {update.Url}", ELogType.Info);
                onUpdateFound?.Invoke(update);
                ShowNotification(update, onNotificationClick);
            }
            catch (Exception e)
            {
                LauncherLog.Write($"Update check failed: {e.Message}", ELogType.Warn);
            }
        }

        // /releases/latest only ever returns full releases; with prereleases enabled the
        // newest published entry (prerelease or not) wins instead.
        private static async Task<GitHubRelease> GetNewestReleaseAsync(bool includePrereleases)
        {
            if (!includePrereleases)
                return ReadRelease(await GetResponseBodyAsync(CreateRequest(LatestReleaseApiUrl)));

            GitHubRelease[] releases = await GetReleasesAsync();
            return releases?.FirstOrDefault();
        }

        private static async Task<GitHubRelease[]> GetReleasesAsync()
        {
            string body = await GetResponseBodyAsync(CreateRequest(ReleasesApiUrl));
            return string.IsNullOrWhiteSpace(body)
                ? null
                : JsonConvert.DeserializeObject<GitHubRelease[]>(body);
        }

        private static HttpWebRequest CreateRequest(string url)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Accept = "application/vnd.github+json";
            request.UserAgent = $"WandEnhancer/{Constants.Version}";
            return request;
        }

        // Timeout does not apply to async responses, so abort the request manually.
        private static async Task<string> GetResponseBodyAsync(HttpWebRequest request)
        {
            using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
            using (timeout.Token.Register(() => request.Abort()))
            using (var response = (HttpWebResponse)await request.GetResponseAsync())
            using (Stream stream = response.GetResponseStream())
            using (var reader = new StreamReader(stream))
            {
                return await reader.ReadToEndAsync();
            }
        }

        private static GitHubRelease ReadRelease(string body)
        {
            return string.IsNullOrWhiteSpace(body)
                ? null
                : JsonConvert.DeserializeObject<GitHubRelease>(body);
        }

        private static string TrimBody(string body)
        {
            return string.IsNullOrWhiteSpace(body) ? string.Empty : body.Trim();
        }

        private static bool TryParseVersion(string tagName, out Version version)
        {
            version = null;
            if (string.IsNullOrWhiteSpace(tagName))
                return false;

            string normalized = tagName.Trim().TrimStart('v', 'V');
            int suffix = normalized.IndexOf('-');
            if (suffix >= 0)
                normalized = normalized.Substring(0, suffix);

            return Version.TryParse(normalized, out version);
        }

        private static void ShowNotification(UpdateRelease update, Action<UpdateRelease> onNotificationClick)
        {
            var thread = new Thread(() =>
            {
                try
                {
                    RunNotification(update, onNotificationClick);
                }
                catch (Exception e)
                {
                    LauncherLog.Write($"Could not show the update notification: {e.Message}", ELogType.Warn);
                }
            })
            {
                IsBackground = true,
                Name = "WandEnhancer update notification"
            };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }

        private static void RunNotification(UpdateRelease update, Action<UpdateRelease> onNotificationClick)
        {
            // ExtractAssociatedIcon allocates a GDI handle NotifyIcon does not own.
            using (Icon icon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location))
            using (var context = new ApplicationContext())
            using (var notification = new NotifyIcon())
            using (var timer = new System.Windows.Forms.Timer { Interval = 30000 })
            {
                notification.Icon = icon;
                notification.Text = "WandEnhancer update available";
                notification.BalloonTipTitle = $"WandEnhancer {update.Version} is available";
                notification.BalloonTipText = BuildNotificationText(update.Notes);
                notification.BalloonTipIcon = ToolTipIcon.Info;
                notification.BalloonTipClicked += (sender, args) =>
                {
                    context.ExitThread();
                    if (onNotificationClick != null)
                        onNotificationClick(update);
                    else
                        OpenRelease(update.Url);
                };

                timer.Tick += (sender, args) => context.ExitThread();
                notification.Visible = true;
                notification.ShowBalloonTip(15000);
                timer.Start();
                Application.Run(context);
                notification.Visible = false;
            }
        }

        public static void OpenRelease(string releaseUrl)
        {
            try
            {
                Process.Start(releaseUrl);
            }
            catch (Exception e)
            {
                LauncherLog.Write($"Could not open the release page: {e.Message}", ELogType.Warn);
            }
        }

        /// <summary>
        /// Downloads the latest release binary from GitHub and automatically swaps/updates the running executable.
        /// </summary>
        public static async Task DownloadAndApplyUpdateAsync(
            UpdateRelease update,
            IProgress<int> progress = null,
            Action<string, ELogType> logger = null)
        {
            if (update == null || string.IsNullOrEmpty(update.DownloadUrl))
            {
                throw new ArgumentException("Invalid update release metadata or download URL.");
            }

            string tempUpdateExe = Path.Combine(Path.GetTempPath(), $"WandEnhancer-Update-{Guid.NewGuid():N}.exe");
            logger?.Invoke($"[GITHUB-UPDATE] Downloading WandEnhancer v{update.Version} from GitHub...", ELogType.Info);

            using (var client = new WebClient())
            {
                client.Headers.Add("User-Agent", $"WandEnhancer/{Constants.Version}");
                client.Headers.Add("Accept", "application/octet-stream");
                
                int lastPercent = -1;
                client.DownloadProgressChanged += (s, e) =>
                {
                    progress?.Report(e.ProgressPercentage);
                    if (e.ProgressPercentage % 25 == 0 && e.ProgressPercentage != lastPercent)
                    {
                        lastPercent = e.ProgressPercentage;
                        logger?.Invoke($"[GITHUB-UPDATE] Downloading update: {e.ProgressPercentage}%", ELogType.Info);
                    }
                };

                await client.DownloadFileTaskAsync(new Uri(update.DownloadUrl), tempUpdateExe);
            }

            if (!File.Exists(tempUpdateExe) || new FileInfo(tempUpdateExe).Length < 10000)
            {
                throw new IOException("Downloaded update file is missing or corrupted.");
            }

            logger?.Invoke("[GITHUB-UPDATE] Update download complete! Applying update and restarting...", ELogType.Success);

            string currentExe = Assembly.GetExecutingAssembly().Location;
            string updaterScript = Path.Combine(Path.GetTempPath(), $"wand-update-{Guid.NewGuid():N}.cmd");

            string scriptContent = $@"@echo off
timeout /t 1 /nobreak > nul
:retry
copy /y ""{tempUpdateExe}"" ""{currentExe}"" > nul
if errorlevel 1 (
    timeout /t 1 /nobreak > nul
    goto retry
)
del ""{tempUpdateExe}"" > nul 2>&1
start """" ""{currentExe}""
del ""%~f0"" > nul 2>&1
";

            File.WriteAllText(updaterScript, scriptContent);

            Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c \"{updaterScript}\"",
                CreateNoWindow = true,
                UseShellExecute = false
            });

            System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
            {
                System.Windows.Application.Current.Shutdown();
            });
        }
    }
}
