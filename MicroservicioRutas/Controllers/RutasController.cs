using Grpc.Core;
using Microservicio_Rutas.Protos;
using MicroservicioRutas.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace MicroservicioRutas.Controllers
{
    [Authorize]
    public class RutasController : RutasService.RutasServiceBase
    {
        private readonly AppDbContext _context;

        public RutasController(AppDbContext context)
        {
            _context = context;
        }

        public override async Task<Ruta> CrearRuta(CrearRutaRequest request, ServerCallContext context)
        {
            var ruta = new Entities.Ruta
            {
                nombre = request.Nombre,
                puntoInicio = request.PuntoInicio,
                puntoFin = request.PuntoFin,
                distancia = Convert.ToDecimal(request.Distancia),
                estado = (Entities.Estado)request.Estado
            };

            _context.Rutas.Add(ruta);
            await _context.SaveChangesAsync();

            return MapToRutaProto(ruta);
        }

        public override async Task<Ruta> ActualizarRuta(ActualizarRutaRequest request, ServerCallContext context)
        {
            var ruta = await _context.Rutas.FindAsync(request.Id);
            if (ruta == null)
                throw new RpcException(new Status(StatusCode.NotFound, "Ruta no encontrada"));

            ruta.nombre = request.Nombre;
            ruta.puntoInicio = request.PuntoInicio;
            ruta.puntoFin = request.PuntoFin;
            ruta.distancia = Convert.ToDecimal(request.Distancia);
            ruta.estado = (Entities.Estado)request.Estado;

            await _context.SaveChangesAsync();

            return MapToRutaProto(ruta);
        }

        public override async Task<EliminarRutaResponse> EliminarRuta(EliminarRutaRequest request, ServerCallContext context)
        {
            var ruta = await _context.Rutas.FindAsync(request.Id);
            if (ruta == null)
                return new EliminarRutaResponse { Exito = false };
            /*
            // Verificar si existen asignaciones asociadas
            bool tieneAsignaciones = await _context.AsignacionRutas
                .AnyAsync(a => a.RutaId == request.Id);

            if (tieneAsignaciones)
            {
                throw new RpcException(new Status(StatusCode.FailedPrecondition, "No se puede eliminar la ruta porque tiene asignaciones activas."));
            }*/

            _context.Rutas.Remove(ruta);
            await _context.SaveChangesAsync();

            return new EliminarRutaResponse { Exito = true };
        }


        public override async Task<Ruta> ObtenerRutaId(ObtenerRutaRequest request, ServerCallContext context)
        {
            var ruta = await _context.Rutas
                .FirstOrDefaultAsync(a => a.id == request.Id);

            if (ruta == null)
                throw new RpcException(new Status(StatusCode.NotFound, "Ruta no encontrada"));

            return MapToRutaProto(ruta);
        }


        public override async Task<ListaRutas> ObtenerTodasRutas(RespuestaVaciaRuta request, ServerCallContext context)
        {
            var rutas = await _context.Rutas.ToListAsync();

            var respuesta = new ListaRutas();
            respuesta.Rutas.AddRange(rutas.Select(MapToRutaProto));
            return respuesta;
        }

        private Ruta MapToRutaProto(Entities.Ruta r) => new Ruta
        {
            Id = r.id,
            Nombre = r.nombre,
            PuntoInicio = r.puntoInicio,
            PuntoFin = r.puntoFin,
            Distancia = (double)r.distancia,
            Estado = (Estado)r.estado,
        };
    }
}
