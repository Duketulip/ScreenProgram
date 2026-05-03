using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using ShowPlayer.App.Models;
using ShowPlayer.App.Services;

namespace ShowPlayer.App
{
    public partial class ShowWindow : Window
    {
        private bool _isFullscreen;
        private double _previousLeft;
        private double _previousTop;
        private double _previousWidth;
        private double _previousHeight;

        private VlcMediaPlayer? _mediaPlayer;
        private TransitionType _transitionType = TransitionType.Fade;
        private int _transitionDurationMs = 500;
        private bool _isTransitioning;
        private PlaylistItem? _pendingItem;
        private int _transitionVersion;
        private bool _forceClose;

        public event Action<PlaylistItem>? ContentLoaded;

        public ShowWindow()
        {
            InitializeComponent();
            PreviewMouseMove += OnPreviewMouseMove;
            PreviewKeyDown += (s, e) =>
            {
                if (e.Key == Key.Escape && _isFullscreen)
                    ExitFullscreen();
            };
        }

        public void SetMediaPlayer(VlcMediaPlayer mediaPlayer)
        {
            _mediaPlayer = mediaPlayer;
            ContentVideo.MediaPlayer = mediaPlayer.VideoPlayer;
        }

        public void SetTransitionConfig(TransitionType type, int durationMs)
        {
            _transitionType = type;
            _transitionDurationMs = Math.Clamp(durationMs, 100, 5000);
        }

        public void TransitionTo(PlaylistItem item)
        {
            if (_isTransitioning)
            {
                _pendingItem = item;
                return;
            }
            _isTransitioning = true;
            _pendingItem = null;
            var version = ++_transitionVersion;
            TitleFileName.Text = item.DisplayName;

            var duration = TimeSpan.FromMilliseconds(_transitionDurationMs);
            var fadeToBlack = new DoubleAnimation(0, 1, duration);

            fadeToBlack.Completed += (s, e) =>
            {
                if (version != _transitionVersion) return;

                ClearContent();
                LoadContent(item);
                ContentLoaded?.Invoke(item);

                var fadeFromBlack = new DoubleAnimation(1, 0, duration);
                fadeFromBlack.Completed += (s2, e2) =>
                {
                    if (version != _transitionVersion) return;

                    FadeOverlay.Opacity = 0;
                    _isTransitioning = false;
                    ProcessPendingTransition();
                };
                FadeOverlay.BeginAnimation(OpacityProperty, fadeFromBlack);
            };

            if (_transitionType == TransitionType.Direct)
            {
                FadeOverlay.Opacity = 0;
                ClearContent();
                LoadContent(item);
                ContentLoaded?.Invoke(item);
                _isTransitioning = false;
                ProcessPendingTransition();
            }
            else
            {
                FadeOverlay.BeginAnimation(OpacityProperty, fadeToBlack);
            }
        }

        private void ProcessPendingTransition()
        {
            if (_pendingItem == null) return;

            var next = _pendingItem;
            _pendingItem = null;
            TransitionTo(next);
        }

        private void LoadContent(PlaylistItem item)
        {
            if (item.Type == MediaType.Image)
            {
                ContentVideo.Visibility = Visibility.Collapsed;
                ContentImage.Visibility = Visibility.Visible;

                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.UriSource = new Uri(item.FilePath);
                    bitmap.EndInit();
                    ContentImage.Source = bitmap;
                }
                catch
                {
                    ContentImage.Source = null;
                }
            }
            else
            {
                ContentImage.Visibility = Visibility.Collapsed;
                ContentVideo.Visibility = Visibility.Visible;
                _mediaPlayer?.PlayVideo(item.FilePath, item.IsVideoLoop);
            }
        }

        public void Stop()
        {
            CancelTransition();
            ClearContent();
            FadeOverlay.Opacity = 0;
            TitleFileName.Text = "";
        }

