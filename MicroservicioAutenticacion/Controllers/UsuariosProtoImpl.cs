using Grpc.Core;
using Microservicio_Administracion.Data;
using Microservicio_Autenticación.Auth;
using MicroservicioAutenticacion.Protos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace MicroservicioAutenticacion.Controllers
{
    public class UsuariosProtoImpl : UsuariosService.UsuariosServiceBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public UsuariosProtoImpl(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [Authorize(Policy = "AdministradorPolitica")]
        public override async Task<Usuario> RegistrarUsuario(UsuarioRegistro request, ServerCallContext context)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.NombreUsuario))
                    throw new RpcException(new Status(StatusCode.InvalidArgument, "Email y nombre de usuario son obligatorios"));

                var rol = await _context.Roles.FindAsync(request.RolId);
                if (rol == null)
                    throw new RpcException(new Status(StatusCode.NotFound, $"Rol con ID {request.RolId} no encontrado"));

                var nuevoUsuario = new Entities.Usuario
                {
                    email = request.Email,
                    Nombre_usuario = request.NombreUsuario,
                    hash_contrasena = request.HashContrasena,
                    rol = rol
                };

                _context.Usuarios.Add(nuevoUsuario);
                await _context.SaveChangesAsync();

                return MapUsuario(nuevoUsuario);
            }
            catch (RpcException) { throw; }
            catch (Exception ex)
            {
                throw new RpcException(new Status(StatusCode.Internal, $"Error al registrar usuario: {ex.Message}"));
            }
        }

        [Authorize(Policy = "AdministradorPolitica")]
        public override async Task<RespuestaVacia> BorrarUsuario(UsuarioBorrar request, ServerCallContext context)
        {
            try
            {
                var usuario = await _context.Usuarios.Include(u => u.rol).FirstOrDefaultAsync(u => u.id == request.Id);
                if (usuario == null)
                    throw new RpcException(new Status(StatusCode.NotFound, "Usuario no encontrado"));

                usuario.estado = Entities.Estado.Eliminado;
                await _context.SaveChangesAsync();

                return new RespuestaVacia();
            }
            catch (RpcException) { throw; }
            catch (Exception ex)
            {
                throw new RpcException(new Status(StatusCode.Internal, $"Error al borrar usuario: {ex.Message}"));
            }
        }

        [Authorize(Policy = "SupervisorAdministradorPolitica")]
        public override async Task<ListaUsuarios> SeleccionarUsuarios(RespuestaVacia request, ServerCallContext context)
        {
            try
            {
                var usuarios = await _context.Usuarios.Include(u => u.rol).ToListAsync();

                var lista = new ListaUsuarios();
                lista.Usuarios.AddRange(usuarios.Select(MapUsuario));

                return lista;
            }
            catch (Exception ex)
            {
                throw new RpcException(new Status(StatusCode.Internal, $"Error al obtener usuarios: {ex.Message}"));
            }
        }

        [Authorize(Policy = "SupervisorAdministradorPolitica")]
        public override async Task<Usuario> ActualizarUsuario(UsuarioActualizar request, ServerCallContext context)
        {
            try
            {
                var usuario = await _context.Usuarios.Include(u => u.rol).FirstOrDefaultAsync(u => u.id == request.Id);
                if (usuario == null)
                    throw new RpcException(new Status(StatusCode.NotFound, "Usuario no encontrado"));

                var rol = await _context.Roles.FindAsync(request.RolId);
                if (rol == null)
                    throw new RpcException(new Status(StatusCode.NotFound, $"Rol con ID {request.RolId} no encontrado"));

                usuario.email = request.Email;
                usuario.Nombre_usuario = request.NombreUsuario;
                usuario.hash_contrasena = request.HashContrasena;
                usuario.estado = (Entities.Estado)request.Estado;
                usuario.rol = rol;

                await _context.SaveChangesAsync();

                return MapUsuario(usuario);
            }
            catch (RpcException) { throw; }
            catch (Exception ex)
            {
                throw new RpcException(new Status(StatusCode.Internal, $"Error al actualizar usuario: {ex.Message}"));
            }
        }

        public override async Task<UsuarioLoginRespuesta> Login(UsuarioLogin request, ServerCallContext context)
        {
            try
            {
                var usuario = await _context.Usuarios
                    .Include(u => u.rol)
                    .FirstOrDefaultAsync(u =>
                        u.Nombre_usuario == request.NombreUsuario &&
                        u.hash_contrasena == request.HashContrasena);

                if (usuario == null || usuario.estado == Entities.Estado.Eliminado)
                {
                    return new UsuarioLoginRespuesta { Token = "" };
                }

                var tokenProvider = new TokenProvider(_configuration);
                var token = tokenProvider.Create(usuario);

                return new UsuarioLoginRespuesta { Token = token };
            }
            catch (Exception ex)
            {
                throw new RpcException(new Status(StatusCode.Internal, $"Error al iniciar sesión: {ex.Message}"));
            }
        }

        private Usuario MapUsuario(Entities.Usuario usuario)
        {
            return new Usuario
            {
                Id = usuario.id,
                Email = usuario.email,
                NombreUsuario = usuario.Nombre_usuario,
                HashContrasena = usuario.hash_contrasena,
                Estado = (Protos.Estado)usuario.estado,
                FechaCreacion = usuario.fecha_creacion.ToString("o"),
                Rol = new Rol
                {
                    Id = usuario.rol.id,
                    Nombre = usuario.rol.nombre,
                    Descripcion = usuario.rol.descripcion
                }
            };
        }
    }
}
