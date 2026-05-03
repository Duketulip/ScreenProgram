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

        public int GetMusicVolume() => _musicPlayer?.Volume ?? 0;

        public void SetMusicVolume(int volume)
        {
            if (_musicPlayer != null)
                _musicPlayer.Volume = Math.Clamp(volume, 0, 100);
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
            var targetVolume = 100;
            var currentVolume = 0;
            var steps = 30;
            var intervalMs = 100;

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

                    currentVolume += targetVolume / steps;
                    if (currentVolume >= targetVolume)
                    {
                        currentVolume = targetVolume;
                        _fadeTimer.Stop();
                    }
                    _musicPlayer.Volume = currentVolume;
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

            var currentVolume = _musicPlayer.Volume;
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

                    currentVolume -= (int)decrement;
                    if (currentVolume <= 0)
                    {
                        currentVolume = 0;
                        _fadeTimer.Stop();
                        onComplete?.Invoke();
                    }
                    _musicPlayer.Volume = currentVolume;
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
            MusicPlaybackCompleted?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _fadeTimer?.Stop();
            _fadeTimer?.Dispose();

            StopMusicImmediate();
            _videoPlayer.Stop();
            _videoPlayer.EndReached -= OnVideoEndReached;
            _videoPlayer.Dispose();
            _libVlc.Dispose();
        }
    }
}
