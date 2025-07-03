using MicroservicioChoferes.Entities;
using MicroservicioRutas.Entities;
using MicroservicioRutas.Models;
using Microsoft.EntityFrameworkCore;

namespace MicroservicioRutas.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {
        }

        public DbSet<Ruta> Rutas { get; set; }
        public DbSet<AsignacionRuta> AsignacionRutas { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //Asinacion de ids 
            modelBuilder.Entity<Ruta>().Property(u => u.estado).HasDefaultValue(Estado.Activo);
            modelBuilder.Entity<Ruta>().HasKey(c => c.id);

            modelBuilder.Entity<AsignacionRuta>().Property(u => u.estado).HasDefaultValue(Estado.Activo);
            modelBuilder.Entity<AsignacionRuta>().HasKey(c => c.id);

            //Excluimos las tablas ya creadas
            modelBuilder.Entity<Chofer>()
                .ToTable("Choferes", t => t.ExcludeFromMigrations());
            modelBuilder.Entity<Vehiculo>()
                .ToTable("Vehiculos", t => t.ExcludeFromMigrations());

            // Relaciones explícitas
            modelBuilder.Entity<AsignacionRuta>()
                .HasOne(a => a.Chofer)
                .WithMany()
                .HasForeignKey(a => a.ChoferId);

            modelBuilder.Entity<AsignacionRuta>()
                .HasOne(a => a.Vehiculo)
                .WithMany()
                .HasForeignKey(a => a.VehiculoId);

            modelBuilder.Entity<AsignacionRuta>()
                .HasOne(a => a.Ruta)
                .WithMany()
                .HasForeignKey(a => a.RutaId);
        }
    }
}
