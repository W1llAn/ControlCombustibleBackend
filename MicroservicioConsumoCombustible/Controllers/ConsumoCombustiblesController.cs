using Grpc.Core;
using MicroservicioConsumoCombustible.Protos;
using MicroservicioConsumoCombustible.Data;
using MicroservicioConsumoCombustible.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace MicroservicioConsumoCombustible.Controllers
{
   [Authorize]
    public class ConsumoCombustiblesController : ConsumoCombustiblesService.ConsumoCombustiblesServiceBase
    {
        private readonly AppDbContext _context;

        public ConsumoCombustiblesController(AppDbContext context)
        {
            _context = context;
        }

        public override async Task<AsignacionConsumoCombustible> CrearConsumoCombustible(CrearConsumoCombustibleRequest request, ServerCallContext context)
        {
            var asignacionRuta = await _context.AsignacionRutas
                .Include(a => a.Chofer)
                .Include(a => a.Vehiculo)
                .Include(a => a.Ruta)
                .FirstOrDefaultAsync(a => a.id == request.AsignacionRutaId);

            if (asignacionRuta == null)
                throw new RpcException(new Status(StatusCode.NotFound, "Asignación de ruta no encontrada"));

            var consumo = new ConsumoCombustible
            {
                fechaRegistro = DateTime.SpecifyKind(DateTime.Parse(request.FechaRegistro), DateTimeKind.Utc),
                estado = (Entities.Estado)request.Estado,
                combustibleEstimado = (decimal)request.CombustibleEstimado,
                combustibleReal = (decimal)request.CombustibleReal,
                idAsignacionRuta = request.AsignacionRutaId
            };

            _context.ConsumoCombustibles.Add(consumo);
            await _context.SaveChangesAsync();

            return await MapToProto(consumo.id);
        }

        public override async Task<AsignacionConsumoCombustible> ActualizarConsumoCombustible(ActualizarConsumoCombustibleRequest request, ServerCallContext context)
        {
            var consumo = await _context.ConsumoCombustibles.FindAsync(request.Id);

            if (consumo == null)
                throw new RpcException(new Status(StatusCode.NotFound, "Consumo no encontrado"));

            consumo.fechaRegistro = DateTime.SpecifyKind(DateTime.Parse(request.FechaRegistro), DateTimeKind.Utc);
            consumo.estado = (Entities.Estado)request.Estado;
            consumo.combustibleEstimado = (decimal)request.CombustibleEstimado;
            consumo.combustibleReal = (decimal)request.CombustibleReal;
            consumo.idAsignacionRuta = request.AsignacionRutaId;

            await _context.SaveChangesAsync();

            return await MapToProto(consumo.id);
        }

        public override async Task<EliminarConsumoCombustibleResponse> EliminarConsumoCombustible(EliminarConsumoCombustibleRequest request, ServerCallContext context)
        {
            var consumo = await _context.ConsumoCombustibles.FindAsync(request.Id);

            if (consumo == null)
                return new EliminarConsumoCombustibleResponse { Exito = false };

            _context.ConsumoCombustibles.Remove(consumo);
            await _context.SaveChangesAsync();

            return new EliminarConsumoCombustibleResponse { Exito = true };
        }

        public override async Task<AsignacionConsumoCombustible> ObtenerConsumoCombustibleId(ObtenerConsumoCombustibleRequest request, ServerCallContext context)
        {
            var consumo = await _context.ConsumoCombustibles
                .Include(c => c.asignacionRuta)
                    .ThenInclude(a => a.Chofer)
                .Include(c => c.asignacionRuta)
                    .ThenInclude(a => a.Vehiculo)
                .Include(c => c.asignacionRuta)
                    .ThenInclude(a => a.Ruta)
                .FirstOrDefaultAsync(c => c.id == request.Id);

            if (consumo == null)
                throw new RpcException(new Status(StatusCode.NotFound, "Consumo no encontrado"));

            return MapToProto(consumo);
        }

        public override async Task<ListaConsumosCombustibles> ObtenerTodosConsumosCombustible(RespuestaVaciaConsumoCombustible request, ServerCallContext context)
        {
            var consumos = await _context.ConsumoCombustibles
                .Include(c => c.asignacionRuta)
                    .ThenInclude(a => a.Chofer)
                .Include(c => c.asignacionRuta)
                    .ThenInclude(a => a.Vehiculo)
                .Include(c => c.asignacionRuta)
                    .ThenInclude(a => a.Ruta)
                .ToListAsync();

            var respuesta = new ListaConsumosCombustibles();
            respuesta.AsignacionConsumoCombustible.AddRange(consumos.Select(MapToProto));
            return respuesta;
        }

        // Helpers

        private async Task<AsignacionConsumoCombustible> MapToProto(int id)
        {
            var consumo = await _context.ConsumoCombustibles
                .Include(c => c.asignacionRuta)
                    .ThenInclude(a => a.Chofer)
                .Include(c => c.asignacionRuta)
                    .ThenInclude(a => a.Vehiculo)
                .Include(c => c.asignacionRuta)
                    .ThenInclude(a => a.Ruta)
                .FirstOrDefaultAsync(c => c.id == id);

            return MapToProto(consumo);
        }

        private AsignacionConsumoCombustible MapToProto(ConsumoCombustible c) => new AsignacionConsumoCombustible
        {
            Id = c.id,
            FechaRegistro = c.fechaRegistro.ToString("yyyy-MM-dd"),
            Estado = (Protos.Estado)c.estado,
            CombustibleEstimado = (double)c.combustibleEstimado,
            CombustibleReal = (double)c.combustibleReal,
            AsignacionRuta = new Protos.AsignacionRuta
            {
                Id = c.asignacionRuta.id,
                FechaAsignacion = c.asignacionRuta.fechaAsignacion.ToString("yyyy-MM-dd"),
                Estado = (Protos.Estado)c.asignacionRuta.estado,
                Chofer = new Protos.Chofer
                {
                    Id = c.asignacionRuta.Chofer.id,
                    Nombre = c.asignacionRuta.Chofer.nombre,
                    Identificacion = c.asignacionRuta.Chofer.identificacion,
                    Disponible = c.asignacionRuta.Chofer.disponible,
                    FechaNacimiento = c.asignacionRuta.Chofer.fecha_nacimiento.ToString("yyyy-MM-dd")
                },
                Vehiculo = new Protos.Vehiculo
                {
                    Id = c.asignacionRuta.Vehiculo.id,
                    Placa = c.asignacionRuta.Vehiculo.placa,
                    TipoMaquinaria = c.asignacionRuta.Vehiculo.tipoMaquinaria,
                    EstadoOperativo = c.asignacionRuta.Vehiculo.estadoOperativo,
                    CapacidadCombustible = (double)c.asignacionRuta.Vehiculo.capacidadCombustible,
                    FechaRegistro = c.asignacionRuta.Vehiculo.fechaRegistro.ToString("yyyy-MM-dd"),
                    ConsumoCombustibleKm = (double)c.asignacionRuta.Vehiculo.consumoCombustibleKm,
                    Estado = c.asignacionRuta.Vehiculo.estado,
                    Descripcion = c.asignacionRuta.Vehiculo.descripcion,
                    Nombre = c.asignacionRuta.Vehiculo.nombre
                },
                Ruta = new Protos.Ruta
                {
                    Id = c.asignacionRuta.Ruta.id,
                    Nombre = c.asignacionRuta.Ruta.nombre,
                    PuntoInicio = c.asignacionRuta.Ruta.puntoInicio,
                    PuntoFin = c.asignacionRuta.Ruta.puntoFin,
                    Distancia = (double)c.asignacionRuta.Ruta.distancia,
                    Estado = (Protos.Estado)c.asignacionRuta.Ruta.estado
                }
            }
        };

    }
}
