using System.Windows;
using Microsoft.Win32;
using ShowPlayer.App.Models;

namespace ShowPlayer.App.Views
{
    public partial class EditItemDialog : Window
    {
        private readonly PlaylistItem _item;

        public EditItemDialog(PlaylistItem item)
        {
            InitializeComponent();
            _item = item;
            LoadData();
        }

        private void LoadData()
        {
            FilePathBox.Text = _item.FilePath;
            TypeBox.Text = _item.Type.ToString();
            MusicPathBox.Text = _item.MusicPath ?? string.Empty;
            MusicLoopCheck.IsChecked = _item.IsMusicLoop;
            VideoLoopCheck.IsChecked = _item.IsVideoLoop;
            ClearMusicCheck.Visibility = string.IsNullOrEmpty(_item.MusicPath) ? Visibility.Collapsed : Visibility.Visible;
        }

        private void OnSelectMusic(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "音频文件|*.mp3;*.wav;*.wma;*.aac;*.flac|所有文件|*.*",
                Title = "选择背景音乐"
            };

            if (dialog.ShowDialog() == true)
            {
                MusicPathBox.Text = dialog.FileName;
                ClearMusicCheck.IsChecked = false;
            }
        }

        private void OnOk(object sender, RoutedEventArgs e)
        {
            if (ClearMusicCheck.IsChecked == true)
            {
                _item.MusicPath = null;
            }
            else if (!string.IsNullOrEmpty(MusicPathBox.Text))
            {
                _item.MusicPath = MusicPathBox.Text;
            }

            _item.IsMusicLoop = MusicLoopCheck.IsChecked == true;
            _item.IsVideoLoop = VideoLoopCheck.IsChecked == true;

            DialogResult = true;
            Close();
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
