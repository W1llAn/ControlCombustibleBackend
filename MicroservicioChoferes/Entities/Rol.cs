using System.ComponentModel.DataAnnotations;

namespace MicroservicioAutenticacion.Entities
{
    public class Rol
    {
        [Key]
        public int id { get; set; }
        public string nombre { get; set; }
        public string descripcion { get; set; }
    }
}
