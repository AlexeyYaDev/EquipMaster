using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace EquipMaster.Models
{
    public class Equipment
    {
        public int Id { get; set; }

        [Required]
        public string SerialNumber { get; set; }

        public string Model { get; set; }

        [Required]
        public int EquipmentTypeId { get; set; }
        public EquipmentType EquipmentType { get; set; }

        public DateTime PurchaseDate { get; set; } = DateTime.Today;

        public EquipmentStatus Status { get; set; } = EquipmentStatus.InReserve;

        public DateTime? LastMaintenanceDate { get; set; }  

        public DateTime? NextMaintenanceDate { get; set; }

        // Новое свойство для даты списания (ручной ввод)
        public DateTime? DecommissionDate { get; set; }

        public List<Assignment> Assignments { get; set; } = new();
        public List<MaintenanceLog> MaintenanceLogs { get; set; } = new();

        public string StatusDisplay
        {
            get
            {
                var type = typeof(EquipmentStatus);
                var memInfo = type.GetMember(Status.ToString());
                if (memInfo.Length > 0)
                {
                    var attr = memInfo[0].GetCustomAttribute<DisplayAttribute>();
                    if (attr != null)
                        return attr.Name;
                }
                return Status.ToString();
            }
        }

        // Метод для обновления дат обслуживания
        public void UpdateMaintenanceDates(DateTime maintenanceDate, TimeSpan maintenanceInterval)
        {
            LastMaintenanceDate = maintenanceDate;
            NextMaintenanceDate = maintenanceDate.Add(maintenanceInterval);
        }
    }

    public enum EquipmentStatus
    {
        [Display(Name = "В использовании")]
        InUse,

        [Display(Name = "В резерве")]
        InReserve,

        [Display(Name = "На обслуживании")]
        UnderMaintenance,

        [Display(Name = "Списано")]
        Decommissioned
    }
}