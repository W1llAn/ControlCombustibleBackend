using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MicroservicioVehiculos.Protos;
using Google.Protobuf.WellKnownTypes;

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

            // Extraer token JWT
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
            var respuesta = await cliente.GetVehiculoAsync(new GetVehiculoRequest { Id = id }, headers: metadata);
            return Ok(respuesta.Vehiculo);
        }

        [HttpGet("listar")]
        public async Task<IActionResult> ListarVehiculos()
        {
            var cliente = CrearClienteGrpc(out var metadata);
            var respuesta = await cliente.GetAllVehiculosAsync(new MicroservicioVehiculos.Protos.Empty(), headers: metadata);
            return Ok(respuesta.Vehiculos);
        }

        [HttpPost("crear")]
        public async Task<IActionResult> CrearVehiculo([FromBody] VehiculoModel vehiculo)
        {
            var cliente = CrearClienteGrpc(out var metadata);
            var request = new CreateVehiculoRequest { Vehiculo = vehiculo };
            var respuesta = await cliente.CreateVehiculoAsync(request, headers: metadata);
            return Ok(respuesta.Vehiculo);
        }

        [HttpPut("actualizar")]
        public async Task<IActionResult> ActualizarVehiculo([FromBody] VehiculoModel vehiculo)
        {
            var cliente = CrearClienteGrpc(out var metadata);
            var request = new UpdateVehiculoRequest { Vehiculo = vehiculo };
            var respuesta = await cliente.UpdateVehiculoAsync(request, headers: metadata);
            return Ok(respuesta.Vehiculo);
        }

        [HttpDelete("eliminar/{id}")]
        public async Task<IActionResult> EliminarVehiculo(int id)
        {
            var cliente = CrearClienteGrpc(out var metadata);
            var request = new DeleteVehiculoRequest { Id = id };
            var respuesta = await cliente.DeleteVehiculoAsync(request, headers: metadata);
            return Ok(new { Eliminado = respuesta.Success });
        }
    }
}
