using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using EquipMaster.Models;
using Microsoft.EntityFrameworkCore;

namespace EquipMaster.ViewModels
{
    public class EquipmentListViewModel : INotifyPropertyChanged
    {
        public EquipmentListViewModel()
        {
            Equipments = new ObservableCollection<EquipmentListItem>();
            EquipmentTypes = new ObservableCollection<EquipmentType>();
            Users = new ObservableCollection<User>();
            EquipmentsView = CollectionViewSource.GetDefaultView(Equipments);
            EquipmentsView.Filter = FilterEquipments;

            LoadCommand = new RelayCommand<object>(_ => LoadEquipments());
            RefreshCommand = new RelayCommand<object>(_ => LoadEquipments());
            AddCommand = new RelayCommand<object>(_ => AddEquipment());
            EditCommand = new RelayCommand<object>(_ => EditEquipment(), _ => SelectedEquipment != null);
            DeleteCommand = new RelayCommand<object>(_ => DeleteEquipment(), _ => SelectedEquipment != null);
            ClearFiltersCommand = new RelayCommand<object>(_ => ClearFilters());

            LoadEquipmentTypes();
            LoadUsers();
            LoadEquipments();
        }

        public ObservableCollection<EquipmentListItem> Equipments { get; }
        public ObservableCollection<EquipmentType> EquipmentTypes { get; }
        public ObservableCollection<User> Users { get; }
        public ICollectionView EquipmentsView { get; }

