using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using WandEnhancer.Core;
using WandEnhancer.Core.Services;
using WandEnhancer.Models;
using WandEnhancer.ReactiveUICore;
using WandEnhancer.Utils;
using WandEnhancer.View.Popups;
using Application = System.Windows.Application;

namespace WandEnhancer.View.MainWindow
{
    public class MainWindowVm : ObservableObject
    {
        private const string LogExportFilter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";

        private readonly IShellView _shell;
        private readonly IFileDialogs _dialogs;
        public ObservableCollection<LogEntry> LogList { get; } = new ObservableCollection<LogEntry>();
        private WeModConfig _weModConfig;

        public WeModConfig WeModInfo
        {
            get => _weModConfig;
            set
            {
                if (SetProperty(ref _weModConfig, value))
                {
                    OnPropertyChanged(nameof(HasInstall));
                    NotifyStatusChanged();
                }
            }
        }

        public bool HasInstall => _weModConfig != null;

        private int _downloadProgress;
        public int DownloadProgress
        {
            get => _downloadProgress;
            set => SetProperty(ref _downloadProgress, value);
        }

        private bool _isDownloading;
        public bool IsDownloading
        {
            get => _isDownloading;
            set => SetProperty(ref _isDownloading, value);
        }

        private void UseInstall(WeModConfig config)
        {
            WeModInfo = config;
            NotifyStatusChanged();
            if (config == null)
            {
                AlreadyPatched = false;
                CanRestore = false;
                IsPatchEnabled = false;
                return;
            }

            var verInfo = !string.IsNullOrEmpty(config.Version) ? $" (v{config.Version})" : "";
            Log(LocalizationManager.Format("log_install_found", config, config.ExecutableName) + verInfo, ELogType.Success);
            AlreadyPatched = Enhancer.IsPatched(config.RootDirectory);
            CanRestore = Enhancer.HasBackup(config.RootDirectory);
            IsPatchEnabled = !AlreadyPatched;
            NotifyStatusChanged();

            Log(LocalizationManager.Get(AlreadyPatched ? "log_already_patched" : "log_ready"),
                AlreadyPatched ? ELogType.Warn : ELogType.Info);
        }

        private bool _isPatchEnabled;

        public bool IsPatchEnabled
        {
            get => _isPatchEnabled;
            set => SetProperty(ref _isPatchEnabled, value);
        }

        private bool _alreadyPatched;

        public bool AlreadyPatched
        {
            get => _alreadyPatched;
            set
            {
                if (SetProperty(ref _alreadyPatched, value))
                {
                    NotifyStatusChanged();
                }
            }
        }

        private bool _canRestore;

        public bool CanRestore
        {
            get => _canRestore;
            set => SetProperty(ref _canRestore, value);
        }

        private bool _isBusy;

