using MicroservicioChoferes.Entities;
using MicroservicioConsumoCombustible.Entities;
using MicroservicioRutas.Models;
using System.ComponentModel.DataAnnotations;
using System.Data;

namespace MicroservicioRutas.Entities
{
    public class AsignacionRuta
    {
        [Key]
        public int id { get; set; }
        public DateTime fechaAsignacion { get; set; }
        public Estado estado { get; set; }

        public int ChoferId { get; set; }
        public Chofer Chofer { get; set; }
        public int VehiculoId { get; set; }
        public Vehiculo Vehiculo { get; set; }
        public int RutaId { get; set; }
        public Ruta Ruta { get; set; }

    }
}
