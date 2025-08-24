using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.EntityFrameworkCore;
using EquipMaster.Models;
using EquipMaster.Views.Reports;
using Microsoft.Win32;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;

namespace EquipMaster.Views
{
    public partial class ReportViewerWindow : Window
    {
        private IEnumerable<object> _originalItems;
        private string _datePropertyPath;
        private ReportType _reportType;

        public ReportViewerWindow(ReportType reportType)
        {
            InitializeComponent();
            _reportType = reportType;

            FromDatePicker.SelectedDate = DateTime.Today.AddMonths(-1);
            ToDatePicker.SelectedDate = DateTime.Today;

            Loaded += ReportViewerWindow_Loaded;
        }

        private void ReportViewerWindow_Loaded(object sender, RoutedEventArgs e)
        {
            AssignmentFiltersPanel.Visibility = Visibility.Collapsed;
            UsersFiltersPanel.Visibility = Visibility.Collapsed;
            EquipmentStatusFiltersPanel.Visibility = Visibility.Collapsed;
            MaintenanceHistoryFiltersPanel.Visibility = Visibility.Collapsed;

            switch (_reportType)
            {
                case ReportType.AssignmentReport:
                    SetupAssignmentReport(); break;
                case ReportType.EquipmentStatus:
                    SetupEquipmentStatusReport(); break;
                case ReportType.MaintenanceHistory:
                    SetupMaintenanceHistoryReport(); break;
                case ReportType.UpcomingMaintenance:
                    SetupUpcomingMaintenanceReport(); break;
                case ReportType.UserActivityLog:
                    SetupUserActivityLogReport(); break;
                case ReportType.UsersList:
                    SetupUsersListReport(); break;
            }
        }

        #region Setup Reports

        private void SetupAssignmentReport()
        {
            Title = "Выдачи и возвраты оборудования";
            AssignmentFiltersPanel.Visibility = Visibility.Visible;

            using var ctx = new ApplicationDbContext();
            UserComboBox.ItemsSource = ctx.Users.OrderBy(u => u.FullName).ToList();
            EquipmentComboBox.ItemsSource = ctx.Equipments.OrderBy(e => e.SerialNumber).ToList();

            _datePropertyPath = "AssignedAt";
            LoadAssignmentData();
        }

        private void LoadAssignmentData()
        {
            using var ctx = new ApplicationDbContext();
            var q = ctx.Assignments
                       .Include(a => a.Equipment).ThenInclude(e => e.EquipmentType)
                       .Include(a => a.User)
                       .AsQueryable();

            if (UserComboBox.SelectedItem is User u) // Убрал Models.
                q = q.Where(a => a.UserId == u.Id);
            if (EquipmentComboBox.SelectedItem is Models.Equipment eq)
                q = q.Where(a => a.EquipmentId == eq.Id);

            if (ReturnedFilterComboBox.SelectedIndex == 1) // Только возвращённые
                q = q.Where(a => a.ReturnedAt != null);
            else if (ReturnedFilterComboBox.SelectedIndex == 2) // Только не возвращённые
                q = q.Where(a => a.ReturnedAt == null);

            if (FromDatePicker.SelectedDate.HasValue)
                q = q.Where(a => a.AssignedAt >= FromDatePicker.SelectedDate.Value.Date);
            if (ToDatePicker.SelectedDate.HasValue)
                q = q.Where(a => a.AssignedAt <= ToDatePicker.SelectedDate.Value.Date.AddDays(1).AddTicks(-1));

            var items = q.OrderByDescending(a => a.AssignedAt).ToList();

            SetColumns(
                ("Оборудование", "Equipment.SerialNumber"),
                ("Тип оборудования", "Equipment.EquipmentType.Name"),
                ("Пользователь", "User.FullName"),
                ("Дата выдачи", "AssignedAt"),
                ("Дата возврата", "ReturnedAt"),
                ("Заметки", "AssignmentNotes"),
                ("Заметки при возврате", "ReturnNotes")
            );

            SetItems(items, _datePropertyPath);
        }

