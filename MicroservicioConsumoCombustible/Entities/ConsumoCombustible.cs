using MicroservicioRutas.Entities;
using System.ComponentModel.DataAnnotations;

namespace MicroservicioConsumoCombustible.Entities
{
    public class ConsumoCombustible
    {
        [Key]
        public int id   { get; set; }
        public decimal combustibleEstimado { get; set; }
        public decimal combustibleReal { get; set; }
        public DateTime fechaRegistro { get; set; }
        public Estado estado { get; set; }

        public string? motivo { get; set; } 
        public int idAsignacionRuta { get; set; }
        public AsignacionRuta asignacionRuta { get; set; }


    }
}
