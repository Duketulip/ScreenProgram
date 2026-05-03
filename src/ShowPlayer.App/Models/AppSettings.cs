using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ShowPlayer.App.Models
{
    public enum TransitionType
    {
        Fade,
        Crossfade
    }

    public class AppSettings : INotifyPropertyChanged
    {
        private TransitionType _transitionType = TransitionType.Fade;
        private int _transitionDurationMs = 500;
        private string? _fallbackImagePath;
        private string? _fallbackVideoPath;
        private bool _isDarkTheme;
        private int _videoVolume = 100;
        private int _musicVolume = 100;

        public TransitionType TransitionType
        {
            get => _transitionType;
            set { _transitionType = value; OnPropertyChanged(); }
        }

        public int TransitionDurationMs
        {
            get => _transitionDurationMs;
            set { _transitionDurationMs = Math.Clamp(value, 100, 5000); OnPropertyChanged(); }
        }

        public string? FallbackImagePath
        {
            get => _fallbackImagePath;
            set { _fallbackImagePath = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasFallbackImage)); }
        }

        public string? FallbackVideoPath
        {
            get => _fallbackVideoPath;
            set { _fallbackVideoPath = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasFallbackVideo)); }
        }

        public bool IsDarkTheme
        {
            get => _isDarkTheme;
            set { _isDarkTheme = value; OnPropertyChanged(); }
        }

        public int VideoVolume
        {
            get => _videoVolume;
            set { _videoVolume = Math.Clamp(value, 0, 100); OnPropertyChanged(); }
        }

        public int MusicVolume
        {
            get => _musicVolume;
            set { _musicVolume = Math.Clamp(value, 0, 100); OnPropertyChanged(); }
        }

        public bool HasFallbackImage => !string.IsNullOrEmpty(FallbackImagePath);
        public bool HasFallbackVideo => !string.IsNullOrEmpty(FallbackVideoPath);

        private string? _defaultIdleImagePath;

        public string? DefaultIdleImagePath
        {
            get => _defaultIdleImagePath;
            set { _defaultIdleImagePath = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasDefaultIdleImage)); }
        }

        public bool HasDefaultIdleImage => !string.IsNullOrEmpty(DefaultIdleImagePath);

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
