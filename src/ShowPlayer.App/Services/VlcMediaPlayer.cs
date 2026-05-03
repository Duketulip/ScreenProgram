using LibVLCSharp.Shared;
using ShowPlayer.App.Models;

namespace ShowPlayer.App.Services
{
    public class VlcMediaPlayer : IDisposable
    {
        private readonly LibVLC _libVlc;
        private readonly MediaPlayer _videoPlayer;
        private MediaPlayer? _musicPlayer;
        private System.Timers.Timer? _fadeTimer;
        private bool _disposed;
        private int _musicVolume = 80;
        private string? _currentMusicPath;
        private bool _currentMusicLoop;
        private int _musicGeneration;

        public MediaPlayer VideoPlayer => _videoPlayer;

        public event EventHandler? MediaEnded;
        public event EventHandler? MusicPlaybackCompleted;

        public VlcMediaPlayer()
        {
            var options = new[]
            {
                "--no-video-title-show",
                "--quiet"
            };

            _libVlc = new LibVLC(options);
            _videoPlayer = new MediaPlayer(_libVlc);
            _videoPlayer.EndReached += OnVideoEndReached;
        }

        public void PlayVideo(string filePath, bool loop)
        {
            StopVideo();

            var media = new Media(_libVlc, filePath, FromType.FromPath);
            if (loop)
            {
                media.AddOption("--input-repeat=-1");
            }
            _videoPlayer.Play(media);
            media.Dispose();
        }

        public void PauseVideo()
        {
            if (_videoPlayer.IsPlaying)
                _videoPlayer.Pause();
        }

        public void ResumeVideo()
        {
            if (!_videoPlayer.IsPlaying)
                _videoPlayer.Play();
        }

        public void StopVideo()
        {
            _videoPlayer.Stop();
        }

        public void SeekVideo(long timeMs)
        {
            _videoPlayer.Time = timeMs;
        }

        public long GetVideoTime() => _videoPlayer.Time;

        public long GetVideoLength() => _videoPlayer.Length;

        public bool IsVideoPlaying => _videoPlayer.IsPlaying;

        public int GetVideoVolume() => _videoPlayer.Volume;

        public void SetVideoVolume(int volume)
        {
            _videoPlayer.Volume = Math.Clamp(volume, 0, 100);
        }

        public int GetMusicVolume() => _musicVolume;

        public void SetMusicVolume(int volume)
        {
            _musicVolume = Math.Clamp(volume, 0, 100);
            if (_musicPlayer != null)
                _musicPlayer.Volume = _musicVolume;
        }

        public bool IsMusicPlaying => _musicPlayer?.IsPlaying ?? false;

        public void PauseMusic()
        {
            if (_musicPlayer != null && _musicPlayer.IsPlaying)
                _musicPlayer.Pause();
        }

        public void ResumeMusic()
        {
            if (_musicPlayer != null && !_musicPlayer.IsPlaying)
                _musicPlayer.Play();
        }

        public void PlayMusic(string filePath, bool loop, Action? onReady = null)
        {
            StopMusic();

            _currentMusicPath = filePath;
            _currentMusicLoop = loop;
            Interlocked.Increment(ref _musicGeneration);

            _musicPlayer = new MediaPlayer(_libVlc);
            _musicPlayer.EndReached += OnMusicEndReached;

            var media = new Media(_libVlc, filePath, FromType.FromPath);
            _musicPlayer.Play(media);
            media.Dispose();

            if (onReady != null)
            {
                _musicPlayer.Playing += (s, e) => onReady();
            }

            StartFadeIn();
        }

        public void StopMusic()
        {
            if (_musicPlayer == null) return;

            if (_musicPlayer.IsPlaying)
            {
                StartFadeOut(() =>
                {
                    _musicPlayer?.Stop();
                    CleanupMusicPlayer();
                });
            }
            else
            {
                _musicPlayer.Stop();
                CleanupMusicPlayer();
            }
        }

