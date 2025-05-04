using MicroservicioAutenticacion.Entities;
using Microsoft.EntityFrameworkCore;

namespace Microservicio_Administracion.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { 
        }

        public DbSet<Rol> Roles { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Usuario>().Property(u => u.fecha_creacion).HasDefaultValueSql("getdate()");
            modelBuilder.Entity<Usuario>().Property(u => u.estado).HasDefaultValue(Estado.Activo);
            //base.OnModelCreating(modelBuilder);
        }
    }
}
