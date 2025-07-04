using Grpc.Core;
using Grpc.Net.Client;
using MicroservicioConsumoCombustible.Protos;
using Microsoft.AspNetCore.Mvc;

namespace ApiGateway.Controllers
{
    [ApiController]
    [Route("ConsumoCombustible")]
    public class ConsumoCombustibleController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public ConsumoCombustibleController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private ConsumoCombustiblesService.ConsumoCombustiblesServiceClient CrearClienteGrpc(out Metadata metadata)
        {
            var url = _configuration["grcp:consumocombustible"]; // Url del microservicio en appsettings.json o secrets
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            var channel = GrpcChannel.ForAddress(url);
            var client = new ConsumoCombustiblesService.ConsumoCombustiblesServiceClient(channel);

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
        public async Task<IActionResult> ObtenerConsumo(int id)
        {
            var client = CrearClienteGrpc(out var metadata);
            var response = await client.ObtenerConsumoCombustibleIdAsync(new ObtenerConsumoCombustibleRequest { Id = id }, headers: metadata);
            return Ok(response);
        }

        [HttpGet("listar")]
        public async Task<IActionResult> ObtenerTodos()
        {
            var client = CrearClienteGrpc(out var metadata);
            var response = await client.ObtenerTodosConsumosCombustibleAsync(new RespuestaVaciaConsumoCombustible(), headers: metadata);
            return Ok(response.AsignacionConsumoCombustible);
        }

        [HttpPost("crear")]
        public async Task<IActionResult> CrearConsumo([FromBody] CrearConsumoCombustibleRequest request)
        {
            var client = CrearClienteGrpc(out var metadata);
            var response = await client.CrearConsumoCombustibleAsync(request, headers: metadata);
            return Ok(response);
        }

        [HttpPut("actualizar")]
        public async Task<IActionResult> ActualizarConsumo([FromBody] ActualizarConsumoCombustibleRequest request)
        {
            var client = CrearClienteGrpc(out var metadata);
            var response = await client.ActualizarConsumoCombustibleAsync(request, headers: metadata);
            return Ok(response);
        }

        [HttpDelete("eliminar/{id}")]
        public async Task<IActionResult> EliminarConsumo(int id)
        {
            var client = CrearClienteGrpc(out var metadata);
            var response = await client.EliminarConsumoCombustibleAsync(new EliminarConsumoCombustibleRequest { Id = id }, headers: metadata);
            return Ok(new { Exito = response.Exito });
        }
    }
}