        /// <summary>True while a patch or restore runs.</summary>
        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    OnPropertyChanged(nameof(IsIdle));
                    NotifyStatusChanged();
                }
            }
        }

        /// <summary>Bound by buttons that must not be clickable mid-run.</summary>
        public bool IsIdle => !_isBusy;

        private void NotifyStatusChanged()
        {
            OnPropertyChanged(nameof(HasInstall));
            OnPropertyChanged(nameof(StatusBadgeText));
            OnPropertyChanged(nameof(StatusBadgeBackground));
            OnPropertyChanged(nameof(StatusBadgeBorder));
            OnPropertyChanged(nameof(StatusBadgeForeground));
            OnPropertyChanged(nameof(AutoButtonText));
        }

        public string StatusBadgeText
        {
            get
            {
                if (IsBusy) return "⚡ Processing...";
                var verStr = (WeModInfo != null && !string.IsNullOrEmpty(WeModInfo.Version)) ? $" v{WeModInfo.Version}" : "";
                if (AlreadyPatched) return $"● Wand{verStr} · Patched";
                if (WeModInfo != null) return $"● Wand{verStr} · Not Patched";
                return "● Wand Not Found";
            }
        }

        public string StatusBadgeBackground
        {
            get
            {
                if (IsBusy) return "#1e1b4b";
                if (AlreadyPatched) return "#052e16";
                if (WeModInfo != null) return "#082f49";
                return "#2e1d05";
            }
        }

        public string StatusBadgeBorder
        {
            get
            {
                if (IsBusy) return "#6366f1";
                if (AlreadyPatched) return "#166534";
                if (WeModInfo != null) return "#0369a1";
                return "#b45309";
            }
        }

        public string StatusBadgeForeground
        {
            get
            {
                if (IsBusy) return "#a5b4fc";
                if (AlreadyPatched) return "#4ade80";
                if (WeModInfo != null) return "#38bdf8";
                return "#fcd34d";
            }
        }

        private bool _isTerminalAutoScroll = true;

        public bool IsTerminalAutoScroll
        {
            get => _isTerminalAutoScroll;
            set => SetProperty(ref _isTerminalAutoScroll, value);
        }

        public string AutoButtonText
        {
            get
            {
                if (IsBusy) return "⚡ " + LocalizationManager.Get("mw_terminal_busy") + "...";
                if (WeModInfo == null) return LocalizationManager.Get("mw_auto_download");
                return LocalizationManager.Get("mw_auto_patch");
            }
        }

        public RelayCommand SetFolderPathCommand { get; }
        public RelayCommand ApplyPatchCommand { get; }
        public RelayCommand RestoreBackupCommand { get; }
        public RelayCommand OpenSettingsCommand { get; }
        public RelayCommand CopyLogsCommand { get; }
        public RelayCommand ExportLogsCommand { get; }
        public RelayCommand FullAutoCommand { get; }
        public RelayCommand AutoRepairCommand { get; }
        public RelayCommand UninstallCommand { get; }
        public RelayCommand ClearLogsCommand { get; }
        public RelayCommand ToggleAutoScrollCommand { get; }

        private void OnFolderPathSelection(object obj)
        {
            string selectedPath = _dialogs.PickFolder(
                LocalizationManager.Get("dialog_pick_install"),
                Environment.GetEnvironmentVariable("LOCALAPPDATA"));
            if (selectedPath == null)
            {
                return;
            }

            var info = WeModInstalls.CheckWeModPath(selectedPath);
            if (info == null)
            {
                Log(LocalizationManager.Format("log_invalid_directory", Path.GetFileName(selectedPath)), ELogType.Error);
                return;
            }

            UseInstall(info);
        }

        // Runs off the UI thread due to heavy file IO.
        private async void OnBackupRestoring(object param)
        {
            if (WeModInfo == null)
            {
                Log(LocalizationManager.Get("log_no_directory"), ELogType.Warn);
                return;
            }

            IsBusy = true;
            bool restored = await Task.Run(() =>
            {
                try
                {
                    new Enhancer(WeModInfo, Log).Restore();
                    return true;
                }
                catch (Exception e)
                {
                    Log(LocalizationManager.Format("log_restore_failed", e.Message), ELogType.Error);
                    return false;
                }
            });

            IsBusy = false;
            if (restored)
            {
                AlreadyPatched = false;
                CanRestore = false;
                IsPatchEnabled = true;
            }
        }

        private void OnPatching(object param)
        {
            if (WeModInfo == null)
            {
                Log(LocalizationManager.Get("log_no_directory"), ELogType.Warn);
                return;
            }

            _shell.OpenPopup(new PatchVectorsPopup(async config =>
            {
                _shell.ClosePopup();
                IsPatchEnabled = false;
                IsBusy = true;
                await Task.Run(() =>
                {
                    try
                    {
                        new Enhancer(WeModInfo, Log, config).Patch();
                        AlreadyPatched = true;
                        CanRestore = true;
                        LaunchWand();
                    }
                    catch (Exception e)
                    {
                        Log(LocalizationManager.Format("log_patch_failed", e.Message), ELogType.Error);
                        IsPatchEnabled = true;
                    }
                });
                IsBusy = false;
            }), LocalizationManager.Get("pv_popup_title"));
        }

        private void Log(string message, ELogType logType)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var entry = new LogEntry
                {
                    Timestamp = DateTime.Now,
                    LogType = logType,
                    Message = message.StartsWith("[") ? message : $"[{logType.ToString().ToUpper()}] {message}"
                };
                LogList.Add(entry);
                if (IsTerminalAutoScroll)
                {
                    _shell.ScrollLogIntoView(entry);
                }
                // Force CanExecute re-evaluation when appending log entries.
                System.Windows.Input.CommandManager.InvalidateRequerySuggested();
            });
        }

        private void OnToggleAutoScroll(object param)
        {
            IsTerminalAutoScroll = !IsTerminalAutoScroll;
            Log(IsTerminalAutoScroll ? "[TERMINAL] Auto-scroll enabled." : "[TERMINAL] Auto-scroll paused.", ELogType.Info);
        }

        private void OnClearLogs(object param)
        {
            LogList.Clear();
            Log("[TERMINAL] Live buffer cleared.", ELogType.Info);
        }

        private async void OnFullAuto(object param)
        {
            if (IsBusy) return;

            IsBusy = true;
            OnPropertyChanged(nameof(AutoButtonText));

            await Task.Run(async () =>
            {
                try
                {
                    if (WeModInfo == null)
                    {
                        Log("[FULL-AUTO] Wand installation not found. Starting automatic download and installation...", ELogType.Info);
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            DownloadProgress = 0;
                            IsDownloading = true;
                        });
                        var progress = new Progress<int>(pct =>
                        {
                            Application.Current.Dispatcher.Invoke(() => DownloadProgress = pct);
                        });
                        string installer = await WandManagerService.DownloadLatestWandAsync(Log, progress);
                        Application.Current.Dispatcher.Invoke(() => IsDownloading = false);
                        var newConfig = await WandManagerService.InstallWandAsync(installer, Log);
                        Application.Current.Dispatcher.Invoke(() => UseInstall(newConfig));
                    }

                    if (WeModInfo == null)
                    {
                        Log("[FULL-AUTO] Could not locate or initialize Wand installation.", ELogType.Error);
                        return;
                    }

                    Log("[FULL-AUTO] Applying full automated patch configuration...", ELogType.Info);
                    WandManagerService.AutoPatch(WeModInfo, Log);

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        AlreadyPatched = true;
                        CanRestore = true;
                        IsPatchEnabled = false;
                        OnPropertyChanged(nameof(AutoButtonText));
                    });

                    Log("[FULL-AUTO] ⚡ Everything is set up and ready! Enjoy Wand with Pro features.", ELogType.Success);
                    LaunchWand();
                }
                catch (Exception ex)
                {
                    Log($"[FULL-AUTO] Auto setup failed: {ex.Message}", ELogType.Error);
                }
                finally
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        IsDownloading = false;
                        IsBusy = false;
                        OnPropertyChanged(nameof(AutoButtonText));
                    });
                }
            });
        }

        private async void OnAutoRepair(object param)
        {
            if (IsBusy) return;

            IsBusy = true;
            OnPropertyChanged(nameof(AutoButtonText));

            await Task.Run(async () =>
            {
                try
                {
                    Log("[AUTO-REPAIR] Initiating full auto-repair and reinstall sequence...", ELogType.Info);
                    var progress = new Progress<int>(pct =>
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            IsDownloading = true;
                            DownloadProgress = pct;
                        });
                    });
                    var repairedConfig = await WandManagerService.AutoRepairAsync(WeModInfo, Log, progress);
                    Application.Current.Dispatcher.Invoke(() => IsDownloading = false);

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (repairedConfig != null)
                        {
                            UseInstall(repairedConfig);
                            AlreadyPatched = true;
                            CanRestore = true;
                            IsPatchEnabled = false;
                        }
                        OnPropertyChanged(nameof(AutoButtonText));
                    });

                    Log("[AUTO-REPAIR] ⚡ Wand successfully repaired and re-patched!", ELogType.Success);
                    LaunchWand();
                }
                catch (Exception ex)
                {
                    Log($"[AUTO-REPAIR] Repair failed: {ex.Message}", ELogType.Error);
                }
                finally
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        IsDownloading = false;
                        IsBusy = false;
                        OnPropertyChanged(nameof(AutoButtonText));
                    });
                }
            });
        }

        private async void OnUninstall(object param)
        {
            if (WeModInfo == null || IsBusy) return;

            var title = LocalizationManager.Get("dialog_uninstall_title");
            var message = LocalizationManager.Get("dialog_uninstall_confirm");
            var result = System.Windows.MessageBox.Show(
                message,
                title,
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (result != System.Windows.MessageBoxResult.Yes)
            {
                return;
            }

            IsBusy = true;
            OnPropertyChanged(nameof(AutoButtonText));

            await Task.Run(async () =>
            {
                try
                {
                    Log("[UNINSTALL] Starting complete Wand/WeMod uninstallation...", ELogType.Info);
                    bool success = await WandManagerService.UninstallWandAsync(WeModInfo, Log);
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (success)
                        {
                            UseInstall(null);
                        }
                    });
                }
                catch (Exception ex)
                {
                    Log($"[UNINSTALL] Error during uninstallation: {ex.Message}", ELogType.Error);
                }
                finally
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        IsBusy = false;
                        OnPropertyChanged(nameof(AutoButtonText));
                    });
                }
            });
        }

        private void OnOpenSettings(object param)
        {
            _shell.OpenPopup(new SettingsPopup(), LocalizationManager.Get("settings_title"));
        }

        private string BuildLogReport()
        {
            var builder = new StringBuilder();
            foreach (var entry in LogList)
            {
                builder.AppendLine($"[{entry.TimeFormatted}] [{entry.Tag}] {entry.CleanMessage}");
            }
            return builder.ToString();
        }

        private void OnCopyLogs(object param)
        {
            try
            {
                System.Windows.Clipboard.SetText(BuildLogReport());
                Log(LocalizationManager.Get("log_copied"), ELogType.Success);
            }
            catch (Exception e)
            {
                Log(LocalizationManager.Format("log_copy_failed", e.Message), ELogType.Error);
            }
        }

        private void OnExportLogs(object param)
        {
            string path = _dialogs.PickSaveFile(
                LogExportFilter,
                $"wand-enhancer-log-{DateTime.Now:yyyyMMdd-HHmmss}.txt");
            if (path == null)
            {
                return;
            }

            try
            {
                File.WriteAllText(path, BuildLogReport());
                Log(LocalizationManager.Format("log_exported", path), ELogType.Success);
            }
            catch (Exception e)
            {
                Log(LocalizationManager.Format("log_export_failed", e.Message), ELogType.Error);
            }
        }

        private bool HasLogs(object param) => LogList.Count > 0;

        /// <summary>Displays the repository URL if the browser fails to open.</summary>
        public void ReportRepositoryLinkFailure(string url)
        {
            Log(LocalizationManager.Format("log_open_link_failed", url), ELogType.Warn);
        }

        private void LaunchWand()
        {
            try
            {
                if (WeModInfo == null || string.IsNullOrEmpty(WeModInfo.ExecutablePath) || !File.Exists(WeModInfo.ExecutablePath))
                {
                    Log("[LAUNCH] Wand executable not found for auto-launch.", ELogType.Warn);
                    return;
                }

                Log($"[LAUNCH] Launching Wand ({Path.GetFileName(WeModInfo.ExecutablePath)})...", ELogType.Info);
                var startInfo = new ProcessStartInfo
                {
                    FileName = WeModInfo.ExecutablePath,
                    WorkingDirectory = Path.GetDirectoryName(WeModInfo.ExecutablePath),
                    UseShellExecute = true
                };
                Process.Start(startInfo);
                Log("[LAUNCH] 🚀 Wand launched successfully!", ELogType.Success);
            }
            catch (Exception ex)
            {
                Log($"[LAUNCH] Failed to auto-launch Wand: {ex.Message}", ELogType.Warn);
            }
        }

        public MainWindowVm(IShellView shell, IFileDialogs dialogs)
        {
            _shell = shell;
            _dialogs = dialogs;
            SetFolderPathCommand = new RelayCommand(OnFolderPathSelection);
            ApplyPatchCommand = new RelayCommand(OnPatching);
            RestoreBackupCommand = new RelayCommand(OnBackupRestoring);
            OpenSettingsCommand = new RelayCommand(OnOpenSettings);
            CopyLogsCommand = new RelayCommand(OnCopyLogs, HasLogs);
            ExportLogsCommand = new RelayCommand(OnExportLogs, HasLogs);
            FullAutoCommand = new RelayCommand(OnFullAuto);
            AutoRepairCommand = new RelayCommand(OnAutoRepair);
            UninstallCommand = new RelayCommand(OnUninstall);
            ClearLogsCommand = new RelayCommand(OnClearLogs);
            ToggleAutoScrollCommand = new RelayCommand(OnToggleAutoScroll);

            // Display anti-scam notice on startup
            Log("[SECURITY] " + LocalizationManager.Get("mw_scam_warning"), ELogType.Warn);

            UseInstall(WeModInstalls.FindWeMod());
            if (WeModInfo == null)
            {
                Log(LocalizationManager.Get("log_install_not_found"), ELogType.Error);
            }

            foreach (var entry in Program.StartupLog)
            {
                Log(entry.Key, entry.Value);
            }
            Program.StartupLog.Clear();

#if ENABLE_UPDATE_NOTIFICATIONS
            ShowUpdateCommand = new RelayCommand(OnShowUpdate);
            UpdateNotifier.CheckInBackground(OnUpdateFound, OnNotificationClick);
#endif
        }

