using MicroservicioAutenticacion.Entities;
using MicroservicioChoferes.Entities;
using Microsoft.EntityFrameworkCore;

namespace Microservicio_Choferes.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { 
        }

        public DbSet<Chofer> Choferes { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Usuario>()
                .ToTable("Usuarios", t => t.ExcludeFromMigrations());
            modelBuilder.Entity<Rol>()
                .ToTable("Roles", t => t.ExcludeFromMigrations());

            modelBuilder.Entity<Chofer>().Property(u => u.estado).HasDefaultValue(Estado.Activo);

            modelBuilder.Entity<Chofer>(entity =>
            {
                entity.HasKey(c => c.id);
            });
            //base.OnModelCreating(modelBuilder);
        }
    }
}
