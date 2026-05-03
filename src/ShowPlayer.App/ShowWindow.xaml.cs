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
            TitleFileName.Text = item.DisplayName;

            if (_isTransitioning) return;
            _isTransitioning = true;

            var duration = TimeSpan.FromMilliseconds(_transitionDurationMs);
            var fadeToBlack = new DoubleAnimation(0, 1, duration);

            fadeToBlack.Completed += (s, e) =>
            {
                ClearContent();
                LoadContent(item);

                var fadeFromBlack = new DoubleAnimation(1, 0, duration);
                fadeFromBlack.Completed += (s2, e2) =>
                {
                    FadeOverlay.Opacity = 0;
                    _isTransitioning = false;
                };
                FadeOverlay.BeginAnimation(OpacityProperty, fadeFromBlack);
            };

            if (_transitionType == TransitionType.Crossfade)
            {
                FadeOverlay.Opacity = 0;
                ClearContent();
                LoadContent(item);
                _isTransitioning = false;
            }
            else
            {
                FadeOverlay.BeginAnimation(OpacityProperty, fadeToBlack);
            }
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
            ClearContent();
            FadeOverlay.Opacity = 0;
            _isTransitioning = false;
            TitleFileName.Text = "";
        }

        public void ShowDefaultImage(string? imagePath)
        {
            ClearContent();
            if (string.IsNullOrEmpty(imagePath)) return;

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
            }
            catch
            {
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

        private void OnCloseWindow(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
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

        private void EnterFullscreen()
        {
            _isFullscreen = true;
            _previousLeft = Left;
            _previousTop = Top;
            _previousWidth = Width;
            _previousHeight = Height;

            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            Topmost = true;
            Left = 0; Top = 0;
            Width = SystemParameters.PrimaryScreenWidth;
            Height = SystemParameters.PrimaryScreenHeight;
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
            ShowInTaskbar = false;
        }

        public void EnsureVisible()
        {
            if (WindowState == WindowState.Minimized)
                WindowState = WindowState.Normal;
            if (!IsVisible)
                Show();
            Activate();
        }

        public bool IsShowWindowVisible => IsVisible && WindowState != WindowState.Minimized;
    }
}
