using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace ShowPlayer.App.Models
{
    public class PlaylistItem : INotifyPropertyChanged
    {
        private string _id = Guid.NewGuid().ToString();
        private MediaType _type;
        private string _filePath = string.Empty;
        private string? _musicPath;
        private bool _isMusicLoop;
        private bool _isVideoLoop;
        private bool _isMissing;
        private bool _isNowPlaying;

        public string Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public MediaType Type
        {
            get => _type;
            set => SetProperty(ref _type, value);
        }

        public string FilePath
        {
            get => _filePath;
            set => SetProperty(ref _filePath, value);
        }

        public string? MusicPath
        {
            get => _musicPath;
            set => SetProperty(ref _musicPath, value);
        }

        public bool IsMusicLoop
        {
            get => _isMusicLoop;
            set => SetProperty(ref _isMusicLoop, value);
        }

        public bool IsVideoLoop
        {
            get => _isVideoLoop;
            set => SetProperty(ref _isVideoLoop, value);
        }

        public bool IsMissing
        {
            get => _isMissing;
            set => SetProperty(ref _isMissing, value);
        }

        public bool IsNowPlaying
        {
            get => _isNowPlaying;
            set => SetProperty(ref _isNowPlaying, value);
        }

        public string DisplayName => Path.GetFileName(FilePath);

        public event PropertyChangedEventHandler? PropertyChanged;

        private void SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (!EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                if (propertyName != nameof(DisplayName))
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayName)));
            }
        }

        public void NotifyAllProperties()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
        }
    }
}