        public void ShowDefaultImage(string? imagePath)
        {
            CancelTransition();
            ClearContent();
            if (string.IsNullOrEmpty(imagePath))
            {
                Logger.Info("未设置默认闲置图片，展示黑屏");
                return;
            }

            if (!System.IO.File.Exists(imagePath))
            {
                Logger.Warning($"默认闲置图片文件不存在: {imagePath}");
                return;
            }

            ContentVideo.Visibility = Visibility.Collapsed;
            ContentImage.Visibility = Visibility.Visible;
            TitleFileName.Text = "默认展示";
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(imagePath);
                bitmap.EndInit();
                ContentImage.Source = bitmap;
                Logger.Info($"已加载默认闲置图片: {imagePath}");
            }
            catch (Exception ex)
            {
                Logger.Error($"加载默认闲置图片失败: {imagePath}", ex);
                ContentImage.Source = null;
            }
        }

        private void ClearContent()
        {
            ContentImage.Source = null;
            ContentImage.Visibility = Visibility.Collapsed;
            _mediaPlayer?.StopVideo();
            ContentVideo.Visibility = Visibility.Collapsed;
        }

        private void CancelTransition()
        {
            _transitionVersion++;
            _pendingItem = null;
            _isTransitioning = false;
            FadeOverlay.BeginAnimation(OpacityProperty, null);
            FadeOverlay.Opacity = 0;
        }

        private void OnContentDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ToggleFullscreen();
        }

        private void OnTitleBarMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && !_isFullscreen)
                DragMove();
        }

        private void OnToggleFullscreen(object sender, RoutedEventArgs e)
        {
            ToggleFullscreen();
        }

        private void OnToggleTopmost(object sender, RoutedEventArgs e)
        {
            Topmost = !Topmost;
            PinBtn.Foreground = Topmost ? System.Windows.Media.Brushes.Gold : System.Windows.Media.Brushes.Gray;
        }

        private void OnMinimize(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (_forceClose) return;

            if (Application.Current.MainWindow == this || Application.Current.MainWindow?.IsVisible == false)
                return;
            e.Cancel = true;
            WindowState = WindowState.Minimized;
        }

        public void ForceClose()
        {
            _forceClose = true;
            Close();
        }

        private void OnPreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_isFullscreen)
            {
                var pos = e.GetPosition(this);
                TitleBar.Visibility = pos.Y < 40 ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public void ToggleFullscreen()
        {
            if (_isFullscreen) ExitFullscreen();
            else EnterFullscreen();
        }

        private static System.Windows.Rect GetCurrentScreenBounds(Window window)
        {
            var hwnd = new System.Windows.Interop.WindowInteropHelper(window).Handle;
            var monitor = NativeMethods.MonitorFromWindow(hwnd, 2); // MONITOR_DEFAULTTONEAREST
            var info = new NativeMethods.MONITORINFO();
            info.cbSize = System.Runtime.InteropServices.Marshal.SizeOf(info);
            if (NativeMethods.GetMonitorInfo(monitor, ref info))
            {
                return new System.Windows.Rect(
                    info.rcMonitor.Left, info.rcMonitor.Top,
                    info.rcMonitor.Right - info.rcMonitor.Left,
                    info.rcMonitor.Bottom - info.rcMonitor.Top);
            }
            return new System.Windows.Rect(0, 0,
                SystemParameters.PrimaryScreenWidth, SystemParameters.PrimaryScreenHeight);
        }

        private void EnterFullscreen()
        {
            _isFullscreen = true;
            _previousLeft = Left;
            _previousTop = Top;
            _previousWidth = Width;
            _previousHeight = Height;

            var bounds = GetCurrentScreenBounds(this);

            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            Topmost = true;
            Left = bounds.Left;
            Top = bounds.Top;
            Width = bounds.Width;
            Height = bounds.Height;
            ContentBorder.Margin = new Thickness(0);
            TitleBar.Visibility = Visibility.Collapsed;
            FullscreenHint.Visibility = Visibility.Visible;
            ShowInTaskbar = false;

            var timer = new System.Timers.Timer(3000) { AutoReset = false };
            timer.Elapsed += (s, args) =>
            {
                Dispatcher.Invoke(() => FullscreenHint.Visibility = Visibility.Collapsed);
                timer.Dispose();
            };
            timer.Start();
        }

        private void ExitFullscreen()
        {
            _isFullscreen = false;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.CanResizeWithGrip;
            Left = _previousLeft; Top = _previousTop;
            Width = _previousWidth; Height = _previousHeight;
            ContentBorder.Margin = new Thickness(0, 32, 0, 0);
            TitleBar.Visibility = Visibility.Visible;
            FullscreenHint.Visibility = Visibility.Collapsed;
            ShowInTaskbar = true;
        }

        public void EnsureVisible()
        {
            if (!IsVisible)
                Show();
            if (WindowState == WindowState.Minimized)
                WindowState = WindowState.Normal;
            Activate();
        }

        public bool IsShowWindowVisible => IsVisible && WindowState != WindowState.Minimized;
    }

    internal static class NativeMethods
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        internal static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        internal static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        internal const int CCHDEVICENAME = 32;

        internal struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        internal struct MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }
    }
}
