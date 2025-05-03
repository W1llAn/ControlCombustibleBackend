using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace MicroservicioAutenticacion.Entities
{
    public class Usuario
    {
        [Key]
        public int id { get; set; }

        public string email { get; set; }
        public string Nombre_usuario { get; set; }
        public string hash_contrasena { get; set; }
        public Estado estado { get; set; }
        public Rol rol { get; set; }
        public DateTime fecha_creacion { get; set; }

    }
}
