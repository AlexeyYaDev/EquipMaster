using System.ComponentModel.DataAnnotations;

public class LogEntry
{
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Action { get; set; } = "Unknown";

    [Required]
    [MaxLength(50)]
    public string Username { get; set; }

    [Required]
    public string EntityName { get; set; }

    public string Details { get; set; } = "No details provided";

    public DateTime Timestamp { get; set; }

    public string DisplayAction
    {
        get
        {
            return Action switch
            {
                "Create" => "Создание",
                "Update" => "Изменение",
                "Delete" => "Удаление",
                "Maintenance" => "Обслуживание",
                "Assignment" => "Выдача",
                "Return" => "Возврат",
                _ => Action
            };
        }
    }

    public string DisplayEntityName
    {
        get
        {
            return EntityName switch
            {
                "Equipment" => "Оборудование",
                "Assignment" => "Выдача",
                "MaintenanceLog" => "ТО",
                "User" => "Пользователь",
                _ => EntityName
            };
        }
    }
}
