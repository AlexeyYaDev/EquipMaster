using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EquipMaster.Models
{
    public class MaintenanceLog
    {
        public int Id { get; set; }

        [Required]
        public int EquipmentId { get; set; }

        [ForeignKey(nameof(EquipmentId))]
        public Equipment Equipment { get; set; }

        [Required]
        [StringLength(100)]
        public string PerformedBy { get; set; }  // ФИО исполнителя или название сервиса

        [Required]
        public DateTime Date { get; set; } = DateTime.Now;

        [Required]
        [StringLength(50)]
        public string MaintenanceType { get; set; }  // "Плановое", "Внеплановое", "Ремонт"

        [StringLength(1000)]
        public string Description { get; set; }

        public decimal? Cost { get; set; }  // Стоимость обслуживания

        [Required]
        public DateTime NextMaintenanceDate { get; set; }  // Дата следующего ТО

        [StringLength(100)]
        public string Result { get; set; }  // "Успешно", "Требуется замена", etc.

        // Вычисляемое свойство (не сохраняется в БД)
        [NotMapped]
        public bool IsOverdue => NextMaintenanceDate < DateTime.Now;
    }
}