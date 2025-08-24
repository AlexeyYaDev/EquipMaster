using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using EquipMaster.Models;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using System.ComponentModel.DataAnnotations;

namespace EquipMaster.ViewModels
{
    public class EquipmentEditViewModel : INotifyPropertyChanged
    {
        public EquipmentEditViewModel() : this(null) { }

        public EquipmentEditViewModel(Equipment equipment)
        {
            EditingEquipment = equipment != null
                ? new Equipment
                {
                    Id = equipment.Id,
                    SerialNumber = equipment.SerialNumber,
                    Model = equipment.Model,
                    EquipmentTypeId = equipment.EquipmentTypeId,
                    PurchaseDate = equipment.PurchaseDate,
                    Status = equipment.Status,
                    LastMaintenanceDate = equipment.LastMaintenanceDate,
                    NextMaintenanceDate = equipment.NextMaintenanceDate,
                    DecommissionDate = equipment.DecommissionDate
                }
                : new Equipment
                {
                    PurchaseDate = DateTime.Today,
                    Status = EquipmentStatus.InUse
                };

            try
            {
                using var ctx = new ApplicationDbContext();
                ctx.Database.EnsureCreated();
                EquipmentTypes = new ObservableCollection<EquipmentType>(ctx.EquipmentTypes.ToList());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке типов оборудования: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                EquipmentTypes = new ObservableCollection<EquipmentType>();
            }

            StatusValues = new ObservableCollection<StatusItem>(
                Enum.GetValues(typeof(EquipmentStatus))
                    .Cast<EquipmentStatus>()
                    .Select(s => new StatusItem
                    {
                        Value = s,
                        DisplayName = GetEnumDisplayName(s)
                    }));

            SelectedStatus = StatusValues.FirstOrDefault(s => s.Value == EditingEquipment.Status);

            SaveCommand = new RelayCommand<object>(SaveExecuted, _ => CanSave());
            CancelCommand = new RelayCommand<Window>(w => w.Close());
        }

        public Equipment EditingEquipment { get; }

        public ObservableCollection<EquipmentType> EquipmentTypes { get; }

        public ObservableCollection<StatusItem> StatusValues { get; }

        private StatusItem _selectedStatus;
        public StatusItem SelectedStatus
        {
            get => _selectedStatus;
            set
            {
                if (_selectedStatus != value)
                {
                    _selectedStatus = value;
                    EditingEquipment.Status = value?.Value ?? EquipmentStatus.InReserve;
                    OnPropertyChanged();
                }
            }
        }

        public int EquipmentTypeId
        {
            get => EditingEquipment.EquipmentTypeId;
            set
            {
                if (EditingEquipment.EquipmentTypeId != value)
                {
                    EditingEquipment.EquipmentTypeId = value;
                    OnPropertyChanged();
                    RecalculateNextMaintenanceDate();
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public DateTime PurchaseDate
        {
            get => EditingEquipment.PurchaseDate;
            set
            {
                if (EditingEquipment.PurchaseDate != value)
                {
                    EditingEquipment.PurchaseDate = value;
                    OnPropertyChanged();
                    RecalculateNextMaintenanceDate();
                }
            }
        }

        public DateTime? LastMaintenanceDate
        {
            get => EditingEquipment.LastMaintenanceDate;
            set
            {
                if (EditingEquipment.LastMaintenanceDate != value)
                {
                    EditingEquipment.LastMaintenanceDate = value;
                    OnPropertyChanged();
                }
            }
        }

        public DateTime? NextMaintenanceDate
        {
            get => EditingEquipment.NextMaintenanceDate;
            set
            {
                if (EditingEquipment.NextMaintenanceDate != value)
                {
                    EditingEquipment.NextMaintenanceDate = value;
                    OnPropertyChanged();
                }
            }
        }

        public DateTime? DecommissionDate
        {
            get => EditingEquipment.DecommissionDate;
            set
            {
                if (EditingEquipment.DecommissionDate != value)
                {
                    EditingEquipment.DecommissionDate = value;
                    OnPropertyChanged();
                }
            }
        }

        #region Commands

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        private bool CanSave()
            => !string.IsNullOrWhiteSpace(EditingEquipment.SerialNumber)
               && EquipmentTypeId > 0;

        private void RecalculateNextMaintenanceDate()
        {
            var type = EquipmentTypes.FirstOrDefault(t => t.Id == EquipmentTypeId);
            if (type != null)
                NextMaintenanceDate = PurchaseDate.AddDays(type.MaintenanceIntervalDays);
            else
                NextMaintenanceDate = null;
            OnPropertyChanged(nameof(NextMaintenanceDate));
        }

        private void SaveExecuted(object parameter)
        {
            try
            {
                using var ctx = new ApplicationDbContext();
                if (EditingEquipment.Id == 0)
                    ctx.Equipments.Add(EditingEquipment);
                else
                {
                    ctx.Equipments.Attach(EditingEquipment);
                    ctx.Entry(EditingEquipment).State = EntityState.Modified;
                }
                ctx.SaveChanges();
                if (parameter is Window win) win.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        #endregion

        private string GetEnumDisplayName(Enum enumValue)
        {
            var mem = enumValue.GetType().GetMember(enumValue.ToString()).FirstOrDefault();
            var attr = mem?.GetCustomAttribute<DisplayAttribute>();
            return attr?.Name ?? enumValue.ToString();
        }

        public class StatusItem
        {
            public EquipmentStatus Value { get; set; }
            public string DisplayName { get; set; }
            public override string ToString() => DisplayName;
        }

        private class RelayCommand<T> : ICommand
        {
            private readonly Action<T> _execute;
            private readonly Predicate<T> _canExecute;
            public RelayCommand(Action<T> exec, Predicate<T> canExec = null)
            {
                _execute = exec;
                _canExecute = canExec;
            }

            public bool CanExecute(object p) => _canExecute == null || _canExecute((T)p);
            public void Execute(object p) => _execute((T)p);
            public event EventHandler CanExecuteChanged
            {
                add => CommandManager.RequerySuggested += value;
                remove => CommandManager.RequerySuggested -= value;
            }
        }
    }
}