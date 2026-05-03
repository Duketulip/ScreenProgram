using System.IO;
using ShowPlayer.App.Models;

namespace ShowPlayer.App.Services
{
    public class FileValidator
    {
        public void ValidateAll(IEnumerable<PlaylistItem> items)
        {
            foreach (var item in items)
            {
                item.IsMissing = !File.Exists(item.FilePath);
                if (item.IsMissing)
                {
                    Logger.Warning($"文件缺失: {item.FilePath}");
                }

                if (!string.IsNullOrEmpty(item.MusicPath) && !File.Exists(item.MusicPath))
                {
                    Logger.Warning($"背景音乐文件缺失: {item.MusicPath}");
                }
            }
        }

        public bool Validate(PlaylistItem item)
        {
            item.IsMissing = !File.Exists(item.FilePath);
            if (item.IsMissing)
            {
                Logger.Warning($"文件缺失: {item.FilePath}");
            }
            return !item.IsMissing;
        }
    }
}
