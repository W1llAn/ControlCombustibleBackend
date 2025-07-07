using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MicroservicioVehiculos.Protos;
using Google.Protobuf.WellKnownTypes;
using GrpcStatusCode = Grpc.Core.StatusCode;
using MicroservicioChoferes.Protos;

namespace MicroservicioPuente.Controllers
{
    [ApiController]
    [Route("Vehiculos")]
    public class VehiculosController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public VehiculosController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private async Task<VehiculoService.VehiculoServiceClient> CrearClienteGrpc(string tipoMaquinaria)
        {

            var url = tipoMaquinaria switch
            {
                "Pesada" => _configuration["grcp:vehiculos-pesados"],
                "Liviana" => _configuration["grcp:vehiculos-livianos"],
                _ => throw new Exception("Tipo de maquinaria no reconocido.")
            };

            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            var channel = GrpcChannel.ForAddress(url);
            return new VehiculoService.VehiculoServiceClient(channel);
        }


        [HttpGet("Liviana/{id}")]
        public async Task<IActionResult> ObtenerVehiculoLiviana(int id)
        {
            var cliente = await CrearClienteGrpc("Liviana");
            try
            {
                var respuesta = await cliente.GetVehiculoAsync(new GetVehiculoRequest { Id = id }, headers: CrearMetadataDesdeToken());
                return Ok(respuesta.Vehiculo);
            }
            catch (RpcException ex)
            {
                return HandleRpcException(ex);
            }
        }
        [HttpGet("Pesada/{id}")]
        public async Task<IActionResult> ObtenerVehiculoPesada(int id)
        {
            var cliente = await CrearClienteGrpc("Pesada");
            try
            {
                var respuesta = await cliente.GetVehiculoAsync(new GetVehiculoRequest { Id = id }, headers: CrearMetadataDesdeToken());
                return Ok(respuesta.Vehiculo);
            }
            catch (RpcException ex)
            {
                return HandleRpcException(ex);
            }
        }

        [HttpGet("listar")]
        public async Task<IActionResult> ListarVehiculos()
        {
            var cliente = await CrearClienteGrpc("Liviana");
            var cliente1 = await CrearClienteGrpc("Pesada");
            try
            {
                var respuesta = await cliente.GetAllVehiculosAsync(new MicroservicioVehiculos.Protos.Empty(), headers: CrearMetadataDesdeToken());
                var respuesta1 = await cliente1.GetAllVehiculosAsync(new MicroservicioVehiculos.Protos.Empty(), headers: CrearMetadataDesdeToken());
                respuesta.Vehiculos.Add(respuesta1.Vehiculos);
                return Ok(respuesta.Vehiculos);
            }
            catch (RpcException ex)
            {
                return HandleRpcException(ex);
            }
        }

        [HttpPost("crear")]
        public async Task<IActionResult> CrearVehiculo([FromBody] VehiculoModel vehiculo)
        {
            var cliente = await CrearClienteGrpc(vehiculo.TipoMaquinaria);
            try
            {
                var request = new CreateVehiculoRequest { Vehiculo = vehiculo };
                var respuesta = await cliente.CreateVehiculoAsync(request, headers: CrearMetadataDesdeToken());
                return Ok(respuesta.Vehiculo);
            }
            catch (RpcException ex)
            {
                return HandleRpcException(ex);
            }
        }

        [HttpPut("actualizar")]
        public async Task<IActionResult> ActualizarVehiculo([FromBody] UpdateVehiculoRequestApiGateway vehiculo)
        {
            try
            {
                var clienteAnterior = await CrearClienteGrpc(vehiculo.TipoMaquinariaAnterior);
                var cliente = await CrearClienteGrpc(vehiculo.TipoMaquinaria);

                await clienteAnterior.DeleteVehiculoAsync(new DeleteVehiculoRequest {Id=vehiculo.Id },headers: CrearMetadataDesdeToken());

                var request =  new CreateVehiculoRequest { 
                Vehiculo= new VehiculoModel
                {
                    CapacidadCombustible = vehiculo.CapacidadCombustible,
                    ConsumoCombustibleKm = vehiculo.ConsumoCombustibleKm,
                    Descripcion = vehiculo.Descripcion,
                    Estado = vehiculo.Estado,
                    EstadoOperativo = vehiculo.EstadoOperativo,
                    FechaRegistro = vehiculo.FechaRegistro,
                    Id = vehiculo.Id,
                    Nombre = vehiculo.Nombre,
                    Placa = vehiculo.Placa,
                    TipoMaquinaria = vehiculo.TipoMaquinaria

                }
                };
                var respuesta = await cliente.CreateVehiculoAsync(request, headers: CrearMetadataDesdeToken());
                return Ok(respuesta.Vehiculo);
            }
            catch (RpcException ex)
            {
                return HandleRpcException(ex);
            }
        }

        [HttpDelete("eliminar/Pesada/{id}")]
        public async Task<IActionResult> EliminarVehiculoPesada(int id)
        {
            var cliente =await CrearClienteGrpc("Pesada");
            try
            {
                var request = new DeleteVehiculoRequest { Id = id };
                var respuesta = await cliente.DeleteVehiculoAsync(request, headers: CrearMetadataDesdeToken());
                return Ok(new { Eliminado = respuesta.Success });
            }
            catch (RpcException ex)
            {
                return HandleRpcException(ex);
            }
        }

        [HttpDelete("eliminar/Liviana/{id}")]
        public async Task<IActionResult> EliminarVehiculoLiviana(int id)
        {
            var cliente = await CrearClienteGrpc("Liviana");
            try
            {
                var request = new DeleteVehiculoRequest { Id = id };
                var respuesta = await cliente.DeleteVehiculoAsync(request, headers: CrearMetadataDesdeToken());
                return Ok(new { Eliminado = respuesta.Success });
            }
            catch (RpcException ex)
            {
                return HandleRpcException(ex);
            }
        }

        // Método reutilizable para mapear errores gRPC → HTTP
        private IActionResult HandleRpcException(RpcException ex)
        {
            return ex.Status.StatusCode switch
            {
                GrpcStatusCode.NotFound => NotFound(new { error = ex.Status.Detail }),
                GrpcStatusCode.InvalidArgument => BadRequest(new { error = ex.Status.Detail }),
                GrpcStatusCode.PermissionDenied => StatusCode(403, new { error = ex.Status.Detail }),
                GrpcStatusCode.Unauthenticated => Unauthorized(new { error = ex.Status.Detail }),
                _ => StatusCode(500, new { error = $"Error interno: {ex.Status.Detail}" })
            };
        }

        private Metadata CrearMetadataDesdeToken()
        {
            var metadata = new Metadata();
            var authHeader = Request.Headers["Authorization"].ToString();
            if (!string.IsNullOrWhiteSpace(authHeader) && authHeader.StartsWith("Bearer "))
            {
                var token = authHeader.Substring("Bearer ".Length);
                metadata.Add("Authorization", $"Bearer {token}");
            }
            return metadata;
        }


    }
}
