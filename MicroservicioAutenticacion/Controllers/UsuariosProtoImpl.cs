using Grpc.Core;
using Microservicio_Administracion.Data;
using Microservicio_Autenticación.Auth;
using MicroservicioAutenticacion.Protos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace MicroservicioAutenticacion.Controllers
{
    public class UsuariosProtoImpl: UsuariosService.UsuariosServiceBase
    {
        private readonly AppDbContext _context;
        private  IConfiguration _configuration;
        public UsuariosProtoImpl(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }
        [Authorize(Policy = "AdministradorPolitica")]
        public override async Task<Usuario> RegistrarUsuario(UsuarioRegistro request, ServerCallContext context)
        {
            var nuevoUsuario = new Entities.Usuario
            {
                email = request.Email,
                Nombre_usuario = request.NombreUsuario,
                hash_contrasena = request.HashContrasena,
                rol = await _context.Roles.FindAsync(request.RolId),
            };

            _context.Usuarios.Add(nuevoUsuario);
            await _context.SaveChangesAsync();

            return MapUsuario(nuevoUsuario);
        }
        [Authorize(Policy = "AdministradorPolitica")]
        public override async Task<RespuestaVacia> BorrarUsuario(UsuarioBorrar request, ServerCallContext context)
        {
            var usuario = await _context.Usuarios.Include(u => u.rol).FirstOrDefaultAsync(u => u.id == request.Id);
            if (usuario == null) throw new RpcException(new Status(StatusCode.NotFound, "Usuario no encontrado"));

            usuario.estado = Entities.Estado.Eliminado;

            await _context.SaveChangesAsync();
            return new RespuestaVacia();
        }
        [Authorize(Policy = "SupervisorAdministradorPolitica")]
        public override async Task<ListaUsuarios> SeleccionarUsuarios(RespuestaVacia request, ServerCallContext context)
        {
            var usuarios = await _context.Usuarios.Include(u => u.rol).ToListAsync();
            var lista = new ListaUsuarios();
            lista.Usuarios.AddRange(usuarios.Select(u => MapUsuario(u)));
            return lista;
        }
        [Authorize(Policy = "SupervisorAdministradorPolitica")]
        public override async Task<Usuario> ActualizarUsuario(UsuarioActualizar request, ServerCallContext context)
        {
            var usuario = await _context.Usuarios.Include(u => u.rol).FirstOrDefaultAsync(u => u.id == request.Id);
            if (usuario == null) throw new RpcException(new Status(StatusCode.NotFound, "Usuario no encontrado"));

            usuario.email = request.Email;
            usuario.Nombre_usuario = request.NombreUsuario;
            usuario.hash_contrasena = request.HashContrasena;
            usuario.estado = (Entities.Estado)request.Estado;
            usuario.rol = await _context.Roles.FindAsync(request.RolId);

            await _context.SaveChangesAsync();
            return MapUsuario(usuario);
        }

        public override async Task<UsuarioLoginRespuesta> Login(UsuarioLogin request, ServerCallContext context)
        {
            var usuario = await _context.Usuarios
                .Include(u => u.rol)
                .FirstOrDefaultAsync(u => u.Nombre_usuario == request.NombreUsuario && u.hash_contrasena == request.HashContrasena);

            if (usuario == null || usuario.estado == Entities.Estado.Eliminado)
            {
                return new UsuarioLoginRespuesta { Token = "" };
            }

            var tokenProvider = new TokenProvider(_configuration);
            return new UsuarioLoginRespuesta { Token = tokenProvider.Create(usuario) };
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
