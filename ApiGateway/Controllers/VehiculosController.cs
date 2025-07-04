using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MicroservicioVehiculos.Protos;
using Google.Protobuf.WellKnownTypes;
using GrpcStatusCode = Grpc.Core.StatusCode;

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

        private VehiculoService.VehiculoServiceClient CrearClienteGrpc(out Metadata metadata)
        {
            var url = _configuration["grcp:vehiculos-livianos"];
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            var channel = GrpcChannel.ForAddress(url);
            var client = new VehiculoService.VehiculoServiceClient(channel);

            metadata = new Metadata();
            var authHeader = Request.Headers["Authorization"].ToString();
            if (!string.IsNullOrWhiteSpace(authHeader) && authHeader.StartsWith("Bearer "))
            {
                var token = authHeader.Substring("Bearer ".Length);
                metadata.Add("Authorization", $"Bearer {token}");
            }

            return client;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> ObtenerVehiculo(int id)
        {
            var cliente = CrearClienteGrpc(out var metadata);
            try
            {
                var respuesta = await cliente.GetVehiculoAsync(new GetVehiculoRequest { Id = id }, headers: metadata);
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
            var cliente = CrearClienteGrpc(out var metadata);
            try
            {
                var respuesta = await cliente.GetAllVehiculosAsync(new MicroservicioVehiculos.Protos.Empty(), headers: metadata);
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
            var cliente = CrearClienteGrpc(out var metadata);
            try
            {
                var request = new CreateVehiculoRequest { Vehiculo = vehiculo };
                var respuesta = await cliente.CreateVehiculoAsync(request, headers: metadata);
                return Ok(respuesta.Vehiculo);
            }
            catch (RpcException ex)
            {
                return HandleRpcException(ex);
            }
        }

        [HttpPut("actualizar")]
        public async Task<IActionResult> ActualizarVehiculo([FromBody] VehiculoModel vehiculo)
        {
            var cliente = CrearClienteGrpc(out var metadata);
            try
            {
                var request = new UpdateVehiculoRequest { Vehiculo = vehiculo };
                var respuesta = await cliente.UpdateVehiculoAsync(request, headers: metadata);
                return Ok(respuesta.Vehiculo);
            }
            catch (RpcException ex)
            {
                return HandleRpcException(ex);
            }
        }

        [HttpDelete("eliminar/{id}")]
        public async Task<IActionResult> EliminarVehiculo(int id)
        {
            var cliente = CrearClienteGrpc(out var metadata);
            try
            {
                var request = new DeleteVehiculoRequest { Id = id };
                var respuesta = await cliente.DeleteVehiculoAsync(request, headers: metadata);
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
    }
}
