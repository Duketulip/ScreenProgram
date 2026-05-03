using System.Windows;
using ShowPlayer.App.Models;

namespace ShowPlayer.App.Views
{
    public partial class SettingsDialog : Window
    {
        private readonly AppSettings _settings;

        public SettingsDialog(AppSettings settings)
        {
            InitializeComponent();
            _settings = settings;

            TransitionTypeBox.SelectedIndex = settings.TransitionType == TransitionType.Fade ? 0 : 1;

            DurationSlider.Value = settings.TransitionDurationMs;
            DurationLabel.Text = $"{settings.TransitionDurationMs}ms";
            DurationSlider.ValueChanged += (s, e) =>
                DurationLabel.Text = $"{e.NewValue:F0}ms";

            VideoVolumeSlider.Value = settings.VideoVolume;
            VideoVolumeLabel.Text = settings.VideoVolume.ToString();
            VideoVolumeSlider.ValueChanged += (s, e) =>
                VideoVolumeLabel.Text = $"{e.NewValue:F0}";

            MusicVolumeSlider.Value = settings.MusicVolume;
            MusicVolumeLabel.Text = settings.MusicVolume.ToString();
            MusicVolumeSlider.ValueChanged += (s, e) =>
                MusicVolumeLabel.Text = $"{e.NewValue:F0}";
        }

        private void OnOk(object sender, RoutedEventArgs e)
        {
            _settings.TransitionType = TransitionTypeBox.SelectedIndex == 0
                ? TransitionType.Fade : TransitionType.Crossfade;
            _settings.TransitionDurationMs = (int)DurationSlider.Value;
            _settings.VideoVolume = (int)VideoVolumeSlider.Value;
            _settings.MusicVolume = (int)MusicVolumeSlider.Value;

            DialogResult = true;
            Close();
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
