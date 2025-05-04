namespace MicroservicioVehiculos.Models
{
    public class Vehiculo
    {
        public int id { get; set; }
        public string placa { get; set; }
        public string tipoMaquinaria { get; set; } // "Liviana" o "Pesada"
        public string estadoOperativo { get; set; } // "Operativo", "Mantenimiento"
        public decimal capacidadCombustible { get; set; }
        public DateTime fechaRegistro { get; set; }
        public decimal consumoCombustibleKm { get; set; }
        public string estado { get; set; } // "Eliminado", "Activo"


    }
}
