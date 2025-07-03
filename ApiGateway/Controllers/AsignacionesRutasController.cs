using Grpc.Core;
using Grpc.Net.Client;
using MicroservicioRutas.Protos;
using Microsoft.AspNetCore.Mvc;

namespace ApiGateway.Controllers
{
    [ApiController]
    [Route("AsignacionRutas")]
    public class AsignacionRutasController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public AsignacionRutasController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private AsignacionRutasService.AsignacionRutasServiceClient CrearClienteGrpc(out Metadata metadata)
        {
            var url = _configuration["grcp:asignacionrutas"]; // debe estar en appsettings.json o secrets
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            var channel = GrpcChannel.ForAddress(url);
            var client = new AsignacionRutasService.AsignacionRutasServiceClient(channel);

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
        public async Task<IActionResult> ObtenerAsignacion(int id)
        {
            var cliente = CrearClienteGrpc(out var metadata);
            var respuesta = await cliente.ObtenerAsignacionRutaIdAsync(new ObtenerRutaAsignadaRequest { Id = id }, headers: metadata);
            return Ok(respuesta);
        }

        [HttpGet("listar")]
        public async Task<IActionResult> ObtenerTodas()
        {
            var cliente = CrearClienteGrpc(out var metadata);
            var respuesta = await cliente.ObtenerTodasRutasAsignadasAsync(new RespuestaVaciaAsignadaRuta(), headers: metadata);
            return Ok(respuesta.AsignacionRuta);
        }

        [HttpPost("crear")]
        public async Task<IActionResult> CrearAsignacion([FromBody] CrearAsignacionRutaRequest request)
        {
            var cliente = CrearClienteGrpc(out var metadata);
            var respuesta = await cliente.CrearAsignacionRutaAsync(request, headers: metadata);
            return Ok(respuesta);
        }

        [HttpPut("actualizar")]
        public async Task<IActionResult> ActualizarAsignacion([FromBody] ActualizarAsignacionRutaRequest request)
        {
            var cliente = CrearClienteGrpc(out var metadata);
            var respuesta = await cliente.ActualizarRutaAsignadaAsync(request, headers: metadata);
            return Ok(respuesta);
        }

        [HttpDelete("eliminar/{id}")]
        public async Task<IActionResult> EliminarAsignacion(int id)
        {
            var cliente = CrearClienteGrpc(out var metadata);
            var respuesta = await cliente.EliminarAsignacionRutaAsync(new EliminarAsignacionRutaRequest { Id = id }, headers: metadata);
            return Ok(new { Exito = respuesta.Exito });
        }
    }
}
