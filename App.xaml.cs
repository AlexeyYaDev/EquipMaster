using System;
using System.Linq;
using System.Windows;
using EquipMaster.Models;
using Microsoft.EntityFrameworkCore;

namespace EquipMaster
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            using (var context = new ApplicationDbContext())
            {
                try
                {
                    // Применяем миграции к базе данных
                    context.Database.Migrate();

                    // Проверяем, есть ли записи в таблице LogEntries
                    if (!context.LogEntries.Any())
                    {
                        // Если в таблице LogEntries нет данных, можно добавить логику для обработки этого случая

                    }
                }
                catch (Exception ex)
                {
                    // Логируем или отображаем ошибку
                    MessageBox.Show($"Ошибка инициализации базы данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    Shutdown(); // Завершаем приложение при ошибке
                }
            }
        }
    }
}
