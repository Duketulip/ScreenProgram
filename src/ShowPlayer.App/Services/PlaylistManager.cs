using System.Collections.ObjectModel;
using ShowPlayer.App.Models;

namespace ShowPlayer.App.Services
{
    public class PlaylistManager
    {
        private readonly ObservableCollection<PlaylistItem> _items = [];
        private readonly PlaylistPersistence _persistence;
        private readonly FileValidator _fileValidator;

        public ReadOnlyObservableCollection<PlaylistItem> Items { get; }

        public PlaylistManager(PlaylistPersistence persistence, FileValidator fileValidator)
        {
            _persistence = persistence;
            _fileValidator = fileValidator;
            Items = new ReadOnlyObservableCollection<PlaylistItem>(_items);
        }

        public void LoadFromPersistence()
        {
            var loaded = _persistence.Load();
            _items.Clear();
            foreach (var item in loaded)
            {
                _items.Add(item);
            }
            _fileValidator.ValidateAll(_items);
        }

        public void Add(PlaylistItem item)
        {
            _fileValidator.Validate(item);
            _items.Add(item);
        }

        public bool Remove(PlaylistItem item)
        {
            return _items.Remove(item);
        }

        public void RemoveAt(int index)
        {
            if (index >= 0 && index < _items.Count)
                _items.RemoveAt(index);
        }

        public void Move(int fromIndex, int toIndex)
        {
            if (fromIndex < 0 || fromIndex >= _items.Count) return;
            if (toIndex < 0 || toIndex >= _items.Count) return;
            if (fromIndex == toIndex) return;

            _items.Move(fromIndex, toIndex);
        }

        public void MoveUp(int index)
        {
            if (index > 0)
                Move(index, index - 1);
        }

        public void MoveDown(int index)
        {
            if (index < _items.Count - 1)
                Move(index, index + 1);
        }

        public void Clear()
        {
            _items.Clear();
        }

        public void UpdateItem(int index, PlaylistItem updatedItem)
        {
            if (index < 0 || index >= _items.Count) return;
            _fileValidator.Validate(updatedItem);
            _items[index] = updatedItem;
        }

        public void SaveToPersistence()
        {
            _persistence.Save([.. _items]);
        }
    }
}
