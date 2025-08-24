using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Microsoft.EntityFrameworkCore; 
using EquipMaster.Models;

namespace EquipMaster.ViewModels
{
    public class UpcomingMaintenanceViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<Equipment> UpcomingMaintenances { get; set; } = new();

        public UpcomingMaintenanceViewModel()
        {
            LoadUpcomingMaintenances();
        }

        private void LoadUpcomingMaintenances()
        {
            using var context = new ApplicationDbContext();

            var upcoming = context.Equipments
                .Include(e => e.EquipmentType) // загружаем связанные типы
                .Where(e => e.NextMaintenanceDate.HasValue &&
                            e.NextMaintenanceDate.Value >= DateTime.Today &&
                            e.NextMaintenanceDate.Value <= DateTime.Today.AddDays(7))
                .OrderBy(e => e.NextMaintenanceDate)
                .ToList();

            UpcomingMaintenances.Clear();
            foreach (var item in upcoming)
                UpcomingMaintenances.Add(item);

            OnPropertyChanged(nameof(UpcomingMaintenances));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
