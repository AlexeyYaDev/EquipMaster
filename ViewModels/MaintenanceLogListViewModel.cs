using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using EquipMaster.Models;
using EquipMaster.ViewModels.Commands;
using Microsoft.EntityFrameworkCore;

namespace EquipMaster.ViewModels.Maintenance
{
    public class MaintenanceLogListViewModel : INotifyPropertyChanged
    {
        public MaintenanceLogListViewModel()
        {
            Logs = new ObservableCollection<MaintenanceLog>();
            LogsView = CollectionViewSource.GetDefaultView(Logs);
            LogsView.Filter = FilterLogs;

            AddCommand = new RelayCommand(AddLog);
            EditCommand = new RelayCommand(EditLog, () => SelectedLog != null);
            RefreshCommand = new RelayCommand(LoadLogs);
            CloseCommand = new RelayCommand(CloseWindow);

            LoadLogs();
        }

        public ObservableCollection<MaintenanceLog> Logs { get; }
        public ICollectionView LogsView { get; }

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
                    LogsView.Refresh();
                }
            }
        }

        private bool FilterLogs(object obj)
        {
            if (obj is not MaintenanceLog log) return false;
            if (string.IsNullOrWhiteSpace(SearchText)) return true;

            var s = SearchText.ToLower();
            return (log.Equipment.SerialNumber?.ToLower().Contains(s) == true)
                || (log.Equipment.Model?.ToLower().Contains(s) == true)
                || (log.Equipment.EquipmentType?.Name.ToLower().Contains(s) == true)
                || (log.PerformedBy?.ToLower().Contains(s) == true)
                || (log.MaintenanceType?.ToLower().Contains(s) == true)
                || (log.Result?.ToLower().Contains(s) == true);
        }

        private MaintenanceLog _selectedLog;
        public MaintenanceLog SelectedLog
        {
            get => _selectedLog;
            set
            {
                if (_selectedLog != value)
                {
                    _selectedLog = value;
                    OnPropertyChanged(nameof(SelectedLog));
                    (EditCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand CloseCommand { get; }

        private void LoadLogs()
        {
            Logs.Clear();
            using var ctx = new ApplicationDbContext();
            var list = ctx.MaintenanceLogs
                          .Include(l => l.Equipment)
                              .ThenInclude(e => e.EquipmentType)
                          .AsNoTracking()
                          .OrderByDescending(l => l.Date)
                          .ToList();
            foreach (var l in list)
                Logs.Add(l);

            LogsView.Refresh();
        }

        private void AddLog()
        {
            var win = new Views.Maintenance.MaintenanceLogEditWindow();
            if (win.ShowDialog() == true)
                LoadLogs();
        }

        private void EditLog()
        {
            if (SelectedLog == null) return;

            // Открываем окно редактирования и передаём копию выбранного MaintenanceLog
            var win = new Views.Maintenance.MaintenanceLogEditWindow();
            win.DataContext = new MaintenanceLogEditViewModel(SelectedLog);
            if (win.ShowDialog() == true)
                LoadLogs();
        }

        private void CloseWindow()
        {
            var wnd = App.Current.Windows
                .OfType<Window>()
                .FirstOrDefault(w => w.DataContext == this);
            wnd?.Close();
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string prop) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        #endregion
    }
}
