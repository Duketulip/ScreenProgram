using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using ShowPlayer.App.ViewModels;

namespace ShowPlayer.App
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;
        private readonly DispatcherTimer _clockTimer;
        private Point _dragStartPoint;
        private bool _isDragging;

        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;

            _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _clockTimer.Tick += (s, e) => TimeText.Text = DateTime.Now.ToString("HH:mm:ss");
            _clockTimer.Start();

            PreviewKeyDown += OnPreviewKeyDown;
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            var ctrl = (Keyboard.Modifiers & ModifierKeys.Control) != 0;
            if (_viewModel.HandleKeyDown(e.Key, ctrl))
                e.Handled = true;
        }

        private void OnSliderPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            _viewModel.IsDraggingSlider = true;
        }

        private void OnSliderPreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is Slider slider)
                _viewModel.SeekVideoCommand.Execute(slider.Value);
            _viewModel.IsDraggingSlider = false;
        }

        private void OnSliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_viewModel.IsDraggingSlider && sender is Slider slider)
            {
                var len = _viewModel.PlaybackController.GetVideoLength();
                if (len > 0)
                {
                    _viewModel.VideoTimeText = FormatTime((long)(slider.Value * len));
                }
            }
        }

        private void OnListBoxPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(null);
            _isDragging = false;

            if (sender is ListBox listBox && e.OriginalSource is DependencyObject source)
            {
                var listBoxItem = FindAncestor<ListBoxItem>(source);
                if (listBoxItem != null)
                {
                    listBox.SelectedItem = listBoxItem.DataContext;
                }
            }
        }

        private static T? FindAncestor<T>(DependencyObject? child) where T : DependencyObject
        {
            while (child != null)
            {
                if (child is T t) return t;
                child = System.Windows.Media.VisualTreeHelper.GetParent(child);
            }
            return null;
        }

        private void OnListBoxPreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed || _isDragging) return;

            var pos = e.GetPosition(null);
            var diff = _dragStartPoint - pos;

            if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
            {
                if (sender is ListBox listBox)
                {
                    var item = listBox.SelectedItem;
                    if (item != null)
                    {
                        _isDragging = true;
                        DragDrop.DoDragDrop(listBox, item, DragDropEffects.Move);
                    }
                }
            }
        }

        private void OnListBoxDrop(object sender, DragEventArgs e)
        {
            if (sender is ListBox listBox && e.Data.GetDataPresent(typeof(ShowPlayer.App.Models.PlaylistItem)))
            {
                var droppedItem = (ShowPlayer.App.Models.PlaylistItem)e.Data.GetData(typeof(ShowPlayer.App.Models.PlaylistItem));
                var dropIndex = GetDropIndex(listBox, e.GetPosition(listBox));

                var sourceIndex = _viewModel.Items.IndexOf(droppedItem);
                if (sourceIndex >= 0 && dropIndex >= 0 && sourceIndex != dropIndex)
                {
                    _viewModel.OnItemDragDrop(sourceIndex, dropIndex);
                }
            }
        }

        private static int GetDropIndex(ListBox listBox, Point dropPosition)
        {
            for (int i = 0; i < listBox.Items.Count; i++)
            {
                var item = listBox.ItemContainerGenerator.ContainerFromIndex(i) as ListBoxItem;
                if (item != null)
                {
                    var bounds = item.TransformToAncestor(listBox).TransformBounds(new Rect(0, 0, item.ActualWidth, item.ActualHeight));
                    var midY = bounds.Top + bounds.Height / 2;
                    if (dropPosition.Y < midY)
                        return i;
                }
            }
            return listBox.Items.Count - 1;
        }

        private static string FormatTime(long ms)
        {
            var ts = TimeSpan.FromMilliseconds(ms);
            return ts.Hours > 0
                ? $"{ts.Hours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}"
                : $"{ts.Minutes:D2}:{ts.Seconds:D2}";
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            _clockTimer.Stop();
            _viewModel.SaveAll();
            base.OnClosing(e);
        }
    }
}
