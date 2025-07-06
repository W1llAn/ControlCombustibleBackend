using System.ComponentModel.DataAnnotations;
using  MicroservicioAutenticacion.Entities;

namespace MicroservicioChoferes.Entities
{
    public class Chofer
    {
        [Key]
        public int id { get; set; }

        public string nombre { get; set; }
        public string identificacion { get; set; }
        public bool disponible { get; set; }
        public DateTime fecha_nacimiento { get; set; }

        public int usuarioid { get; set; }
        public Usuario  Usuario { get; set; }

    }
}
