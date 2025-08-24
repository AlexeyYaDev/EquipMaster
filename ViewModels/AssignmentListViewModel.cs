using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using EquipMaster.Models;
using System;
using System.Linq;

namespace EquipMaster.ViewModels.AssignmentList
{
    public class AssignmentListViewModel : INotifyPropertyChanged
    {
        public AssignmentListViewModel()
        {
            Assignments = new ObservableCollection<Models.Assignment>();

            LoadCommand = new RelayCommand<object>(_ => LoadAssignments());
            IssueCommand = new RelayCommand<object>(_ => IssueSelected(), _ => SelectedAssignment != null);
            ReturnCommand = new RelayCommand<object>(_ => ReturnSelected(), _ => SelectedAssignment != null);
        }

        public ObservableCollection<Models.Assignment> Assignments { get; set; }

        private Models.Assignment _selectedAssignment;
        public Models.Assignment SelectedAssignment
        {
            get => _selectedAssignment;
            set
            {
                if (_selectedAssignment != value)
                {
                    _selectedAssignment = value;
                    OnPropertyChanged(nameof(SelectedAssignment));
                }
            }
        }

        public ICommand LoadCommand { get; }
        public ICommand IssueCommand { get; }
        public ICommand ReturnCommand { get; }

        private void LoadAssignments()
        {
            try
            {
                using (var ctx = new ApplicationDbContext())
                {
                    Assignments.Clear();
                    var list = ctx.Assignments
                        .ToList();
                    foreach (var assignment in list)
                        Assignments.Add(assignment);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке назначений: {ex.Message}",
                                "Ошибка",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
        }

        private void IssueSelected()
        {
           
            MessageBox.Show("Выдача пока не реализована.");
        }

        private void ReturnSelected()
        {
           
            MessageBox.Show("Возврат пока не реализован.");
        }

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

            public bool CanExecute(object parameter) =>
                _canExecute == null || _canExecute((T)parameter);

            public void Execute(object parameter) =>
                _execute((T)parameter);

            public event EventHandler CanExecuteChanged
            {
                add => CommandManager.RequerySuggested += value;
                remove => CommandManager.RequerySuggested -= value;
            }
        }

        #endregion
    }
}
