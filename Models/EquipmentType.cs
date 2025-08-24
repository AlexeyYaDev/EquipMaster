using System.ComponentModel.DataAnnotations;

namespace EquipMaster.Models
{
    public class EquipmentType
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } // Например: "Ноутбук", "Принтер", "Монитор"

        public string? Description { get; set; }

        [Required]
        [Range(1, 3650, ErrorMessage = "Интервал обслуживания должен быть в диапазоне от 1 до 3650 дней.")]
        public int MaintenanceIntervalDays { get; set; }
    }
}
