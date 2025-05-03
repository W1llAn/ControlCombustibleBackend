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


    }
}
