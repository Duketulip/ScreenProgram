using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Win32;
using ShowPlayer.App.Models;
using ShowPlayer.App.Services;

namespace ShowPlayer.App.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly PlaylistManager _playlistManager;
        private readonly PlaybackController _playbackController;
        private readonly PlaylistPersistence _persistence;
        private readonly AppSettings _appSettings;
        private readonly ShowWindow _showWindow;
        private readonly DispatcherTimer _positionTimer;

        private PlaylistItem? _currentItem;
        private PlaylistItem? _nowPlayingItem;
        private string _statusText = "就绪";
        private bool _isPlaying;
        private double _videoProgress;
        private string _videoTimeText = "--:--";
        private string _videoDurationText = "--:--";
        private bool _isDraggingSlider;

        public ReadOnlyObservableCollection<PlaylistItem> Items => _playlistManager.Items;
        public PlaybackController PlaybackController => _playbackController;
        public AppSettings Settings => _appSettings;

        public PlaylistItem? CurrentItem
        {
            get => _currentItem;
            set
            {
                _currentItem = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsSelectedItemVideo));
                OnPropertyChanged(nameof(IsSelectedItemImage));
                OnPropertyChanged(nameof(SelectedMusicPath));
                OnPropertyChanged(nameof(SelectedHasMusic));
                OnPropertyChanged(nameof(SelectedVideoLoop));
                OnPropertyChanged(nameof(SelectedMusicLoop));
                OnPropertyChanged(nameof(SelectedMusicFileName));
                OnPropertyChanged(nameof(SelectedItemDisplayName));
                OnPropertyChanged(nameof(ShowSelectedPlayButton));
            }
        }

        public PlaylistItem? NowPlayingItem
        {
            get => _nowPlayingItem;
            set
            {
                if (_nowPlayingItem != null && _nowPlayingItem != value)
                    _nowPlayingItem.IsNowPlaying = false;
                _nowPlayingItem = value;
                if (_nowPlayingItem != null)
                    _nowPlayingItem.IsNowPlaying = true;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ShowNowPlayingInfo));
                OnPropertyChanged(nameof(NowPlayingDisplayName));
                OnPropertyChanged(nameof(NowPlayingTypeText));
                OnPropertyChanged(nameof(ShowSelectedPlayButton));
                ResetVideoProgress();
            }
        }

        public bool ShowNowPlayingInfo => NowPlayingItem != null;
        public string? NowPlayingDisplayName => NowPlayingItem?.DisplayName;
        public string? NowPlayingTypeText => NowPlayingItem != null ? $"[{NowPlayingItem.Type}]" : null;

        public bool IsPlaying
        {
            get => _isPlaying;
            set { _isPlaying = value; OnPropertyChanged(); }
        }

        public string StatusText
        {
            get => _statusText;
            set { _statusText = value; OnPropertyChanged(); }
        }

        public bool IsNowPlayingVideo => NowPlayingItem?.Type == MediaType.Video;
        public bool IsNowPlayingImage => NowPlayingItem?.Type == MediaType.Image;

        public bool IsSelectedItemVideo => CurrentItem?.Type == MediaType.Video;
        public bool IsSelectedItemImage => CurrentItem?.Type == MediaType.Image;
        public bool ShowSelectedPlayButton => CurrentItem != null && CurrentItem != NowPlayingItem;
        public string PlayPauseButtonText
        {
            get
            {
                if (NowPlayingItem == null) return "▶ 播放";
                if (IsNowPlayingVideo && _playbackController.IsVideoPlaying) return "⏸ 暂停";
                if (IsNowPlayingImage && _playbackController.IsMusicPlaying) return "⏸ 暂停";
                return "▶ 播放";
            }
        }

        public string? SelectedItemDisplayName => CurrentItem?.DisplayName;

        public string? SelectedMusicPath => CurrentItem?.MusicPath;
        public bool SelectedHasMusic => !string.IsNullOrEmpty(SelectedMusicPath);
        public string? SelectedMusicFileName => SelectedHasMusic ? Path.GetFileName(SelectedMusicPath) : null;

        public bool SelectedVideoLoop
        {
            get => CurrentItem?.IsVideoLoop ?? false;
            set { if (CurrentItem != null) { CurrentItem.IsVideoLoop = value; OnPropertyChanged(); } }
        }

        public bool SelectedMusicLoop
        {
            get => CurrentItem?.IsMusicLoop ?? false;
            set { if (CurrentItem != null) { CurrentItem.IsMusicLoop = value; OnPropertyChanged(); } }
        }

        public double VideoProgress
        {
            get => _videoProgress;
            set { _videoProgress = value; OnPropertyChanged(); }
        }

        public string VideoTimeText
        {
            get => _videoTimeText;
            set { _videoTimeText = value; OnPropertyChanged(); }
        }

        public string VideoDurationText
        {
            get => _videoDurationText;
            set { _videoDurationText = value; OnPropertyChanged(); }
        }

        public bool IsDraggingSlider
        {
            get => _isDraggingSlider;
            set { _isDraggingSlider = value; OnPropertyChanged(); }
        }

        public int VideoVolume
        {
            get => _appSettings.VideoVolume;
            set
            {
                _appSettings.VideoVolume = value;
                _mediaPlayer.SetVideoVolume(value);
                OnPropertyChanged();
            }
        }

        public int MusicVolume
        {
            get => _appSettings.MusicVolume;
            set
            {
                _appSettings.MusicVolume = value;
                _mediaPlayer.SetMusicVolume(value);
                OnPropertyChanged();
            }
        }

        public int TransitionDurationMs
        {
            get => _appSettings.TransitionDurationMs;
            set
            {
                _appSettings.TransitionDurationMs = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TransitionDurationText));
            }
        }

        public string TransitionDurationText => $"{_appSettings.TransitionDurationMs}ms";

        public bool UseCrossfade
        {
            get => _appSettings.TransitionType == TransitionType.Crossfade;
            set
            {
                _appSettings.TransitionType = value ? TransitionType.Crossfade : TransitionType.Fade;
                OnPropertyChanged();
            }
        }

        public string? DefaultIdleImagePath
        {
            get => _appSettings.DefaultIdleImagePath;
            set
            {
                _appSettings.DefaultIdleImagePath = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasDefaultIdleImage));
                OnPropertyChanged(nameof(DefaultIdleImageDisplayName));
            }
        }

        public bool HasDefaultIdleImage => !string.IsNullOrEmpty(DefaultIdleImagePath);
        public string? DefaultIdleImageDisplayName => HasDefaultIdleImage ? System.IO.Path.GetFileName(DefaultIdleImagePath) : null;

        public bool IsShowWindowVisible
        {
            get => _showWindow.IsShowWindowVisible;
            set
            {
                if (value)
                    _showWindow.EnsureVisible();
                else
                    _showWindow.WindowState = WindowState.Minimized;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ShowWindowToggleText));
            }
        }

        public string ShowWindowToggleText => IsShowWindowVisible ? "隐藏展示窗口" : "显示展示窗口";

        private VlcMediaPlayer _mediaPlayer;

        public ICommand AddFilesCommand { get; }
        public ICommand RemoveItemCommand { get; }
        public ICommand ClearAllCommand { get; }
        public ICommand PlayItemCommand { get; }
        public ICommand NextCommand { get; }
        public ICommand PreviousCommand { get; }
        public ICommand StopCommand { get; }
        public ICommand PauseResumeCommand { get; }
        public ICommand ToggleVideoLoopCommand { get; }
        public ICommand SelectMusicCommand { get; }
        public ICommand ClearMusicCommand { get; }
        public ICommand ToggleMusicLoopCommand { get; }
        public ICommand ShowShortcutsCommand { get; }
        public ICommand SeekVideoCommand { get; }
        public ICommand MoveItemCommand { get; }
        public ICommand ToggleShowWindowCommand { get; }
        public ICommand SelectIdleImageCommand { get; }

        public MainViewModel(
            PlaylistManager playlistManager,
            PlaybackController playbackController,
            FileValidator fileValidator,
            PlaylistPersistence persistence,
            AppSettings appSettings,
            ShowWindow showWindow)
        {
            _playlistManager = playlistManager;
            _playbackController = playbackController;
            _persistence = persistence;
            _appSettings = appSettings;
            _showWindow = showWindow;
            _mediaPlayer = playbackController.MediaPlayer;

            _playbackController.ItemStarted += OnItemStarted;
            _playbackController.PlaybackStopped += OnPlaybackStopped;
            _playbackController.MediaEnded += OnMediaEnded;

            AddFilesCommand = new RelayCommand(OnAddFiles);
            RemoveItemCommand = new RelayCommand(OnRemoveItem);
            ClearAllCommand = new RelayCommand(_ => { _playbackController.Stop(); _playlistManager.Clear(); ResetVideoProgress(); });
            PlayItemCommand = new RelayCommand(OnPlayItem);
            NextCommand = new RelayCommand(_ => _playbackController.Next());
            PreviousCommand = new RelayCommand(_ => _playbackController.Previous());
            StopCommand = new RelayCommand(_ => _playbackController.Stop());
            PauseResumeCommand = new RelayCommand(OnPauseResume);
            ToggleVideoLoopCommand = new RelayCommand(OnToggleVideoLoop);
            SelectMusicCommand = new RelayCommand(OnSelectMusic);
            ClearMusicCommand = new RelayCommand(OnClearMusic);
            ToggleMusicLoopCommand = new RelayCommand(OnToggleMusicLoop);
            ShowShortcutsCommand = new RelayCommand(_ => ShowShortcuts());
            SeekVideoCommand = new RelayCommand(OnSeekVideo);
            MoveItemCommand = new RelayCommand(OnMoveItem);
            ToggleShowWindowCommand = new RelayCommand(_ => IsShowWindowVisible = !IsShowWindowVisible);
            SelectIdleImageCommand = new RelayCommand(_ => OnSelectIdleImage());

            _positionTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
            _positionTimer.Tick += OnPositionTimerTick;

            _playlistManager.LoadFromPersistence();
        }

        private void OnItemStarted(object? sender, PlaylistItem item)
        {
            NowPlayingItem = item;
            IsPlaying = true;
            StatusText = "▶ 播放中";
            OnPropertyChanged(nameof(PlayPauseButtonText));
            OnPropertyChanged(nameof(ShowSelectedPlayButton));

            if (item.Type == MediaType.Video)
                _positionTimer.Start();
            else
            {
                _positionTimer.Stop();
                ResetVideoProgress();
                _playbackController.PlayMusicForCurrent();
            }
        }

        private void OnPlaybackStopped(object? sender, EventArgs e)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                _positionTimer.Stop();
                ResetVideoProgress();
                NowPlayingItem = null;
                IsPlaying = false;
                StatusText = "已停止";
                OnPropertyChanged(nameof(PlayPauseButtonText));
                _showWindow.ShowDefaultImage(_appSettings.DefaultIdleImagePath);
            });
        }

        private void OnMediaEnded(object? sender, EventArgs e)
        {
            try
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => _playbackController.Next());
            }
            catch { }
        }

        private void OnPositionTimerTick(object? sender, EventArgs e)
        {
            if (_isDraggingSlider) return;
            var pos = _playbackController.GetVideoTime();
            var len = _playbackController.GetVideoLength();
            if (len > 0)
            {
                VideoProgress = Math.Min(1.0, (double)pos / len);
                VideoTimeText = FormatTime(pos);
                VideoDurationText = FormatTime(len);
            }
        }

        private void ResetVideoProgress()
        {
            VideoProgress = 0;
            VideoTimeText = "--:--";
            VideoDurationText = "--:--";
        }

        private void OnAddFiles(object? parameter)
        {
            var dialog = new OpenFileDialog
            {
                Multiselect = true,
                Filter = "支持的文件|*.jpg;*.jpeg;*.png;*.mp4;*.mov;*.avi;*.mkv;*.wmv|图片文件|*.jpg;*.jpeg;*.png|视频文件|*.mp4;*.mov;*.avi;*.mkv;*.wmv|所有文件|*.*"
            };
            if (dialog.ShowDialog() == true)
            {
                foreach (var filePath in dialog.FileNames)
                {
                    var type = IsImageFile(filePath) ? MediaType.Image : MediaType.Video;
                    _playlistManager.Add(new PlaylistItem { Type = type, FilePath = filePath });
                }
                StatusText = $"已添加 {dialog.FileNames.Length} 个文件";
            }
        }

        private void OnRemoveItem(object? parameter)
        {
            if (parameter is PlaylistItem item)
            {
                if (item == NowPlayingItem) _playbackController.Stop();
                _playlistManager.Remove(item);
            }
        }

        private void OnPlayItem(object? parameter)
        {
            if (parameter is PlaylistItem item)
            {
                _playbackController.PlayItem(item);
            }
            else if (parameter is int index)
            {
                _playbackController.PlayItem(index);
            }
        }

        private void OnPauseResume(object? parameter)
        {
            if (NowPlayingItem?.Type == MediaType.Video)
            {
                if (_playbackController.IsVideoPlaying) _playbackController.PauseVideo();
                else _playbackController.ResumeVideo();
                OnPropertyChanged(nameof(PlayPauseButtonText));
            }
            else if (NowPlayingItem?.Type == MediaType.Image && !string.IsNullOrEmpty(NowPlayingItem.MusicPath))
            {
                if (_playbackController.IsMusicPlaying) _playbackController.PauseVideo();
                else _playbackController.ResumeVideo();
                OnPropertyChanged(nameof(PlayPauseButtonText));
            }
        }

        private void OnSeekVideo(object? parameter)
        {
            if (NowPlayingItem?.Type != MediaType.Video || parameter is not double progress) return;
            var len = _playbackController.GetVideoLength();
            if (len > 0) _playbackController.SeekVideo((long)(progress * len));
        }

        private void OnToggleVideoLoop(object? parameter)
        {
            if (CurrentItem == null) return;
            CurrentItem.IsVideoLoop = !CurrentItem.IsVideoLoop;
            OnPropertyChanged(nameof(SelectedVideoLoop));
        }

        private void OnSelectMusic(object? parameter)
        {
            if (CurrentItem == null) return;
            var dialog = new OpenFileDialog
            {
                Filter = "音频文件|*.mp3;*.wav;*.wma;*.aac;*.flac|所有文件|*.*",
                Title = "选择背景音乐"
            };
            if (dialog.ShowDialog() == true)
            {
                CurrentItem.MusicPath = dialog.FileName;
                RefreshSelectedProps();
                StatusText = $"已设置背景音乐: {Path.GetFileName(dialog.FileName)}";
            }
        }

        private void OnClearMusic(object? parameter)
        {
            if (CurrentItem == null) return;
            CurrentItem.MusicPath = null;
            RefreshSelectedProps();
            StatusText = "已清除背景音乐";
        }

        private void OnToggleMusicLoop(object? parameter)
        {
            if (CurrentItem == null) return;
            CurrentItem.IsMusicLoop = !CurrentItem.IsMusicLoop;
            OnPropertyChanged(nameof(SelectedMusicLoop));
        }

        private void RefreshSelectedProps()
        {
            OnPropertyChanged(nameof(SelectedMusicPath));
            OnPropertyChanged(nameof(SelectedHasMusic));
            OnPropertyChanged(nameof(SelectedMusicFileName));
        }

        private void OnMoveItem(object? parameter)
        {
            if (parameter is object[] args && args.Length == 2 &&
                args[0] is int fromIdx && args[1] is int toIdx)
            {
                _playlistManager.Move(fromIdx, toIdx);
                _playbackController.RefreshCurrentIndex();
            }
        }

        private void OnSelectIdleImage()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "图片文件|*.jpg;*.jpeg;*.png",
                Title = "选择默认展示图片"
            };
            if (dialog.ShowDialog() == true)
            {
                DefaultIdleImagePath = dialog.FileName;
                StatusText = $"默认展示图片已设置: {DefaultIdleImageDisplayName}";
            }
        }

        public void OnItemDragDrop(int fromIndex, int toIndex)
        {
            _playlistManager.Move(fromIndex, toIndex);
            _playbackController.RefreshCurrentIndex();
        }

        private static void ShowShortcuts()
        {
            var msg = "全局快捷键（中控窗口聚焦时生效）\n\n"
                    + "  空格键    切到下一个\n"
                    + "  Ctrl+P    暂停/继续 视频\n"
                    + "  ← / →     视频快退/快进 5秒";
            MessageBox.Show(msg, "快捷键说明", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void HandleKeyDown(Key key, bool ctrl)
        {
            if (ctrl && key == Key.P) { OnPauseResume(null); return; }
            switch (key)
            {
                case Key.Space: _playbackController.Next(); break;
                case Key.Left:
                    if (IsNowPlayingVideo)
                        _playbackController.SeekVideo(Math.Max(0, _playbackController.GetVideoTime() - 5000));
                    break;
                case Key.Right:
                    if (IsNowPlayingVideo)
                        _playbackController.SeekVideo(_playbackController.GetVideoTime() + 5000);
                    break;
            }
        }

        private static bool IsImageFile(string path)
        {
            var ext = Path.GetExtension(path).ToLowerInvariant();
            return ext is ".jpg" or ".jpeg" or ".png";
        }

        private static string FormatTime(long ms)
        {
            var ts = TimeSpan.FromMilliseconds(ms);
            return ts.Hours > 0
                ? $"{ts.Hours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}"
                : $"{ts.Minutes:D2}:{ts.Seconds:D2}";
        }

        public void SaveAll()
        {
            _positionTimer.Stop();
            _persistence.SaveSettings(_appSettings);
            _playlistManager.SaveToPersistence();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
