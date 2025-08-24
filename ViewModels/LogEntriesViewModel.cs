using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;
using EquipMaster.Models;
using EquipMaster.ViewModels.Commands;
using Microsoft.EntityFrameworkCore;

namespace EquipMaster.ViewModels
{
    public class LogEntriesViewModel : INotifyPropertyChanged
    {
        private readonly ApplicationDbContext _ctx;
        private const int PageSize = 50;

        public ObservableCollection<LogEntry> AllEntries { get; }
        public ICollectionView FilteredView { get; }
        public ICollectionView PagedEntriesView { get; }
        public ObservableCollection<string> ActionTypes { get; }
        public ObservableCollection<string> EntityTypes { get; }

        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged(nameof(SearchText));
                RefreshFilters();
            }
        }

        private string _selectedActionType = "Все";
        public string SelectedActionType
        {
            get => _selectedActionType;
            set
            {
                _selectedActionType = value;
                OnPropertyChanged(nameof(SelectedActionType));
                RefreshFilters();
            }
        }

        private string _selectedEntityType = "Все";
        public string SelectedEntityType
        {
            get => _selectedEntityType;
            set
            {
                _selectedEntityType = value;
                OnPropertyChanged(nameof(SelectedEntityType));
                RefreshFilters();
            }
        }

        private DateTime? _startDate;
        public DateTime? StartDate
        {
            get => _startDate;
            set
            {
                _startDate = value;
                OnPropertyChanged(nameof(StartDate));
                RefreshFilters();
            }
        }

        private DateTime? _endDate;
        public DateTime? EndDate
        {
            get => _endDate;
            set
            {
                _endDate = value;
                OnPropertyChanged(nameof(EndDate));
                RefreshFilters();
            }
        }

        private int _currentPage = 1;
        public int CurrentPage
        {
            get => _currentPage;
            set
            {
                _currentPage = value;
                OnPropertyChanged(nameof(CurrentPage));
                OnPropertyChanged(nameof(CanPrevPage));
                OnPropertyChanged(nameof(CanNextPage));
                PagedEntriesView.Refresh();
            }
        }

        public int TotalPages => (int)Math.Ceiling((double)FilteredView.Cast<object>().Count() / PageSize);
        public bool CanPrevPage => CurrentPage > 1;
        public bool CanNextPage => CurrentPage < TotalPages;

        public ICommand RefreshCommand { get; }
        public ICommand NextPageCommand { get; }
        public ICommand PrevPageCommand { get; }

        public LogEntriesViewModel()
        {
            _ctx = new ApplicationDbContext();
            AllEntries = new ObservableCollection<LogEntry>();

            FilteredView = CollectionViewSource.GetDefaultView(AllEntries);
            FilteredView.Filter = _ => true;

            PagedEntriesView = CollectionViewSource.GetDefaultView(AllEntries);
            PagedEntriesView.Filter = PageFilter;

            ActionTypes = new ObservableCollection<string>
{
    "Все",
    "Создание",
    "Изменение",
    "Удаление",
  
};


            EntityTypes = new ObservableCollection<string> { "Все" };
            var distinctEntities = _ctx.LogEntries
                .Select(l => l.EntityName)
                .Distinct()
                .ToList();

            foreach (var entity in distinctEntities)
            {
                if (!EntityTypes.Contains(entity))
                    EntityTypes.Add(entity);
            }

            
            if (!EntityTypes.Contains("Assignment"))
                EntityTypes.Add("Assignment");

            RefreshCommand = new RelayCommand<object>(_ => LoadEntries());
            NextPageCommand = new RelayCommand<object>(_ => CurrentPage++, _ => CanNextPage);
            PrevPageCommand = new RelayCommand<object>(_ => CurrentPage--, _ => CanPrevPage);

            LoadEntries();
        }

        private void LoadEntries()
        {
            AllEntries.Clear();
            var list = _ctx.LogEntries
                .AsNoTracking()
                .OrderByDescending(l => l.Timestamp)
                .ToList();

            foreach (var e in list)
            {
                AllEntries.Add(e);
            }

            CurrentPage = 1;
            RefreshFilters();
        }

        private void RefreshFilters()
        {
            FilteredView.Filter = FilterEntries;
            FilteredView.Refresh();

            OnPropertyChanged(nameof(TotalPages));

            if (CurrentPage > TotalPages)
                CurrentPage = TotalPages == 0 ? 1 : TotalPages;

            PagedEntriesView.Refresh();
        }

        private bool FilterEntries(object obj)
        {
            if (obj is not LogEntry entry) return false;

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var key = SearchText.ToLower();
                if (!(entry.Username.ToLower().Contains(key)
                      || entry.EntityName.ToLower().Contains(key)
                      || entry.Details?.ToLower().Contains(key) == true))
                {
                    return false;
                }
            }

            if (SelectedActionType != "Все" && entry.DisplayAction != SelectedActionType)
                return false;

            if (SelectedEntityType != "Все" && entry.EntityName != SelectedEntityType)
                return false;

            if (StartDate.HasValue && entry.Timestamp < StartDate.Value)
                return false;
            if (EndDate.HasValue && entry.Timestamp > EndDate.Value.AddDays(1).AddTicks(-1))
                return false;

            return true;
        }

        private bool PageFilter(object obj)
        {
            if (obj is not LogEntry) return false;
            var index = FilteredView.Cast<object>().ToList().IndexOf(obj);
            return index >= (CurrentPage - 1) * PageSize && index < CurrentPage * PageSize;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
    }
}