        public void StopMusicImmediate()
        {
            Interlocked.Increment(ref _musicGeneration);
            _currentMusicPath = null;
            _currentMusicLoop = false;

            _fadeTimer?.Stop();
            _fadeTimer?.Dispose();
            _fadeTimer = null;

            _musicPlayer?.Stop();
            CleanupMusicPlayer();
        }

        private void StartFadeIn()
        {
            if (_musicPlayer == null) return;

            _fadeTimer?.Stop();
            _fadeTimer?.Dispose();

            _musicPlayer.Volume = 0;
            var targetVolume = _musicVolume;
            var currentVolume = 0.0;
            var steps = 30;
            var intervalMs = 100;
            var increment = targetVolume / (double)steps;

            _fadeTimer = new System.Timers.Timer(intervalMs);
            _fadeTimer.Elapsed += (s, e) =>
            {
                try
                {
                    if (_musicPlayer == null)
                    {
                        _fadeTimer?.Stop();
                        return;
                    }

                    currentVolume += increment;
                    if (currentVolume >= targetVolume)
                    {
                        currentVolume = targetVolume;
                        _fadeTimer.Stop();
                    }
                    _musicPlayer.Volume = (int)Math.Round(currentVolume);
                }
                catch
                {
                    _fadeTimer?.Stop();
                }
            };
            _fadeTimer.Start();
        }

        private void StartFadeOut(Action? onComplete = null)
        {
            if (_musicPlayer == null)
            {
                onComplete?.Invoke();
                return;
            }

            _fadeTimer?.Stop();
            _fadeTimer?.Dispose();

            var currentVolume = (double)_musicPlayer.Volume;
            var steps = 30;
            var intervalMs = 100;
            var decrement = currentVolume / (double)steps;

            _fadeTimer = new System.Timers.Timer(intervalMs);
            _fadeTimer.Elapsed += (s, e) =>
            {
                try
                {
                    if (_musicPlayer == null)
                    {
                        _fadeTimer?.Stop();
                        onComplete?.Invoke();
                        return;
                    }

                    currentVolume -= decrement;
                    if (currentVolume <= 0)
                    {
                        currentVolume = 0;
                        _fadeTimer.Stop();
                        onComplete?.Invoke();
                    }
                    _musicPlayer.Volume = (int)Math.Round(currentVolume);
                }
                catch
                {
                    _fadeTimer?.Stop();
                    onComplete?.Invoke();
                }
            };
            _fadeTimer.Start();
        }

        private void CleanupMusicPlayer()
        {
            if (_musicPlayer != null)
            {
                _musicPlayer.EndReached -= OnMusicEndReached;
                _musicPlayer.Dispose();
                _musicPlayer = null;
            }
        }

        private void OnVideoEndReached(object? sender, EventArgs e)
        {
            MediaEnded?.Invoke(this, EventArgs.Empty);
        }

        private void OnMusicEndReached(object? sender, EventArgs e)
        {
            var path = _currentMusicPath;
            var loop = _currentMusicLoop;
            var generation = _musicGeneration;

            if (loop && !string.IsNullOrEmpty(path))
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(50).ConfigureAwait(false);
                        if (_disposed || generation != _musicGeneration) return;

                        StopMusicImmediate();
                        if (_disposed || generation != _musicGeneration - 1) return;
                        PlayMusic(path, true);
                    }
                    catch { }
                });
            }
            else
            {
                MusicPlaybackCompleted?.Invoke(this, EventArgs.Empty);
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _fadeTimer?.Stop();
            _fadeTimer?.Dispose();

            _videoPlayer.EndReached -= OnVideoEndReached;
            _videoPlayer.Stop();
            _videoPlayer.Dispose();

            if (_musicPlayer != null)
            {
                _musicPlayer.EndReached -= OnMusicEndReached;
                _musicPlayer.Stop();
                _musicPlayer.Dispose();
            }

            _libVlc.Dispose();
        }
    }
}