        private void SetupEquipmentStatusReport()
        {
            Title = "Состояние оборудования";
            EquipmentStatusFiltersPanel.Visibility = Visibility.Visible;

            using var ctx = new ApplicationDbContext();
            EquipmentTypeComboBox.ItemsSource = ctx.EquipmentTypes.OrderBy(et => et.Name).ToList();

            // Устанавливаем правильные значения для фильтра статусов
            EquipmentStatusComboBox.Items.Clear();
            EquipmentStatusComboBox.Items.Add("Все");
            EquipmentStatusComboBox.Items.Add("В использовании");
            EquipmentStatusComboBox.Items.Add("В резерве");
            EquipmentStatusComboBox.Items.Add("На обслуживании");
            EquipmentStatusComboBox.Items.Add("Списано");
            EquipmentStatusComboBox.SelectedIndex = 0;

            _datePropertyPath = null;
            LoadEquipmentStatusData();
        }

        private void LoadEquipmentStatusData()
        {
            using var ctx = new ApplicationDbContext();
            var q = ctx.Equipments
                       .Include(e => e.EquipmentType)
                       .Include(e => e.Assignments)
                           .ThenInclude(a => a.User)
                       .AsQueryable();

            if (EquipmentStatusComboBox.SelectedIndex > 0)
            {
                var selectedStatus = (EquipmentStatus)(EquipmentStatusComboBox.SelectedIndex - 1);
                q = q.Where(e => e.Status == selectedStatus);
            }

            if (EquipmentTypeComboBox.SelectedItem is EquipmentType type)
                q = q.Where(e => e.EquipmentTypeId == type.Id);

            if (LastMaintenanceFilterComboBox.SelectedIndex == 1) // Есть дата
                q = q.Where(e => e.LastMaintenanceDate != null);
            else if (LastMaintenanceFilterComboBox.SelectedIndex == 2) // Нет даты
                q = q.Where(e => e.LastMaintenanceDate == null);

            var items = q.OrderBy(e => e.SerialNumber).ToList();

            // Создаем анонимный тип с нужными свойствами, включая текущего пользователя
            var result = items.Select(e => new
            {
                e.Id,
                e.SerialNumber,
                e.Model,
                EquipmentTypeName = e.EquipmentType?.Name,
                e.StatusDisplay,
                CurrentUser = e.Assignments.FirstOrDefault(a => a.ReturnedAt == null)?.User?.FullName,
                LastMaintenanceDate = e.LastMaintenanceDate,
                NextMaintenanceDate = e.NextMaintenanceDate,
                e.DecommissionDate
            }).ToList();

            SetColumns(
                ("Серийный номер", "SerialNumber"),
                ("Модель", "Model"),
                ("Тип", "EquipmentTypeName"),
                ("Статус", "StatusDisplay"),
                ("Текущий пользователь", "CurrentUser"),
                ("Дата последнего ТО", "LastMaintenanceDate"),
                ("Дата следующего ТО", "NextMaintenanceDate"),
                ("Дата списания", "DecommissionDate")
            );

            SetItems(result);
        }

        private void SetupMaintenanceHistoryReport()
        {
            Title = "История обслуживания";
            MaintenanceHistoryFiltersPanel.Visibility = Visibility.Visible;

            using var ctx = new ApplicationDbContext();
            MaintenanceEquipmentTypeComboBox.ItemsSource = ctx.EquipmentTypes.OrderBy(et => et.Name).ToList();

            // Заполняем комбобоксы уникальными значениями из базы
            PerformedByComboBox.ItemsSource = ctx.MaintenanceLogs
                .Select(m => m.PerformedBy)
                .Distinct()
                .OrderBy(p => p)
                .ToList();

            ResultComboBox.ItemsSource = ctx.MaintenanceLogs
                .Select(m => m.Result)
                .Distinct()
                .OrderBy(r => r)
                .ToList();

            _datePropertyPath = "Date";
            LoadMaintenanceHistoryData();
        }


