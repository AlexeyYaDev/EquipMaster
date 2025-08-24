using EquipMaster.Models;
using System.ComponentModel.DataAnnotations;

public class User
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Обязательное поле")]
    [StringLength(100, ErrorMessage = "Максимум 100 символов")]
    public string FullName { get; set; }

    [Required(ErrorMessage = "Обязательное поле")]
    [StringLength(50, ErrorMessage = "Максимум 50 символов")]
    public string Department { get; set; }

    [StringLength(20)]
    public string PersonnelNumber { get; set; }

    public bool IsBlocked { get; set; } //  флаг блокировки

    public ICollection<Assignment> AssignedEquipment { get; set; } = new List<Assignment>();
}
