using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using WandEnhancer.Core;
using WandEnhancer.Core.Services;

namespace WandEnhancer.View.Popups
{
    public partial class UpdatePopup : UserControl
    {
        private readonly Func<Task<string>> _loadFullChangelog;
        private readonly string _latestNotes;
        private readonly string _releaseUrl;
        private readonly UpdateNotifier.UpdateRelease _updateRelease;
        private string _fullChangelog;
        private bool _showingFullChangelog;

        internal UpdatePopup(string currentVersion, string latestVersion, string latestNotes,
            string releaseUrl, Func<Task<string>> loadFullChangelog, UpdateNotifier.UpdateRelease updateRelease = null)
        {
            InitializeComponent();

            _updateRelease = updateRelease ?? new UpdateNotifier.UpdateRelease
            {
                Version = latestVersion,
                Notes = latestNotes,
                Url = releaseUrl,
                DownloadUrl = $"{Constants.RepositoryUrl}/releases/download/{latestVersion}/WandEnhancer.exe"
            };

            CurrentVersionValue.Text = currentVersion;
            LatestVersionValue.Text = latestVersion;
            _latestNotes = string.IsNullOrWhiteSpace(latestNotes)
                ? LocalizationManager.Get("up_release_notes_unavailable")
                : latestNotes;
            _releaseUrl = releaseUrl;
            _loadFullChangelog = loadFullChangelog;

            SetNotesText(_latestNotes);
            ShowMoreButton.Visibility = loadFullChangelog == null ? Visibility.Collapsed : Visibility.Visible;
        }

        private void OnOpenReleaseClick(object sender, RoutedEventArgs e)
        {
            UpdateNotifier.OpenRelease(_releaseUrl);
        }

        private async void OnAutoUpdateClick(object sender, RoutedEventArgs e)
        {
            try
            {
                AutoUpdateButton.IsEnabled = false;
                AutoUpdateButton.Content = LocalizationManager.Get("up_updating");
                SetNotesText(LocalizationManager.Get("up_downloading_update"));

                var progress = new Progress<int>(percent =>
                {
                    AutoUpdateButton.Content = $"{percent}%";
                });

                await UpdateNotifier.DownloadAndApplyUpdateAsync(_updateRelease, progress, (msg, type) =>
                {
                    Dispatcher.Invoke(() => SetNotesText(msg));
                });
            }
            catch (Exception ex)
            {
                AutoUpdateButton.IsEnabled = true;
                AutoUpdateButton.Content = LocalizationManager.Get("up_auto_update");
                SetNotesText($"Update failed: {ex.Message}");
            }
        }

        private async void OnShowMoreClick(object sender, RoutedEventArgs e)
        {
            if (_loadFullChangelog == null)
                return;

            if (_showingFullChangelog)
            {
                _showingFullChangelog = false;
                SetNotesText(_latestNotes);
                ShowMoreButton.Content = LocalizationManager.Get("up_show_more");
                return;
            }

            if (_fullChangelog == null)
            {
                ShowMoreButton.IsEnabled = false;
                SetNotesText(LocalizationManager.Get("up_loading_changelog"));

                _fullChangelog = await _loadFullChangelog();
                if (_fullChangelog == null)
                    _fullChangelog = LocalizationManager.Get("up_changelog_failed");

                ShowMoreButton.IsEnabled = true;
            }

            _showingFullChangelog = true;
            SetNotesText(_fullChangelog);
            ShowMoreButton.Content = LocalizationManager.Get("up_show_less");
        }

        private void SetNotesText(string text)
        {
            NotesTextBlock.Text = text;
            NotesScrollViewer.ScrollToTop();
        }
    }
}
