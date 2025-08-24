using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using EquipMaster.Models;
using EquipMaster.ViewModels.Commands;

namespace EquipMaster.ViewModels
{
    public class AssignmentEditViewModel : INotifyPropertyChanged
    {
        // выбранные в интерфейсе оборудование и пользователь
        private Equipment _selectedEquipment;
        public Equipment SelectedEquipment
        {
            get => _selectedEquipment;
            set
            {
                if (_selectedEquipment != value)
                {
                    _selectedEquipment = value;
                    OnPropertyChanged(nameof(SelectedEquipment));
                }
            }
        }

        private User _selectedUser;
        public User SelectedUser
        {
            get => _selectedUser;
            set
            {
                if (_selectedUser != value)
                {
                    _selectedUser = value;
                    OnPropertyChanged(nameof(SelectedUser));
                }
            }
        }

        // дата и заметка
        private DateTime _assignedAt;
        public DateTime AssignedAt
        {
            get => _assignedAt;
            set
            {
                if (_assignedAt != value)
                {
                    _assignedAt = value;
                    OnPropertyChanged(nameof(AssignedAt));
                }
            }
        }

        private string _assignmentNotes;
        public string AssignmentNotes
        {
            get => _assignmentNotes;
            set
            {
                if (_assignmentNotes != value)
                {
                    _assignmentNotes = value;
                    OnPropertyChanged(nameof(AssignmentNotes));
                }
            }
        }

        public ObservableCollection<Equipment> AvailableEquipments { get; }
        public ObservableCollection<User> Users { get; }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public AssignmentEditViewModel()
        {
            AssignedAt = DateTime.Now;
            AssignmentNotes = string.Empty;

            using (var ctx = new ApplicationDbContext())
            {
                AvailableEquipments = new ObservableCollection<Equipment>(
                    ctx.Equipments
                       .Where(e => e.Status == EquipmentStatus.InReserve)
                       .ToList());

                Users = new ObservableCollection<User>(ctx.Users.ToList());
            }

            SaveCommand = new RelayCommand<Window>(SaveExecuted, CanSave);
            CancelCommand = new RelayCommand<Window>(w => w.Close());
        }

        private bool CanSave(Window win) =>
            SelectedEquipment != null &&
            SelectedUser != null;

        private void SaveExecuted(Window win)
        {
            try
            {
                using (var ctx = new ApplicationDbContext())
                {
                    // переводим оборудование в использование
                    var eqEntity = ctx.Equipments.Find(SelectedEquipment.Id);
                    if (eqEntity != null)
                        eqEntity.Status = EquipmentStatus.InUse;

                    // создаём новую запись назначения
                    var assignmentEntity = new EquipMaster.Models.Assignment
                    {
                        EquipmentId = SelectedEquipment.Id,
                        UserId = SelectedUser.Id,
                        AssignedAt = AssignedAt,
                        AssignmentNotes = AssignmentNotes,
                        ReturnedAt = null,
                        ReturnNotes = string.Empty
                    };

                    ctx.Assignments.Add(assignmentEntity);
                    ctx.SaveChanges();
                }

                MessageBox.Show("Оборудование успешно выдано.", "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);

                win.DialogResult = true;
                win.Close();
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message;
                MessageBox.Show(
                    $"Ошибка при выдаче: {ex.Message}" +
                    (inner != null ? $"\n\nInner: {inner}" : ""),
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));

        #endregion
    }
}
