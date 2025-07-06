using MicroservicioChoferes.Entities;
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

    }
}