        private void LoadMaintenanceHistoryData()
        {
            using var ctx = new ApplicationDbContext();
            var q = ctx.MaintenanceLogs
                       .Include(m => m.Equipment).ThenInclude(e => e.EquipmentType)
                       .AsQueryable();

            if (MaintenanceEquipmentTypeComboBox.SelectedItem is EquipmentType type)
                q = q.Where(m => m.Equipment.EquipmentTypeId == type.Id);

            if (!string.IsNullOrWhiteSpace(PerformedByComboBox.Text))
                q = q.Where(m => m.PerformedBy.Contains(PerformedByComboBox.Text));

            if (!string.IsNullOrWhiteSpace(ResultComboBox.Text))
                q = q.Where(m => m.Result.Contains(ResultComboBox.Text));

            if (FromDatePicker.SelectedDate.HasValue)
                q = q.Where(m => m.Date >= FromDatePicker.SelectedDate.Value.Date);
            if (ToDatePicker.SelectedDate.HasValue)
                q = q.Where(m => m.Date <= ToDatePicker.SelectedDate.Value.Date.AddDays(1).AddTicks(-1));

            var items = q.OrderByDescending(m => m.Date).ToList();

            SetColumns(
                ("Оборудование", "Equipment.SerialNumber"),
                ("Тип", "Equipment.EquipmentType.Name"),
                ("Дата ТО", "Date"),
                ("Описание", "Description"),
                ("Исполнитель", "PerformedBy"),
                ("Результат", "Result")
            );

            SetItems(items, _datePropertyPath);
        }

        private void SetupUpcomingMaintenanceReport()
        {
            Title = "Сводка по приближающимся ТО";
            _datePropertyPath = "NextMaintenanceDate";

            using var ctx = new ApplicationDbContext();
            var threshold = DateTime.Now.AddDays(14);
            var items = ctx.Equipments
                           .Include(e => e.EquipmentType)
                           .Where(e => e.NextMaintenanceDate != null && e.NextMaintenanceDate <= threshold)
                           .OrderBy(e => e.NextMaintenanceDate)
                           .ToList();

            SetColumns(
                ("Серийный номер", "SerialNumber"),
                ("Модель", "Model"),
                ("Тип", "EquipmentType.Name"),
                ("Статус", "StatusDisplay"),
                ("Последнее ТО", "LastMaintenanceDate"),
                ("Следующее ТО", "NextMaintenanceDate")
            );

            SetItems(items, _datePropertyPath);
        }

        private void SetupUserActivityLogReport()
        {
            Title = "Журнал действий пользователей";
            _datePropertyPath = "Timestamp";

            using var ctx = new ApplicationDbContext();
            var items = ctx.LogEntries.OrderByDescending(l => l.Timestamp).ToList();

            SetColumns(
                ("Дата и время", "Timestamp"),
                ("Пользователь", "Username"),
                ("Действие", "DisplayAction"),
                ("Объект", "DisplayEntityName"),
                ("Описание", "Details")
            );

            SetItems(items, _datePropertyPath);
        }

        private void SetupUsersListReport()
        {
            Title = "Список пользователей";
            UsersFiltersPanel.Visibility = Visibility.Visible;
            _datePropertyPath = null;

            using var ctx = new ApplicationDbContext();
            var users = ctx.Users.OrderBy(u => u.FullName).ToList();

            // Создаем колонки вручную, чтобы для IsBlocked использовать DataGridCheckBoxColumn
            ReportDataGrid.Columns.Clear();

            // Колонка ФИО
            ReportDataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "ФИО",
                Binding = new Binding("FullName")
            });

