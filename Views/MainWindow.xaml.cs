using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using EquipMaster.Views.Assignment;
using EquipMaster.Views.Equipment;
using EquipMaster.Views.Maintenance;
using EquipMaster.Views.Users;
using EquipMaster.Views.Reports;
using EquipMaster.Models;
using System.Windows.Threading;
using Microsoft.EntityFrameworkCore;

namespace EquipMaster.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            ShowWelcomeContent();
        }

        private void ShowWelcomeContent()
        {
            var mainPanel = new StackPanel { Margin = new Thickness(20) };

            mainPanel.Children.Add(new TextBlock
            {
                Text = "Добро пожаловать в систему учёта оборудования EquipMaster!",
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.Black,
                Margin = new Thickness(0, 0, 0, 15)
            });

            mainPanel.Children.Add(new TextBlock
            {
                Text = "Обзор состояния оборудования",
                FontSize = 16,
                Foreground = Brushes.DimGray,
                Margin = new Thickness(0, 0, 0, 20)
            });

            using var context = new ApplicationDbContext();

            //  Напоминания о ТО (исключая списанное) 
            var upcoming = context.Equipments
                .Where(e => e.Status != EquipmentStatus.Decommissioned &&
                           e.NextMaintenanceDate.HasValue &&
                           e.NextMaintenanceDate.Value >= DateTime.Today &&
                           e.NextMaintenanceDate.Value <= DateTime.Today.AddDays(7))
                .OrderBy(e => e.NextMaintenanceDate)
                .ToList();

            if (upcoming.Any())
            {
                var border = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(255, 251, 234)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(255, 213, 128)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(10),
                    Padding = new Thickness(15),
                    Margin = new Thickness(0, 0, 0, 20)
                };

                var reminderStack = new StackPanel();

                reminderStack.Children.Add(new TextBlock
                {
                    Text = "📅 Ближайшее обслуживание оборудования",
                    FontSize = 18,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(166, 124, 0)),
                    Margin = new Thickness(0, 0, 0, 10)
                });

                foreach (var eq in upcoming)
                {
                    reminderStack.Children.Add(new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Margin = new Thickness(0, 5, 0, 0),
                        Children =
                        {
                            new TextBlock { Text = "🔧", FontSize = 16, Margin = new Thickness(0,0,5,0) },
                            new TextBlock { Text = $"{eq.SerialNumber} ({eq.Model ?? "без модели"}) — {eq.NextMaintenanceDate:dd.MM.yyyy}" }
                        }
                    });
                }

                var viewAllButton = new Button
                {
                    Content = "Посмотреть все напоминания",
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(0, 10, 0, 0),
                    Padding = new Thickness(10),
                    Cursor = System.Windows.Input.Cursors.Hand,
                    FontWeight = FontWeights.SemiBold,
                    Background = new SolidColorBrush(Color.FromRgb(68, 68, 68)),
                    Foreground = Brushes.White,
                    BorderBrush = Brushes.Transparent
                };
                viewAllButton.Click += OpenUpcomingMaintenance_Click;

                reminderStack.Children.Add(viewAllButton);
                border.Child = reminderStack;
                mainPanel.Children.Add(border);
            }

            // Метрики оборудования (исключая списанное) 
            var metricsBorder = new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(15),
                Margin = new Thickness(0, 10, 0, 0),
                BorderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                BorderThickness = new Thickness(1),
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
            for (int i = 0; i < 4; i++)
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            AddHeaderCell(grid, "Тип / Статус", 0, 0);
            AddHeaderCell(grid, "Всего", 1, 0);
            AddHeaderCell(grid, "В работе", 2, 0);
            AddHeaderCell(grid, "В резерве", 3, 0);
            AddHeaderCell(grid, "На обслуживании", 4, 0);
            AddHeaderCell(grid, "Пользователи", 5, 0, true);

            var equipmentTypes = context.EquipmentTypes.ToList();
            var allEquipments = context.Equipments
                .Where(e => e.Status != EquipmentStatus.Decommissioned)
                .ToList();

            var maintenanceIds = context.MaintenanceLogs
                .Where(m => m.Result == "В процессе")
                .Select(m => m.EquipmentId)
                .ToHashSet();

            var inUseIds = context.Assignments
                .Where(a => a.ReturnedAt == null)
                .Select(a => a.EquipmentId)
                .ToList()
                .Where(id => !maintenanceIds.Contains(id))
                .ToHashSet();

            int total = allEquipments.Count;
            int inMaintenance = maintenanceIds.Count(id => allEquipments.Any(e => e.Id == id));
            int inUse = inUseIds.Count(id => allEquipments.Any(e => e.Id == id));
            int inReserve = total - inMaintenance - inUse;

            // Получаем данные о пользователях
            int activeUsers = context.Users.Count(u => !u.IsBlocked);
            int blockedUsers = context.Users.Count(u => u.IsBlocked);
            int totalUsers = activeUsers + blockedUsers;

            AddDataRow(grid, "Оборудования всего", 1, total, inUse, inReserve, inMaintenance);

            int row = 2;
            foreach (var type in equipmentTypes)
            {
                var typeEquipments = allEquipments.Where(e => e.EquipmentTypeId == type.Id).ToList();
                var typeIds = typeEquipments.Select(e => e.Id).ToHashSet();
                int typeTotal = typeEquipments.Count;
                int typeInMaintenance = typeIds.Count(id => maintenanceIds.Contains(id));
                int typeInUse = typeIds.Count(id => inUseIds.Contains(id));
                int typeInReserve = typeTotal - typeInUse - typeInMaintenance;

                AddDataRow(grid, type.Name, row++, typeTotal, typeInUse, typeInReserve, typeInMaintenance);
            }

            AddTotalUsersCell(grid, activeUsers, blockedUsers, equipmentTypes.Count + 2);
            metricsBorder.Child = grid;
            mainPanel.Children.Add(metricsBorder);

            // Блок списанного оборудования 
            var decommissionedCount = context.Equipments
                .Count(e => e.Status == EquipmentStatus.Decommissioned);

            if (decommissionedCount > 0)
            {
                var decommissionedBorder = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(255, 240, 240)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(255, 180, 180)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(10),
                    Padding = new Thickness(15),
                    Margin = new Thickness(0, 20, 0, 0)
                };

                var decommissionedStack = new StackPanel();

                decommissionedStack.Children.Add(new TextBlock
                {
                    Text = "⚠️ Списанное оборудование",
                    FontSize = 18,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(166, 0, 0)),
                    Margin = new Thickness(0, 0, 0, 5)
                });

                decommissionedStack.Children.Add(new TextBlock
                {
                    Text = $"Всего списано единиц оборудования: {decommissionedCount}",
                    Foreground = Brushes.DimGray,
                    Margin = new Thickness(0, 0, 0, 10)
                });

                var viewButton = new Button
                {
                    Content = "Перейти к управлению оборудованием",
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(0, 10, 0, 0),
                    Padding = new Thickness(10),
                    Cursor = System.Windows.Input.Cursors.Hand,
                    FontWeight = FontWeights.SemiBold,
                    Background = new SolidColorBrush(Color.FromRgb(68, 68, 68)),
                    Foreground = Brushes.White,
                    BorderBrush = Brushes.Transparent
                };
                viewButton.Click += (s, e) =>
                {
                    var window = new EquipmentListWindow { Owner = this };
                    window.ShowDialog();
                };

                decommissionedStack.Children.Add(viewButton);
                decommissionedBorder.Child = decommissionedStack;
                mainPanel.Children.Add(decommissionedBorder);
            }

            MainContent.Content = mainPanel;
        }

        private void AddHeaderCell(Grid grid, string text, int column, int row, bool isUsersColumn = false)
        {
            var border = new Border
            {
                Background = isUsersColumn ?
                    new SolidColorBrush(Color.FromRgb(230, 240, 255)) :
                    new SolidColorBrush(Color.FromRgb(245, 245, 245)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(220, 220, 220)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(10, 5, 10, 5)
            };

            var textBlock = new TextBlock
            {
                Text = text,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Foreground = Brushes.Black
            };

            border.Child = textBlock;
            Grid.SetColumn(border, column);
            Grid.SetRow(border, row);
            grid.Children.Add(border);

            if (row == 0)
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        }

        private void AddDataRow(Grid grid, string typeName, int row, int total, int inUse, int inReserve, int inMaintenance)
        {
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            AddCell(grid, typeName, 0, row, HorizontalAlignment.Left);
            AddCell(grid, total.ToString(), 1, row);
            AddCell(grid, inUse.ToString(), 2, row);
            AddCell(grid, inReserve.ToString(), 3, row);
            AddCell(grid, inMaintenance.ToString(), 4, row);
        }

        private void AddCell(Grid grid, string text, int column, int row, HorizontalAlignment alignment = HorizontalAlignment.Center)
        {
            var border = new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromRgb(220, 220, 220)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(10, 5, 10, 5)
            };

            var textBlock = new TextBlock
            {
                Text = text,
                TextAlignment = alignment == HorizontalAlignment.Left ? TextAlignment.Left : TextAlignment.Center,
                Foreground = Brushes.Black
            };

            border.Child = textBlock;
            Grid.SetColumn(border, column);
            Grid.SetRow(border, row);
            grid.Children.Add(border);
        }

        private void AddTotalUsersCell(Grid grid, int activeUsers, int blockedUsers, int rowsCount)
        {
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(230, 240, 255)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(220, 220, 220)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(10)
            };

            var stack = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            // Основное число - активные пользователи
            stack.Children.Add(new TextBlock
            {
                Text = activeUsers.ToString(),
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Foreground = new SolidColorBrush(Color.FromRgb(30, 30, 30))
            });

            // Надпись: активных пользователей 
            stack.Children.Add(new TextBlock
            {
                Text = "активных пользователей",
                TextAlignment = TextAlignment.Center,
                Foreground = Brushes.DimGray,
                Margin = new Thickness(0, 5, 0, 0)
            });

            // Надпись: заблокированных пользователей 
            stack.Children.Add(new TextBlock
            {
                Text = $"заблокированных пользователей: {blockedUsers}",
                FontSize = 12,
                TextAlignment = TextAlignment.Center,
                Foreground = Brushes.Gray,
                Margin = new Thickness(0, 5, 0, 0)
            });

            // Кнопка управления пользователями
            var manageButton = new Button
            {
                Content = "Управление пользователями",
                Margin = new Thickness(0, 10, 0, 0),
                Padding = new Thickness(5),
                Background = Brushes.Transparent,
                BorderBrush = new SolidColorBrush(Color.FromRgb(100, 149, 237)),
                Foreground = new SolidColorBrush(Color.FromRgb(100, 149, 237)),
                Cursor = System.Windows.Input.Cursors.Hand,
                FontSize = 12
            };
            manageButton.Click += OpenUsers_Click;
            stack.Children.Add(manageButton);

            border.Child = stack;
            Grid.SetColumn(border, 5);
            Grid.SetRow(border, 1);
            Grid.SetRowSpan(border, rowsCount);
            grid.Children.Add(border);
        }

        private void OpenUsers_Click(object sender, RoutedEventArgs e) =>
            new UserListWindow { Owner = this }.ShowDialog();

        private void OpenEquipment_Click(object sender, RoutedEventArgs e) =>
            new EquipmentListWindow { Owner = this }.ShowDialog();

        private void OpenAssignment_Click(object sender, RoutedEventArgs e) =>
            new AssignmentWindow { Owner = this }.ShowDialog();

        private void OpenReturn_Click(object sender, RoutedEventArgs e) =>
            new ReturnEquipmentWindow { Owner = this }.ShowDialog();

        private void OpenMaintenance_Click(object sender, RoutedEventArgs e) =>
            new MaintenanceLogWindow { Owner = this }.ShowDialog();

        private void OpenAssignmentHistory_Click(object sender, RoutedEventArgs e) =>
            new AssignmentHistoryWindow { Owner = this }.ShowDialog();

        private void OpenLogEntries_Click(object sender, RoutedEventArgs e) =>
            new LogEntriesWindow { Owner = this }.ShowDialog();

        private void OpenUpcomingMaintenance_Click(object sender, RoutedEventArgs e) =>
            new UpcomingMaintenanceWindow { Owner = this }.ShowDialog();

        private void OpenReports_Click(object sender, RoutedEventArgs e) =>
            new ReportsWindow { Owner = this }.ShowDialog();

        private void Exit_Click(object sender, RoutedEventArgs e) => Close();

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            string helpText =
@"Добро пожаловать в EquipMaster!

— Используйте меню слева для перехода между разделами:
  • Пользователи — добавление и управление учетными записями сотрудников.
  • Оборудование — каталог техники с подробной информацией.
  • Выдача/возврат — оформление передачи и возврата оборудования ответственным.
  • Журнал обслуживания — планирование и запись проведённых технических работ.
  • Напоминания ТО — контроль и предупреждение о приближающихся сроках обслуживания.
  • Журнал действий — отслеживание всех системных операций.

...

Спасибо, что используете EquipMaster!";
            MessageBox.Show(helpText, "Справка по работе с программой", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void RefreshDashboard_Click(object sender, RoutedEventArgs e)
        {
            ShowWelcomeContent();
            (sender as Button).Content = "✓ Обновлено";
            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            timer.Tick += (s, args) =>
            {
                (sender as Button).Content = "🔄 Обновить Dashboard";
                timer.Stop();
            };
            timer.Start();
        }
    }
}