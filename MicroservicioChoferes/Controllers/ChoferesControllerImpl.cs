using Grpc.Core;
using Microservicio_Choferes.Data;
using MicroservicioChoferes.Protos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;



namespace MicroservicioChoferes.Controllers
{
    [Authorize]
    public class ChoferesControllerImpl : ChoferesService.ChoferesServiceBase
    {
        private readonly AppDbContext _context;
        public ChoferesControllerImpl(AppDbContext context)
        {
            _context = context;
        }
        
        public override async Task<Chofer> CrearChofer(CrearChoferRequest request, ServerCallContext context)
        {
            var chofer = new Entities.Chofer
            {
                nombre = request.Nombre,
                identificacion = request.Identificacion,
                disponible = request.Disponible,
                estado = (Entities.Estado)request.Estado,
                tipo_maquinaria = (Entities.TipoMaquinaria)request.TipoMaquinaria,
                fecha_nacimiento = DateTime.SpecifyKind(DateTime.Parse(request.FechaNacimiento), DateTimeKind.Utc),
                usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.id == request.IdUsuario)
            };

            _context.Choferes.Add(chofer);
            await _context.SaveChangesAsync();

            return MapToChoferProto(chofer);
        }

        public override async Task<Chofer> ActualizarChofer(ActualizarChoferRequest request, ServerCallContext context)
        {
            var chofer = await _context.Choferes.FindAsync(request.Id);
            if (chofer == null)
                throw new RpcException(new Status(StatusCode.NotFound, "Chofer no encontrado"));

            chofer.nombre = request.Nombre;
            chofer.identificacion = request.Identificacion;
            chofer.disponible = true;
            chofer.tipo_maquinaria = (Entities.TipoMaquinaria)request.TipoMaquinaria;
            chofer.fecha_nacimiento = DateTime.SpecifyKind(DateTime.Parse(request.FechaNacimiento), DateTimeKind.Utc);
            chofer.usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.id == request.IdUsuario);

            await _context.SaveChangesAsync();

            return MapToChoferProto(chofer);
        }

        public override async Task<EliminarChoferResponse> EliminarChofer(EliminarChoferRequest request, ServerCallContext context)
        {
            var chofer = await _context.Choferes.FindAsync(request.Id);
            if (chofer == null)
                return new EliminarChoferResponse { Exito = false };

            _context.Choferes.Remove(chofer);
            await _context.SaveChangesAsync();

            return new EliminarChoferResponse { Exito = true };
        }

        public override async Task<Chofer> ObtenerChofer(ObtenerChoferRequest request, ServerCallContext context)
        {
            var chofer = await _context.Choferes.Include(c => c.usuario).FirstOrDefaultAsync(c=>c.id==request.Id);
            if (chofer == null)
                throw new RpcException(new Status(StatusCode.NotFound, "Chofer no encontrado"));

            return MapToChoferProto(chofer);
        }

        public override async Task<ListaChoferes> ObtenerTodosChoferes(RespuestaVaciaChofer request, ServerCallContext context)
        {
            var choferes = await _context.Choferes.Include(c=>c.usuario).ToListAsync();

            var respuesta = new ListaChoferes();
            respuesta.Usuarios.AddRange(choferes.Select(MapToChoferProto));
            return respuesta;
        }

        private Chofer MapToChoferProto(Entities.Chofer c) => new Chofer
        {
            Id = c.id,
            Nombre = c.nombre,
            Identificacion = c.identificacion,
            Disponible = c.disponible,
            Estado = (Estado)c.estado,
            TipoMaquinaria = (TipoMaquinaria)c.tipo_maquinaria,
            FechaNacimiento = c.fecha_nacimiento.ToString("yyyy-MM-dd"),
            IdUsuario = c.usuario.id
        };

    }
}
