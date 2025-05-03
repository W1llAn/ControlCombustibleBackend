using Grpc.Core;
using Microservicio_Administracion.Data;
using MicroservicioAutenticacion.Protos;
using Microsoft.EntityFrameworkCore;

namespace MicroservicioAutenticacion.Controllers
{
    public class UsuariosProtoImpl: UsuariosService.UsuariosServiceBase
    {
        private readonly AppDbContext _context;

        public UsuariosProtoImpl(AppDbContext context)
        {
            _context = context;
        }

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

        public override async Task<RespuestaVacia> BorrarUsuario(UsuarioBorrar request, ServerCallContext context)
        {
            var usuario = await _context.Usuarios.FindAsync(request.Id);
            if (usuario != null)
            {
                usuario.estado = Entities.Estado.Eliminado;
                await _context.SaveChangesAsync();
            }
            return new RespuestaVacia();
        }

        public override async Task<ListaUsuarios> SeleccionarUsuarios(RespuestaVacia request, ServerCallContext context)
        {
            var usuarios = await _context.Usuarios.Include(u => u.rol).ToListAsync();
            var lista = new ListaUsuarios();
            lista.Usuarios.AddRange(usuarios.Select(u => MapUsuario(u)));
            return lista;
        }

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
                .FirstOrDefaultAsync(u => u.Nombre_usuario == request.NombreUsuario && u.hash_contrasena == request.HashContrasena);

            return new UsuarioLoginRespuesta
            {
                Token = usuario != null ? "token_generado_aqui" : ""
            };
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
