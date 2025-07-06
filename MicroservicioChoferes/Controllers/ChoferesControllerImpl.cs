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
        private readonly UsuariosGrpcClient _usuariosClient;

        public ChoferesControllerImpl(AppDbContext context, UsuariosGrpcClient usuariosClient)
        {
            _context = context;
            _usuariosClient = usuariosClient;
        }

        public override async Task<Chofer> CrearChofer(CrearChoferRequest request, ServerCallContext context)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Nombre) || string.IsNullOrWhiteSpace(request.Identificacion))
                    throw new RpcException(new Status(StatusCode.InvalidArgument, "Nombre e Identificación son obligatorios"));

                var token = context.RequestHeaders
                  .FirstOrDefault(h => h.Key == "authorization")?.Value?.Replace("Bearer ", "");


                var usuarioRegistrado = await _usuariosClient.RegistrarUsuarioAsync(
                    request.Email,
                    request.NombreUsuario,
                    request.HashContrasena,
                    request.RolId,
                    token
                );

                if (usuarioRegistrado == null)
                    throw new RpcException(new Status(StatusCode.Internal, "No se pudo registrar el usuario"));

                var usuarioLocal = await _context.Usuarios.FirstOrDefaultAsync(u => u.id == usuarioRegistrado.Id);
                if (usuarioLocal == null)
                    throw new RpcException(new Status(StatusCode.NotFound, "Usuario registrado no se encontró en base de datos local"));

                var chofer = new Entities.Chofer
                {
                    nombre = request.Nombre,
                    identificacion = request.Identificacion,
                    disponible = request.Disponible,
                    estado = (Entities.Estado)request.Estado,
                    tipo_maquinaria = (Entities.TipoMaquinaria)request.TipoMaquinaria,
                    fecha_nacimiento = DateTime.SpecifyKind(DateTime.Parse(request.FechaNacimiento), DateTimeKind.Utc),
                    usuario = usuarioLocal
                };

                _context.Choferes.Add(chofer);
                await _context.SaveChangesAsync();

                return MapToChoferProto(chofer);
            }
            catch (RpcException) { throw; }
            catch (Exception ex)
            {
                throw new RpcException(new Status(StatusCode.Internal, $"Error interno al crear el chofer: {ex.Message}"));
            }
        }

        public override async Task<Chofer> ActualizarChofer(ActualizarChoferRequest request, ServerCallContext context)
        {
            try
            {
                var chofer = await _context.Choferes.FindAsync(request.Id);
                if (chofer == null)
                    throw new RpcException(new Status(StatusCode.NotFound, "Chofer no encontrado"));

                var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.id == request.IdUsuario);
                if (usuario == null)
                    throw new RpcException(new Status(StatusCode.NotFound, "Usuario asociado no encontrado"));

                chofer.nombre = request.Nombre;
                chofer.identificacion = request.Identificacion;
                chofer.disponible = request.Disponible;
                chofer.estado = (Entities.Estado)request.Estado;
                chofer.tipo_maquinaria = (Entities.TipoMaquinaria)request.TipoMaquinaria;
                chofer.fecha_nacimiento = DateTime.SpecifyKind(DateTime.Parse(request.FechaNacimiento), DateTimeKind.Utc);
                chofer.usuario = usuario;

                await _context.SaveChangesAsync();

                return MapToChoferProto(chofer);
            }
            catch (RpcException) { throw; }
            catch (Exception ex)
            {
                throw new RpcException(new Status(StatusCode.Internal, $"Error al actualizar chofer: {ex.Message}"));
            }
        }

        public override async Task<EliminarChoferResponse> EliminarChofer(EliminarChoferRequest request, ServerCallContext context)
        {
            try
            {
                var chofer = await _context.Choferes.FindAsync(request.Id);
                if (chofer == null)
                    return new EliminarChoferResponse { Exito = false };

                _context.Choferes.Remove(chofer);
                await _context.SaveChangesAsync();

                return new EliminarChoferResponse { Exito = true };
            }
            catch (Exception ex)
            {
                throw new RpcException(new Status(StatusCode.Internal, $"Error al eliminar chofer: {ex.Message}"));
            }
        }

        public override async Task<Chofer> ObtenerChofer(ObtenerChoferRequest request, ServerCallContext context)
        {
            try
            {
                var chofer = await _context.Choferes.Include(c => c.usuario).FirstOrDefaultAsync(c => c.id == request.Id);
                if (chofer == null)
                    throw new RpcException(new Status(StatusCode.NotFound, "Chofer no encontrado"));

                return MapToChoferProto(chofer);
            }
            catch (RpcException) { throw; }
            catch (Exception ex)
            {
                throw new RpcException(new Status(StatusCode.Internal, $"Error al obtener chofer: {ex.Message}"));
            }
        }

        public override async Task<ListaChoferes> ObtenerTodosChoferes(RespuestaVaciaChofer request, ServerCallContext context)
        {
            try
            {
                var choferes = await _context.Choferes.Include(c => c.usuario).ToListAsync();

                var respuesta = new ListaChoferes();
                respuesta.Usuarios.AddRange(choferes.Select(MapToChoferProto));
                return respuesta;
            }
            catch (Exception ex)
            {
                throw new RpcException(new Status(StatusCode.Internal, $"Error al obtener todos los choferes: {ex.Message}"));
            }
        }

        private Chofer MapToChoferProto(Entities.Chofer c)
        {
            return new Chofer
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
}
