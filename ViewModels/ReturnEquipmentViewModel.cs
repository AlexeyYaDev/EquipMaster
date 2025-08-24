using EquipMaster.Models;
using EquipMaster.ViewModels.Commands;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace EquipMaster.ViewModels.Assignment
{
    public class ReturnEquipmentViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<Models.Assignment> _activeAssignments;
        public ObservableCollection<Models.Assignment> ActiveAssignments
        {
            get => _activeAssignments;
            set
            {
                _activeAssignments = value;
                OnPropertyChanged(nameof(ActiveAssignments));
            }
        }

        private Models.Assignment _selectedAssignment;
        public Models.Assignment SelectedAssignment
        {
            get => _selectedAssignment;
            set
            {
                _selectedAssignment = value;
                OnPropertyChanged(nameof(SelectedAssignment));
                ((RelayCommand)ReturnCommand).RaiseCanExecuteChanged();
            }
        }

        private string _returnNotes;
        public string ReturnNotes
        {
            get => _returnNotes;
            set
            {
                _returnNotes = value;
                OnPropertyChanged(nameof(ReturnNotes));
            }
        }

        public ICommand ReturnCommand { get; }

        public ReturnEquipmentViewModel()
        {
            ReturnCommand = new RelayCommand(ReturnEquipment, CanReturnEquipment);
            LoadActiveAssignments();
        }

        private void LoadActiveAssignments()
        {
            using var context = new ApplicationDbContext();
            var active = context.Assignments
                .Include(a => a.User)
                .Include(a => a.Equipment)
                .Where(a => a.ReturnedAt == null)
                .ToList();

            ActiveAssignments = new ObservableCollection<Models.Assignment>(active);
        }

        private bool CanReturnEquipment() => SelectedAssignment != null;

        private void ReturnEquipment()
        {
            try
            {
                // Передаём имя текущего пользователя
                var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
                using var context = new ApplicationDbContext(optionsBuilder.Options, Environment.UserName);

                var assignment = context.Assignments
                    .Include(a => a.Equipment)
                    .Include(a => a.User)
                    .FirstOrDefault(a => a.Id == SelectedAssignment.Id);

                if (assignment == null) return;

                assignment.ReturnedAt = DateTime.Now;
                assignment.ReturnNotes = ReturnNotes;

                if (assignment.Equipment != null)
                {
                    assignment.Equipment.Status = EquipmentStatus.InReserve;
                }

                // Помечаем как изменённое
                context.Entry(assignment).State = EntityState.Modified;
                context.Entry(assignment.Equipment).State = EntityState.Modified;

                context.SaveChanges();

                MessageBox.Show("Оборудование успешно возвращено.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                Application.Current.Windows.OfType<Window>().SingleOrDefault(w => w.IsActive)?.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при возврате: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
