using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using EquipMaster.Models;
using Microsoft.EntityFrameworkCore;

namespace EquipMaster.ViewModels
{
    public class AssignmentHistoryViewModel : INotifyPropertyChanged
    {
        public AssignmentHistoryViewModel()
        {
            AssignmentHistory = new ObservableCollection<AssignmentHistoryItem>();
            AssignmentHistoryView = CollectionViewSource.GetDefaultView(AssignmentHistory);
            AssignmentHistoryView.Filter = FilterHistory;
            LoadHistory();
        }

        public ObservableCollection<AssignmentHistoryItem> AssignmentHistory { get; }
        public ICollectionView AssignmentHistoryView { get; }

        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    OnPropertyChanged(nameof(SearchText));
                    AssignmentHistoryView.Refresh();
                }
            }
        }

        private DateTime? _assignedFrom;
        public DateTime? AssignedFrom
        {
            get => _assignedFrom;
            set
            {
                if (_assignedFrom != value)
                {
                    _assignedFrom = value;
                    OnPropertyChanged(nameof(AssignedFrom));
                    AssignmentHistoryView.Refresh();
                }
            }
        }

        private DateTime? _assignedTo;
        public DateTime? AssignedTo
        {
            get => _assignedTo;
            set
            {
                if (_assignedTo != value)
                {
                    _assignedTo = value;
                    OnPropertyChanged(nameof(AssignedTo));
                    AssignmentHistoryView.Refresh();
                }
            }
        }

        private DateTime? _returnedFrom;
        public DateTime? ReturnedFrom
        {
            get => _returnedFrom;
            set
            {
                if (_returnedFrom != value)
                {
                    _returnedFrom = value;
                    OnPropertyChanged(nameof(ReturnedFrom));
                    AssignmentHistoryView.Refresh();
                }
            }
        }

        private DateTime? _returnedTo;
        public DateTime? ReturnedTo
        {
            get => _returnedTo;
            set
            {
                if (_returnedTo != value)
                {
                    _returnedTo = value;
                    OnPropertyChanged(nameof(ReturnedTo));
                    AssignmentHistoryView.Refresh();
                }
            }
        }

        private AssignmentHistoryItem _selectedItem;
        public AssignmentHistoryItem SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (_selectedItem != value)
                {
                    _selectedItem = value;
                    OnPropertyChanged(nameof(SelectedItem));
                }
            }
        }

        private bool FilterHistory(object obj)
        {
            if (obj is not AssignmentHistoryItem item) return false;

            var key = SearchText?.Trim().ToLower() ?? string.Empty;

            // Поиск по текстовым полям и по строковому представлению дат
            bool textMatch = string.IsNullOrWhiteSpace(key) ||
                (item.UserName?.ToLower().Contains(key) == true) ||
                (item.SerialNumber?.ToLower().Contains(key) == true) ||
                (item.EquipmentModel?.ToLower().Contains(key) == true) ||
                (item.EquipmentType?.ToLower().Contains(key) == true) ||
                (item.AssignmentNotes?.ToLower().Contains(key) == true) ||
                (item.ReturnNotes?.ToLower().Contains(key) == true) ||

                // Также ищем по дате выдачи
                item.AssignedAt.ToString("dd.MM.yyyy").ToLower().Contains(key) ||
                item.AssignedAt.ToString("MM.yyyy").ToLower().Contains(key) ||
                item.AssignedAt.ToString("yyyy").ToLower().Contains(key) ||

                // Также ищем по дате возврата (если есть)
                (item.ReturnedAt?.ToString("dd.MM.yyyy").ToLower().Contains(key) == true) ||
                (item.ReturnedAt?.ToString("MM.yyyy").ToLower().Contains(key) == true) ||
                (item.ReturnedAt?.ToString("yyyy").ToLower().Contains(key) == true);

            // Диапазон дат выдачи
            bool assignedMatch = (!AssignedFrom.HasValue || item.AssignedAt >= AssignedFrom.Value)
                              && (!AssignedTo.HasValue || item.AssignedAt <= AssignedTo.Value);

            // Диапазон дат возврата
            bool returnedMatch = (!ReturnedFrom.HasValue || (item.ReturnedAt.HasValue && item.ReturnedAt.Value >= ReturnedFrom.Value))
                              && (!ReturnedTo.HasValue || (item.ReturnedAt.HasValue && item.ReturnedAt.Value <= ReturnedTo.Value));

            return textMatch && assignedMatch && returnedMatch;
        }


        private void LoadHistory()
        {
            try
            {
                using var ctx = new ApplicationDbContext();
                var list = ctx.Assignments
                              .Include(a => a.Equipment)
                                  .ThenInclude(e => e.EquipmentType)
                              .Include(a => a.User)
                              .Select(a => new AssignmentHistoryItem
                              {
                                  UserName = a.User.FullName,
                                  SerialNumber = a.Equipment.SerialNumber,
                                  EquipmentModel = a.Equipment.Model,
                                  EquipmentType = a.Equipment.EquipmentType.Name,
                                  AssignedAt = a.AssignedAt,
                                  ReturnedAt = a.ReturnedAt,
                                  AssignmentNotes = a.AssignmentNotes,
                                  ReturnNotes = a.ReturnNotes
                              })
                              .OrderByDescending(x => x.AssignedAt)
                              .ToList();

                AssignmentHistory.Clear();
                foreach (var item in list)
                    AssignmentHistory.Add(item);

                AssignmentHistoryView.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке истории:\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string prop) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }

    public class AssignmentHistoryItem
    {
        public string UserName { get; set; }
        public string SerialNumber { get; set; }
        public string EquipmentModel { get; set; }
        public string EquipmentType { get; set; }
        public DateTime AssignedAt { get; set; }
        public DateTime? ReturnedAt { get; set; }
        public string AssignmentNotes { get; set; }
        public string ReturnNotes { get; set; }
    }
}
