using MicroservicioChoferes.Entities;
using MicroservicioConsumoCombustible.Entities;
using MicroservicioRutas.Entities;
using MicroservicioRutas.Models;
using Microsoft.EntityFrameworkCore;

namespace MicroservicioConsumoCombustible.Data

{
    public class AppDbContext: DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options){      
        }
        
        public DbSet<ConsumoCombustible> ConsumoCombustibles { get; set; }
        public DbSet<AsignacionRuta> AsignacionRutas { get; set; }
        public DbSet<Chofer> Choferes { get; set; }
        public DbSet<Vehiculo> Vehiculos { get; set; }
        public DbSet<Ruta> Rutas { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //Asinacion de ids 
 
            modelBuilder.Entity<ConsumoCombustible>().Property(u => u.estado).HasDefaultValue(Estado.Activo);
            modelBuilder.Entity<ConsumoCombustible>().HasKey(c => c.id);

            //Excluimos las tablas ya creadas
            modelBuilder.Entity<Chofer>()
                .ToTable("Choferes", t => t.ExcludeFromMigrations());
            modelBuilder.Entity<Vehiculo>()
                .ToTable("Vehiculos", t => t.ExcludeFromMigrations());
            modelBuilder.Entity<AsignacionRuta>()
              .ToTable("AsignacionRutas", t => t.ExcludeFromMigrations());
            modelBuilder.Entity<Ruta>()
              .ToTable("Rutas", t => t.ExcludeFromMigrations());

            // Relaciones explícitas
            modelBuilder.Entity<ConsumoCombustible>()
                .HasOne(a => a.asignacionRuta)
                .WithMany()
                .HasForeignKey(a => a.idAsignacionRuta);
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
