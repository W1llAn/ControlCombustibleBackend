using MicroservicioVehiculos.Models;
using Microsoft.EntityFrameworkCore;

namespace MicroservicioVehiculos.Data
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options) { }
        public DbSet<Vehiculo> Vehiculos { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Vehiculo>().Property(v => v.consumoCombustibleKm).IsRequired();
        }

    }
}