            // Колонка Подразделение
            ReportDataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Подразделение",
                Binding = new Binding("Department")
            });

            // Колонка Табельный номер
            ReportDataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Табельный номер",
                Binding = new Binding("PersonnelNumber")
            });

            // Колонка Заблокирован - теперь с чекбоксом
            ReportDataGrid.Columns.Add(new DataGridCheckBoxColumn
            {
                Header = "Заблокирован",
                Binding = new Binding("IsBlocked"),
                IsThreeState = false
            });

            SetItems(users);
        }

        #endregion

        #region Универсальные методы для ReportViewerWindow

        public void SetColumns(IEnumerable<(string Header, string PropertyPath)> columns)
        {
            ReportDataGrid.Columns.Clear();
            foreach (var (header, path) in columns)
            {
                // Проверяем тип данных свойства (для булевых значений)
                var firstItem = (ReportDataGrid.ItemsSource as IEnumerable<object>)?.FirstOrDefault();
                if (firstItem != null)
                {
                    var prop = firstItem.GetType().GetProperty(path);
                    if (prop != null && prop.PropertyType == typeof(bool))
                    {
                        // Создаем колонку с чекбоксом для булевых значений
                        var checkBoxCol = new DataGridCheckBoxColumn
                        {
                            Header = header,
                            Binding = new Binding(path),
                            IsThreeState = false
                        };
                        ReportDataGrid.Columns.Add(checkBoxCol);
                        continue;
                    }
                }

                // Стандартная текстовая колонка
                var col = new DataGridTextColumn
                {
                    Header = header,
                    Binding = new Binding(path)
                };

                // Форматирование дат
                if (path.Contains("Date") || path.Contains("Timestamp") || header.Contains("Дата"))
                {
                    ((Binding)col.Binding).StringFormat = "dd.MM.yyyy";

                    // Для даты+время используем полный формат
                    if (path == "AssignedAt" || path == "ReturnedAt" || path == "Timestamp")
                        ((Binding)col.Binding).StringFormat = "dd.MM.yyyy HH:mm";
                }

                ReportDataGrid.Columns.Add(col);
            }
        }

        public void SetColumns(params (string Header, string PropertyPath)[] columns)
            => SetColumns((IEnumerable<(string Header, string PropertyPath)>)columns);

        public void SetItems(IEnumerable<object> items, string datePropertyPath = null)
        {
            _originalItems = items.ToList();
            _datePropertyPath = datePropertyPath;
            ReportDataGrid.ItemsSource = _originalItems;
            FromDatePicker.IsEnabled = ToDatePicker.IsEnabled = !string.IsNullOrEmpty(_datePropertyPath);
        }

        private void ApplyFilter_Click(object sender, RoutedEventArgs e)
        {
            switch (_reportType)
            {
                case ReportType.AssignmentReport:
                    LoadAssignmentData(); break;
                case ReportType.EquipmentStatus:
                    LoadEquipmentStatusData(); break;
                case ReportType.MaintenanceHistory:
                    LoadMaintenanceHistoryData(); break;
                case ReportType.UsersList:
                    ApplyUsersListFilter(); break;
                default:
                    // Для других отчетов просто обновляем данные с учетом дат
                    ReportDataGrid.ItemsSource = FilterByDate(_originalItems, _datePropertyPath);
                    break;
            }
        }

        private void ClearFilter_Click(object sender, RoutedEventArgs e)
        {
            // Сброс общих фильтров по дате
            FromDatePicker.SelectedDate = null;
            ToDatePicker.SelectedDate = null;

            // Сброс специфичных фильтров по типу отчёта
            switch (_reportType)
            {
                case ReportType.AssignmentReport:
                    UserComboBox.SelectedItem = null;
                    EquipmentComboBox.SelectedItem = null;
                    ReturnedFilterComboBox.SelectedIndex = 0;
                    LoadAssignmentData();
                    break;

                case ReportType.EquipmentStatus:
                    EquipmentStatusComboBox.SelectedIndex = 0;
                    EquipmentTypeComboBox.SelectedItem = null;
                    LastMaintenanceFilterComboBox.SelectedIndex = 0;
                    LoadEquipmentStatusData();
                    break;

                case ReportType.MaintenanceHistory:
                    MaintenanceEquipmentTypeComboBox.SelectedItem = null;
                    PerformedByComboBox.Text = string.Empty;
                    ResultComboBox.Text = string.Empty;
                    LoadMaintenanceHistoryData();
                    break;

                case ReportType.UsersList:
                    UserStatusComboBox.SelectedIndex = 0;
                    ApplyUsersListFilter();
                    break;

                case ReportType.UpcomingMaintenance:
                case ReportType.UserActivityLog:
                    // Для этих отчётов нет дополнительных фильтров — возвращаем исходные данные
                    ReportDataGrid.ItemsSource = _originalItems;
                    break;
            }
        }
        private IEnumerable<object> FilterByDate(IEnumerable<object> items, string dateProperty)
        {
            if (string.IsNullOrEmpty(dateProperty) || items == null || !items.Any())
                return items;

            var fromDate = FromDatePicker.SelectedDate;
            var toDate = ToDatePicker.SelectedDate;

            if (!fromDate.HasValue && !toDate.HasValue)
                return items;

            return items.Where(item =>
            {
                var prop = item.GetType().GetProperty(dateProperty);
                if (prop == null) return true;

                var value = prop.GetValue(item);
                if (value == null) return false;

                if (value is DateTime dateValue)
                {
                    if (fromDate.HasValue && dateValue < fromDate.Value.Date) return false;
                    if (toDate.HasValue && dateValue > toDate.Value.Date.AddDays(1).AddTicks(-1)) return false;
                    return true;
                }

                return true;
            });
        }

        private void ApplyUsersListFilter()
        {
            var all = _originalItems.Cast<User>();
            var sel = (UserStatusComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString();
            var list = sel switch
            {
                "Активные" => all.Where(u => !u.IsBlocked),
                "Заблокированные" => all.Where(u => u.IsBlocked),
                _ => all
            };
            ReportDataGrid.ItemsSource = list.ToList();
        }

        private void ExportToCsv_Click(object sender, RoutedEventArgs e)
        {
            var items = ReportDataGrid.ItemsSource as IEnumerable<object>;
            if (items == null || !items.Any())
            {
                MessageBox.Show("Нет данных для экспорта", "Экспорт",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var saveFileDialog = new SaveFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                FileName = $"{Title}_{DateTime.Now:yyyyMMdd_HHmmss}.csv",
                DefaultExt = ".csv"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    using (var writer = new StreamWriter(saveFileDialog.FileName, false, Encoding.UTF8))
                    {
                        // Запись заголовков
                        var headers = ReportDataGrid.Columns
                            .Where(col => col.Visibility == Visibility.Visible)
                            .Select(col => EscapeCsvField(col.Header?.ToString() ?? ""));

                        writer.WriteLine(string.Join(";", headers));

                        // Запись данных
                        foreach (var item in items)
                        {
                            var rowValues = new List<string>();
                            foreach (var column in ReportDataGrid.Columns.Where(c => c.Visibility == Visibility.Visible))
                            {
                                string value = GetCellValue(item, column);
                                rowValues.Add(EscapeCsvField(value));
                            }
                            writer.WriteLine(string.Join(";", rowValues));
                        }
                    }
                    MessageBox.Show("Данные успешно экспортированы", "Экспорт",
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при экспорте: {ex.Message}", "Ошибка",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // Вспомогательные методы (добавить в тот же класс):
        private string GetCellValue(object item, DataGridColumn column)
        {
            if (column is DataGridBoundColumn boundColumn)
            {
                var binding = boundColumn.Binding as Binding;
                if (binding?.Path != null)
                {
                    object value = GetPropertyValue(item, binding.Path.Path);
                    return FormatValue(value, binding.StringFormat);
                }
            }
            return string.Empty;
        }

        private object GetPropertyValue(object obj, string path)
        {
            if (string.IsNullOrEmpty(path)) return null;

            foreach (string part in path.Split('.'))
            {
                if (obj == null) return null;

                PropertyInfo prop = obj.GetType().GetProperty(part);
                if (prop == null) return null;

                obj = prop.GetValue(obj);
            }
            return obj;
        }

        private string FormatValue(object value, string format)
        {
            if (value == null) return string.Empty;

            if (value is DateTime date)
            {
                return date.ToString(string.IsNullOrEmpty(format)
                    ? "dd.MM.yyyy"
                    : format.Replace("HH:mm", "HH:mm:ss"));
            }

            if (value is bool boolValue)
            {
                return boolValue ? "Да" : "Нет";
            }

            return string.Format(CultureInfo.CurrentCulture,
                                string.IsNullOrEmpty(format) ? "{0}" : format,
                                value);
        }

        private string EscapeCsvField(string data)
        {
            if (string.IsNullOrEmpty(data)) return "\"\"";

            data = data.Replace("\"", "\"\"")
                       .Replace("\r\n", " ")
                       .Replace("\n", " ")
                       .Replace("\r", " ");

            return data.Contains(";") || data.Contains("\"")
                ? $"\"{data}\""
                : data;
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();

        #endregion
    }
}

public enum ReportType
    {
        AssignmentReport,
        EquipmentStatus,
        MaintenanceHistory,
        UpcomingMaintenance,
        UserActivityLog,
        UsersList
    }
