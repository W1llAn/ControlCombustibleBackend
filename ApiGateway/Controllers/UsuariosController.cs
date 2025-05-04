using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MicroservicioAutenticacion.Protos;

namespace MicroservicioPuente.Controllers
{
    [ApiController]
    [Route("Usuarios")]
    public class UsuariosController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public UsuariosController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private UsuariosService.UsuariosServiceClient CrearClienteGrpc(out Metadata metadata)
        {
            var url = _configuration["grcp:autenticacion"];

            // Permitir conexiones gRPC sin TLS si usas HTTP
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

            var channel = GrpcChannel.ForAddress(url);
            var client = new UsuariosService.UsuariosServiceClient(channel);

            // Extraer el token del header Authorization
            metadata = new Metadata();
            var authHeader = Request.Headers["Authorization"].ToString();
            if (!string.IsNullOrWhiteSpace(authHeader) && authHeader.StartsWith("Bearer "))
            {
                var token = authHeader.Substring("Bearer ".Length);
                metadata.Add("Authorization", $"Bearer {token}");
            }

            return client;
        }

        [HttpPost("registrar")]
        public async Task<IActionResult> Registrar([FromBody] UsuarioRegistro request)
        {
            var cliente = CrearClienteGrpc(out var metadata);
            var respuesta = await cliente.RegistrarUsuarioAsync(request, headers: metadata);
            return Ok(respuesta);
        }

        [HttpDelete("borrar/{id}")]
        public async Task<IActionResult> Borrar(int id)
        {
            var cliente = CrearClienteGrpc(out var metadata);
            await cliente.BorrarUsuarioAsync(new UsuarioBorrar { Id = id }, headers: metadata);
            return NoContent();
        }

        [HttpGet("listar")]
        public async Task<IActionResult> Listar()
        {
            var cliente = CrearClienteGrpc(out var metadata);
            var lista = await cliente.SeleccionarUsuariosAsync(new RespuestaVacia(), headers: metadata);
            return Ok(lista.Usuarios);
        }

        [HttpPut("actualizar")]
        public async Task<IActionResult> Actualizar([FromBody] UsuarioActualizar request)
        {
            var cliente = CrearClienteGrpc(out var metadata);
            var actualizado = await cliente.ActualizarUsuarioAsync(request, headers: metadata);
            return Ok(actualizado);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UsuarioLogin request)
        {
            // Login no necesita token
            var cliente = CrearClienteGrpc(out _);
            var token = await cliente.LoginAsync(request);
            return Ok(token);
        }
    }
}
