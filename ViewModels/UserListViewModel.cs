using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using EquipMaster.Models;
using Microsoft.EntityFrameworkCore;
using EquipMaster.ViewModels.Commands;

namespace EquipMaster.ViewModels
{
    public class UserListViewModel : INotifyPropertyChanged
    {
        public UserListViewModel()
        {
            Users = new ObservableCollection<UserListItem>();
            UsersView = CollectionViewSource.GetDefaultView(Users);
            UsersView.Filter = FilterUsers;

            CreateCommand = new RelayCommand<object>(_ => CreateUser());
            EditCommand = new RelayCommand<object>(_ => EditUser(), _ => SelectedUser != null);
            DeleteCommand = new RelayCommand<object>(_ => DeleteUser(), _ => SelectedUser != null);
            ClearFilterCommand = new RelayCommand<object>(_ => ClearFilters());

            LoadUsers();
        }

        public ObservableCollection<UserListItem> Users { get; }
        public ICollectionView UsersView { get; }

        private string _filterFullName;
        public string FilterFullName
        {
            get => _filterFullName;
            set
            {
                if (_filterFullName != value)
                {
                    _filterFullName = value;
                    OnPropertyChanged(nameof(FilterFullName));
                    UsersView.Refresh();
                }
            }
        }

        private string _filterDepartment;
        public string FilterDepartment
        {
            get => _filterDepartment;
            set
            {
                if (_filterDepartment != value)
                {
                    _filterDepartment = value;
                    OnPropertyChanged(nameof(FilterDepartment));
                    UsersView.Refresh();
                }
            }
        }

        private string _filterPersonnelNumber;
        public string FilterPersonnelNumber
        {
            get => _filterPersonnelNumber;
            set
            {
                if (_filterPersonnelNumber != value)
                {
                    _filterPersonnelNumber = value;
                    OnPropertyChanged(nameof(FilterPersonnelNumber));
                    UsersView.Refresh();
                }
            }
        }

        private bool? _filterBlocked;
        public bool? FilterBlocked
        {
            get => _filterBlocked;
            set
            {
                if (_filterBlocked != value)
                {
                    _filterBlocked = value;
                    OnPropertyChanged(nameof(FilterBlocked));
                    UsersView.Refresh();
                }
            }
        }

        public ICommand CreateCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand ClearFilterCommand { get; }

        private UserListItem _selectedUser;
        public UserListItem SelectedUser
        {
            get => _selectedUser;
            set
            {
                if (_selectedUser != value)
                {
                    _selectedUser = value;
                    OnPropertyChanged(nameof(SelectedUser));
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        private void LoadUsers()
        {
            try
            {
                Users.Clear();
                using var ctx = new ApplicationDbContext();
                var list = ctx.Users
                              .AsNoTracking()
                              .Select(u => new UserListItem
                              {
                                  Id = u.Id,
                                  FullName = u.FullName,
                                  Department = u.Department,
                                  PersonnelNumber = u.PersonnelNumber,
                                  IsBlocked = u.IsBlocked
                              })
                              .ToList();
                foreach (var u in list)
                    Users.Add(u);

                UsersView.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки пользователей:\n{ex.Message}",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CreateUser()
        {
            var win = new Views.Users.UserEditWindow
            {
                DataContext = new UserEditViewModel(new User())
            };
            if (win.ShowDialog() == true)
                LoadUsers();
        }

        private void EditUser()
        {
            if (SelectedUser == null) return;

            using var ctx = new ApplicationDbContext();
            var entity = ctx.Users.Find(SelectedUser.Id);
            if (entity == null) return;

            var win = new Views.Users.UserEditWindow
            {
                DataContext = new UserEditViewModel(entity)
            };
            if (win.ShowDialog() == true)
                LoadUsers();
        }

        private void DeleteUser()
        {
            if (SelectedUser == null) return;

            var res = MessageBox.Show($"Удалить пользователя «{SelectedUser.FullName}»?",
                                      "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (res != MessageBoxResult.Yes) return;

            try
            {
                using var ctx = new ApplicationDbContext();
                var toDel = ctx.Users.Find(SelectedUser.Id);
                if (toDel != null)
                {
                    ctx.Users.Remove(toDel);
                    ctx.SaveChanges();
                }
                LoadUsers();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении:\n{ex.Message}",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearFilters()
        {
            FilterFullName = null;
            FilterDepartment = null;
            FilterPersonnelNumber = null;
            FilterBlocked = null;
        }

        private bool FilterUsers(object item)
        {
            if (item is not UserListItem user) return false;

            if (!string.IsNullOrWhiteSpace(FilterFullName))
                if (user.FullName?.IndexOf(FilterFullName, StringComparison.OrdinalIgnoreCase) < 0)
                    return false;

            if (!string.IsNullOrWhiteSpace(FilterDepartment))
                if (user.Department?.IndexOf(FilterDepartment, StringComparison.OrdinalIgnoreCase) < 0)
                    return false;

            if (!string.IsNullOrWhiteSpace(FilterPersonnelNumber))
                if (user.PersonnelNumber?.IndexOf(FilterPersonnelNumber, StringComparison.OrdinalIgnoreCase) < 0)
                    return false;

            if (FilterBlocked.HasValue)
                if (user.IsBlocked != FilterBlocked.Value)
                    return false;

            return true;
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string prop) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        #endregion
    }

    public class UserListItem
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Department { get; set; }
        public string PersonnelNumber { get; set; }
        public bool IsBlocked { get; set; }
    }
}
