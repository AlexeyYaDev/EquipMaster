using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EquipMaster.Models
{
    public class ApplicationDbContext : DbContext
    {
        private readonly string _currentUsername;
        private bool _logSuppressed;

        // Пустой конструктор для EF Core и миграций
        public ApplicationDbContext() { }

        // Конструктор для конфигурации с опциями (EF Core tooling)
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        // Конструктор для рантайма с передачей имени пользователя
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, string currentUsername)
            : base(options)
        {
            _currentUsername = currentUsername;
        }

        public DbSet<LogEntry> LogEntries { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Equipment> Equipments { get; set; }
        public DbSet<EquipmentType> EquipmentTypes { get; set; }
        public DbSet<Assignment> Assignments { get; set; }
        public DbSet<MaintenanceLog> MaintenanceLogs { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer(
                    @"Server=(localdb)\MSSQLLocalDB;Database=EquipMaster;Trusted_Connection=True;",
                    opts => opts
                        .EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null)
                        .CommandTimeout(30));
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Конфигурация LogEntry
            modelBuilder.Entity<LogEntry>(entity =>
            {
                entity.HasKey(l => l.Id);
                entity.Property(l => l.Timestamp)
                    .HasDefaultValueSql("GETDATE()");

                entity.Property(l => l.Action)
                    .IsRequired()
                    .HasMaxLength(50)
                    .HasDefaultValue("Unknown");

                entity.Property(l => l.Username)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(l => l.EntityName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(l => l.Details)
                    .HasMaxLength(2000) // Увеличим максимальную длину
                    .HasDefaultValue("No details provided");
            });

            // Конфигурация User
            modelBuilder.Entity<User>(entity =>
            {
                // Ваша текущая конфигурация User
            });

            // Конфигурация EquipmentType
            modelBuilder.Entity<EquipmentType>(entity =>
            {
                entity.HasIndex(et => et.Name).IsUnique();
                entity.Property(et => et.Name)
                      .IsRequired()
                      .HasMaxLength(100);
                entity.Property(et => et.MaintenanceIntervalDays)
                      .IsRequired();

                // Начальное заполнение таблицы EquipmentType
                entity.HasData(
                    new EquipmentType { Id = 1, Name = "Персональный компьютер", MaintenanceIntervalDays = 180 },
                    new EquipmentType { Id = 2, Name = "Монитор", MaintenanceIntervalDays = 730 },
                    new EquipmentType { Id = 3, Name = "Ноутбук", MaintenanceIntervalDays = 180 },
                    new EquipmentType { Id = 4, Name = "Принтер", MaintenanceIntervalDays = 365 },
                    new EquipmentType { Id = 5, Name = "МФУ", MaintenanceIntervalDays = 365 },
                    new EquipmentType { Id = 6, Name = "Серверное оборудование", MaintenanceIntervalDays = 90 },
                    new EquipmentType { Id = 7, Name = "Телефон", MaintenanceIntervalDays = 365 }
                );
            });

            // Конфигурация Equipment
            modelBuilder.Entity<Equipment>(entity =>
            {
                entity.HasIndex(e => e.SerialNumber).IsUnique();
                entity.Property(e => e.SerialNumber)
                      .IsRequired()
                      .HasMaxLength(100);
                entity.Property(e => e.Model)
                      .HasMaxLength(150);
                entity.Property(e => e.Status)
                      .HasConversion<string>()
                      .HasDefaultValue(EquipmentStatus.InReserve);
                entity.Property(e => e.PurchaseDate)
                      .HasColumnType("date");
                entity.Property(e => e.NextMaintenanceDate)
                      .HasColumnType("date");
            });

            // Конфигурация Assignment
            modelBuilder.Entity<Assignment>(entity =>
            {
                entity.HasOne(a => a.Equipment)
                      .WithMany(e => e.Assignments)
                      .HasForeignKey(a => a.EquipmentId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(a => a.User)
                      .WithMany(u => u.AssignedEquipment)
                      .HasForeignKey(a => a.UserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.Property(a => a.AssignedAt)
                      .HasDefaultValueSql("GETDATE()");
                entity.Property(a => a.AssignmentNotes)
                      .HasMaxLength(500);
                entity.Property(a => a.ReturnNotes)
                      .HasMaxLength(500);
            });

            // Конфигурация MaintenanceLog
            modelBuilder.Entity<MaintenanceLog>(entity =>
            {
                entity.HasOne(m => m.Equipment)
                      .WithMany(e => e.MaintenanceLogs)
                      .HasForeignKey(m => m.EquipmentId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.Property(m => m.PerformedBy)
                      .IsRequired()
                      .HasMaxLength(100);
                entity.Property(m => m.MaintenanceType)
                      .IsRequired()
                      .HasMaxLength(50);
                entity.Property(m => m.Description)
                      .HasMaxLength(1000);
                entity.Property(m => m.Cost)
                      .HasColumnType("decimal(18,2)");
                entity.Property(m => m.Result)
                      .HasMaxLength(100);
                entity.Property(m => m.Date)
                      .HasColumnType("date")
                      .HasDefaultValueSql("GETDATE()");
                entity.Property(m => m.NextMaintenanceDate)
                      .HasColumnType("date");
            });
        }

        // Перехватываем SaveChanges и SaveChangesAsync, чтобы добавить записи в лог
        public override int SaveChanges()
        {
            if (_logSuppressed)
                return base.SaveChanges();

            // Собираем список записей и их исходных состояний
            var trackedStates = ChangeTracker.Entries()
                .Where(e => !(e.Entity is LogEntry) &&
                            (e.State == EntityState.Added ||
                             e.State == EntityState.Modified ||
                             e.State == EntityState.Deleted))
                .Select(e => (Entry: e, State: e.State))
                .ToList();

            // Сохраняем основную БД
            var result = base.SaveChanges();

            // Генерируем логи по сохранённым состояниям
            AddLogEntries(trackedStates);

            // Сохраняем логи без рекурсии
            _logSuppressed = true;
            base.SaveChanges();
            _logSuppressed = false;

            return result;
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            if (_logSuppressed)
                return await base.SaveChangesAsync(cancellationToken);

            var trackedStates = ChangeTracker.Entries()
                .Where(e => !(e.Entity is LogEntry) &&
                            (e.State == EntityState.Added ||
                             e.State == EntityState.Modified ||
                             e.State == EntityState.Deleted))
                .Select(e => (Entry: e, State: e.State))
                .ToList();

            var result = await base.SaveChangesAsync(cancellationToken);

            AddLogEntries(trackedStates);

            _logSuppressed = true;
            await base.SaveChangesAsync(cancellationToken);
            _logSuppressed = false;

            return result;
        }

        private void AddLogEntries(List<(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry Entry, EntityState State)> trackedStates)
        {
            var now = DateTime.Now;
            var username = _currentUsername ?? Environment.UserName;
            var logEntriesToAdd = new List<LogEntry>();

            foreach (var (entry, originalState) in trackedStates)
            {
                var entityName = entry.Entity.GetType().Name;
                var pkProp = entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey());
                var pkVal = pkProp?.CurrentValue?.ToString() ?? "unknown";

                string action;

                if (originalState == EntityState.Modified && entityName == "Assignment")
                {
                    var returnDateProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "ReturnedAt");
                    if (returnDateProp != null && returnDateProp.IsModified)
                    {
                        var oldVal = returnDateProp.OriginalValue as DateTime?;
                        var newVal = returnDateProp.CurrentValue as DateTime?;

                        if (oldVal == null && newVal != null)
                        {
                            action = "Return";
                            entityName = "Assignment";
                        }
                        else
                        {
                            action = "Update";
                        }
                    }
                    else
                    {
                        action = "Update";
                    }
                }
                else
                {
                    action = originalState switch
                    {
                        EntityState.Added => "Create",
                        EntityState.Modified => "Update",
                        EntityState.Deleted => "Delete",
                        _ => "Unknown"
                    };
                }

                string details;
                if (action == "Return")
                {
                    details = $"Оборудование (ID: {pkVal}) возвращено пользователем {username}.";
                }
                else if (originalState == EntityState.Added)
                {
                    var addedProps = entry.Properties
                        .Where(p => !p.Metadata.IsPrimaryKey())
                        .Select(p => $"{p.Metadata.Name}: {p.CurrentValue}")
                        .ToList();
                    details = $"Создана новая запись {entityName} (ID: {pkVal})"
                              + (addedProps.Any() ? $". Поля: {string.Join(", ", addedProps)}" : "");
                }
                else if (originalState == EntityState.Deleted)
                {
                    var deletedProps = entry.Properties
                        .Select(p => $"{p.Metadata.Name}: {p.OriginalValue}")
                        .ToList();
                    details = $"Удалена запись {entityName} (ID: {pkVal})"
                              + (deletedProps.Any() ? $". Состояние перед удалением: {string.Join(", ", deletedProps)}" : "");
                }
                else // Modified
                {
                    var modifiedProps = entry.Properties
                        .Where(p => p.IsModified && !p.Metadata.IsPrimaryKey())
                        .Select(p => $"{p.Metadata.Name}: {p.OriginalValue} -> {p.CurrentValue}")
                        .ToList();
                    details = modifiedProps.Any()
                        ? $"Изменены поля {entityName} (ID: {pkVal}): {string.Join(", ", modifiedProps)}"
                        : $"Запись {entityName} (ID: {pkVal}) обновлена";
                }

                logEntriesToAdd.Add(new LogEntry
                {
                    Action = action,
                    Username = username,
                    EntityName = entityName,
                    Details = details,
                    Timestamp = now
                });
            }

            if (logEntriesToAdd.Any())
                LogEntries.AddRange(logEntriesToAdd);
        }
    }
}
