using MicroservicioChoferes.Entities;
using System.ComponentModel.DataAnnotations;
namespace MicroservicioRutas.Entities
{
    public class Ruta
    {
        [Key]
        public int id { get; set; }
        public string nombre { get; set; }
        public string puntoInicio { get; set; }
        public string puntoFin { get; set; }
        public decimal distancia { get; set; }
        public Estado estado  { get; set; }

    }
}