        // фильтры и поиск 

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
                    EquipmentsView.Refresh();
                }
            }
        }

        private DateTime? _purchaseFrom;
        public DateTime? PurchaseFrom
        {
            get => _purchaseFrom;
            set
            {
                if (_purchaseFrom != value)
                {
                    _purchaseFrom = value;
                    OnPropertyChanged(nameof(PurchaseFrom));
                    EquipmentsView.Refresh();
                }
            }
        }

        private DateTime? _purchaseTo;
        public DateTime? PurchaseTo
        {
            get => _purchaseTo;
            set
            {
                if (_purchaseTo != value)
                {
                    _purchaseTo = value;
                    OnPropertyChanged(nameof(PurchaseTo));
                    EquipmentsView.Refresh();
                }
            }
        }

        private DateTime? _nextMaintFrom;
        public DateTime? NextMaintFrom
        {
            get => _nextMaintFrom;
            set
            {
                if (_nextMaintFrom != value)
                {
                    _nextMaintFrom = value;
                    OnPropertyChanged(nameof(NextMaintFrom));
                    EquipmentsView.Refresh();
                }
            }
        }

        private DateTime? _nextMaintTo;
        public DateTime? NextMaintTo
        {
            get => _nextMaintTo;
            set
            {
                if (_nextMaintTo != value)
                {
                    _nextMaintTo = value;
                    OnPropertyChanged(nameof(NextMaintTo));
                    EquipmentsView.Refresh();
                }
            }
        }

        private DateTime? _lastMaintFrom;
        public DateTime? LastMaintFrom
        {
            get => _lastMaintFrom;
            set
            {
                if (_lastMaintFrom != value)
                {
                    _lastMaintFrom = value;
                    OnPropertyChanged(nameof(LastMaintFrom));
                    EquipmentsView.Refresh();
                }
            }
        }

        private DateTime? _lastMaintTo;
        public DateTime? LastMaintTo
        {
            get => _lastMaintTo;
            set
            {
                if (_lastMaintTo != value)
                {
                    _lastMaintTo = value;
                    OnPropertyChanged(nameof(LastMaintTo));
                    EquipmentsView.Refresh();
                }
            }
        }

        private DateTime? _decommissionFrom;
        public DateTime? DecommissionFrom
        {
            get => _decommissionFrom;
            set
            {
                if (_decommissionFrom != value)
                {
                    _decommissionFrom = value;
                    OnPropertyChanged(nameof(DecommissionFrom));
                    EquipmentsView.Refresh();
                }
            }
        }

        private DateTime? _decommissionTo;
        public DateTime? DecommissionTo
        {
            get => _decommissionTo;
            set
            {
                if (_decommissionTo != value)
                {
                    _decommissionTo = value;
                    OnPropertyChanged(nameof(DecommissionTo));
                    EquipmentsView.Refresh();
                }
            }
        }

        private int? _selectedEquipmentTypeId;
        public int? SelectedEquipmentTypeId
        {
            get => _selectedEquipmentTypeId;
            set
            {
                if (_selectedEquipmentTypeId != value)
                {
                    _selectedEquipmentTypeId = value;
                    OnPropertyChanged(nameof(SelectedEquipmentTypeId));
                    EquipmentsView.Refresh();
                }
            }
        }

        private int? _selectedUserId;
        public int? SelectedUserId
        {
            get => _selectedUserId;
            set
            {
                if (_selectedUserId != value)
                {
                    _selectedUserId = value;
                    OnPropertyChanged(nameof(SelectedUserId));
                    EquipmentsView.Refresh();
                }
            }
        }

        public ICommand ClearFiltersCommand { get; }

        private void ClearFilters()
        {
            SearchText = string.Empty;
            PurchaseFrom = null;
            PurchaseTo = null;
            NextMaintFrom = null;
            NextMaintTo = null;
            LastMaintFrom = null;
            LastMaintTo = null;
            DecommissionFrom = null;
            DecommissionTo = null;
            SelectedEquipmentTypeId = null;
            SelectedUserId = null;
        }

        private void LoadEquipmentTypes()
        {
            try
            {
                EquipmentTypes.Clear();
                using var ctx = new ApplicationDbContext();
                var types = ctx.EquipmentTypes.OrderBy(t => t.Name).ToList();
                foreach (var type in types)
                {
                    EquipmentTypes.Add(type);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке типов оборудования: {ex.Message}",
                                "Ошибка",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
        }

        private void LoadUsers()
        {
            try
            {
                Users.Clear();
                using var ctx = new ApplicationDbContext();
                var users = ctx.Users.OrderBy(u => u.FullName).ToList();
                foreach (var user in users)
                {
                    Users.Add(user);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке пользователей: {ex.Message}",
                                "Ошибка",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
        }

        private bool FilterEquipments(object obj)
        {
            if (obj is not EquipmentListItem item) return false;
            var key = SearchText?.Trim().ToLower() ?? string.Empty;

            bool textOrDateMatch = string.IsNullOrWhiteSpace(key)
                || (item.SerialNumber?.ToLower().Contains(key) == true)
                || (item.Model?.ToLower().Contains(key) == true)
                || (item.EquipmentTypeName?.ToLower().Contains(key) == true)
                || (item.AssignedUserName?.ToLower().Contains(key) == true)
                || (item.StatusDisplay?.ToLower().Contains(key) == true)
                || item.PurchaseDate.ToString("dd.MM.yyyy").Contains(key)
                || item.PurchaseDate.ToString("MM.yyyy").Contains(key)
                || item.PurchaseDate.ToString("yyyy").Contains(key)
                || (item.NextMaintenanceDate.HasValue && (
                       item.NextMaintenanceDate.Value.ToString("dd.MM.yyyy").Contains(key)
                    || item.NextMaintenanceDate.Value.ToString("MM.yyyy").Contains(key)
                    || item.NextMaintenanceDate.Value.ToString("yyyy").Contains(key)))
                || (item.LastMaintenanceDate.HasValue && (
                       item.LastMaintenanceDate.Value.ToString("dd.MM.yyyy").Contains(key)
                    || item.LastMaintenanceDate.Value.ToString("MM.yyyy").Contains(key)
                    || item.LastMaintenanceDate.Value.ToString("yyyy").Contains(key)))
                || (item.DecommissionDate.HasValue && (
                       item.DecommissionDate.Value.ToString("dd.MM.yyyy").Contains(key)
                    || item.DecommissionDate.Value.ToString("MM.yyyy").Contains(key)
                    || item.DecommissionDate.Value.ToString("yyyy").Contains(key)));

            bool purchaseInRange =
                (!PurchaseFrom.HasValue || item.PurchaseDate >= PurchaseFrom.Value) &&
                (!PurchaseTo.HasValue || item.PurchaseDate <= PurchaseTo.Value);

            bool nextMaintInRange =
                (!NextMaintFrom.HasValue || (item.NextMaintenanceDate.HasValue && item.NextMaintenanceDate.Value >= NextMaintFrom.Value)) &&
                (!NextMaintTo.HasValue || (item.NextMaintenanceDate.HasValue && item.NextMaintenanceDate.Value <= NextMaintTo.Value));

            bool lastMaintInRange =
                (!LastMaintFrom.HasValue || (item.LastMaintenanceDate.HasValue && item.LastMaintenanceDate.Value >= LastMaintFrom.Value)) &&
                (!LastMaintTo.HasValue || (item.LastMaintenanceDate.HasValue && item.LastMaintenanceDate.Value <= LastMaintTo.Value));

            bool decommissionInRange =
                (!DecommissionFrom.HasValue || (item.DecommissionDate.HasValue && item.DecommissionDate.Value >= DecommissionFrom.Value)) &&
                (!DecommissionTo.HasValue || (item.DecommissionDate.HasValue && item.DecommissionDate.Value <= DecommissionTo.Value));

            bool equipmentTypeMatch =
                !SelectedEquipmentTypeId.HasValue ||
                item.EquipmentTypeName == EquipmentTypes.FirstOrDefault(t => t.Id == SelectedEquipmentTypeId)?.Name;

            bool userMatch =
                !SelectedUserId.HasValue ||
                item.AssignedUserName == Users.FirstOrDefault(u => u.Id == SelectedUserId)?.FullName;

            return textOrDateMatch && purchaseInRange && nextMaintInRange &&
                   lastMaintInRange && decommissionInRange && equipmentTypeMatch && userMatch;
        }

        //  остальной CRUD 

        private EquipmentListItem _selectedEquipment;
        public EquipmentListItem SelectedEquipment
        {
            get => _selectedEquipment;
            set
            {
                if (_selectedEquipment != value)
                {
                    _selectedEquipment = value;
                    OnPropertyChanged(nameof(SelectedEquipment));
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public ICommand LoadCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }

        private void LoadEquipments()
        {
            try
            {
                Equipments.Clear();
                using var ctx = new ApplicationDbContext();
                var list = ctx.Equipments
                              .Include(e => e.EquipmentType)
                              .Include(e => e.Assignments).ThenInclude(a => a.User)
                              .Include(e => e.MaintenanceLogs)
                              .ToList()
                              .Select(e => new EquipmentListItem
                              {
                                  Id = e.Id,
                                  SerialNumber = e.SerialNumber,
                                  Model = e.Model,
                                  EquipmentTypeName = e.EquipmentType.Name,
                                  StatusDisplay = GetStatusDisplay(e.Status),
                                  PurchaseDate = e.PurchaseDate,
                                  NextMaintenanceDate = e.NextMaintenanceDate,
                                  LastMaintenanceDate = e.LastMaintenanceDate,
                                  DecommissionDate = e.DecommissionDate,
                                  AssignedUserName = e.Assignments
                                        .Where(a => a.ReturnedAt == null)
                                        .OrderByDescending(a => a.AssignedAt)
                                        .Select(a => a.User.FullName)
                                        .FirstOrDefault() ?? "Не назначено"
                              })
                              .ToList();

                foreach (var it in list)
                    Equipments.Add(it);

                EquipmentsView.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке оборудования: {ex.Message}",
                                "Ошибка",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
        }

        private void AddEquipment()
        {
            var window = new Views.Equipment.EquipmentEditWindow();
            if (window.ShowDialog() == true)
                LoadEquipments();
        }

        private void EditEquipment()
        {
            if (SelectedEquipment == null) return;
            using var ctx = new ApplicationDbContext();
            var entity = ctx.Equipments.Find(SelectedEquipment.Id);
            var vm = new EquipmentEditViewModel(entity);
            var w = new Views.Equipment.EquipmentEditWindow { DataContext = vm };
            if (w.ShowDialog() == true)
                LoadEquipments();
        }

        private void DeleteEquipment()
        {
            if (SelectedEquipment == null) return;

            var warningMessage = new StringBuilder()
                .AppendLine("Внимание! Вы собираетесь полностью удалить оборудование из базы данных.")
                .AppendLine()
                .AppendLine("При этом будут безвозвратно удалены:")
                .AppendLine("- Все записи о назначениях этого оборудования пользователям")
                .AppendLine("- Вся история технического обслуживания")
                .AppendLine("- Все связанные с оборудованием данные")
                .AppendLine()
                .AppendLine("Это действие нельзя отменить!")
                .AppendLine()
                .Append("Вы уверены, что хотите продолжить?");

            if (MessageBox.Show(warningMessage.ToString(),
                               "ПОДТВЕРЖДЕНИЕ ПОЛНОГО УДАЛЕНИЯ",
                               MessageBoxButton.YesNo,
                               MessageBoxImage.Warning,
                               MessageBoxResult.No) != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                using var ctx = new ApplicationDbContext();
                var equipment = ctx.Equipments
                                  .Include(e => e.Assignments)
                                  .Include(e => e.MaintenanceLogs)
                                  .FirstOrDefault(e => e.Id == SelectedEquipment.Id);

                if (equipment != null)
                {
                    // Удаляем связанные записи
                    ctx.Assignments.RemoveRange(equipment.Assignments);
                    ctx.MaintenanceLogs.RemoveRange(equipment.MaintenanceLogs);

                    // Затем удаляем само оборудование
                    ctx.Equipments.Remove(equipment);
                    ctx.SaveChanges();

                    MessageBox.Show("Оборудование и все связанные данные успешно удалены.",
                                   "Удаление завершено",
                                   MessageBoxButton.OK,
                                   MessageBoxImage.Information);
                }
                LoadEquipments();
            }
            catch (Exception ex)
            {
                string errorDetails = ex.InnerException != null
                    ? $"{ex.Message}\n\nДетали:\n{ex.InnerException.Message}"
                    : ex.Message;

                MessageBox.Show($"Не удалось удалить оборудование:\n{errorDetails}",
                               "Ошибка удаления",
                               MessageBoxButton.OK,
                               MessageBoxImage.Error);
            }
        }

        private string GetStatusDisplay(EquipmentStatus status) =>
            status switch
            {
                EquipmentStatus.InReserve => "В резерве",
                EquipmentStatus.InUse => "В использовании",
                EquipmentStatus.UnderMaintenance => "На обслуживании",
                EquipmentStatus.Decommissioned => "Списано",
                _ => status.ToString()
            };

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        #endregion

        #region RelayCommand
        public class RelayCommand<T> : ICommand
        {
            private readonly Action<T> _execute;
            private readonly Func<T, bool> _canExecute;
            public RelayCommand(Action<T> execute, Func<T, bool> canExecute = null)
            {
                _execute = execute ?? throw new ArgumentNullException(nameof(execute));
                _canExecute = canExecute;
            }
            public bool CanExecute(object param) =>
                _canExecute == null || _canExecute((T)param);
            public void Execute(object param) =>
                _execute((T)param);
            public event EventHandler CanExecuteChanged
            {
                add => CommandManager.RequerySuggested += value;
                remove => CommandManager.RequerySuggested -= value;
            }
        }
        #endregion
    }

    public class EquipmentListItem
    {
        public int Id { get; set; }
        public string SerialNumber { get; set; }
        public string Model { get; set; }
        public string EquipmentTypeName { get; set; }
        public string StatusDisplay { get; set; }
        public DateTime PurchaseDate { get; set; }
        public DateTime? NextMaintenanceDate { get; set; }
        public DateTime? LastMaintenanceDate { get; set; }
        public DateTime? DecommissionDate { get; set; }
        public string AssignedUserName { get; set; }
    }
}