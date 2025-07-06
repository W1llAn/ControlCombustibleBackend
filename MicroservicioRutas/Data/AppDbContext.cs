using MicroservicioRutas.Entities;
using Microsoft.EntityFrameworkCore;


namespace MicroservicioRutas.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {
        }

        public DbSet<Ruta> Rutas { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //Asinacion de ids 
            modelBuilder.Entity<Ruta>().Property(u => u.estado).HasDefaultValue(Estado.Activo);
            modelBuilder.Entity<Ruta>().HasKey(c => c.id);
        }
    }
}
