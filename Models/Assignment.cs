using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EquipMaster.Models
{
    public class Assignment
    {
        public int Id { get; set; }

        [Required]
        public int EquipmentId { get; set; }

        [ForeignKey(nameof(EquipmentId))]
        public Equipment Equipment { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User User { get; set; }

        [Required]
        public DateTime AssignedAt { get; set; } = DateTime.Now;

        public DateTime? ReturnedAt { get; set; }

        [StringLength(500)]
        public string AssignmentNotes { get; set; }

        [StringLength(500)]
        public string ReturnNotes { get; set; }

        [NotMapped]
        public bool IsActive => ReturnedAt == null;

        [NotMapped]
        public string DisplayInfo => $"Пользователь: {User?.FullName}, С/Н: {Equipment?.SerialNumber}";
    }
}
