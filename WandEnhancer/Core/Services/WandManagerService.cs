using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using WandEnhancer.Models;
using WandEnhancer.Utils;

namespace WandEnhancer.Core.Services
{
    public static class WandManagerService
    {
        // Primary official direct setup download endpoint and fallback CDN links.
        public const string PrimaryWandDownloadUrl = "https://api.wemod.com/client/download";
        public const string FallbackWandCdnUrl = "https://storage-cdn.wemod.com/app/releases/stable/Wand-12.63.0.exe";
        public const string FallbackWeModCdnUrl = "https://storage-updater.wemod.com/releases/WeMod-Setup.exe";

        static WandManagerService()
        {
            try
            {
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12 | (SecurityProtocolType)3072;
            }
            catch
            {
                // Fall back to default security protocols if TLS 1.3/1.2 assignment fails.
            }
        }

        /// <summary>
        /// Automatically downloads the latest Wand/WeMod official installer from CDN to %TEMP%.
        /// </summary>
        public static async Task<string> DownloadLatestWandAsync(
            Action<string, ELogType> logger,
            IProgress<int> progress = null)
        {
            logger("[WAND-AUTO] Resolving latest official Wand installer from CDN...", ELogType.Info);

            string targetUrl = await ResolveDownloadUrlAsync(PrimaryWandDownloadUrl, logger);
            string tempInstallerPath = Path.Combine(Path.GetTempPath(), "Wand-Setup.exe");

            if (File.Exists(tempInstallerPath))
            {
                try
                {
                    File.Delete(tempInstallerPath);
                }
                catch
                {
                    tempInstallerPath = Path.Combine(Path.GetTempPath(), $"Wand-Setup-{Guid.NewGuid():N}.exe");
                }
            }

            logger($"[WAND-AUTO] Connecting to download server: {targetUrl}", ELogType.Info);

            using (var webClient = new WebClient())
            {
                webClient.Headers.Add("User-Agent", $"WandEnhancer/{Constants.Version} AutoInstaller");
                
                int lastReported = -1;
                webClient.DownloadProgressChanged += (s, e) =>
                {
                    progress?.Report(e.ProgressPercentage);
                    if (e.ProgressPercentage % 20 == 0 && e.ProgressPercentage != lastReported)
                    {
                        lastReported = e.ProgressPercentage;
                        long mbReceived = e.BytesReceived / (1024 * 1024);
                        long mbTotal = e.TotalBytesToReceive > 0 ? e.TotalBytesToReceive / (1024 * 1024) : 0;
                        string sizeInfo = mbTotal > 0 ? $"{mbReceived}MB / {mbTotal}MB" : $"{mbReceived}MB";
                        logger($"[WAND-AUTO] Downloading latest Wand... {e.ProgressPercentage}% ({sizeInfo})", ELogType.Info);
                    }
                };

                try
                {
                    await webClient.DownloadFileTaskAsync(new Uri(targetUrl), tempInstallerPath);
                }
                catch (Exception ex)
                {
                    logger($"[WAND-AUTO] Primary download failed: {ex.Message}. Trying fallback CDN...", ELogType.Warn);
                    await webClient.DownloadFileTaskAsync(new Uri(FallbackWandCdnUrl), tempInstallerPath);
                }
            }

            var fileInfo = new FileInfo(tempInstallerPath);
            if (!fileInfo.Exists || fileInfo.Length < 1024 * 1024)
            {
                throw new IOException($"Downloaded installer is invalid or empty ({fileInfo.Length} bytes).");
            }

            logger($"[WAND-AUTO] Download complete! ({fileInfo.Length / (1024 * 1024):N1} MB) -> {tempInstallerPath}", ELogType.Success);
            return tempInstallerPath;
        }

