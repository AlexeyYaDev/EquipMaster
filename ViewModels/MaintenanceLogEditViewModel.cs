using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using EquipMaster.Models;
using EquipMaster.ViewModels.Commands;
using Microsoft.EntityFrameworkCore;

namespace EquipMaster.ViewModels.Maintenance
{
    public class MaintenanceLogEditViewModel : INotifyPropertyChanged
    {
        private readonly ApplicationDbContext _context;
        private ObservableCollection<Equipment> _equipments;
        private Equipment _selectedEquipment;

        // Конструктор для создания новой записи ТО
        public MaintenanceLogEditViewModel()
        {
            _context = new ApplicationDbContext();
            LoadEquipments();

            EditingLog = new MaintenanceLog
            {
                Date = DateTime.Today,
                MaintenanceType = "Плановое",
                Result = "В процессе"  // По умолчанию в ожидании
            };

            SaveCommand = new RelayCommand<Window>(SaveExecuted, CanSave);
            CancelCommand = new RelayCommand<Window>(w => w?.Close());
        }

        // Конструктор для редактирования существующей записи ТО
        public MaintenanceLogEditViewModel(MaintenanceLog existingLog) : this()
        {
            // Копируем данные, чтобы не мутировать оригинал до сохранения
            EditingLog = new MaintenanceLog
            {
                Id = existingLog.Id,
                EquipmentId = existingLog.EquipmentId,
                Date = existingLog.Date,
                MaintenanceType = existingLog.MaintenanceType,
                Result = existingLog.Result,
                PerformedBy = existingLog.PerformedBy,
                Description = existingLog.Description,
                NextMaintenanceDate = existingLog.NextMaintenanceDate
            };

            SelectedEquipment = Equipments.FirstOrDefault(e => e.Id == existingLog.EquipmentId);
        }

        public MaintenanceLog EditingLog { get; set; }

        public ObservableCollection<Equipment> Equipments
        {
            get => _equipments;
            set
            {
                _equipments = value;
                OnPropertyChanged(nameof(Equipments));
            }
        }

        public Equipment SelectedEquipment
        {
            get => _selectedEquipment;
            set
            {
                _selectedEquipment = value;
                OnPropertyChanged(nameof(SelectedEquipment));
                if (value != null)
                    EditingLog.EquipmentId = value.Id;
            }
        }

        public ObservableCollection<string> MaintenanceTypes { get; } =
            new ObservableCollection<string> { "Плановое", "Внеплановое", "Ремонт" };

        public ObservableCollection<string> ResultOptions { get; } =
            new ObservableCollection<string> { "В процессе", "Успешно", "Требуется замена", "Неудача" };

        public RelayCommand<Window> SaveCommand { get; }
        public RelayCommand<Window> CancelCommand { get; }

        private bool CanSave(Window window)
        {
            return SelectedEquipment != null
                && !string.IsNullOrWhiteSpace(EditingLog.PerformedBy)
                && !string.IsNullOrWhiteSpace(EditingLog.MaintenanceType)
                && !string.IsNullOrWhiteSpace(EditingLog.Result)
                && EditingLog.Date != default;
        }

        private void SaveExecuted(Window window)
        {
            try
            {
                var equipment = _context.Equipments
                    .Include(e => e.EquipmentType)
                    .FirstOrDefault(e => e.Id == EditingLog.EquipmentId)
                    ?? throw new InvalidOperationException("Оборудование не найдено.");

                // Проверка что дата ТО не в будущем
                if (EditingLog.Date > DateTime.Now)
                {
                    MessageBox.Show("Дата ТО не может быть в будущем", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Проверка что оборудование не списано
                if (equipment.Status == EquipmentStatus.Decommissioned)
                {
                    MessageBox.Show("Нельзя выполнить ТО для списанного оборудования", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Обновляем даты обслуживания
                equipment.LastMaintenanceDate = EditingLog.Date; // Устанавливаем дату последнего ТО

                // Рассчитываем NextMaintenanceDate если указан интервал обслуживания
                if (equipment.EquipmentType?.MaintenanceIntervalDays > 0)
                {
                    EditingLog.NextMaintenanceDate =
                        EditingLog.Date.AddDays(equipment.EquipmentType.MaintenanceIntervalDays);
                    equipment.NextMaintenanceDate = EditingLog.NextMaintenanceDate;
                }

                // Устанавливаем статус оборудования в зависимости от результата ТО
                switch (EditingLog.Result)
                {
                    case "В процессе":
                        equipment.Status = EquipmentStatus.UnderMaintenance;
                        break;
                    case "Успешно":
                        equipment.Status = EquipmentStatus.InReserve;
                        break;
                    case "Требуется замена":
                        equipment.Status = EquipmentStatus.InReserve;
                        // Можно добавить дополнительную логику для пометки оборудования к замене
                        break;
                    case "Неудача":
                        equipment.Status = EquipmentStatus.Decommissioned;
                        equipment.DecommissionDate = DateTime.Now; // Автоматически списываем при неудачном ТО
                        break;
                    default:
                        equipment.Status = EquipmentStatus.InReserve;
                        break;
                }

                if (EditingLog.Id == 0)
                    _context.MaintenanceLogs.Add(EditingLog);
                else
                    _context.MaintenanceLogs.Update(EditingLog);

                _context.SaveChanges();

                window.DialogResult = true;
                window.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadEquipments()
        {
            var list = _context.Equipments
                               .Include(e => e.EquipmentType)
                               .ToList();
            Equipments = new ObservableCollection<Equipment>(list);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string prop) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }
}