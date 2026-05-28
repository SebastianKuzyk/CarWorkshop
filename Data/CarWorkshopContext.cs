using Microsoft.EntityFrameworkCore;
using CarWorkshopWPF.Models;

namespace CarWorkshopWPF.Data
{
    public class CarWorkshopContext : DbContext
    {
        public DbSet<ServiceType> ServiceTypes { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Vehicle> Vehicles { get; set; }
        public DbSet<RepairOrder> RepairOrders { get; set; }
        public DbSet<RepairTask> RepairTasks { get; set; }
        public DbSet<Part> Parts { get; set; }
        public DbSet<OrderPart> OrderParts { get; set; }
        public DbSet<PartRequest> PartRequests { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Plik bazy SQLite leży w katalogu projektu (obok .csproj).
            // Ścieżka jest względna do bieżącego katalogu roboczego procesu,
            // a "dotnet run" oraz Visual Studio uruchamiają aplikację z bin/...,
            // dlatego plik .db jest kopiowany przy buildzie (CopyToOutputDirectory).
            optionsBuilder.UseSqlite("Data Source=car_workshop.db");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Klucz złożony dla tabeli order_parts (zgodnie ze schemą bazy).
            modelBuilder.Entity<OrderPart>()
                .HasKey(op => new { op.OrderId, op.PartId });
        }
    }
}
