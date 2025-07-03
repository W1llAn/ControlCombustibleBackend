using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microservicio_Rutas.Protos;

namespace ApiGateway.Controllers
{
    [ApiController]
    [Route("Rutas")]
    public class RutasController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public RutasController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private RutasService.RutasServiceClient CrearClienteGrpc(out Metadata metadata)
        {
            var url = _configuration["grcp:asignacionrutas"]; // Debe coincidir con appsettings.json

            if (string.IsNullOrWhiteSpace(url))
                throw new Exception("No se encontró la configuración 'grpc:rutas' en appsettings.json");

            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            var channel = GrpcChannel.ForAddress(url);
            var client = new RutasService.RutasServiceClient(channel);

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
        public async Task<IActionResult> ObtenerRuta(int id)
        {
            var cliente = CrearClienteGrpc(out var metadata);
            var respuesta = await cliente.ObtenerRutaIdAsync(new ObtenerRutaRequest { Id = id }, headers: metadata);
            return Ok(respuesta);
        }

        [HttpGet("listar")]
        public async Task<IActionResult> ObtenerTodas()
        {
            var cliente = CrearClienteGrpc(out var metadata);
            var respuesta = await cliente.ObtenerTodasRutasAsync(new RespuestaVaciaRuta(), headers: metadata);
            return Ok(respuesta.Rutas);
        }

        [HttpPost("crear")]
        public async Task<IActionResult> CrearRuta([FromBody] CrearRutaRequest request)
        {
            var cliente = CrearClienteGrpc(out var metadata);
            var respuesta = await cliente.CrearRutaAsync(request, headers: metadata);
            return Ok(respuesta);
        }

        [HttpPut("actualizar")]
        public async Task<IActionResult> ActualizarRuta([FromBody] ActualizarRutaRequest request)
        {
            var cliente = CrearClienteGrpc(out var metadata);
            var respuesta = await cliente.ActualizarRutaAsync(request, headers: metadata);
            return Ok(respuesta);
        }

        [HttpDelete("eliminar/{id}")]
        public async Task<IActionResult> EliminarRuta(int id)
        {
            var cliente = CrearClienteGrpc(out var metadata);
            var respuesta = await cliente.EliminarRutaAsync(new EliminarRutaRequest { Id = id }, headers: metadata);
            return Ok(new { Exito = respuesta.Exito });
        }
    }
}
