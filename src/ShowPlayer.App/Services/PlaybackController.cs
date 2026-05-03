using ShowPlayer.App.Models;

namespace ShowPlayer.App.Services
{
    public class PlaybackController
    {
        private readonly PlaylistManager _playlistManager;
        private readonly VlcMediaPlayer _mediaPlayer;
        private int _currentIndex = -1;

        public int CurrentIndex => _currentIndex;
        public bool HasCurrent => CurrentItem != null;
        public bool IsVideoPlaying => _mediaPlayer.IsVideoPlaying;
        public bool IsMusicPlaying => _mediaPlayer.IsMusicPlaying;
        public VlcMediaPlayer MediaPlayer => _mediaPlayer;

        public PlaylistItem? CurrentItem => _playingItem;

        private PlaylistItem? _playingItem;

        public event EventHandler<PlaylistItem>? ItemStarted;
        public event EventHandler? PlaybackStopped;
        public event EventHandler? MediaEnded;

        public PlaybackController(PlaylistManager playlistManager, VlcMediaPlayer mediaPlayer)
        {
            _playlistManager = playlistManager;
            _mediaPlayer = mediaPlayer;
            _mediaPlayer.MediaEnded += OnMediaEnded;
        }

        public void PlayItem(int index)
        {
            PlayItem(index, 0);
        }

        private void PlayItem(int index, int attempt)
        {
            if (attempt >= _playlistManager.Items.Count)
            {
                Logger.Warning("所有文件均缺失或无效，停止播放");
                Stop();
                return;
            }

            if (index < 0 || index >= _playlistManager.Items.Count)
            {
                Stop();
                return;
            }

            var item = _playlistManager.Items[index];
            if (item.IsMissing)
            {
                Logger.Warning($"跳过缺失文件: {item.FilePath}");
                PlayItem((index + 1) % _playlistManager.Items.Count, attempt + 1);
                return;
            }

            StopCurrent();

            _currentIndex = index;
            _playingItem = item;
            ItemStarted?.Invoke(this, item);
        }

        public void PlayItem(PlaylistItem item)
        {
            var index = _playlistManager.Items.IndexOf(item);
            if (index >= 0)
                PlayItem(index);
        }

        public void Next()
        {
            var nextIndex = _currentIndex + 1;
            if (nextIndex >= _playlistManager.Items.Count)
            {
                if (_currentIndex >= _playlistManager.Items.Count)
                {
                    nextIndex = 0;
                }
                else
                {
                    _currentIndex = _playlistManager.Items.Count;
                    _playingItem = null;
                    StopCurrent();
                    PlaybackStopped?.Invoke(this, EventArgs.Empty);
                    return;
                }
            }

            PlayItem(nextIndex);
        }

        public void Previous()
        {
            var prevIndex = _currentIndex - 1;
            if (prevIndex < 0)
            {
                Logger.Info("已在列表开头");
                return;
            }

            PlayItem(prevIndex);
        }

        public void Stop()
        {
            StopCurrent();
            _currentIndex = -1;
            _playingItem = null;
            PlaybackStopped?.Invoke(this, EventArgs.Empty);
        }

        public void StopCurrent()
        {
            _mediaPlayer.StopVideo();
            _mediaPlayer.StopMusicImmediate();
        }

        public void PauseVideo()
        {
            if (_playingItem?.Type == MediaType.Video)
                _mediaPlayer.PauseVideo();
            else if (_playingItem?.Type == MediaType.Image)
                _mediaPlayer.PauseMusic();
        }

        public void ResumeVideo()
        {
            if (_playingItem?.Type == MediaType.Video)
                _mediaPlayer.ResumeVideo();
            else if (_playingItem?.Type == MediaType.Image)
                _mediaPlayer.ResumeMusic();
        }

        public void SeekVideo(long timeMs)
        {
            if (_playingItem?.Type == MediaType.Video)
                _mediaPlayer.SeekVideo(timeMs);
        }

        public long GetVideoTime() => _mediaPlayer.GetVideoTime();

        public long GetVideoLength() => _mediaPlayer.GetVideoLength();

        public void SetVideoLoop(bool loop)
        {
            if (_playingItem != null)
                _playingItem.IsVideoLoop = loop;
        }

        public void SetMusicPath(string? musicPath)
        {
            if (_playingItem != null)
                _playingItem.MusicPath = musicPath;
        }

        public void SetMusicLoop(bool loop)
        {
            if (_playingItem != null)
                _playingItem.IsMusicLoop = loop;
        }

        public void PlayMusicForCurrent()
        {
            var item = _playingItem;
            if (item?.Type == MediaType.Image && !string.IsNullOrEmpty(item.MusicPath))
            {
                _mediaPlayer.PlayMusic(item.MusicPath, item.IsMusicLoop);
            }
        }

        public void RestartMusic()
        {
            StopCurrent();
            PlayMusicForCurrent();
        }

        private void OnMediaEnded(object? sender, EventArgs e)
        {
            MediaEnded?.Invoke(this, EventArgs.Empty);
        }

        public bool CanGoNext => _currentIndex < _playlistManager.Items.Count - 1;
        public bool CanGoPrevious => _currentIndex > 0;

        public void RefreshCurrentIndex()
        {
            if (_playingItem != null)
            {
                var newIndex = _playlistManager.Items.IndexOf(_playingItem);
                if (newIndex >= 0)
                    _currentIndex = newIndex;
            }
        }
    }
}
