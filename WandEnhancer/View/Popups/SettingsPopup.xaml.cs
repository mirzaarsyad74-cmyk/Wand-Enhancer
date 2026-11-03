using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using WandEnhancer.Core;
using WandEnhancer.Core.Services;
using WandEnhancer.View.MainWindow;

namespace WandEnhancer.View.Popups
{
    public partial class SettingsPopup : UserControl
    {
        private CultureInfo _selectedLanguage;

        public SettingsPopup()
        {
            InitializeComponent();
            LoadLanguages();
            var savedSettings = SettingsManager.LoadSettings();
            if (string.Equals(savedSettings?.PreferredDownloadClient, "WeMod", System.StringComparison.OrdinalIgnoreCase))
            {
                PreferredClientComboBox.SelectedIndex = 1;
            }
            else
            {
                PreferredClientComboBox.SelectedIndex = 0;
            }
#if ENABLE_UPDATE_NOTIFICATIONS
            UpdateCheckCheckBox.IsChecked = savedSettings?.CheckUpdates ?? true;
            CheckPrereleasesCheckBox.IsChecked = savedSettings?.CheckPrereleases ?? false;
#else
            UpdateCheckRows.Visibility = Visibility.Collapsed;
#endif
        }

        private void LoadLanguages()
        {
            var items = LocalizationManager.SupportedLanguages
                .Select(c => new LanguageItem
                {
                    Culture = c,
                    DisplayName = LocalizationManager.GetLanguageDisplayName(c)
                })
                .ToList();

            LanguageComboBox.ItemsSource = items;
            
            var currentItem = items.FirstOrDefault(i => i.Culture.Name == LocalizationManager.CurrentLanguage?.Name);
            if (currentItem != null)
            {
                LanguageComboBox.SelectedItem = currentItem;
            }
            
            _selectedLanguage = LocalizationManager.CurrentLanguage;
        }

        private void OnLanguageSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LanguageComboBox.SelectedItem is LanguageItem item)
            {
                _selectedLanguage = item.Culture;
            }
        }

        private void OnSaveClick(object sender, RoutedEventArgs e)
        {
            if (_selectedLanguage != null &&
                (LocalizationManager.CurrentLanguage == null ||
                 _selectedLanguage.Name != LocalizationManager.CurrentLanguage.Name))
            {
                LocalizationManager.CurrentLanguage = _selectedLanguage;
            }

            var settings = SettingsManager.LoadSettings() ?? new AppSettings();
            var selectedItem = PreferredClientComboBox.SelectedItem as ComboBoxItem;
            settings.PreferredDownloadClient = selectedItem?.Tag?.ToString() ?? "Wand";

#if ENABLE_UPDATE_NOTIFICATIONS
            settings.CheckUpdates = UpdateCheckCheckBox.IsChecked == true;
            settings.CheckPrereleases = CheckPrereleasesCheckBox.IsChecked == true;
#endif
            SettingsManager.SaveSettings(settings);
            MainWindow.MainWindow.Instance?.ClosePopup();
        }

#if ENABLE_UPDATE_NOTIFICATIONS
        private async void OnCheckUpdatesNowClick(object sender, RoutedEventArgs e)
        {
            CheckUpdatesNowButton.IsEnabled = false;
            CheckUpdatesNowButton.Content = "Checking...";
            try
            {
                var update = await UpdateNotifier.CheckForUpdatesAsync((msg, type) =>
                {
                    MainWindow.MainWindow.Instance?.ViewModel?.Log(msg, type);
                }, silent: false);

                if (update != null && update.IsNewer)
                {
                    MainWindow.MainWindow.Instance?.ClosePopup();
                    MainWindow.MainWindow.Instance?.ViewModel?.OpenUpdatePopup(update);
                }
            }
            finally
            {
                CheckUpdatesNowButton.IsEnabled = true;
                CheckUpdatesNowButton.Content = "Check for Updates Now";
            }
        }
#else
        private void OnCheckUpdatesNowClick(object sender, RoutedEventArgs e)
        {
        }
#endif

        private class LanguageItem
        {
            public CultureInfo Culture { get; set; }
            public string DisplayName { get; set; }
        }
    }
}
