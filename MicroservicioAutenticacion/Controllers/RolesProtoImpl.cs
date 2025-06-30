using Grpc.Core;
using Microservicio_Administracion.Data;
using MicroservicioAutenticacion.Protos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace MicroservicioAutenticacion.Controllers
{
        public class RolesProtoImpl : RolesService.RolesServiceBase
        {
            private readonly AppDbContext _context;

            public RolesProtoImpl(AppDbContext context)
            {
                _context = context;
            }

            [Authorize(Policy = "AdministradorPolitica")]
            public override async Task<Rol> CrearRol(RolCrear request, ServerCallContext context)
            {
                var rol = new Entities.Rol
                {
                    nombre = request.Nombre,
                    descripcion = request.Descripcion
                };

                _context.Roles.Add(rol);
                await _context.SaveChangesAsync();

                return MapRol(rol);
            }

            [Authorize(Policy = "AdministradorPolitica")]
            public override async Task<Rol> ActualizarRol(RolActualizar request, ServerCallContext context)
            {
                var rol = await _context.Roles.FindAsync(request.Id);
                if (rol == null)
                    throw new RpcException(new Status(StatusCode.NotFound, "Rol no encontrado"));

                rol.nombre = request.Nombre;
                rol.descripcion = request.Descripcion;
                await _context.SaveChangesAsync();

                return MapRol(rol);
            }

            [Authorize(Policy = "AdministradorPolitica")]
            public override async Task<RespuestaVacia> EliminarRol(RolEliminar request, ServerCallContext context)
            {
                var rol = await _context.Roles.FindAsync(request.Id);
                if (rol == null)
                    throw new RpcException(new Status(StatusCode.NotFound, "Rol no encontrado"));

                _context.Roles.Remove(rol);
                await _context.SaveChangesAsync();

                return new RespuestaVacia();
            }

            [Authorize]
            public override async Task<ListaRoles> ListarRoles(RespuestaVacia request, ServerCallContext context)
            {
                var roles = await _context.Roles.ToListAsync();
                var lista = new ListaRoles();
                lista.Roles.AddRange(roles.Select(MapRol));
                return lista;
            }

            [Authorize]
            public override async Task<Rol> ObtenerRol(RolObtener request, ServerCallContext context)
            {
                var rol = await _context.Roles.FindAsync(request.Id);
                if (rol == null)
                    throw new RpcException(new Status(StatusCode.NotFound, "Rol no encontrado"));

                return MapRol(rol);
            }

            private Rol MapRol(Entities.Rol rol)
            {
                return new Rol
                {
                    Id = rol.id,
                    Nombre = rol.nombre,
                    Descripcion = rol.descripcion
                };
            }
        }
    
}
