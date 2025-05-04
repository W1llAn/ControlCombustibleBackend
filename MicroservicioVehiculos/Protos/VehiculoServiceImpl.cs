using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using MicroservicioVehiculos.Data;
using MicroservicioVehiculos.Models;
using MicroservicioVehiculos.Protos;

namespace MicroservicioVehiculos.Services
{
    public class VehiculoServiceImpl : VehiculoService.VehiculoServiceBase
    {
        private readonly DataContext _context;

        public VehiculoServiceImpl(DataContext context)
        {
            _context = context;
        }

        public override async Task<GetVehiculoResponse> GetVehiculo(GetVehiculoRequest request, ServerCallContext context)
        {
            var veh = await _context.Vehiculos.FindAsync(request.Id);
            if (veh == null)
                throw new RpcException(new Status(StatusCode.NotFound, "Vehículo no encontrado"));

            return new GetVehiculoResponse
            {
                Vehiculo = new VehiculoModel
                {
                    Id = veh.id,
                    Placa = veh.placa,
                    TipoMaquinaria = veh.tipoMaquinaria,
                    EstadoOperativo = veh.estadoOperativo,
                    CapacidadCombustible = (double)veh.capacidadCombustible,
                    FechaRegistro = veh.fechaRegistro.ToString("o"),
                    ConsumoCombustibleKm = (double)veh.consumoCombustibleKm,
                    Estado = veh.estado
                }
            };
        }

        public override async Task<GetAllVehiculosResponse> GetAllVehiculos(Empty request, ServerCallContext context)
        {
            var list = await _context.Vehiculos.ToListAsync();
            var response = new GetAllVehiculosResponse();
            response.Vehiculos.AddRange(list.Select(veh => new VehiculoModel
            {
                Id = veh.id,
                Placa = veh.placa,
                TipoMaquinaria = veh.tipoMaquinaria,
                EstadoOperativo = veh.estadoOperativo,
                CapacidadCombustible = (double)veh.capacidadCombustible,
                FechaRegistro = veh.fechaRegistro.ToString("o"),
                ConsumoCombustibleKm = (double)veh.consumoCombustibleKm,
                Estado = veh.estado
            }));
            return response;
        }

        public override async Task<CreateVehiculoResponse> CreateVehiculo(CreateVehiculoRequest request, ServerCallContext context)
        {
            var model = request.Vehiculo;
            var veh = new Vehiculo
            {
                placa = model.Placa,
                tipoMaquinaria = model.TipoMaquinaria,
                estadoOperativo = model.EstadoOperativo,
                capacidadCombustible = (decimal)model.CapacidadCombustible,
                fechaRegistro = DateTime.Parse(model.FechaRegistro),
                consumoCombustibleKm = (decimal)model.ConsumoCombustibleKm,
                estado = model.Estado
            };
            _context.Vehiculos.Add(veh);
            await _context.SaveChangesAsync();

            model.Id = veh.id;
            model.FechaRegistro = veh.fechaRegistro.ToString("o");
            return new CreateVehiculoResponse { Vehiculo = model };
        }

        public override async Task<UpdateVehiculoResponse> UpdateVehiculo(UpdateVehiculoRequest request, ServerCallContext context)
        {
            var model = request.Vehiculo;
            var veh = await _context.Vehiculos.FindAsync(model.Id);
            if (veh == null)
                throw new RpcException(new Status(StatusCode.NotFound, "Vehículo no encontrado"));

            veh.placa = model.Placa;
            veh.tipoMaquinaria = model.TipoMaquinaria;
            veh.estadoOperativo = model.EstadoOperativo;
            veh.capacidadCombustible = (decimal)model.CapacidadCombustible;
            veh.fechaRegistro = DateTime.Parse(model.FechaRegistro);
            veh.consumoCombustibleKm = (decimal)model.ConsumoCombustibleKm;
            veh.estado = model.Estado;

            await _context.SaveChangesAsync();

            return new UpdateVehiculoResponse { Vehiculo = model };
        }

        public override async Task<DeleteVehiculoResponse> DeleteVehiculo(DeleteVehiculoRequest request, ServerCallContext context)
        {
            var veh = await _context.Vehiculos.FindAsync(request.Id);
            if (veh == null)
                throw new RpcException(new Status(StatusCode.NotFound, "Vehículo no encontrado"));

            _context.Vehiculos.Remove(veh);
            await _context.SaveChangesAsync();
            return new DeleteVehiculoResponse { Success = true };
        }
    }
}
