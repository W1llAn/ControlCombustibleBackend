using Grpc.Core;
using MicroservicioRutas.Data;
using MicroservicioRutas.Protos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace MicroservicioRutas.Controllers
{
    [Authorize]
    public class AsignacionRutasController : AsignacionRutasService.AsignacionRutasServiceBase
    {
        private readonly AppDbContext _context;

        public AsignacionRutasController(AppDbContext context)
        {
            _context = context;
        }

        public override async Task<AsignacionRuta> CrearAsignacionRuta(CrearAsignacionRutaRequest request, ServerCallContext context)
        {
            var asignacion = new Entities.AsignacionRuta
            {
                fechaAsignacion = DateTime.SpecifyKind(DateTime.Parse(request.FechaAsignacion), DateTimeKind.Utc),
                estado = (Entities.Estado)request.Estado,
                ChoferId = request.ChoferId,
                VehiculoId = request.VehiculoId,
                RutaId = request.RutaId
            };

            _context.AsignacionRutas.Add(asignacion);
            await _context.SaveChangesAsync();

            var asignacionConRelaciones = await _context.AsignacionRutas
            .Include(a => a.Chofer)
            .ThenInclude(c => c.Usuario)
            .Include(a => a.Vehiculo)
            .Include(a => a.Ruta)
            .FirstOrDefaultAsync(a => a.id == asignacion.id);

            return MapToAsignacionRutaProto(asignacionConRelaciones!);
        }

        public override async Task<AsignacionRuta> ActualizarRutaAsignada(ActualizarAsignacionRutaRequest request, ServerCallContext context)
        {
            var asignacion = await _context.AsignacionRutas.FindAsync(request.Id);
            if (asignacion == null)
                throw new RpcException(new Status(StatusCode.NotFound, "Asignación no encontrada"));

            asignacion.fechaAsignacion = DateTime.SpecifyKind(DateTime.Parse(request.FechaAsignacion), DateTimeKind.Utc);
            asignacion.estado = (Entities.Estado)request.Estado;
            asignacion.ChoferId = request.ChoferId;
            asignacion.VehiculoId = request.VehiculoId;
            asignacion.RutaId = request.RutaId;

            await _context.SaveChangesAsync();

            var asignacionConRelaciones = await _context.AsignacionRutas
            .Include(a => a.Chofer)
                .ThenInclude(c => c.Usuario)
            .Include(a => a.Vehiculo)
            .Include(a => a.Ruta)
            .FirstOrDefaultAsync(a => a.id == asignacion.id);

            return MapToAsignacionRutaProto(asignacionConRelaciones!);

        }

        public override async Task<EliminarAsignacionRutaResponse> EliminarAsignacionRuta(EliminarAsignacionRutaRequest request, ServerCallContext context)
        {
            var asignacion = await _context.AsignacionRutas.FindAsync(request.Id);
            if (asignacion == null)
                return new EliminarAsignacionRutaResponse { Exito = false };

            _context.AsignacionRutas.Remove(asignacion);
            await _context.SaveChangesAsync();

            return new EliminarAsignacionRutaResponse { Exito = true };
        }

        public override async Task<AsignacionRuta> ObtenerAsignacionRutaId(ObtenerRutaAsignadaRequest request, ServerCallContext context)
        {
            try
            {
                var asignacion = await _context.AsignacionRutas
                    .Include(a => a.Chofer)
                        .ThenInclude(c => c.Usuario)
                    .Include(a => a.Vehiculo)
                    .Include(a => a.Ruta)
                    .FirstOrDefaultAsync(a => a.id == request.Id);

                if (asignacion == null)
                {
                    Console.WriteLine($"❌ Asignación con ID={request.Id} no encontrada.");
                    throw new RpcException(new Status(StatusCode.NotFound, "Asignación no encontrada"));
                }

                Console.WriteLine($"✅ Asignación encontrada: ID={asignacion.id}");
                return MapToAsignacionRutaProto(asignacion);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERROR en ObtenerAsignacionRutaId: {ex}");
                throw new RpcException(new Status(StatusCode.Unknown, "Error interno al procesar la solicitud"));
            }
        }


        public override async Task<ListaRutasAsignadas> ObtenerTodasRutasAsignadas(RespuestaVaciaAsignadaRuta request, ServerCallContext context)
        {
            var asignaciones = await _context.AsignacionRutas
                .Include(a => a.Chofer)
                .ThenInclude(c=> c.Usuario)
                .Include(a => a.Vehiculo)
                .Include(a => a.Ruta)
                .ToListAsync();

            var respuesta = new ListaRutasAsignadas();
            respuesta.AsignacionRuta.AddRange(asignaciones.Select(MapToAsignacionRutaProto));
            return respuesta;
        }

        private AsignacionRuta MapToAsignacionRutaProto(Entities.AsignacionRuta a)
        {
            try
            {
                if (a.Chofer == null) Console.WriteLine("⚠️ Chofer es null");
                if (a.Vehiculo == null) Console.WriteLine("⚠️ Vehiculo es null");
                if (a.Ruta == null) Console.WriteLine("⚠️ Ruta es null");

                return new AsignacionRuta
                {
                    Id = a.id,
                    FechaAsignacion = a.fechaAsignacion.ToString("yyyy-MM-dd"),
                    Estado = (Protos.Estado)a.estado,
                    Chofer = a.Chofer == null ? null : new Chofer
                    {
                        Id = a.Chofer.id,
                        Nombre = a.Chofer.nombre,
                        Identificacion = a.Chofer.identificacion,
                        Disponible = a.Chofer.disponible,
                        FechaNacimiento = a.Chofer.fecha_nacimiento.ToString("yyyy-MM-dd"),
                        IdUsuario = a.Chofer.usuarioid,
                        Usuario = a.Chofer.Usuario == null ? null : new Usuario
                        {
                            Id = a.Chofer.Usuario.id,
                            Email = a.Chofer.Usuario.email ?? string.Empty,
                            NombreUsuario = a.Chofer.Usuario.Nombre_usuario ?? string.Empty
                        }
                    },
                    Vehiculo = a.Vehiculo == null ? null : new Vehiculo
                    {
                        Id = a.Vehiculo.id,
                        Placa = a.Vehiculo.placa,
                        TipoMaquinaria = a.Vehiculo.tipoMaquinaria,
                        EstadoOperativo = a.Vehiculo.estadoOperativo,
                        CapacidadCombustible = (double)a.Vehiculo.capacidadCombustible,
                        FechaRegistro = a.Vehiculo.fechaRegistro.ToString("yyyy-MM-dd"),
                        ConsumoCombustibleKm = (double)a.Vehiculo.consumoCombustibleKm,
                        Estado = a.Vehiculo.estado,
                        Descripcion = a.Vehiculo.descripcion,
                        Nombre = a.Vehiculo.nombre
                    },
                    Ruta = a.Ruta == null ? null : new Ruta
                    {
                        Id = a.Ruta.id,
                        Nombre = a.Ruta.nombre,
                        PuntoInicio = a.Ruta.puntoInicio,
                        PuntoFin = a.Ruta.puntoFin,
                        Distancia = (double)a.Ruta.distancia,
                        Estado = (Protos.Estado)a.Ruta.estado
                    }
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERROR en MapToAsignacionRutaProto: {ex}");
                throw;
            }
        }

    }
}