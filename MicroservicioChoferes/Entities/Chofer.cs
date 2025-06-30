using MicroservicioAutenticacion.Entities;
using System.ComponentModel.DataAnnotations;

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

        public Estado estado { get; set; }

        public TipoMaquinaria tipo_maquinaria { get; set; }
        public Genero genero { get; set; }
        public Usuario usuario { get; set; }


    }
}