#if ENABLE_UPDATE_NOTIFICATIONS
        private UpdateNotifier.UpdateRelease _availableUpdate;
        private bool _isUpdateAvailable;

        public bool IsUpdateAvailable
        {
            get => _isUpdateAvailable;
            private set => SetProperty(ref _isUpdateAvailable, value);
        }

        public RelayCommand ShowUpdateCommand { get; }

        private void OnShowUpdate(object param)
        {
            if (_availableUpdate != null)
                OpenUpdatePopup(_availableUpdate);
        }

        private void OnUpdateFound(UpdateNotifier.UpdateRelease update)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _availableUpdate = update;
                IsUpdateAvailable = true;
            });
        }

        private void OnNotificationClick(UpdateNotifier.UpdateRelease update)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var window = Application.Current.MainWindow;
                if (window != null)
                {
                    window.Topmost = true;
                    window.Activate();
                    window.Topmost = false;
                }

                OpenUpdatePopup(update);
            });
        }

        private void OpenUpdatePopup(UpdateNotifier.UpdateRelease update)
        {
            _shell.OpenPopup(
                new UpdatePopup(Constants.Version.ToString(), update.Version, update.Notes,
                    update.Url, UpdateNotifier.GetFullChangelogAsync),
                LocalizationManager.Get("up_popup_title"));
        }
#endif
    }
}