        private static async Task<string> ResolveDownloadUrlAsync(string primaryUrl, Action<string, ELogType> logger)
        {
            try
            {
                var request = (HttpWebRequest)WebRequest.Create(primaryUrl);
                request.Method = "HEAD";
                request.AllowAutoRedirect = false;
                request.UserAgent = $"WandEnhancer/{Constants.Version}";
                request.Timeout = 10000;

                using (var response = (HttpWebResponse)await request.GetResponseAsync())
                {
                    if (response.StatusCode == HttpStatusCode.Redirect ||
                        response.StatusCode == HttpStatusCode.MovedPermanently ||
                        response.StatusCode == HttpStatusCode.Found)
                    {
                        string location = response.Headers["Location"];
                        if (!string.IsNullOrEmpty(location))
                        {
                            return location;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger($"[WAND-AUTO] Head request note: {ex.Message}. Proceeding with direct URL.", ELogType.Info);
            }

            return primaryUrl;
        }

        /// <summary>
        /// Automatically installs or reinstalls Wand silently, polling until the install directory appears.
        /// </summary>
        public static async Task<WeModConfig> InstallWandAsync(string installerPath, Action<string, ELogType> logger)
        {
            logger("[WAND-AUTO] Terminating any running Wand/WeMod processes before install...", ELogType.Info);
            KillAllWandProcesses();

            logger("[WAND-AUTO] Launching Wand setup...", ELogType.Info);
            var psi = new ProcessStartInfo
            {
                FileName = installerPath,
                Arguments = "--silent",
                UseShellExecute = true
            };

            Process setupProcess = null;
            try
            {
                setupProcess = Process.Start(psi);
            }
            catch (Exception ex)
            {
                logger($"[WAND-AUTO] Launch failed with --silent: {ex.Message}. Retrying default...", ELogType.Warn);
                psi.Arguments = "";
                setupProcess = Process.Start(psi);
            }

            logger("[WAND-AUTO] Monitoring installation progress...", ELogType.Info);

            // Poll for newly created install directory (up to 90 seconds)
            WeModConfig foundConfig = null;
            for (int i = 0; i < 60; i++)
            {
                await Task.Delay(1500);

                foundConfig = WeModInstalls.FindWeMod();
                if (foundConfig != null &&
                    File.Exists(Path.Combine(foundConfig.RootDirectory, "resources", "app.asar")))
                {
                    logger($"[WAND-AUTO] Wand installation detected at: {foundConfig.RootDirectory}", ELogType.Success);
                    break;
                }

                if (i % 6 == 0 && i > 0)
                {
                    logger($"[WAND-AUTO] Waiting for Wand installation to finalize... ({i * 1.5:F0}s)", ELogType.Info);
                }
            }

            // Wait a brief moment for files to settle and lock to release
            await Task.Delay(2500);

            // Kill any process auto-launched by the installer so files are unlocked for patching
            KillAllWandProcesses();

            if (foundConfig == null)
            {
                foundConfig = WeModInstalls.FindWeMod();
            }

            if (foundConfig == null)
            {
                throw new TimeoutException("Timed out waiting for Wand installation to complete.");
            }

            return foundConfig;
        }

        /// <summary>
        /// Automatically applies the recommended patch vectors (Pro Activation, DevTools F12, Remote Web Panel).
        /// </summary>
        public static void AutoPatch(WeModConfig config, Action<string, ELogType> logger)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config), "Wand configuration is required for auto-patching.");
            }

            logger("[AUTO-PATCH] Applying full enhancement package...", ELogType.Info);
            logger("[AUTO-PATCH] Selected: Activate Pro, DevTools F12, Remote Web Panel, Static Strategy.", ELogType.Info);

            var patchConfig = new PatchConfig
            {
                PatchTypes = new System.Collections.Generic.HashSet<EPatchType>
                {
                    EPatchType.ActivatePro,
                    EPatchType.RemoteWebPanelPreview,
                    EPatchType.DevToolsOnF12
                },
                Strategy = EPatchStrategy.Static,
                AutoApplyAfterUpdate = true
            };

            new Enhancer(config, logger, patchConfig).Patch();
            logger("[AUTO-PATCH] ⚡ Auto-patching completed successfully! Wand is fully enhanced.", ELogType.Success);
        }

        /// <summary>
        /// Automatically repairs a damaged install: attempts clean restore first,
        /// or downloads and reinstalls the official version, then re-applies patches.
        /// </summary>
        public static async Task<WeModConfig> AutoRepairAsync(
            WeModConfig currentConfig,
            Action<string, ELogType> logger,
            IProgress<int> progress = null)
        {
            logger("[AUTO-REPAIR] Starting automated repair and restore sequence...", ELogType.Info);
            KillAllWandProcesses();

            bool restoredSuccessfully = false;
            if (currentConfig != null && Enhancer.HasBackup(currentConfig.RootDirectory))
            {
                try
                {
                    logger("[AUTO-REPAIR] Restoring clean backup from disk...", ELogType.Info);
                    new Enhancer(currentConfig, logger).Restore();
                    restoredSuccessfully = true;
                    logger("[AUTO-REPAIR] Backup restored cleanly.", ELogType.Success);
                }
                catch (Exception ex)
                {
                    logger($"[AUTO-REPAIR] Disk restore could not finish: {ex.Message}. Proceeding to reinstall...", ELogType.Warn);
                }
            }

            if (!restoredSuccessfully)
            {
                logger("[AUTO-REPAIR] Downloading fresh official Wand installer for clean repair...", ELogType.Info);
                string installerPath = await DownloadLatestWandAsync(logger, progress);
                currentConfig = await InstallWandAsync(installerPath, logger);
            }

            logger("[AUTO-REPAIR] Re-applying auto-patch to repaired installation...", ELogType.Info);
            AutoPatch(currentConfig, logger);
            logger("[AUTO-REPAIR] ⚡ Auto-repair completed! Wand is restored and enhanced.", ELogType.Success);

            return currentConfig;
        }

        /// <summary>
        /// Terminates all running Wand and WeMod process instances.
        /// </summary>
        public static void KillAllWandProcesses()
        {
            foreach (var brand in Constants.WeModBrandNames)
            {
                ProcessTerminator.TryKillProcess(brand);
            }
        }
    }
}
