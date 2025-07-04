using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using MicroservicioVehiculos.Data;
using MicroservicioVehiculos.Models;
using MicroservicioVehiculos.Protos;
using Microsoft.AspNetCore.Authorization;

namespace MicroservicioVehiculos.Services
{
    public class VehiculoServiceImpl : VehiculoService.VehiculoServiceBase
    {
        private readonly DataContext _context;

        public VehiculoServiceImpl(DataContext context)
        {
            _context = context;
        }

        [Authorize]
        public override async Task<GetVehiculoResponse> GetVehiculo(GetVehiculoRequest request, ServerCallContext context)
        {
            try
            {
                var veh = await _context.Vehiculos.FindAsync(request.Id);
                if (veh == null)
                    throw new RpcException(new Status(StatusCode.NotFound, "Vehículo no encontrado"));

                return new GetVehiculoResponse
                {
                    Vehiculo = MapToModel(veh)
                };
            }
            catch (RpcException) { throw; }
            catch (Exception ex)
            {
                throw new RpcException(new Status(StatusCode.Internal, $"Error al obtener vehículo: {ex.Message}"));
            }
        }

        [Authorize]
        public override async Task<GetAllVehiculosResponse> GetAllVehiculos(Empty request, ServerCallContext context)
        {
            try
            {
                var list = await _context.Vehiculos.ToListAsync();
                var response = new GetAllVehiculosResponse();
                response.Vehiculos.AddRange(list.Select(MapToModel));
                return response;
            }
            catch (Exception ex)
            {
                throw new RpcException(new Status(StatusCode.Internal, $"Error al listar vehículos: {ex.Message}"));
            }
        }

        [Authorize(Policy = "SupervisorAdministradorPolitica")]
        public override async Task<CreateVehiculoResponse> CreateVehiculo(CreateVehiculoRequest request, ServerCallContext context)
        {
            try
            {
                var model = request.Vehiculo;

                if (string.IsNullOrWhiteSpace(model.Placa))
                    throw new RpcException(new Status(StatusCode.InvalidArgument, "La placa es obligatoria."));

                DateTime fechaRegistro;
                if (!DateTime.TryParse(model.FechaRegistro, out fechaRegistro))
                    throw new RpcException(new Status(StatusCode.InvalidArgument, "Fecha de registro inválida."));

                var veh = new Vehiculo
                {
                    placa = model.Placa,
                    tipoMaquinaria = model.TipoMaquinaria,
                    estadoOperativo = model.EstadoOperativo,
                    capacidadCombustible = (decimal)model.CapacidadCombustible,
                    fechaRegistro = fechaRegistro,
                    consumoCombustibleKm = (decimal)model.ConsumoCombustibleKm,
                    estado = model.Estado,
                    descripcion = model.Descripcion,
                    nombre = model.Nombre
                };

                _context.Vehiculos.Add(veh);
                await _context.SaveChangesAsync();

                model.Id = veh.id;
                model.FechaRegistro = veh.fechaRegistro.ToString("o");

                return new CreateVehiculoResponse { Vehiculo = model };
            }
            catch (RpcException) { throw; }
            catch (Exception ex)
            {
                throw new RpcException(new Status(StatusCode.Internal, $"Error al crear vehículo: {ex.Message}"));
            }
        }

        [Authorize(Policy = "SupervisorAdministradorPolitica")]
        public override async Task<UpdateVehiculoResponse> UpdateVehiculo(UpdateVehiculoRequest request, ServerCallContext context)
        {
            try
            {
                var model = request.Vehiculo;
                var veh = await _context.Vehiculos.FindAsync(model.Id);

                if (veh == null)
                    throw new RpcException(new Status(StatusCode.NotFound, "Vehículo no encontrado"));

                if (!DateTime.TryParse(model.FechaRegistro, out var fecha))
                    throw new RpcException(new Status(StatusCode.InvalidArgument, "Fecha de registro inválida."));

                veh.placa = model.Placa;
                veh.tipoMaquinaria = model.TipoMaquinaria;
                veh.estadoOperativo = model.EstadoOperativo;
                veh.capacidadCombustible = (decimal)model.CapacidadCombustible;
                veh.fechaRegistro = fecha;
                veh.consumoCombustibleKm = (decimal)model.ConsumoCombustibleKm;
                veh.estado = model.Estado;
                veh.descripcion = model.Descripcion;
                veh.nombre = model.Nombre;

                await _context.SaveChangesAsync();

                return new UpdateVehiculoResponse { Vehiculo = model };
            }
            catch (RpcException) { throw; }
            catch (Exception ex)
            {
                throw new RpcException(new Status(StatusCode.Internal, $"Error al actualizar vehículo: {ex.Message}"));
            }
        }

        [Authorize(Policy = "SupervisorAdministradorPolitica")]
        public override async Task<DeleteVehiculoResponse> DeleteVehiculo(DeleteVehiculoRequest request, ServerCallContext context)
        {
            try
            {
                var veh = await _context.Vehiculos.FindAsync(request.Id);
                if (veh == null)
                    throw new RpcException(new Status(StatusCode.NotFound, "Vehículo no encontrado"));

                _context.Vehiculos.Remove(veh);
                await _context.SaveChangesAsync();

                return new DeleteVehiculoResponse { Success = true };
            }
            catch (RpcException) { throw; }
            catch (Exception ex)
            {
                throw new RpcException(new Status(StatusCode.Internal, $"Error al eliminar vehículo: {ex.Message}"));
            }
        }

        private VehiculoModel MapToModel(Vehiculo veh)
        {
            return new VehiculoModel
            {
                Id = veh.id,
                Placa = veh.placa,
                TipoMaquinaria = veh.tipoMaquinaria,
                EstadoOperativo = veh.estadoOperativo,
                CapacidadCombustible = (double)veh.capacidadCombustible,
                FechaRegistro = veh.fechaRegistro.ToString("o"),
                ConsumoCombustibleKm = (double)veh.consumoCombustibleKm,
                Estado = veh.estado,
                Nombre = veh.nombre,
                Descripcion = veh.descripcion
            };
        }
    }
}
