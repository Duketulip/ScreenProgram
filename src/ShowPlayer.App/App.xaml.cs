using System.Windows;
using ShowPlayer.App.Models;
using ShowPlayer.App.Services;
using ShowPlayer.App.ViewModels;

namespace ShowPlayer.App
{
    public partial class App : Application
    {
        private AppSettings _appSettings = new();
        private ShowWindow _showWindow = null!;
        private VlcMediaPlayer _mediaPlayer = null!;
        private PlaybackController _playbackController = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            ShutdownMode = ShutdownMode.OnMainWindowClose;

            var persistence = new PlaylistPersistence();
            _appSettings = persistence.LoadSettings();

            ThemeManager.Initialize(_appSettings.IsDarkTheme);

            var fileValidator = new FileValidator();
            var playlistManager = new PlaylistManager(persistence, fileValidator);

            _mediaPlayer = new VlcMediaPlayer();
            _mediaPlayer.SetVideoVolume(_appSettings.VideoVolume);

            _playbackController = new PlaybackController(playlistManager, _mediaPlayer);

            _showWindow = new ShowWindow();
            _showWindow.SetMediaPlayer(_mediaPlayer);
            _showWindow.SetTransitionConfig(_appSettings.TransitionType, _appSettings.TransitionDurationMs);
            _showWindow.Show();

            var viewModel = new MainViewModel(
                playlistManager, _playbackController, fileValidator, persistence, _appSettings, _showWindow);

            _playbackController.ItemStarted += (s, item) =>
            {
                _showWindow.Dispatcher.Invoke(() =>
                {
                    _showWindow.EnsureVisible();
                    _showWindow.SetTransitionConfig(_appSettings.TransitionType, _appSettings.TransitionDurationMs);
                    _showWindow.TransitionTo(item);
                });
            };

            var mainWindow = new MainWindow(viewModel);
            MainWindow = mainWindow;
            mainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _playbackController.Stop();
            _mediaPlayer.Dispose();
            _showWindow.Close();
            base.OnExit(e);
        }
    }
}
