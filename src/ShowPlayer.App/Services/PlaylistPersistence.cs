using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using ShowPlayer.App.Models;

namespace ShowPlayer.App.Services
{
    public class PlaylistPersistence
    {
        private static readonly string SaveDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ShowPlayer");

        private static readonly string SavePath = Path.Combine(SaveDir, "playlist.json");
        private static readonly string SettingsPath = Path.Combine(SaveDir, "settings.json");

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };

        public void Save(List<PlaylistItem> items)
        {
            try
            {
                if (!Directory.Exists(SaveDir))
                    Directory.CreateDirectory(SaveDir);

                var dto = new PlaylistDto
                {
                    Items = items.Select(i => new PlaylistItemDto
                    {
                        Id = i.Id,
                        Type = i.Type,
                        FilePath = i.FilePath,
                        MusicPath = i.MusicPath,
                        IsMusicLoop = i.IsMusicLoop,
                        IsVideoLoop = i.IsVideoLoop,
                        IsMissing = i.IsMissing
                    }).ToList()
                };

                var json = JsonSerializer.Serialize(dto, JsonOptions);
                File.WriteAllText(SavePath, json);
                Logger.Info($"播放列表已保存，共 {items.Count} 项");
            }
            catch (Exception ex)
            {
                Logger.Error("保存播放列表失败", ex);
            }
        }

        public List<PlaylistItem> Load()
        {
            try
            {
                if (!File.Exists(SavePath))
                {
                    Logger.Info("未找到保存的播放列表文件，返回空列表");
                    return [];
                }

                var json = File.ReadAllText(SavePath);
                var dto = JsonSerializer.Deserialize<PlaylistDto>(json, JsonOptions);
                if (dto?.Items == null || dto.Items.Count == 0)
                {
                    Logger.Info("播放列表文件为空，返回空列表");
                    return [];
                }

                var items = dto.Items.Select(i => new PlaylistItem
                {
                    Id = i.Id,
                    Type = i.Type,
                    FilePath = i.FilePath,
                    MusicPath = i.MusicPath,
                    IsMusicLoop = i.IsMusicLoop,
                    IsVideoLoop = i.IsVideoLoop,
                    IsMissing = i.IsMissing
                }).ToList();

                Logger.Info($"已加载播放列表，共 {items.Count} 项");
                return items;
            }
            catch (Exception ex)
            {
                Logger.Error("加载播放列表失败", ex);
                return [];
            }
        }

        public void SaveSettings(AppSettings settings)
        {
            try
            {
                if (!Directory.Exists(SaveDir))
                    Directory.CreateDirectory(SaveDir);

                var dto = new SettingsDto
                {
                    TransitionType = settings.TransitionType,
                    TransitionDurationMs = settings.TransitionDurationMs,
                    FallbackImagePath = settings.FallbackImagePath,
                    FallbackVideoPath = settings.FallbackVideoPath,
                    IsDarkTheme = settings.IsDarkTheme,
                    VideoVolume = settings.VideoVolume,
                    MusicVolume = settings.MusicVolume,
                    DefaultIdleImagePath = settings.DefaultIdleImagePath
                };

                var json = JsonSerializer.Serialize(dto, JsonOptions);
                File.WriteAllText(SettingsPath, json);
                Logger.Info("设置已保存");
            }
            catch (Exception ex)
            {
                Logger.Error("保存设置失败", ex);
            }
        }

        public AppSettings LoadSettings()
        {
            try
            {
                if (!File.Exists(SettingsPath))
                {
                    Logger.Info("未找到设置文件，返回默认设置");
                    return new AppSettings();
                }

                var json = File.ReadAllText(SettingsPath);
                json = json.Replace("\"Crossfade\"", "\"Direct\"");
                var dto = JsonSerializer.Deserialize<SettingsDto>(json, JsonOptions);
                if (dto == null) return new AppSettings();

                return new AppSettings
                {
                    TransitionType = dto.TransitionType,
                    TransitionDurationMs = dto.TransitionDurationMs,
                    FallbackImagePath = dto.FallbackImagePath,
                    FallbackVideoPath = dto.FallbackVideoPath,
                    IsDarkTheme = dto.IsDarkTheme,
                    VideoVolume = dto.VideoVolume,
                    MusicVolume = dto.MusicVolume,
                    DefaultIdleImagePath = dto.DefaultIdleImagePath
                };
            }
            catch (Exception ex)
            {
                Logger.Error("加载设置失败", ex);
                return new AppSettings();
            }
        }

        private class PlaylistDto
        {
            public List<PlaylistItemDto> Items { get; set; } = [];
        }

        private class PlaylistItemDto
        {
            public string Id { get; set; } = string.Empty;
            public MediaType Type { get; set; }
            public string FilePath { get; set; } = string.Empty;
            public string? MusicPath { get; set; }
            public bool IsMusicLoop { get; set; }
            public bool IsVideoLoop { get; set; }
            public bool IsMissing { get; set; }
        }

        private class SettingsDto
        {
            public TransitionType TransitionType { get; set; }
            public int TransitionDurationMs { get; set; } = 500;
            public string? FallbackImagePath { get; set; }
            public string? FallbackVideoPath { get; set; }
            public bool IsDarkTheme { get; set; }
            public int VideoVolume { get; set; } = 100;
            public int MusicVolume { get; set; } = 100;
            public string? DefaultIdleImagePath { get; set; }
        }
    }
}
